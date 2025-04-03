using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prolab_4.Models
{
    public class Tramvay : Arac
    {
        private double sabitUcret;
        private int sabitSure;

        public Tramvay(double mesafe, double ucret, int sure)
        {
            AracTuru = "tram"; 
            this.Mesafe = mesafe;
            this.Ucret = ucret;
            this.TahminiSure = sure;
            this.sabitUcret = ucret;
            this.sabitSure = sure;
        }

        public override double UcretHesapla(double mesafe, Yolcu yolcu)
        {
            // normal bilet = sabitUcret
            double normal = sabitUcret;
            return yolcu.IndirimliUcret(sabitUcret);
        }

        public override int TahminiSüreHesapla(double mesafe)
        {
            return sabitSure;
        }
    }

}
