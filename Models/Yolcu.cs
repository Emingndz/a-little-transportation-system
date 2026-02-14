using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prolab_4.Models
{
    public abstract class Yolcu
    {
        public abstract string Tip { get; }

        public abstract double IndirimOrani { get; }

        public double IndirimliUcret(double normalUcret)
        {
            return normalUcret * (1.0 - IndirimOrani);
        }
    }

}
