using GMap.NET;
using GMap.NET.WindowsForms;
using Prolab_4.Models;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Prolab_4.Services.Interfaces
{
    /// <summary>
    /// Harita işlemleri için servis interface'i.
    /// GMap.NET ile etkileşimi soyutlar.
    /// 
    /// Sorumluluklar:
    /// - Harita yapılandırma
    /// - Marker yönetimi
    /// - Rota çizimi
    /// - Konum seçimi
    /// </summary>
    public interface IHaritaServisi
    {
        #region Harita Yapılandırma
        
        /// <summary>
        /// Harita kontrolünü başlangıç ayarları ile yapılandırır.
        /// </summary>
        /// <param name="haritaKontrol">GMapControl instance</param>
        void HaritaYapilandir(GMapControl haritaKontrol);
        
        /// <summary>
        /// Haritayı belirtilen konuma ortalayıp zoom yapar.
        /// </summary>
        /// <param name="enlem">Merkez enlem</param>
        /// <param name="boylam">Merkez boylam</param>
        /// <param name="zoom">Zoom seviyesi (opsiyonel)</param>
        void HaritaOrtala(double enlem, double boylam, int? zoom = null);
        
        /// <summary>
        /// Haritayı varsayılan konuma (İzmit merkez) sıfırlar.
        /// </summary>
        void HaritaSifirla();
        
        #endregion
        
        #region Marker İşlemleri
        
        /// <summary>
        /// Tüm durakları haritaya marker olarak ekler.
        /// </summary>
        /// <param name="duraklar">Eklenecek duraklar</param>
        void DurakMarkerlariniEkle(List<Durak> duraklar);
        
        /// <summary>
        /// Tek bir durak marker'ı ekler.
        /// </summary>
        /// <param name="durak">Eklenecek durak</param>
        /// <param name="markerTipi">Marker tipi</param>
        void DurakMarkerEkle(Durak durak, HaritaMarkerTipi markerTipi = HaritaMarkerTipi.Normal);
        
        /// <summary>
        /// Kullanıcı konumu için özel marker ekler.
        /// </summary>
        /// <param name="enlem">Konum enlemi</param>
        /// <param name="boylam">Konum boylamı</param>
        /// <param name="aciklama">Marker açıklaması</param>
        void KullaniciMarkerEkle(double enlem, double boylam, string aciklama);
        
        /// <summary>
        /// Tüm marker ve overlay'leri temizler.
        /// </summary>
        void MarkerlarTemizle();
        
        #endregion
        
        #region Rota Çizimi
        
        /// <summary>
        /// Seçilen rotayı haritaya çizer.
        /// Farklı araç türleri farklı renklerde gösterilir.
        /// </summary>
        /// <param name="rota">Çizilecek rota</param>
        /// <param name="durakDict">Durak bilgilerini içeren dictionary</param>
        void RotaCiz(Rota rota, Dictionary<string, Durak> durakDict);
        
        /// <summary>
        /// Birden fazla rotayı karşılaştırmalı olarak çizer.
        /// Her rota farklı stil ile gösterilir.
        /// </summary>
        /// <param name="rotalar">Çizilecek rotalar</param>
        /// <param name="durakDict">Durak bilgilerini içeren dictionary</param>
        void RotalarCiz(List<Rota> rotalar, Dictionary<string, Durak> durakDict);
        
        /// <summary>
        /// Araç türüne göre segment rengi döndürür.
        /// </summary>
        /// <param name="arac">Araç tipi</param>
        /// <returns>Segment rengi</returns>
        Color AracRengiGetir(Arac arac);
        
        /// <summary>
        /// Rota çizimini temizler.
        /// </summary>
        void RotaTemizle();
        
        #endregion
        
        #region Konum Seçimi
        
        /// <summary>
        /// Harita tıklama olaylarını dinlemeye başlar.
        /// Kullanıcının harita üzerinden konum seçmesini sağlar.
        /// </summary>
        /// <param name="callback">Konum seçildiğinde çağrılacak callback</param>
        void KonumSeciminiBaslat(Action<KonumSecimSonucu> callback);
        
        /// <summary>
        /// Konum seçim modunu durdurur.
        /// </summary>
        void KonumSeciminiDurdur();
        
        /// <summary>
        /// Mevcut konum seçimlerini sıfırlar.
        /// </summary>
        void KonumSecimleriniSifirla();
        
        /// <summary>
        /// Seçili başlangıç konumunu döndürür.
        /// </summary>
        KonumSecimSonucu BaslangicKonumu { get; }
        
        /// <summary>
        /// Seçili hedef konumunu döndürür.
        /// </summary>
        KonumSecimSonucu HedefKonumu { get; }
        
        #endregion
        
        #region Olaylar
        
        /// <summary>
        /// Konum seçildiğinde tetiklenir.
        /// </summary>
        event EventHandler<KonumSecimEventArgs> KonumSecildi;
        
        /// <summary>
        /// Harita tıklandığında tetiklenir.
        /// </summary>
        event EventHandler<HaritaTiklamaEventArgs> HaritaTiklandi;
        
        #endregion
    }
    
    #region Yardımcı Tipler
    
    /// <summary>
    /// Harita marker tipleri
    /// </summary>
    public enum HaritaMarkerTipi
    {
        /// <summary>Normal durak marker'ı</summary>
        Normal,
        
        /// <summary>Başlangıç noktası (yeşil)</summary>
        Baslangic,
        
        /// <summary>Bitiş noktası (kırmızı)</summary>
        Bitis,
        
        /// <summary>Ara durak (mavi)</summary>
        AraDurak,
        
        /// <summary>Kullanıcı konumu (sarı)</summary>
        KullaniciKonumu,
        
        /// <summary>Otobüs durağı</summary>
        Otobus,
        
        /// <summary>Tramvay durağı</summary>
        Tramvay
    }
    
    /// <summary>
    /// Konum seçim sonucu
    /// </summary>
    public class KonumSecimSonucu
    {
        /// <summary>Seçilen enlem</summary>
        public double Enlem { get; set; }
        
        /// <summary>Seçilen boylam</summary>
        public double Boylam { get; set; }
        
        /// <summary>Durak mı yoksa serbest konum mu?</summary>
        public bool DurakMi { get; set; }
        
        /// <summary>Eğer durak ise, durak bilgisi</summary>
        public Durak Durak { get; set; }
        
        /// <summary>En yakın durağa olan mesafe (km)</summary>
        public double EnYakinDurakMesafesi { get; set; }
        
        /// <summary>Seçim türü (başlangıç/hedef)</summary>
        public KonumSecimTuru Tur { get; set; }
    }
    
    /// <summary>
    /// Konum seçim türleri
    /// </summary>
    public enum KonumSecimTuru
    {
        /// <summary>Başlangıç noktası seçimi</summary>
        Baslangic,
        
        /// <summary>Hedef noktası seçimi</summary>
        Hedef
    }
    
    /// <summary>
    /// Konum seçildi event argümanları
    /// </summary>
    public class KonumSecimEventArgs : EventArgs
    {
        public KonumSecimSonucu Sonuc { get; set; }
    }
    
    /// <summary>
    /// Harita tıklama event argümanları
    /// </summary>
    public class HaritaTiklamaEventArgs : EventArgs
    {
        public double Enlem { get; set; }
        public double Boylam { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }
    
    #endregion
}
