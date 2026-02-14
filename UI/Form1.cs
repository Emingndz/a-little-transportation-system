using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using Prolab_4.Core;
using Prolab_4.Core.DependencyInjection;
using Prolab_4.Core.Logging;
using Prolab_4.Models;
using Prolab_4.Services;
using Prolab_4.Services.Interfaces;

namespace Prolab_4
{
    /// <summary>
    /// Ana form - MVP pattern ile refactor edildi.
    /// Servisler DI Container üzerinden alınır.
    /// </summary>
    public partial class Form1 : Form
    {
        #region Private Fields
        
        // Durak verileri
        private Dictionary<string, Durak> _globalDurakDict;
        private List<Durak> _globalDurakList;

        // Servisler - DI Container'dan alınacak
        private readonly IDurakServisi _durakServisi;
        private readonly IRotaServisi _rotaServisi;
        private readonly DurakService _legacyDurakService; // Geriye uyumluluk için
        private readonly KonumSecimServisi _konumServisi;
        
        // İşlem durumu
        private bool _isProcessing = false;
        
        #endregion

        #region Constructor
        
        public Form1()
        {
            InitializeComponent();
            
            // Servisleri initialize et
            ServiceBootstrapper.Initialize();
            
            // DI Container'dan servisleri al
            _durakServisi = ServiceContainer.Instance.Resolve<IDurakServisi>();
            _rotaServisi = ServiceContainer.Instance.Resolve<IRotaServisi>();
            
            // Legacy servisler (geriye uyumluluk)
            _legacyDurakService = new DurakService();
            _konumServisi = new KonumSecimServisi();
            
            Logger.Info("Form1 başlatıldı - Servisler yüklendi.");
            
            this.Load += Form1_Load;
        }
        
        #endregion

        #region Form Events
        
        private async void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                // Profesyonel UI stillerini uygula
                ApplyProfessionalStyles();
                
                // Harita ayarlarını yap
                HaritaAyarla();
                
                // Durakları asenkron yükle
                await DurakEkleAsync();
                
                // ComboBox'ları doldur
                ComboBoxDuraklariniDoldur();
                
                // Konum seçim servisini bağla
                _konumServisi.HaritaTiklamaBagla(gMapControl1);
                
                Logger.Info("Form1 yüklendi - Tüm bileşenler hazır.");
            }
            catch (Exception ex)
            {
                Logger.Error("Form yüklenirken hata oluştu.", ex);
                MessageBox.Show($"Form yüklenirken hata: {ex.Message}", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Profesyonel UI stillerini uygular.
        /// </summary>
        private void ApplyProfessionalStyles()
        {
            // Kartlara rounded corner efekti ekle
            AddCardShadowEffect(pnlLocationCard);
            AddCardShadowEffect(pnlPaymentCard);
            
            // Hover efektleri ekle
            AddButtonHoverEffect(btnRotaOlustur, Color.FromArgb(22, 163, 74), Color.FromArgb(34, 197, 94));
            AddButtonHoverEffect(btnHaritaSifirla, Color.FromArgb(243, 244, 246), Color.White);
            
            // Legend panel'e border ekle
            pnlLegend.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(209, 213, 219), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlLegend.Width - 1, pnlLegend.Height - 1);
                }
            };
            
            // Map panel'e border ekle
            pnlMap.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(209, 213, 219), 2))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlMap.Width - 1, pnlMap.Height - 1);
                }
            };
            
            // ComboBox'lara focus efekti ekle
            AddComboBoxFocusEffect(cmbBaslangic);
            AddComboBoxFocusEffect(cmbHedef);
            AddComboBoxFocusEffect(cmbKartDurumu);
        }
        
        /// <summary>
        /// ComboBox'a focus efekti ekler.
        /// </summary>
        private void AddComboBoxFocusEffect(ComboBox comboBox)
        {
            comboBox.Enter += (s, e) => comboBox.BackColor = Color.FromArgb(239, 246, 255);
            comboBox.Leave += (s, e) => comboBox.BackColor = Color.White;
        }
        
        /// <summary>
        /// Kart paneline gölge efekti ekler.
        /// </summary>
        private void AddCardShadowEffect(Panel panel)
        {
            panel.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(229, 231, 235), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
                }
            };
        }
        
        /// <summary>
        /// Butona hover efekti ekler.
        /// </summary>
        private void AddButtonHoverEffect(Button button, Color hoverColor, Color normalColor)
        {
            button.MouseEnter += (s, e) => button.BackColor = hoverColor;
            button.MouseLeave += (s, e) => button.BackColor = normalColor;
        }
        
        #endregion

        #region Harita Ayarları
        
        /// <summary>
        /// Harita kontrolünü yapılandırır.
        /// Constants sınıfındaki değerler kullanılır.
        /// </summary>
        private void HaritaAyarla()
        {
            gMapControl1.MapProvider = GMapProviders.OpenStreetMap;
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            gMapControl1.Position = new PointLatLng(
                Constants.HARITA_VARSAYILAN_ENLEM, 
                Constants.HARITA_VARSAYILAN_BOYLAM);

            gMapControl1.MinZoom = Constants.HARITA_MIN_ZOOM;
            gMapControl1.MaxZoom = Constants.HARITA_MAX_ZOOM;
            gMapControl1.Zoom = Constants.HARITA_VARSAYILAN_ZOOM;

            gMapControl1.MouseWheelZoomEnabled = true;
            gMapControl1.MouseWheelZoomType = MouseWheelZoomType.ViewCenter;
            gMapControl1.CanDragMap = true;
            gMapControl1.DragButton = MouseButtons.Left;
            gMapControl1.ShowCenter = false;
            
            Logger.Debug("Harita ayarları yapılandırıldı.");
        }
        
        #endregion

        #region Durak İşlemleri
        
        /// <summary>
        /// Durakları asenkron olarak yükler ve haritaya ekler.
        /// </summary>
        private async Task DurakEkleAsync()
        {
            try
            {
                // DI servisi ile asenkron yükle
                var durakResult = await _durakServisi.DuraklariGetirAsync();
                
                List<Durak> duraklar;
                if (durakResult.IsSuccess)
                {
                    duraklar = durakResult.Value;
                }
                else
                {
                    // Fallback: Legacy servis kullan
                    Logger.Warning($"DI servisi başarısız, legacy servis kullanılıyor: {durakResult.ErrorMessage}");
                    duraklar = _legacyDurakService.DurakKonumlariniGetir();
                }
                
                var overlay = new GMapOverlay("baslangicDuraklar");

                foreach (var d in duraklar)
                {
                    var marker = new GMarkerGoogle(
                        new PointLatLng(d.Enlem, d.Boylam),
                        d.Tur == "bus" ? GMarkerGoogleType.blue_dot : GMarkerGoogleType.red_dot
                    );
                    marker.ToolTipText = d.Ad;
                    overlay.Markers.Add(marker);
                }

                gMapControl1.Overlays.Clear();
                gMapControl1.Overlays.Add(overlay);
                
                Logger.Info($"{duraklar.Count} durak haritaya eklendi.");
            }
            catch (Exception ex)
            {
                Logger.Error("Duraklar yüklenirken hata.", ex);
                // Fallback: Senkron yükle
                DurakEkle();
            }
        }
        
        /// <summary>
        /// Durakları senkron olarak yükler (fallback).
        /// </summary>
        private void DurakEkle()
        {
            var duraklar = _legacyDurakService.DurakKonumlariniGetir();
            var overlay = new GMapOverlay("baslangicDuraklar");

            foreach (var d in duraklar)
            {
                var marker = new GMarkerGoogle(
                    new PointLatLng(d.Enlem, d.Boylam),
                    d.Tur == "bus" ? GMarkerGoogleType.blue_dot : GMarkerGoogleType.red_dot
                );
                marker.ToolTipText = d.Ad;
                overlay.Markers.Add(marker);
            }

            gMapControl1.Overlays.Clear();
            gMapControl1.Overlays.Add(overlay);
        }

        /// <summary>
        /// ComboBox'ları durak verileriyle doldurur.
        /// </summary>
        private void ComboBoxDuraklariniDoldur()
        {
            var duraklar = _legacyDurakService.DurakKonumlariniGetir();
            
            cmbBaslangic.DataSource = new List<Durak>(duraklar);
            cmbBaslangic.DisplayMember = "Ad";
            cmbBaslangic.ValueMember = "Id";

            cmbHedef.DataSource = new List<Durak>(duraklar);
            cmbHedef.DisplayMember = "Ad";
            cmbHedef.ValueMember = "Id";
        }
        
        #endregion

        #region Rota Hesaplama
        
        /// <summary>
        /// Rota hesaplama butonu - Asenkron işlem.
        /// </summary>
        private async void button1_Click(object sender, EventArgs e)
        {
            // Çift tıklama engelleme
            if (_isProcessing)
            {
                Logger.Warning("İşlem zaten devam ediyor.");
                return;
            }
            
            try
            {
                _isProcessing = true;
                this.Cursor = Cursors.WaitCursor;
                
                // Yolcu tipini belirle
                Yolcu seciliYolcu = cmbKartDurumu.SelectedIndex switch
                {
                    1 => new Ogrenci(),
                    2 => new Yasli(),
                    _ => new Genel()
                };

                // Durak seçimlerini doğrula
                var secilenBaslangic = cmbBaslangic.SelectedItem as Durak;
                var secilenHedef = cmbHedef.SelectedItem as Durak;
                
                if (secilenBaslangic == null || secilenHedef == null)
                {
                    MessageBox.Show("Lütfen hem başlangıç hem de hedef durak seçiniz.", 
                        "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Konum bilgilerini al
                PointLatLng baslangic = new PointLatLng(secilenBaslangic.Enlem, secilenBaslangic.Boylam);
                PointLatLng hedef = new PointLatLng(secilenHedef.Enlem, secilenHedef.Boylam);

                if (_konumServisi.BaslangicKonumu.HasValue)
                    baslangic = _konumServisi.BaslangicKonumu.Value;

                if (_konumServisi.HedefKonumu.HasValue)
                    hedef = _konumServisi.HedefKonumu.Value;

                // Durak verilerini yükle
                _globalDurakList = _legacyDurakService.DuraklariOkuVeGrafOlustur();
                _globalDurakDict = _globalDurakList.ToDictionary(d => d.Id, d => d);

                // Kullanıcı node'larını oluştur
                Durak userNode1 = null, userNode2 = null;
                string baslangicId = BelirleBaslangicId(secilenBaslangic, baslangic, ref userNode1);
                string hedefId = BelirleHedefId(secilenHedef, hedef, ref userNode2);

                // Kullanıcı node'ları arasına taksi bağlantısı ekle
                if (userNode1 != null && userNode2 != null)
                {
                    EkleKullaniciNodeBaglantisi(userNode1, userNode2);
                }

                // Ödeme yöntemi
                IOdemeYontemi secilenOdeme = OdemeYontemiOlustur();
                if (secilenOdeme == null) return;

                // Rota servisi için durak dict güncelle
                _rotaServisi.DurakDictGuncelle(_globalDurakDict);

                // Asenkron rota hesaplama
                var rotalarResult = await _rotaServisi.TumRotalariHesaplaAsync(
                    baslangicId, hedefId, seciliYolcu, secilenOdeme);

                List<Rota> tumRotalar;
                if (rotalarResult.IsSuccess)
                {
                    tumRotalar = rotalarResult.Value;
                    Logger.Info($"{tumRotalar.Count} rota hesaplandı (async).");
                }
                else
                {
                    // Fallback: Legacy hesaplayıcı
                    Logger.Warning($"Async hesaplama başarısız: {rotalarResult.ErrorMessage}");
                    var hesaplayici = new RotaHesaplayici();
                    tumRotalar = hesaplayici.TumRotalariBul(_globalDurakDict, baslangicId, hedefId, seciliYolcu, secilenOdeme);
                }

                // Sonuçları göster
                GosterRotalari(tumRotalar);
                
                MessageBox.Show($"{tumRotalar.Count} alternatif rota bulundu (süreye göre sıralı).", 
                    "Rota Hesaplama Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.Error("Rota hesaplanırken hata oluştu.", ex);
                MessageBox.Show($"Rota hesaplanırken hata: {ex.Message}", 
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isProcessing = false;
                this.Cursor = Cursors.Default;
            }
        }
        
        /// <summary>
        /// Başlangıç ID'sini belirler, gerekirse kullanıcı node'u oluşturur.
        /// </summary>
        private string BelirleBaslangicId(Durak secilenBaslangic, PointLatLng baslangic, ref Durak userNode)
        {
            if (_konumServisi.BaslangicDurak != null)
                return _konumServisi.BaslangicDurak.Id;
                
            if (_konumServisi.BaslangicKonumu.HasValue)
            {
                userNode = _legacyDurakService.AddUserNode(baslangic.Lat, baslangic.Lng, _globalDurakList);
                _globalDurakList.Add(userNode);
                _globalDurakDict[userNode.Id] = userNode;
                return userNode.Id;
            }
            
            return secilenBaslangic.Id;
        }
        
        /// <summary>
        /// Hedef ID'sini belirler, gerekirse kullanıcı node'u oluşturur.
        /// </summary>
        private string BelirleHedefId(Durak secilenHedef, PointLatLng hedef, ref Durak userNode)
        {
            if (_konumServisi.HedefDurak != null)
                return _konumServisi.HedefDurak.Id;
                
            if (_konumServisi.HedefKonumu.HasValue)
            {
                userNode = _legacyDurakService.AddUserNode(hedef.Lat, hedef.Lng, _globalDurakList);
                _globalDurakList.Add(userNode);
                _globalDurakDict[userNode.Id] = userNode;
                return userNode.Id;
            }
            
            return secilenHedef.Id;
        }
        
        /// <summary>
        /// İki kullanıcı node'u arasına taksi bağlantısı ekler.
        /// </summary>
        private void EkleKullaniciNodeBaglantisi(Durak node1, Durak node2)
        {
            double mesafe = _legacyDurakService.MesafeHesapla(
                node1.Enlem, node1.Boylam, node2.Enlem, node2.Boylam);
            var taksi = new Taksi(mesafe);
            node1.Baglantilar.Add(new DurakBaglantisi { HedefDurakId = node2.Id, Arac = taksi });
            node2.Baglantilar.Add(new DurakBaglantisi { HedefDurakId = node1.Id, Arac = taksi });
        }
        
        /// <summary>
        /// Rotaları DataGridView'de gösterir.
        /// </summary>
        private void GosterRotalari(List<Rota> rotalar)
        {
            // Süreye göre sırala ve görüntü formatına dönüştür
            var gorunum = rotalar
                .OrderBy(r => r.ToplamSure)
                .Select((r, index) => new
                {
                    Sira = $"#{index + 1}",
                    Guzergah = TransformRoute(r, _globalDurakDict),
                    Ucret = $"₺{r.ToplamUcret:F2}",
                    Sure = $"{r.ToplamSure} dk",
                    RotaObj = r
                })
                .ToList();

            // DataGridView'e bağla
            dataGridView1.DataSource = gorunum;

            // Sütun başlıklarını ayarla
            if (dataGridView1.Columns.Count >= 5)
            {
                dataGridView1.Columns[0].HeaderText = "#";
                dataGridView1.Columns[0].Width = 50;
                dataGridView1.Columns[0].MinimumWidth = 50;
                dataGridView1.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                
                dataGridView1.Columns[1].HeaderText = "🗺️ Güzergah";
                dataGridView1.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                
                dataGridView1.Columns[2].HeaderText = "💰 Ücret";
                dataGridView1.Columns[2].Width = 100;
                dataGridView1.Columns[2].MinimumWidth = 100;
                dataGridView1.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                
                dataGridView1.Columns[3].HeaderText = "⏱️ Süre";
                dataGridView1.Columns[3].Width = 100;
                dataGridView1.Columns[3].MinimumWidth = 100;
                dataGridView1.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                
                dataGridView1.Columns[4].Visible = false;
            }

            // Görsel ayarlar - Profesyonel stil
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView1.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dataGridView1.RowTemplate.Height = 50;
            dataGridView1.AllowUserToResizeRows = false;
            
            // Font ayarları
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            dataGridView1.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            dataGridView1.DefaultCellStyle.ForeColor = Color.FromArgb(31, 41, 55);
            dataGridView1.DefaultCellStyle.BackColor = Color.White;
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(239, 246, 255);
            dataGridView1.DefaultCellStyle.SelectionForeColor = Color.FromArgb(31, 41, 55);
            dataGridView1.DefaultCellStyle.Padding = new Padding(8, 4, 8, 4);
            
            // Alternatif satır rengi
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
            
            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridView1.GridColor = Color.FromArgb(229, 231, 235);
        }
        
        #endregion

        #region Rota Görüntüleme
        
        /// <summary>
        /// Rotayı okunabilir formata dönüştürür.
        /// Format: "Başlangıç 🚌 Durak1 🚋 Durak2 🚶 Hedef"
        /// </summary>
        private string TransformRoute(Rota rota, Dictionary<string, Durak> durakDict)
        {
            if (rota == null || rota.DurakIdList == null || rota.DurakIdList.Count == 0)
                return "Rota bulunamadı";

            var sonuc = new System.Text.StringBuilder();

            for (int i = 0; i < rota.DurakIdList.Count; i++)
            {
                string currentId = rota.DurakIdList[i];
                
                // Durak adını al
                string durakAdi;
                if (currentId.StartsWith("userNode_"))
                {
                    durakAdi = i == 0 ? "📍 Konum" : "🎯 Hedef";
                }
                else if (durakDict.ContainsKey(currentId))
                {
                    durakAdi = durakDict[currentId].Ad;
                }
                else
                {
                    durakAdi = currentId;
                }

                // İlk durak değilse, araç tipini ekle
                if (i > 0)
                {
                    string prevId = rota.DurakIdList[i - 1];
                    string aracEmoji = GetAracEmoji(prevId, currentId, durakDict);
                    sonuc.Append($" {aracEmoji} ");
                }

                sonuc.Append(durakAdi);
            }

            return sonuc.ToString();
        }
        
        /// <summary>
        /// İki durak arasındaki araç tipine göre emoji döndürür.
        /// </summary>
        private string GetAracEmoji(string fromId, string toId, Dictionary<string, Durak> durakDict)
        {
            if (!durakDict.ContainsKey(fromId))
                return "→";

            var fromDurak = durakDict[fromId];
            var baglanti = fromDurak.Baglantilar?.FirstOrDefault(b => b.HedefDurakId == toId);

            if (baglanti?.Arac == null)
                return "→";

            return baglanti.Arac switch
            {
                Otobus => "🚌→",
                Tramvay => "🚋→",
                Taksi => "🚕→",
                Yurumek => "🚶→",
                AktarmaAraci => "🔄→",
                _ => "→"
            };
        }

        /// <summary>
        /// DataGridView satır tıklama - Seçilen rotayı haritada göster.
        /// </summary>
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            dynamic seciliSatir = dataGridView1.Rows[e.RowIndex].DataBoundItem;
            if (seciliSatir == null) return;

            Rota seciliRota = seciliSatir.RotaObj as Rota;
            if (seciliRota == null) return;

            if (_globalDurakDict == null) return;

            // Haritayı temizle
            gMapControl1.Overlays.Clear();

            var rotaOverlay = new GMapOverlay("seciliRota");
            var rotaNoktalari = new List<PointLatLng>();

            // Her durak için marker ve segment ekle
            for (int i = 0; i < seciliRota.DurakIdList.Count; i++)
            {
                string durakId = seciliRota.DurakIdList[i];
                if (!_globalDurakDict.ContainsKey(durakId))
                    continue;

                var durak = _globalDurakDict[durakId];
                var nokta = new PointLatLng(durak.Enlem, durak.Boylam);
                rotaNoktalari.Add(nokta);

                // Marker tipi belirle
                GMarkerGoogle marker;
                if (i == 0)
                {
                    marker = new GMarkerGoogle(nokta, GMarkerGoogleType.green);
                    marker.ToolTipText = "Başlangıç";
                }
                else if (i == seciliRota.DurakIdList.Count - 1)
                {
                    marker = new GMarkerGoogle(nokta, GMarkerGoogleType.red);
                    marker.ToolTipText = "Bitiş";
                }
                else if (durakId.StartsWith("userNode_"))
                {
                    marker = new GMarkerGoogle(nokta, GMarkerGoogleType.yellow_dot);
                    marker.ToolTipText = "Kullanıcı Konumu";
                }
                else
                {
                    marker = new GMarkerGoogle(nokta, GMarkerGoogleType.blue_dot);
                    marker.ToolTipText = durak.Ad;
                }
                rotaOverlay.Markers.Add(marker);

                // Segmentleri çiz
                if (i < seciliRota.DurakIdList.Count - 1)
                {
                    string sonrakiDurakId = seciliRota.DurakIdList[i + 1];
                    if (_globalDurakDict.ContainsKey(sonrakiDurakId))
                    {
                        var durak2 = _globalDurakDict[sonrakiDurakId];
                        var nokta2 = new PointLatLng(durak2.Enlem, durak2.Boylam);

                        var baglanti = durak.Baglantilar
                            .FirstOrDefault(b => b.HedefDurakId == sonrakiDurakId);

                        Color cizgiRenk = baglanti != null 
                            ? GetSegmentColor(baglanti.Arac) 
                            : Color.Black;

                        var segmentPoints = new List<PointLatLng> { nokta, nokta2 };
                        var routeSegment = new GMapRoute(segmentPoints, "segment_" + i);
                        routeSegment.Stroke = new Pen(cizgiRenk, 3);
                        rotaOverlay.Routes.Add(routeSegment);
                    }
                }
            }

            gMapControl1.Overlays.Add(rotaOverlay);

            // Haritayı yenile
            gMapControl1.Zoom++;
            gMapControl1.Zoom--;

            Logger.Debug($"Rota haritada gösterildi: {seciliRota.DurakIdList.Count} durak");
        }

        /// <summary>
        /// Araç tipine göre segment rengi döndürür.
        /// </summary>
        private Color GetSegmentColor(Arac arac)
        {
            return arac switch
            {
                Otobus => Color.Blue,
                Tramvay => Color.Green,
                Taksi => Color.Orange,
                Yurumek => Color.Brown,
                AktarmaAraci => Color.Purple,
                _ => Color.Black
            };
        }
        
        #endregion

        #region Yardımcı Metodlar
        
        /// <summary>
        /// Konum sıfırlama butonu.
        /// </summary>
        private void btnKonumSifirla_Click(object sender, EventArgs e)
        {
            SifirlamaYap();
        }

        /// <summary>
        /// Haritayı ve seçimleri sıfırlar.
        /// </summary>
        private void SifirlamaYap()
        {
            _konumServisi.ResetKonumlar();
            gMapControl1.Overlays.Clear();
            DurakEkle();
            
            gMapControl1.Position = new PointLatLng(
                Constants.HARITA_VARSAYILAN_ENLEM, 
                Constants.HARITA_VARSAYILAN_BOYLAM);
            gMapControl1.Zoom = Constants.HARITA_VARSAYILAN_ZOOM;
            
            Logger.Info("Harita ve seçimler sıfırlandı.");
        }

        /// <summary>
        /// Ödeme yöntemi nesnesini oluşturur.
        /// </summary>
        private IOdemeYontemi OdemeYontemiOlustur()
        {
            if (nakitrbutton.Checked)
                return new NakitOdeme();
            else if (kentkartrbutton.Checked)
                return new KentKartOdeme();
            else if (kredikartirbutton.Checked)
                return new KrediKartiOdeme();
            else
            {
                MessageBox.Show("Lütfen bir ödeme yöntemi seçiniz.", 
                    "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
        }
        
        #endregion
    }
}
