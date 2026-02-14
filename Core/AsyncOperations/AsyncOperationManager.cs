using System;
using System.Threading;
using System.Threading.Tasks;
using Prolab_4.Core.Logging;

namespace Prolab_4.Core.AsyncOperations
{
    /// <summary>
    /// Asenkron işlemleri yönetmek için yardımcı sınıf.
    /// CancellationToken desteği, timeout yönetimi ve progress reporting sağlar.
    /// </summary>
    public static class AsyncOperationManager
    {
        #region Progress Reporting
        
        /// <summary>
        /// Progress bilgisi rapor eden operasyon çalıştırır.
        /// </summary>
        /// <typeparam name="T">Sonuç tipi</typeparam>
        /// <param name="operation">Çalıştırılacak operasyon</param>
        /// <param name="progress">Progress rapor callback</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>Operasyon sonucu</returns>
        public static async Task<Result<T>> RunWithProgressAsync<T>(
            Func<IProgress<int>, CancellationToken, Task<T>> operation,
            Action<int> onProgress,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var progress = new Progress<int>(onProgress);
                var result = await operation(progress, cancellationToken);
                return Result<T>.Success(result);
            }
            catch (OperationCanceledException)
            {
                Logger.Info("Operasyon kullanıcı tarafından iptal edildi.");
                return Result<T>.Failure("Operasyon iptal edildi.", "OPERATION_CANCELLED");
            }
            catch (Exception ex)
            {
                Logger.Error("Asenkron operasyon hatası.", ex);
                return Result<T>.Failure(ex.Message, "ASYNC_ERROR");
            }
        }
        
        #endregion

        #region Timeout İşlemleri
        
        /// <summary>
        /// Timeout'lu asenkron operasyon çalıştırır.
        /// </summary>
        /// <typeparam name="T">Sonuç tipi</typeparam>
        /// <param name="operation">Çalıştırılacak operasyon</param>
        /// <param name="timeout">Maksimum bekleme süresi</param>
        /// <returns>Operasyon sonucu veya timeout hatası</returns>
        public static async Task<Result<T>> RunWithTimeoutAsync<T>(
            Func<Task<T>> operation,
            TimeSpan timeout)
        {
            try
            {
                using var cts = new CancellationTokenSource(timeout);
                var task = operation();
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token));
                
                if (completedTask == task)
                {
                    return Result<T>.Success(await task);
                }
                else
                {
                    Logger.Warning($"Operasyon timeout'a uğradı: {timeout.TotalSeconds}s");
                    return Result<T>.Failure("Operasyon zaman aşımına uğradı.", "TIMEOUT");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Timeout'lu operasyon hatası.", ex);
                return Result<T>.Failure(ex.Message, "ASYNC_ERROR");
            }
        }
        
        /// <summary>
        /// Retry mekanizmalı asenkron operasyon çalıştırır.
        /// </summary>
        /// <typeparam name="T">Sonuç tipi</typeparam>
        /// <param name="operation">Çalıştırılacak operasyon</param>
        /// <param name="maxRetries">Maksimum deneme sayısı</param>
        /// <param name="delayMs">Denemeler arası bekleme süresi (ms)</param>
        /// <returns>Operasyon sonucu</returns>
        public static async Task<Result<T>> RunWithRetryAsync<T>(
            Func<Task<T>> operation,
            int maxRetries = 3,
            int delayMs = 1000)
        {
            Exception lastException = null;
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var result = await operation();
                    return Result<T>.Success(result);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Logger.Warning($"Deneme {attempt}/{maxRetries} başarısız: {ex.Message}");
                    
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(delayMs * attempt); // Exponential backoff
                    }
                }
            }
            
            Logger.Error($"Operasyon {maxRetries} denemeden sonra başarısız.", lastException);
            return Result<T>.Failure(lastException?.Message ?? "Bilinmeyen hata", "RETRY_EXHAUSTED");
        }
        
        #endregion

        #region Batch İşlemleri
        
        /// <summary>
        /// Birden fazla asenkron operasyonu paralel çalıştırır.
        /// </summary>
        /// <typeparam name="T">Sonuç tipi</typeparam>
        /// <param name="operations">Çalıştırılacak operasyonlar</param>
        /// <returns>Tüm sonuçlar</returns>
        public static async Task<Result<T[]>> RunAllAsync<T>(params Func<Task<T>>[] operations)
        {
            try
            {
                var tasks = new Task<T>[operations.Length];
                for (int i = 0; i < operations.Length; i++)
                {
                    tasks[i] = operations[i]();
                }
                
                var results = await Task.WhenAll(tasks);
                return Result<T[]>.Success(results);
            }
            catch (Exception ex)
            {
                Logger.Error("Paralel operasyon hatası.", ex);
                return Result<T[]>.Failure(ex.Message, "PARALLEL_ERROR");
            }
        }
        
        #endregion

        #region UI Thread İşlemleri
        
        /// <summary>
        /// Ağır işlemi arka planda çalıştırıp UI'ı dondurmadan sonuç döndürür.
        /// </summary>
        /// <typeparam name="T">Sonuç tipi</typeparam>
        /// <param name="heavyOperation">CPU-yoğun operasyon</param>
        /// <returns>Operasyon sonucu</returns>
        public static async Task<Result<T>> RunOnBackgroundAsync<T>(Func<T> heavyOperation)
        {
            try
            {
                var result = await Task.Run(heavyOperation);
                return Result<T>.Success(result);
            }
            catch (Exception ex)
            {
                Logger.Error("Arka plan operasyonu hatası.", ex);
                return Result<T>.Failure(ex.Message, "BACKGROUND_ERROR");
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Asenkron operasyon durumu.
    /// </summary>
    public enum AsyncOperationStatus
    {
        /// <summary>Operasyon başlamadı</summary>
        NotStarted,
        
        /// <summary>Operasyon çalışıyor</summary>
        Running,
        
        /// <summary>Operasyon tamamlandı</summary>
        Completed,
        
        /// <summary>Operasyon iptal edildi</summary>
        Cancelled,
        
        /// <summary>Operasyon hata ile sonuçlandı</summary>
        Failed
    }
}
