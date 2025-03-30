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
            // Örneğin mesafe = 0, TahminiSure = sure
            this.Mesafe = 0;
            this.TahminiSure = sure;
            this.Ucret = ucret;
        }

        public override double UcretHesapla(double mesafe, bool indirimli = false)
        {
            // Aktarma ücreti sabit ise:
            return Ucret;
        }

        public override int TahminiSüreHesapla(double mesafe)
        {
            return TahminiSure;
        }
    }

}
