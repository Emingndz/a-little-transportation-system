using Prolab_4.Core;
using Prolab_4.Core.Logging;
using Prolab_4.Models;
using Prolab_4.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Prolab_4.Services
{
    /// <summary>
    /// IRotaServisi interface'inin tam implementasyonu.
    /// Dijkstra algoritması kullanarak en kısa ve en ucuz rotaları hesaplar.
    /// Mevcut DijkstraRotaHesaplayici sınıfını sarmalar.
    /// </summary>
    public class RotaServisiImpl : IRotaServisi
    {
        #region Private Fields
        
        /// <summary>Durak servisi referansı</summary>
        private readonly IDurakServisi _durakServisi;
        
        /// <summary>Rota hesaplayıcı (Dijkstra implementasyonu)</summary>
        private readonly IRotaHesaplayici _rotaHesaplayici;
        
        /// <summary>Durak dictionary cache'i</summary>
        private Dictionary<string, Durak> _durakDict;
        
        #endregion

        #region Constructor
        
        /// <summary>
        /// Bağımlılıkları enjekte eden constructor.
        /// </summary>
        /// <param name="durakServisi">Durak servisi</param>
        /// <param name="rotaHesaplayici">Rota hesaplayıcı (null ise varsayılan Dijkstra kullanılır)</param>
        public RotaServisiImpl(IDurakServisi durakServisi, IRotaHesaplayici rotaHesaplayici = null)
        {
            _durakServisi = durakServisi ?? throw new ArgumentNullException(nameof(durakServisi));
            _rotaHesaplayici = rotaHesaplayici ?? new DijkstraRotaHesaplayici();
            _durakDict = new Dictionary<string, Durak>();
            Logger.Debug("RotaServisiImpl oluşturuldu (dependency injection ile).");
        }
        
        /// <summary>
        /// Varsayılan constructor.
        /// Dijkstra algoritmasını varsayılan olarak kullanır.
        /// </summary>
        public RotaServisiImpl()
        {
            _durakServisi = null;
            _rotaHesaplayici = new DijkstraRotaHesaplayici();
            _durakDict = new Dictionary<string, Durak>();
            Logger.Debug("RotaServisiImpl oluşturuldu (varsayılan).");
        }
        
        #endregion

        #region Durak Dict Yönetimi
        
        /// <inheritdoc />
        public void DurakDictGuncelle(Dictionary<string, Durak> durakDict)
        {
            _durakDict = durakDict ?? new Dictionary<string, Durak>();
            Logger.Debug($"DurakDict güncellendi: {_durakDict.Count} durak");
        }
        
        /// <inheritdoc />
        public Dictionary<string, Durak> DurakDictGetir()
        {
            return _durakDict;
        }
        
        #endregion

        #region Temel Rota Hesaplama (Senkron)
        
        /// <inheritdoc />
        public Rota EnKisaRotaHesapla(string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odemeYontemi)
        {
            try
            {
                if (!ParametreleriDogrula(baslangicId, hedefId, out string hata))
                {
                    Logger.Warning($"Parametre hatası: {hata}");
                    return null;
                }
                
                var rota = _rotaHesaplayici.EnKisaRotaBul(_durakDict, baslangicId, hedefId, yolcu, odemeYontemi);
                
                if (rota != null)
                {
                    Logger.Info($"En kısa rota hesaplandı: {baslangicId} -> {hedefId}, Süre: {rota.ToplamSure} dk");
                }
                else
                {
                    Logger.Warning($"En kısa rota bulunamadı: {baslangicId} -> {hedefId}");
                }
                
                return rota;
            }
            catch (Exception ex)
            {
                Logger.Error("En kısa rota hesaplanırken hata oluştu.", ex);
                return null;
            }
        }
        
        /// <inheritdoc />
        public Rota EnUcuzRotaHesapla(string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odemeYontemi)
        {
            try
            {
                if (!ParametreleriDogrula(baslangicId, hedefId, out string hata))
                {
                    Logger.Warning($"Parametre hatası: {hata}");
                    return null;
                }
                
                var rota = _rotaHesaplayici.EnUcuzRotaBul(_durakDict, baslangicId, hedefId, yolcu, odemeYontemi);
                
                if (rota != null)
                {
                    Logger.Info($"En ucuz rota hesaplandı: {baslangicId} -> {hedefId}, Ücret: {rota.ToplamUcret:F2} TL");
                }
                else
                {
                    Logger.Warning($"En ucuz rota bulunamadı: {baslangicId} -> {hedefId}");
                }
                
                return rota;
            }
            catch (Exception ex)
            {
                Logger.Error("En ucuz rota hesaplanırken hata oluştu.", ex);
                return null;
            }
        }
        
        /// <inheritdoc />
        public List<Rota> TumRotalariHesapla(string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odemeYontemi)
        {
            try
            {
                if (!ParametreleriDogrula(baslangicId, hedefId, out string hata))
                {
                    Logger.Warning($"Parametre hatası: {hata}");
                    return new List<Rota>();
                }
                
                var rotalar = _rotaHesaplayici.TumRotalariBul(_durakDict, baslangicId, hedefId, yolcu, odemeYontemi);
                
                Logger.Info($"Toplam {rotalar?.Count ?? 0} alternatif rota hesaplandı.");
                
                return rotalar ?? new List<Rota>();
            }
            catch (Exception ex)
            {
                Logger.Error("Tüm rotalar hesaplanırken hata oluştu.", ex);
                return new List<Rota>();
            }
        }
        
        #endregion

        #region Asenkron Rota Hesaplama
        
        /// <inheritdoc />
        public async Task<Result<Rota>> EnKisaRotaHesaplaAsync(string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odemeYontemi)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var rota = EnKisaRotaHesapla(baslangicId, hedefId, yolcu, odemeYontemi);
                    
                    if (rota == null)
                    {
                        return Result<Rota>.Failure("Rota bulunamadı.", "ROTA_NOT_FOUND");
                    }
                    
                    return Result<Rota>.Success(rota);
                }
                catch (Exception ex)
                {
                    return Result<Rota>.Failure(ex.Message, "ROTA_HESAPLAMA_HATASI");
                }
            });
        }
        
        /// <inheritdoc />
        public async Task<Result<Rota>> EnUcuzRotaHesaplaAsync(string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odemeYontemi)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var rota = EnUcuzRotaHesapla(baslangicId, hedefId, yolcu, odemeYontemi);
                    
                    if (rota == null)
                    {
                        return Result<Rota>.Failure("Rota bulunamadı.", "ROTA_NOT_FOUND");
                    }
                    
                    return Result<Rota>.Success(rota);
                }
                catch (Exception ex)
                {
                    return Result<Rota>.Failure(ex.Message, "ROTA_HESAPLAMA_HATASI");
                }
            });
        }
        
        /// <inheritdoc />
        public async Task<Result<List<Rota>>> TumRotalariHesaplaAsync(string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odemeYontemi)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var rotalar = TumRotalariHesapla(baslangicId, hedefId, yolcu, odemeYontemi);
                    
                    if (rotalar == null || !rotalar.Any())
                    {
                        return Result<List<Rota>>.Failure("Hiç rota bulunamadı.", "ROTA_NOT_FOUND");
                    }
                    
                    return Result<List<Rota>>.Success(rotalar);
                }
                catch (Exception ex)
                {
                    return Result<List<Rota>>.Failure(ex.Message, "ROTA_HESAPLAMA_HATASI");
                }
            });
        }
        
        #endregion

        #region Sıralama ve Filtreleme
        
        /// <inheritdoc />
        public List<Rota> RotalariSirala(List<Rota> rotalar, RotaSiralamaKriteri kriter, bool azalan = false)
        {
            if (rotalar == null || !rotalar.Any()) 
            {
                return rotalar ?? new List<Rota>();
            }
            
            // Mevcut Rota modeli yalnızca DurakIdList, ToplamSure, ToplamUcret içerir
            // Mesafe ve Araclar property'leri yok, bu nedenle sadece mevcut property'ler kullanılır
            IOrderedEnumerable<Rota> siralama = kriter switch
            {
                RotaSiralamaKriteri.Sure => azalan 
                    ? rotalar.OrderByDescending(r => r.ToplamSure) 
                    : rotalar.OrderBy(r => r.ToplamSure),
                    
                RotaSiralamaKriteri.Ucret => azalan 
                    ? rotalar.OrderByDescending(r => r.ToplamUcret) 
                    : rotalar.OrderBy(r => r.ToplamUcret),
                    
                // Aktarma sayısı = durak sayısı - 2 (başlangıç ve hedef hariç)
                RotaSiralamaKriteri.AktarmaSayisi => azalan 
                    ? rotalar.OrderByDescending(r => Math.Max(0, (r.DurakIdList?.Count ?? 2) - 2)) 
                    : rotalar.OrderBy(r => Math.Max(0, (r.DurakIdList?.Count ?? 2) - 2)),
                    
                // Mesafe bilgisi mevcut modelde yok, süre proxy olarak kullanılır
                RotaSiralamaKriteri.Mesafe => azalan 
                    ? rotalar.OrderByDescending(r => r.ToplamSure) 
                    : rotalar.OrderBy(r => r.ToplamSure),
                    
                _ => rotalar.OrderBy(r => r.ToplamSure)
            };
            
            Logger.Debug($"Rotalar sıralandı: Kriter={kriter}, Azalan={azalan}");
            return siralama.ToList();
        }
        
        /// <inheritdoc />
        public List<Rota> RotalariFiltrele(List<Rota> rotalar, RotaFiltresi filtre)
        {
            if (rotalar == null || !rotalar.Any() || filtre == null) 
            {
                return rotalar ?? new List<Rota>();
            }
            
            var sonuc = rotalar.AsEnumerable();
            
            // Maksimum ücret filtresi
            if (filtre.MaxUcret.HasValue)
            {
                sonuc = sonuc.Where(r => r.ToplamUcret <= filtre.MaxUcret.Value);
            }
            
            // Maksimum süre filtresi
            if (filtre.MaxSureDakika.HasValue)
            {
                sonuc = sonuc.Where(r => r.ToplamSure <= filtre.MaxSureDakika.Value);
            }
            
            // Maksimum aktarma sayısı filtresi
            // Aktarma sayısı = durak sayısı - 2 (başlangıç ve hedef hariç)
            if (filtre.MaxAktarmaSayisi.HasValue)
            {
                sonuc = sonuc.Where(r => 
                    Math.Max(0, (r.DurakIdList?.Count ?? 2) - 2) <= filtre.MaxAktarmaSayisi.Value);
            }
            
            // Not: SadeceYurume ve TaksiHaric filtreleri mevcut Rota modelinde
            // araç bilgisi olmadığı için uygulanamıyor (gelecekte genişletilebilir)
            if (filtre.SadeceYurume || filtre.TaksiHaric)
            {
                Logger.Warning("SadeceYurume/TaksiHaric filtreleri mevcut model yapısında desteklenmiyor.");
            }
            
            var filtrelenmis = sonuc.ToList();
            Logger.Debug($"Rotalar filtrelendi: {rotalar.Count} -> {filtrelenmis.Count}");
            
            return filtrelenmis;
        }
        
        /// <inheritdoc />
        public RotaKarsilastirma RotalariKarsilastir(Rota rota1, Rota rota2)
        {
            if (rota1 == null || rota2 == null)
            {
                throw new ArgumentNullException("Karşılaştırılacak rotalar null olamaz.");
            }
            
            // Aktarma sayısı hesaplama
            int aktarma1 = Math.Max(0, (rota1.DurakIdList?.Count ?? 2) - 2);
            int aktarma2 = Math.Max(0, (rota2.DurakIdList?.Count ?? 2) - 2);
            
            var karsilastirma = new RotaKarsilastirma
            {
                SureFarki = rota1.ToplamSure - rota2.ToplamSure,
                UcretFarki = rota1.ToplamUcret - rota2.ToplamUcret,
                AktarmaFarki = aktarma1 - aktarma2
            };
            
            // Özet değerlendirme oluştur
            var avantajlar1 = new List<string>();
            var avantajlar2 = new List<string>();
            
            if (karsilastirma.SureFarki < 0) avantajlar1.Add("daha hızlı");
            else if (karsilastirma.SureFarki > 0) avantajlar2.Add("daha hızlı");
            
            if (karsilastirma.UcretFarki < 0) avantajlar1.Add("daha ucuz");
            else if (karsilastirma.UcretFarki > 0) avantajlar2.Add("daha ucuz");
            
            if (karsilastirma.AktarmaFarki < 0) avantajlar1.Add("daha az aktarma");
            else if (karsilastirma.AktarmaFarki > 0) avantajlar2.Add("daha az aktarma");
            
            // Özet oluştur
            if (avantajlar1.Any() && avantajlar2.Any())
            {
                karsilastirma.Ozet = $"Rota 1: {string.Join(", ", avantajlar1)}. Rota 2: {string.Join(", ", avantajlar2)}.";
            }
            else if (avantajlar1.Any())
            {
                karsilastirma.Ozet = $"Rota 1 tercih edilir: {string.Join(", ", avantajlar1)}.";
            }
            else if (avantajlar2.Any())
            {
                karsilastirma.Ozet = $"Rota 2 tercih edilir: {string.Join(", ", avantajlar2)}.";
            }
            else
            {
                karsilastirma.Ozet = "Her iki rota eşdeğerdir.";
            }
            
            return karsilastirma;
        }
        
        #endregion

        #region Yardımcı Metodlar
        
        /// <summary>
        /// Başlangıç ve hedef parametrelerini doğrular.
        /// </summary>
        /// <param name="baslangicId">Başlangıç durak ID'si</param>
        /// <param name="hedefId">Hedef durak ID'si</param>
        /// <param name="hata">Hata mesajı (varsa)</param>
        /// <returns>Parametreler geçerliyse true</returns>
        private bool ParametreleriDogrula(string baslangicId, string hedefId, out string hata)
        {
            if (string.IsNullOrWhiteSpace(baslangicId))
            {
                hata = "Başlangıç ID boş olamaz.";
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(hedefId))
            {
                hata = "Hedef ID boş olamaz.";
                return false;
            }
            
            if (baslangicId == hedefId)
            {
                hata = "Başlangıç ve hedef aynı olamaz.";
                return false;
            }
            
            if (_durakDict == null || _durakDict.Count == 0)
            {
                hata = "Durak verisi yüklenmemiş. Önce DurakDictGuncelle çağrılmalı.";
                return false;
            }
            
            if (!_durakDict.ContainsKey(baslangicId))
            {
                hata = $"Başlangıç durağı bulunamadı: {baslangicId}";
                return false;
            }
            
            if (!_durakDict.ContainsKey(hedefId))
            {
                hata = $"Hedef durağı bulunamadı: {hedefId}";
                return false;
            }
            
            hata = null;
            return true;
        }
        
        #endregion
    }
}
