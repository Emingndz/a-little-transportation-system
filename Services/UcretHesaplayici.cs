﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prolab_4.Models;

namespace Prolab_4.Services
{
    // 1. Ortak arayüz
    public interface IOdemeYontemi
    {
        double UcretHesapla(Yolcu yolcu, Arac arac);
    }

    // 2. Nakit Ödeme: herkes tam öder (indirim uygulanmaz)
    public class NakitOdeme : IOdemeYontemi
    {
        public double UcretHesapla(Yolcu yolcu, Arac arac)
        {
            return arac.Ucret; // indirim yok
        }
    }

    // 3. KentKart Ödeme: sisteme tanımlı indirimler geçerli
    public class KentKartOdeme : IOdemeYontemi
    {
        public double UcretHesapla(Yolcu yolcu, Arac arac)
        {
            return yolcu.IndirimliUcret(arac.Ucret); // normal sistem
        }
    }

    // 4. Kredi Kartı Ödeme: herkes %25 daha fazla öder
    public class KrediKartiOdeme : IOdemeYontemi
    {
        public double UcretHesapla(Yolcu yolcu, Arac arac)
        {
            return arac.Ucret * 1.25; // %25 fazlası
        }
    }

    // 5. Servis sınıfı (opsiyonel ama kodu temiz tutar)
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
