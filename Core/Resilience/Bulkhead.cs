using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Prolab_4.Core.Logging;

namespace Prolab_4.Core.Resilience
{
    /// <summary>
    /// Bulkhead Pattern - Kaynak izolasyonu ve eş zamanlılık kontrolü.
    /// Belirli bir operasyona eş zamanlı erişim sayısını sınırlar.
    /// </summary>
    public class Bulkhead : IDisposable
    {
        #region Fields
        
        private readonly string _name;
        private readonly SemaphoreSlim _semaphore;
        private readonly int _maxConcurrency;
        private readonly int _maxQueueLength;
        private readonly TimeSpan _timeout;
        
        private int _currentQueueLength;
        private int _executingCount;
        private long _totalExecutions;
        private long _rejectedCount;
        private long _timeoutCount;
        private bool _disposed;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Bulkhead adı.
        /// </summary>
        public string Name => _name;
        
        /// <summary>
        /// Şu an çalışan operasyon sayısı.
        /// </summary>
        public int ExecutingCount => _executingCount;
        
        /// <summary>
        /// Kuyrukta bekleyen sayısı.
        /// </summary>
        public int QueueLength => _currentQueueLength;
        
        /// <summary>
        /// Mevcut kapasite.
        /// </summary>
        public int AvailableCount => _semaphore.CurrentCount;
        
        /// <summary>
        /// Toplam çalıştırılan operasyon sayısı.
        /// </summary>
        public long TotalExecutions => _totalExecutions;
        
        /// <summary>
        /// Reddedilen operasyon sayısı.
        /// </summary>
        public long RejectedCount => _rejectedCount;
        
        /// <summary>
        /// Timeout olan operasyon sayısı.
        /// </summary>
        public long TimeoutCount => _timeoutCount;
        
        /// <summary>
        /// Kullanım oranı (0-1).
        /// </summary>
        public double Utilization => (double)(_maxConcurrency - _semaphore.CurrentCount) / _maxConcurrency;
        
        #endregion

        #region Events
        
        /// <summary>
        /// Bulkhead dolduğunda tetiklenir.
        /// </summary>
        public event EventHandler<BulkheadFullEventArgs> OnBulkheadFull;
        
        /// <summary>
        /// Operasyon reddedildiğinde tetiklenir.
        /// </summary>
        public event EventHandler<BulkheadRejectedEventArgs> OnRejected;
        
        #endregion

        #region Constructor
        
        /// <summary>
        /// Bulkhead oluşturur.
        /// </summary>
        /// <param name="name">Bulkhead adı</param>
        /// <param name="maxConcurrency">Maksimum eş zamanlı operasyon</param>
        /// <param name="maxQueueLength">Maksimum kuyruk uzunluğu (0 = kuyruk yok)</param>
        /// <param name="timeoutSeconds">Kuyrukta bekleme timeout süresi</param>
        public Bulkhead(string name, int maxConcurrency, int maxQueueLength = 0, int timeoutSeconds = 30)
        {
            if (maxConcurrency <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Must be > 0");
            
            _name = name ?? "Default";
            _maxConcurrency = maxConcurrency;
            _maxQueueLength = maxQueueLength;
            _timeout = TimeSpan.FromSeconds(timeoutSeconds);
            _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        }
        
        #endregion

        #region Execute Methods
        
        /// <summary>
        /// Operasyonu bulkhead ile çalıştırır.
        /// </summary>
        public async Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            // Kuyruk kontrolü
            if (_maxQueueLength > 0)
            {
                var currentQueue = Interlocked.Increment(ref _currentQueueLength);
                if (currentQueue > _maxQueueLength + _maxConcurrency)
                {
                    Interlocked.Decrement(ref _currentQueueLength);
                    Interlocked.Increment(ref _rejectedCount);
                    
                    OnRejected?.Invoke(this, new BulkheadRejectedEventArgs(_name, "Queue full"));
                    throw new BulkheadRejectedException(_name, "Kuyruk dolu");
                }
            }
            
            bool acquired = false;
            
            try
            {
                // Semaphore'u al
                acquired = await _semaphore.WaitAsync(_timeout, cancellationToken);
                
                if (!acquired)
                {
                    Interlocked.Increment(ref _timeoutCount);
                    
                    if (_maxQueueLength > 0)
                        Interlocked.Decrement(ref _currentQueueLength);
                    
                    throw new BulkheadRejectedException(_name, "Timeout bekleniyor");
                }
                
                if (_maxQueueLength > 0)
                    Interlocked.Decrement(ref _currentQueueLength);
                
                Interlocked.Increment(ref _executingCount);
                Interlocked.Increment(ref _totalExecutions);
                
                // Dolu kontrolü
                if (_semaphore.CurrentCount == 0)
                {
                    OnBulkheadFull?.Invoke(this, new BulkheadFullEventArgs(_name, _maxConcurrency));
                    Logger.Warning($"[Bulkhead:{_name}] Full - {_maxConcurrency} concurrent operations");
                }
                
                return await operation(cancellationToken);
            }
            finally
            {
                if (acquired)
                {
                    Interlocked.Decrement(ref _executingCount);
                    _semaphore.Release();
                }
            }
        }
        
        /// <summary>
        /// Operasyonu bulkhead ile çalıştırır (basit overload).
        /// </summary>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            return await ExecuteAsync(_ => operation());
        }
        
        /// <summary>
        /// Void operasyonu bulkhead ile çalıştırır.
        /// </summary>
        public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            await ExecuteAsync(async _ =>
            {
                await operation();
                return true;
            }, cancellationToken);
        }
        
        /// <summary>
        /// Senkron operasyonu bulkhead ile çalıştırır.
        /// </summary>
        public T Execute<T>(Func<T> operation)
        {
            ThrowIfDisposed();
            
            bool acquired = _semaphore.Wait(_timeout);
            
            if (!acquired)
            {
                Interlocked.Increment(ref _timeoutCount);
                throw new BulkheadRejectedException(_name, "Timeout");
            }
            
            try
            {
                Interlocked.Increment(ref _executingCount);
                Interlocked.Increment(ref _totalExecutions);
                return operation();
            }
            finally
            {
                Interlocked.Decrement(ref _executingCount);
                _semaphore.Release();
            }
        }
        
        #endregion

        #region Utility Methods
        
        /// <summary>
        /// Bulkhead istatistiklerini sıfırlar.
        /// </summary>
        public void ResetStats()
        {
            Interlocked.Exchange(ref _totalExecutions, 0);
            Interlocked.Exchange(ref _rejectedCount, 0);
            Interlocked.Exchange(ref _timeoutCount, 0);
        }
        
        /// <summary>
        /// Bulkhead durumunu raporlar.
        /// </summary>
        public BulkheadStatus GetStatus()
        {
            return new BulkheadStatus
            {
                Name = _name,
                MaxConcurrency = _maxConcurrency,
                CurrentConcurrency = _executingCount,
                QueueLength = _currentQueueLength,
                MaxQueueLength = _maxQueueLength,
                AvailableSlots = _semaphore.CurrentCount,
                TotalExecutions = _totalExecutions,
                RejectedCount = _rejectedCount,
                TimeoutCount = _timeoutCount,
                Utilization = Utilization
            };
        }
        
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Bulkhead));
        }
        
        #endregion

        #region IDisposable
        
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _semaphore?.Dispose();
        }
        
        #endregion
    }
    
    #region Supporting Types
    
    /// <summary>
    /// Bulkhead durum bilgisi.
    /// </summary>
    public class BulkheadStatus
    {
        public string Name { get; set; }
        public int MaxConcurrency { get; set; }
        public int CurrentConcurrency { get; set; }
        public int QueueLength { get; set; }
        public int MaxQueueLength { get; set; }
        public int AvailableSlots { get; set; }
        public long TotalExecutions { get; set; }
        public long RejectedCount { get; set; }
        public long TimeoutCount { get; set; }
        public double Utilization { get; set; }
        
        public override string ToString()
        {
            return $"Bulkhead[{Name}]: {CurrentConcurrency}/{MaxConcurrency} active, " +
                   $"{QueueLength}/{MaxQueueLength} queued, {Utilization:P0} utilized";
        }
    }
    
    /// <summary>
    /// Bulkhead dolu event args.
    /// </summary>
    public class BulkheadFullEventArgs : EventArgs
    {
        public string BulkheadName { get; }
        public int MaxConcurrency { get; }
        
        public BulkheadFullEventArgs(string name, int maxConcurrency)
        {
            BulkheadName = name;
            MaxConcurrency = maxConcurrency;
        }
    }
    
    /// <summary>
    /// Bulkhead reddetti event args.
    /// </summary>
    public class BulkheadRejectedEventArgs : EventArgs
    {
        public string BulkheadName { get; }
        public string Reason { get; }
        
        public BulkheadRejectedEventArgs(string name, string reason)
        {
            BulkheadName = name;
            Reason = reason;
        }
    }
    
    /// <summary>
    /// Bulkhead reddetti exception.
    /// </summary>
    public class BulkheadRejectedException : Exception
    {
        public string BulkheadName { get; }
        
        public BulkheadRejectedException(string bulkheadName, string reason)
            : base($"Bulkhead '{bulkheadName}' operasyonu reddetti: {reason}")
        {
            BulkheadName = bulkheadName;
        }
    }
    
    #endregion
    
    #region Bulkhead Registry
    
    /// <summary>
    /// Global bulkhead registry - isimlere göre bulkhead yönetimi.
    /// </summary>
    public static class BulkheadRegistry
    {
        private static readonly ConcurrentDictionary<string, Bulkhead> _bulkheads 
            = new ConcurrentDictionary<string, Bulkhead>();
        
        /// <summary>
        /// Bulkhead alır veya oluşturur.
        /// </summary>
        public static Bulkhead GetOrCreate(string name, int maxConcurrency, int maxQueueLength = 0)
        {
            return _bulkheads.GetOrAdd(name, n => new Bulkhead(n, maxConcurrency, maxQueueLength));
        }
        
        /// <summary>
        /// Belirli bir bulkhead'i alır.
        /// </summary>
        public static Bulkhead Get(string name)
        {
            return _bulkheads.TryGetValue(name, out var bulkhead) ? bulkhead : null;
        }
        
        /// <summary>
        /// Tüm bulkhead durumlarını alır.
        /// </summary>
        public static IEnumerable<BulkheadStatus> GetAllStatuses()
        {
            foreach (var bulkhead in _bulkheads.Values)
            {
                yield return bulkhead.GetStatus();
            }
        }
        
        /// <summary>
        /// Bulkhead'i kaldırır.
        /// </summary>
        public static bool Remove(string name)
        {
            if (_bulkheads.TryRemove(name, out var bulkhead))
            {
                bulkhead.Dispose();
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Tüm bulkhead'leri temizler.
        /// </summary>
        public static void Clear()
        {
            foreach (var bulkhead in _bulkheads.Values)
            {
                bulkhead.Dispose();
            }
            _bulkheads.Clear();
        }
    }
    
    #endregion
}
