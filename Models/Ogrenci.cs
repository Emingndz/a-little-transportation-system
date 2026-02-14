using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prolab_4.Core;

namespace Prolab_4.Models
{
    public class Ogrenci : Yolcu
    {
        public override string Tip => Constants.YOLCU_TIPI_OGRENCI;
       
        public override double IndirimOrani => Constants.OGRENCI_INDIRIM_ORANI;
    }
}
