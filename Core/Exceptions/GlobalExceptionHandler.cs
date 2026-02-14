using Prolab_4.Core.Logging;
using Prolab_4.Core.Metrics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Prolab_4.Core.Exceptions
{
    #region Error Aggregator

    /// <summary>
    /// Hata toplama ve raporlama servisi.
    /// </summary>
    public class ErrorAggregator
    {
        private readonly ConcurrentQueue<ErrorRecord> _errors = new ConcurrentQueue<ErrorRecord>();
        private readonly int _maxErrors;
        private readonly TimeSpan _retentionPeriod;
        private readonly System.Threading.Timer _cleanupTimer;

        public ErrorAggregator(int maxErrors = 1000, int retentionMinutes = 60)
        {
            _maxErrors = maxErrors;
            _retentionPeriod = TimeSpan.FromMinutes(retentionMinutes);
            
            _cleanupTimer = new System.Threading.Timer(
                _ => Cleanup(),
                null,
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(5));
        }

        public void RecordError(Exception ex, string context = null)
        {
            var record = new ErrorRecord
            {
                Timestamp = DateTime.Now,
                Exception = ex,
                Context = context,
                StackTrace = ex.StackTrace,
                ThreadId = Thread.CurrentThread.ManagedThreadId,
                CorrelationId = Logger.CorrelationId
            };

            _errors.Enqueue(record);

            while (_errors.Count > _maxErrors)
            {
                _errors.TryDequeue(out _);
            }
        }

        public IEnumerable<ErrorRecord> GetRecentErrors(int count = 100)
        {
            return _errors.OrderByDescending(e => e.Timestamp).Take(count);
        }

        public IEnumerable<ErrorSummary> GetErrorSummary()
        {
            return _errors
                .GroupBy(e => e.Exception.GetType().Name)
                .Select(g => new ErrorSummary
                {
                    ExceptionType = g.Key,
                    Count = g.Count(),
                    FirstOccurrence = g.Min(e => e.Timestamp),
                    LastOccurrence = g.Max(e => e.Timestamp),
                    SampleMessage = g.First().Exception.Message
                })
                .OrderByDescending(s => s.Count);
        }

        private void Cleanup()
        {
            var cutoff = DateTime.Now - _retentionPeriod;
            var recentErrors = _errors.Where(e => e.Timestamp >= cutoff).ToList();
            
            while (_errors.TryDequeue(out _)) { }
            
            foreach (var error in recentErrors)
            {
                _errors.Enqueue(error);
            }
        }

        public void Clear()
        {
            while (_errors.TryDequeue(out _)) { }
        }
    }

    public class ErrorRecord
    {
        public DateTime Timestamp { get; set; }
        public Exception Exception { get; set; }
        public string Context { get; set; }
        public string StackTrace { get; set; }
        public int ThreadId { get; set; }
        public string CorrelationId { get; set; }
    }

    public class ErrorSummary
    {
        public string ExceptionType { get; set; }
        public int Count { get; set; }
        public DateTime FirstOccurrence { get; set; }
        public DateTime LastOccurrence { get; set; }
        public string SampleMessage { get; set; }
    }

    #endregion

    #region Global Exception Handler

    /// <summary>
    /// Global exception handler.
    /// Yakalanmayan tüm hataları merkezi olarak işler.
    /// </summary>
    public static class GlobalExceptionHandler
    {
        #region Fields
        
        private static bool _isInitialized = false;
        private static readonly ErrorAggregator _errorAggregator = new ErrorAggregator();
        private static readonly List<IExceptionObserver> _observers = new List<IExceptionObserver>();
        private static readonly object _lock = new object();
        private static Func<Exception, bool> _customFilter;
        private static Action<Exception> _customHandler;
        private static bool _showDialogs = true;
        private static string _crashReportPath;
        
        #endregion

        #region Properties

        /// <summary>
        /// Hata toplama servisi.
        /// </summary>
        public static ErrorAggregator ErrorAggregator => _errorAggregator;

        /// <summary>
        /// Hata dialoglarını göster/gizle.
        /// </summary>
        public static bool ShowDialogs
        {
            get => _showDialogs;
            set => _showDialogs = value;
        }

        #endregion

        #region Initialization
        
        /// <summary>
        /// Global exception handler'ı aktif eder.
        /// Program.cs'te Main metodunda çağrılmalıdır.
        /// </summary>
        public static void Initialize(GlobalExceptionHandlerOptions options = null)
        {
            if (_isInitialized) return;
            
            options ??= new GlobalExceptionHandlerOptions();
            
            _showDialogs = options.ShowDialogs;
            _customFilter = options.ExceptionFilter;
            _customHandler = options.CustomHandler;
            _crashReportPath = options.CrashReportPath ?? 
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CrashReports");

            // CrashReports klasörünü oluştur
            if (!Directory.Exists(_crashReportPath))
            {
                Directory.CreateDirectory(_crashReportPath);
            }

            // Windows Forms için
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += OnThreadException;
            
            // Diğer thread'ler için
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            
            // Task hataları için
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            
            _isInitialized = true;
            Logger.Info("GlobalExceptionHandler aktif edildi.");
        }
        
        #endregion

        #region Observer Pattern

        /// <summary>
        /// Exception observer ekler.
        /// </summary>
        public static void AddObserver(IExceptionObserver observer)
        {
            lock (_lock)
            {
                _observers.Add(observer);
            }
        }

        /// <summary>
        /// Exception observer kaldırır.
        /// </summary>
        public static void RemoveObserver(IExceptionObserver observer)
        {
            lock (_lock)
            {
                _observers.Remove(observer);
            }
        }

        private static void NotifyObservers(Exception ex, ExceptionContext context)
        {
            lock (_lock)
            {
                foreach (var observer in _observers)
                {
                    try
                    {
                        observer.OnException(ex, context);
                    }
                    catch { }
                }
            }
        }

        #endregion

        #region Event Handlers
        
        /// <summary>
        /// UI thread exception handler.
        /// </summary>
        private static void OnThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            HandleException(e.Exception, new ExceptionContext
            {
                Source = "UI Thread",
                IsTerminating = false
            });
        }
        
        /// <summary>
        /// Non-UI thread exception handler.
        /// </summary>
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                HandleException(ex, new ExceptionContext
                {
                    Source = "AppDomain",
                    IsTerminating = e.IsTerminating
                });
            }
        }

        /// <summary>
        /// Unobserved task exception handler.
        /// </summary>
        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved(); // Mark as observed to prevent crash

            if (e.Exception != null)
            {
                HandleException(e.Exception.Flatten(), new ExceptionContext
                {
                    Source = "Task",
                    IsTerminating = false
                });
            }
        }
        
        #endregion

        #region Error Handling
        
        /// <summary>
        /// Exception'ı işler, loglar ve kullanıcıya gösterir.
        /// </summary>
        public static void HandleException(Exception ex, ExceptionContext context = null)
        {
            context ??= new ExceptionContext();

            // Custom filter kontrolü
            if (_customFilter != null && !_customFilter(ex))
            {
                return;
            }

            // Metrikleri güncelle
            try
            {
                MetricsRegistry.Counter("exception_total").Increment();
                MetricsRegistry.Counter($"exception_{ex.GetType().Name}").Increment();
            }
            catch { }

            // Error aggregator'a kaydet
            _errorAggregator.RecordError(ex, context.Source);

            // Detaylı log
            var severity = GetSeverity(ex);
            LogException(ex, context, severity);

            // Observer'ları bilgilendir
            NotifyObservers(ex, context);

            // Custom handler çağır
            try
            {
                _customHandler?.Invoke(ex);
            }
            catch { }

            // Kritik hatalarda crash report oluştur
            if (severity >= ErrorSeverity.Critical || context.IsTerminating)
            {
                CreateCrashReport(ex, context);
            }

            // Kullanıcıya göster
            if (_showDialogs && !context.SuppressDialog)
            {
                ShowErrorDialog(ex, context, severity);
            }
            
            // Kritik hata ise uygulamayı kapat
            if (context.IsTerminating)
            {
                Logger.Critical("Uygulama kritik hata nedeniyle kapanıyor.");
            }
        }

        private static void LogException(Exception ex, ExceptionContext context, ErrorSeverity severity)
        {
            var message = new StringBuilder();
            message.AppendLine($"[{context.Source}] {ex.GetType().Name}: {ex.Message}");
            
            if (ex is ApplicationException appEx)
            {
                message.AppendLine($"  ErrorCode: {appEx.ErrorCode}");
                message.AppendLine($"  CorrelationId: {appEx.CorrelationId}");
                message.AppendLine($"  IsRetryable: {appEx.IsRetryable}");
            }

            switch (severity)
            {
                case ErrorSeverity.Debug:
                case ErrorSeverity.Info:
                    Logger.Info(message.ToString());
                    break;
                case ErrorSeverity.Warning:
                    Logger.Warning(message.ToString(), ex);
                    break;
                case ErrorSeverity.Error:
                    Logger.Error(message.ToString(), ex);
                    break;
                case ErrorSeverity.Critical:
                case ErrorSeverity.Fatal:
                    Logger.Critical(message.ToString(), ex);
                    break;
            }
        }

        private static void ShowErrorDialog(Exception ex, ExceptionContext context, ErrorSeverity severity)
        {
            string userMessage = GetUserFriendlyMessage(ex);
            string title = severity switch
            {
                ErrorSeverity.Warning => "Uyarı",
                ErrorSeverity.Critical or ErrorSeverity.Fatal => "Kritik Hata",
                _ => "Hata"
            };

            var icon = severity switch
            {
                ErrorSeverity.Warning => MessageBoxIcon.Warning,
                ErrorSeverity.Critical or ErrorSeverity.Fatal => MessageBoxIcon.Stop,
                _ => MessageBoxIcon.Error
            };

            try
            {
                if (Application.OpenForms.Count > 0)
                {
                    var mainForm = Application.OpenForms[0];
                    if (mainForm.InvokeRequired)
                    {
                        mainForm.BeginInvoke((Action)(() =>
                        {
                            MessageBox.Show(mainForm, userMessage, title, MessageBoxButtons.OK, icon);
                        }));
                    }
                    else
                    {
                        MessageBox.Show(mainForm, userMessage, title, MessageBoxButtons.OK, icon);
                    }
                }
                else
                {
                    MessageBox.Show(userMessage, title, MessageBoxButtons.OK, icon);
                }
            }
            catch
            {
                Logger.Error("Hata mesajı gösterilemedi.");
            }
        }

        private static ErrorSeverity GetSeverity(Exception ex)
        {
            if (ex is ApplicationException appEx)
            {
                return appEx.Severity;
            }

            return ex switch
            {
                OutOfMemoryException => ErrorSeverity.Fatal,
                StackOverflowException => ErrorSeverity.Fatal,
                AccessViolationException => ErrorSeverity.Fatal,
                ArgumentException => ErrorSeverity.Warning,
                InvalidOperationException => ErrorSeverity.Warning,
                OperationCanceledException => ErrorSeverity.Info,
                _ => ErrorSeverity.Error
            };
        }

        private static void CreateCrashReport(Exception ex, ExceptionContext context)
        {
            try
            {
                var reportPath = Path.Combine(_crashReportPath, 
                    $"crash_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.txt");

                var report = new StringBuilder();
                report.AppendLine("=== CRASH REPORT ===");
                report.AppendLine($"Timestamp: {DateTime.Now:O}");
                report.AppendLine($"CorrelationId: {Logger.CorrelationId}");
                report.AppendLine($"Source: {context.Source}");
                report.AppendLine($"IsTerminating: {context.IsTerminating}");
                report.AppendLine();
                
                report.AppendLine("=== EXCEPTION ===");
                report.AppendLine($"Type: {ex.GetType().FullName}");
                report.AppendLine($"Message: {ex.Message}");
                report.AppendLine($"StackTrace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    report.AppendLine();
                    report.AppendLine("=== INNER EXCEPTION ===");
                    report.AppendLine($"Type: {ex.InnerException.GetType().FullName}");
                    report.AppendLine($"Message: {ex.InnerException.Message}");
                    report.AppendLine($"StackTrace: {ex.InnerException.StackTrace}");
                }

                report.AppendLine();
                report.AppendLine("=== SYSTEM INFO ===");
                report.AppendLine($"OS: {Environment.OSVersion}");
                report.AppendLine($".NET Version: {Environment.Version}");
                report.AppendLine($"Working Set: {Environment.WorkingSet / 1024 / 1024} MB");
                report.AppendLine($"Processor Count: {Environment.ProcessorCount}");
                report.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
                report.AppendLine($"64-bit Process: {Environment.Is64BitProcess}");

                File.WriteAllText(reportPath, report.ToString());
                Logger.Info($"Crash report oluşturuldu: {reportPath}");
            }
            catch (Exception reportEx)
            {
                Logger.Error($"Crash report oluşturulamadı: {reportEx.Message}");
            }
        }
        
        /// <summary>
        /// Exception'dan kullanıcı dostu mesaj üretir.
        /// </summary>
        public static string GetUserFriendlyMessage(Exception ex)
        {
            // Application exception'lar için özel mesaj
            if (ex is ApplicationException appEx && appEx.IsUserFacing)
            {
                return appEx.Message;
            }

            return ex switch
            {
                // Application-specific exceptions
                DurakNotFoundException durakEx => 
                    $"Durak bulunamadı: {durakEx.DurakId}",
                RotaCalculationException rotaEx => 
                    $"Rota hesaplanamadı: {rotaEx.Message}",
                DataLoadException dataEx => 
                    $"Veri yüklenemedi: {dataEx.Message}",
                Exceptions.ValidationException validEx => 
                    FormatValidationErrors(validEx),
                NetworkException => 
                    "Ağ bağlantısı kurulamadı. Lütfen internet bağlantınızı kontrol edin.",
                ConfigurationException => 
                    "Yapılandırma hatası. Lütfen uygulama ayarlarını kontrol edin.",

                // Dosya hataları
                FileNotFoundException => 
                    "Gerekli dosya bulunamadı. Lütfen uygulamayı yeniden yükleyin.",
                DirectoryNotFoundException => 
                    "Gerekli klasör bulunamadı. Lütfen uygulamayı yeniden yükleyin.",
                
                // Ağ hataları
                System.Net.WebException => 
                    "İnternet bağlantısı kurulamadı. Lütfen bağlantınızı kontrol edin.",
                System.Net.Http.HttpRequestException => 
                    "Sunucuya bağlanılamadı. Lütfen daha sonra tekrar deneyin.",
                
                // Veri hataları
                System.Text.Json.JsonException => 
                    "Veri formatı hatalı. Lütfen veri dosyalarını kontrol edin.",
                
                // Bellek hataları
                OutOfMemoryException => 
                    "Bellek yetersiz. Lütfen diğer uygulamaları kapatıp tekrar deneyin.",
                
                // İzin hataları
                UnauthorizedAccessException => 
                    "Dosya erişim izni yok. Lütfen yönetici olarak çalıştırın.",
                
                // Null hataları
                ArgumentNullException argNull => 
                    $"Gerekli bilgi eksik: {argNull.ParamName}",
                NullReferenceException => 
                    "Beklenmeyen veri hatası. Lütfen işlemi tekrar deneyin.",
                
                // Validation hataları
                ArgumentException argEx => 
                    $"Geçersiz değer: {argEx.Message}",
                InvalidOperationException => 
                    "Bu işlem şu anda yapılamaz. Lütfen sırayı kontrol edin.",
                
                // İptal
                OperationCanceledException => 
                    "İşlem iptal edildi.",
                
                // Timeout
                TimeoutException => 
                    "İşlem zaman aşımına uğradı. Lütfen tekrar deneyin.",
                
                // Varsayılan
                _ => "Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.\n\n" +
                     "Hata devam ederse uygulamayı yeniden başlatın."
            };
        }

        private static string FormatValidationErrors(Exceptions.ValidationException validEx)
        {
            if (validEx.Errors == null || validEx.Errors.Count == 0)
            {
                return validEx.Message;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Doğrulama hataları:");
            foreach (var error in validEx.Errors.Take(5))
            {
                sb.AppendLine($"• {error.FieldName}: {error.Message}");
            }

            if (validEx.Errors.Count > 5)
            {
                sb.AppendLine($"... ve {validEx.Errors.Count - 5} hata daha");
            }

            return sb.ToString();
        }
        
        #endregion

        #region Utility Methods

        /// <summary>
        /// Exception'ı güvenli şekilde çalıştırır.
        /// </summary>
        public static T ExecuteSafe<T>(Func<T> action, T defaultValue = default, string context = null)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                HandleException(ex, new ExceptionContext
                {
                    Source = context ?? "ExecuteSafe",
                    SuppressDialog = true
                });
                return defaultValue;
            }
        }

        /// <summary>
        /// Async exception'ı güvenli şekilde çalıştırır.
        /// </summary>
        public static async Task<T> ExecuteSafeAsync<T>(Func<Task<T>> action, T defaultValue = default, string context = null)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                HandleException(ex, new ExceptionContext
                {
                    Source = context ?? "ExecuteSafeAsync",
                    SuppressDialog = true
                });
                return defaultValue;
            }
        }

        #endregion
    }
    
    #endregion

    #region Supporting Types

    /// <summary>
    /// Exception context bilgisi.
    /// </summary>
    public class ExceptionContext
    {
        public string Source { get; set; } = "Unknown";
        public bool IsTerminating { get; set; }
        public bool SuppressDialog { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Exception observer interface.
    /// </summary>
    public interface IExceptionObserver
    {
        void OnException(Exception ex, ExceptionContext context);
    }

    /// <summary>
    /// Global exception handler ayarları.
    /// </summary>
    public class GlobalExceptionHandlerOptions
    {
        public bool ShowDialogs { get; set; } = true;
        public Func<Exception, bool> ExceptionFilter { get; set; }
        public Action<Exception> CustomHandler { get; set; }
        public string CrashReportPath { get; set; }
    }

    #endregion

    #region Legacy Exceptions (Backward Compatibility)

    /// <summary>
    /// Rota hesaplama hatası exception (eski).
    /// </summary>
    public class RotaHesaplamaException : Exception
    {
        public string BaslangicId { get; }
        public string HedefId { get; }
        
        public RotaHesaplamaException(string message, string baslangicId = null, string hedefId = null) 
            : base(message)
        {
            BaslangicId = baslangicId;
            HedefId = hedefId;
        }
    }
    
    /// <summary>
    /// Durak bulunamadı exception (eski).
    /// </summary>
    public class DurakBulunamadiException : Exception
    {
        public string DurakId { get; }
        
        public DurakBulunamadiException(string durakId) 
            : base($"Durak bulunamadı: {durakId}")
        {
            DurakId = durakId;
        }
    }

    #endregion
}
