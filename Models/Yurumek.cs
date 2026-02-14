using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prolab_4.Core;

namespace Prolab_4.Models
{
    public class Yurumek : Arac
    {
        public Yurumek(double mesafe)
        {
            AracTuru = Constants.ARAC_TIPI_YURUME;
            this.Mesafe = mesafe;
            this.Ucret = 0; 
            this.TahminiSure = (int)(mesafe * Constants.YURUME_DAKIKA_PER_KM);
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
