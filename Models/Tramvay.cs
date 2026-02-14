using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prolab_4.Core;

namespace Prolab_4.Models
{
    public class Tramvay : Arac
    {
        private double sabitUcret;
        private int sabitSure;

        public Tramvay(double mesafe, double ucret, int sure)
        {
            AracTuru = Constants.ARAC_TIPI_TRAMVAY; 
            this.Mesafe = mesafe;
            this.Ucret = ucret;
            this.TahminiSure = sure;
            this.sabitUcret = ucret;
            this.sabitSure = sure;
        }

        public override double UcretHesapla(double mesafe, Yolcu yolcu)
        {
            
            double normal = sabitUcret;
            return yolcu.IndirimliUcret(sabitUcret);
        }

        public override int TahminiSüreHesapla(double mesafe)
        {
            return sabitSure;
        }
    }

}
