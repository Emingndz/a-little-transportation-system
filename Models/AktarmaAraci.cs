using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prolab_4.Models
{
    public class AktarmaAraci : Arac
    {
        public AktarmaAraci(int sure, double ucret)
        {
            this.Mesafe = 0;
            this.TahminiSure = sure;
            this.Ucret = ucret;
        }

        // Imza: Arac'taki double UcretHesapla(double, Yolcu) ile aynı
        public override double UcretHesapla(double mesafe, Yolcu yolcu)
        {
            // Aktarma ücreti sabit ise mesafe = 0
            // Yolcu tipine göre indirim (isterseniz):
            // return yolcu.IndirimliUcret(Ucret);

            return this.Ucret;
        }

        public override int TahminiSüreHesapla(double mesafe)
        {
            return this.TahminiSure;
        }
    }

}
