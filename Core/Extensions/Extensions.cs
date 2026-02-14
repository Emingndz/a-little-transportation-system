using Prolab_4.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Prolab_4.Core.Extensions
{
    /// <summary>
    /// Rota koleksiyonları için extension metodları
    /// </summary>
    public static class RotaExtensions
    {
        /// <summary>
        /// En ucuz rotayı getirir
        /// </summary>
        public static Rota EnUcuzu(this IEnumerable<Rota> rotalar)
        {
            return rotalar?.OrderBy(r => r.ToplamUcret).FirstOrDefault();
        }

        /// <summary>
        /// En hızlı (kısa süreli) rotayı getirir
        /// </summary>
        public static Rota EnHizlisi(this IEnumerable<Rota> rotalar)
        {
            return rotalar?.OrderBy(r => r.ToplamSure).FirstOrDefault();
        }

        /// <summary>
        /// En az aktarmalı rotayı getirir
        /// </summary>
        public static Rota EnAzAktarmali(this IEnumerable<Rota> rotalar)
        {
            return rotalar?.OrderBy(r => r.DurakIdList.Count).FirstOrDefault();
        }

        /// <summary>
        /// Belirli ücret limitinin altındaki rotaları filtreler
        /// </summary>
        public static IEnumerable<Rota> UcretLimitiIle(this IEnumerable<Rota> rotalar, double maxUcret)
        {
            return rotalar?.Where(r => r.ToplamUcret <= maxUcret);
        }

        /// <summary>
        /// Belirli süre limitinin altındaki rotaları filtreler
        /// </summary>
        public static IEnumerable<Rota> SureLimitiIle(this IEnumerable<Rota> rotalar, int maxSureDakika)
        {
            return rotalar?.Where(r => r.ToplamSure <= maxSureDakika);
        }

        /// <summary>
        /// Rotaları ücrete göre sıralar
        /// </summary>
        public static IEnumerable<Rota> UcreeSirala(this IEnumerable<Rota> rotalar, bool azalan = false)
        {
            return azalan 
                ? rotalar?.OrderByDescending(r => r.ToplamUcret)
                : rotalar?.OrderBy(r => r.ToplamUcret);
        }

        /// <summary>
        /// Rotaları süreye göre sıralar
        /// </summary>
        public static IEnumerable<Rota> SureyeSirala(this IEnumerable<Rota> rotalar, bool azalan = false)
        {
            return azalan 
                ? rotalar?.OrderByDescending(r => r.ToplamSure)
                : rotalar?.OrderBy(r => r.ToplamSure);
        }

        /// <summary>
        /// İlk N rotayı getirir
        /// </summary>
        public static IEnumerable<Rota> IlkN(this IEnumerable<Rota> rotalar, int n)
        {
            return rotalar?.Take(n);
        }

        /// <summary>
        /// Rotada belirli bir durak var mı kontrol eder
        /// </summary>
        public static bool DurakIcerir(this Rota rota, string durakId)
        {
            return rota?.DurakIdList?.Contains(durakId) ?? false;
        }

        /// <summary>
        /// Rotanın aktarma sayısını hesaplar
        /// </summary>
        public static int AktarmaSayisi(this Rota rota)
        {
            if (rota?.DurakIdList == null || rota.DurakIdList.Count <= 2)
                return 0;
            
            return rota.DurakIdList.Count - 2;
        }
    }

    /// <summary>
    /// Durak koleksiyonları için extension metodları
    /// </summary>
    public static class DurakExtensions
    {
        /// <summary>
        /// En yakın durağı bulur
        /// </summary>
        public static Durak EnYakin(this IEnumerable<Durak> duraklar, double enlem, double boylam)
        {
            return duraklar?
                .OrderBy(d => HaversineMesafe(d.Enlem, d.Boylam, enlem, boylam))
                .FirstOrDefault();
        }

        /// <summary>
        /// Belirli mesafe içindeki durakları filtreler
        /// </summary>
        public static IEnumerable<Durak> MesafeIcinde(this IEnumerable<Durak> duraklar, 
            double enlem, double boylam, double maxMesafeKm)
        {
            return duraklar?.Where(d => 
                HaversineMesafe(d.Enlem, d.Boylam, enlem, boylam) <= maxMesafeKm);
        }

        /// <summary>
        /// Durakları türe göre filtreler
        /// </summary>
        public static IEnumerable<Durak> TureGore(this IEnumerable<Durak> duraklar, string tur)
        {
            return duraklar?.Where(d => d.Tur == tur);
        }

        /// <summary>
        /// Sadece otobüs duraklarını getirir
        /// </summary>
        public static IEnumerable<Durak> OtobusDuraklari(this IEnumerable<Durak> duraklar)
        {
            return duraklar?.TureGore(Constants.ARAC_TIPI_OTOBUS);
        }

        /// <summary>
        /// Sadece tramvay duraklarını getirir
        /// </summary>
        public static IEnumerable<Durak> TramvayDuraklari(this IEnumerable<Durak> duraklar)
        {
            return duraklar?.TureGore(Constants.ARAC_TIPI_TRAMVAY);
        }

        /// <summary>
        /// İki koordinat arasındaki Haversine mesafesini hesaplar (km)
        /// </summary>
        private static double HaversineMesafe(double lat1, double lon1, double lat2, double lon2)
        {
            double R = Constants.DUNYA_YARICAPI_KM;
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }
    }

    /// <summary>
    /// String extension metodları
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Null veya boş mu kontrol eder
        /// </summary>
        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Null, boş veya sadece boşluk mu kontrol eder
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// User node ID'si mi kontrol eder
        /// </summary>
        public static bool IsUserNode(this string durakId)
        {
            return durakId?.StartsWith("userNode_") ?? false;
        }
    }

    /// <summary>
    /// Yolcu extension metodları
    /// </summary>
    public static class YolcuExtensions
    {
        /// <summary>
        /// Yolcunun indirimli mi olduğunu kontrol eder
        /// </summary>
        public static bool IndirimliMi(this Yolcu yolcu)
        {
            return yolcu?.IndirimOrani > 0;
        }

        /// <summary>
        /// İndirim yüzdesini formatlar
        /// </summary>
        public static string IndirimYuzdesiFormatli(this Yolcu yolcu)
        {
            if (yolcu == null) return "0%";
            return $"{yolcu.IndirimOrani * 100:F0}%";
        }
    }

    /// <summary>
    /// Double extension metodları
    /// </summary>
    public static class DoubleExtensions
    {
        /// <summary>
        /// TL formatında string döndürür
        /// </summary>
        public static string ToTL(this double value)
        {
            return $"{value:F2} TL";
        }

        /// <summary>
        /// Kilometre formatında string döndürür
        /// </summary>
        public static string ToKm(this double value)
        {
            return $"{value:F2} km";
        }
    }

    /// <summary>
    /// Int extension metodları
    /// </summary>
    public static class IntExtensions
    {
        /// <summary>
        /// Dakika formatında string döndürür
        /// </summary>
        public static string ToDakika(this int value)
        {
            return $"{value} dk";
        }

        /// <summary>
        /// Dakikayı saat:dakika formatına çevirir
        /// </summary>
        public static string ToSaatDakika(this int dakika)
        {
            int saat = dakika / 60;
            int dk = dakika % 60;
            
            if (saat > 0)
                return $"{saat} saat {dk} dk";
            return $"{dk} dk";
        }
    }
}
