using Prolab_4.Core;
using Prolab_4.Core.Configuration;
using Prolab_4.Core.Logging;
using Prolab_4.Models;
using Prolab_4.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Prolab_4.Services
{
    /// <summary>
    /// IDurakServisi interface'inin implementasyonu.
    /// JSON dosyasından durak verilerini okur ve yönetir.
    /// 
    /// Özellikler:
    /// - Önbellekleme (5 dakika TTL)
    /// - Asenkron operasyonlar
    /// - Haversine mesafe hesaplama
    /// - Kullanıcı node oluşturma
    /// </summary>
    public class DurakServisiImpl : IDurakServisi
    {
        #region Private Fields
        
        // JSON dosya yolu
        private readonly string _jsonPath;
        
        // Önbellek
        private List<Durak> _cachedDuraklar;
        private List<Durak> _cachedGrafDuraklar;
        private DateTime _lastCacheTime;
        private readonly TimeSpan _cacheTTL = TimeSpan.FromMinutes(5);
        
        // Thread safety
        private readonly object _cacheLock = new object();
        
        #endregion

        #region Constructor
        
        /// <summary>
        /// Varsayılan constructor - config'den dosya yolunu alır
        /// </summary>
        public DurakServisiImpl()
        {
            _jsonPath = Constants.VERISETI_DOSYA_YOLU;
            Logger.Debug($"DurakServisi oluşturuldu. Veri yolu: {_jsonPath}");
        }
        
        /// <summary>
        /// Özel dosya yolu ile constructor (test için)
        /// </summary>
        /// <param name="jsonPath">JSON dosya yolu</param>
        public DurakServisiImpl(string jsonPath)
        {
            _jsonPath = jsonPath ?? throw new ArgumentNullException(nameof(jsonPath));
            Logger.Debug($"DurakServisi oluşturuldu (özel yol). Veri yolu: {_jsonPath}");
        }
        
        #endregion

        #region Senkron Metodlar
        
        /// <inheritdoc />
        public List<Durak> DuraklariGetir()
        {
            try
            {
                // Önbellek kontrolü
                if (OnbellekGecerliMi() && _cachedDuraklar != null)
                {
                    Logger.Debug("Duraklar önbellekten döndürülüyor.");
                    return new List<Durak>(_cachedDuraklar);
                }
                
                // Dosyadan oku
                var durakListesi = DosyadanDuraklariOku(grafOlarakMi: false);
                
                // Önbelleğe al
                lock (_cacheLock)
                {
                    _cachedDuraklar = durakListesi;
                    _lastCacheTime = DateTime.Now;
                }
                
                Logger.Info($"{durakListesi.Count} durak yüklendi.");
                return new List<Durak>(durakListesi);
            }
            catch (Exception ex)
            {
                Logger.Error("Duraklar yüklenirken hata oluştu.", ex);
                throw;
            }
        }
        
        /// <inheritdoc />
        public List<Durak> DuraklariGrafOlarakGetir()
        {
            try
            {
                // Önbellek kontrolü
                if (OnbellekGecerliMi() && _cachedGrafDuraklar != null)
                {
                    Logger.Debug("Graf duraklar önbellekten döndürülüyor.");
                    // Deep copy yaparak döndür (bağlantılar değiştirilebilir olduğu için)
                    return DeepCopyDuraklar(_cachedGrafDuraklar);
                }
                
                // Dosyadan oku (bağlantılar dahil)
                var durakListesi = DosyadanDuraklariOku(grafOlarakMi: true);
                
                // Önbelleğe al
                lock (_cacheLock)
                {
                    _cachedGrafDuraklar = durakListesi;
                    _lastCacheTime = DateTime.Now;
                }
                
                Logger.Info($"{durakListesi.Count} durak (graf) yüklendi.");
                return DeepCopyDuraklar(durakListesi);
            }
            catch (Exception ex)
            {
                Logger.Error("Graf duraklar yüklenirken hata oluştu.", ex);
                throw;
            }
        }
        
        /// <inheritdoc />
        public Durak DurakGetir(string durakId)
        {
            if (string.IsNullOrWhiteSpace(durakId))
            {
                Logger.Warning("DurakGetir: Boş durakId.");
                return null;
            }
            
            var duraklar = DuraklariGetir();
            return duraklar.FirstOrDefault(d => d.Id == durakId);
        }
        
        #endregion

        #region Asenkron Metodlar
        
        /// <inheritdoc />
        public async Task<Result<List<Durak>>> DuraklariGetirAsync()
        {
            try
            {
                // CPU-bound işlemi background thread'e taşı
                var duraklar = await Task.Run(() => DuraklariGetir());
                return Result<List<Durak>>.Success(duraklar);
            }
            catch (FileNotFoundException ex)
            {
                Logger.Error("Veri dosyası bulunamadı.", ex);
                return Result<List<Durak>>.Failure($"Veri dosyası bulunamadı: {_jsonPath}", "FILE_NOT_FOUND");
            }
            catch (JsonException ex)
            {
                Logger.Error("JSON parse hatası.", ex);
                return Result<List<Durak>>.Failure("Veri dosyası okunamadı: JSON formatı hatalı.", "JSON_PARSE_ERROR");
            }
            catch (Exception ex)
            {
                Logger.Error("Beklenmeyen hata.", ex);
                return Result<List<Durak>>.Failure($"Beklenmeyen hata: {ex.Message}", "UNEXPECTED_ERROR");
            }
        }
        
        /// <inheritdoc />
        public async Task<Result<List<Durak>>> DuraklariGrafOlarakGetirAsync()
        {
            try
            {
                var duraklar = await Task.Run(() => DuraklariGrafOlarakGetir());
                return Result<List<Durak>>.Success(duraklar);
            }
            catch (Exception ex)
            {
                Logger.Error("Graf duraklar yüklenirken hata.", ex);
                return Result<List<Durak>>.Failure($"Graf duraklar yüklenemedi: {ex.Message}", "LOAD_ERROR");
            }
        }
        
        /// <inheritdoc />
        public async Task<Result<Durak>> DurakGetirAsync(string durakId)
        {
            if (string.IsNullOrWhiteSpace(durakId))
            {
                return Result<Durak>.Failure("Durak ID boş olamaz.", "INVALID_ID");
            }
            
            var sonuc = await DuraklariGetirAsync();
            if (!sonuc.IsSuccess)
            {
                return Result<Durak>.Failure(sonuc.ErrorMessage, sonuc.ErrorCode);
            }
            
            var durak = sonuc.Value.FirstOrDefault(d => d.Id == durakId);
            if (durak == null)
            {
                return Result<Durak>.Failure($"Durak bulunamadı: {durakId}", "DURAK_NOT_FOUND");
            }
            
            return Result<Durak>.Success(durak);
        }
        
        #endregion

        #region Konum İşlemleri
        
        /// <inheritdoc />
        public Durak KullaniciNodeuOlustur(double enlem, double boylam, List<Durak> mevcutDuraklar)
        {
            if (mevcutDuraklar == null || !mevcutDuraklar.Any())
            {
                Logger.Warning("KullaniciNodeuOlustur: Mevcut durak listesi boş.");
                throw new ArgumentException("Mevcut durak listesi boş olamaz.", nameof(mevcutDuraklar));
            }
            
            // Yeni kullanıcı node'u oluştur
            var userNode = new Durak
            {
                Id = $"userNode_{Guid.NewGuid():N}",
                Ad = "Kullanıcı Konumu",
                Tur = "user",
                Enlem = enlem,
                Boylam = boylam,
                SonDurak = false,
                Baglantilar = new List<DurakBaglantisi>()
            };
            
            // Tüm mevcut duraklara bağlantı kur
            foreach (var durak in mevcutDuraklar)
            {
                double mesafe = MesafeHesapla(enlem, boylam, durak.Enlem, durak.Boylam);
                
                // Mesafeye göre araç tipi belirle
                Arac baglantıAraci;
                if (mesafe <= Constants.MAX_YURUME_MESAFESI)
                {
                    // Yürüme mesafesi içinde
                    baglantıAraci = new Yurumek(mesafe);
                }
                else
                {
                    // Taksi gerekli
                    baglantıAraci = new Taksi(mesafe);
                }
                
                // Çift yönlü bağlantı
                userNode.Baglantilar.Add(new DurakBaglantisi
                {
                    HedefDurakId = durak.Id,
                    Arac = baglantıAraci
                });
                
                durak.Baglantilar.Add(new DurakBaglantisi
                {
                    HedefDurakId = userNode.Id,
                    Arac = baglantıAraci
                });
            }
            
            Logger.Info($"Kullanıcı node'u oluşturuldu: {userNode.Id} ({enlem:F5}, {boylam:F5})");
            return userNode;
        }
        
        /// <inheritdoc />
        public Durak EnYakinDuragiBul(double enlem, double boylam)
        {
            var duraklar = DuraklariGetir();
            
            if (!duraklar.Any())
            {
                Logger.Warning("EnYakinDuragiBul: Durak listesi boş.");
                return null;
            }
            
            Durak enYakin = null;
            double minMesafe = double.MaxValue;
            
            foreach (var durak in duraklar)
            {
                double mesafe = MesafeHesapla(enlem, boylam, durak.Enlem, durak.Boylam);
                if (mesafe < minMesafe)
                {
                    minMesafe = mesafe;
                    enYakin = durak;
                }
            }
            
            Logger.Debug($"En yakın durak: {enYakin?.Ad} ({minMesafe:F2} km)");
            return enYakin;
        }
        
        /// <inheritdoc />
        public List<Durak> YakinDuraklariGetir(double enlem, double boylam, double yaricapKm)
        {
            if (yaricapKm <= 0)
            {
                Logger.Warning($"Geçersiz yarıçap: {yaricapKm}");
                return new List<Durak>();
            }
            
            var duraklar = DuraklariGetir();
            
            var yakinDuraklar = duraklar
                .Select(d => new { Durak = d, Mesafe = MesafeHesapla(enlem, boylam, d.Enlem, d.Boylam) })
                .Where(x => x.Mesafe <= yaricapKm)
                .OrderBy(x => x.Mesafe)
                .Select(x => x.Durak)
                .ToList();
            
            Logger.Debug($"{yakinDuraklar.Count} durak {yaricapKm} km yarıçap içinde bulundu.");
            return yakinDuraklar;
        }
        
        #endregion

        #region Yardımcı Metodlar
        
        /// <inheritdoc />
        public double MesafeHesapla(double enlem1, double boylam1, double enlem2, double boylam2)
        {
            // Haversine formülü
            double R = Constants.DUNYA_YARICAPI_KM;
            double dLat = ToRadians(enlem2 - enlem1);
            double dLon = ToRadians(boylam2 - boylam1);
            
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(enlem1)) * Math.Cos(ToRadians(enlem2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
        
        /// <inheritdoc />
        public void OnbellekTemizle()
        {
            lock (_cacheLock)
            {
                _cachedDuraklar = null;
                _cachedGrafDuraklar = null;
                _lastCacheTime = DateTime.MinValue;
            }
            Logger.Info("Durak önbelleği temizlendi.");
        }
        
        #endregion

        #region Private Helper Methods
        
        /// <summary>
        /// Önbelleğin hala geçerli olup olmadığını kontrol eder.
        /// </summary>
        private bool OnbellekGecerliMi()
        {
            lock (_cacheLock)
            {
                return DateTime.Now - _lastCacheTime < _cacheTTL;
            }
        }
        
        /// <summary>
        /// Dereceyi radyana çevirir.
        /// </summary>
        private double ToRadians(double derece)
        {
            return Math.PI * derece / 180.0;
        }
        
        /// <summary>
        /// JSON dosyasından durakları okur.
        /// </summary>
        /// <param name="grafOlarakMi">True ise bağlantıları da yükler</param>
        private List<Durak> DosyadanDuraklariOku(bool grafOlarakMi)
        {
            if (!File.Exists(_jsonPath))
            {
                throw new FileNotFoundException($"Veri dosyası bulunamadı: {_jsonPath}");
            }
            
            string jsonText = File.ReadAllText(_jsonPath);
            using JsonDocument doc = JsonDocument.Parse(jsonText);
            var duraklar = doc.RootElement.GetProperty("duraklar");
            
            var durakListesi = new List<Durak>();
            var durakDict = new Dictionary<string, Durak>();
            
            // İlk geçiş: Durakları oluştur
            foreach (var item in duraklar.EnumerateArray())
            {
                var durak = new Durak
                {
                    Id = item.GetProperty("id").GetString(),
                    Ad = item.GetProperty("name").GetString(),
                    Tur = item.GetProperty("type").GetString(),
                    Enlem = item.GetProperty("lat").GetDouble(),
                    Boylam = item.GetProperty("lon").GetDouble(),
                    SonDurak = grafOlarakMi && item.TryGetProperty("sonDurak", out var sd) && sd.GetBoolean(),
                    Baglantilar = new List<DurakBaglantisi>()
                };
                
                durakListesi.Add(durak);
                durakDict[durak.Id] = durak;
            }
            
            // Graf okuması ise bağlantıları da yükle
            if (grafOlarakMi)
            {
                int index = 0;
                foreach (var item in duraklar.EnumerateArray())
                {
                    var mevcutDurak = durakListesi[index++];
                    
                    // nextStops bağlantıları
                    if (item.TryGetProperty("nextStops", out JsonElement nextStops) && nextStops.GetArrayLength() > 0)
                    {
                        foreach (var next in nextStops.EnumerateArray())
                        {
                            string hedefId = next.GetProperty("stopId").GetString();
                            double mesafe = next.GetProperty("mesafe").GetDouble();
                            int sure = next.GetProperty("sure").GetInt32();
                            double ucret = next.GetProperty("ucret").GetDouble();
                            
                            Arac arac = mevcutDurak.Tur switch
                            {
                                "bus" => new Otobus(mesafe, ucret, sure),
                                "tram" => new Tramvay(mesafe, ucret, sure),
                                _ => null
                            };
                            
                            mevcutDurak.Baglantilar.Add(new DurakBaglantisi
                            {
                                HedefDurakId = hedefId,
                                Arac = arac
                            });
                        }
                    }
                    
                    // Transfer bağlantıları
                    if (item.TryGetProperty("transfer", out JsonElement transfer) && transfer.ValueKind == JsonValueKind.Object)
                    {
                        string transferStopId = transfer.GetProperty("transferStopId").GetString();
                        int transferSure = transfer.GetProperty("transferSure").GetInt32();
                        double transferUcret = transfer.GetProperty("transferUcret").GetDouble();
                        
                        mevcutDurak.Baglantilar.Add(new DurakBaglantisi
                        {
                            HedefDurakId = transferStopId,
                            Arac = new AktarmaAraci(transferSure, transferUcret)
                        });
                    }
                }
            }
            
            return durakListesi;
        }
        
        /// <summary>
        /// Durak listesinin deep copy'sini oluşturur.
        /// Bağlantılar değiştirilebilir olduğu için gerekli.
        /// </summary>
        private List<Durak> DeepCopyDuraklar(List<Durak> source)
        {
            var newList = new List<Durak>();
            var newDict = new Dictionary<string, Durak>();
            
            // Önce tüm durakları kopyala
            foreach (var d in source)
            {
                var newDurak = new Durak
                {
                    Id = d.Id,
                    Ad = d.Ad,
                    Tur = d.Tur,
                    Enlem = d.Enlem,
                    Boylam = d.Boylam,
                    SonDurak = d.SonDurak,
                    Baglantilar = new List<DurakBaglantisi>()
                };
                newList.Add(newDurak);
                newDict[newDurak.Id] = newDurak;
            }
            
            // Sonra bağlantıları kopyala
            for (int i = 0; i < source.Count; i++)
            {
                foreach (var bag in source[i].Baglantilar)
                {
                    newList[i].Baglantilar.Add(new DurakBaglantisi
                    {
                        HedefDurakId = bag.HedefDurakId,
                        Arac = bag.Arac // Arac immutable kabul ediliyor
                    });
                }
            }
            
            return newList;
        }
        
        #endregion
    }
}
