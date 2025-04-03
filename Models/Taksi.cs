using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prolab_4.Models
{
    public class Taksi : Arac
    {
        public Taksi(double mesafe)
        {
            // PDF: 10 + 4 * mesafe
            this.Mesafe = mesafe;
            this.Ucret = 10 + 4 * mesafe;
            // Süre = km / 50 km/s => dakikaya çevir
            this.TahminiSure = (int)((mesafe / 50.0) * 60);
        }

        public override double UcretHesapla(double mesafe, Yolcu yolcu)
        {
            // Takside belki indirim yok diyebilirsiniz, 
            // yoksa: return yolcu.IndirimliUcret(this.Ucret);
            return this.Ucret;
        }

        public override int TahminiSüreHesapla(double mesafe)
        {
            return this.TahminiSure;
        }
    }

}
