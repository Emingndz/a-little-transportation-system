using Prolab_4.Core.DependencyInjection;
using Prolab_4.Core.Logging;
using Prolab_4.Services.Interfaces;
using static Prolab_4.Core.DependencyInjection.ServiceContainer;
using System;

namespace Prolab_4.Core
{
    /// <summary>
    /// Uygulama başlangıcında tüm servisleri DI Container'a kaydeder.
    /// Bu sınıf, Composition Root pattern'ini uygular.
    /// 
    /// Kullanım:
    /// 1. Program.cs veya Form constructor'ında Initialize() çağrılır
    /// 2. Servisler ServiceContainer.Instance üzerinden resolve edilir
    /// 
    /// Örnek:
    /// <code>
    /// ServiceBootstrapper.Initialize();
    /// var durakServisi = ServiceContainer.Instance.Resolve&lt;IDurakServisi&gt;();
    /// </code>
    /// </summary>
    public static class ServiceBootstrapper
    {
        #region Fields
        
        private static bool _isInitialized = false;
        private static readonly object _initLock = new object();
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Tüm servisleri DI Container'a kaydeder.
        /// Bu metot uygulama başlangıcında bir kez çağrılmalıdır.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Birden fazla kez çağrılırsa (debug modunda) fırlatılır.
        /// </exception>
        public static void Initialize()
        {
            lock (_initLock)
            {
                if (_isInitialized)
                {
                    Logger.Warning("ServiceBootstrapper.Initialize() zaten çağrıldı. Atlanıyor.");
                    return;
                }
                
                try
                {
                    Logger.Info("Servisler kaydediliyor...");
                    
                    var container = ServiceContainer.Instance;
                    
                    // ===============================================
                    // 1. CORE SERVİSLER
                    // ===============================================
                    
                    // Logger zaten static, kayıt gerektirmez
                    
                    // ===============================================
                    // 2. DATA SERVİSLERİ
                    // ===============================================
                    
                    // Durak Servisi - Singleton
                    // Veri önbelleğe alındığı için tek instance yeterli
                    container.Register<IDurakServisi, Services.DurakServisiImpl>(ServiceLifetime.Singleton);
                    Logger.Debug("IDurakServisi kaydedildi (Singleton).");
                    
                    // ===============================================
                    // 3. İŞ MANTİĞI SERVİSLERİ
                    // ===============================================
                    
                    // Rota Servisi - Transient
                    // Her hesaplama bağımsız olduğu için yeni instance
                    container.Register<IRotaServisi, Services.RotaServisiImpl>(ServiceLifetime.Transient);
                    Logger.Debug("IRotaServisi kaydedildi (Transient).");
                    
                    // ===============================================
                    // 4. UI SERVİSLERİ
                    // ===============================================
                    
                    // Harita Servisi - Singleton
                    // Tek bir harita kontrolü olduğu için
                    container.Register<IHaritaServisi, Services.HaritaServisiImpl>(ServiceLifetime.Singleton);
                    Logger.Debug("IHaritaServisi kaydedildi (Singleton).");
                    
                    // ===============================================
                    // 5. FACTORY KAYITLARI
                    // ===============================================
                    
                    // RotaServisi için factory - bağımlılıkları inject eder
                    // Not: Register<T>(Func<T>) overload'u kullanılıyor
                    container.Register<IRotaServisi>(() =>
                    {
                        var durakServisi = container.Resolve<IDurakServisi>();
                        return new Services.RotaServisiImpl(durakServisi);
                    }, ServiceLifetime.Transient);
                    Logger.Debug("IRotaServisi factory kaydedildi.");
                    
                    _isInitialized = true;
                    Logger.Info("Tüm servisler başarıyla kaydedildi.");
                }
                catch (Exception ex)
                {
                    Logger.Error("Servis kaydı sırasında hata oluştu.", ex);
                    throw new InvalidOperationException("ServiceBootstrapper başlatılamadı.", ex);
                }
            }
        }
        
        /// <summary>
        /// DI Container'ın başlatılıp başlatılmadığını kontrol eder.
        /// </summary>
        public static bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// Bir servisi resolve eder.
        /// Kısa yol metodu - ServiceContainer.Instance.Resolve() ile aynı.
        /// </summary>
        /// <typeparam name="T">Servis interface tipi</typeparam>
        /// <returns>Servis instance'ı</returns>
        /// <exception cref="InvalidOperationException">
        /// Servis kaydedilmemişse fırlatılır.
        /// </exception>
        public static T GetService<T>() where T : class
        {
            EnsureInitialized();
            return ServiceContainer.Instance.Resolve<T>();
        }
        
        /// <summary>
        /// Bir servisi resolve eder, bulunamazsa null döner.
        /// </summary>
        /// <typeparam name="T">Servis interface tipi</typeparam>
        /// <returns>Servis instance'ı veya null</returns>
        public static T GetServiceOrDefault<T>() where T : class
        {
            if (!_isInitialized)
            {
                return null;
            }
            
            return ServiceContainer.Instance.TryResolve<T>();
        }
        
        /// <summary>
        /// Container'ı sıfırlar. Sadece test amaçlı kullanılmalıdır.
        /// </summary>
        internal static void Reset()
        {
            lock (_initLock)
            {
                _isInitialized = false;
                ServiceContainer.Instance.Clear();
                Logger.Warning("ServiceBootstrapper sıfırlandı.");
            }
        }
        
        #endregion

        #region Private Methods
        
        /// <summary>
        /// Container'ın başlatıldığından emin olur.
        /// </summary>
        private static void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException(
                    "ServiceBootstrapper henüz başlatılmadı. " +
                    "Önce ServiceBootstrapper.Initialize() çağırın.");
            }
        }
        
        #endregion
    }
}
