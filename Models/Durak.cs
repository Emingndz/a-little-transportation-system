using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prolab_4.Models
{
    public class Durak
    {
        public string Id { get; set; }
        public string Ad { get; set; }
        public string Tur { get; set; }  // bus veya tram
        public double Enlem { get; set; }
        public double Boylam { get; set; }
        public bool SonDurak { get; set; }

        public List<DurakBaglantisi> Baglantilar { get; set; } = new List<DurakBaglantisi>(); 


    }
}
