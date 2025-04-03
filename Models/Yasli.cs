using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prolab_4.Models
{
    public class Yasli : Yolcu
    {
        public override string Tip => "65+";
        // PDF'e göre, %100 ücretsiz yapmak isterseniz => 1.0
        // Yarı yarıya isterseniz => 0.5
        public override double IndirimOrani => 1.0;
    }

}
