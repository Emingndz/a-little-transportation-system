using System;
using System.Collections.Generic;

namespace Prolab_4.Core.Exceptions
{
    /// <summary>
    /// Uygulama base exception sınıfı.
    /// Tüm özel exceptionlar bundan türer.
    /// </summary>
    public abstract class ApplicationException : Exception
    {
        /// <summary>
        /// Hata kodu (loglama ve izleme için).
        /// </summary>
        public string ErrorCode { get; }
        
        /// <summary>
        /// Hata detayları (key-value).
        /// </summary>
        public Dictionary<string, object> Details { get; }
        
        /// <summary>
        /// Hata seviyesi.
        /// </summary>
        public ErrorSeverity Severity { get; }
        
        /// <summary>
        /// Kullanıcıya gösterilebilir mi.
        /// </summary>
        public bool IsUserFacing { get; }
        
        /// <summary>
        /// Retry yapılabilir mi.
        /// </summary>
        public bool IsRetryable { get; }
        
        /// <summary>
        /// Hata zamanı.
        /// </summary>
        public DateTime OccurredAt { get; }
        
        /// <summary>
        /// Correlation ID (distributed tracing için).
        /// </summary>
        public string CorrelationId { get; }

        protected ApplicationException(
            string message,
            string errorCode,
            ErrorSeverity severity = ErrorSeverity.Error,
            bool isUserFacing = true,
            bool isRetryable = false,
            Exception innerException = null)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            Severity = severity;
            IsUserFacing = isUserFacing;
            IsRetryable = isRetryable;
            Details = new Dictionary<string, object>();
            OccurredAt = DateTime.UtcNow;
            CorrelationId = Guid.NewGuid().ToString("N")[..8].ToUpper();
        }

        /// <summary>
        /// Detay ekler.
        /// </summary>
        public ApplicationException WithDetail(string key, object value)
        {
            Details[key] = value;
            return this;
        }
    }

    /// <summary>
    /// Hata seviyeleri.
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>Debug bilgisi.</summary>
        Debug = 0,
        /// <summary>Bilgi.</summary>
        Info = 1,
        /// <summary>Uyarı.</summary>
        Warning = 2,
        /// <summary>Hata.</summary>
        Error = 3,
        /// <summary>Kritik hata.</summary>
        Critical = 4,
        /// <summary>Sistem çökmesi.</summary>
        Fatal = 5
    }

    #region Domain Exceptions

    // Note: DurakNotFoundException is defined in TransportExceptions.cs

    /// <summary>
    /// Rota hesaplanamadı hatası.
    /// </summary>
    public class RotaCalculationException : ApplicationException
    {
        public string BaslangicId { get; }
        public string HedefId { get; }
        public RotaCalculationFailureReason Reason { get; }

        public RotaCalculationException(
            string baslangicId,
            string hedefId,
            RotaCalculationFailureReason reason,
            Exception innerException = null)
            : base(
                GetMessage(reason),
                "ROTA_CALC_FAILED",
                ErrorSeverity.Warning,
                isUserFacing: true,
                isRetryable: reason == RotaCalculationFailureReason.TemporaryFailure,
                innerException)
        {
            BaslangicId = baslangicId;
            HedefId = hedefId;
            Reason = reason;
            WithDetail("BaslangicId", baslangicId)
                .WithDetail("HedefId", hedefId)
                .WithDetail("Reason", reason.ToString());
        }

        private static string GetMessage(RotaCalculationFailureReason reason)
        {
            return reason switch
            {
                RotaCalculationFailureReason.NoRouteExists => "Bu iki nokta arasında ulaşılabilir bir rota bulunamadı.",
                RotaCalculationFailureReason.InvalidStartPoint => "Başlangıç noktası geçersiz.",
                RotaCalculationFailureReason.InvalidEndPoint => "Hedef noktası geçersiz.",
                RotaCalculationFailureReason.SameStartAndEnd => "Başlangıç ve hedef noktası aynı olamaz.",
                RotaCalculationFailureReason.GraphNotInitialized => "Rota grafiği henüz hazır değil. Lütfen bekleyin.",
                RotaCalculationFailureReason.TemporaryFailure => "Geçici bir hata oluştu. Lütfen tekrar deneyin.",
                RotaCalculationFailureReason.Timeout => "İşlem zaman aşımına uğradı. Lütfen tekrar deneyin.",
                _ => "Rota hesaplanamadı."
            };
        }
    }

    public enum RotaCalculationFailureReason
    {
        NoRouteExists,
        InvalidStartPoint,
        InvalidEndPoint,
        SameStartAndEnd,
        GraphNotInitialized,
        TemporaryFailure,
        Timeout
    }

    // Note: DataLoadException is defined in TransportExceptions.cs

    /// <summary>
    /// Validasyon hatası.
    /// </summary>
    public class ValidationException : ApplicationException
    {
        public List<ValidationError> Errors { get; }

        public ValidationException(string message, params ValidationError[] errors)
            : base(message, "VALIDATION_FAILED", ErrorSeverity.Warning)
        {
            Errors = new List<ValidationError>(errors);
            foreach (var error in errors)
            {
                WithDetail($"Field_{error.FieldName}", error.Message);
            }
        }

        public ValidationException(ValidationError error)
            : this(error.Message, error)
        {
        }
    }

    public class ValidationError
    {
        public string FieldName { get; set; }
        public string Message { get; set; }
        public object AttemptedValue { get; set; }

        public ValidationError(string fieldName, string message, object attemptedValue = null)
        {
            FieldName = fieldName;
            Message = message;
            AttemptedValue = attemptedValue;
        }
    }

    /// <summary>
    /// Ağ/Bağlantı hatası.
    /// </summary>
    public class NetworkException : ApplicationException
    {
        public string Endpoint { get; }
        public int? StatusCode { get; }

        public NetworkException(string endpoint, int? statusCode = null, Exception innerException = null)
            : base(
                "Bağlantı hatası oluştu. Lütfen internet bağlantınızı kontrol edin.",
                "NETWORK_ERROR",
                ErrorSeverity.Warning,
                isUserFacing: true,
                isRetryable: true,
                innerException)
        {
            Endpoint = endpoint;
            StatusCode = statusCode;
            WithDetail("Endpoint", endpoint);
            if (statusCode.HasValue)
                WithDetail("StatusCode", statusCode.Value);
        }
    }

    /// <summary>
    /// Kaynak bulunamadı hatası.
    /// </summary>
    public class ResourceNotFoundException : ApplicationException
    {
        public string ResourceType { get; }
        public string ResourceId { get; }

        public ResourceNotFoundException(string resourceType, string resourceId)
            : base(
                $"{resourceType} bulunamadı: {resourceId}",
                "RESOURCE_NOT_FOUND",
                ErrorSeverity.Warning)
        {
            ResourceType = resourceType;
            ResourceId = resourceId;
            WithDetail("ResourceType", resourceType)
                .WithDetail("ResourceId", resourceId);
        }
    }

    /// <summary>
    /// Yetki hatası.
    /// </summary>
    public class AuthorizationException : ApplicationException
    {
        public string Operation { get; }

        public AuthorizationException(string operation)
            : base(
                "Bu işlem için yetkiniz bulunmamaktadır.",
                "UNAUTHORIZED",
                ErrorSeverity.Warning,
                isUserFacing: true)
        {
            Operation = operation;
            WithDetail("Operation", operation);
        }
    }

    /// <summary>
    /// Rate limit aşıldı hatası.
    /// </summary>
    public class RateLimitExceededException : ApplicationException
    {
        public TimeSpan RetryAfter { get; }

        public RateLimitExceededException(TimeSpan retryAfter)
            : base(
                $"Çok fazla istek gönderildi. Lütfen {retryAfter.TotalSeconds:F0} saniye sonra tekrar deneyin.",
                "RATE_LIMIT_EXCEEDED",
                ErrorSeverity.Warning,
                isUserFacing: true,
                isRetryable: true)
        {
            RetryAfter = retryAfter;
            WithDetail("RetryAfterSeconds", retryAfter.TotalSeconds);
        }
    }

    /// <summary>
    /// Konfigürasyon hatası.
    /// </summary>
    public class ConfigurationException : ApplicationException
    {
        public string ConfigKey { get; }

        public ConfigurationException(string configKey, string message)
            : base(
                message,
                "CONFIG_ERROR",
                ErrorSeverity.Critical,
                isUserFacing: false)
        {
            ConfigKey = configKey;
            WithDetail("ConfigKey", configKey);
        }
    }

    #endregion
}
