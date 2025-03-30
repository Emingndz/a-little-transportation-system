using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prolab_4.Models
{
    public class Taksi : Arac
    {
        private double acilisUcreti = 10.0;
        private double kmBasiUcret = 4.0;

        public Taksi(double mesafe)
        {
            // AracTuru = "taxi"; (isterseniz)
            this.Mesafe = mesafe;
            // Süreyi ortalama 50 km/s gibi hesaplayabilirsiniz (ya da sabit)
            this.TahminiSure = (int)(mesafe / 50.0 * 60); // km/50 * 60 => dakika
            this.Ucret = UcretHesapla(mesafe);
        }

        public override double UcretHesapla(double mesafe, bool indirimli = false)
        {
            // Taksilerde indirim yoksa:
            return acilisUcreti + (kmBasiUcret * mesafe);
        }

        public override int TahminiSüreHesapla(double mesafe)
        {
            return (int)(mesafe / 50.0 * 60);
        }
    }

}
