using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prolab_4.Core;

namespace Prolab_4.Models
{
    public class AktarmaAraci : Arac
    {
        public AktarmaAraci(int sure, double ucret)
        {
            AracTuru = Constants.ARAC_TIPI_AKTARMA;
            this.Mesafe = 0;
            this.TahminiSure = sure;
            this.Ucret = ucret;
        }

       
        public override double UcretHesapla(double mesafe, Yolcu yolcu)
        {
           

            return this.Ucret;
        }

        public override int TahminiSüreHesapla(double mesafe)
        {
            return this.TahminiSure;
        }
    }

}
