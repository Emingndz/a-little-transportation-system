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
            this.Ucret = 0; // yürüyüş bedava
                            // Örneğin: 4 km/saat => 15 dk / km
            this.TahminiSure = (int)(mesafe * 15);
        }

        public override double UcretHesapla(double mesafe, Yolcu yolcu)
        {
            return 0;
        }

        public override int TahminiSüreHesapla(double mesafe)
        {
            return this.TahminiSure;
        }
    }

}
