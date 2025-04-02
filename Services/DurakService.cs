using GMap.NET;
using Prolab_4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Prolab_4.Services
{
    public class DurakService
    {
        private string jsonPath = @"Data/veriseti.json";

        // 1. Duraklar arası araç listesi oluşturur (önceki kod)
        public List<Arac> DuraklariOkuVeAracListesiOlustur()
        {
            var aracListesi = new List<Arac>();

            string jsonText = File.ReadAllText(jsonPath);
            using JsonDocument doc = JsonDocument.Parse(jsonText);

            var duraklar = doc.RootElement.GetProperty("duraklar");

            foreach (var durak in duraklar.EnumerateArray())
            {
                string id = durak.GetProperty("id").GetString();
                string name = durak.GetProperty("name").GetString();
                string type = durak.GetProperty("type").GetString();
                double lat = durak.GetProperty("lat").GetDouble();
                double lon = durak.GetProperty("lon").GetDouble();
                bool sonDurak = durak.GetProperty("sonDurak").GetBoolean();

                if (durak.TryGetProperty("nextStops", out JsonElement nextStops) && nextStops.GetArrayLength() > 0)
                {
                    foreach (var nextStop in nextStops.EnumerateArray())
                    {
                        double mesafe = nextStop.GetProperty("mesafe").GetDouble();
                        int sure = nextStop.GetProperty("sure").GetInt32();
                        double ucret = nextStop.GetProperty("ucret").GetDouble();

                        Arac arac = null;
                        if (type == "bus")
                            arac = new Otobus(mesafe, ucret, sure);
                        else if (type == "tram")
                            arac = new Tramvay(mesafe, ucret, sure);

                        if (arac != null)
                            aracListesi.Add(arac);
                    }
                }
            }

            return aracListesi;
        }

        // 2. Haritada gösterilecek durakların konumlarını getirir (önceki kod)
        public List<Durak> DurakKonumlariniGetir()
        {
            var durakListesi = new List<Durak>();
            string jsonText = File.ReadAllText(jsonPath);
            using JsonDocument doc = JsonDocument.Parse(jsonText);
            var duraklar = doc.RootElement.GetProperty("duraklar");

            foreach (var item in duraklar.EnumerateArray())
            {
                durakListesi.Add(new Durak
                {
                    Id = item.GetProperty("id").GetString(),
                    Ad = item.GetProperty("name").GetString(),
                    Tur = item.GetProperty("type").GetString(),
                    Enlem = item.GetProperty("lat").GetDouble(),
                    Boylam = item.GetProperty("lon").GetDouble()
                });
            }

            return durakListesi;
        }

        // 3. Duraklar arası bağlantı grafını kurar (yeni eklenen yapı)
        public List<Durak> DuraklariOkuVeGrafOlustur()
        {
            var durakListesi = new List<Durak>();
            var durakDict = new Dictionary<string, Durak>();

            string jsonText = File.ReadAllText(jsonPath);
            using JsonDocument doc = JsonDocument.Parse(jsonText);
            var duraklar = doc.RootElement.GetProperty("duraklar");

            // Önce tüm durakları oluştur
            foreach (var item in duraklar.EnumerateArray())
            {
                var durak = new Durak
                {
                    Id = item.GetProperty("id").GetString(),
                    Ad = item.GetProperty("name").GetString(),
                    Tur = item.GetProperty("type").GetString(),
                    Enlem = item.GetProperty("lat").GetDouble(),
                    Boylam = item.GetProperty("lon").GetDouble(),
                    SonDurak = item.GetProperty("sonDurak").GetBoolean(),
                    Baglantilar = new List<DurakBaglantisi>()
                };

                durakListesi.Add(durak);
                durakDict[durak.Id] = durak;
            }

            // Sonra bağlantıları kur
            int index = 0;
            foreach (var item in duraklar.EnumerateArray())
            {
                var mevcutDurak = durakListesi[index];
                index++;

                if (item.TryGetProperty("nextStops", out JsonElement nextStops) && nextStops.GetArrayLength() > 0)
                {
                    foreach (var next in nextStops.EnumerateArray())
                    {
                        string hedefId = next.GetProperty("stopId").GetString();
                        double mesafe = next.GetProperty("mesafe").GetDouble();
                        int sure = next.GetProperty("sure").GetInt32();
                        double ucret = next.GetProperty("ucret").GetDouble();

                        Arac arac = null;
                        if (mevcutDurak.Tur == "bus")
                            arac = new Otobus(mesafe, ucret, sure);
                        else if (mevcutDurak.Tur == "tram")
                            arac = new Tramvay(mesafe, ucret, sure);

                        mevcutDurak.Baglantilar.Add(new DurakBaglantisi
                        {
                            HedefDurakId = hedefId,
                            Arac = arac
                        });
                    }
                }
                if (item.TryGetProperty("transfer", out JsonElement transfer) && transfer.ValueKind == JsonValueKind.Object)
                {
                    // Mesela: "transferStopId", "transferSure", "transferUcret"
                    string transferStopId = transfer.GetProperty("transferStopId").GetString();
                    int transferSure = transfer.GetProperty("transferSure").GetInt32();
                    double transferUcret = transfer.GetProperty("transferUcret").GetDouble();

                    // Aktarma da bir çeşit "Arac" sayılabilir.
                    // İsterseniz "AktarmaAraci" diye özel bir sınıf oluşturabilirsiniz.
                    // Veya basitçe "Otobus" gibi bir Arac:
                    Arac aktarmaAraci = new AktarmaAraci(transferSure, transferUcret);

                    // Şimdi mevcut durağın "Baglantilar" listesine ekliyoruz.
                    mevcutDurak.Baglantilar.Add(new DurakBaglantisi
                    {
                        HedefDurakId = transferStopId,
                        Arac = aktarmaAraci
                    });
                }
            }

            return durakListesi;
        }

        public Durak AddUserNode(double userLat, double userLon, List<Durak> tumDuraklar)
        {
            // 1) Yeni durak nesnesi (sanal durak)
            var userNode = new Durak
            {
                Id = "userNode_" + Guid.NewGuid().ToString("N"), // her seferinde eşsiz ID türetilmeli
                Ad = "KullaniciKonumu",
                Enlem = userLat,
                Boylam = userLon,
                Baglantilar = new List<DurakBaglantisi>()
            };

            // 2) Her durakla mesafe ölç, 3 km kuralına göre Arac oluştur
            foreach (var durak in tumDuraklar)
            {
                double mesafe = MesafeHesapla(userLat, userLon, durak.Enlem, durak.Boylam);

                Arac baglantiAraci ;
                if (mesafe <= 3.0)
                {
                    // Yürüyüş
                    baglantiAraci = new Yurumek(mesafe);
                }
                else
                {
                    // Taksi
                    baglantiAraci = new Taksi(mesafe); // 10 + 4 * d
                }

                userNode.Baglantilar.Add(new DurakBaglantisi
                {
                    HedefDurakId = durak.Id,
                    Arac = baglantiAraci
                });

                durak.Baglantilar.Add(new DurakBaglantisi
                {
                    HedefDurakId = userNode.Id,
                    Arac = baglantiAraci
                });
            }

            return userNode;
        }

        

        public double MesafeHesapla(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371.0; // dünya yarıçapı km
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
        private double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }


    }
}
