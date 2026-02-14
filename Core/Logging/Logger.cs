using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Prolab_4.Core.Logging
{
    #region Log Level
    
    /// <summary>
    /// Log seviyeleri (genişletilmiş)
    /// </summary>
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Critical = 5,
        None = 6
    }
    
    #endregion

    #region Log Entry
    
    /// <summary>
    /// Zengin log kaydı
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public LogLevel Level { get; set; }
        public string Message { get; set; }
        public string Category { get; set; }
        public Exception Exception { get; set; }
        public string CallerMemberName { get; set; }
        public string CallerFilePath { get; set; }
        public int CallerLineNumber { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public string CorrelationId { get; set; }
        public int ThreadId { get; set; } = Thread.CurrentThread.ManagedThreadId;
        public long ElapsedMs { get; set; }
        
        public string FormattedMessage
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append($"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] ");
                sb.Append($"[{Level,-8}] ");
                
                if (!string.IsNullOrEmpty(CorrelationId))
                    sb.Append($"[{CorrelationId}] ");
                
                if (!string.IsNullOrEmpty(Category))
                    sb.Append($"[{Category}] ");
                
                sb.Append(Message);
                
                if (ElapsedMs > 0)
                    sb.Append($" ({ElapsedMs}ms)");
                
                if (Exception != null)
                {
                    sb.AppendLine();
                    sb.Append($"  Exception: {Exception.GetType().Name}: {Exception.Message}");
                    sb.AppendLine();
                    sb.Append($"  StackTrace: {Exception.StackTrace}");
                }
                
                return sb.ToString();
            }
        }
    }
    
    #endregion

    #region ILogger Interface
    
    /// <summary>
    /// Logging interface - farklı logger implementasyonları için
    /// </summary>
    public interface ILogger
    {
        void Log(LogLevel level, string message, Exception exception = null);
        void Log(LogEntry entry);
        void Debug(string message);
        void Info(string message);
        void Warning(string message, Exception exception = null);
        void Error(string message, Exception exception = null);
        void Critical(string message, Exception exception = null);
        IDisposable BeginScope(string scopeName);
        IDisposable BeginPerformanceScope(string operationName);
    }
    
    #endregion

    #region Log Sink Interface
    
    /// <summary>
    /// Log çıkış hedefi interface
    /// </summary>
    public interface ILogSink
    {
        string Name { get; }
        LogLevel MinimumLevel { get; set; }
        void Write(LogEntry entry);
        Task FlushAsync();
    }
    
    #endregion

    #region Console Sink
    
    /// <summary>
    /// Console log sink - renkli çıktı
    /// </summary>
    public class ConsoleSink : ILogSink
    {
        public string Name => "Console";
        public LogLevel MinimumLevel { get; set; } = LogLevel.Debug;
        
        private static readonly object _lock = new object();
        
        private static readonly Dictionary<LogLevel, ConsoleColor> _colors = new Dictionary<LogLevel, ConsoleColor>
        {
            { LogLevel.Trace, ConsoleColor.DarkGray },
            { LogLevel.Debug, ConsoleColor.Gray },
            { LogLevel.Info, ConsoleColor.White },
            { LogLevel.Warning, ConsoleColor.Yellow },
            { LogLevel.Error, ConsoleColor.Red },
            { LogLevel.Critical, ConsoleColor.DarkRed }
        };
        
        public void Write(LogEntry entry)
        {
            if (entry.Level < MinimumLevel) return;
            
            lock (_lock)
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = _colors.GetValueOrDefault(entry.Level, ConsoleColor.White);
                Console.WriteLine(entry.FormattedMessage);
                Console.ForegroundColor = originalColor;
            }
        }
        
        public Task FlushAsync() => Task.CompletedTask;
    }
    
    #endregion

    #region File Sink
    
    /// <summary>
    /// Dosya log sink - rotation destekli
    /// </summary>
    public class FileSink : ILogSink, IDisposable
    {
        public string Name => "File";
        public LogLevel MinimumLevel { get; set; } = LogLevel.Info;
        
        private readonly string _logDirectory;
        private readonly string _filePrefix;
        private readonly long _maxFileSizeBytes;
        private readonly int _maxFileCount;
        private readonly ConcurrentQueue<LogEntry> _buffer;
        private readonly System.Threading.Timer _flushTimer;
        private StreamWriter _writer;
        private string _currentFilePath;
        private readonly object _writeLock = new object();
        private bool _disposed;
        
        public FileSink(
            string logDirectory = null,
            string filePrefix = "app",
            long maxFileSizeMB = 10,
            int maxFileCount = 10,
            int flushIntervalSeconds = 3)
        {
            _logDirectory = logDirectory ?? Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Logs");
            _filePrefix = filePrefix;
            _maxFileSizeBytes = maxFileSizeMB * 1024 * 1024;
            _maxFileCount = maxFileCount;
            _buffer = new ConcurrentQueue<LogEntry>();
            
            EnsureDirectoryExists();
            OpenNewFile();
            
            _flushTimer = new System.Threading.Timer(
                _ => FlushBuffer(),
                null,
                TimeSpan.FromSeconds(flushIntervalSeconds),
                TimeSpan.FromSeconds(flushIntervalSeconds));
        }
        
        public void Write(LogEntry entry)
        {
            if (entry.Level < MinimumLevel || _disposed) return;
            _buffer.Enqueue(entry);
            
            // Buffer çok doluysa hemen flush et
            if (_buffer.Count > 100)
            {
                Task.Run(() => FlushBuffer());
            }
        }
        
        public async Task FlushAsync()
        {
            await Task.Run(() => FlushBuffer());
        }
        
        private void FlushBuffer()
        {
            if (_disposed) return;
            
            lock (_writeLock)
            {
                try
                {
                    while (_buffer.TryDequeue(out var entry))
                    {
                        _writer?.WriteLine(entry.FormattedMessage);
                    }
                    _writer?.Flush();
                    
                    CheckRotation();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"FileSink error: {ex.Message}");
                }
            }
        }
        
        private void CheckRotation()
        {
            try
            {
                var fileInfo = new FileInfo(_currentFilePath);
                if (fileInfo.Exists && fileInfo.Length > _maxFileSizeBytes)
                {
                    _writer?.Dispose();
                    RotateFiles();
                    OpenNewFile();
                }
            }
            catch { }
        }
        
        private void OpenNewFile()
        {
            _currentFilePath = Path.Combine(_logDirectory, 
                $"{_filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            _writer = new StreamWriter(_currentFilePath, true, Encoding.UTF8);
        }
        
        private void RotateFiles()
        {
            try
            {
                var logFiles = Directory.GetFiles(_logDirectory, $"{_filePrefix}_*.log")
                    .OrderByDescending(f => f)
                    .ToList();
                
                while (logFiles.Count >= _maxFileCount)
                {
                    var oldestFile = logFiles.Last();
                    File.Delete(oldestFile);
                    logFiles.Remove(oldestFile);
                }
            }
            catch { }
        }
        
        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            
            _flushTimer?.Dispose();
            FlushBuffer();
            _writer?.Dispose();
        }
    }
    
    #endregion

    #region Debug Sink
    
    /// <summary>
    /// Debug output sink
    /// </summary>
    public class DebugSink : ILogSink
    {
        public string Name => "Debug";
        public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;
        
        public void Write(LogEntry entry)
        {
            if (entry.Level < MinimumLevel) return;
            System.Diagnostics.Debug.WriteLine(entry.FormattedMessage);
        }
        
        public Task FlushAsync() => Task.CompletedTask;
    }
    
    #endregion

    #region Memory Sink
    
    /// <summary>
    /// Bellek içi log sink (test ve debug için)
    /// </summary>
    public class MemorySink : ILogSink
    {
        public string Name => "Memory";
        public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;
        
        private readonly ConcurrentQueue<LogEntry> _entries = new ConcurrentQueue<LogEntry>();
        private readonly int _maxEntries;
        
        public MemorySink(int maxEntries = 1000)
        {
            _maxEntries = maxEntries;
        }
        
        public IEnumerable<LogEntry> Entries => _entries.ToArray();
        public int Count => _entries.Count;
        
        public IEnumerable<LogEntry> GetErrors() => 
            _entries.Where(e => e.Level >= LogLevel.Error).ToArray();
        
        public void Write(LogEntry entry)
        {
            if (entry.Level < MinimumLevel) return;
            
            _entries.Enqueue(entry);
            
            while (_entries.Count > _maxEntries)
            {
                _entries.TryDequeue(out _);
            }
        }
        
        public Task FlushAsync() => Task.CompletedTask;
        
        public void Clear()
        {
            while (_entries.TryDequeue(out _)) { }
        }
    }
    
    #endregion

    #region Advanced Logger
    
    /// <summary>
    /// Gelişmiş dosya ve Console'a log yazan logger implementasyonu
    /// </summary>
    public class AdvancedLogger : ILogger
    {
        private readonly List<ILogSink> _sinks = new List<ILogSink>();
        private readonly LogLevel _minimumLevel;
        private readonly object _lock = new object();
        private readonly AsyncLocal<string> _correlationId = new AsyncLocal<string>();
        private readonly AsyncLocal<string> _scope = new AsyncLocal<string>();
        
        private static AdvancedLogger _instance;
        public static AdvancedLogger Instance => _instance ??= CreateDefault();
        
        private static AdvancedLogger CreateDefault()
        {
            var logger = new AdvancedLogger(LogLevel.Debug);
            logger.AddSink(new DebugSink());
            logger.AddSink(new FileSink(filePrefix: "transport"));
            return logger;
        }
        
        public AdvancedLogger(LogLevel minimumLevel = LogLevel.Debug)
        {
            _minimumLevel = minimumLevel;
        }
        
        public string CorrelationId
        {
            get => _correlationId.Value ?? Guid.NewGuid().ToString("N").Substring(0, 8);
            set => _correlationId.Value = value;
        }
        
        public void AddSink(ILogSink sink)
        {
            lock (_lock)
            {
                _sinks.Add(sink);
            }
        }
        
        public void Log(LogLevel level, string message, Exception exception = null)
        {
            if (level < _minimumLevel) return;
            
            var entry = new LogEntry
            {
                Level = level,
                Message = message,
                Exception = exception,
                CorrelationId = _correlationId.Value,
                Category = _scope.Value
            };
            
            Log(entry);
        }
        
        public void Log(LogEntry entry)
        {
            if (entry.Level < _minimumLevel) return;
            
            lock (_lock)
            {
                foreach (var sink in _sinks)
                {
                    try
                    {
                        sink.Write(entry);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Sink error: {ex.Message}");
                    }
                }
            }
        }
        
        public void Debug(string message) => Log(LogLevel.Debug, message);
        public void Info(string message) => Log(LogLevel.Info, message);
        public void Warning(string message, Exception exception = null) => Log(LogLevel.Warning, message, exception);
        public void Error(string message, Exception exception = null) => Log(LogLevel.Error, message, exception);
        public void Critical(string message, Exception exception = null) => Log(LogLevel.Critical, message, exception);
        
        public IDisposable BeginScope(string scopeName)
        {
            var previousScope = _scope.Value;
            _scope.Value = scopeName;
            return new ScopeDisposer(() => _scope.Value = previousScope);
        }
        
        public IDisposable BeginPerformanceScope(string operationName)
        {
            return new PerformanceScope(this, operationName);
        }
        
        private class ScopeDisposer : IDisposable
        {
            private readonly Action _onDispose;
            public ScopeDisposer(Action onDispose) => _onDispose = onDispose;
            public void Dispose() => _onDispose?.Invoke();
        }
        
        private class PerformanceScope : IDisposable
        {
            private readonly AdvancedLogger _logger;
            private readonly string _operationName;
            private readonly Stopwatch _stopwatch;
            
            public PerformanceScope(AdvancedLogger logger, string operationName)
            {
                _logger = logger;
                _operationName = operationName;
                _stopwatch = Stopwatch.StartNew();
                _logger.Debug($"[PERF] {_operationName} started");
            }
            
            public void Dispose()
            {
                _stopwatch.Stop();
                var entry = new LogEntry
                {
                    Level = LogLevel.Info,
                    Message = $"[PERF] {_operationName} completed",
                    ElapsedMs = _stopwatch.ElapsedMilliseconds
                };
                _logger.Log(entry);
            }
        }
        
        public async Task FlushAsync()
        {
            foreach (var sink in _sinks)
            {
                await sink.FlushAsync();
            }
        }
    }
    
    #endregion

    #region Legacy FileLogger (Geriye uyumluluk)
    
    /// <summary>
    /// Eski FileLogger - geriye uyumluluk için
    /// </summary>
    public class FileLogger : ILogger
    {
        private readonly string _logFilePath;
        private readonly LogLevel _minimumLevel;
        private readonly object _lock = new object();
        private static FileLogger _instance;

        public static FileLogger Instance => _instance ??= new FileLogger();

        private FileLogger(LogLevel minimumLevel = LogLevel.Debug)
        {
            _minimumLevel = minimumLevel;
            
            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            
            _logFilePath = Path.Combine(logDirectory, $"transport_{DateTime.Now:yyyyMMdd}.log");
        }

        public void Log(LogLevel level, string message, Exception exception = null)
        {
            if (level < _minimumLevel) return;

            var logEntry = new StringBuilder();
            logEntry.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ");
            logEntry.Append($"[{level.ToString().ToUpper()}] ");
            logEntry.Append(message);

            if (exception != null)
            {
                logEntry.AppendLine();
                logEntry.Append($"  Exception: {exception.GetType().Name}: {exception.Message}");
                logEntry.AppendLine();
                logEntry.Append($"  StackTrace: {exception.StackTrace}");
            }

            var logMessage = logEntry.ToString();
            WriteToConsole(level, logMessage);
            WriteToFile(logMessage);
        }

        public void Log(LogEntry entry)
        {
            Log(entry.Level, entry.Message, entry.Exception);
        }

        private void WriteToConsole(LogLevel level, string message)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };
            
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        private void WriteToFile(string message)
        {
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(_logFilePath, message + Environment.NewLine);
                }
            }
            catch { }
        }

        public void Debug(string message) => Log(LogLevel.Debug, message);
        public void Info(string message) => Log(LogLevel.Info, message);
        public void Warning(string message, Exception exception = null) => Log(LogLevel.Warning, message, exception);
        public void Error(string message, Exception exception = null) => Log(LogLevel.Error, message, exception);
        public void Critical(string message, Exception exception = null) => Log(LogLevel.Critical, message, exception);
        public IDisposable BeginScope(string scopeName) => new NullDisposable();
        public IDisposable BeginPerformanceScope(string operationName) => new NullDisposable();
        
        private class NullDisposable : IDisposable { public void Dispose() { } }
    }
    
    #endregion

    #region Static Logger
    
    /// <summary>
    /// Kolay erişim için static logger yardımcı sınıfı
    /// </summary>
    public static class Logger
    {
        private static ILogger _logger = AdvancedLogger.Instance;
        private static readonly AsyncLocal<string> _correlationId = new AsyncLocal<string>();

        public static void SetLogger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public static string CorrelationId
        {
            get => _correlationId.Value ?? Guid.NewGuid().ToString("N").Substring(0, 8);
            set => _correlationId.Value = value;
        }

        public static void Trace(string message) => Debug(message);
        public static void Debug(string message) => _logger.Debug(message);
        public static void Info(string message) => _logger.Info(message);
        public static void Warning(string message, Exception exception = null) => _logger.Warning(message, exception);
        public static void Error(string message, Exception exception = null) => _logger.Error(message, exception);
        public static void Critical(string message, Exception exception = null) => _logger.Critical(message, exception);
        
        public static IDisposable BeginScope(string scopeName) => _logger.BeginScope(scopeName);
        public static IDisposable BeginPerformanceScope(string operationName) => _logger.BeginPerformanceScope(operationName);
        
        public static IDisposable BeginCorrelationScope(string correlationId = null)
        {
            var previousId = _correlationId.Value;
            _correlationId.Value = correlationId ?? Guid.NewGuid().ToString("N").Substring(0, 8);
            
            if (_logger is AdvancedLogger advLogger)
            {
                advLogger.CorrelationId = _correlationId.Value;
            }
            
            return new CorrelationScope(previousId);
        }
        
        private class CorrelationScope : IDisposable
        {
            private readonly string _previousId;
            public CorrelationScope(string previousId) => _previousId = previousId;
            public void Dispose() => _correlationId.Value = _previousId;
        }
        
        /// <summary>
        /// Performans ölçümü
        /// </summary>
        public static T MeasurePerformance<T>(string operationName, Func<T> operation)
        {
            using (BeginPerformanceScope(operationName))
            {
                return operation();
            }
        }
        
        /// <summary>
        /// Async performans ölçümü
        /// </summary>
        public static async Task<T> MeasurePerformanceAsync<T>(string operationName, Func<Task<T>> operation)
        {
            using (BeginPerformanceScope(operationName))
            {
                return await operation();
            }
        }
    }
    
    #endregion
}
