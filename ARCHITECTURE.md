# 🏗️ Mimari Dokümantasyon

## İçindekiler

1. [Genel Bakış](#genel-bakış)
2. [Katman Mimarisi](#katman-mimarisi)
3. [Dependency Injection](#dependency-injection)
4. [Servis Katmanı](#servis-katmanı)
5. [Core Altyapı](#core-altyapı)
6. [Tasarım Desenleri](#tasarım-desenleri)
7. [Best Practices](#best-practices)

---

## Genel Bakış

Proje, **Clean Architecture** prensipleri ve **SOLID** ilkeleri temelinde tasarlanmıştır. Amaç:

- ✅ Test edilebilir kod
- ✅ Gevşek bağlı (loosely coupled) bileşenler
- ✅ Tek sorumluluk prensibi
- ✅ Kolay bakım ve genişletilebilirlik

---

## Katman Mimarisi

### 1. Presentation Layer (UI)

```
UI/
├── Form1.cs              # Ana form
├── Form1.Designer.cs     # Designer kodu
└── Form1.resx            # Resources
```

**Sorumluluklar:**
- Kullanıcı etkileşimi
- Görsel geri bildirim
- Event handling
- Servis çağrıları

**Önemli Kurallar:**
- İş mantığı içermemeli
- Doğrudan veri erişimi yapmamalı
- Servisler aracılığıyla çalışmalı

### 2. Service Layer

```
Services/
├── Interfaces/
│   ├── IDurakServisi.cs
│   ├── IRotaServisi.cs
│   └── IHaritaServisi.cs
└── Implementations/
    ├── DurakServisiImpl.cs
    ├── RotaServisiImpl.cs
    └── HaritaServisiImpl.cs
```

**Sorumluluklar:**
- İş mantığı
- Veri dönüşümleri
- Koordinasyon

### 3. Core Layer

```
Core/
├── DependencyInjection/  # IoC container
├── Validation/           # Doğrulama
├── Logging/              # Loglama
├── Caching/              # Cache
├── Security/             # Güvenlik
└── Extensions/           # Helpers
```

**Sorumluluklar:**
- Cross-cutting concerns
- Altyapı servisleri
- Utilities

### 4. Data Layer

```
Data/
└── veriseti.json         # Veri kaynağı

Models/
├── Durak.cs
├── Rota.cs
└── ...
```

---

## Dependency Injection

### ServiceContainer

Custom IoC container implementasyonu:

```csharp
// Servis kaydı
ServiceContainer.Register<IDurakServisi, DurakServisiImpl>(ServiceLifetime.Singleton);

// Servis çözümleme
var durakServisi = ServiceContainer.Resolve<IDurakServisi>();
```

### Lifetime Türleri

| Lifetime | Açıklama |
|----------|----------|
| Singleton | Uygulama boyunca tek instance |
| Transient | Her çağrıda yeni instance |

### ServiceBootstrapper

Tüm servislerin tek noktadan kaydı:

```csharp
public static void Initialize()
{
    // Core services
    ServiceContainer.Register<ICacheService, MemoryCacheService>();
    
    // Business services
    ServiceContainer.Register<IDurakServisi, DurakServisiImpl>();
    ServiceContainer.Register<IRotaServisi, RotaServisiImpl>();
}
```

---

## Servis Katmanı

### IDurakServisi

Durak yönetimi servisi:

```csharp
public interface IDurakServisi
{
    Task<Result<List<Durak>>> DuraklariGetirAsync();
    Task<Result<Durak>> DurakGetirAsync(string id);
    Task<Result<Dictionary<string, Durak>>> DurakDictGetirAsync();
}
```

### IRotaServisi

Rota hesaplama servisi:

```csharp
public interface IRotaServisi
{
    Task<Result<List<Rota>>> TumRotalariHesaplaAsync(
        string baslangicId, 
        string hedefId, 
        string yolcuTipi);
}
```

### Result<T> Pattern

Fonksiyonel hata yönetimi:

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }
    
    public static Result<T> Success(T value);
    public static Result<T> Failure(string error);
}
```

**Kullanım:**

```csharp
var result = await durakServisi.DuraklariGetirAsync();

result.Match(
    onSuccess: duraklar => DisplayDuraklar(duraklar),
    onFailure: error => ShowError(error));
```

---

## Core Altyapı

### Logging

Merkezi loglama:

```csharp
Logger.Info("Bilgi mesajı");
Logger.Warning("Uyarı mesajı");
Logger.Error("Hata mesajı", exception);
```

### Validation

Input doğrulama:

```csharp
ValidationHelper.IsNullOrEmpty(value);
ValidationHelper.IsValidCoordinate(lat, lon);
ValidationHelper.IsValidDurakId(id);
```

### Caching

Memory cache:

```csharp
var cache = ServiceContainer.Resolve<ICacheService>();

var duraklar = await cache.GetOrCreateAsync(
    "duraklar:list",
    () => LoadDuraklarAsync(),
    TimeSpan.FromMinutes(30));
```

### Security

Input sanitization:

```csharp
var safeId = InputSanitizer.SanitizeId(userInput);
var safeCoords = InputSanitizer.SanitizeCoordinates(lat, lon);
```

---

## Tasarım Desenleri

### 1. Repository Pattern

Veri erişimini soyutlar:

```csharp
public interface IDurakRepository
{
    Task<List<Durak>> GetAllAsync();
    Task<Durak> GetByIdAsync(string id);
}
```

### 2. Factory Pattern

Nesne oluşturmayı soyutlar:

```csharp
public static class AracFactory
{
    public static IArac Create(string tip) => tip switch
    {
        "Otobus" => new Otobus(),
        "Tramvay" => new Tramvay(),
        _ => throw new ArgumentException()
    };
}
```

### 3. Strategy Pattern

Algoritma seçimini soyutlar:

```csharp
public interface IRotaHesaplayici
{
    List<Rota> Hesapla(string baslangic, string hedef);
}
```

### 4. Singleton Pattern

Tek instance garantisi:

```csharp
ServiceContainer.Register<ILogger, Logger>(ServiceLifetime.Singleton);
```

---

## Best Practices

### Kod Organizasyonu

```csharp
public class MyService
{
    #region Fields
    private readonly IDependency _dependency;
    #endregion
    
    #region Constructor
    public MyService(IDependency dependency)
    {
        _dependency = dependency;
    }
    #endregion
    
    #region Public Methods
    public async Task<Result<T>> DoWorkAsync() { }
    #endregion
    
    #region Private Methods
    private void HelperMethod() { }
    #endregion
}
```

### Naming Conventions

| Tür | Örnek |
|-----|-------|
| Private field | `_durakServisi` |
| Constant | `VARSAYILAN_DEGER` |
| Interface | `IDurakServisi` |
| Async method | `GetDuraklarAsync` |

### Async/Await

```csharp
// ✅ Doğru
public async Task<List<Durak>> GetDuraklarAsync()
{
    return await _repository.GetAllAsync();
}

// ❌ Yanlış
public Task<List<Durak>> GetDuraklar()
{
    return _repository.GetAllAsync(); // async/await yok
}
```

### Error Handling

```csharp
// ✅ Result pattern kullan
public async Task<Result<T>> DoWorkAsync()
{
    try
    {
        var result = await DoActualWork();
        return Result<T>.Success(result);
    }
    catch (Exception ex)
    {
        Logger.Error("İşlem başarısız", ex);
        return Result<T>.Failure(ex.Message);
    }
}
```

---

## Akış Diyagramları

### Rota Hesaplama Akışı

```
┌──────────┐    ┌───────────────┐    ┌─────────────────┐
│  Form1   │───▶│ IRotaServisi  │───▶│ RotaHesaplayici │
└──────────┘    └───────────────┘    └─────────────────┘
     │                  │                     │
     │                  │                     ▼
     │                  │              ┌─────────────┐
     │                  │              │  Dijkstra   │
     │                  │              └─────────────┘
     │                  │                     │
     ◀──────────────────┴─────────────────────┘
     │
     ▼
┌─────────────────┐
│ Haritada Göster │
└─────────────────┘
```

---

## Gelecek İyileştirmeler

- [ ] Event-driven architecture
- [ ] CQRS pattern
- [ ] Message queue entegrasyonu
- [ ] Microservices ayrımı
- [ ] GraphQL API
