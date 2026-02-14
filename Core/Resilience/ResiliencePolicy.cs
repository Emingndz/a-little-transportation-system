using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Prolab_4.Core.Logging;

namespace Prolab_4.Core.Resilience
{
    /// <summary>
    /// Policy Wrapper - Circuit Breaker, Retry, Bulkhead'i birleştirir.
    /// </summary>
    public class ResiliencePolicy
    {
        #region Fields
        
        private readonly string _name;
        private CircuitBreaker _circuitBreaker;
        private RetryPolicy _retryPolicy;
        private Bulkhead _bulkhead;
        private TimeSpan? _timeout;
        private Func<Exception, Task> _onError;
        private Func<Task> _onSuccess;
        private Func<TimeSpan, Exception, Task<bool>> _onRetry;
        
        #endregion

        #region Constructor
        
        /// <summary>
        /// ResiliencePolicy oluşturur.
        /// </summary>
        public ResiliencePolicy(string name = "DefaultPolicy")
        {
            _name = name;
        }
        
        #endregion

        #region Builder Methods
        
        /// <summary>
        /// Circuit breaker ekler.
        /// </summary>
        public ResiliencePolicy WithCircuitBreaker(
            int failureThreshold = 5,
            int openDurationSeconds = 30,
            int halfOpenMaxAttempts = 3)
        {
            _circuitBreaker = new CircuitBreaker(_name, failureThreshold, openDurationSeconds, halfOpenMaxAttempts);
            return this;
        }
        
        /// <summary>
        /// Mevcut circuit breaker kullanır.
        /// </summary>
        public ResiliencePolicy WithCircuitBreaker(CircuitBreaker circuitBreaker)
        {
            _circuitBreaker = circuitBreaker;
            return this;
        }
        
        /// <summary>
        /// Retry policy ekler.
        /// </summary>
        public ResiliencePolicy WithRetry(
            int maxRetries = 3,
            int initialDelayMs = 1000,
            double backoffMultiplier = 2.0)
        {
            _retryPolicy = new RetryPolicy(
                maxRetries,
                initialDelayMs,
                30000,
                backoffMultiplier,
                true);
            return this;
        }
        
        /// <summary>
        /// Mevcut retry policy kullanır.
        /// </summary>
        public ResiliencePolicy WithRetry(RetryPolicy retryPolicy)
        {
            _retryPolicy = retryPolicy;
            return this;
        }
        
        /// <summary>
        /// Bulkhead ekler.
        /// </summary>
        public ResiliencePolicy WithBulkhead(int maxConcurrency, int maxQueueLength = 0)
        {
            _bulkhead = new Bulkhead(_name, maxConcurrency, maxQueueLength);
            return this;
        }
        
        /// <summary>
        /// Mevcut bulkhead kullanır.
        /// </summary>
        public ResiliencePolicy WithBulkhead(Bulkhead bulkhead)
        {
            _bulkhead = bulkhead;
            return this;
        }
        
        /// <summary>
        /// Timeout ekler.
        /// </summary>
        public ResiliencePolicy WithTimeout(int timeoutSeconds)
        {
            _timeout = TimeSpan.FromSeconds(timeoutSeconds);
            return this;
        }
        
        /// <summary>
        /// Timeout ekler.
        /// </summary>
        public ResiliencePolicy WithTimeout(TimeSpan timeout)
        {
            _timeout = timeout;
            return this;
        }
        
        /// <summary>
        /// Hata callback'i ekler.
        /// </summary>
        public ResiliencePolicy OnError(Func<Exception, Task> handler)
        {
            _onError = handler;
            return this;
        }
        
        /// <summary>
        /// Başarı callback'i ekler.
        /// </summary>
        public ResiliencePolicy OnSuccess(Func<Task> handler)
        {
            _onSuccess = handler;
            return this;
        }
        
        /// <summary>
        /// Retry callback'i ekler.
        /// </summary>
        public ResiliencePolicy OnRetry(Func<TimeSpan, Exception, Task<bool>> handler)
        {
            _onRetry = handler;
            return this;
        }
        
        #endregion

        #region Execute Methods
        
        /// <summary>
        /// Operasyonu tüm policy'lerle çalıştırır.
        /// </summary>
        public async Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            Func<CancellationToken, Task<T>> wrappedOperation = operation;
            
            // Timeout wrapper
            if (_timeout.HasValue)
            {
                var originalOp = wrappedOperation;
                wrappedOperation = async (ct) =>
                {
                    using var timeoutCts = new CancellationTokenSource(_timeout.Value);
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
                    
                    try
                    {
                        return await originalOp(linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        throw new TimeoutException($"Operation timed out after {_timeout.Value.TotalSeconds}s");
                    }
                };
            }
            
            // Bulkhead wrapper
            if (_bulkhead != null)
            {
                var originalOp = wrappedOperation;
                wrappedOperation = async (ct) => await _bulkhead.ExecuteAsync(async (token) => await originalOp(token), ct);
            }
            
            // Circuit breaker wrapper
            if (_circuitBreaker != null)
            {
                var originalOp = wrappedOperation;
                wrappedOperation = async (ct) => await _circuitBreaker.ExecuteAsync(async (token) => await originalOp(token), ct);
            }
            
            try
            {
                T result;
                
                // Retry wrapper
                if (_retryPolicy != null)
                {
                    result = await _retryPolicy.ExecuteAsync(
                        async (attempt, ct) =>
                        {
                            Logger.Debug($"[{_name}] Attempt {attempt + 1}");
                            return await wrappedOperation(ct);
                        },
                        cancellationToken,
                        (retryAttempt) =>
                        {
                            _onRetry?.Invoke(retryAttempt.Delay, retryAttempt.LastException);
                        });
                }
                else
                {
                    result = await wrappedOperation(cancellationToken);
                }
                
                if (_onSuccess != null)
                {
                    await _onSuccess();
                }
                
                return result;
            }
            catch (Exception ex)
            {
                if (_onError != null)
                {
                    await _onError(ex);
                }
                throw;
            }
        }
        
        /// <summary>
        /// Operasyonu çalıştırır (basit overload).
        /// </summary>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            return await ExecuteAsync(_ => operation());
        }
        
        /// <summary>
        /// Void operasyonu çalıştırır.
        /// </summary>
        public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            await ExecuteAsync(async (ct) =>
            {
                await operation();
                return true;
            }, cancellationToken);
        }
        
        #endregion

        #region Static Builders
        
        /// <summary>
        /// Standard web API policy.
        /// </summary>
        public static ResiliencePolicy ForWebApi(string name = "WebApi")
        {
            return new ResiliencePolicy(name)
                .WithCircuitBreaker(5, 30, 3)
                .WithRetry(3, 500, 2.0)
                .WithBulkhead(100, 50)
                .WithTimeout(30);
        }
        
        /// <summary>
        /// Database erişimi için policy.
        /// </summary>
        public static ResiliencePolicy ForDatabase(string name = "Database")
        {
            return new ResiliencePolicy(name)
                .WithCircuitBreaker(3, 60, 2)
                .WithRetry(2, 200, 1.5)
                .WithTimeout(10);
        }
        
        /// <summary>
        /// Dosya işlemleri için policy.
        /// </summary>
        public static ResiliencePolicy ForFileOperations(string name = "FileOps")
        {
            return new ResiliencePolicy(name)
                .WithRetry(5, 100, 1.2)
                .WithTimeout(60);
        }
        
        /// <summary>
        /// Kritik işlemler için policy (yüksek dayanıklılık).
        /// </summary>
        public static ResiliencePolicy ForCritical(string name = "Critical")
        {
            return new ResiliencePolicy(name)
                .WithCircuitBreaker(10, 120, 5)
                .WithRetry(10, 1000, 2.0)
                .WithBulkhead(20, 10)
                .WithTimeout(120);
        }
        
        #endregion
    }
    
    #region Policy Registry
    
    /// <summary>
    /// Global policy registry - isimle policy yönetimi.
    /// </summary>
    public static class PolicyRegistry
    {
        private static readonly ConcurrentDictionary<string, ResiliencePolicy> _policies 
            = new ConcurrentDictionary<string, ResiliencePolicy>();
        
        /// <summary>
        /// Policy kaydeder.
        /// </summary>
        public static void Register(string name, ResiliencePolicy policy)
        {
            _policies[name] = policy;
        }
        
        /// <summary>
        /// Policy alır.
        /// </summary>
        public static ResiliencePolicy Get(string name)
        {
            return _policies.TryGetValue(name, out var policy) ? policy : null;
        }
        
        /// <summary>
        /// Policy alır veya oluşturur.
        /// </summary>
        public static ResiliencePolicy GetOrCreate(string name, Func<ResiliencePolicy> factory)
        {
            return _policies.GetOrAdd(name, _ => factory());
        }
        
        /// <summary>
        /// Tüm kayıtlı policy isimlerini alır.
        /// </summary>
        public static IEnumerable<string> GetRegisteredNames()
        {
            return _policies.Keys.ToList();
        }
        
        /// <summary>
        /// Varsayılan policy'leri yükler.
        /// </summary>
        public static void LoadDefaults()
        {
            Register("WebApi", ResiliencePolicy.ForWebApi());
            Register("Database", ResiliencePolicy.ForDatabase());
            Register("FileOps", ResiliencePolicy.ForFileOperations());
            Register("Critical", ResiliencePolicy.ForCritical());
        }
    }
    
    #endregion

    #region Health Check
    
    /// <summary>
    /// Sağlık durumu.
    /// </summary>
    public enum HealthStatus
    {
        Healthy,
        Degraded,
        Unhealthy
    }
    
    /// <summary>
    /// Sağlık kontrolü sonucu.
    /// </summary>
    public class HealthCheckResult
    {
        public string Name { get; set; }
        public HealthStatus Status { get; set; }
        public string Description { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public Exception Exception { get; set; }
        
        public static HealthCheckResult Healthy(string name, string description = null)
        {
            return new HealthCheckResult
            {
                Name = name,
                Status = HealthStatus.Healthy,
                Description = description ?? "Healthy"
            };
        }
        
        public static HealthCheckResult Degraded(string name, string description)
        {
            return new HealthCheckResult
            {
                Name = name,
                Status = HealthStatus.Degraded,
                Description = description
            };
        }
        
        public static HealthCheckResult Unhealthy(string name, string description, Exception ex = null)
        {
            return new HealthCheckResult
            {
                Name = name,
                Status = HealthStatus.Unhealthy,
                Description = description,
                Exception = ex
            };
        }
    }
    
    /// <summary>
    /// Sağlık kontrolü interface.
    /// </summary>
    public interface IHealthCheck
    {
        string Name { get; }
        Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// Sağlık kontrolü servisi.
    /// </summary>
    public class HealthCheckService
    {
        private readonly List<IHealthCheck> _checks = new List<IHealthCheck>();
        
        /// <summary>
        /// Sağlık kontrolü ekler.
        /// </summary>
        public void AddCheck(IHealthCheck check)
        {
            _checks.Add(check);
        }
        
        /// <summary>
        /// Lambda-based sağlık kontrolü ekler.
        /// </summary>
        public void AddCheck(string name, Func<CancellationToken, Task<HealthCheckResult>> check)
        {
            _checks.Add(new LambdaHealthCheck(name, check));
        }
        
        /// <summary>
        /// Tüm sağlık kontrollerini çalıştırır.
        /// </summary>
        public async Task<OverallHealthResult> CheckAllAsync(CancellationToken cancellationToken = default)
        {
            var results = new List<HealthCheckResult>();
            
            foreach (var check in _checks)
            {
                try
                {
                    var result = await check.CheckAsync(cancellationToken);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    results.Add(HealthCheckResult.Unhealthy(check.Name, ex.Message, ex));
                }
            }
            
            var overallStatus = HealthStatus.Healthy;
            
            if (results.Any(r => r.Status == HealthStatus.Unhealthy))
                overallStatus = HealthStatus.Unhealthy;
            else if (results.Any(r => r.Status == HealthStatus.Degraded))
                overallStatus = HealthStatus.Degraded;
            
            return new OverallHealthResult
            {
                Status = overallStatus,
                Results = results,
                CheckedAt = DateTime.Now
            };
        }
        
        private class LambdaHealthCheck : IHealthCheck
        {
            public string Name { get; }
            private readonly Func<CancellationToken, Task<HealthCheckResult>> _check;
            
            public LambdaHealthCheck(string name, Func<CancellationToken, Task<HealthCheckResult>> check)
            {
                Name = name;
                _check = check;
            }
            
            public Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken)
            {
                return _check(cancellationToken);
            }
        }
    }
    
    /// <summary>
    /// Genel sağlık sonucu.
    /// </summary>
    public class OverallHealthResult
    {
        public HealthStatus Status { get; set; }
        public List<HealthCheckResult> Results { get; set; }
        public DateTime CheckedAt { get; set; }
        
        public override string ToString()
        {
            return $"Health: {Status} ({Results.Count} checks, {Results.Count(r => r.Status == HealthStatus.Healthy)} healthy)";
        }
    }
    
    #endregion

    #region Built-in Health Checks
    
    /// <summary>
    /// Dosya sistemi sağlık kontrolü.
    /// </summary>
    public class FileSystemHealthCheck : IHealthCheck
    {
        private readonly string _path;
        public string Name => "FileSystem";
        
        public FileSystemHealthCheck(string path = null)
        {
            _path = path ?? AppDomain.CurrentDomain.BaseDirectory;
        }
        
        public Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken)
        {
            try
            {
                var tempFile = Path.Combine(_path, $"health_check_{Guid.NewGuid()}.tmp");
                File.WriteAllText(tempFile, "test");
                File.Delete(tempFile);
                
                return Task.FromResult(HealthCheckResult.Healthy(Name, "File system is accessible"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(Name, $"File system error: {ex.Message}", ex));
            }
        }
    }
    
    /// <summary>
    /// Bellek sağlık kontrolü.
    /// </summary>
    public class MemoryHealthCheck : IHealthCheck
    {
        private readonly long _maxMemoryMB;
        public string Name => "Memory";
        
        public MemoryHealthCheck(long maxMemoryMB = 500)
        {
            _maxMemoryMB = maxMemoryMB;
        }
        
        public Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken)
        {
            var workingSet = Environment.WorkingSet / (1024 * 1024);
            var gcMemory = GC.GetTotalMemory(false) / (1024 * 1024);
            
            var result = new HealthCheckResult
            {
                Name = Name,
                Data = new Dictionary<string, object>
                {
                    ["WorkingSetMB"] = workingSet,
                    ["GCMemoryMB"] = gcMemory
                }
            };
            
            if (workingSet > _maxMemoryMB)
            {
                result.Status = HealthStatus.Unhealthy;
                result.Description = $"Memory usage ({workingSet}MB) exceeds threshold ({_maxMemoryMB}MB)";
            }
            else if (workingSet > _maxMemoryMB * 0.8)
            {
                result.Status = HealthStatus.Degraded;
                result.Description = $"Memory usage ({workingSet}MB) approaching threshold ({_maxMemoryMB}MB)";
            }
            else
            {
                result.Status = HealthStatus.Healthy;
                result.Description = $"Memory usage is normal ({workingSet}MB)";
            }
            
            return Task.FromResult(result);
        }
    }
    
    /// <summary>
    /// Circuit breaker sağlık kontrolü.
    /// </summary>
    public class CircuitBreakerHealthCheck : IHealthCheck
    {
        private readonly CircuitBreaker _circuitBreaker;
        private readonly string _name;
        public string Name => _name;
        
        public CircuitBreakerHealthCheck(CircuitBreaker circuitBreaker, string name = "CircuitBreaker")
        {
            _circuitBreaker = circuitBreaker;
            _name = $"CircuitBreaker_{name}";
        }
        
        public Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken)
        {
            var result = new HealthCheckResult
            {
                Name = Name,
                Data = new Dictionary<string, object>
                {
                    ["State"] = _circuitBreaker.State.ToString(),
                    ["FailureCount"] = _circuitBreaker.FailureCount
                }
            };
            
            switch (_circuitBreaker.State)
            {
                case CircuitBreaker.CircuitState.Closed:
                    result.Status = HealthStatus.Healthy;
                    result.Description = "Circuit breaker is closed (normal)";
                    break;
                case CircuitBreaker.CircuitState.HalfOpen:
                    result.Status = HealthStatus.Degraded;
                    result.Description = "Circuit breaker is half-open (testing)";
                    break;
                case CircuitBreaker.CircuitState.Open:
                    result.Status = HealthStatus.Unhealthy;
                    result.Description = "Circuit breaker is open (blocking requests)";
                    break;
            }
            
            return Task.FromResult(result);
        }
    }
    
    #endregion
}
