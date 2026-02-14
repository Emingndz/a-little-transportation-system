using Prolab_4.Core;
using Prolab_4.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prolab_4.Services.Interfaces
{
    /// <summary>
    /// Rota hesaplama işlemleri için servis interface'i.
    /// Farklı algoritma stratejilerini destekler.
    /// 
    /// Sorumluluklar:
    /// - En kısa (süre) rota hesaplama
    /// - En ucuz rota hesaplama
    /// - Alternatif rota listeleme
    /// - Rota filtreleme ve sıralama
    /// </summary>
    public interface IRotaServisi
    {
        #region Temel Rota Hesaplama
        
        /// <summary>
        /// En kısa süreli rotayı hesaplar.
        /// Dijkstra algoritması kullanır.
        /// </summary>
        /// <param name="baslangicId">Başlangıç durak ID'si</param>
        /// <param name="hedefId">Hedef durak ID'si</param>
        /// <param name="yolcu">Yolcu tipi (indirim için)</param>
        /// <param name="odemeYontemi">Ödeme yöntemi</param>
        /// <returns>En kısa rota veya null</returns>
        Rota EnKisaRotaHesapla(string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odemeYontemi);
        
        /// <summary>
        /// En ucuz rotayı hesaplar.
        /// Dijkstra algoritması kullanır.
        /// </summary>
        /// <param name="baslangicId">Başlangıç durak ID'si</param>
        /// <param name="hedefId">Hedef durak ID'si</param>
        /// <param name="yolcu">Yolcu tipi (indirim için)</param>
        /// <param name="odemeYontemi">Ödeme yöntemi</param>
        /// <returns>En ucuz rota veya null</returns>
        Rota EnUcuzRotaHesapla(string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odemeYontemi);
        
        /// <summary>
        /// Tüm alternatif rotaları hesaplar.
        /// DFS algoritması kullanır, performans için sınırlandırılmıştır.
        /// </summary>
        /// <param name="baslangicId">Başlangıç durak ID'si</param>
        /// <param name="hedefId">Hedef durak ID'si</param>
        /// <param name="yolcu">Yolcu tipi (indirim için)</param>
        /// <param name="odemeYontemi">Ödeme yöntemi</param>
        /// <returns>Tüm alternatif rotalar</returns>
        List<Rota> TumRotalariHesapla(string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odemeYontemi);
        
        #endregion
        
        #region Asenkron Rota Hesaplama
        
        /// <summary>
        /// En kısa süreli rotayı asenkron hesaplar.
        /// UI thread'ini bloklamaz.
        /// </summary>
        Task<Result<Rota>> EnKisaRotaHesaplaAsync(string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odemeYontemi);
        
        /// <summary>
        /// En ucuz rotayı asenkron hesaplar.
        /// </summary>
        Task<Result<Rota>> EnUcuzRotaHesaplaAsync(string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odemeYontemi);
        
        /// <summary>
        /// Tüm alternatif rotaları asenkron hesaplar.
        /// </summary>
        Task<Result<List<Rota>>> TumRotalariHesaplaAsync(string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odemeYontemi);
        
        #endregion
        
        #region Gelişmiş Rota İşlemleri
        
        /// <summary>
        /// Rotaları belirtilen kritere göre sıralar.
        /// </summary>
        /// <param name="rotalar">Sıralanacak rotalar</param>
        /// <param name="kriter">Sıralama kriteri</param>
        /// <param name="azalan">True ise büyükten küçüğe</param>
        /// <returns>Sıralanmış rotalar</returns>
        List<Rota> RotalariSirala(List<Rota> rotalar, RotaSiralamaKriteri kriter, bool azalan = false);
        
        /// <summary>
        /// Rotaları belirtilen filtrelere göre filtreler.
        /// </summary>
        /// <param name="rotalar">Filtrelenecek rotalar</param>
        /// <param name="filtre">Filtre kriterleri</param>
        /// <returns>Filtrelenmiş rotalar</returns>
        List<Rota> RotalariFiltrele(List<Rota> rotalar, RotaFiltresi filtre);
        
        /// <summary>
        /// İki rota arasındaki farkları karşılaştırır.
        /// </summary>
        /// <param name="rota1">Birinci rota</param>
        /// <param name="rota2">İkinci rota</param>
        /// <returns>Karşılaştırma sonucu</returns>
        RotaKarsilastirma RotalariKarsilastir(Rota rota1, Rota rota2);
        
        #endregion
        
        #region Durak Dict Yönetimi
        
        /// <summary>
        /// Mevcut durak dictionary'sini günceller.
        /// Kullanıcı node'u eklendiğinde çağrılır.
        /// </summary>
        /// <param name="durakDict">Güncel durak dictionary</param>
        void DurakDictGuncelle(Dictionary<string, Durak> durakDict);
        
        /// <summary>
        /// Mevcut durak dictionary'sini döndürür.
        /// </summary>
        /// <returns>Durak dictionary</returns>
        Dictionary<string, Durak> DurakDictGetir();
        
        #endregion
    }
    
    /// <summary>
    /// Rota sıralama kriterleri
    /// </summary>
    public enum RotaSiralamaKriteri
    {
        /// <summary>Süreye göre sırala</summary>
        Sure,
        
        /// <summary>Ücrete göre sırala</summary>
        Ucret,
        
        /// <summary>Aktarma sayısına göre sırala</summary>
        AktarmaSayisi,
        
        /// <summary>Mesafeye göre sırala</summary>
        Mesafe
    }
    
    /// <summary>
    /// Rota filtreleme kriterleri
    /// </summary>
    public class RotaFiltresi
    {
        /// <summary>Maksimum ücret limiti (null = limit yok)</summary>
        public double? MaxUcret { get; set; }
        
        /// <summary>Maksimum süre limiti dakika cinsinden (null = limit yok)</summary>
        public int? MaxSureDakika { get; set; }
        
        /// <summary>Maksimum aktarma sayısı (null = limit yok)</summary>
        public int? MaxAktarmaSayisi { get; set; }
        
        /// <summary>Sadece yürüme içeren rotalar</summary>
        public bool SadeceYurume { get; set; }
        
        /// <summary>Taksi içermeyen rotalar</summary>
        public bool TaksiHaric { get; set; }
    }
    
    /// <summary>
    /// İki rota arasındaki karşılaştırma sonucu
    /// </summary>
    public class RotaKarsilastirma
    {
        /// <summary>Süre farkı (dakika). Pozitif = rota1 daha uzun</summary>
        public int SureFarki { get; set; }
        
        /// <summary>Ücret farkı (TL). Pozitif = rota1 daha pahalı</summary>
        public double UcretFarki { get; set; }
        
        /// <summary>Aktarma sayısı farkı. Pozitif = rota1 daha fazla aktarmalı</summary>
        public int AktarmaFarki { get; set; }
        
        /// <summary>Özet değerlendirme</summary>
        public string Ozet { get; set; }
    }
}
