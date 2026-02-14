using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prolab_4.Models
{
    public abstract class Arac
    {
        public string AracTuru { get; protected set; } 
        public double Ucret { get; protected set; }
        public int TahminiSure { get; protected set; } 
        public double Mesafe { get; protected set; } 

        

        public abstract double UcretHesapla(double mesafe, Yolcu yolcu);

        public abstract int TahminiSüreHesapla(double mesafe);
    }
}
