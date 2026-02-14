using Prolab_4.Core;
using Prolab_4.Core.Configuration;
using Prolab_4.Core.Exceptions;
using Prolab_4.Core.Logging;
using Prolab_4.Models;
using Prolab_4.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Prolab_4.Repositories
{
    /// <summary>
    /// Durak repository interface
    /// </summary>
    public interface IDurakRepository
    {
        Task<Result<List<Durak>>> GetAllAsync();
        Task<Result<Durak>> GetByIdAsync(string id);
        Task<Result<List<Durak>>> GetByTurAsync(string tur);
        Task<Result<Durak>> GetNearestAsync(double enlem, double boylam);
        Task<Result<List<Durak>>> GetInRadiusAsync(double enlem, double boylam, double radiusKm);
    }

    /// <summary>
    /// JSON dosyasından durak verilerini okuyan repository
    /// </summary>
    public class JsonDurakRepository : IDurakRepository
    {
        private readonly string _jsonPath;
        private List<Durak> _cachedDuraklar;
        private Dictionary<string, Durak> _cachedDurakDict;
        private DateTime _lastLoadTime;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
        private readonly object _cacheLock = new object();

        public JsonDurakRepository()
        {
            _jsonPath = Config.App.DataFilePath;
        }

        public JsonDurakRepository(string jsonPath)
        {
            _jsonPath = jsonPath;
        }

        /// <summary>
        /// Tüm durakları getirir
        /// </summary>
        public async Task<Result<List<Durak>>> GetAllAsync()
        {
            try
            {
                await EnsureCacheLoadedAsync();
                return Result<List<Durak>>.Success(new List<Durak>(_cachedDuraklar));
            }
            catch (Exception ex)
            {
                Logger.Error("Duraklar yüklenirken hata oluştu", ex);
                return Result<List<Durak>>.Failure($"Duraklar yüklenemedi: {ex.Message}", "DATA_LOAD_ERROR");
            }
        }

        /// <summary>
        /// ID'ye göre durak getirir
        /// </summary>
        public async Task<Result<Durak>> GetByIdAsync(string id)
        {
            try
            {
                await EnsureCacheLoadedAsync();

                if (_cachedDurakDict.TryGetValue(id, out var durak))
                {
                    return Result<Durak>.Success(durak);
                }

                return Result<Durak>.Failure($"Durak bulunamadı: {id}", "DURAK_NOT_FOUND");
            }
            catch (Exception ex)
            {
                Logger.Error($"Durak aranırken hata: {id}", ex);
                return Result<Durak>.Failure($"Durak aranırken hata: {ex.Message}", "DATA_ERROR");
            }
        }

        /// <summary>
        /// Türe göre durakları getirir
        /// </summary>
        public async Task<Result<List<Durak>>> GetByTurAsync(string tur)
        {
            try
            {
                await EnsureCacheLoadedAsync();
                var filteredDuraklar = _cachedDuraklar.Where(d => d.Tur == tur).ToList();
                return Result<List<Durak>>.Success(filteredDuraklar);
            }
            catch (Exception ex)
            {
                Logger.Error($"Duraklar türe göre filtrelenirken hata: {tur}", ex);
                return Result<List<Durak>>.Failure($"Filtreleme hatası: {ex.Message}", "FILTER_ERROR");
            }
        }

        /// <summary>
        /// En yakın durağı bulur
        /// </summary>
        public async Task<Result<Durak>> GetNearestAsync(double enlem, double boylam)
        {
            try
            {
                await EnsureCacheLoadedAsync();

                if (!_cachedDuraklar.Any())
                {
                    return Result<Durak>.Failure("Hiç durak bulunamadı", "NO_DATA");
                }

                var nearest = _cachedDuraklar
                    .OrderBy(d => MesafeHesapla(d.Enlem, d.Boylam, enlem, boylam))
                    .First();

                return Result<Durak>.Success(nearest);
            }
            catch (Exception ex)
            {
                Logger.Error("En yakın durak aranırken hata", ex);
                return Result<Durak>.Failure($"Yakın durak arama hatası: {ex.Message}", "SEARCH_ERROR");
            }
        }

        /// <summary>
        /// Belirli yarıçap içindeki durakları getirir
        /// </summary>
        public async Task<Result<List<Durak>>> GetInRadiusAsync(double enlem, double boylam, double radiusKm)
        {
            try
            {
                await EnsureCacheLoadedAsync();

                var duraklar = _cachedDuraklar
                    .Where(d => MesafeHesapla(d.Enlem, d.Boylam, enlem, boylam) <= radiusKm)
                    .OrderBy(d => MesafeHesapla(d.Enlem, d.Boylam, enlem, boylam))
                    .ToList();

                return Result<List<Durak>>.Success(duraklar);
            }
            catch (Exception ex)
            {
                Logger.Error($"Yarıçap içi durak aramasında hata: {radiusKm}km", ex);
                return Result<List<Durak>>.Failure($"Arama hatası: {ex.Message}", "SEARCH_ERROR");
            }
        }

        /// <summary>
        /// Cache'in güncel olduğundan emin olur
        /// </summary>
        private async Task EnsureCacheLoadedAsync()
        {
            bool needsReload = false;

            lock (_cacheLock)
            {
                if (_cachedDuraklar == null || DateTime.Now - _lastLoadTime > _cacheExpiration)
                {
                    needsReload = true;
                }
            }

            if (needsReload)
            {
                await LoadDataAsync();
            }
        }

        /// <summary>
        /// JSON'dan veriyi yükler
        /// </summary>
        private async Task LoadDataAsync()
        {
            Logger.Info($"Veri yükleniyor: {_jsonPath}");

            if (!File.Exists(_jsonPath))
            {
                throw new DataLoadException(_jsonPath, new FileNotFoundException($"Dosya bulunamadı: {_jsonPath}"));
            }

            string jsonText = await File.ReadAllTextAsync(_jsonPath);
            using JsonDocument doc = JsonDocument.Parse(jsonText);
            var duraklar = doc.RootElement.GetProperty("duraklar");

            var durakListesi = new List<Durak>();
            var durakDict = new Dictionary<string, Durak>();

            // İlk geçiş: Tüm durakları oluştur
            foreach (var item in duraklar.EnumerateArray())
            {
                var durak = new Durak
                {
                    Id = item.GetProperty("id").GetString(),
                    Ad = item.GetProperty("name").GetString(),
                    Tur = item.GetProperty("type").GetString(),
                    Enlem = item.GetProperty("lat").GetDouble(),
                    Boylam = item.GetProperty("lon").GetDouble(),
                    SonDurak = item.GetProperty("sonDurak").GetBoolean(),
                    Baglantilar = new List<DurakBaglantisi>()
                };

                durakListesi.Add(durak);
                durakDict[durak.Id] = durak;
            }

            // İkinci geçiş: Bağlantıları kur
            int index = 0;
            foreach (var item in duraklar.EnumerateArray())
            {
                var mevcutDurak = durakListesi[index];
                index++;

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

            lock (_cacheLock)
            {
                _cachedDuraklar = durakListesi;
                _cachedDurakDict = durakDict;
                _lastLoadTime = DateTime.Now;
            }

            Logger.Info($"{durakListesi.Count} durak yüklendi");
        }

        /// <summary>
        /// İki koordinat arasındaki mesafeyi hesaplar (Haversine formülü)
        /// </summary>
        private double MesafeHesapla(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371.0;
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double angle) => Math.PI * angle / 180.0;

        /// <summary>
        /// Cache'i temizler
        /// </summary>
        public void ClearCache()
        {
            lock (_cacheLock)
            {
                _cachedDuraklar = null;
                _cachedDurakDict = null;
            }
            Logger.Info("Durak cache temizlendi");
        }
    }

    /// <summary>
    /// Rota repository interface
    /// </summary>
    public interface IRotaRepository
    {
        Task<Result<List<Rota>>> GetAllRotalarAsync(string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odeme);
        Task<Result<Rota>> GetEnKisaRotaAsync(string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odeme);
        Task<Result<Rota>> GetEnUcuzRotaAsync(string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odeme);
    }

    /// <summary>
    /// Rota hesaplama repository implementasyonu
    /// </summary>
    public class RotaRepository : IRotaRepository
    {
        private readonly IDurakRepository _durakRepository;
        private readonly IRotaHesaplayici _rotaHesaplayici;

        public RotaRepository(IDurakRepository durakRepository, IRotaHesaplayici rotaHesaplayici)
        {
            _durakRepository = durakRepository;
            _rotaHesaplayici = rotaHesaplayici;
        }

        public async Task<Result<List<Rota>>> GetAllRotalarAsync(string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odeme)
        {
            var durakResult = await _durakRepository.GetAllAsync();
            if (!durakResult.IsSuccess)
            {
                return Result<List<Rota>>.Failure(durakResult.ErrorMessage, durakResult.ErrorCode);
            }

            var durakDict = durakResult.Value.ToDictionary(d => d.Id, d => d);
            var rotalar = _rotaHesaplayici.TumRotalariBul(durakDict, baslangicId, hedefId, yolcu, odeme);
            
            return Result<List<Rota>>.Success(rotalar);
        }

        public async Task<Result<Rota>> GetEnKisaRotaAsync(string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odeme)
        {
            var durakResult = await _durakRepository.GetAllAsync();
            if (!durakResult.IsSuccess)
            {
                return Result<Rota>.Failure(durakResult.ErrorMessage, durakResult.ErrorCode);
            }

            var durakDict = durakResult.Value.ToDictionary(d => d.Id, d => d);
            var rota = _rotaHesaplayici.EnKisaRotaBul(durakDict, baslangicId, hedefId, yolcu, odeme);

            if (rota == null)
            {
                return Result<Rota>.Failure("Rota bulunamadı", "ROUTE_NOT_FOUND");
            }

            return Result<Rota>.Success(rota);
        }

        public async Task<Result<Rota>> GetEnUcuzRotaAsync(string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odeme)
        {
            var durakResult = await _durakRepository.GetAllAsync();
            if (!durakResult.IsSuccess)
            {
                return Result<Rota>.Failure(durakResult.ErrorMessage, durakResult.ErrorCode);
            }

            var durakDict = durakResult.Value.ToDictionary(d => d.Id, d => d);
            var rota = _rotaHesaplayici.EnUcuzRotaBul(durakDict, baslangicId, hedefId, yolcu, odeme);

            if (rota == null)
            {
                return Result<Rota>.Failure("Rota bulunamadı", "ROUTE_NOT_FOUND");
            }

            return Result<Rota>.Success(rota);
        }
    }
}
