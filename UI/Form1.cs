using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using Prolab_4.Models;
using Prolab_4.Services;

namespace Prolab_4
{
    public partial class Form1 : Form
    {
        // --- GLOBAL DEĞİŞKENLER (UserNode Dahil Tüm Durak Sözlüğü) ---
        private Dictionary<string, Durak> globalDurakDict;
        private List<Durak> globalDurakList;

        private DurakService durakService = new DurakService();
        private KonumSecimServisi konumServisi = new KonumSecimServisi();

        public Form1()
        {
            InitializeComponent();
            
            this.Load += Form1_Load;
            
        }

        // --------------------------------------------------------------------------------
        // FORM LOAD
        // Harita ayarları ve başlangıçta şehirdeki otobüs/tram duraklarını göstermek isterseniz
        // DurakEkle'yi bu aşamada çağırıyorsunuz. (Ama rota çiziminde tekrar çağırmıyoruz.)
        // --------------------------------------------------------------------------------
        private void Form1_Load(object sender, EventArgs e)
        {
            this.BackgroundImage = Image.FromFile("Resource\\Arkaplan.png");
            this.BackgroundImageLayout = ImageLayout.Stretch;
            HaritaAyarla();
            DurakEkle();          // İsterseniz bunu kaldırabilirsiniz, harita tamamen boş başlar.
            comboboxdurakekleme();
            konumServisi.HaritaTiklamaBagla(gMapControl1);

            lblTaksi.ForeColor = Color.FromArgb(255, 165, 0);       // Turuncu
            lblOtobus.ForeColor = Color.FromArgb(70, 130, 180);     // Mavi
            lblTramvay.ForeColor = Color.FromArgb(60, 179, 113);    // Yeşil
            lblYurume.ForeColor = Color.FromArgb(139, 69, 19);      // Kahverengi
            lblAktarma.ForeColor = Color.FromArgb(138, 43, 226);    // Mor

        }

        
        // ------------------------------------------------------------------------
        // 1) HARİTA AYARLARI
        // ------------------------------------------------------------------------
        private void HaritaAyarla()
        {
            gMapControl1.MapProvider = GMapProviders.OpenStreetMap;
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            gMapControl1.Position = new PointLatLng(40.7655, 29.9400); // İzmit civarı

            gMapControl1.MinZoom = 1;
            gMapControl1.MaxZoom = 18;
            gMapControl1.Zoom = 12;

            gMapControl1.MouseWheelZoomEnabled = true;
            gMapControl1.MouseWheelZoomType = MouseWheelZoomType.ViewCenter;
            gMapControl1.CanDragMap = true;
            gMapControl1.DragButton = MouseButtons.Left;
            gMapControl1.ShowCenter = false;
        }

        // ------------------------------------------------------------------------
        // 2) BAŞLANGIÇTA DURAK MARKER EKLEME (İSTEĞE BAĞLI)
        // ------------------------------------------------------------------------
        private void DurakEkle()
        {
            // JSON'dan okunan durakların konumlarını marker olarak eklemek istersek
            var duraklar = durakService.DurakKonumlariniGetir();
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

        // ------------------------------------------------------------------------
        // 3) COMBOBOX'LARA DURAK ADI YÜKLEME
        // ------------------------------------------------------------------------
        private void comboboxdurakekleme()
        {
            var duraklar = durakService.DurakKonumlariniGetir();
            cmbBaslangic.DataSource = new List<Durak>(duraklar);
            cmbBaslangic.DisplayMember = "Ad";
            cmbBaslangic.ValueMember = "Id";

            cmbHedef.DataSource = new List<Durak>(duraklar);
            cmbHedef.DisplayMember = "Ad";
            cmbHedef.ValueMember = "Id";
        }

        // ------------------------------------------------------------------------
        // 4) ROTA HESAPLAMA (button1_Click) ve DataGridView LİSTELEME
        // ------------------------------------------------------------------------
        private void button1_Click(object sender, EventArgs e)
        {
            // 4a) Yolcu tipini al
            Yolcu seciliYolcu;
            switch (cmbKartDurumu.SelectedIndex)
            {
                case 1: seciliYolcu = new Ogrenci(); break;
                case 2: seciliYolcu = new Yasli(); break;
                default: seciliYolcu = new Genel(); break;
            }

            // 4b) ComboBox'tan seçili durak
            var secilenBaslangic = cmbBaslangic.SelectedItem as Durak;
            var secilenHedef = cmbHedef.SelectedItem as Durak;
            if (secilenBaslangic == null || secilenHedef == null)
            {
                MessageBox.Show("Lütfen hem başlangıç hem de hedef durak seçiniz.");
                return;
            }

            // 4c) Kullanıcı harita üzerinden konum seçtiyse, lat/lon al
            PointLatLng baslangic = new PointLatLng(secilenBaslangic.Enlem, secilenBaslangic.Boylam);
            PointLatLng hedef = new PointLatLng(secilenHedef.Enlem, secilenHedef.Boylam);

            if (konumServisi.BaslangicKonumu.HasValue)
                baslangic = konumServisi.BaslangicKonumu.Value;

            if (konumServisi.HedefKonumu.HasValue)
                hedef = konumServisi.HedefKonumu.Value;

            // 4d) DurakService ile graf oluştur, global değişkenlere koy
            var ds = new DurakService();
            globalDurakList = ds.DuraklariOkuVeGrafOlustur();
            globalDurakDict = globalDurakList.ToDictionary(d => d.Id, d => d);

            // 4e) Başlangıç/Hedef ID belirle, userNode eklenmesi vb.
            Durak userNode1 = null, userNode2 = null;
            string baslangicId;
            if (konumServisi.BaslangicDurak != null)
                baslangicId = konumServisi.BaslangicDurak.Id;
            else if (konumServisi.BaslangicKonumu.HasValue)
            {
                userNode1 = ds.AddUserNode(baslangic.Lat, baslangic.Lng, globalDurakList);
                globalDurakList.Add(userNode1);
                globalDurakDict[userNode1.Id] = userNode1;
                baslangicId = userNode1.Id;
            }
            else
                baslangicId = secilenBaslangic.Id;

            string hedefId;
            if (konumServisi.HedefDurak != null)
                hedefId = konumServisi.HedefDurak.Id;
            else if (konumServisi.HedefKonumu.HasValue)
            {
                userNode2 = ds.AddUserNode(hedef.Lat, hedef.Lng, globalDurakList);
                globalDurakList.Add(userNode2);
                globalDurakDict[userNode2.Id] = userNode2;
                hedefId = userNode2.Id;
            }
            else
                hedefId = secilenHedef.Id;

            // İki userNode varsa aralarına taksi ekleyebilirsiniz:
            if (userNode1 != null && userNode2 != null)
            {
                double mesafe = ds.MesafeHesapla(userNode1.Enlem, userNode1.Boylam, userNode2.Enlem, userNode2.Boylam);
                var taksi = new Taksi(mesafe);
                userNode1.Baglantilar.Add(new DurakBaglantisi { HedefDurakId = userNode2.Id, Arac = taksi });
                userNode2.Baglantilar.Add(new DurakBaglantisi { HedefDurakId = userNode1.Id, Arac = taksi });
            }


            IOdemeYontemi secilenOdeme = OdemeYontemiOlustur();

            if (secilenOdeme == null)
                return;

            // 4f) RotaHesaplayici ile tüm rotaları bul
            var hesaplayici = new RotaHesaplayici();
            var tumRotalar = hesaplayici.TumRotalariBul(globalDurakDict, baslangicId, hedefId, seciliYolcu, secilenOdeme);

            // 4g) Süreye göre sıralı anonim liste
            var gorunum = tumRotalar
                .OrderBy(r => r.ToplamSure) // <<< EN KRİTİK SATIR: Süreye göre artan sıralama
                .Select(r => new
                {
                    Duraklar = TransformRoute(r, globalDurakDict),
                    Ucret = r.ToplamUcret,
                    Sure = r.ToplamSure,
                    RotaObj = r
                })
                .ToList();

            // DataGridView'e bağla
            dataGridView1.DataSource = gorunum;

            // Sütun başlıklarını ayarla
            if (dataGridView1.Columns.Count >= 4)
            {
                dataGridView1.Columns[0].HeaderText = "Durak Sırası";
                dataGridView1.Columns[1].HeaderText = "Toplam Ücret";
                dataGridView1.Columns[2].HeaderText = "Toplam Süre (dk)";
                dataGridView1.Columns[3].Visible = false; // RotaObj sütununu gizle
            }

            // Görünüm ayarları
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView1.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dataGridView1.RowTemplate.Height = 40;
            dataGridView1.AllowUserToResizeRows = false;

            // Yazı tipi ve stil
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dataGridView1.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            dataGridView1.DefaultCellStyle.ForeColor = Color.Black;
            dataGridView1.DefaultCellStyle.BackColor = Color.WhiteSmoke;
            dataGridView1.BorderStyle = BorderStyle.None;

            MessageBox.Show("Tüm alternatif rotalar (süreye göre sıralı) listelendi.");
        }

        // ------------------------------------------------------------------------
        // 5) userNode ADINI YÜRÜME/TAKSİ YAPAN YARDIMCI FONKSİYON
        // ------------------------------------------------------------------------
        private string TransformRoute(Rota rota, Dictionary<string, Durak> durakDict)
        {
            // userNode_... => Yürüme veya Taksi, aksi halde durak Ad
            var adList = new List<string>();

            for (int i = 0; i < rota.DurakIdList.Count; i++)
            {
                string currentId = rota.DurakIdList[i];
                if (!currentId.StartsWith("userNode_"))
                {
                    // Normal durak
                    if (durakDict.ContainsKey(currentId))
                        adList.Add(durakDict[currentId].Ad);
                    else
                        adList.Add(currentId); // fallback
                }
                else
                {
                    // userNode => bir sonraki/önceki baglantinin arac tipine gore
                    string aracAdi = "Durak Dışı";
                    if (i < rota.DurakIdList.Count - 1)
                    {
                        string nextId = rota.DurakIdList[i + 1];
                        var currentDurak = durakDict[currentId];
                        foreach (var b in currentDurak.Baglantilar)
                        {
                            if (b.HedefDurakId == nextId)
                            {
                                if (b.Arac is Yurumek) aracAdi = "Yürüme";
                                else if (b.Arac is Taksi) aracAdi = "Taksi";
                                break;
                            }
                        }
                    }
                    else if (i > 0)
                    {
                        string prevId = rota.DurakIdList[i - 1];
                        var prevDurak = durakDict[prevId];
                        foreach (var b in prevDurak.Baglantilar)
                        {
                            if (b.HedefDurakId == currentId)
                            {
                                if (b.Arac is Yurumek) aracAdi = "Yürüme";
                                else if (b.Arac is Taksi) aracAdi = "Taksi";
                                break;
                            }
                        }
                    }
                    adList.Add(aracAdi);
                }
            }

            return string.Join(" → ", adList);
        }

        // ------------------------------------------------------------------------
        // 6) DATAGRIDVIEW'DE ROTA SEÇİLİNCE HARİTAYA SADECE O ROTA'YI ÇİZME
        // ------------------------------------------------------------------------
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            dynamic seciliSatir = dataGridView1.Rows[e.RowIndex].DataBoundItem;
            if (seciliSatir == null) return;

            Rota seciliRota = seciliSatir.RotaObj as Rota;
            if (seciliRota == null) return;

            // userNode dahil tüm duraklar globalDurakDict'te
            if (globalDurakDict == null) return;

            // Haritayı temizle
            gMapControl1.Overlays.Clear();

            // Rotanın noktaları
            var rotaOverlay = new GMapOverlay("seciliRota");
            var rotaNoktalari = new List<PointLatLng>();

            // Bu döngüde:
            // 1) Düğüm çiftleri için arac tipine göre segment rengi
            // 2) Marker renkleri (ilk=yeşil, son=kırmızı, aradakiler= mavi / userNode= sarı?)
            for (int i = 0; i < seciliRota.DurakIdList.Count; i++)
            {
                string durakId = seciliRota.DurakIdList[i];
                if (!globalDurakDict.ContainsKey(durakId))
                    continue; // beklenmeyen durum

                var durak = globalDurakDict[durakId];
                var nokta = new PointLatLng(durak.Enlem, durak.Boylam);
                rotaNoktalari.Add(nokta);

                // Marker rengi
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
                else
                {
                    // ara durak
                    if (durakId.StartsWith("userNode_"))
                    {
                        marker = new GMarkerGoogle(nokta, GMarkerGoogleType.yellow_dot);
                        marker.ToolTipText = "Kullanıcı Konumu";
                    }
                    else
                    {
                        marker = new GMarkerGoogle(nokta, GMarkerGoogleType.blue_dot);
                        marker.ToolTipText = durak.Ad;
                    }
                }
                rotaOverlay.Markers.Add(marker);

                // Eğer segment çizilecekse (i < son eleman)
                if (i < seciliRota.DurakIdList.Count - 1)
                {
                    string sonrakiDurakId = seciliRota.DurakIdList[i + 1];
                    if (globalDurakDict.ContainsKey(sonrakiDurakId))
                    {
                        var durak2 = globalDurakDict[sonrakiDurakId];
                        var nokta2 = new PointLatLng(durak2.Enlem, durak2.Boylam);

                        // Arac tipini bulmak için durak -> baglanti -> arac
                        var baglanti = durak.Baglantilar
                            .FirstOrDefault(b => b.HedefDurakId == sonrakiDurakId);

                        // Rengi araca göre belirle
                        Color cizgiRenk = Color.Black; // varsayılan
                        if (baglanti != null)
                            cizgiRenk = GetSegmentColor(baglanti.Arac);

                        var segmentPoints = new List<PointLatLng> { nokta, nokta2 };
                        var routeSegment = new GMapRoute(segmentPoints, "segment_" + i);
                        routeSegment.Stroke = new Pen(cizgiRenk, 3);
                        rotaOverlay.Routes.Add(routeSegment);
                    }
                }
            }

            // Overlay'i ekle
            gMapControl1.Overlays.Add(rotaOverlay);

            // Haritayı tazelemek için ufak zoom hilesi
            gMapControl1.Zoom++;
            gMapControl1.Zoom--;

            MessageBox.Show($"Seçilen rota {seciliRota.DurakIdList.Count} duraktan oluşuyor.");
        }

        // ------------------------------------------------------------------------
        // 7) ARAÇ TİPİNE GÖRE SEGMENT RENKLERİ
        // ------------------------------------------------------------------------
        private Color GetSegmentColor(Arac arac)
        {
            if (arac is Otobus) return Color.Blue;      // Otobüs => Mavi
            if (arac is Tramvay) return Color.Green;     // Tramvay => Yeşil
            if (arac is Taksi) return Color.Orange;    // Taksi => Turuncu
            if (arac is Yurumek) return Color.Brown;     // Yürüme => Kahverengi
            if (arac is AktarmaAraci) return Color.Purple;    // Aktarma => Mor
            return Color.Black;                              // Varsayılan => Siyah
        }

        // ------------------------------------------------------------------------
        // 8) KONUM SIFIRLAMA BUTONU
        // ------------------------------------------------------------------------
        private void btnKonumSifirla_Click(object sender, EventArgs e)
        {
            sifirlamafonksiyonu();
        }

        private void sifirlamafonksiyonu()
        {
            // Harita konum seçimlerini sıfırla
            konumServisi.ResetKonumlar();

            // Haritadaki tüm katmanları temizle
            gMapControl1.Overlays.Clear();

            // Geri isterseniz tekrar şehirdeki durakları göstermek için:
            DurakEkle();

            gMapControl1.Position = new PointLatLng(40.76520, 29.96190);
            gMapControl1.Zoom = 13;

            // Global sözlükleri de sıfırlayabiliriz (opsiyonel)
            // globalDurakDict = null;
            // globalDurakList = null;
        }

        private IOdemeYontemi OdemeYontemiOlustur()
        {
            if (nakitrbutton.Checked)
                return new NakitOdeme();
            else if (kentkartrbutton.Checked)
                return new KentKartOdeme();
            else if (kredikartırbutton.Checked)
                return new KrediKartiOdeme();
            else
            {
                MessageBox.Show("Lütfen bir ödeme yöntemi seçiniz.");
                return null;
            }
        }

    }
}
