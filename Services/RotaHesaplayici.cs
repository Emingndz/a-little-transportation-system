using Prolab_4.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Prolab_4.Services
{
    /// <summary>
    /// Duraklar arasındaki tüm olası rotaları (DFS yöntemiyle) bulur ve 
    /// bu rotaların toplam ücret/süresini hesaplar.
    /// </summary>
    public class RotaHesaplayici
    {
        /// <summary>
        /// baslangicId -> hedefId tüm yolları arar.
        /// Her bir yolun ToplamUcret ve ToplamSure'si Rota nesnesinde tutulur.
        /// </summary>
        public List<Rota> TumRotalariBul(Dictionary<string, Durak> durakDict, string baslangicId, string hedefId)
        {
            var tumRotalar = new List<Rota>();

            // 1. Geçersiz ID kontrolü
            if (!durakDict.ContainsKey(baslangicId) || !durakDict.ContainsKey(hedefId))
            {
                // Böyle bir durak yoksa boş liste dön
                return tumRotalar;
            }

            // 2. DFS için hazırlık
            // visited: recursion çağrısı boyunca hangi duraklara girdik, track etmek için
            var visited = new HashSet<string>();
            var yol = new List<string> { baslangicId }; // başlangıç durak eklendi

            // 3. DFS fonksiyonunu çağır
            AramaDFS(durakDict, baslangicId, hedefId, 0.0, 0, yol, visited, tumRotalar);

            return tumRotalar;
        }

        /// <summary>
        /// Asıl DFS (derinlik öncelikli) arama fonksiyonu.
        /// currentId: şu anki durağın Id'si
        /// hedefId: ulaşmak istediğimiz durağın Id'si
        /// ucretSoFar / sureSoFar: bu ana kadarki toplam ücret / süre
        /// yol: o anki rota (düğüm ID'leri)
        /// visited: mevcut recursion path'te ziyaret edilen duraklar
        /// tumRotalar: tamamlanmış bütün yolların tutulduğu liste
        /// </summary>
        private void AramaDFS(
            Dictionary<string, Durak> durakDict,
            string currentId,
            string hedefId,
            double ucretSoFar,
            int sureSoFar,
            List<string> yol,
            HashSet<string> visited,
            List<Rota> tumRotalar)
        {
            // 1. Hedefe ulaştıysak, bulduğumuz rota kaydedilip return edilir
            if (currentId == hedefId)
            {
                var rota = new Rota
                {
                    DurakIdList = new List<string>(yol), // kopyasını al
                    ToplamUcret = ucretSoFar,
                    ToplamSure = sureSoFar
                };
                tumRotalar.Add(rota);
                return;
            }

            // 2. Mevcut düğümü visited setine ekle
            visited.Add(currentId);

            // 3. Sıradaki duraklara geçiş
            var currentDurak = durakDict[currentId];
            foreach (var baglanti in currentDurak.Baglantilar)
            {
                string nextId = baglanti.HedefDurakId;

                // cycle önlemek: eğer bu ID visited'daysa zaten bu yol üstünde gezdik
                if (visited.Contains(nextId))
                    continue;

                // Ücret / süreyi hesapla (Arac null olursa 0 eklenir)
                double yeniUcret = ucretSoFar + (baglanti.Arac?.Ucret ?? 0);
                int yeniSure = sureSoFar + (baglanti.Arac?.TahminiSure ?? 0);

                // yol'a ekle
                yol.Add(nextId);

                // recursion
                AramaDFS(durakDict, nextId, hedefId, yeniUcret, yeniSure, yol, visited, tumRotalar);

                // geri dönüş
                yol.RemoveAt(yol.Count - 1);
            }

            // 4. Bu düğümden çıkarken visited'dan kaldır (backtracking)
            visited.Remove(currentId);
        }
    }
}
