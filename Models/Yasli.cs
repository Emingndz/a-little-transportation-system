using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prolab_4.Core;

namespace Prolab_4.Models
{
    public class Yasli : Yolcu
    {
        public override string Tip => Constants.YOLCU_TIPI_YASLI;
        
        public override double IndirimOrani => Constants.YASLI_INDIRIM_ORANI;
    }
}
