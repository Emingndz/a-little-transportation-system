using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Prolab_4.Core.Logging;

namespace Prolab_4.Core.Security
{
    #region Input Validation

    /// <summary>
    /// Gelişmiş input doğrulama.
    /// </summary>
    public static class InputValidator
    {
        private static readonly Regex EmailRegex = new(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);
        private static readonly Regex PhoneRegex = new(@"^\+?[\d\s-]{10,15}$", RegexOptions.Compiled);
        private static readonly Regex AlphanumericRegex = new(@"^[a-zA-Z0-9]+$", RegexOptions.Compiled);
        private static readonly Regex UrlRegex = new(@"^https?://[^\s/$.?#].[^\s]*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static ValidationResult Validate<T>(T value, params IValidationRule<T>[] rules)
        {
            var result = new ValidationResult();

            foreach (var rule in rules)
            {
                var ruleResult = rule.Validate(value);
                if (!ruleResult.IsValid)
                {
                    result.Errors.AddRange(ruleResult.Errors);
                }
            }

            return result;
        }

        public static bool IsValidEmail(string email) =>
            !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email);

        public static bool IsValidPhone(string phone) =>
            !string.IsNullOrWhiteSpace(phone) && PhoneRegex.IsMatch(phone);

        public static bool IsAlphanumeric(string value) =>
            !string.IsNullOrWhiteSpace(value) && AlphanumericRegex.IsMatch(value);

        public static bool IsValidUrl(string url) =>
            !string.IsNullOrWhiteSpace(url) && UrlRegex.IsMatch(url);

        public static bool IsInRange(int value, int min, int max) =>
            value >= min && value <= max;

        public static bool IsInRange(double value, double min, double max) =>
            value >= min && value <= max;

        public static bool IsValidCoordinate(double latitude, double longitude) =>
            latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;

        public static bool IsValidDurakId(string id) =>
            !string.IsNullOrWhiteSpace(id) && id.Length <= 50 && AlphanumericRegex.IsMatch(id.Replace("_", "").Replace("-", ""));
    }

    public interface IValidationRule<T>
    {
        ValidationResult Validate(T value);
    }

    public class ValidationResult
    {
        public List<string> Errors { get; } = new List<string>();
        public bool IsValid => Errors.Count == 0;

        public static ValidationResult Success() => new ValidationResult();

        public static ValidationResult Failure(string error)
        {
            var result = new ValidationResult();
            result.Errors.Add(error);
            return result;
        }

        public static ValidationResult Failure(IEnumerable<string> errors)
        {
            var result = new ValidationResult();
            result.Errors.AddRange(errors);
            return result;
        }
    }

    public class RequiredRule<T> : IValidationRule<T>
    {
        private readonly string _fieldName;

        public RequiredRule(string fieldName = "Value")
        {
            _fieldName = fieldName;
        }

        public ValidationResult Validate(T value)
        {
            if (value == null || (value is string s && string.IsNullOrWhiteSpace(s)))
            {
                return ValidationResult.Failure($"{_fieldName} is required");
            }
            return ValidationResult.Success();
        }
    }

    public class StringLengthRule : IValidationRule<string>
    {
        private readonly int _minLength;
        private readonly int _maxLength;
        private readonly string _fieldName;

        public StringLengthRule(int minLength, int maxLength, string fieldName = "Value")
        {
            _minLength = minLength;
            _maxLength = maxLength;
            _fieldName = fieldName;
        }

        public ValidationResult Validate(string value)
        {
            if (value == null) return ValidationResult.Success();

            if (value.Length < _minLength || value.Length > _maxLength)
            {
                return ValidationResult.Failure(
                    $"{_fieldName} must be between {_minLength} and {_maxLength} characters");
            }
            return ValidationResult.Success();
        }
    }

    public class RangeRule<T> : IValidationRule<T> where T : IComparable<T>
    {
        private readonly T _min;
        private readonly T _max;
        private readonly string _fieldName;

        public RangeRule(T min, T max, string fieldName = "Value")
        {
            _min = min;
            _max = max;
            _fieldName = fieldName;
        }

        public ValidationResult Validate(T value)
        {
            if (value.CompareTo(_min) < 0 || value.CompareTo(_max) > 0)
            {
                return ValidationResult.Failure(
                    $"{_fieldName} must be between {_min} and {_max}");
            }
            return ValidationResult.Success();
        }
    }

    public class CustomRule<T> : IValidationRule<T>
    {
        private readonly Func<T, bool> _predicate;
        private readonly string _errorMessage;

        public CustomRule(Func<T, bool> predicate, string errorMessage)
        {
            _predicate = predicate;
            _errorMessage = errorMessage;
        }

        public ValidationResult Validate(T value)
        {
            if (!_predicate(value))
            {
                return ValidationResult.Failure(_errorMessage);
            }
            return ValidationResult.Success();
        }
    }

    #endregion

    #region Rate Limiting

    /// <summary>
    /// Gelişmiş Rate limiter - sliding window algoritması (thread-safe, IDisposable).
    /// Not: InputSanitizer.cs'deki RateLimiter basit versiyondur.
    /// </summary>
    public class AdvancedRateLimiter : IDisposable
    {
        private readonly ConcurrentDictionary<string, SlidingWindow> _windows;
        private readonly int _maxRequests;
        private readonly TimeSpan _windowSize;
        private readonly System.Threading.Timer _cleanupTimer;

        public AdvancedRateLimiter(int maxRequests, TimeSpan windowSize)
        {
            _maxRequests = maxRequests;
            _windowSize = windowSize;
            _windows = new ConcurrentDictionary<string, SlidingWindow>();

            _cleanupTimer = new System.Threading.Timer(_ => Cleanup(), null,
                TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        public bool IsAllowed(string key)
        {
            var window = _windows.GetOrAdd(key, _ => new SlidingWindow(_windowSize));
            return window.IsAllowed(_maxRequests);
        }

        public RateLimitInfo GetInfo(string key)
        {
            if (!_windows.TryGetValue(key, out var window))
            {
                return new RateLimitInfo
                {
                    IsAllowed = true,
                    RemainingRequests = _maxRequests,
                    ResetTime = DateTime.Now.Add(_windowSize)
                };
            }

            var count = window.GetCount();
            var resetTime = window.GetResetTime();

            return new RateLimitInfo
            {
                IsAllowed = count < _maxRequests,
                RemainingRequests = Math.Max(0, _maxRequests - count),
                ResetTime = resetTime
            };
        }

        public void Reset(string key)
        {
            _windows.TryRemove(key, out _);
        }

        private void Cleanup()
        {
            var expiredKeys = _windows
                .Where(kvp => kvp.Value.IsExpired())
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _windows.TryRemove(key, out _);
            }
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }

        private class SlidingWindow
        {
            private readonly Queue<DateTime> _requests;
            private readonly TimeSpan _windowSize;
            private readonly object _lock = new object();

            public SlidingWindow(TimeSpan windowSize)
            {
                _windowSize = windowSize;
                _requests = new Queue<DateTime>();
            }

            public bool IsAllowed(int maxRequests)
            {
                lock (_lock)
                {
                    CleanOldRequests();

                    if (_requests.Count >= maxRequests)
                    {
                        return false;
                    }

                    _requests.Enqueue(DateTime.Now);
                    return true;
                }
            }

            public int GetCount()
            {
                lock (_lock)
                {
                    CleanOldRequests();
                    return _requests.Count;
                }
            }

            public DateTime GetResetTime()
            {
                lock (_lock)
                {
                    if (_requests.Count == 0) return DateTime.Now.Add(_windowSize);
                    return _requests.Peek().Add(_windowSize);
                }
            }

            public bool IsExpired()
            {
                lock (_lock)
                {
                    CleanOldRequests();
                    return _requests.Count == 0;
                }
            }

            private void CleanOldRequests()
            {
                var cutoff = DateTime.Now - _windowSize;
                while (_requests.Count > 0 && _requests.Peek() < cutoff)
                {
                    _requests.Dequeue();
                }
            }
        }
    }

    public class RateLimitInfo
    {
        public bool IsAllowed { get; set; }
        public int RemainingRequests { get; set; }
        public DateTime ResetTime { get; set; }
    }

    #endregion

    #region Encryption

    /// <summary>
    /// Şifreleme yardımcıları.
    /// </summary>
    public static class EncryptionHelper
    {
        private static readonly byte[] DefaultKey = new byte[32];
        private static readonly byte[] DefaultIV = new byte[16];

        static EncryptionHelper()
        {
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(DefaultKey);
            rng.GetBytes(DefaultIV);
        }

        public static string Encrypt(string plainText, byte[] key = null, byte[] iv = null)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            key ??= DefaultKey;
            iv ??= DefaultIV;

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string cipherText, byte[] key = null, byte[] iv = null)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            key ??= DefaultKey;
            iv ??= DefaultIV;

            try
            {
                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);
                return sr.ReadToEnd();
            }
            catch (Exception ex)
            {
                Logger.Warning($"Decryption failed: {ex.Message}");
                return null;
            }
        }

        public static string HashPassword(string password, out string salt)
        {
            var saltBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(saltBytes);
            salt = Convert.ToBase64String(saltBytes);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);
            return Convert.ToBase64String(hash);
        }

        public static bool VerifyPassword(string password, string hash, string salt)
        {
            try
            {
                var saltBytes = Convert.FromBase64String(salt);
                using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100000, HashAlgorithmName.SHA256);
                var computedHash = Convert.ToBase64String(pbkdf2.GetBytes(32));
                return computedHash == hash;
            }
            catch
            {
                return false;
            }
        }

        public static string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public static string GenerateSecureToken(int length = 32)
        {
            var bytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
    }

    #endregion

    #region Audit Logging

    /// <summary>
    /// Güvenlik audit log.
    /// </summary>
    public class AuditLogger
    {
        private static readonly Lazy<AuditLogger> _instance = new(() => new AuditLogger());
        public static AuditLogger Instance => _instance.Value;

        private readonly ConcurrentQueue<AuditEntry> _entries;
        private readonly List<IAuditSink> _sinks;
        private readonly int _maxEntries;
        private readonly object _lock = new object();

        private AuditLogger()
        {
            _entries = new ConcurrentQueue<AuditEntry>();
            _sinks = new List<IAuditSink>();
            _maxEntries = 10000;
        }

        public void AddSink(IAuditSink sink)
        {
            _sinks.Add(sink);
        }

        public void Log(AuditAction action, string details, string userId = null, string ipAddress = null)
        {
            var entry = new AuditEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.Now,
                Action = action,
                Details = details,
                UserId = userId ?? "system",
                IpAddress = ipAddress,
                MachineName = Environment.MachineName,
                ProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName
            };

            _entries.Enqueue(entry);

            // Limit entries
            while (_entries.Count > _maxEntries && _entries.TryDequeue(out _)) { }

            // Write to sinks
            foreach (var sink in _sinks)
            {
                try
                {
                    sink.Write(entry);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Audit sink error: {ex.Message}");
                }
            }
        }

        public void LogSecurityEvent(SecurityEvent securityEvent, string details, string userId = null)
        {
            Log(AuditAction.SecurityEvent, $"[{securityEvent}] {details}", userId);
        }

        public void LogDataAccess(string resource, string operation, string userId = null)
        {
            Log(AuditAction.DataAccess, $"{operation} on {resource}", userId);
        }

        public void LogConfigChange(string setting, string oldValue, string newValue, string userId = null)
        {
            Log(AuditAction.ConfigChange, $"Changed {setting}: '{oldValue}' -> '{newValue}'", userId);
        }

        public IEnumerable<AuditEntry> GetEntries(int count = 100)
        {
            return _entries.TakeLast(count);
        }

        public IEnumerable<AuditEntry> Query(Func<AuditEntry, bool> predicate)
        {
            return _entries.Where(predicate);
        }
    }

    public class AuditEntry
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public AuditAction Action { get; set; }
        public string Details { get; set; }
        public string UserId { get; set; }
        public string IpAddress { get; set; }
        public string MachineName { get; set; }
        public string ProcessName { get; set; }
    }

    public enum AuditAction
    {
        Login,
        Logout,
        DataAccess,
        DataModify,
        DataDelete,
        ConfigChange,
        SecurityEvent,
        Error,
        Other
    }

    public enum SecurityEvent
    {
        LoginSuccess,
        LoginFailed,
        PasswordChanged,
        AccountLocked,
        RateLimitExceeded,
        InvalidInput,
        UnauthorizedAccess,
        SuspiciousActivity
    }

    public interface IAuditSink
    {
        void Write(AuditEntry entry);
    }

    public class FileAuditSink : IAuditSink
    {
        private readonly string _filePath;
        private readonly object _lock = new object();

        public FileAuditSink(string filePath)
        {
            _filePath = filePath;
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        }

        public void Write(AuditEntry entry)
        {
            var line = JsonSerializer.Serialize(entry) + Environment.NewLine;

            lock (_lock)
            {
                File.AppendAllText(_filePath, line);
            }
        }
    }

    public class ConsoleAuditSink : IAuditSink
    {
        public void Write(AuditEntry entry)
        {
            var color = entry.Action switch
            {
                AuditAction.SecurityEvent => ConsoleColor.Red,
                AuditAction.Error => ConsoleColor.Red,
                AuditAction.DataModify => ConsoleColor.Yellow,
                AuditAction.DataDelete => ConsoleColor.Magenta,
                _ => ConsoleColor.Gray
            };

            Console.ForegroundColor = color;
            Console.WriteLine($"[AUDIT] {entry.Timestamp:HH:mm:ss} [{entry.Action}] {entry.Details}");
            Console.ResetColor();
        }
    }

    #endregion

    #region Secure Configuration

    /// <summary>
    /// Güvenli yapılandırma yönetimi.
    /// </summary>
    public class SecureConfiguration
    {
        private readonly Dictionary<string, string> _values;
        private readonly HashSet<string> _sensitiveKeys;
        private readonly object _lock = new object();

        public SecureConfiguration()
        {
            _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _sensitiveKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "password", "secret", "key", "token", "connectionstring", "apikey"
            };
        }

        public void Set(string key, string value, bool isSensitive = false)
        {
            lock (_lock)
            {
                _values[key] = value;

                if (isSensitive)
                {
                    _sensitiveKeys.Add(key);
                }

                AuditLogger.Instance.LogConfigChange(key,
                    IsSensitive(key) ? "***" : "N/A",
                    IsSensitive(key) ? "***" : value);
            }
        }

        public string Get(string key, string defaultValue = null)
        {
            lock (_lock)
            {
                return _values.TryGetValue(key, out var value) ? value : defaultValue;
            }
        }

        public T Get<T>(string key, T defaultValue = default)
        {
            var value = Get(key);
            if (value == null) return defaultValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        public bool IsSensitive(string key)
        {
            return _sensitiveKeys.Contains(key) ||
                   _sensitiveKeys.Any(s => key.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        public Dictionary<string, string> GetAllSafe()
        {
            lock (_lock)
            {
                return _values.ToDictionary(
                    kvp => kvp.Key,
                    kvp => IsSensitive(kvp.Key) ? "***HIDDEN***" : kvp.Value);
            }
        }
    }

    #endregion

    #region Security Context

    /// <summary>
    /// Güvenlik bağlamı.
    /// </summary>
    public class SecurityContext
    {
        private static readonly AsyncLocal<SecurityContext> _current = new AsyncLocal<SecurityContext>();

        public static SecurityContext Current
        {
            get => _current.Value ?? new SecurityContext();
            set => _current.Value = value;
        }

        public string UserId { get; set; }
        public string[] Roles { get; set; } = Array.Empty<string>();
        public Dictionary<string, object> Claims { get; set; } = new Dictionary<string, object>();
        public DateTime? AuthenticatedAt { get; set; }
        public string IpAddress { get; set; }

        public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);

        public bool HasRole(string role)
        {
            return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }

        public bool HasAnyRole(params string[] roles)
        {
            return roles.Any(r => HasRole(r));
        }

        public bool HasAllRoles(params string[] roles)
        {
            return roles.All(r => HasRole(r));
        }

        public T GetClaim<T>(string key, T defaultValue = default)
        {
            if (Claims.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
    }

    #endregion

    #region Security Utilities

    /// <summary>
    /// Güvenlik yardımcı metodları.
    /// </summary>
    public static class SecurityUtils
    {
        private static readonly char[] DangerousChars = new[] { '<', '>', '"', '\'', '&', '\0' };
        private static readonly string[] SqlKeywords = new[] { "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "UNION", "OR", "AND", "--", "/*", "*/" };

        /// <summary>
        /// HTML encoding.
        /// </summary>
        public static string HtmlEncode(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            return System.Net.WebUtility.HtmlEncode(input);
        }

        /// <summary>
        /// SQL injection koruması.
        /// </summary>
        public static bool IsPotentialSqlInjection(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;

            var upper = input.ToUpperInvariant();
            return SqlKeywords.Any(keyword => upper.Contains(keyword));
        }

        /// <summary>
        /// Path traversal koruması.
        /// </summary>
        public static string SanitizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            // Remove dangerous patterns
            path = path.Replace("..", "");
            path = path.Replace("~", "");

            // Normalize separators
            path = path.Replace("/", Path.DirectorySeparatorChar.ToString());
            path = path.Replace("\\", Path.DirectorySeparatorChar.ToString());

            return path;
        }

        /// <summary>
        /// XSS koruması.
        /// </summary>
        public static string SanitizeForDisplay(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var sanitized = HtmlEncode(input);

            // Remove script tags
            sanitized = Regex.Replace(sanitized, @"<script[^>]*>.*?</script>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            return sanitized;
        }

        /// <summary>
        /// Güvenli karşılaştırma (timing attack koruması).
        /// </summary>
        public static bool SecureCompare(string a, string b)
        {
            if (a == null || b == null) return a == b;
            if (a.Length != b.Length) return false;

            var diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }

            return diff == 0;
        }

        /// <summary>
        /// Geçici dosya adı oluşturur (güvenli).
        /// </summary>
        public static string GenerateSecureFileName(string extension = null)
        {
            var name = Path.GetRandomFileName().Replace(".", "");
            return extension != null ? $"{name}.{extension.TrimStart('.')}" : name;
        }
    }

    #endregion
}
