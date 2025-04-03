using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prolab_4.Models
{
    public class Ogrenci : Yolcu
    {
        public override string Tip => "Öğrenci";
        // Örnek: %50 indirim
        public override double IndirimOrani => 0.5;
    }

}
