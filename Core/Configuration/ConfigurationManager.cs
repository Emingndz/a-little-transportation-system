using System;
using System.IO;
using System.Text.Json;
using Prolab_4.Core.Logging;

namespace Prolab_4.Core.Configuration
{
    /// <summary>
    /// Uygulama ayarları modeli
    /// </summary>
    public class AppSettings
    {
        public string City { get; set; } = "Izmit";
        public string DataFilePath { get; set; } = "Data/veriseti.json";
        public string BackgroundImagePath { get; set; } = "Resource/Arkaplan.png";
        public string LogsDirectory { get; set; } = "Logs";
    }

    /// <summary>
    /// Harita ayarları modeli
    /// </summary>
    public class MapSettings
    {
        public double DefaultLatitude { get; set; } = 40.7655;
        public double DefaultLongitude { get; set; } = 29.9400;
        public int DefaultZoom { get; set; } = 12;
        public int MinZoom { get; set; } = 1;
        public int MaxZoom { get; set; } = 18;
    }

    /// <summary>
    /// Taksi ayarları
    /// </summary>
    public class TaxiSettings
    {
        public double OpeningFee { get; set; } = 10.0;
        public double CostPerKm { get; set; } = 4.0;
        public double AverageSpeedKmh { get; set; } = 50.0;
    }

    /// <summary>
    /// Yürüme ayarları
    /// </summary>
    public class WalkingSettings
    {
        public double MaxDistanceKm { get; set; } = 3.0;
        public double MinutesPerKm { get; set; } = 15.0;
    }

    /// <summary>
    /// Ulaşım ayarları modeli
    /// </summary>
    public class TransportSettings
    {
        public TaxiSettings Taxi { get; set; } = new TaxiSettings();
        public WalkingSettings Walking { get; set; } = new WalkingSettings();
    }

    /// <summary>
    /// İndirim ayarları modeli
    /// </summary>
    public class DiscountSettings
    {
        public double StudentDiscount { get; set; } = 0.5;
        public double SeniorDiscount { get; set; } = 0.3;
        public double CreditCardSurcharge { get; set; } = 0.25;
    }

    /// <summary>
    /// Algoritma ayarları modeli
    /// </summary>
    public class AlgorithmSettings
    {
        public int MaxDfsDepth { get; set; } = 50;
        public int MaxRouteCount { get; set; } = 100;
    }

    /// <summary>
    /// Tüm yapılandırma modeli
    /// </summary>
    public class ApplicationConfiguration
    {
        public AppSettings AppSettings { get; set; } = new AppSettings();
        public MapSettings MapSettings { get; set; } = new MapSettings();
        public TransportSettings TransportSettings { get; set; } = new TransportSettings();
        public DiscountSettings DiscountSettings { get; set; } = new DiscountSettings();
        public AlgorithmSettings AlgorithmSettings { get; set; } = new AlgorithmSettings();
    }

    /// <summary>
    /// Yapılandırma yöneticisi - Singleton pattern
    /// </summary>
    public class ConfigurationManager
    {
        private static ConfigurationManager _instance;
        private static readonly object _lock = new object();
        private ApplicationConfiguration _config;
        private readonly string _configFilePath;

        public static ConfigurationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ConfigurationManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private ConfigurationManager()
        {
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            LoadConfiguration();
        }

        /// <summary>
        /// Yapılandırmayı yükler
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    string json = File.ReadAllText(_configFilePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    _config = JsonSerializer.Deserialize<ApplicationConfiguration>(json, options);
                    Logger.Info("Yapılandırma dosyası yüklendi.");
                }
                else
                {
                    Logger.Warning($"Yapılandırma dosyası bulunamadı: {_configFilePath}. Varsayılan değerler kullanılacak.");
                    _config = new ApplicationConfiguration();
                    SaveConfiguration(); // Varsayılan yapılandırmayı kaydet
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Yapılandırma yüklenirken hata oluştu. Varsayılan değerler kullanılacak.", ex);
                _config = new ApplicationConfiguration();
            }
        }

        /// <summary>
        /// Yapılandırmayı kaydeder
        /// </summary>
        public void SaveConfiguration()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(_config, options);
                File.WriteAllText(_configFilePath, json);
                Logger.Info("Yapılandırma dosyası kaydedildi.");
            }
            catch (Exception ex)
            {
                Logger.Error("Yapılandırma kaydedilirken hata oluştu.", ex);
            }
        }

        /// <summary>
        /// Yapılandırmayı yeniden yükler
        /// </summary>
        public void ReloadConfiguration()
        {
            LoadConfiguration();
        }

        // Kolay erişim property'leri
        public AppSettings App => _config.AppSettings;
        public MapSettings Map => _config.MapSettings;
        public TransportSettings Transport => _config.TransportSettings;
        public DiscountSettings Discount => _config.DiscountSettings;
        public AlgorithmSettings Algorithm => _config.AlgorithmSettings;

        /// <summary>
        /// Tam yapılandırma nesnesine erişim
        /// </summary>
        public ApplicationConfiguration Configuration => _config;
    }

    /// <summary>
    /// Kolay erişim için static helper
    /// </summary>
    public static class Config
    {
        public static AppSettings App => ConfigurationManager.Instance.App;
        public static MapSettings Map => ConfigurationManager.Instance.Map;
        public static TransportSettings Transport => ConfigurationManager.Instance.Transport;
        public static DiscountSettings Discount => ConfigurationManager.Instance.Discount;
        public static AlgorithmSettings Algorithm => ConfigurationManager.Instance.Algorithm;
    }
}
