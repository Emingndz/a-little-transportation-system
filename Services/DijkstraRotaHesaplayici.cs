using Prolab_4.Core;
using Prolab_4.Core.Logging;
using Prolab_4.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Prolab_4.Services
{
    /// <summary>
    /// Rota hesaplama stratejileri için interface
    /// </summary>
    public interface IRotaHesaplayici
    {
        List<Rota> TumRotalariBul(Dictionary<string, Durak> durakDict, string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odemeYontemi);
        Rota EnKisaRotaBul(Dictionary<string, Durak> durakDict, string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odemeYontemi);
        Rota EnUcuzRotaBul(Dictionary<string, Durak> durakDict, string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odemeYontemi);
    }

    /// <summary>
    /// Dijkstra algoritması ile optimum rota hesaplama
    /// En kısa süre veya en düşük ücret için kullanılır
    /// </summary>
    public class DijkstraRotaHesaplayici : IRotaHesaplayici
    {
        private enum OptimizasyonKriteri
        {
            Sure,
            Ucret
        }

        /// <summary>
        /// Dijkstra için priority queue elemanı
        /// </summary>
        private class DijkstraNode : IComparable<DijkstraNode>
        {
            public string DurakId { get; set; }
            public double Maliyet { get; set; }
            public List<string> Yol { get; set; }
            public double ToplamUcret { get; set; }
            public int ToplamSure { get; set; }

            public int CompareTo(DijkstraNode other)
            {
                return Maliyet.CompareTo(other.Maliyet);
            }
        }

        /// <summary>
        /// En kısa süreli rotayı bulur (Dijkstra algoritması)
        /// </summary>
        public Rota EnKisaRotaBul(Dictionary<string, Durak> durakDict, string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odemeYontemi)
        {
            Logger.Info($"En kısa rota aranıyor: {baslangicId} -> {hedefId}");
            return DijkstraArama(durakDict, baslangicId, hedefId, yolcu, odemeYontemi, OptimizasyonKriteri.Sure);
        }

        /// <summary>
        /// En ucuz rotayı bulur (Dijkstra algoritması)
        /// </summary>
        public Rota EnUcuzRotaBul(Dictionary<string, Durak> durakDict, string baslangicId, string hedefId, Yolcu yolcu, IOdemeYontemi odemeYontemi)
        {
            Logger.Info($"En ucuz rota aranıyor: {baslangicId} -> {hedefId}");
            return DijkstraArama(durakDict, baslangicId, hedefId, yolcu, odemeYontemi, OptimizasyonKriteri.Ucret);
        }

        private Rota DijkstraArama(Dictionary<string, Durak> durakDict, string baslangicId, string hedefId, 
            Yolcu yolcu, IOdemeYontemi odemeYontemi, OptimizasyonKriteri kriter)
        {
            if (!durakDict.ContainsKey(baslangicId) || !durakDict.ContainsKey(hedefId))
            {
                Logger.Warning($"Durak bulunamadı: Başlangıç={baslangicId}, Hedef={hedefId}");
                return null;
            }

            // Mesafe/maliyet dictionary
            var mesafeler = new Dictionary<string, double>();
            var oncekiDurak = new Dictionary<string, string>();
            var ucretler = new Dictionary<string, double>();
            var sureler = new Dictionary<string, int>();
            var ziyaretEdildi = new HashSet<string>();

            // Priority queue (min-heap simulation with SortedSet)
            var pq = new SortedSet<(double maliyet, int sira, string durakId)>();
            int siraNo = 0;

            // Başlangıç değerleri
            foreach (var durakId in durakDict.Keys)
            {
                mesafeler[durakId] = double.MaxValue;
                ucretler[durakId] = 0;
                sureler[durakId] = 0;
            }
            
            mesafeler[baslangicId] = 0;
            pq.Add((0, siraNo++, baslangicId));

            while (pq.Count > 0)
            {
                var current = pq.Min;
                pq.Remove(current);
                string currentId = current.durakId;

                if (ziyaretEdildi.Contains(currentId))
                    continue;

                ziyaretEdildi.Add(currentId);

                // Hedefe ulaştık mı?
                if (currentId == hedefId)
                {
                    return YoluOlustur(oncekiDurak, baslangicId, hedefId, ucretler[hedefId], sureler[hedefId]);
                }

                var currentDurak = durakDict[currentId];

                foreach (var baglanti in currentDurak.Baglantilar)
                {
                    string hedefDurakId = baglanti.HedefDurakId;
                    
                    if (!durakDict.ContainsKey(hedefDurakId) || ziyaretEdildi.Contains(hedefDurakId))
                        continue;

                    // Maliyet hesapla
                    double aracUcret = 0;
                    int aracSure = 0;
                    
                    if (baglanti.Arac != null)
                    {
                        aracUcret = odemeYontemi.UcretHesapla(yolcu, baglanti.Arac);
                        aracSure = baglanti.Arac.TahminiSure;
                    }

                    double yeniMaliyet = kriter == OptimizasyonKriteri.Sure
                        ? mesafeler[currentId] + aracSure
                        : mesafeler[currentId] + aracUcret;

                    if (yeniMaliyet < mesafeler[hedefDurakId])
                    {
                        mesafeler[hedefDurakId] = yeniMaliyet;
                        oncekiDurak[hedefDurakId] = currentId;
                        ucretler[hedefDurakId] = ucretler[currentId] + aracUcret;
                        sureler[hedefDurakId] = sureler[currentId] + aracSure;
                        
                        pq.Add((yeniMaliyet, siraNo++, hedefDurakId));
                    }
                }
            }

            Logger.Warning($"Rota bulunamadı: {baslangicId} -> {hedefId}");
            return null;
        }

        private Rota YoluOlustur(Dictionary<string, string> oncekiDurak, string baslangicId, string hedefId, double toplamUcret, int toplamSure)
        {
            var yol = new List<string>();
            string current = hedefId;

            while (current != null)
            {
                yol.Add(current);
                oncekiDurak.TryGetValue(current, out current);
            }

            yol.Reverse();

            if (yol.First() != baslangicId)
            {
                return null;
            }

            Logger.Info($"Rota bulundu: {yol.Count} durak, {toplamSure} dk, {toplamUcret:F2} TL");

            return new Rota
            {
                DurakIdList = yol,
                ToplamUcret = toplamUcret,
                ToplamSure = toplamSure
            };
        }

        /// <summary>
        /// Tüm rotaları DFS ile bulur (mevcut implementasyon)
        /// Not: Büyük graflarda yavaş olabilir, maksimum rota sayısı sınırlandırılmıştır
        /// </summary>
        public List<Rota> TumRotalariBul(Dictionary<string, Durak> durakDict, string baslangicId, string hedefId, 
            Yolcu yolcu, IOdemeYontemi odemeYontemi)
        {
            var tumRotalar = new List<Rota>();

            if (!durakDict.ContainsKey(baslangicId) || !durakDict.ContainsKey(hedefId))
            {
                Logger.Warning($"Durak bulunamadı: Başlangıç={baslangicId}, Hedef={hedefId}");
                return tumRotalar;
            }

            var visited = new HashSet<string>();
            var yol = new List<string> { baslangicId };

            AramaDFS(durakDict, baslangicId, hedefId, 0.0, 0, yol, visited, tumRotalar, yolcu, odemeYontemi, 0);

            Logger.Info($"Toplam {tumRotalar.Count} rota bulundu");
            return tumRotalar;
        }

        private void AramaDFS(Dictionary<string, Durak> durakDict, string currentId, string hedefId,
            double ucretSoFar, int sureSoFar, List<string> yol, HashSet<string> visited,
            List<Rota> tumRotalar, Yolcu yolcu, IOdemeYontemi odemeYontemi, int derinlik)
        {
            // Maksimum derinlik kontrolü (sonsuz döngü önleme)
            if (derinlik > Constants.MAX_DFS_DERINLIK)
            {
                Logger.Warning($"Maksimum DFS derinliğine ulaşıldı: {derinlik}");
                return;
            }

            // Maksimum rota sayısı kontrolü (bellek optimizasyonu)
            if (tumRotalar.Count >= Constants.MAX_ROTA_SAYISI)
            {
                return;
            }

            if (currentId == hedefId)
            {
                var rota = new Rota
                {
                    DurakIdList = new List<string>(yol),
                    ToplamUcret = ucretSoFar,
                    ToplamSure = sureSoFar
                };
                tumRotalar.Add(rota);
                return;
            }

            visited.Add(currentId);
            var currentDurak = durakDict[currentId];

            foreach (var baglanti in currentDurak.Baglantilar)
            {
                string nextId = baglanti.HedefDurakId;

                if (!durakDict.ContainsKey(nextId) || visited.Contains(nextId))
                    continue;

                double yeniUcret = ucretSoFar;
                int yeniSure = sureSoFar;

                if (baglanti.Arac != null)
                {
                    double aracUcret = odemeYontemi.UcretHesapla(yolcu, baglanti.Arac);
                    yeniUcret += aracUcret;
                    yeniSure += baglanti.Arac.TahminiSure;
                }

                yol.Add(nextId);
                AramaDFS(durakDict, nextId, hedefId, yeniUcret, yeniSure, yol, visited, tumRotalar, yolcu, odemeYontemi, derinlik + 1);
                yol.RemoveAt(yol.Count - 1);
            }

            visited.Remove(currentId);
        }
    }

    /// <summary>
    /// A* algoritması ile rota hesaplama (Haversine heuristic ile)
    /// </summary>
    public class AStarRotaHesaplayici : IRotaHesaplayici
    {
        private readonly DurakService _durakService = new DurakService();

        public Rota EnKisaRotaBul(Dictionary<string, Durak> durakDict, string baslangicId, string hedefId, 
            Yolcu yolcu, IOdemeYontemi odemeYontemi)
        {
            Logger.Info($"A* ile en kısa rota aranıyor: {baslangicId} -> {hedefId}");
            
            if (!durakDict.ContainsKey(baslangicId) || !durakDict.ContainsKey(hedefId))
                return null;

            var hedefDurak = durakDict[hedefId];
            
            var gScore = new Dictionary<string, double> { [baslangicId] = 0 };
            var fScore = new Dictionary<string, double> { [baslangicId] = Heuristic(durakDict[baslangicId], hedefDurak) };
            var onceki = new Dictionary<string, string>();
            var ucretler = new Dictionary<string, double> { [baslangicId] = 0 };
            var sureler = new Dictionary<string, int> { [baslangicId] = 0 };
            
            var openSet = new SortedSet<(double fScore, int sira, string id)>();
            int siraNo = 0;
            openSet.Add((fScore[baslangicId], siraNo++, baslangicId));

            var closedSet = new HashSet<string>();

            while (openSet.Count > 0)
            {
                var current = openSet.Min;
                openSet.Remove(current);
                string currentId = current.id;

                if (currentId == hedefId)
                {
                    return ReconstructPath(onceki, baslangicId, hedefId, ucretler[hedefId], sureler[hedefId]);
                }

                closedSet.Add(currentId);
                var currentDurak = durakDict[currentId];

                foreach (var baglanti in currentDurak.Baglantilar)
                {
                    string neighborId = baglanti.HedefDurakId;
                    
                    if (!durakDict.ContainsKey(neighborId) || closedSet.Contains(neighborId))
                        continue;

                    double aracUcret = 0;
                    int aracSure = 0;
                    
                    if (baglanti.Arac != null)
                    {
                        aracUcret = odemeYontemi.UcretHesapla(yolcu, baglanti.Arac);
                        aracSure = baglanti.Arac.TahminiSure;
                    }

                    double tentativeG = gScore.GetValueOrDefault(currentId, double.MaxValue) + aracSure;

                    if (tentativeG < gScore.GetValueOrDefault(neighborId, double.MaxValue))
                    {
                        onceki[neighborId] = currentId;
                        gScore[neighborId] = tentativeG;
                        ucretler[neighborId] = ucretler.GetValueOrDefault(currentId, 0) + aracUcret;
                        sureler[neighborId] = sureler.GetValueOrDefault(currentId, 0) + aracSure;
                        
                        double h = Heuristic(durakDict[neighborId], hedefDurak);
                        fScore[neighborId] = tentativeG + h;
                        
                        openSet.Add((fScore[neighborId], siraNo++, neighborId));
                    }
                }
            }

            return null;
        }

        private double Heuristic(Durak from, Durak to)
        {
            // Haversine mesafesini tahmini süreye çevir (ortalama 30 km/saat varsayımı)
            double mesafe = _durakService.MesafeHesapla(from.Enlem, from.Boylam, to.Enlem, to.Boylam);
            return (mesafe / 30.0) * 60; // dakika cinsinden
        }

        private Rota ReconstructPath(Dictionary<string, string> onceki, string baslangic, string hedef, double ucret, int sure)
        {
            var yol = new List<string>();
            string current = hedef;

            while (current != null)
            {
                yol.Add(current);
                onceki.TryGetValue(current, out current);
            }

            yol.Reverse();
            
            return new Rota
            {
                DurakIdList = yol,
                ToplamUcret = ucret,
                ToplamSure = sure
            };
        }

        public Rota EnUcuzRotaBul(Dictionary<string, Durak> durakDict, string baslangicId, string hedefId, 
            Yolcu yolcu, IOdemeYontemi odemeYontemi)
        {
            // Ücret optimizasyonu için Dijkstra daha uygun
            var dijkstra = new DijkstraRotaHesaplayici();
            return dijkstra.EnUcuzRotaBul(durakDict, baslangicId, hedefId, yolcu, odemeYontemi);
        }

        public List<Rota> TumRotalariBul(Dictionary<string, Durak> durakDict, string baslangicId, string hedefId, 
            Yolcu yolcu, IOdemeYontemi odemeYontemi)
        {
            // Tüm rotalar için DFS kullan
            var dijkstra = new DijkstraRotaHesaplayici();
            return dijkstra.TumRotalariBul(durakDict, baslangicId, hedefId, yolcu, odemeYontemi);
        }
    }
}
