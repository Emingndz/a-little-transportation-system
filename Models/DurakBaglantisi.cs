using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prolab_4.Models
{
    public class DurakBaglantisi
    {
        public string HedefDurakId { get; set; }  // Hangi durağa gidiliyor
        public Arac Arac { get; set; }            // Hangi araçla (otobüs/tramvay), süre, ücret, mesafe
    }

}
