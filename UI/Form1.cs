using System;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.WindowsPresentation;
using Prolab_4.Models;
using Prolab_4.Services;

namespace Prolab_4
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;

        }

        private void Form1_Load(object sender, EventArgs e)
        { 
            HaritaAyarla();
            DurakEkle();
            comboboxdurakekleme();
            konumServisi.HaritaTiklamaBagla(gMapControl1);
        }

        

        private DurakService durakService = new DurakService();
        private KonumSecimServisi konumServisi = new KonumSecimServisi();

        private void DurakEkle()
        {
            var duraklar = durakService.DurakKonumlariniGetir();
            var overlay = new GMapOverlay("duraklar");

            foreach (var d in duraklar)
            {
                var marker = new GMarkerGoogle(
                    new PointLatLng(d.Enlem, d.Boylam),
                    d.Tur == "bus" ? GMarkerGoogleType.blue_dot : GMarkerGoogleType.red_dot
                );
                marker.ToolTipText = d.Ad;
                overlay.Markers.Add(marker);
            }

            gMapControl1.Overlays.Clear(); // temizlemeden ekleme yapma!
            gMapControl1.Overlays.Add(overlay);
        }

        private void HaritaAyarla()
        {
            gMapControl1.MapProvider = GMapProviders.OpenStreetMap; // OSM Harita Sağlayıcısı
            GMaps.Instance.Mode = AccessMode.ServerAndCache; // Sunucu ve önbellekten yükle
            gMapControl1.Position = new PointLatLng(40.7655, 29.9400); // İzmit, Kocaeli başlangıç noktası

            // Yakınlaştırma ayarları
            gMapControl1.MinZoom = 1;
            gMapControl1.MaxZoom = 18;
            gMapControl1.Zoom = 12;

            // Fare ile zoom açma
            gMapControl1.MouseWheelZoomEnabled = true;
            gMapControl1.MouseWheelZoomType = MouseWheelZoomType.ViewCenter;

            // Harita sürükleme ayarları
            gMapControl1.CanDragMap = true;
            gMapControl1.DragButton = MouseButtons.Left;
            gMapControl1.ShowCenter = false;
        }

        private void comboboxdurakekleme()
        {
            var duraklar = durakService.DurakKonumlariniGetir();

            // ComboBox'lara durak adlarını yükle
            cmbBaslangic.DataSource = new List<Durak>(duraklar);
            cmbBaslangic.DisplayMember = "Ad";
            cmbBaslangic.ValueMember = "Id";

            cmbHedef.DataSource = new List<Durak>(duraklar);
            cmbHedef.DisplayMember = "Ad";
            cmbHedef.ValueMember = "Id";

            // Kart türleri
            cmbKartDurumu.Items.AddRange(new string[] { "Genel", "Öğrenci", "Yaşlı" });

        }


        private void button1_Click(object sender, EventArgs e)
        {
            // 1) Başlangıç ve hedef duraklarını al
            var secilenBaslangic = cmbBaslangic.SelectedItem as Durak;
            var secilenHedef = cmbHedef.SelectedItem as Durak;

            if (secilenBaslangic == null || secilenHedef == null)
            {
                MessageBox.Show("Lütfen hem başlangıç hem de hedef durak seçiniz (ComboBox).");
                return;
            }

            // 2) Kullanıcı harita üzerinden konum seçmişse (varsa) - İsterseniz bu kısmı da silebilirsiniz
            PointLatLng baslangic = new PointLatLng(secilenBaslangic.Enlem, secilenBaslangic.Boylam);
            PointLatLng hedef = new PointLatLng(secilenHedef.Enlem, secilenHedef.Boylam);

            if (konumServisi.BaslangicKonumu.HasValue)
                baslangic = konumServisi.BaslangicKonumu.Value;

            if (konumServisi.HedefKonumu.HasValue)
                hedef = konumServisi.HedefKonumu.Value;

            // --- BURADA eskiden tek çizgilik rota oluşturma kodu vardı, onu kaldırdık. ---

            // 3) DurakService ile graf oluştur
            DurakService ds = new DurakService();
            var durakList = ds.DuraklariOkuVeGrafOlustur();
            var durakDict = durakList.ToDictionary(d => d.Id, d => d);

            // 4) RotaHesaplayici ile tüm olası yolları bul
            RotaHesaplayici hesaplayici = new RotaHesaplayici();
            string baslangicId = secilenBaslangic.Id;
            string hedefId = secilenHedef.Id;

            var tumRotalar = hesaplayici.TumRotalariBul(durakDict, baslangicId, hedefId);

            // 5) DataGridView'de göstermek için anonim sınıf
            var gorunum = tumRotalar.Select(r => new {
                Duraklar = string.Join(" → ", r.DurakIdList),
                Ucret = r.ToplamUcret,
                Sure = r.ToplamSure,
                RotaObj = r  // DataGridView'de tıklayınca çizdirmek isterseniz
            }).ToList();

            dataGridView1.DataSource = gorunum;

            // 6) Kolon başlıklarını değiştirmek isterseniz
            if (dataGridView1.Columns.Count >= 4)
            {
                dataGridView1.Columns[0].HeaderText = "Durak Sırası";
                dataGridView1.Columns[1].HeaderText = "Toplam Ücret";
                dataGridView1.Columns[2].HeaderText = "Toplam Süre (dk)";
                dataGridView1.Columns[3].Visible = false; // RotaObj'i gizliyoruz
            }

            MessageBox.Show("Tüm alternatif yollar listelendi.");
        }


        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Başlık satırı tıklanmışsa veya satır index < 0 ise iptal
            if (e.RowIndex < 0) return;

            // Seçilen satırın DataBoundItem'ını al
            dynamic seciliSatir = dataGridView1.Rows[e.RowIndex].DataBoundItem;
            if (seciliSatir == null) return;

            // Tabloda 'RotaObj' adında bir alanımız vardı
            Rota seciliRota = seciliSatir.RotaObj as Rota;
            if (seciliRota == null) return;

            // Rota içindeki durak ID listesinden harita noktalarını oluştur
            // Durak ID -> Enlem/Boylam mapping'i yapmak için gene "durakDict" lazım.
            // (Bunu global alana veya form seviyesine ekleyebilirsiniz.)
            DurakService ds = new DurakService();
            var durakList = ds.DuraklariOkuVeGrafOlustur();
            var durakDict = durakList.ToDictionary(d => d.Id, d => d);

            // Noktaları toplayacağımız liste
            var rotaNoktalari = new List<GMap.NET.PointLatLng>();

            foreach (var durakId in seciliRota.DurakIdList)
            {
                if (durakDict.ContainsKey(durakId))
                {
                    var dr = durakDict[durakId];
                    rotaNoktalari.Add(new GMap.NET.PointLatLng(dr.Enlem, dr.Boylam));
                }
            }

            // Harita üzerine çizelim
            // 1) Overlay sıfırla
            gMapControl1.Overlays.Clear();

            // 2) Yeni rota overlay
            var rotaOverlay = new GMap.NET.WindowsForms.GMapOverlay("seciliRota");

            // 3) GMapRoute
            var cizilecekRota = new GMap.NET.WindowsForms.GMapRoute(rotaNoktalari, "rotaSecim");
            cizilecekRota.Stroke = new Pen(Color.DarkMagenta, 3);

            // 4) Marker'lar eklemek isterseniz (ilk nokta → yeşil, son nokta → kırmızı, ara noktalar → mavi)
            for (int i = 0; i < rotaNoktalari.Count; i++)
            {
                var nokta = rotaNoktalari[i];
                if (i == 0)
                    rotaOverlay.Markers.Add(new GMap.NET.WindowsForms.Markers.GMarkerGoogle(nokta, GMap.NET.WindowsForms.Markers.GMarkerGoogleType.green));
                else if (i == rotaNoktalari.Count - 1)
                    rotaOverlay.Markers.Add(new GMap.NET.WindowsForms.Markers.GMarkerGoogle(nokta, GMap.NET.WindowsForms.Markers.GMarkerGoogleType.red));
                else
                    rotaOverlay.Markers.Add(new GMap.NET.WindowsForms.Markers.GMarkerGoogle(nokta, GMap.NET.WindowsForms.Markers.GMarkerGoogleType.blue_dot));
            }

            // 5) Rota'yı overlay'e ekle
            rotaOverlay.Routes.Add(cizilecekRota);

            // 6) Ekrana yansıt
            gMapControl1.Overlays.Add(rotaOverlay);

            // Haritayı yeniden çiz
            gMapControl1.Zoom++;
            gMapControl1.Zoom--;

            // (İsteğe bağlı) Bilgi verebilirsiniz:
            MessageBox.Show($"Seçilen rota {seciliRota.DurakIdList.Count} duraktan oluşuyor.");
        }





        private void btnKonumSifirla_Click(object sender, EventArgs e)
        {

            sifirlamafonksiyonu();

        }

        private void sifirlamafonksiyonu()
        {

            // 1. Servisten seçilen konumları sıfırla
            konumServisi.ResetKonumlar();

            // 2. Haritadaki tüm çizgileri ve marker'ları sil
            gMapControl1.Overlays.Clear();

            // 3. Durakları tekrar çiz (ilk yükleme gibi)
            DurakEkle(); // veya HaritaAyarla() içinde varsa çağırın

            // 4. Harita pozisyonunu sıfırla
            gMapControl1.Position = new PointLatLng(40.76520, 29.96190); // İzmit örneği
            gMapControl1.Zoom = 13;

            
        }

    }
}
