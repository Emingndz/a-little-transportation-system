using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prolab_4.Models
{
    public class Genel : Yolcu
    {
        public override string Tip => "Genel";
        public override double IndirimOrani => 0.0; // İndirim yok
    }

}
