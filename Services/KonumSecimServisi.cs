using GMap.NET;
using GMap.NET.WindowsForms;
using Prolab_4.Models;
using System.Windows.Forms;
using System;
using System.Collections.Generic;

namespace Prolab_4.Services
{
    public class KonumSecimServisi
    {
        
        public Durak BaslangicDurak { get; set; }
        public Durak HedefDurak { get; set; }

        
        public PointLatLng? BaslangicKonumu { get; set; }
        public PointLatLng? HedefKonumu { get; set; }

        
        private bool baslangicSeciliyor = true;

        
        public void HaritaTiklamaBagla(GMapControl harita)
        {
            
            harita.MouseClick -= Harita_MouseClick;
            harita.MouseClick += Harita_MouseClick;
        }

        private void Harita_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var gmap = sender as GMapControl;
                var clicked = gmap.FromLocalToLatLng(e.X, e.Y);

                
                var ds = new DurakService();
                var duraklar = ds.DurakKonumlariniGetir();

               
                double minMesafe = double.MaxValue;
                Durak enYakinDurak = null;

                foreach (var d in duraklar)
                {
                    double dist = MesafeHesapla(clicked.Lat, clicked.Lng, d.Enlem, d.Boylam);
                    if (dist < minMesafe)
                    {
                        minMesafe = dist;
                        enYakinDurak = d;
                    }
                }

                
                double yakinlikEsigi = 0.02;

                if (minMesafe < yakinlikEsigi && enYakinDurak != null)
                {
                    
                    if (baslangicSeciliyor)
                    {
                        BaslangicDurak = enYakinDurak;
                        BaslangicKonumu = null;
                        MessageBox.Show($"Başlangıç olarak DURAK seçildi: {enYakinDurak.Ad} (yakınlık: {(minMesafe * 1000):F1} m)");
                    }
                    else
                    {
                        HedefDurak = enYakinDurak;
                        HedefKonumu = null;
                        MessageBox.Show($"Hedef olarak DURAK seçildi: {enYakinDurak.Ad} (yakınlık: {(minMesafe * 1000):F1} m)");
                    }
                }
                else
                {
                    if (baslangicSeciliyor)
                    {
                        BaslangicDurak = null;
                        BaslangicKonumu = clicked;
                        MessageBox.Show($"Başlangıç konumu harita üzerinden (durak dışı) seçildi.");
                    }
                    else
                    {
                        HedefDurak = null;
                        HedefKonumu = clicked;
                        MessageBox.Show($"Hedef konumu harita üzerinden (durak dışı) seçildi.");
                    }
                }

                baslangicSeciliyor = !baslangicSeciliyor; 
            }
        }

        
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
        private double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        public void ResetKonumlar()
        {
            BaslangicKonumu = null;
            HedefKonumu = null;
            baslangicSeciliyor = true;
        }
    }

}
