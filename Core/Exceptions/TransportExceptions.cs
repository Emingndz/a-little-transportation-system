namespace Prolab_4.Core.Exceptions
{
    /// <summary>
    /// Uygulama genelinde kullanılan temel exception sınıfı
    /// </summary>
    public class TransportException : Exception
    {
        public string ErrorCode { get; }
        
        public TransportException(string message) : base(message)
        {
            ErrorCode = "TRANSPORT_ERROR";
        }
        
        public TransportException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }
        
        public TransportException(string message, Exception innerException) : base(message, innerException)
        {
            ErrorCode = "TRANSPORT_ERROR";
        }
    }

    /// <summary>
    /// Durak bulunamadığında fırlatılır
    /// </summary>
    public class DurakNotFoundException : TransportException
    {
        public string DurakId { get; }
        
        public DurakNotFoundException(string durakId) 
            : base($"Durak bulunamadı: {durakId}", "DURAK_NOT_FOUND")
        {
            DurakId = durakId;
        }
    }

    /// <summary>
    /// Rota bulunamadığında fırlatılır
    /// </summary>
    public class RotaNotFoundException : TransportException
    {
        public string BaslangicId { get; }
        public string HedefId { get; }
        
        public RotaNotFoundException(string baslangicId, string hedefId) 
            : base($"'{baslangicId}' ile '{hedefId}' arasında rota bulunamadı.", "ROTA_NOT_FOUND")
        {
            BaslangicId = baslangicId;
            HedefId = hedefId;
        }
    }

    /// <summary>
    /// Veri dosyası okunamadığında fırlatılır
    /// </summary>
    public class DataLoadException : TransportException
    {
        public string FilePath { get; }
        
        public DataLoadException(string filePath, Exception innerException) 
            : base($"Veri dosyası okunamadı: {filePath}", innerException)
        {
            FilePath = filePath;
        }
    }

    /// <summary>
    /// Geçersiz parametre durumunda fırlatılır
    /// </summary>
    public class InvalidParameterException : TransportException
    {
        public string ParameterName { get; }
        
        public InvalidParameterException(string parameterName, string message) 
            : base($"Geçersiz parametre '{parameterName}': {message}", "INVALID_PARAMETER")
        {
            ParameterName = parameterName;
        }
    }

    /// <summary>
    /// Ödeme işlemi başarısız olduğunda fırlatılır
    /// </summary>
    public class PaymentException : TransportException
    {
        public PaymentException(string message) 
            : base(message, "PAYMENT_ERROR")
        {
        }
    }
}
