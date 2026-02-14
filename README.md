# 🚌 Toplu Taşıma Rota Hesaplama Sistemi

## 📋 Proje Hakkında

Bu proje, **Kocaeli şehri için akıllı toplu taşıma rota hesaplama sistemi**dir. Kullanıcıların başlangıç ve hedef noktaları arasında en uygun rotayı bulmalarını sağlar.

### ✨ Özellikler

- 🗺️ **İnteraktif Harita**: GMap.NET ile zengin harita deneyimi
- 🔍 **Akıllı Rota Hesaplama**: Dijkstra algoritması ile en kısa yol
- 🚌 **Çoklu Ulaşım Modu**: Otobüs, tramvay, taksi ve yürüme seçenekleri
- 💰 **Ücret Hesaplama**: Yolcu tipine göre dinamik ücret hesabı
- 👨‍🎓 **İndirimli Tarifeler**: Öğrenci ve yaşlı indirimleri
- 📍 **Konum Seçimi**: Haritadan veya durak listesinden seçim

---

## 🏗️ Mimari

Proje, modern yazılım mimari prensipleri ile tasarlanmıştır:

```
┌─────────────────────────────────────────────────┐
│                    UI Layer                      │
│                  (Windows Forms)                 │
├─────────────────────────────────────────────────┤
│                 Service Layer                    │
│   (IDurakServisi, IRotaServisi, IHaritaServisi) │
├─────────────────────────────────────────────────┤
│                  Core Layer                      │
│   (DI, Validation, Logging, Caching, Security)  │
├─────────────────────────────────────────────────┤
│                  Data Layer                      │
│            (JSON Repository, Models)             │
└─────────────────────────────────────────────────┘
```

### 📁 Klasör Yapısı

```
Prolab_4/
├── Core/                      # Çekirdek altyapı
│   ├── AsyncOperations/       # Async işlem yardımcıları
│   ├── Caching/               # Cache servisleri
│   ├── Constants/             # Sabit değerler
│   ├── DependencyInjection/   # DI container
│   ├── Exceptions/            # Özel exception'lar
│   ├── Extensions/            # Extension metodlar
│   ├── Logging/               # Loglama servisi
│   ├── Performance/           # Performans araçları
│   ├── Security/              # Güvenlik kontrolleri
│   ├── UI/                    # UI yardımcıları
│   └── Validation/            # Doğrulama servisleri
├── Data/                      # Veri dosyaları
│   └── veriseti.json          # Durak ve bağlantı verileri
├── Models/                    # Domain modelleri
├── Services/                  # İş mantığı servisleri
│   ├── Interfaces/            # Servis arayüzleri
│   └── Implementations/       # Servis implementasyonları
├── UI/                        # Form ve UI kodları
└── Tests/                     # Unit testler
```

---

## 🚀 Kurulum

### Gereksinimler

- **.NET 8.0 SDK** veya üzeri
- **Visual Studio 2022** veya VS Code
- **Windows 10/11**

### Adımlar

1. **Projeyi klonlayın:**
   ```bash
   git clone https://github.com/your-repo/transportation-system.git
   cd transportation-system
   ```

2. **Bağımlılıkları yükleyin:**
   ```bash
   dotnet restore
   ```

3. **Projeyi derleyin:**
   ```bash
   dotnet build
   ```

4. **Uygulamayı çalıştırın:**
   ```bash
   dotnet run
   ```

---

## 📖 Kullanım

### Rota Hesaplama

1. **Başlangıç noktası seçin:**
   - Haritada sağ tık → "Başlangıç Olarak Seç"
   - veya Durak listesinden seçin

2. **Hedef noktası seçin:**
   - Haritada sağ tık → "Hedef Olarak Seç"
   - veya Durak listesinden seçin

3. **Yolcu tipini seçin:**
   - Genel, Öğrenci veya Yaşlı

4. **"Rota Hesapla" butonuna tıklayın**

### Ücret Bilgileri

| Araç Tipi | Genel | Öğrenci | Yaşlı |
|-----------|-------|---------|-------|
| Otobüs    | 10 ₺  | 5 ₺     | 7 ₺   |
| Tramvay   | 7.5 ₺ | 3.75 ₺  | 5.25 ₺|
| Taksi     | 50₺/km| 50₺/km  | 50₺/km|
| Yürüme    | Ücretsiz | Ücretsiz | Ücretsiz |

---

## 🧪 Test

### Unit Testleri Çalıştırma

```bash
cd Tests
dotnet test
```

### Test Kategorileri

- **Core Tests**: Validation, Result, Extensions
- **Service Tests**: UcretHesaplayici, RotaHesaplayici
- **Integration Tests**: End-to-end senaryolar

---

## 🔧 Konfigürasyon

### appsettings.json

```json
{
  "HaritaAyarlari": {
    "VarsayilanEnlem": 40.7669,
    "VarsayilanBoylam": 29.9169,
    "VarsayilanZoom": 13
  },
  "LogAyarlari": {
    "MinimumSeviye": "Info",
    "DosyaYolu": "Logs/app.log"
  },
  "CacheAyarlari": {
    "VarsayilanSure": 30
  }
}
```

---

## 🤝 Katkıda Bulunma

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Commit yapın (`git commit -m 'Add amazing feature'`)
4. Push yapın (`git push origin feature/amazing-feature`)
5. Pull Request açın

### Kod Standartları

- ✅ C# naming conventions
- ✅ XML documentation comments
- ✅ Unit test coverage
- ✅ SOLID prensipleri
- ✅ Clean Code practices

---

## 📝 API Referansı

### IDurakServisi

```csharp
// Tüm durakları getirir
Task<Result<List<Durak>>> DuraklariGetirAsync();

// ID ile durak getirir
Task<Result<Durak>> DurakGetirAsync(string id);
```

### IRotaServisi

```csharp
// İki nokta arası rota hesaplar
Task<Result<List<Rota>>> TumRotalariHesaplaAsync(
    string baslangicId, 
    string hedefId, 
    string yolcuTipi);
```

### IHaritaServisi

```csharp
// Haritayı başlatır
Task<bool> HaritaBaslatAsync(GMapControl control);

// Durakları haritaya ekler
Task DuraklariEkleAsync(List<Durak> duraklar);
```

---

## 📄 Lisans

Bu proje [MIT Lisansı](LICENSE) altında lisanslanmıştır.

---

## 🙏 Teşekkürler

- [GMap.NET](https://github.com/judero01col/GMap.NET) - Harita kütüphanesi
- [Kocaeli Büyükşehir Belediyesi](https://www.kocaeli.bel.tr/) - Veri desteği

---

<p align="center">
  Made with ❤️ for Kocaeli
</p>
