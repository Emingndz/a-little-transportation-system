using System;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.WindowsPresentation;
using Prolab_4.Services;

namespace Prolab_4
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            HaritaAyarla();
            DurakEkle();

        }

        private DurakService durakService = new DurakService();

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

       
    }
}
