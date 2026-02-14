namespace Prolab_4.Core
{
    /// <summary>
    /// İşlem sonuçlarını sarmalayan generic result sınıfı.
    /// Exception fırlatmadan hata yönetimi sağlar.
    /// </summary>
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }
        public string ErrorMessage { get; }
        public string ErrorCode { get; }

        private Result(bool isSuccess, T value, string errorMessage, string errorCode)
        {
            IsSuccess = isSuccess;
            Value = value;
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Başarılı sonuç oluşturur
        /// </summary>
        public static Result<T> Success(T value)
        {
            return new Result<T>(true, value, null, null);
        }

        /// <summary>
        /// Başarısız sonuç oluşturur
        /// </summary>
        public static Result<T> Failure(string errorMessage, string errorCode = "ERROR")
        {
            return new Result<T>(false, default, errorMessage, errorCode);
        }

        /// <summary>
        /// Başarılı ise değeri, başarısız ise alternatif değeri döndürür
        /// </summary>
        public T GetValueOrDefault(T defaultValue = default)
        {
            return IsSuccess ? Value : defaultValue;
        }
    }

    /// <summary>
    /// Değer döndürmeyen işlemler için result sınıfı
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }
        public string ErrorCode { get; }

        private Result(bool isSuccess, string errorMessage, string errorCode)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
        }

        public static Result Success()
        {
            return new Result(true, null, null);
        }

        public static Result Failure(string errorMessage, string errorCode = "ERROR")
        {
            return new Result(false, errorMessage, errorCode);
        }
    }
}
