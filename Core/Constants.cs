namespace Prolab_4.Core
{
    /// <summary>
    /// Uygulama genelinde kullanılan sabit değerler.
    /// Magic number'ları ortadan kaldırır ve merkezi yönetim sağlar.
    /// </summary>
    public static class Constants
    {
        #region Taksi Ayarları
        /// <summary>Taksi açılış ücreti (TL)</summary>
        public const double TAKSI_ACILIS_UCRETI = 10.0;
        
        /// <summary>Taksi kilometre başı ücret (TL/km)</summary>
        public const double TAKSI_KM_UCRETI = 4.0;
        
        /// <summary>Taksi ortalama hızı (km/saat)</summary>
        public const double TAKSI_ORTALAMA_HIZ = 50.0;
        #endregion

        #region Yürüme Ayarları
        /// <summary>Maksimum yürüme mesafesi (km) - bunun üstünde taksi önerilir</summary>
        public const double MAX_YURUME_MESAFESI = 3.0;
        
        /// <summary>Yürüme hızı (dakika/km)</summary>
        public const double YURUME_DAKIKA_PER_KM = 15.0;
        #endregion

        #region İndirim Oranları
        /// <summary>Öğrenci indirim oranı (%50)</summary>
        public const double OGRENCI_INDIRIM_ORANI = 0.5;
        
        /// <summary>Yaşlı indirim oranı (%30)</summary>
        public const double YASLI_INDIRIM_ORANI = 0.3;
        
        /// <summary>Genel yolcu indirim oranı (%0)</summary>
        public const double GENEL_INDIRIM_ORANI = 0.0;
        #endregion

        #region Ödeme Yöntemi Ek Ücretleri
        /// <summary>Kredi kartı ek ücret çarpanı (%25 ek)</summary>
        public const double KREDI_KARTI_CARPANI = 1.25;
        #endregion

        #region Harita Ayarları
        /// <summary>Varsayılan harita merkezi - Enlem (İzmit)</summary>
        public const double HARITA_VARSAYILAN_ENLEM = 40.7655;
        
        /// <summary>Varsayılan harita merkezi - Boylam (İzmit)</summary>
        public const double HARITA_VARSAYILAN_BOYLAM = 29.9400;
        
        /// <summary>Varsayılan harita zoom seviyesi</summary>
        public const int HARITA_VARSAYILAN_ZOOM = 12;
        
        /// <summary>Minimum zoom seviyesi</summary>
        public const int HARITA_MIN_ZOOM = 1;
        
        /// <summary>Maximum zoom seviyesi</summary>
        public const int HARITA_MAX_ZOOM = 18;
        #endregion

        #region Dosya Yolları
        /// <summary>Veri seti JSON dosya yolu</summary>
        public const string VERISETI_DOSYA_YOLU = @"Data/veriseti.json";
        
        /// <summary>Arkaplan resmi dosya yolu</summary>
        public const string ARKAPLAN_RESIM_YOLU = @"Resource\Arkaplan.png";
        #endregion

        #region Algoritma Ayarları
        /// <summary>DFS maksimum derinlik (sonsuz döngü önleme)</summary>
        public const int MAX_DFS_DERINLIK = 50;
        
        /// <summary>Maksimum rota sayısı (bellek optimizasyonu)</summary>
        public const int MAX_ROTA_SAYISI = 100;
        #endregion

        #region Dünya Sabitleri
        /// <summary>Dünya yarıçapı (km) - Haversine formülü için</summary>
        public const double DUNYA_YARICAPI_KM = 6371.0;
        #endregion

        #region Araç Tipleri
        public const string ARAC_TIPI_OTOBUS = "bus";
        public const string ARAC_TIPI_TRAMVAY = "tram";
        public const string ARAC_TIPI_TAKSI = "taxi";
        public const string ARAC_TIPI_AKTARMA = "transfer";
        public const string ARAC_TIPI_YURUME = "walk";
        #endregion

        #region Yolcu Tipleri
        public const string YOLCU_TIPI_OGRENCI = "Öğrenci";
        public const string YOLCU_TIPI_YASLI = "Yaşlı";
        public const string YOLCU_TIPI_GENEL = "Genel";
        #endregion
    }
}
