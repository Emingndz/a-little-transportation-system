using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prolab_4.Models;

namespace Prolab_4.Services
{
    
    public interface IOdemeYontemi
    {
        double UcretHesapla(Yolcu yolcu, Arac arac);
    }

   
    public class NakitOdeme : IOdemeYontemi
    {
        public double UcretHesapla(Yolcu yolcu, Arac arac)
        {
            return arac.Ucret; 
        }
    }

    
    public class KentKartOdeme : IOdemeYontemi
    {
        public double UcretHesapla(Yolcu yolcu, Arac arac)
        {
            return yolcu.IndirimliUcret(arac.Ucret);
        }
    }

    
    public class KrediKartiOdeme : IOdemeYontemi
    {
        public double UcretHesapla(Yolcu yolcu, Arac arac)
        {
            return arac.Ucret * 1.25; 
        }
    }

    
    public class OdemeServisi
    {
        private readonly IOdemeYontemi odemeYontemi;

        public OdemeServisi(IOdemeYontemi odemeYontemi)
        {
            this.odemeYontemi = odemeYontemi;
        }

        public double Hesapla(Yolcu yolcu, Arac arac)
        {
            return odemeYontemi.UcretHesapla(yolcu, arac);
        }
    }
}
