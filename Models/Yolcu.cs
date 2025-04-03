using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prolab_4.Models
{
    public abstract class Yolcu
    {
        // Hangi tip kullanıcı: Öğrenci, 65+, Genel vb.
        public abstract string Tip { get; }

        // Otobüs ve tramvay ücretlerini hesaplarken kullanacağımız indirim
        // 0.0 => %0 indirim, 0.5 => %50 indirim, 1.0 => %100 indirim
        public abstract double IndirimOrani { get; }

        // Uygulama esnasında belki kimlik, yaş, vs. girebilirsiniz
        // public int Yas {get;set;} vb.

        // İndirimli ücreti hesaplarken kısayol fonksiyonu
        public double IndirimliUcret(double normalUcret)
        {
            return normalUcret * (1.0 - IndirimOrani);
        }
    }

}
