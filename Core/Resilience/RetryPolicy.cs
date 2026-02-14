using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Prolab_4.Core.Logging;

namespace Prolab_4.Core.Resilience
{
    /// <summary>
    /// Retry Policy - Configurable retry strategies with exponential backoff.
    /// </summary>
    public class RetryPolicy
    {
        #region Fields
        
        private readonly int _maxRetries;
        private readonly TimeSpan _initialDelay;
        private readonly TimeSpan _maxDelay;
        private readonly double _backoffMultiplier;
        private readonly bool _useJitter;
        private readonly HashSet<Type> _retryableExceptions;
        private readonly Func<Exception, bool> _shouldRetry;
        
        private static readonly Random _jitterRandom = new Random();
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Retry yapılmayacak policy.
        /// </summary>
        public static RetryPolicy None => new RetryPolicy(0);
        
        /// <summary>
        /// Varsayılan policy (3 retry, exponential backoff).
        /// </summary>
        public static RetryPolicy Default => new RetryPolicy(3, 1000, 30000, 2.0, true);
        
        /// <summary>
        /// Agresif retry (5 retry, hızlı).
        /// </summary>
        public static RetryPolicy Aggressive => new RetryPolicy(5, 100, 5000, 1.5, true);
        
        /// <summary>
        /// Yavaş retry (10 retry, uzun bekleme).
        /// </summary>
        public static RetryPolicy Patient => new RetryPolicy(10, 2000, 60000, 2.0, true);
        
        #endregion

        #region Constructor
        
        /// <summary>
        /// RetryPolicy oluşturur.
        /// </summary>
        /// <param name="maxRetries">Maksimum retry sayısı</param>
        /// <param name="initialDelayMs">İlk bekleme süresi (ms)</param>
        /// <param name="maxDelayMs">Maksimum bekleme süresi (ms)</param>
        /// <param name="backoffMultiplier">Exponential backoff çarpanı</param>
        /// <param name="useJitter">Rastgele jitter ekle</param>
        /// <param name="retryableExceptions">Retry yapılacak exception tipleri</param>
        /// <param name="shouldRetry">Custom retry condition</param>
        public RetryPolicy(
            int maxRetries = 3,
            int initialDelayMs = 1000,
            int maxDelayMs = 30000,
            double backoffMultiplier = 2.0,
            bool useJitter = true,
            IEnumerable<Type> retryableExceptions = null,
            Func<Exception, bool> shouldRetry = null)
        {
            _maxRetries = maxRetries;
            _initialDelay = TimeSpan.FromMilliseconds(initialDelayMs);
            _maxDelay = TimeSpan.FromMilliseconds(maxDelayMs);
            _backoffMultiplier = backoffMultiplier;
            _useJitter = useJitter;
            _retryableExceptions = retryableExceptions != null 
                ? new HashSet<Type>(retryableExceptions) 
                : new HashSet<Type>();
            _shouldRetry = shouldRetry ?? DefaultShouldRetry;
        }
        
        #endregion

        #region Execute Methods
        
        /// <summary>
        /// Operasyonu retry policy ile çalıştırır.
        /// </summary>
        public async Task<T> ExecuteAsync<T>(
            Func<int, CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default,
            Action<RetryAttempt> onRetry = null)
        {
            Exception lastException = null;
            
            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return await operation(attempt, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    if (attempt >= _maxRetries || !ShouldRetryException(ex))
                    {
                        break;
                    }
                    
                    var delay = CalculateDelay(attempt);
                    var retryAttempt = new RetryAttempt(attempt + 1, _maxRetries, delay, ex);
                    
                    Logger.Warning($"[Retry] Attempt {attempt + 1}/{_maxRetries} failed: {ex.Message}. " +
                                   $"Retrying in {delay.TotalMilliseconds:F0}ms...");
                    
                    onRetry?.Invoke(retryAttempt);
                    
                    await Task.Delay(delay, cancellationToken);
                }
            }
            
            throw new RetryExhaustedException(_maxRetries, lastException);
        }
        
        /// <summary>
        /// Operasyonu retry policy ile çalıştırır (basit overload).
        /// </summary>
        public async Task<T> ExecuteAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(
                (_, ct) => operation(),
                cancellationToken);
        }
        
        /// <summary>
        /// Void operasyonu retry policy ile çalıştırır.
        /// </summary>
        public async Task ExecuteAsync(
            Func<Task> operation,
            CancellationToken cancellationToken = default)
        {
            await ExecuteAsync(async (_, ct) =>
            {
                await operation();
                return true;
            }, cancellationToken);
        }
        
        /// <summary>
        /// Senkron operasyonu retry policy ile çalıştırır.
        /// </summary>
        public T Execute<T>(Func<int, T> operation, Action<RetryAttempt> onRetry = null)
        {
            Exception lastException = null;
            
            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    return operation(attempt);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    if (attempt >= _maxRetries || !ShouldRetryException(ex))
                    {
                        break;
                    }
                    
                    var delay = CalculateDelay(attempt);
                    var retryAttempt = new RetryAttempt(attempt + 1, _maxRetries, delay, ex);
                    
                    Logger.Warning($"[Retry] Attempt {attempt + 1}/{_maxRetries} failed. " +
                                   $"Retrying in {delay.TotalMilliseconds:F0}ms...");
                    
                    onRetry?.Invoke(retryAttempt);
                    
                    Thread.Sleep(delay);
                }
            }
            
            throw new RetryExhaustedException(_maxRetries, lastException);
        }
        
        #endregion

        #region Helper Methods
        
        private TimeSpan CalculateDelay(int attempt)
        {
            // Exponential backoff
            var delay = TimeSpan.FromMilliseconds(
                _initialDelay.TotalMilliseconds * Math.Pow(_backoffMultiplier, attempt));
            
            // Cap at max delay
            if (delay > _maxDelay)
            {
                delay = _maxDelay;
            }
            
            // Add jitter (±25%)
            if (_useJitter)
            {
                var jitterFactor = 0.75 + (_jitterRandom.NextDouble() * 0.5);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * jitterFactor);
            }
            
            return delay;
        }
        
        private bool ShouldRetryException(Exception ex)
        {
            // Custom predicate
            if (!_shouldRetry(ex))
            {
                return false;
            }
            
            // Specific exception types
            if (_retryableExceptions.Count > 0)
            {
                return _retryableExceptions.Contains(ex.GetType()) ||
                       (ex.InnerException != null && _retryableExceptions.Contains(ex.InnerException.GetType()));
            }
            
            return true;
        }
        
        private static bool DefaultShouldRetry(Exception ex)
        {
            // İptal edilmiş işlemler retry yapılmaz
            if (ex is OperationCanceledException)
                return false;
            
            // Bellek hataları retry yapılmaz
            if (ex is OutOfMemoryException)
                return false;
            
            // Argument hataları retry yapılmaz
            if (ex is ArgumentException)
                return false;
            
            // Application exception'lar IsRetryable'a bakılır
            if (ex is Exceptions.ApplicationException appEx)
                return appEx.IsRetryable;
            
            // Diğer tüm hatalar retry yapılabilir
            return true;
        }
        
        #endregion

        #region Builder Pattern
        
        /// <summary>
        /// RetryPolicy builder'ı başlatır.
        /// </summary>
        public static RetryPolicyBuilder Builder() => new RetryPolicyBuilder();
        
        #endregion
    }
    
    /// <summary>
    /// Retry girişimi bilgisi.
    /// </summary>
    public class RetryAttempt
    {
        public int AttemptNumber { get; }
        public int MaxAttempts { get; }
        public TimeSpan Delay { get; }
        public Exception LastException { get; }
        public bool IsLastAttempt => AttemptNumber >= MaxAttempts;
        
        public RetryAttempt(int attemptNumber, int maxAttempts, TimeSpan delay, Exception lastException)
        {
            AttemptNumber = attemptNumber;
            MaxAttempts = maxAttempts;
            Delay = delay;
            LastException = lastException;
        }
    }
    
    /// <summary>
    /// Retry policy builder.
    /// </summary>
    public class RetryPolicyBuilder
    {
        private int _maxRetries = 3;
        private int _initialDelayMs = 1000;
        private int _maxDelayMs = 30000;
        private double _backoffMultiplier = 2.0;
        private bool _useJitter = true;
        private readonly List<Type> _retryableExceptions = new List<Type>();
        private Func<Exception, bool> _shouldRetry;
        
        public RetryPolicyBuilder MaxRetries(int count)
        {
            _maxRetries = count;
            return this;
        }
        
        public RetryPolicyBuilder InitialDelay(int milliseconds)
        {
            _initialDelayMs = milliseconds;
            return this;
        }
        
        public RetryPolicyBuilder MaxDelay(int milliseconds)
        {
            _maxDelayMs = milliseconds;
            return this;
        }
        
        public RetryPolicyBuilder BackoffMultiplier(double multiplier)
        {
            _backoffMultiplier = multiplier;
            return this;
        }
        
        public RetryPolicyBuilder WithJitter(bool useJitter = true)
        {
            _useJitter = useJitter;
            return this;
        }
        
        public RetryPolicyBuilder RetryOn<TException>() where TException : Exception
        {
            _retryableExceptions.Add(typeof(TException));
            return this;
        }
        
        public RetryPolicyBuilder RetryWhen(Func<Exception, bool> predicate)
        {
            _shouldRetry = predicate;
            return this;
        }
        
        public RetryPolicy Build()
        {
            return new RetryPolicy(
                _maxRetries,
                _initialDelayMs,
                _maxDelayMs,
                _backoffMultiplier,
                _useJitter,
                _retryableExceptions,
                _shouldRetry);
        }
    }
    
    /// <summary>
    /// Retry'lar tükendi exception.
    /// </summary>
    public class RetryExhaustedException : Exception
    {
        public int AttemptCount { get; }
        
        public RetryExhaustedException(int attemptCount, Exception lastException)
            : base($"İşlem {attemptCount} deneme sonrasında başarısız oldu.", lastException)
        {
            AttemptCount = attemptCount;
        }
    }
}
