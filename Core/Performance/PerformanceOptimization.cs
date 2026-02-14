using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Prolab_4.Core.Logging;
using Prolab_4.Core.Metrics;

namespace Prolab_4.Core.Performance
{
    #region Memory Pool

    /// <summary>
    /// High-performance object pool - nesneleri yeniden kullanarak GC baskısını azaltır.
    /// Note: CacheHelpers.cs'deki ObjectPool ile karıştırılmamalı - bu versiyon new() constraint içerir.
    /// </summary>
    public class HighPerformancePool<T> where T : class, new()
    {
        private readonly ConcurrentBag<T> _objects;
        private readonly Func<T> _factory;
        private readonly Action<T> _reset;
        private readonly int _maxSize;
        private int _currentSize;

        public HighPerformancePool(int maxSize = 100, Func<T> factory = null, Action<T> reset = null)
        {
            _maxSize = maxSize;
            _factory = factory ?? (() => new T());
            _reset = reset;
            _objects = new ConcurrentBag<T>();
        }

        public int Count => _objects.Count;
        public int TotalCreated => _currentSize;

        public T Rent()
        {
            if (_objects.TryTake(out var item))
            {
                return item;
            }

            Interlocked.Increment(ref _currentSize);
            return _factory();
        }

        public void Return(T item)
        {
            if (item == null) return;

            _reset?.Invoke(item);

            if (_objects.Count < _maxSize)
            {
                _objects.Add(item);
            }
        }

        public PooledItem<T> Get()
        {
            return new PooledItem<T>(this, Rent());
        }
    }

    /// <summary>
    /// Pooled object wrapper - using ile otomatik return.
    /// </summary>
    public struct PooledItem<T> : IDisposable where T : class, new()
    {
        private readonly HighPerformancePool<T> _pool;
        private T _value;

        public PooledItem(HighPerformancePool<T> pool, T value)
        {
            _pool = pool;
            _value = value;
        }

        public T Value => _value;

        public void Dispose()
        {
            if (_value != null)
            {
                _pool.Return(_value);
                _value = null;
            }
        }
    }

    #endregion

    #region StringBuilder Pool

    /// <summary>
    /// StringBuilder pool - string birleştirme için optimize.
    /// </summary>
    public static class StringBuilderPool
    {
        private static readonly HighPerformancePool<System.Text.StringBuilder> _pool =
            new HighPerformancePool<System.Text.StringBuilder>(
                maxSize: 50,
                factory: () => new System.Text.StringBuilder(256),
                reset: sb => sb.Clear());

        public static PooledStringBuilder Get() => new PooledStringBuilder();

        public struct PooledStringBuilder : IDisposable
        {
            private System.Text.StringBuilder _builder;

            public PooledStringBuilder()
            {
                _builder = _pool.Rent();
            }

            public System.Text.StringBuilder Builder => _builder;

            public override string ToString() => _builder?.ToString() ?? string.Empty;

            public void Dispose()
            {
                if (_builder != null)
                {
                    _pool.Return(_builder);
                    _builder = null;
                }
            }
        }
    }

    #endregion

    #region List Pool

    /// <summary>
    /// Generic list pool.
    /// </summary>
    public static class ListPool<T>
    {
        private static readonly HighPerformancePool<List<T>> _pool =
            new HighPerformancePool<List<T>>(
                maxSize: 50,
                factory: () => new List<T>(32),
                reset: list => list.Clear());

        public static List<T> Rent() => _pool.Rent();
        public static void Return(List<T> list) => _pool.Return(list);

        public static PooledList Get() => new PooledList();

        public struct PooledList : IDisposable
        {
            private List<T> _list;

            public PooledList()
            {
                _list = _pool.Rent();
            }

            public List<T> List => _list;

            public void Dispose()
            {
                if (_list != null)
                {
                    _pool.Return(_list);
                    _list = null;
                }
            }
        }
    }

    #endregion

    #region Dictionary Pool

    /// <summary>
    /// Generic dictionary pool.
    /// </summary>
    public static class DictionaryPool<TKey, TValue>
    {
        private static readonly HighPerformancePool<Dictionary<TKey, TValue>> _pool =
            new HighPerformancePool<Dictionary<TKey, TValue>>(
                maxSize: 30,
                factory: () => new Dictionary<TKey, TValue>(32),
                reset: dict => dict.Clear());

        public static Dictionary<TKey, TValue> Rent() => _pool.Rent();
        public static void Return(Dictionary<TKey, TValue> dict) => _pool.Return(dict);
    }

    #endregion

    #region Batch Processor

    /// <summary>
    /// Toplu işleme için batch processor.
    /// </summary>
    public class BatchProcessor<T>
    {
        private readonly List<T> _buffer;
        private readonly int _batchSize;
        private readonly Func<IReadOnlyList<T>, Task> _processBatch;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly System.Threading.Timer _flushTimer;
        private readonly TimeSpan _flushInterval;

        public BatchProcessor(
            int batchSize,
            Func<IReadOnlyList<T>, Task> processBatch,
            TimeSpan? flushInterval = null)
        {
            _batchSize = batchSize;
            _processBatch = processBatch;
            _buffer = new List<T>(batchSize);
            _flushInterval = flushInterval ?? TimeSpan.FromSeconds(5);

            if (_flushInterval > TimeSpan.Zero)
            {
                _flushTimer = new System.Threading.Timer(
                    async _ => await FlushAsync(),
                    null,
                    _flushInterval,
                    _flushInterval);
            }
        }

        public int BufferCount => _buffer.Count;

        public async Task AddAsync(T item)
        {
            await _semaphore.WaitAsync();
            try
            {
                _buffer.Add(item);

                if (_buffer.Count >= _batchSize)
                {
                    await ProcessBufferAsync();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Add(T item)
        {
            _semaphore.Wait();
            try
            {
                _buffer.Add(item);

                if (_buffer.Count >= _batchSize)
                {
                    ProcessBufferAsync().Wait();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task FlushAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_buffer.Count > 0)
                {
                    await ProcessBufferAsync();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task ProcessBufferAsync()
        {
            if (_buffer.Count == 0) return;

            var batch = new List<T>(_buffer);
            _buffer.Clear();

            try
            {
                await _processBatch(batch);
            }
            catch (Exception ex)
            {
                Logger.Error($"Batch processing failed: {ex.Message}", ex);
                throw;
            }
        }

        public void Dispose()
        {
            _flushTimer?.Dispose();
            FlushAsync().Wait();
        }
    }

    #endregion

    #region Parallel Helper

    /// <summary>
    /// Paralel işleme yardımcıları.
    /// </summary>
    public static class ParallelHelper
    {
        /// <summary>
        /// Throttled parallel foreach - aynı anda çalışan task sayısını sınırlar.
        /// </summary>
        public static async Task ForEachAsync<T>(
            IEnumerable<T> source,
            int maxDegreeOfParallelism,
            Func<T, Task> action,
            CancellationToken cancellationToken = default)
        {
            using var throttler = new SemaphoreSlim(maxDegreeOfParallelism);
            var tasks = new List<Task>();

            foreach (var item in source)
            {
                await throttler.WaitAsync(cancellationToken);

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await action(item);
                    }
                    finally
                    {
                        throttler.Release();
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Sonuç döndüren throttled parallel select.
        /// </summary>
        public static async Task<IEnumerable<TResult>> SelectAsync<T, TResult>(
            IEnumerable<T> source,
            int maxDegreeOfParallelism,
            Func<T, Task<TResult>> selector,
            CancellationToken cancellationToken = default)
        {
            using var throttler = new SemaphoreSlim(maxDegreeOfParallelism);
            var tasks = new List<Task<TResult>>();

            foreach (var item in source)
            {
                await throttler.WaitAsync(cancellationToken);

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        return await selector(item);
                    }
                    finally
                    {
                        throttler.Release();
                    }
                }, cancellationToken));
            }

            return await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Chunk'lara bölerek paralel işleme.
        /// </summary>
        public static async Task ProcessInChunksAsync<T>(
            IList<T> source,
            int chunkSize,
            Func<IList<T>, Task> processor,
            CancellationToken cancellationToken = default)
        {
            for (int i = 0; i < source.Count; i += chunkSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var chunk = new List<T>();
                for (int j = i; j < Math.Min(i + chunkSize, source.Count); j++)
                {
                    chunk.Add(source[j]);
                }

                await processor(chunk);
            }
        }
    }

    #endregion

    #region Performance Monitor

    /// <summary>
    /// Performans izleme servisi.
    /// </summary>
    public class PerformanceMonitor : IDisposable
    {
        private readonly System.Threading.Timer _monitorTimer;
        private readonly ConcurrentDictionary<string, PerformanceSnapshot> _snapshots;
        private long _gcGen0Count;
        private long _gcGen1Count;
        private long _gcGen2Count;

        public PerformanceMonitor(int intervalSeconds = 30)
        {
            _snapshots = new ConcurrentDictionary<string, PerformanceSnapshot>();

            _monitorTimer = new System.Threading.Timer(
                _ => TakeSnapshot(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(intervalSeconds));
        }

        public event EventHandler<PerformanceAlertEventArgs> OnAlert;

        private void TakeSnapshot()
        {
            var snapshot = new PerformanceSnapshot
            {
                Timestamp = DateTime.Now,
                WorkingSetMB = Environment.WorkingSet / (1024.0 * 1024.0),
                GCMemoryMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0),
                Gen0Collections = GC.CollectionCount(0) - _gcGen0Count,
                Gen1Collections = GC.CollectionCount(1) - _gcGen1Count,
                Gen2Collections = GC.CollectionCount(2) - _gcGen2Count,
                ThreadCount = Process.GetCurrentProcess().Threads.Count,
                HandleCount = Process.GetCurrentProcess().HandleCount
            };

            _gcGen0Count = GC.CollectionCount(0);
            _gcGen1Count = GC.CollectionCount(1);
            _gcGen2Count = GC.CollectionCount(2);

            var key = snapshot.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            _snapshots[key] = snapshot;

            // Eski snapshot'ları temizle (son 1 saat)
            var cutoff = DateTime.Now.AddHours(-1);
            foreach (var k in _snapshots.Keys.ToArray())
            {
                if (DateTime.TryParse(k, out var time) && time < cutoff)
                {
                    _snapshots.TryRemove(k, out _);
                }
            }

            // Metrikleri güncelle
            ApplicationMetrics.UpdateMemoryMetrics();

            // Uyarıları kontrol et
            CheckAlerts(snapshot);
        }

        private void CheckAlerts(PerformanceSnapshot snapshot)
        {
            // Yüksek bellek kullanımı
            if (snapshot.WorkingSetMB > 500)
            {
                OnAlert?.Invoke(this, new PerformanceAlertEventArgs
                {
                    AlertType = PerformanceAlertType.HighMemory,
                    Message = $"Yüksek bellek kullanımı: {snapshot.WorkingSetMB:F0}MB",
                    Snapshot = snapshot
                });
            }

            // Çok fazla Gen2 GC
            if (snapshot.Gen2Collections > 5)
            {
                OnAlert?.Invoke(this, new PerformanceAlertEventArgs
                {
                    AlertType = PerformanceAlertType.HighGCPressure,
                    Message = $"Yüksek GC baskısı: {snapshot.Gen2Collections} Gen2 collection",
                    Snapshot = snapshot
                });
            }
        }

        public IEnumerable<PerformanceSnapshot> GetSnapshots()
        {
            return _snapshots.Values.OrderByDescending(s => s.Timestamp);
        }

        public PerformanceSnapshot GetLatest()
        {
            return _snapshots.Values.OrderByDescending(s => s.Timestamp).FirstOrDefault();
        }

        /// <summary>
        /// Manuel GC tetikle (dikkatli kullanın).
        /// </summary>
        public void ForceGC()
        {
            Logger.Warning("Manuel GC tetikleniyor...");
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            Logger.Info("Manuel GC tamamlandı.");
        }

        public void Dispose()
        {
            _monitorTimer?.Dispose();
        }
    }

    public class PerformanceSnapshot
    {
        public DateTime Timestamp { get; set; }
        public double WorkingSetMB { get; set; }
        public double GCMemoryMB { get; set; }
        public long Gen0Collections { get; set; }
        public long Gen1Collections { get; set; }
        public long Gen2Collections { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }

        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss}] Memory: {WorkingSetMB:F0}MB, GC: {GCMemoryMB:F0}MB, " +
                   $"Gen0: {Gen0Collections}, Gen1: {Gen1Collections}, Gen2: {Gen2Collections}";
        }
    }

    public enum PerformanceAlertType
    {
        HighMemory,
        HighGCPressure,
        HighCPU,
        SlowOperation
    }

    public class PerformanceAlertEventArgs : EventArgs
    {
        public PerformanceAlertType AlertType { get; set; }
        public string Message { get; set; }
        public PerformanceSnapshot Snapshot { get; set; }
    }

    #endregion

    #region Stopwatch Extensions

    /// <summary>
    /// Stopwatch extension'ları.
    /// </summary>
    public static class StopwatchExtensions
    {
        public static IDisposable StartNew(out Stopwatch stopwatch)
        {
            stopwatch = Stopwatch.StartNew();
            return new StopwatchScope(stopwatch);
        }

        private class StopwatchScope : IDisposable
        {
            private readonly Stopwatch _stopwatch;

            public StopwatchScope(Stopwatch stopwatch)
            {
                _stopwatch = stopwatch;
            }

            public void Dispose()
            {
                _stopwatch.Stop();
            }
        }
    }

    #endregion

    #region Debouncer

    /// <summary>
    /// Debounce - art arda gelen çağrıları birleştirir.
    /// </summary>
    public class Debouncer : IDisposable
    {
        private readonly TimeSpan _delay;
        private CancellationTokenSource _cts;
        private readonly object _lock = new object();

        public Debouncer(int delayMs = 300)
        {
            _delay = TimeSpan.FromMilliseconds(delayMs);
        }

        public async Task DebounceAsync(Func<Task> action)
        {
            lock (_lock)
            {
                _cts?.Cancel();
                _cts = new CancellationTokenSource();
            }

            try
            {
                await Task.Delay(_delay, _cts.Token);
                await action();
            }
            catch (OperationCanceledException)
            {
                // Debounced - beklenen davranış
            }
        }

        public void Debounce(Action action)
        {
            lock (_lock)
            {
                _cts?.Cancel();
                _cts = new CancellationTokenSource();
            }

            var token = _cts.Token;

            Task.Delay(_delay, token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                {
                    action();
                }
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        public void Dispose()
        {
            _cts?.Dispose();
        }
    }

    #endregion

    #region Throttler

    /// <summary>
    /// Throttle - çağrıları belirli aralıklarla sınırlar.
    /// </summary>
    public class Throttler
    {
        private readonly TimeSpan _interval;
        private DateTime _lastExecution = DateTime.MinValue;
        private readonly object _lock = new object();

        public Throttler(int intervalMs = 100)
        {
            _interval = TimeSpan.FromMilliseconds(intervalMs);
        }

        public bool TryExecute(Action action)
        {
            lock (_lock)
            {
                var now = DateTime.Now;
                if (now - _lastExecution >= _interval)
                {
                    _lastExecution = now;
                    action();
                    return true;
                }
                return false;
            }
        }

        public async Task<bool> TryExecuteAsync(Func<Task> action)
        {
            lock (_lock)
            {
                var now = DateTime.Now;
                if (now - _lastExecution < _interval)
                {
                    return false;
                }
                _lastExecution = now;
            }

            await action();
            return true;
        }
    }

    #endregion

    #region Lazy Cache Dictionary

    /// <summary>
    /// Lazy loading cache dictionary.
    /// </summary>
    public class LazyCacheDictionary<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, Lazy<TValue>> _cache;
        private readonly Func<TKey, TValue> _factory;

        public LazyCacheDictionary(Func<TKey, TValue> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _cache = new ConcurrentDictionary<TKey, Lazy<TValue>>();
        }

        public TValue this[TKey key] => GetOrCreate(key);

        public TValue GetOrCreate(TKey key)
        {
            var lazy = _cache.GetOrAdd(key, k => new Lazy<TValue>(() => _factory(k)));
            return lazy.Value;
        }

        public bool TryGet(TKey key, out TValue value)
        {
            if (_cache.TryGetValue(key, out var lazy) && lazy.IsValueCreated)
            {
                value = lazy.Value;
                return true;
            }

            value = default;
            return false;
        }

        public void Remove(TKey key)
        {
            _cache.TryRemove(key, out _);
        }

        public void Clear()
        {
            _cache.Clear();
        }

        public int Count => _cache.Count;
    }

    #endregion
}
