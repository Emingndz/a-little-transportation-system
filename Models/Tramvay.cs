using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prolab_4.Models
{
    public class Tramvay : Arac
    {
        public Tramvay(double mesafe, double ucret, int sure)
        {
            AracTuru = "tram";
            Mesafe = mesafe;
            Ucret = ucret;
            TahminiSure = sure;
        }

        public override double UcretHesapla(double mesafe, bool indirimli = false)
        {
            return indirimli ? Ucret * 0.5 : Ucret;
        }

        public override int TahminiSüreHesapla(double mesafe)
        {
            return TahminiSure;
        }
    }
}
