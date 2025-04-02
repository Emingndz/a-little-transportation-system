using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prolab_4.Models
{
    public class Yurumek : Arac
    {
        public Yurumek(double mesafe)
        {
            this.Mesafe = mesafe;
            // yürüyüş ücreti = 0
            this.Ucret = 0;
            // süre (örnek): 4 km/saat = 15 dk/ km
            this.TahminiSure = (int)(mesafe * 15);
        }

        public override double UcretHesapla(double mesafe, bool indirimli = false)
        {
            return 0;
        }

        public override int TahminiSüreHesapla(double mesafe)
        {
            return (int)(mesafe * 15);
        }
    }
}
