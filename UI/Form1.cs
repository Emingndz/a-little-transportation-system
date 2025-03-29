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
            gMapControl1.MapProvider = GMapProviders.OpenStreetMap; // OSM Harita Sa�lay�c�s�
            GMaps.Instance.Mode = AccessMode.ServerAndCache; // Sunucu ve �nbellekten y�kle
            gMapControl1.Position = new PointLatLng(40.7655, 29.9400); // �zmit, Kocaeli ba�lang�� noktas�

            // Yak�nla�t�rma ayarlar�
            gMapControl1.MinZoom = 1;
            gMapControl1.MaxZoom = 18;
            gMapControl1.Zoom = 12;

            // Fare ile zoom a�ma
            gMapControl1.MouseWheelZoomEnabled = true;
            gMapControl1.MouseWheelZoomType = MouseWheelZoomType.ViewCenter;

            // Harita s�r�kleme ayarlar�
            gMapControl1.CanDragMap = true;
            gMapControl1.DragButton = MouseButtons.Left;
            gMapControl1.ShowCenter = false;
        }

        private void comboboxdurakekleme()
        {
            var duraklar = durakService.DurakKonumlariniGetir();

            // ComboBox'lara durak adlar�n� y�kle
            cmbBaslangic.DataSource = new List<Durak>(duraklar);
            cmbBaslangic.DisplayMember = "Ad";
            cmbBaslangic.ValueMember = "Id";

            cmbHedef.DataSource = new List<Durak>(duraklar);
            cmbHedef.DisplayMember = "Ad";
            cmbHedef.ValueMember = "Id";

            // Kart t�rleri
            cmbKartDurumu.Items.AddRange(new string[] { "Genel", "��renci", "Ya�l�" });

        }


        private void button1_Click(object sender, EventArgs e)
        {
            PointLatLng baslangic;
            PointLatLng hedef;

            // Varsay�lan olarak ComboBox�tan al�n�r
            var secilenBaslangic = cmbBaslangic.SelectedItem as Durak;
            var secilenHedef = cmbHedef.SelectedItem as Durak;

            if (secilenBaslangic == null || secilenHedef == null)
            {
                MessageBox.Show("L�tfen hem ba�lang�� hem de hedef durak se�iniz (ComboBox).");
                return;
            }

            baslangic = new PointLatLng(secilenBaslangic.Enlem, secilenBaslangic.Boylam);
            hedef = new PointLatLng(secilenHedef.Enlem, secilenHedef.Boylam);

            // E�er kullan�c� haritadan se�im yapt�ysa, �ncelikli olarak onu kullan
            if (konumServisi.BaslangicKonumu.HasValue)
                baslangic = konumServisi.BaslangicKonumu.Value;

            if (konumServisi.HedefKonumu.HasValue)
                hedef = konumServisi.HedefKonumu.Value;

            // Rota noktalar�
            List<PointLatLng> rotaNoktalari = new List<PointLatLng> { baslangic, hedef };

            // Yeni rota overlay olu�tur
            var rotaOverlay = new GMap.NET.WindowsForms.GMapOverlay("rota");
            var rota = new GMap.NET.WindowsForms.GMapRoute(rotaNoktalari, "geciciRota");
            rota.Stroke = new Pen(Color.DarkOrange, 3);

            // Marker'lar� ekle
            rotaOverlay.Markers.Add(new GMap.NET.WindowsForms.Markers.GMarkerGoogle(baslangic, GMap.NET.WindowsForms.Markers.GMarkerGoogleType.green));
            rotaOverlay.Markers.Add(new GMap.NET.WindowsForms.Markers.GMarkerGoogle(hedef, GMap.NET.WindowsForms.Markers.GMarkerGoogleType.red));

            // Rota �izgisi ekle
            rotaOverlay.Routes.Add(rota);

            // �nceki t�m overlay'leri temizle (duraklar dahilse onlar� yeniden eklemelisiniz!)
            gMapControl1.Overlays.Clear();

            // Yeni rota overlay'i ekle
            gMapControl1.Overlays.Add(rotaOverlay);

            // Haritay� yeniden �izdir (zorla refresh)
            gMapControl1.Zoom++;
            gMapControl1.Zoom--;

            // Se�ilen konumlar� ekrana yaz (test ama�l�)
            Console.WriteLine($"Ba�lang��: {baslangic.Lat}, {baslangic.Lng}");
            Console.WriteLine($"Hedef: {hedef.Lat}, {hedef.Lng}");

            MessageBox.Show("Ba�lang�� ve hedef aras�nda �izgi �izildi.");
        }





        private void btnKonumSifirla_Click(object sender, EventArgs e)
        {

            sifirlamafonksiyonu();

        }

        private void sifirlamafonksiyonu()
        {

            // 1. Servisten se�ilen konumlar� s�f�rla
            konumServisi.ResetKonumlar();

            // 2. Haritadaki t�m �izgileri ve marker'lar� sil
            gMapControl1.Overlays.Clear();

            // 3. Duraklar� tekrar �iz (ilk y�kleme gibi)
            DurakEkle(); // veya HaritaAyarla() i�inde varsa �a��r�n

            // 4. Harita pozisyonunu s�f�rla
            gMapControl1.Position = new PointLatLng(40.76520, 29.96190); // �zmit �rne�i
            gMapControl1.Zoom = 13;

            
        }

    }
}
