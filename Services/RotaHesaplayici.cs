using Prolab_4.Models;
using System;
using System.Collections.Generic;

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
        /// Yolcu parametresi eklendi, böylece Otobüs/Tramvay ücretinde indirim vb. hesaplanabilir.
        /// </summary>
        public List<Rota> TumRotalariBul(
            Dictionary<string, Durak> durakDict,
            string baslangicId,
            string hedefId,
            Yolcu yolcu  // YENİ parametre
        )
        {
            var tumRotalar = new List<Rota>();

            // 1. Geçersiz ID kontrolü
            if (!durakDict.ContainsKey(baslangicId) || !durakDict.ContainsKey(hedefId))
            {
                // Böyle bir durak yoksa boş liste dön
                return tumRotalar;
            }

            // 2. DFS için hazırlık
            var visited = new HashSet<string>();
            var yol = new List<string> { baslangicId }; // başlangıç durağını ekliyoruz

            // 3. DFS fonksiyonunu çağırırken yolcu da veriyoruz
            AramaDFS(durakDict, baslangicId, hedefId, 0.0, 0, yol, visited, tumRotalar, yolcu);

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
        /// yolcu: hangi tip yolcu (öğrenci, 65+ vb.)
        /// </summary>
        private void AramaDFS(
            Dictionary<string, Durak> durakDict,
            string currentId,
            string hedefId,
            double ucretSoFar,
            int sureSoFar,
            List<string> yol,
            HashSet<string> visited,
            List<Rota> tumRotalar,
            Yolcu yolcu // YENİ parametre
        )
        {
            // 1. Hedefe ulaştıysak, bulduğumuz rota kaydedip return ediyoruz
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

                // *** YENİ: Hedefe direkt gidiyorsa cycle'ı atlamadan kaydedelim
                if (nextId == hedefId)
                {
                    // Otobüs/Tramvay ise UcretHesapla(mesafe, yolcu) diyebilirsiniz.
                    double finalUcret = ucretSoFar;
                    int finalSure = sureSoFar;

                    if (baglanti.Arac != null)
                    {
                        double aracUcret = baglanti.Arac.UcretHesapla(baglanti.Arac.Mesafe, yolcu);
                        finalUcret += aracUcret;
                        finalSure += baglanti.Arac.TahminiSure;
                    }

                    yol.Add(nextId);

                    var directRota = new Rota
                    {
                        DurakIdList = new List<string>(yol),
                        ToplamUcret = finalUcret,
                        ToplamSure = finalSure
                    };
                    tumRotalar.Add(directRota);

                    // geri dönüş
                    yol.RemoveAt(yol.Count - 1);

                    // Bu kenarı işledik, devam edelim (diğer alternatifleri de görebilir)
                    continue;
                }

                // 4. cycle önlemek: eğer nextId visited setinde varsa bu path'i atla
                if (visited.Contains(nextId))
                    continue;

                // 5. Ücret / süreyi hesapla (Arac null olursa 0 eklenir)
                double yeniUcret = ucretSoFar;
                int yeniSure = sureSoFar;

                if (baglanti.Arac != null)
                {
                    double aracUcret = baglanti.Arac.UcretHesapla(baglanti.Arac.Mesafe, yolcu);
                    yeniUcret += aracUcret;
                    yeniSure += baglanti.Arac.TahminiSure;
                }

                // 6. Yol'a ekle
                yol.Add(nextId);

                // 7. recursion
                AramaDFS(
                    durakDict,
                    nextId,
                    hedefId,
                    yeniUcret,
                    yeniSure,
                    yol,
                    visited,
                    tumRotalar,
                    yolcu // YENİ parametre de geçiyoruz
                );

                // 8. geri dönüş
                yol.RemoveAt(yol.Count - 1);
            }

            // 9. Bu düğümden çıkarken visited'dan kaldır (backtracking)
            visited.Remove(currentId);
        }
    }
}
