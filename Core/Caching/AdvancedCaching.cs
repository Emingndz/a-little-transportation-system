using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Prolab_4.Core.Logging;
using Prolab_4.Core.Metrics;

namespace Prolab_4.Core.Caching
{
    #region Cache Policy

    /// <summary>
    /// Cache politikası.
    /// </summary>
    public class CachePolicy
    {
        /// <summary>
        /// Expire süresi.
        /// </summary>
        public TimeSpan? AbsoluteExpiration { get; set; }

        /// <summary>
        /// Sliding expiration - son erişimden itibaren.
        /// </summary>
        public TimeSpan? SlidingExpiration { get; set; }

        /// <summary>
        /// Öncelik (düşük öncelikli öğeler bellek baskısında ilk silinir).
        /// </summary>
        public CachePriority Priority { get; set; } = CachePriority.Normal;

        /// <summary>
        /// Size (bellek yönetimi için).
        /// </summary>
        public long? Size { get; set; }

        /// <summary>
        /// Bağımlı cache key'leri (bunlar expire olursa bu da olur).
        /// </summary>
        public string[] Dependencies { get; set; }

        public static CachePolicy Default => new CachePolicy
        {
            AbsoluteExpiration = TimeSpan.FromMinutes(30),
            Priority = CachePriority.Normal
        };

        public static CachePolicy ShortLived => new CachePolicy
        {
            AbsoluteExpiration = TimeSpan.FromMinutes(5),
            Priority = CachePriority.Low
        };

        public static CachePolicy LongLived => new CachePolicy
        {
            AbsoluteExpiration = TimeSpan.FromHours(4),
            Priority = CachePriority.High
        };

        public static CachePolicy Permanent => new CachePolicy
        {
            AbsoluteExpiration = null,
            Priority = CachePriority.NeverRemove
        };

        public static CachePolicy Sliding(int minutes) => new CachePolicy
        {
            SlidingExpiration = TimeSpan.FromMinutes(minutes),
            Priority = CachePriority.Normal
        };
    }

    public enum CachePriority
    {
        Low = 0,
        BelowNormal = 1,
        Normal = 2,
        AboveNormal = 3,
        High = 4,
        NeverRemove = 5
    }

    #endregion

    #region Advanced Cache Entry

    /// <summary>
    /// Gelişmiş cache girdisi.
    /// </summary>
    internal class AdvancedCacheEntry
    {
        public string Key { get; set; }
        public object Value { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? AbsoluteExpiration { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public DateTime LastAccessed { get; set; }
        public int AccessCount { get; set; }
        public CachePriority Priority { get; set; }
        public long? Size { get; set; }
        public string[] Dependencies { get; set; }

        public bool IsExpired
        {
            get
            {
                if (AbsoluteExpiration.HasValue && DateTime.Now > AbsoluteExpiration.Value)
                    return true;

                if (SlidingExpiration.HasValue &&
                    DateTime.Now - LastAccessed > SlidingExpiration.Value)
                    return true;

                return false;
            }
        }

        public void Touch()
        {
            LastAccessed = DateTime.Now;
            AccessCount++;
        }
    }

    #endregion

    #region Multi-Layer Cache

    /// <summary>
    /// Multi-layer cache - L1 (local) + L2 (shared) cache.
    /// </summary>
    public class MultiLayerCache : ICacheService
    {
        private readonly ICacheService _l1Cache;
        private readonly ICacheService _l2Cache;
        private readonly string _instanceId;

        public MultiLayerCache(ICacheService l1Cache, ICacheService l2Cache = null)
        {
            _l1Cache = l1Cache ?? throw new ArgumentNullException(nameof(l1Cache));
            _l2Cache = l2Cache;
            _instanceId = Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            // L1'de ara
            if (_l1Cache.TryGet<T>(key, out var l1Value))
            {
                ApplicationMetrics.CacheHit.Increment();
                return l1Value;
            }

            // L2'de ara (varsa)
            if (_l2Cache != null && _l2Cache.TryGet<T>(key, out var l2Value))
            {
                // L1'e de koy (cache warming)
                _l1Cache.Set(key, l2Value, expiration);
                ApplicationMetrics.CacheHit.Increment();
                return l2Value;
            }

            ApplicationMetrics.CacheMiss.Increment();

            // Factory'den al
            var value = await factory();

            // Her iki cache'e de koy
            _l1Cache.Set(key, value, expiration);
            _l2Cache?.Set(key, value, expiration);

            return value;
        }

        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            _l1Cache.Set(key, value, expiration);
            _l2Cache?.Set(key, value, expiration);
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (_l1Cache.TryGet(key, out value))
            {
                ApplicationMetrics.CacheHit.Increment();
                return true;
            }

            if (_l2Cache != null && _l2Cache.TryGet(key, out value))
            {
                // L1'e de koy
                _l1Cache.Set(key, value);
                ApplicationMetrics.CacheHit.Increment();
                return true;
            }

            ApplicationMetrics.CacheMiss.Increment();
            return false;
        }

        public void Remove(string key)
        {
            _l1Cache.Remove(key);
            _l2Cache?.Remove(key);
        }

        public void RemoveByPrefix(string prefix)
        {
            _l1Cache.RemoveByPrefix(prefix);
            _l2Cache?.RemoveByPrefix(prefix);
        }

        public void Clear()
        {
            _l1Cache.Clear();
            _l2Cache?.Clear();
        }

        public CacheStatistics GetStatistics()
        {
            return _l1Cache.GetStatistics();
        }
    }

    #endregion

    #region Advanced Memory Cache

    /// <summary>
    /// Gelişmiş memory cache - LRU eviction, dependencies, metrics.
    /// </summary>
    public class AdvancedMemoryCache : ICacheService, IDisposable
    {
        private readonly ConcurrentDictionary<string, AdvancedCacheEntry> _cache;
        private readonly ReaderWriterLockSlim _lock;
        private readonly System.Threading.Timer _cleanupTimer;
        private readonly long _maxSizeBytes;
        private long _currentSizeBytes;
        private int _hitCount;
        private int _missCount;

        public AdvancedMemoryCache(long maxSizeMB = 100, int cleanupIntervalSeconds = 60)
        {
            _cache = new ConcurrentDictionary<string, AdvancedCacheEntry>();
            _lock = new ReaderWriterLockSlim();
            _maxSizeBytes = maxSizeMB * 1024 * 1024;

            _cleanupTimer = new System.Threading.Timer(
                _ => Cleanup(),
                null,
                TimeSpan.FromSeconds(cleanupIntervalSeconds),
                TimeSpan.FromSeconds(cleanupIntervalSeconds));
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            if (TryGet<T>(key, out var existing))
            {
                return existing;
            }

            var value = await factory();
            Set(key, value, expiration);
            return value;
        }

        public Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, CachePolicy policy)
        {
            if (TryGet<T>(key, out var existing))
            {
                return Task.FromResult(existing);
            }

            return GetOrCreateInternalAsync(key, factory, policy);
        }

        private async Task<T> GetOrCreateInternalAsync<T>(string key, Func<Task<T>> factory, CachePolicy policy)
        {
            var value = await factory();
            Set(key, value, policy);
            return value;
        }

        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            var policy = new CachePolicy { AbsoluteExpiration = expiration ?? TimeSpan.FromMinutes(30) };
            Set(key, value, policy);
        }

        public void Set<T>(string key, T value, CachePolicy policy)
        {
            var entry = new AdvancedCacheEntry
            {
                Key = key,
                Value = value,
                CreatedAt = DateTime.Now,
                LastAccessed = DateTime.Now,
                Priority = policy.Priority,
                Dependencies = policy.Dependencies,
                Size = policy.Size ?? EstimateSize(value)
            };

            if (policy.AbsoluteExpiration.HasValue)
            {
                entry.AbsoluteExpiration = DateTime.Now.Add(policy.AbsoluteExpiration.Value);
            }

            entry.SlidingExpiration = policy.SlidingExpiration;

            // Boyut kontrolü
            while (_currentSizeBytes + (entry.Size ?? 0) > _maxSizeBytes && _cache.Count > 0)
            {
                Evict();
            }

            if (_cache.TryGetValue(key, out var oldEntry))
            {
                Interlocked.Add(ref _currentSizeBytes, -(oldEntry.Size ?? 0));
            }

            _cache[key] = entry;
            Interlocked.Add(ref _currentSizeBytes, entry.Size ?? 0);

            Logger.Debug($"Cache SET: {key} (size: {entry.Size ?? 0} bytes)");
        }

        public bool TryGet<T>(string key, out T value)
        {
            value = default;

            if (!_cache.TryGetValue(key, out var entry))
            {
                Interlocked.Increment(ref _missCount);
                return false;
            }

            if (entry.IsExpired)
            {
                Remove(key);
                Interlocked.Increment(ref _missCount);
                return false;
            }

            // Dependency check
            if (entry.Dependencies != null)
            {
                foreach (var dep in entry.Dependencies)
                {
                    if (!_cache.ContainsKey(dep))
                    {
                        Remove(key);
                        Interlocked.Increment(ref _missCount);
                        return false;
                    }
                }
            }

            entry.Touch();
            Interlocked.Increment(ref _hitCount);

            try
            {
                value = (T)entry.Value;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Remove(string key)
        {
            if (_cache.TryRemove(key, out var entry))
            {
                Interlocked.Add(ref _currentSizeBytes, -(entry.Size ?? 0));

                // Bağımlı öğeleri de sil
                var dependents = _cache.Values
                    .Where(e => e.Dependencies?.Contains(key) ?? false)
                    .Select(e => e.Key)
                    .ToList();

                foreach (var dep in dependents)
                {
                    Remove(dep);
                }
            }
        }

        public void RemoveByPrefix(string prefix)
        {
            var keysToRemove = _cache.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keysToRemove)
            {
                Remove(key);
            }
        }

        public void Clear()
        {
            _cache.Clear();
            Interlocked.Exchange(ref _currentSizeBytes, 0);
            Logger.Info("Cache cleared");
        }

        public CacheStatistics GetStatistics()
        {
            return new CacheStatistics
            {
                TotalItems = _cache.Count,
                HitCount = _hitCount,
                MissCount = _missCount
            };
        }

        public AdvancedCacheStatistics GetAdvancedStatistics()
        {
            var entries = _cache.Values.ToList();

            return new AdvancedCacheStatistics
            {
                TotalItems = entries.Count,
                HitCount = _hitCount,
                MissCount = _missCount,
                TotalSizeBytes = _currentSizeBytes,
                MaxSizeBytes = _maxSizeBytes,
                UtilizationPercent = _maxSizeBytes > 0 ? (double)_currentSizeBytes / _maxSizeBytes * 100 : 0,
                AverageAccessCount = entries.Count > 0 ? entries.Average(e => e.AccessCount) : 0,
                OldestEntry = entries.OrderBy(e => e.CreatedAt).FirstOrDefault()?.CreatedAt,
                NewestEntry = entries.OrderByDescending(e => e.CreatedAt).FirstOrDefault()?.CreatedAt,
                PriorityDistribution = entries
                    .GroupBy(e => e.Priority)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        private void Cleanup()
        {
            var expiredKeys = _cache.Values
                .Where(e => e.IsExpired)
                .Select(e => e.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                Remove(key);
            }

            if (expiredKeys.Count > 0)
            {
                Logger.Debug($"Cache cleanup: {expiredKeys.Count} expired entries removed");
            }
        }

        private void Evict()
        {
            // LRU with priority - düşük öncelikli ve en az kullanılanı sil
            var candidate = _cache.Values
                .Where(e => e.Priority != CachePriority.NeverRemove)
                .OrderBy(e => e.Priority)
                .ThenBy(e => e.AccessCount)
                .ThenBy(e => e.LastAccessed)
                .FirstOrDefault();

            if (candidate != null)
            {
                Remove(candidate.Key);
                Logger.Debug($"Cache evicted: {candidate.Key}");
            }
        }

        private long EstimateSize<T>(T value)
        {
            if (value == null) return 0;

            // Basit tahmin - gerçek uygulamada daha sofistike olabilir
            try
            {
                var json = JsonSerializer.Serialize(value);
                return Encoding.UTF8.GetByteCount(json);
            }
            catch
            {
                return 1024; // Varsayılan 1KB
            }
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            _lock?.Dispose();
        }
    }

    public class AdvancedCacheStatistics : CacheStatistics
    {
        public long TotalSizeBytes { get; set; }
        public long MaxSizeBytes { get; set; }
        public double UtilizationPercent { get; set; }
        public double AverageAccessCount { get; set; }
        public DateTime? OldestEntry { get; set; }
        public DateTime? NewestEntry { get; set; }
        public Dictionary<CachePriority, int> PriorityDistribution { get; set; }
    }

    #endregion

    #region Cache Key Builder

    /// <summary>
    /// Cache key oluşturucu.
    /// </summary>
    public static class CacheKeyBuilder
    {
        private const string Separator = ":";

        public static string Build(params object[] parts)
        {
            var builder = new StringBuilder();

            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0) builder.Append(Separator);

                var part = parts[i];
                if (part == null)
                {
                    builder.Append("null");
                }
                else if (part is IEnumerable<object> enumerable && !(part is string))
                {
                    builder.Append("[");
                    builder.Append(string.Join(",", enumerable));
                    builder.Append("]");
                }
                else
                {
                    builder.Append(part.ToString());
                }
            }

            return builder.ToString();
        }

        public static string BuildHashed(params object[] parts)
        {
            var key = Build(parts);
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
            return Convert.ToBase64String(hash).Replace("/", "_").Replace("+", "-").Substring(0, 16);
        }

        public static string ForRoute(string baslangicId, string hedefId, string yolcuTipi)
        {
            return Build("route", baslangicId, hedefId, yolcuTipi);
        }

        public static string ForDurak(string durakId)
        {
            return Build("durak", durakId);
        }

        public static string ForGraph(string version = null)
        {
            return Build("graph", version ?? "default");
        }
    }

    #endregion

    #region Cache Warming

    /// <summary>
    /// Cache ön ısıtma servisi.
    /// </summary>
    public class CacheWarmingService
    {
        private readonly ICacheService _cache;
        private readonly List<Func<Task>> _warmupTasks;

        public CacheWarmingService(ICacheService cache)
        {
            _cache = cache;
            _warmupTasks = new List<Func<Task>>();
        }

        public void RegisterWarmupTask(Func<Task> task)
        {
            _warmupTasks.Add(task);
        }

        public async Task WarmupAsync(IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            Logger.Info($"Cache warming başlıyor... ({_warmupTasks.Count} task)");

            for (int i = 0; i < _warmupTasks.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await _warmupTasks[i]();
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Cache warmup task {i + 1} failed: {ex.Message}");
                }

                progress?.Report((i + 1) * 100 / _warmupTasks.Count);
            }

            Logger.Info("Cache warming tamamlandı.");
        }
    }

    #endregion

    #region Cache Invalidation

    /// <summary>
    /// Cache invalidation stratejileri.
    /// </summary>
    public class CacheInvalidationManager
    {
        private readonly ICacheService _cache;
        private readonly Dictionary<string, HashSet<string>> _tags;
        private readonly object _lock = new object();

        public CacheInvalidationManager(ICacheService cache)
        {
            _cache = cache;
            _tags = new Dictionary<string, HashSet<string>>();
        }

        /// <summary>
        /// Tag ile key ilişkilendirir.
        /// </summary>
        public void TagKey(string key, params string[] tags)
        {
            lock (_lock)
            {
                foreach (var tag in tags)
                {
                    if (!_tags.TryGetValue(tag, out var keys))
                    {
                        keys = new HashSet<string>();
                        _tags[tag] = keys;
                    }
                    keys.Add(key);
                }
            }
        }

        /// <summary>
        /// Tag'e ait tüm key'leri invalidate eder.
        /// </summary>
        public void InvalidateByTag(string tag)
        {
            lock (_lock)
            {
                if (_tags.TryGetValue(tag, out var keys))
                {
                    foreach (var key in keys)
                    {
                        _cache.Remove(key);
                    }

                    Logger.Info($"Invalidated {keys.Count} cache entries for tag: {tag}");
                    _tags.Remove(tag);
                }
            }
        }

        /// <summary>
        /// Birden fazla tag'i invalidate eder.
        /// </summary>
        public void InvalidateByTags(params string[] tags)
        {
            foreach (var tag in tags)
            {
                InvalidateByTag(tag);
            }
        }

        /// <summary>
        /// Tüm tag'leri temizler.
        /// </summary>
        public void ClearAllTags()
        {
            lock (_lock)
            {
                _cache.Clear();
                _tags.Clear();
            }
        }
    }

    #endregion
}
