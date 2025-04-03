using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace Prolab_4.Models
{
    public class Otobus : Arac
    {
        private double sabitUcret; // normal tam bilet
        private int sabitSure;

        public Otobus(double mesafe, double ucret, int sure)
        {
            AracTuru = "bus"; 
            this.Mesafe = mesafe;
            this.Ucret = ucret;        // normal tam ucret
            this.TahminiSure = sure;   // sabit vs
            this.sabitUcret = ucret;
            this.sabitSure = sure;
        }

        public override double UcretHesapla(double mesafe, Yolcu yolcu)
        {
            // Tam bileti sabitUcret ile = "3.0 TL" gibi
            // Yolcu indirimini uygula
            double normal = sabitUcret;
            return yolcu.IndirimliUcret(normal);
        }

        public override int TahminiSüreHesapla(double mesafe)
        {
            return sabitSure;
        }
    }
}
