using Prolab_4.Models;
using System;
using System.Collections.Generic;

namespace Prolab_4.Services
{
    
    public class RotaHesaplayici
    {
        
        public List<Rota> TumRotalariBul(
            Dictionary<string, Durak> durakDict,
            string baslangicId,
            string hedefId,
            Yolcu yolcu, 
            IOdemeYontemi odemeYontemi
        )
        {
            var tumRotalar = new List<Rota>();

           
            if (!durakDict.ContainsKey(baslangicId) || !durakDict.ContainsKey(hedefId))
            {
                
                return tumRotalar;
            }

            
            var visited = new HashSet<string>();
            var yol = new List<string> { baslangicId }; 

            
            AramaDFS(durakDict, baslangicId, hedefId, 0.0, 0, yol, visited, tumRotalar, yolcu, odemeYontemi);

            return tumRotalar;
        }

       
        private void AramaDFS(
            Dictionary<string, Durak> durakDict,
            string currentId,
            string hedefId,
            double ucretSoFar,
            int sureSoFar,
            List<string> yol,
            HashSet<string> visited,
            List<Rota> tumRotalar,
            Yolcu yolcu, 
            IOdemeYontemi odemeYontemi
        )
        {
            
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

                
                if (nextId == hedefId)
                {
                    
                    double finalUcret = ucretSoFar;
                    int finalSure = sureSoFar;

                    if (baglanti.Arac != null)
                    {
                        double aracUcret = odemeYontemi.UcretHesapla(yolcu, baglanti.Arac);
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

                    
                    yol.RemoveAt(yol.Count - 1);

                    
                    continue;
                }

                
                if (visited.Contains(nextId))
                    continue;

                
                double yeniUcret = ucretSoFar;
                int yeniSure = sureSoFar;

                if (baglanti.Arac != null)
                {
                    double aracUcret = odemeYontemi.UcretHesapla(yolcu,baglanti.Arac);
                    yeniUcret += aracUcret;
                    yeniSure += baglanti.Arac.TahminiSure;
                }

                
                yol.Add(nextId);

                
                AramaDFS(
                    durakDict,
                    nextId,
                    hedefId,
                    yeniUcret,
                    yeniSure,
                    yol,
                    visited,
                    tumRotalar,
                    yolcu,
                    odemeYontemi

                );

                yol.RemoveAt(yol.Count - 1);
            }

            
            visited.Remove(currentId);
        }
    }
}
