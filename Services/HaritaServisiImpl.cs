using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using Prolab_4.Core;
using Prolab_4.Core.Logging;
using Prolab_4.Models;
using Prolab_4.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Prolab_4.Services
{
    /// <summary>
    /// IHaritaServisi interface'inin tam implementasyonu.
    /// GMap.NET kontrolünü sarmalar ve harita işlemlerini yönetir.
    /// Mevcut Rota modeli (DurakIdList, ToplamSure, ToplamUcret) ile uyumludur.
    /// </summary>
    public class HaritaServisiImpl : IHaritaServisi
    {
        #region Private Fields
        
        /// <summary>GMap.NET harita kontrolü</summary>
        private GMapControl _haritaKontrol;
        
        /// <summary>Duraklar için overlay katmanı</summary>
        private GMapOverlay _durakOverlay;
        
        /// <summary>Rotalar için overlay katmanı</summary>
        private GMapOverlay _rotaOverlay;
        
        /// <summary>Kullanıcı marker'ları için overlay katmanı</summary>
        private GMapOverlay _kullaniciOverlay;
        
        /// <summary>Konum seçimi aktif mi</summary>
        private bool _konumSecimAktif;
        
        /// <summary>Konum seçimi callback fonksiyonu</summary>
        private Action<KonumSecimSonucu> _konumSecimCallback;
        
        /// <summary>Marker -> Durak eşleşme haritası</summary>
        private readonly Dictionary<GMapMarker, Durak> _markerDurakMap;
        
        #endregion

        #region Properties
        
        /// <inheritdoc />
        public KonumSecimSonucu BaslangicKonumu { get; private set; }
        
        /// <inheritdoc />
        public KonumSecimSonucu HedefKonumu { get; private set; }
        
        #endregion

        #region Events
        
        /// <inheritdoc />
        public event EventHandler<KonumSecimEventArgs> KonumSecildi;
        
        /// <inheritdoc />
        public event EventHandler<HaritaTiklamaEventArgs> HaritaTiklandi;
        
        #endregion

        #region Constructor
        
        /// <summary>
        /// HaritaServisiImpl constructor.
        /// </summary>
        public HaritaServisiImpl()
        {
            _markerDurakMap = new Dictionary<GMapMarker, Durak>();
            Logger.Debug("HaritaServisiImpl oluşturuldu.");
        }
        
        #endregion

        #region Harita Yapılandırma
        
        /// <inheritdoc />
        public void HaritaYapilandir(GMapControl haritaKontrol)
        {
            _haritaKontrol = haritaKontrol ?? throw new ArgumentNullException(nameof(haritaKontrol));
            
            // Harita temel ayarları
            _haritaKontrol.MapProvider = GMapProviders.GoogleMap;
            _haritaKontrol.MinZoom = Constants.HARITA_MIN_ZOOM;
            _haritaKontrol.MaxZoom = Constants.HARITA_MAX_ZOOM;
            _haritaKontrol.Zoom = Constants.HARITA_VARSAYILAN_ZOOM;
            _haritaKontrol.Position = new PointLatLng(
                Constants.HARITA_VARSAYILAN_ENLEM, 
                Constants.HARITA_VARSAYILAN_BOYLAM);
            _haritaKontrol.DragButton = MouseButtons.Left;
            _haritaKontrol.ShowCenter = false;
            
            // Overlay katmanları oluştur
            _durakOverlay = new GMapOverlay("duraklar");
            _rotaOverlay = new GMapOverlay("rotalar");
            _kullaniciOverlay = new GMapOverlay("kullanici");
            
            _haritaKontrol.Overlays.Add(_durakOverlay);
            _haritaKontrol.Overlays.Add(_rotaOverlay);
            _haritaKontrol.Overlays.Add(_kullaniciOverlay);
            
            // Event handler'ları bağla
            _haritaKontrol.OnMapClick += OnMapClick;
            _haritaKontrol.OnMarkerClick += OnMarkerClick;
            
            Logger.Info("Harita yapılandırıldı.");
        }
        
        /// <inheritdoc />
        public void HaritaOrtala(double enlem, double boylam, int? zoom = null)
        {
            if (_haritaKontrol == null) return;
            
            _haritaKontrol.Position = new PointLatLng(enlem, boylam);
            if (zoom.HasValue)
            {
                _haritaKontrol.Zoom = zoom.Value;
            }
            Logger.Debug($"Harita ortalandı: ({enlem:F5}, {boylam:F5})");
        }
        
        /// <inheritdoc />
        public void HaritaSifirla()
        {
            HaritaOrtala(
                Constants.HARITA_VARSAYILAN_ENLEM, 
                Constants.HARITA_VARSAYILAN_BOYLAM, 
                Constants.HARITA_VARSAYILAN_ZOOM);
            MarkerlarTemizle();
            RotaTemizle();
            KonumSecimleriniSifirla();
            Logger.Info("Harita sıfırlandı.");
        }
        
        #endregion

        #region Marker İşlemleri
        
        /// <inheritdoc />
        public void DurakMarkerlariniEkle(List<Durak> duraklar)
        {
            if (duraklar == null || !duraklar.Any()) return;
            
            _durakOverlay?.Markers.Clear();
            _markerDurakMap.Clear();
            
            foreach (var durak in duraklar)
            {
                DurakMarkerEkle(durak, HaritaMarkerTipi.Normal);
            }
            
            Logger.Info($"{duraklar.Count} durak marker'ı eklendi.");
        }
        
        /// <inheritdoc />
        public void DurakMarkerEkle(Durak durak, HaritaMarkerTipi markerTipi = HaritaMarkerTipi.Normal)
        {
            if (durak == null || _durakOverlay == null) return;
            
            var pozisyon = new PointLatLng(durak.Enlem, durak.Boylam);
            
            // Marker tipine göre görsel seç
            GMarkerGoogleType tip = markerTipi switch
            {
                HaritaMarkerTipi.Baslangic => GMarkerGoogleType.green_dot,
                HaritaMarkerTipi.Bitis => GMarkerGoogleType.red_dot,
                HaritaMarkerTipi.AraDurak => GMarkerGoogleType.blue_small,
                HaritaMarkerTipi.KullaniciKonumu => GMarkerGoogleType.yellow_dot,
                HaritaMarkerTipi.Otobus => GMarkerGoogleType.blue_small,
                HaritaMarkerTipi.Tramvay => GMarkerGoogleType.orange_small,
                _ => DurakTipineGoreMarkerSec(durak)
            };
            
            var marker = new GMarkerGoogle(pozisyon, tip)
            {
                ToolTipText = $"{durak.Ad}\n({durak.Tur})",
                ToolTipMode = MarkerTooltipMode.OnMouseOver
            };
            
            _durakOverlay.Markers.Add(marker);
            _markerDurakMap[marker] = durak;
        }
        
        /// <summary>
        /// Durak tipine göre uygun marker tipi seçer.
        /// </summary>
        /// <param name="durak">Durak bilgisi</param>
        /// <returns>GMap marker tipi</returns>
        private GMarkerGoogleType DurakTipineGoreMarkerSec(Durak durak)
        {
            if (durak?.Tur == null) return GMarkerGoogleType.gray_small;
            
            return durak.Tur.ToLowerInvariant() switch
            {
                "bus" => GMarkerGoogleType.blue_small,
                "tram" => GMarkerGoogleType.orange_small,
                "otobus" => GMarkerGoogleType.blue_small,
                "tramvay" => GMarkerGoogleType.orange_small,
                _ => GMarkerGoogleType.gray_small
            };
        }
        
        /// <inheritdoc />
        public void KullaniciMarkerEkle(double enlem, double boylam, string aciklama)
        {
            if (_kullaniciOverlay == null) return;
            
            var marker = new GMarkerGoogle(
                new PointLatLng(enlem, boylam), 
                GMarkerGoogleType.yellow_pushpin)
            {
                ToolTipText = aciklama ?? "Kullanıcı Konumu",
                ToolTipMode = MarkerTooltipMode.OnMouseOver
            };
            
            _kullaniciOverlay.Markers.Add(marker);
            Logger.Debug($"Kullanıcı marker'ı eklendi: ({enlem:F5}, {boylam:F5})");
        }
        
        /// <inheritdoc />
        public void MarkerlarTemizle()
        {
            _durakOverlay?.Markers.Clear();
            _kullaniciOverlay?.Markers.Clear();
            _markerDurakMap.Clear();
            Logger.Debug("Tüm marker'lar temizlendi.");
        }
        
        #endregion

        #region Rota Çizimi
        
        /// <inheritdoc />
        /// <remarks>
        /// Mevcut Rota modeli DurakIdList içerdiğinden, durakDict parametresi
        /// ID'leri Durak nesnelerine dönüştürmek için kullanılır.
        /// </remarks>
        public void RotaCiz(Rota rota, Dictionary<string, Durak> durakDict)
        {
            // Mevcut model: Rota { DurakIdList, ToplamUcret, ToplamSure }
            if (rota?.DurakIdList == null || rota.DurakIdList.Count < 2 || _rotaOverlay == null)
            {
                Logger.Warning("Rota çizilemedi: Yetersiz durak bilgisi.");
                return;
            }
            
            if (durakDict == null || durakDict.Count == 0)
            {
                Logger.Warning("Rota çizilemedi: Durak dictionary boş.");
                return;
            }
            
            RotaTemizle();
            
            // DurakIdList'teki her ardışık çift için çizgi çiz
            for (int i = 0; i < rota.DurakIdList.Count - 1; i++)
            {
                string baslangicId = rota.DurakIdList[i];
                string hedefId = rota.DurakIdList[i + 1];
                
                // Durak ID'lerinden Durak nesnelerini al
                if (!durakDict.TryGetValue(baslangicId, out Durak baslangic) ||
                    !durakDict.TryGetValue(hedefId, out Durak hedef))
                {
                    Logger.Warning($"Durak bulunamadı: {baslangicId} veya {hedefId}");
                    continue;
                }
                
                // İki durak arasında araç tipini belirle
                Arac arac = BaglantiAraciniGetir(baslangic, hedefId);
                
                var noktalar = new List<PointLatLng>
                {
                    new PointLatLng(baslangic.Enlem, baslangic.Boylam),
                    new PointLatLng(hedef.Enlem, hedef.Boylam)
                };
                
                var route = new GMapRoute(noktalar, $"segment_{i}")
                {
                    Stroke = new Pen(AracRengiGetir(arac), 4)
                };
                
                _rotaOverlay.Routes.Add(route);
            }
            
            // Başlangıç ve bitiş marker'ları ekle
            if (durakDict.TryGetValue(rota.DurakIdList.First(), out Durak ilkDurak))
            {
                KullaniciMarkerEkle(ilkDurak.Enlem, ilkDurak.Boylam, "Başlangıç");
            }
            
            if (durakDict.TryGetValue(rota.DurakIdList.Last(), out Durak sonDurak))
            {
                KullaniciMarkerEkle(sonDurak.Enlem, sonDurak.Boylam, "Hedef");
            }
            
            Logger.Info($"Rota çizildi: {rota.DurakIdList.Count} durak");
        }
        
        /// <summary>
        /// İki durak arasındaki bağlantı aracını bulur.
        /// </summary>
        /// <param name="baslangicDurak">Başlangıç durağı</param>
        /// <param name="hedefDurakId">Hedef durak ID'si</param>
        /// <returns>Bağlantı aracı veya null</returns>
        private Arac BaglantiAraciniGetir(Durak baslangicDurak, string hedefDurakId)
        {
            if (baslangicDurak?.Baglantilar == null) return null;
            
            var baglanti = baslangicDurak.Baglantilar
                .FirstOrDefault(b => b.HedefDurakId == hedefDurakId);
            
            return baglanti?.Arac;
        }
        
        /// <inheritdoc />
        public void RotalarCiz(List<Rota> rotalar, Dictionary<string, Durak> durakDict)
        {
            if (rotalar == null || !rotalar.Any() || durakDict == null) return;
            
            RotaTemizle();
            
            for (int r = 0; r < rotalar.Count; r++)
            {
                var rota = rotalar[r];
                
                // İlk rota daha belirgin, diğerleri daha soluk
                int opacity = r == 0 ? 255 : 128;
                int kalinlik = r == 0 ? 4 : 2;
                
                if (rota?.DurakIdList == null || rota.DurakIdList.Count < 2) continue;
                
                for (int i = 0; i < rota.DurakIdList.Count - 1; i++)
                {
                    string baslangicId = rota.DurakIdList[i];
                    string hedefId = rota.DurakIdList[i + 1];
                    
                    if (!durakDict.TryGetValue(baslangicId, out Durak baslangic) ||
                        !durakDict.TryGetValue(hedefId, out Durak hedef))
                    {
                        continue;
                    }
                    
                    Arac arac = BaglantiAraciniGetir(baslangic, hedefId);
                    var renk = AracRengiGetir(arac);
                    
                    var noktalar = new List<PointLatLng>
                    {
                        new PointLatLng(baslangic.Enlem, baslangic.Boylam),
                        new PointLatLng(hedef.Enlem, hedef.Boylam)
                    };
                    
                    var route = new GMapRoute(noktalar, $"rota{r}_segment{i}")
                    {
                        Stroke = new Pen(Color.FromArgb(opacity, renk), kalinlik)
                    };
                    
                    _rotaOverlay.Routes.Add(route);
                }
            }
            
            Logger.Info($"{rotalar.Count} rota çizildi.");
        }
        
        /// <inheritdoc />
        public Color AracRengiGetir(Arac arac)
        {
            return arac switch
            {
                Otobus => Color.Blue,
                Tramvay => Color.Orange,
                Taksi => Color.Yellow,
                Yurumek => Color.Green,
                AktarmaAraci => Color.Purple,
                _ => Color.Gray
            };
        }
        
        /// <inheritdoc />
        public void RotaTemizle()
        {
            _rotaOverlay?.Routes.Clear();
            _kullaniciOverlay?.Markers.Clear();
            Logger.Debug("Rotalar temizlendi.");
        }
        
        #endregion

        #region Konum Seçimi
        
        /// <inheritdoc />
        public void KonumSeciminiBaslat(Action<KonumSecimSonucu> callback)
        {
            _konumSecimAktif = true;
            _konumSecimCallback = callback;
            
            if (_haritaKontrol != null)
            {
                _haritaKontrol.Cursor = Cursors.Cross;
            }
            
            Logger.Info("Konum seçim modu aktif.");
        }
        
        /// <inheritdoc />
        public void KonumSeciminiDurdur()
        {
            _konumSecimAktif = false;
            _konumSecimCallback = null;
            
            if (_haritaKontrol != null)
            {
                _haritaKontrol.Cursor = Cursors.Default;
            }
            
            Logger.Debug("Konum seçim modu deaktif.");
        }
        
        /// <inheritdoc />
        public void KonumSecimleriniSifirla()
        {
            BaslangicKonumu = null;
            HedefKonumu = null;
            _kullaniciOverlay?.Markers.Clear();
            Logger.Debug("Konum seçimleri sıfırlandı.");
        }
        
        #endregion

        #region Event Handlers
        
        /// <summary>
        /// Harita tıklama event handler'ı.
        /// </summary>
        private void OnMapClick(PointLatLng point, MouseEventArgs e)
        {
            // Event'i tetikle (mevcut interface X/Y kullanıyor, MouseButton değil)
            HaritaTiklandi?.Invoke(this, new HaritaTiklamaEventArgs
            {
                Enlem = point.Lat,
                Boylam = point.Lng,
                X = e.X,
                Y = e.Y
            });
            
            // Konum seçimi aktifse işle
            if (_konumSecimAktif)
            {
                // Mevcut interface: Enlem, Boylam, DurakMi, Durak, EnYakinDurakMesafesi, Tur
                var sonuc = new KonumSecimSonucu
                {
                    Enlem = point.Lat,
                    Boylam = point.Lng,
                    DurakMi = false,
                    Durak = null,
                    EnYakinDurakMesafesi = 0
                };
                
                // Başlangıç mı hedef mi?
                if (BaslangicKonumu == null)
                {
                    BaslangicKonumu = sonuc;
                    sonuc.Tur = KonumSecimTuru.Baslangic;
                    KullaniciMarkerEkle(point.Lat, point.Lng, "Başlangıç");
                }
                else
                {
                    HedefKonumu = sonuc;
                    sonuc.Tur = KonumSecimTuru.Hedef;
                    KullaniciMarkerEkle(point.Lat, point.Lng, "Hedef");
                    KonumSeciminiDurdur();
                }
                
                // Callback'i çağır
                _konumSecimCallback?.Invoke(sonuc);
                
                // Event'i tetikle
                KonumSecildi?.Invoke(this, new KonumSecimEventArgs
                {
                    Sonuc = sonuc
                });
            }
        }
        
        /// <summary>
        /// Marker tıklama event handler'ı.
        /// </summary>
        private void OnMarkerClick(GMapMarker item, MouseEventArgs e)
        {
            // Marker'a karşılık gelen durağı bul
            if (_markerDurakMap.TryGetValue(item, out Durak durak))
            {
                Logger.Debug($"Durak marker'ına tıklandı: {durak.Ad}");
                
                // Konum seçimi aktifse bu durağı seç
                if (_konumSecimAktif)
                {
                    // Mevcut interface: Enlem, Boylam, DurakMi, Durak, EnYakinDurakMesafesi, Tur
                    var sonuc = new KonumSecimSonucu
                    {
                        Enlem = durak.Enlem,
                        Boylam = durak.Boylam,
                        DurakMi = true,
                        Durak = durak,
                        EnYakinDurakMesafesi = 0
                    };
                    
                    if (BaslangicKonumu == null)
                    {
                        BaslangicKonumu = sonuc;
                        sonuc.Tur = KonumSecimTuru.Baslangic;
                    }
                    else
                    {
                        HedefKonumu = sonuc;
                        sonuc.Tur = KonumSecimTuru.Hedef;
                        KonumSeciminiDurdur();
                    }
                    
                    _konumSecimCallback?.Invoke(sonuc);
                    
                    KonumSecildi?.Invoke(this, new KonumSecimEventArgs
                    {
                        Sonuc = sonuc
                    });
                }
            }
        }
        
        #endregion
    }
}
