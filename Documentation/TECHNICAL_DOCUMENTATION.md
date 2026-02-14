# Prolab 4 - Toplu Taşıma Sistemi

## Teknik Dokümantasyon

### İçindekiler

1. [Genel Bakış](#genel-bakış)
2. [Mimari Tasarım](#mimari-tasarım)
3. [Core Modülleri](#core-modülleri)
4. [API Referansı](#api-referansı)
5. [Kurulum ve Yapılandırma](#kurulum-ve-yapılandırma)
6. [Performans Kılavuzu](#performans-kılavuzu)
7. [Güvenlik](#güvenlik)
8. [Hata Yönetimi](#hata-yönetimi)
9. [Test Stratejisi](#test-stratejisi)

---

## Genel Bakış

### Proje Hakkında

Bu proje, şehir içi toplu taşıma sistemi için rota hesaplama ve optimizasyon çözümü sunar. Dijkstra algoritması tabanlı en kısa yol hesaplaması, çoklu araç tipi desteği ve gerçek zamanlı maliyet optimizasyonu sağlar.

### Özellikler

- **Rota Hesaplama**: Dijkstra algoritması ile en kısa/en ucuz yol
- **Çoklu Araç Desteği**: Otobüs, tramvay, taksi, yürüme
- **Yolcu Tipleri**: Öğrenci, yaşlı, standart
- **Görsel Harita**: GMap.NET entegrasyonu
- **Önbellekleme**: Multi-layer cache sistemi
- **Metrik Toplama**: Performans izleme

### Teknoloji Yığını

| Teknoloji | Sürüm | Amaç |
|-----------|-------|------|
| .NET | 8.0 | Runtime |
| Windows Forms | - | UI Framework |
| GMap.NET | 2.1+ | Harita Görselleştirme |
| System.Text.Json | - | JSON İşleme |

---

## Mimari Tasarım

### Katmanlı Mimari

```
┌─────────────────────────────────────────────────────┐
│                    UI Layer                          │
│  (Form1.cs, ModernComponents, NotificationSystem)   │
├─────────────────────────────────────────────────────┤
│                 Service Layer                        │
│  (RotaHesaplayici, UcretHesaplayici, DurakService)  │
├─────────────────────────────────────────────────────┤
│                  Core Layer                          │
│  (Caching, Logging, Resilience, Security, Metrics)  │
├─────────────────────────────────────────────────────┤
│                  Model Layer                         │
│  (Durak, Arac, Yolcu, Rota, DurakBaglantisi)        │
├─────────────────────────────────────────────────────┤
│               Infrastructure Layer                   │
│  (File I/O, JSON, Configuration)                    │
└─────────────────────────────────────────────────────┘
```

### Dependency Injection

```csharp
// ServiceContainer kullanımı
var container = new ServiceContainer();

// Singleton servisleri
container.RegisterSingleton<ICacheService, AdvancedMemoryCache>();
container.RegisterSingleton<DurakService>();

// Transient servisleri
container.RegisterTransient<RotaHesaplayici>();

// Çözümleme
var calculator = container.Resolve<RotaHesaplayici>();
```

### Result Pattern

```csharp
// İşlem sonucu döndürme
public Result<Rota> HesaplaRota(string baslangic, string hedef)
{
    if (string.IsNullOrEmpty(baslangic))
        return Result<Rota>.Failure("Başlangıç durağı gerekli");
        
    // İşlem...
    return Result<Rota>.Success(rota);
}

// Kullanım
var result = calculator.HesaplaRota("A", "B");
if (result.IsSuccess)
{
    var rota = result.Value;
}
else
{
    Logger.Error(result.Error);
}
```

---

## Core Modülleri

### 1. Caching Sistemi

#### AdvancedMemoryCache

Gelişmiş bellek önbelleği - LRU eviction, bağımlılıklar ve metrikler.

```csharp
var cache = new AdvancedMemoryCache(maxSizeMB: 100);

// Temel kullanım
cache.Set("key", value, TimeSpan.FromMinutes(30));
var hasValue = cache.TryGet<MyType>("key", out var cached);

// Policy ile kullanım
cache.Set("key", value, new CachePolicy
{
    AbsoluteExpiration = TimeSpan.FromHours(1),
    Priority = CachePriority.High,
    Dependencies = new[] { "parent-key" }
});

// GetOrCreate pattern
var data = await cache.GetOrCreateAsync("key", async () =>
{
    return await FetchDataAsync();
});
```

#### MultiLayerCache

L1 (local) + L2 (shared) cache yapısı.

```csharp
var l1 = new AdvancedMemoryCache(maxSizeMB: 50);  // Hızlı, küçük
var l2 = new AdvancedMemoryCache(maxSizeMB: 500); // Yavaş, büyük
var cache = new MultiLayerCache(l1, l2);

// L1'de yoksa L2'ye bakar, orada da yoksa factory çalışır
var data = await cache.GetOrCreateAsync("key", FetchDataAsync);
```

#### Cache Key Builder

```csharp
// Tutarlı key oluşturma
var key = CacheKeyBuilder.ForRoute("durak1", "durak2", "ogrenci");
// Sonuç: "route:durak1:durak2:ogrenci"

var graphKey = CacheKeyBuilder.ForGraph("v2");
// Sonuç: "graph:v2"
```

### 2. Resilience Patterns

#### Circuit Breaker

```csharp
var breaker = new CircuitBreaker(
    failureThreshold: 5,
    successThreshold: 2,
    openDuration: TimeSpan.FromSeconds(30)
);

try
{
    var result = await breaker.ExecuteAsync(async () =>
    {
        return await RiskyOperationAsync();
    });
}
catch (CircuitBreakerOpenException)
{
    // Devre açık - fallback kullan
}
```

#### Retry Policy

```csharp
var policy = new RetryPolicyBuilder()
    .WithMaxRetries(3)
    .WithExponentialBackoff(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(5))
    .WithJitter(0.2)
    .OnRetry(attempt => Logger.Warning($"Retry {attempt.RetryCount}"))
    .Build();

var result = await policy.ExecuteAsync(async () =>
{
    return await UnstableOperationAsync();
});
```

#### Combined Resilience

```csharp
var policy = new ResiliencePolicyBuilder()
    .WithCircuitBreaker(failureThreshold: 3)
    .WithRetry(maxRetries: 2)
    .WithBulkhead(maxConcurrency: 10)
    .WithTimeout(TimeSpan.FromSeconds(30))
    .Build();

var result = await policy.ExecuteAsync(OperationAsync);
```

### 3. Logging Sistemi

```csharp
// Temel loglama
Logger.Info("Uygulama başladı");
Logger.Warning("Dikkat edilecek durum");
Logger.Error("Hata oluştu", exception);

// Korelasyon ID ile
Logger.SetCorrelationId(Guid.NewGuid().ToString());
Logger.Info("İşlem başladı"); // Korelasyon ID otomatik eklenir

// Performance scope
using (Logger.BeginPerformanceScope("HeavyOperation"))
{
    // ... işlem
} // Süre otomatik loglanır

// Gelişmiş logger
var logger = new AdvancedLogger();
logger.AddSink(new FileSink("logs/app.log"));
logger.AddSink(new ConsoleSink());
logger.Info("Mesaj");
```

### 4. Metrics Sistemi

```csharp
// Counter
ApplicationMetrics.RotaHesaplama.Increment();

// Gauge
ApplicationMetrics.AktifBaglanti.Increment();
ApplicationMetrics.AktifBaglanti.Decrement();

// Histogram
ApplicationMetrics.YuklemeSuresi.Record(duration.TotalMilliseconds);

// Timer
using (ApplicationMetrics.UcretHesaplamaSuresi.Time())
{
    // İşlem
}

// Snapshot
var snapshot = MetricsRegistry.GetSnapshot();
Console.WriteLine($"Total requests: {snapshot.Counters["rota_hesaplama"]}");
```

### 5. Security

#### Input Validation

```csharp
var result = InputValidator.Validate(durakId,
    new RequiredRule<string>("Durak ID"),
    new StringLengthRule(1, 50, "Durak ID"),
    new CustomRule<string>(InputValidator.IsValidDurakId, "Geçersiz durak ID formatı")
);

if (!result.IsValid)
{
    Logger.Warning($"Validation failed: {string.Join(", ", result.Errors)}");
}
```

#### Rate Limiting

```csharp
var limiter = new RateLimiter(maxRequests: 100, windowSize: TimeSpan.FromMinutes(1));

if (!limiter.IsAllowed(userId))
{
    AuditLogger.Instance.LogSecurityEvent(SecurityEvent.RateLimitExceeded, userId);
    throw new RateLimitException();
}
```

#### Encryption

```csharp
// Şifreleme
var encrypted = EncryptionHelper.Encrypt("sensitive data");
var decrypted = EncryptionHelper.Decrypt(encrypted);

// Password hashing
var hash = EncryptionHelper.HashPassword("password123", out var salt);
var isValid = EncryptionHelper.VerifyPassword("password123", hash, salt);
```

### 6. Performance Optimization

#### Object Pooling

```csharp
// StringBuilder pool
using (var pooled = StringBuilderPool.Rent())
{
    pooled.Value.Append("Hello");
    pooled.Value.Append(" World");
    var result = pooled.Value.ToString();
} // Otomatik geri verilir

// Generic pool
var pool = new ObjectPool<ExpensiveObject>(() => new ExpensiveObject(), 10);
using (var obj = pool.Rent())
{
    obj.Value.DoWork();
}
```

#### Batch Processing

```csharp
var processor = new BatchProcessor<LogEntry>(
    batchSize: 100,
    maxWait: TimeSpan.FromSeconds(5),
    async batch =>
    {
        await BulkInsertAsync(batch);
    });

processor.Add(new LogEntry { ... });
// Batch dolduğunda veya süre geçtiğinde otomatik işlenir
```

#### Parallel Processing

```csharp
// Throttled parallel işlem
await ParallelHelper.ForEachAsync(
    items,
    maxDegreeOfParallelism: 4,
    async item =>
    {
        await ProcessAsync(item);
    });

// Chunk processing
await ParallelHelper.ProcessInChunksAsync(
    largeList,
    chunkSize: 100,
    async chunk =>
    {
        await BulkProcessAsync(chunk);
    });
```

---

## API Referansı

### RotaHesaplayici

```csharp
public class RotaHesaplayici
{
    /// <summary>
    /// İki durak arasındaki en kısa rotayı hesaplar.
    /// </summary>
    /// <param name="baslangicId">Başlangıç durağının ID'si</param>
    /// <param name="hedefId">Hedef durağın ID'si</param>
    /// <param name="yolcuTipi">Yolcu tipi (ogrenci, yasli, normal)</param>
    /// <returns>Hesaplanan rota veya hata</returns>
    public Result<Rota> HesaplaEnKisaRota(string baslangicId, string hedefId, YolcuTipi yolcuTipi);
    
    /// <summary>
    /// En ucuz rotayı hesaplar.
    /// </summary>
    public Result<Rota> HesaplaEnUcuzRota(string baslangicId, string hedefId, YolcuTipi yolcuTipi);
}
```

### DurakService

```csharp
public class DurakService
{
    /// <summary>
    /// Tüm durakları getirir.
    /// </summary>
    public Result<IReadOnlyList<Durak>> GetAllDuraklar();
    
    /// <summary>
    /// ID ile durak getirir.
    /// </summary>
    public Result<Durak> GetDurakById(string id);
    
    /// <summary>
    /// Koordinata en yakın durağı bulur.
    /// </summary>
    public Result<Durak> GetNearestDurak(double latitude, double longitude);
}
```

### UcretHesaplayici

```csharp
public class UcretHesaplayici
{
    /// <summary>
    /// Rota için toplam ücreti hesaplar.
    /// </summary>
    public Result<decimal> HesaplaToplamUcret(Rota rota, YolcuTipi yolcuTipi);
    
    /// <summary>
    /// Araç tipi ve mesafeye göre ücret hesaplar.
    /// </summary>
    public decimal HesaplaAracUcreti(AracTipi aracTipi, double mesafe, YolcuTipi yolcuTipi);
}
```

---

## Kurulum ve Yapılandırma

### Gereksinimler

- .NET 8.0 SDK
- Windows 10/11
- Visual Studio 2022 (önerilen)

### Kurulum

```bash
# Projeyi klonla
git clone <repo-url>
cd a-little-transportation-system-2

# Bağımlılıkları yükle
dotnet restore

# Çalıştır
dotnet run
```

### Yapılandırma

Uygulama yapılandırması `appsettings.json` dosyasında:

```json
{
  "Logging": {
    "Level": "Information",
    "FilePath": "logs/app.log",
    "MaxFileSizeMB": 10
  },
  "Cache": {
    "MaxSizeMB": 100,
    "DefaultExpirationMinutes": 30,
    "CleanupIntervalSeconds": 60
  },
  "Resilience": {
    "CircuitBreaker": {
      "FailureThreshold": 5,
      "OpenDurationSeconds": 30
    },
    "Retry": {
      "MaxRetries": 3,
      "InitialDelayMs": 100
    }
  }
}
```

---

## Performans Kılavuzu

### Önerilen Optimizasyonlar

1. **Cache Kullanımı**
   - Sık erişilen verileri cache'leyin
   - Uygun TTL değerleri belirleyin
   - Cache warm-up stratejisi uygulayın

2. **Bellek Yönetimi**
   - Object pooling kullanın
   - Büyük koleksiyonlar için capacity belirleyin
   - IDisposable'ları düzgün dispose edin

3. **Paralel İşlem**
   - CPU-bound işlemler için Parallel.ForEach
   - I/O-bound işlemler için async/await
   - Uygun concurrency limitleri

### Performans Metrikleri

| Metrik | Hedef | Ölçüm Yöntemi |
|--------|-------|---------------|
| Rota hesaplama | < 100ms | Timer metric |
| UI yanıt süresi | < 16ms | Frame time |
| Bellek kullanımı | < 500MB | Memory gauge |
| Cache hit ratio | > 80% | Cache statistics |

---

## Güvenlik

### Güvenlik Kontrol Listesi

- [x] Input validation
- [x] Rate limiting
- [x] Audit logging
- [x] Secure configuration
- [x] Path traversal koruması
- [x] XSS koruması (display için)

### Audit Log

Tüm güvenlik olayları `AuditLogger` ile kaydedilir:

```csharp
AuditLogger.Instance.LogSecurityEvent(
    SecurityEvent.LoginSuccess,
    $"User logged in",
    userId: "user123"
);
```

---

## Hata Yönetimi

### Exception Hierarchy

```
Exception
├── TransportException (base)
│   ├── DurakNotFoundException
│   ├── RotaNotFoundException
│   ├── InvalidRouteException
│   └── CalculationException
├── DataException
│   ├── DataLoadException
│   ├── DataParseException
│   └── DataValidationException
└── InfrastructureException
    ├── CacheException
    ├── FileAccessException
    └── ConfigurationException
```

### Hata İşleme Pattern'i

```csharp
try
{
    var result = await service.DoWorkAsync();
    result.Match(
        success => HandleSuccess(success),
        failure => HandleFailure(failure)
    );
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    GlobalExceptionHandler.Handle(ex);
}
```

---

## Test Stratejisi

### Test Framework Kullanımı

```csharp
[TestClass]
public class RotaHesaplayiciTests
{
    private RotaHesaplayici _calculator;
    
    [TestSetup]
    public void Setup()
    {
        _calculator = new RotaHesaplayici();
    }
    
    [TestMethod]
    public void HesaplaRota_ValidInput_ReturnsSuccess()
    {
        var result = _calculator.HesaplaEnKisaRota("A", "B", YolcuTipi.Normal);
        
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
    }
    
    [TestCase("", "B")]
    [TestCase("A", "")]
    [TestCase(null, "B")]
    public void HesaplaRota_InvalidInput_ReturnsFailure(string baslangic, string hedef)
    {
        var result = _calculator.HesaplaEnKisaRota(baslangic, hedef, YolcuTipi.Normal);
        
        Assert.IsFalse(result.IsSuccess);
    }
}
```

### Test Çalıştırma

```csharp
var runner = new TestRunner()
    .AddTestClassesFromAssembly(typeof(Program).Assembly)
    .FilterByCategory("Unit")
    .StopOnFirstFailure();

var summary = await runner.RunAsync();
summary.PrintSummary();
```

---

## Değişiklik Günlüğü

### v2.0.0 (Enterprise Edition)

- ✅ Gelişmiş hata yönetimi sistemi
- ✅ Circuit breaker, retry, bulkhead pattern'leri
- ✅ Multi-layer caching
- ✅ Performance monitoring
- ✅ Modern UI bileşenleri
- ✅ Toast notification sistemi
- ✅ Object pooling
- ✅ Audit logging
- ✅ Rate limiting
- ✅ Kapsamlı test framework

### v1.0.0 (Initial)

- Temel rota hesaplama
- Dijkstra algoritması
- Basit UI

---

## Lisans

Bu proje eğitim amaçlı geliştirilmiştir.

---

*Son Güncelleme: 2024*
