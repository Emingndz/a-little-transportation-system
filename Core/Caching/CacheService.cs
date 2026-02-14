using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Prolab_4.Core.Logging;

namespace Prolab_4.Core.Caching
{
    /// <summary>
    /// In-memory cache servisi.
    /// Sık kullanılan verileri bellekte tutar.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Cache'den değer alır veya oluşturur.
        /// </summary>
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
        
        /// <summary>
        /// Cache'e değer ekler.
        /// </summary>
        void Set<T>(string key, T value, TimeSpan? expiration = null);
        
        /// <summary>
        /// Cache'den değer alır.
        /// </summary>
        bool TryGet<T>(string key, out T value);
        
        /// <summary>
        /// Cache'den değeri siler.
        /// </summary>
        void Remove(string key);
        
        /// <summary>
        /// Belirli prefix ile başlayan tüm key'leri siler.
        /// </summary>
        void RemoveByPrefix(string prefix);
        
        /// <summary>
        /// Tüm cache'i temizler.
        /// </summary>
        void Clear();
        
        /// <summary>
        /// Cache istatistiklerini döner.
        /// </summary>
        CacheStatistics GetStatistics();
    }
    
    /// <summary>
    /// Cache istatistikleri.
    /// </summary>
    public class CacheStatistics
    {
        public int TotalItems { get; set; }
        public int HitCount { get; set; }
        public int MissCount { get; set; }
        public double HitRatio => TotalRequests > 0 ? (double)HitCount / TotalRequests : 0;
        public int TotalRequests => HitCount + MissCount;
    }
    
    /// <summary>
    /// Memory cache implementasyonu.
    /// </summary>
    public class MemoryCacheService : ICacheService
    {
        #region Inner Types
        
        private class CacheEntry
        {
            public object Value { get; set; }
            public DateTime? ExpiresAt { get; set; }
            public bool IsExpired => ExpiresAt.HasValue && DateTime.Now > ExpiresAt.Value;
        }
        
        #endregion

        #region Fields
        
        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private readonly SemaphoreSlim _cleanupLock = new SemaphoreSlim(1, 1);
        private readonly System.Threading.Timer _cleanupTimer;
        private readonly TimeSpan _defaultExpiration;
        
        private int _hitCount;
        private int _missCount;
        
        #endregion

        #region Constructor
        
        /// <summary>
        /// MemoryCacheService oluşturur.
        /// </summary>
        /// <param name="defaultExpirationMinutes">Varsayılan expire süresi (dakika)</param>
        /// <param name="cleanupIntervalMinutes">Temizleme aralığı (dakika)</param>
        public MemoryCacheService(int defaultExpirationMinutes = 30, int cleanupIntervalMinutes = 5)
        {
            _cache = new ConcurrentDictionary<string, CacheEntry>();
            _defaultExpiration = TimeSpan.FromMinutes(defaultExpirationMinutes);
            
            // Otomatik temizleme timer'ı
            _cleanupTimer = new System.Threading.Timer(
                _ => CleanupExpiredEntries(),
                null,
                TimeSpan.FromMinutes(cleanupIntervalMinutes),
                TimeSpan.FromMinutes(cleanupIntervalMinutes));
        }
        
        #endregion

        #region Public Methods
        
        /// <inheritdoc/>
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
        
        /// <inheritdoc/>
        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            var entry = new CacheEntry
            {
                Value = value,
                ExpiresAt = expiration.HasValue 
                    ? DateTime.Now.Add(expiration.Value)
                    : DateTime.Now.Add(_defaultExpiration)
            };
            
            _cache.AddOrUpdate(key, entry, (_, _) => entry);
            Logger.Debug($"Cache SET: {key}");
        }
        
        /// <inheritdoc/>
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
                _cache.TryRemove(key, out _);
                Interlocked.Increment(ref _missCount);
                return false;
            }
            
            Interlocked.Increment(ref _hitCount);
            value = (T)entry.Value;
            Logger.Debug($"Cache HIT: {key}");
            return true;
        }
        
        /// <inheritdoc/>
        public void Remove(string key)
        {
            _cache.TryRemove(key, out _);
            Logger.Debug($"Cache REMOVE: {key}");
        }
        
        /// <inheritdoc/>
        public void RemoveByPrefix(string prefix)
        {
            var keysToRemove = new List<string>();
            
            foreach (var key in _cache.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    keysToRemove.Add(key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }
            
            Logger.Debug($"Cache REMOVE BY PREFIX: {prefix} ({keysToRemove.Count} items)");
        }
        
        /// <inheritdoc/>
        public void Clear()
        {
            _cache.Clear();
            Logger.Info("Cache CLEARED");
        }
        
        /// <inheritdoc/>
        public CacheStatistics GetStatistics()
        {
            return new CacheStatistics
            {
                TotalItems = _cache.Count,
                HitCount = _hitCount,
                MissCount = _missCount
            };
        }
        
        #endregion

        #region Private Methods
        
        private void CleanupExpiredEntries()
        {
            if (!_cleanupLock.Wait(0))
            {
                return; // Zaten temizlik yapılıyor
            }
            
            try
            {
                var expiredKeys = new List<string>();
                
                foreach (var kvp in _cache)
                {
                    if (kvp.Value.IsExpired)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }
                
                foreach (var key in expiredKeys)
                {
                    _cache.TryRemove(key, out _);
                }
                
                if (expiredKeys.Count > 0)
                {
                    Logger.Debug($"Cache CLEANUP: {expiredKeys.Count} expired entries removed");
                }
            }
            finally
            {
                _cleanupLock.Release();
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Cache key oluşturucu helper.
    /// Tutarlı key oluşturmak için kullanılır.
    /// </summary>
    public static class CacheKeys
    {
        // Durak cache keys
        public const string DURAK_LIST = "duraklar:list";
        public const string DURAK_DICT = "duraklar:dict";
        public static string DurakById(string id) => $"durak:{id}";
        
        // Rota cache keys
        public static string Rota(string baslangic, string hedef, string yolcuTipi) 
            => $"rota:{baslangic}:{hedef}:{yolcuTipi}";
        
        // Genel prefixler
        public const string PREFIX_DURAK = "durak";
        public const string PREFIX_ROTA = "rota";
    }
}
