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
            gMapControl1.MapProvider = GMapProviders.OpenStreetMap; // OSM Harita Saðlayýcýsý
            GMaps.Instance.Mode = AccessMode.ServerAndCache; // Sunucu ve önbellekten yükle
            gMapControl1.Position = new PointLatLng(40.7655, 29.9400); // Ýzmit, Kocaeli baþlangýç noktasý

            // Yakýnlaþtýrma ayarlarý
            gMapControl1.MinZoom = 1;
            gMapControl1.MaxZoom = 18;
            gMapControl1.Zoom = 12;

            // Fare ile zoom açma
            gMapControl1.MouseWheelZoomEnabled = true;
            gMapControl1.MouseWheelZoomType = MouseWheelZoomType.ViewCenter;

            // Harita sürükleme ayarlarý
            gMapControl1.CanDragMap = true;
            gMapControl1.DragButton = MouseButtons.Left;
            gMapControl1.ShowCenter = false;
        }

        private void comboboxdurakekleme()
        {
            var duraklar = durakService.DurakKonumlariniGetir();

            // ComboBox'lara durak adlarýný yükle
            cmbBaslangic.DataSource = new List<Durak>(duraklar);
            cmbBaslangic.DisplayMember = "Ad";
            cmbBaslangic.ValueMember = "Id";

            cmbHedef.DataSource = new List<Durak>(duraklar);
            cmbHedef.DisplayMember = "Ad";
            cmbHedef.ValueMember = "Id";

            // Kart türleri
            cmbKartDurumu.Items.AddRange(new string[] { "Genel", "Öðrenci", "Yaþlý" });

        }


        private void button1_Click(object sender, EventArgs e)
        {
            PointLatLng baslangic;
            PointLatLng hedef;

            // Varsayýlan olarak ComboBox’tan alýnýr
            var secilenBaslangic = cmbBaslangic.SelectedItem as Durak;
            var secilenHedef = cmbHedef.SelectedItem as Durak;

            if (secilenBaslangic == null || secilenHedef == null)
            {
                MessageBox.Show("Lütfen hem baþlangýç hem de hedef durak seçiniz (ComboBox).");
                return;
            }

            baslangic = new PointLatLng(secilenBaslangic.Enlem, secilenBaslangic.Boylam);
            hedef = new PointLatLng(secilenHedef.Enlem, secilenHedef.Boylam);

            // Eðer kullanýcý haritadan seçim yaptýysa, öncelikli olarak onu kullan
            if (konumServisi.BaslangicKonumu.HasValue)
                baslangic = konumServisi.BaslangicKonumu.Value;

            if (konumServisi.HedefKonumu.HasValue)
                hedef = konumServisi.HedefKonumu.Value;

            // Rota noktalarý
            List<PointLatLng> rotaNoktalari = new List<PointLatLng> { baslangic, hedef };

            // Yeni rota overlay oluþtur
            var rotaOverlay = new GMap.NET.WindowsForms.GMapOverlay("rota");
            var rota = new GMap.NET.WindowsForms.GMapRoute(rotaNoktalari, "geciciRota");
            rota.Stroke = new Pen(Color.DarkOrange, 3);

            // Marker'larý ekle
            rotaOverlay.Markers.Add(new GMap.NET.WindowsForms.Markers.GMarkerGoogle(baslangic, GMap.NET.WindowsForms.Markers.GMarkerGoogleType.green));
            rotaOverlay.Markers.Add(new GMap.NET.WindowsForms.Markers.GMarkerGoogle(hedef, GMap.NET.WindowsForms.Markers.GMarkerGoogleType.red));

            // Rota çizgisi ekle
            rotaOverlay.Routes.Add(rota);

            // Önceki tüm overlay'leri temizle (duraklar dahilse onlarý yeniden eklemelisiniz!)
            gMapControl1.Overlays.Clear();

            // Yeni rota overlay'i ekle
            gMapControl1.Overlays.Add(rotaOverlay);

            // Haritayý yeniden çizdir (zorla refresh)
            gMapControl1.Zoom++;
            gMapControl1.Zoom--;

            // Seçilen konumlarý ekrana yaz (test amaçlý)
            Console.WriteLine($"Baþlangýç: {baslangic.Lat}, {baslangic.Lng}");
            Console.WriteLine($"Hedef: {hedef.Lat}, {hedef.Lng}");

            MessageBox.Show("Baþlangýç ve hedef arasýnda çizgi çizildi.");
        }





        private void btnKonumSifirla_Click(object sender, EventArgs e)
        {

            sifirlamafonksiyonu();

        }

        private void sifirlamafonksiyonu()
        {

            // 1. Servisten seçilen konumlarý sýfýrla
            konumServisi.ResetKonumlar();

            // 2. Haritadaki tüm çizgileri ve marker'larý sil
            gMapControl1.Overlays.Clear();

            // 3. Duraklarý tekrar çiz (ilk yükleme gibi)
            DurakEkle(); // veya HaritaAyarla() içinde varsa çaðýrýn

            // 4. Harita pozisyonunu sýfýrla
            gMapControl1.Position = new PointLatLng(40.76520, 29.96190); // Ýzmit örneði
            gMapControl1.Zoom = 13;

            
        }

    }
}
