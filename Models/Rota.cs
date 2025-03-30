using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prolab_4.Models
{
    public class Rota
    {
        public List<string> DurakIdList { get; set; } = new List<string>();
        public double ToplamUcret { get; set; }
        public int ToplamSure { get; set; }
    }

}
