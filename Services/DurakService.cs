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

        
        public List<Durak> DuraklariOkuVeGrafOlustur()
        {
            var durakListesi = new List<Durak>();
            var durakDict = new Dictionary<string, Durak>();

            string jsonText = File.ReadAllText(jsonPath);
            using JsonDocument doc = JsonDocument.Parse(jsonText);
            var duraklar = doc.RootElement.GetProperty("duraklar");

            
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
                    
                    string transferStopId = transfer.GetProperty("transferStopId").GetString();
                    int transferSure = transfer.GetProperty("transferSure").GetInt32();
                    double transferUcret = transfer.GetProperty("transferUcret").GetDouble();

                    
                    Arac aktarmaAraci = new AktarmaAraci(transferSure, transferUcret);

                    
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
            
            var userNode = new Durak
            {
                Id = "userNode_" + Guid.NewGuid().ToString("N"), 
                Ad = "KullaniciKonumu",
                Enlem = userLat,
                Boylam = userLon,
                Baglantilar = new List<DurakBaglantisi>()
            };

           
            foreach (var durak in tumDuraklar)
            {
                double mesafe = MesafeHesapla(userLat, userLon, durak.Enlem, durak.Boylam);

                Arac baglantiAraci ;
                if (mesafe <= 3.0)
                {
                    
                    baglantiAraci = new Yurumek(mesafe);
                }
                else
                {
                    
                    baglantiAraci = new Taksi(mesafe); 
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
            double R = 6371.0; 
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
