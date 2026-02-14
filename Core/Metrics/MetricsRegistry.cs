using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Prolab_4.Core.Logging;

namespace Prolab_4.Core.Metrics
{
    #region Counter
    
    /// <summary>
    /// Thread-safe sayaç.
    /// </summary>
    public class Counter
    {
        private long _value;
        
        public string Name { get; }
        public string Description { get; }
        public Dictionary<string, string> Tags { get; }
        
        public Counter(string name, string description = null, Dictionary<string, string> tags = null)
        {
            Name = name;
            Description = description;
            Tags = tags ?? new Dictionary<string, string>();
        }
        
        public long Value => Interlocked.Read(ref _value);
        
        public void Increment(long delta = 1)
        {
            Interlocked.Add(ref _value, delta);
        }
        
        public void Decrement(long delta = 1)
        {
            Interlocked.Add(ref _value, -delta);
        }
        
        public void Reset()
        {
            Interlocked.Exchange(ref _value, 0);
        }
    }
    
    #endregion

    #region Gauge
    
    /// <summary>
    /// Anlık değer ölçümü.
    /// </summary>
    public class Gauge
    {
        private double _value;
        private readonly object _lock = new object();
        
        public string Name { get; }
        public string Description { get; }
        public Dictionary<string, string> Tags { get; }
        
        public Gauge(string name, string description = null, Dictionary<string, string> tags = null)
        {
            Name = name;
            Description = description;
            Tags = tags ?? new Dictionary<string, string>();
        }
        
        public double Value
        {
            get { lock (_lock) return _value; }
        }
        
        public void Set(double value)
        {
            lock (_lock) _value = value;
        }
        
        public void Increment(double delta = 1)
        {
            lock (_lock) _value += delta;
        }
        
        public void Decrement(double delta = 1)
        {
            lock (_lock) _value -= delta;
        }
    }
    
    #endregion

    #region Histogram
    
    /// <summary>
    /// Dağılım ölçümü (latency, size, vb).
    /// </summary>
    public class Histogram
    {
        private readonly ConcurrentBag<double> _values = new ConcurrentBag<double>();
        private readonly double[] _buckets;
        private readonly int[] _bucketCounts;
        private long _count;
        private double _sum;
        private double _min = double.MaxValue;
        private double _max = double.MinValue;
        private readonly object _lock = new object();
        
        public string Name { get; }
        public string Description { get; }
        public Dictionary<string, string> Tags { get; }
        
        public Histogram(string name, string description = null, double[] buckets = null, Dictionary<string, string> tags = null)
        {
            Name = name;
            Description = description;
            Tags = tags ?? new Dictionary<string, string>();
            _buckets = buckets ?? new double[] { 1, 5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000 };
            _bucketCounts = new int[_buckets.Length + 1];
        }
        
        public void Observe(double value)
        {
            lock (_lock)
            {
                _values.Add(value);
                _count++;
                _sum += value;
                
                if (value < _min) _min = value;
                if (value > _max) _max = value;
                
                for (int i = 0; i < _buckets.Length; i++)
                {
                    if (value <= _buckets[i])
                    {
                        _bucketCounts[i]++;
                        return;
                    }
                }
                _bucketCounts[_buckets.Length]++;
            }
        }
        
        public HistogramSnapshot GetSnapshot()
        {
            lock (_lock)
            {
                var values = _values.OrderBy(v => v).ToList();
                
                return new HistogramSnapshot
                {
                    Name = Name,
                    Count = _count,
                    Sum = _sum,
                    Min = _count > 0 ? _min : 0,
                    Max = _count > 0 ? _max : 0,
                    Mean = _count > 0 ? _sum / _count : 0,
                    Median = GetPercentile(values, 50),
                    P75 = GetPercentile(values, 75),
                    P90 = GetPercentile(values, 90),
                    P95 = GetPercentile(values, 95),
                    P99 = GetPercentile(values, 99)
                };
            }
        }
        
        private double GetPercentile(List<double> sortedValues, int percentile)
        {
            if (sortedValues.Count == 0) return 0;
            
            var index = (int)Math.Ceiling(percentile / 100.0 * sortedValues.Count) - 1;
            return sortedValues[Math.Max(0, Math.Min(index, sortedValues.Count - 1))];
        }
        
        public IDisposable StartTimer()
        {
            return new HistogramTimer(this);
        }
        
        private class HistogramTimer : IDisposable
        {
            private readonly Histogram _histogram;
            private readonly Stopwatch _stopwatch;
            
            public HistogramTimer(Histogram histogram)
            {
                _histogram = histogram;
                _stopwatch = Stopwatch.StartNew();
            }
            
            public void Dispose()
            {
                _stopwatch.Stop();
                _histogram.Observe(_stopwatch.ElapsedMilliseconds);
            }
        }
    }
    
    /// <summary>
    /// Histogram anlık görüntüsü.
    /// </summary>
    public class HistogramSnapshot
    {
        public string Name { get; set; }
        public long Count { get; set; }
        public double Sum { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Mean { get; set; }
        public double Median { get; set; }
        public double P75 { get; set; }
        public double P90 { get; set; }
        public double P95 { get; set; }
        public double P99 { get; set; }
        
        public override string ToString()
        {
            return $"{Name}: count={Count}, mean={Mean:F2}ms, p50={Median:F2}ms, p95={P95:F2}ms, p99={P99:F2}ms";
        }
    }
    
    #endregion

    #region Timer (high-level)
    
    /// <summary>
    /// Yüksek seviyeli zamanlayıcı.
    /// </summary>
    public class Timer
    {
        private readonly Histogram _histogram;
        private readonly Counter _counter;
        
        public string Name { get; }
        
        public Timer(string name, string description = null)
        {
            Name = name;
            _histogram = new Histogram($"{name}_duration", description);
            _counter = new Counter($"{name}_total", $"Total {name} operations");
        }
        
        public IDisposable Time()
        {
            _counter.Increment();
            return _histogram.StartTimer();
        }
        
        public T Time<T>(Func<T> operation)
        {
            using (Time())
            {
                return operation();
            }
        }
        
        public async Task<T> TimeAsync<T>(Func<Task<T>> operation)
        {
            using (Time())
            {
                return await operation();
            }
        }
        
        public HistogramSnapshot GetSnapshot() => _histogram.GetSnapshot();
        public long TotalOperations => _counter.Value;
    }
    
    #endregion

    #region Metrics Registry
    
    /// <summary>
    /// Global metrics registry.
    /// </summary>
    public static class MetricsRegistry
    {
        private static readonly ConcurrentDictionary<string, Counter> _counters 
            = new ConcurrentDictionary<string, Counter>();
        private static readonly ConcurrentDictionary<string, Gauge> _gauges 
            = new ConcurrentDictionary<string, Gauge>();
        private static readonly ConcurrentDictionary<string, Histogram> _histograms 
            = new ConcurrentDictionary<string, Histogram>();
        private static readonly ConcurrentDictionary<string, Timer> _timers 
            = new ConcurrentDictionary<string, Timer>();
        
        #region Counter
        
        public static Counter GetOrCreateCounter(string name, string description = null)
        {
            return _counters.GetOrAdd(name, n => new Counter(n, description));
        }
        
        public static Counter Counter(string name) => GetOrCreateCounter(name);
        
        #endregion

        #region Gauge
        
        public static Gauge GetOrCreateGauge(string name, string description = null)
        {
            return _gauges.GetOrAdd(name, n => new Gauge(n, description));
        }
        
        public static Gauge Gauge(string name) => GetOrCreateGauge(name);
        
        #endregion

        #region Histogram
        
        public static Histogram GetOrCreateHistogram(string name, string description = null, double[] buckets = null)
        {
            return _histograms.GetOrAdd(name, n => new Histogram(n, description, buckets));
        }
        
        public static Histogram Histogram(string name) => GetOrCreateHistogram(name);
        
        #endregion

        #region Timer
        
        public static Timer GetOrCreateTimer(string name, string description = null)
        {
            return _timers.GetOrAdd(name, n => new Timer(n, description));
        }
        
        public static Timer Timer(string name) => GetOrCreateTimer(name);
        
        #endregion

        #region Snapshot
        
        /// <summary>
        /// Tüm metrik değerlerini alır.
        /// </summary>
        public static MetricsSnapshot GetSnapshot()
        {
            return new MetricsSnapshot
            {
                Timestamp = DateTime.Now,
                Counters = _counters.ToDictionary(kv => kv.Key, kv => kv.Value.Value),
                Gauges = _gauges.ToDictionary(kv => kv.Key, kv => kv.Value.Value),
                Histograms = _histograms.ToDictionary(kv => kv.Key, kv => kv.Value.GetSnapshot()),
                Timers = _timers.ToDictionary(kv => kv.Key, kv => kv.Value.GetSnapshot())
            };
        }
        
        /// <summary>
        /// Tüm metrikleri sıfırlar.
        /// </summary>
        public static void Reset()
        {
            foreach (var counter in _counters.Values) counter.Reset();
            _histograms.Clear();
            _timers.Clear();
        }
        
        #endregion
    }
    
    /// <summary>
    /// Tüm metriklerin anlık görüntüsü.
    /// </summary>
    public class MetricsSnapshot
    {
        public DateTime Timestamp { get; set; }
        public Dictionary<string, long> Counters { get; set; }
        public Dictionary<string, double> Gauges { get; set; }
        public Dictionary<string, HistogramSnapshot> Histograms { get; set; }
        public Dictionary<string, HistogramSnapshot> Timers { get; set; }
        
        public string ToJson()
        {
            // Simple JSON serialization
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"timestamp\": \"{Timestamp:O}\",");
            
            sb.AppendLine("  \"counters\": {");
            sb.AppendLine(string.Join(",\n", Counters.Select(kv => $"    \"{kv.Key}\": {kv.Value}")));
            sb.AppendLine("  },");
            
            sb.AppendLine("  \"gauges\": {");
            sb.AppendLine(string.Join(",\n", Gauges.Select(kv => $"    \"{kv.Key}\": {kv.Value}")));
            sb.AppendLine("  }");
            
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
    
    #endregion

    #region Application Metrics
    
    /// <summary>
    /// Uygulama metrikleri.
    /// </summary>
    public static class ApplicationMetrics
    {
        // Rota hesaplama metrikleri
        public static Counter RotaHesaplamaBasari => MetricsRegistry.Counter("rota_hesaplama_basari");
        public static Counter RotaHesaplamaHata => MetricsRegistry.Counter("rota_hesaplama_hata");
        public static Histogram RotaHesaplamaSuresi => MetricsRegistry.Histogram("rota_hesaplama_suresi_ms");
        
        // Veri yükleme metrikleri
        public static Counter VeriYuklemeBasari => MetricsRegistry.Counter("veri_yukleme_basari");
        public static Counter VeriYuklemeHata => MetricsRegistry.Counter("veri_yukleme_hata");
        public static Histogram VeriYuklemeSuresi => MetricsRegistry.Histogram("veri_yukleme_suresi_ms");
        
        // Cache metrikleri
        public static Counter CacheHit => MetricsRegistry.Counter("cache_hit");
        public static Counter CacheMiss => MetricsRegistry.Counter("cache_miss");
        public static Gauge CacheSize => MetricsRegistry.Gauge("cache_size");
        
        // API metrikleri
        public static Counter ApiIstek => MetricsRegistry.Counter("api_istek_toplam");
        public static Counter ApiHata => MetricsRegistry.Counter("api_hata_toplam");
        public static Histogram ApiLatency => MetricsRegistry.Histogram("api_latency_ms");
        
        // UI metrikleri
        public static Counter UiTiklamaOlaylari => MetricsRegistry.Counter("ui_tiklama_olaylari");
        public static Gauge AktifFormSayisi => MetricsRegistry.Gauge("aktif_form_sayisi");
        
        // Bellek metrikleri
        public static Gauge BellekKullanimi => MetricsRegistry.Gauge("bellek_kullanimi_mb");
        public static Gauge GCBellegi => MetricsRegistry.Gauge("gc_bellek_mb");
        
        /// <summary>
        /// Rota hesaplama süresini ölçer.
        /// </summary>
        public static IDisposable MeasureRotaHesaplama()
        {
            return RotaHesaplamaSuresi.StartTimer();
        }
        
        /// <summary>
        /// Bellek metriklerini günceller.
        /// </summary>
        public static void UpdateMemoryMetrics()
        {
            BellekKullanimi.Set(Environment.WorkingSet / (1024.0 * 1024.0));
            GCBellegi.Set(GC.GetTotalMemory(false) / (1024.0 * 1024.0));
        }
        
        /// <summary>
        /// Periyodik olarak sistem metriklerini günceller.
        /// </summary>
        public static System.Threading.Timer StartPeriodicUpdates(int intervalSeconds = 30)
        {
            return new System.Threading.Timer(
                _ => UpdateMemoryMetrics(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(intervalSeconds));
        }
    }
    
    #endregion
}
