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
        // Yeni eklenen: Durak bilgilerini model nesnesi olarak döner (harita için)
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
    }
}
