using GMap.NET;
using GMap.NET.WindowsForms;
using System.Windows.Forms;

namespace Prolab_4.Services
{
    public class KonumSecimServisi
    {
        public PointLatLng? BaslangicKonumu { get; private set; }
        public PointLatLng? HedefKonumu { get; private set; }

        private bool baslangicSeciliyor = true; 

        public void HaritaTiklamaBagla(GMapControl harita)
        {
            // Önce varsa eski olay bağlantısını kaldır
            harita.MouseClick -= Harita_MouseClick;
            harita.MouseClick += Harita_MouseClick;
        }

        private void Harita_MouseClick(object sender, MouseEventArgs e)
        {
            var harita = sender as GMapControl;

            if (e.Button == MouseButtons.Left)
            {
                var secilenKonum = harita.FromLocalToLatLng(e.X, e.Y);

                if (baslangicSeciliyor)
                {
                    BaslangicKonumu = secilenKonum;
                    MessageBox.Show("Başlangıç konumu seçildi.");
                }
                else
                {
                    HedefKonumu = secilenKonum;
                    MessageBox.Show("Hedef konumu seçildi.");
                }

                baslangicSeciliyor = !baslangicSeciliyor;
            }
        }

        public void ResetKonumlar()
        {
            BaslangicKonumu = null;
            HedefKonumu = null;
            baslangicSeciliyor = true;
        }
    }

}
