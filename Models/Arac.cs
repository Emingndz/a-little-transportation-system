using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prolab_4.Models
{
    public abstract class Arac
    {
        public string AracTuru { get; protected set; } // "bus", "tram", "taxi"
        public double Ucret { get; protected set; } // Araçla seyahat ücreti (varsayılan)
        public int TahminiSure { get; protected set; } // Dakika cinsinden
        public double Mesafe { get; protected set; } // Kilometre cinsinden

        // Ortak davranışlar
        public abstract double UcretHesapla(double mesafe, bool indirimli = false);
        public abstract int TahminiSüreHesapla(double mesafe);
    }
}
