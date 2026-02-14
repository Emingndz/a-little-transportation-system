using System;
using System.Threading;
using System.Threading.Tasks;
using Prolab_4.Core.Logging;

namespace Prolab_4.Core.Resilience
{
    /// <summary>
    /// Circuit Breaker Pattern implementasyonu.
    /// Ardışık hatalardan sonra servisi korur ve kademeli iyileşme sağlar.
    /// </summary>
    public class CircuitBreaker
    {
        #region State Enum
        
        public enum CircuitState
        {
            /// <summary>Normal çalışma.</summary>
            Closed,
            /// <summary>Devre açık, tüm istekler reddediliyor.</summary>
            Open,
            /// <summary>Test aşaması, sınırlı istek kabul ediliyor.</summary>
            HalfOpen
        }
        
        #endregion

        #region Fields
        
        private readonly string _name;
        private readonly int _failureThreshold;
        private readonly TimeSpan _openDuration;
        private readonly int _halfOpenMaxAttempts;
        private readonly object _lock = new object();
        
        private CircuitState _state = CircuitState.Closed;
        private int _failureCount;
        private int _halfOpenAttempts;
        private DateTime _lastFailureTime;
        private DateTime _openedAt;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Mevcut durum.
        /// </summary>
        public CircuitState State
        {
            get
            {
                lock (_lock)
                {
                    if (_state == CircuitState.Open && DateTime.UtcNow - _openedAt >= _openDuration)
                    {
                        TransitionTo(CircuitState.HalfOpen);
                    }
                    return _state;
                }
            }
        }
        
        /// <summary>
        /// Başarısızlık sayısı.
        /// </summary>
        public int FailureCount => _failureCount;
        
        /// <summary>
        /// Açık kalma süresi.
        /// </summary>
        public TimeSpan? TimeUntilReset
        {
            get
            {
                if (_state != CircuitState.Open) return null;
                var remaining = _openDuration - (DateTime.UtcNow - _openedAt);
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }
        
        #endregion

        #region Events
        
        /// <summary>
        /// Durum değiştiğinde tetiklenir.
        /// </summary>
        public event EventHandler<CircuitStateChangedEventArgs> StateChanged;
        
        #endregion

        #region Constructor
        
        /// <summary>
        /// CircuitBreaker oluşturur.
        /// </summary>
        /// <param name="name">Devre adı (loglama için)</param>
        /// <param name="failureThreshold">Açılma için gereken hata sayısı</param>
        /// <param name="openDurationSeconds">Açık kalma süresi (saniye)</param>
        /// <param name="halfOpenMaxAttempts">Half-open'da max deneme</param>
        public CircuitBreaker(
            string name,
            int failureThreshold = 5,
            int openDurationSeconds = 30,
            int halfOpenMaxAttempts = 3)
        {
            _name = name;
            _failureThreshold = failureThreshold;
            _openDuration = TimeSpan.FromSeconds(openDurationSeconds);
            _halfOpenMaxAttempts = halfOpenMaxAttempts;
        }
        
        #endregion

        #region Execute Methods
        
        /// <summary>
        /// Operasyonu circuit breaker koruması altında çalıştırır.
        /// </summary>
        public async Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            EnsureCircuitAllowsExecution();
            
            try
            {
                var result = await operation(cancellationToken);
                OnSuccess();
                return result;
            }
            catch (Exception ex) when (!IsCriticalException(ex))
            {
                OnFailure(ex);
                throw;
            }
        }
        
        /// <summary>
        /// Void operasyonu circuit breaker koruması altında çalıştırır.
        /// </summary>
        public async Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            await ExecuteAsync(async ct =>
            {
                await operation(ct);
                return true;
            }, cancellationToken);
        }
        
        /// <summary>
        /// Senkron operasyonu circuit breaker koruması altında çalıştırır.
        /// </summary>
        public T Execute<T>(Func<T> operation)
        {
            EnsureCircuitAllowsExecution();
            
            try
            {
                var result = operation();
                OnSuccess();
                return result;
            }
            catch (Exception ex) when (!IsCriticalException(ex))
            {
                OnFailure(ex);
                throw;
            }
        }
        
        #endregion

        #region State Management
        
        private void EnsureCircuitAllowsExecution()
        {
            var currentState = State; // Property içinde state güncellemesi yapılır
            
            lock (_lock)
            {
                switch (_state)
                {
                    case CircuitState.Open:
                        throw new CircuitBreakerOpenException(_name, TimeUntilReset ?? TimeSpan.Zero);
                    
                    case CircuitState.HalfOpen:
                        if (_halfOpenAttempts >= _halfOpenMaxAttempts)
                        {
                            throw new CircuitBreakerOpenException(_name, TimeSpan.FromSeconds(5));
                        }
                        _halfOpenAttempts++;
                        break;
                }
            }
        }
        
        private void OnSuccess()
        {
            lock (_lock)
            {
                if (_state == CircuitState.HalfOpen)
                {
                    TransitionTo(CircuitState.Closed);
                }
                _failureCount = 0;
                _halfOpenAttempts = 0;
            }
        }
        
        private void OnFailure(Exception ex)
        {
            lock (_lock)
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;
                
                Logger.Warning($"[CircuitBreaker:{_name}] Failure #{_failureCount}: {ex.Message}");
                
                if (_state == CircuitState.HalfOpen)
                {
                    // Half-open'da hata olursa tekrar aç
                    TransitionTo(CircuitState.Open);
                }
                else if (_failureCount >= _failureThreshold)
                {
                    TransitionTo(CircuitState.Open);
                }
            }
        }
        
        private void TransitionTo(CircuitState newState)
        {
            if (_state == newState) return;
            
            var oldState = _state;
            _state = newState;
            
            if (newState == CircuitState.Open)
            {
                _openedAt = DateTime.UtcNow;
                Logger.Warning($"[CircuitBreaker:{_name}] OPENED - Too many failures ({_failureCount})");
            }
            else if (newState == CircuitState.Closed)
            {
                _failureCount = 0;
                _halfOpenAttempts = 0;
                Logger.Info($"[CircuitBreaker:{_name}] CLOSED - Service recovered");
            }
            else if (newState == CircuitState.HalfOpen)
            {
                _halfOpenAttempts = 0;
                Logger.Info($"[CircuitBreaker:{_name}] HALF-OPEN - Testing recovery");
            }
            
            StateChanged?.Invoke(this, new CircuitStateChangedEventArgs(oldState, newState));
        }
        
        private static bool IsCriticalException(Exception ex)
        {
            return ex is OutOfMemoryException ||
                   ex is StackOverflowException ||
                   ex is ThreadAbortException;
        }
        
        #endregion

        #region Manual Control
        
        /// <summary>
        /// Devreyi manuel olarak sıfırlar.
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _failureCount = 0;
                _halfOpenAttempts = 0;
                TransitionTo(CircuitState.Closed);
            }
        }
        
        /// <summary>
        /// Devreyi manuel olarak açar.
        /// </summary>
        public void Trip()
        {
            lock (_lock)
            {
                TransitionTo(CircuitState.Open);
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Circuit breaker state change event args.
    /// </summary>
    public class CircuitStateChangedEventArgs : EventArgs
    {
        public CircuitBreaker.CircuitState OldState { get; }
        public CircuitBreaker.CircuitState NewState { get; }
        
        public CircuitStateChangedEventArgs(
            CircuitBreaker.CircuitState oldState,
            CircuitBreaker.CircuitState newState)
        {
            OldState = oldState;
            NewState = newState;
        }
    }
    
    /// <summary>
    /// Circuit breaker açık exception.
    /// </summary>
    public class CircuitBreakerOpenException : Exception
    {
        public string CircuitName { get; }
        public TimeSpan RetryAfter { get; }
        
        public CircuitBreakerOpenException(string circuitName, TimeSpan retryAfter)
            : base($"Circuit breaker '{circuitName}' açık. {retryAfter.TotalSeconds:F0} saniye sonra tekrar deneyin.")
        {
            CircuitName = circuitName;
            RetryAfter = retryAfter;
        }
    }
}
