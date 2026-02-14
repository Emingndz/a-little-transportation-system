using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prolab_4.Core;

namespace Prolab_4.Models
{
    public class Taksi : Arac
    {
        public Taksi(double mesafe)
        {
            AracTuru = Constants.ARAC_TIPI_TAKSI;
            this.Mesafe = mesafe;
            this.Ucret = Constants.TAKSI_ACILIS_UCRETI + Constants.TAKSI_KM_UCRETI * mesafe;
            this.TahminiSure = (int)((mesafe / Constants.TAKSI_ORTALAMA_HIZ) * 60);
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
