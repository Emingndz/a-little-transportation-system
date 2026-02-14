using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Prolab_4.Core.Security
{
    /// <summary>
    /// Input sanitization ve güvenlik kontrolleri.
    /// Kullanıcı girdilerini temizler ve doğrular.
    /// </summary>
    public static class InputSanitizer
    {
        #region Constants
        
        // Maximum uzunluklar
        private const int MAX_ID_LENGTH = 50;
        private const int MAX_NAME_LENGTH = 200;
        private const int MAX_PATH_LENGTH = 260;
        
        // Tehlikeli karakterler
        private static readonly char[] DangerousChars = { '<', '>', '"', '\'', '&', '\0', '\r', '\n' };
        
        // Geçerli ID pattern
        private static readonly Regex ValidIdPattern = new Regex(@"^[a-zA-Z0-9_\-]+$", RegexOptions.Compiled);
        
        // Path traversal pattern
        private static readonly Regex PathTraversalPattern = new Regex(@"\.\.[/\\]", RegexOptions.Compiled);
        
        #endregion

        #region String Sanitization
        
        /// <summary>
        /// String değeri temizler ve güvenli hale getirir.
        /// </summary>
        /// <param name="input">Temizlenecek değer</param>
        /// <param name="maxLength">Maximum uzunluk</param>
        /// <returns>Temizlenmiş değer</returns>
        public static string Sanitize(string input, int maxLength = MAX_NAME_LENGTH)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            
            // Trim
            var result = input.Trim();
            
            // Uzunluk kontrolü
            if (result.Length > maxLength)
            {
                result = result.Substring(0, maxLength);
            }
            
            // Tehlikeli karakterleri temizle
            foreach (var c in DangerousChars)
            {
                result = result.Replace(c.ToString(), string.Empty);
            }
            
            return result;
        }
        
        /// <summary>
        /// ID değerini temizler ve doğrular.
        /// </summary>
        /// <param name="id">Temizlenecek ID</param>
        /// <returns>Temizlenmiş ID veya null</returns>
        public static string SanitizeId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }
            
            var sanitized = Sanitize(id, MAX_ID_LENGTH);
            
            // Sadece izin verilen karakterler
            if (!ValidIdPattern.IsMatch(sanitized))
            {
                return null;
            }
            
            return sanitized;
        }
        
        /// <summary>
        /// Dosya yolunu temizler ve güvenli hale getirir.
        /// </summary>
        /// <param name="path">Temizlenecek yol</param>
        /// <returns>Temizlenmiş yol</returns>
        public static string SanitizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }
            
            // Path traversal kontrolü
            if (PathTraversalPattern.IsMatch(path))
            {
                throw new SecurityException("Path traversal tespit edildi!");
            }
            
            var sanitized = Sanitize(path, MAX_PATH_LENGTH);
            
            // Geçersiz path karakterlerini temizle
            foreach (var c in System.IO.Path.GetInvalidPathChars())
            {
                sanitized = sanitized.Replace(c.ToString(), string.Empty);
            }
            
            return sanitized;
        }
        
        #endregion

        #region Numeric Sanitization
        
        /// <summary>
        /// Koordinat değerini doğrular ve sınırlar içinde tutar.
        /// </summary>
        /// <param name="lat">Enlem</param>
        /// <param name="lon">Boylam</param>
        /// <returns>Doğrulanmış koordinatlar veya null</returns>
        public static (double Lat, double Lon)? SanitizeCoordinates(double lat, double lon)
        {
            // NaN ve Infinity kontrolü
            if (double.IsNaN(lat) || double.IsInfinity(lat) ||
                double.IsNaN(lon) || double.IsInfinity(lon))
            {
                return null;
            }
            
            // Geçerli aralık kontrolü
            if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
            {
                return null;
            }
            
            return (lat, lon);
        }
        
        /// <summary>
        /// Pozitif tam sayı değerini doğrular.
        /// </summary>
        /// <param name="value">Değer</param>
        /// <param name="max">Maximum değer</param>
        /// <returns>Doğrulanmış değer veya null</returns>
        public static int? SanitizePositiveInt(int value, int max = int.MaxValue)
        {
            if (value <= 0 || value > max)
            {
                return null;
            }
            return value;
        }
        
        /// <summary>
        /// Non-negative double değerini doğrular.
        /// </summary>
        /// <param name="value">Değer</param>
        /// <param name="max">Maximum değer</param>
        /// <returns>Doğrulanmış değer veya null</returns>
        public static double? SanitizeNonNegativeDouble(double value, double max = double.MaxValue)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return null;
            }
            
            if (value < 0 || value > max)
            {
                return null;
            }
            
            return value;
        }
        
        #endregion

        #region Collection Sanitization
        
        /// <summary>
        /// ID listesini temizler.
        /// </summary>
        /// <param name="ids">ID listesi</param>
        /// <returns>Temizlenmiş ID listesi</returns>
        public static List<string> SanitizeIdList(IEnumerable<string> ids)
        {
            var result = new List<string>();
            
            if (ids == null)
            {
                return result;
            }
            
            foreach (var id in ids)
            {
                var sanitized = SanitizeId(id);
                if (!string.IsNullOrEmpty(sanitized))
                {
                    result.Add(sanitized);
                }
            }
            
            return result;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Güvenlik exception.
    /// </summary>
    public class SecurityException : Exception
    {
        public SecurityException(string message) : base(message) { }
    }
    
    /// <summary>
    /// Rate limiter - aşırı istek koruması.
    /// </summary>
    public class RateLimiter
    {
        #region Fields
        
        private readonly int _maxRequests;
        private readonly TimeSpan _timeWindow;
        private readonly Dictionary<string, Queue<DateTime>> _requestLog;
        private readonly object _lock = new object();
        
        #endregion

        #region Constructor
        
        /// <summary>
        /// RateLimiter oluşturur.
        /// </summary>
        /// <param name="maxRequests">Zaman penceresi içinde izin verilen max istek</param>
        /// <param name="timeWindowSeconds">Zaman penceresi (saniye)</param>
        public RateLimiter(int maxRequests = 100, int timeWindowSeconds = 60)
        {
            _maxRequests = maxRequests;
            _timeWindow = TimeSpan.FromSeconds(timeWindowSeconds);
            _requestLog = new Dictionary<string, Queue<DateTime>>();
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// İsteğin kabul edilip edilmeyeceğini kontrol eder.
        /// </summary>
        /// <param name="clientId">İstemci tanımlayıcı</param>
        /// <returns>İstek kabul edildi mi</returns>
        public bool TryAcquire(string clientId = "default")
        {
            lock (_lock)
            {
                var now = DateTime.Now;
                var cutoff = now - _timeWindow;
                
                // İstemci için queue oluştur
                if (!_requestLog.TryGetValue(clientId, out var queue))
                {
                    queue = new Queue<DateTime>();
                    _requestLog[clientId] = queue;
                }
                
                // Eski istekleri temizle
                while (queue.Count > 0 && queue.Peek() < cutoff)
                {
                    queue.Dequeue();
                }
                
                // Limit kontrolü
                if (queue.Count >= _maxRequests)
                {
                    return false;
                }
                
                // Yeni isteği ekle
                queue.Enqueue(now);
                return true;
            }
        }
        
        /// <summary>
        /// Kalan istek sayısını döner.
        /// </summary>
        /// <param name="clientId">İstemci tanımlayıcı</param>
        /// <returns>Kalan istek sayısı</returns>
        public int GetRemainingRequests(string clientId = "default")
        {
            lock (_lock)
            {
                if (!_requestLog.TryGetValue(clientId, out var queue))
                {
                    return _maxRequests;
                }
                
                var now = DateTime.Now;
                var cutoff = now - _timeWindow;
                
                // Eski istekleri say
                var validCount = 0;
                foreach (var time in queue)
                {
                    if (time >= cutoff) validCount++;
                }
                
                return Math.Max(0, _maxRequests - validCount);
            }
        }
        
        /// <summary>
        /// İstemci için rate limit'i sıfırlar.
        /// </summary>
        /// <param name="clientId">İstemci tanımlayıcı</param>
        public void Reset(string clientId = "default")
        {
            lock (_lock)
            {
                if (_requestLog.ContainsKey(clientId))
                {
                    _requestLog[clientId].Clear();
                }
            }
        }
        
        #endregion
    }
}
