using Prolab_4.Core;
using Prolab_4.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prolab_4.Services.Interfaces
{
    /// <summary>
    /// Durak işlemleri için servis interface'i.
    /// Tüm durak ile ilgili operasyonları tanımlar.
    /// 
    /// Sorumluluklar:
    /// - Durak verilerini okuma
    /// - Durak arama ve filtreleme
    /// - Kullanıcı konumu node'u oluşturma
    /// - Mesafe hesaplama
    /// </summary>
    public interface IDurakServisi
    {
        #region Senkron Metodlar (Backward Compatibility)
        
        /// <summary>
        /// Tüm durakların listesini döndürür.
        /// Sadece temel bilgileri içerir (Id, Ad, Konum, Tür).
        /// </summary>
        /// <returns>Durak listesi</returns>
        List<Durak> DuraklariGetir();
        
        /// <summary>
        /// Tüm durakları bağlantıları (graf yapısı) ile birlikte döndürür.
        /// Rota hesaplama için kullanılır.
        /// </summary>
        /// <returns>Graf yapısında durak listesi</returns>
        List<Durak> DuraklariGrafOlarakGetir();
        
        /// <summary>
        /// ID'ye göre tek bir durak döndürür.
        /// </summary>
        /// <param name="durakId">Durak ID'si</param>
        /// <returns>Bulunan durak veya null</returns>
        Durak DurakGetir(string durakId);
        
        #endregion
        
        #region Asenkron Metodlar
        
        /// <summary>
        /// Tüm durakların listesini asenkron olarak döndürür.
        /// UI thread'ini bloklamaz.
        /// </summary>
        /// <returns>Result ile sarılmış durak listesi</returns>
        Task<Result<List<Durak>>> DuraklariGetirAsync();
        
        /// <summary>
        /// Tüm durakları bağlantıları ile birlikte asenkron döndürür.
        /// </summary>
        /// <returns>Result ile sarılmış graf yapısında durak listesi</returns>
        Task<Result<List<Durak>>> DuraklariGrafOlarakGetirAsync();
        
        /// <summary>
        /// ID'ye göre tek bir durak asenkron döndürür.
        /// </summary>
        /// <param name="durakId">Durak ID'si</param>
        /// <returns>Result ile sarılmış durak</returns>
        Task<Result<Durak>> DurakGetirAsync(string durakId);
        
        #endregion
        
        #region Konum İşlemleri
        
        /// <summary>
        /// Kullanıcının bulunduğu konum için geçici bir durak node'u oluşturur.
        /// Bu node, tüm mevcut duraklara yürüme veya taksi bağlantısı ile bağlanır.
        /// </summary>
        /// <param name="enlem">Kullanıcı enlemi</param>
        /// <param name="boylam">Kullanıcı boylamı</param>
        /// <param name="mevcutDuraklar">Bağlantı kurulacak mevcut duraklar</param>
        /// <returns>Oluşturulan kullanıcı node'u</returns>
        Durak KullaniciNodeuOlustur(double enlem, double boylam, List<Durak> mevcutDuraklar);
        
        /// <summary>
        /// Belirtilen koordinata en yakın durağı bulur.
        /// </summary>
        /// <param name="enlem">Aranacak enlem</param>
        /// <param name="boylam">Aranacak boylam</param>
        /// <returns>En yakın durak</returns>
        Durak EnYakinDuragiBul(double enlem, double boylam);
        
        /// <summary>
        /// Belirtilen yarıçap içindeki durakları döndürür.
        /// </summary>
        /// <param name="enlem">Merkez enlem</param>
        /// <param name="boylam">Merkez boylam</param>
        /// <param name="yaricapKm">Arama yarıçapı (km)</param>
        /// <returns>Yarıçap içindeki duraklar</returns>
        List<Durak> YakinDuraklariGetir(double enlem, double boylam, double yaricapKm);
        
        #endregion
        
        #region Yardımcı Metodlar
        
        /// <summary>
        /// İki koordinat arasındaki mesafeyi kilometre cinsinden hesaplar.
        /// Haversine formülü kullanır.
        /// </summary>
        /// <param name="enlem1">Birinci nokta enlemi</param>
        /// <param name="boylam1">Birinci nokta boylamı</param>
        /// <param name="enlem2">İkinci nokta enlemi</param>
        /// <param name="boylam2">İkinci nokta boylamı</param>
        /// <returns>Mesafe (km)</returns>
        double MesafeHesapla(double enlem1, double boylam1, double enlem2, double boylam2);
        
        /// <summary>
        /// Önbelleği temizler ve verileri yeniden yükler.
        /// </summary>
        void OnbellekTemizle();
        
        #endregion
    }
}
