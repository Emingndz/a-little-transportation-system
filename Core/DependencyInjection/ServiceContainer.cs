using System;
using System.Collections.Generic;
using Prolab_4.Core.Logging;

namespace Prolab_4.Core.DependencyInjection
{
    /// <summary>
    /// Basit ve hafif bir Dependency Injection Container.
    /// Singleton pattern ile uygulama genelinde tek instance olarak çalışır.
    /// 
    /// Kullanım:
    /// 1. Kayıt: ServiceContainer.Instance.Register&lt;IService, ServiceImpl&gt;();
    /// 2. Çözümleme: var service = ServiceContainer.Instance.Resolve&lt;IService&gt;();
    /// 
    /// Not: Production uygulamalarında Microsoft.Extensions.DependencyInjection
    /// veya Autofac gibi olgun kütüphaneler tercih edilebilir.
    /// </summary>
    public sealed class ServiceContainer
    {
        #region Singleton Implementation
        
        private static readonly Lazy<ServiceContainer> _instance = 
            new Lazy<ServiceContainer>(() => new ServiceContainer());
        
        /// <summary>
        /// ServiceContainer'ın global tek instance'ı
        /// </summary>
        public static ServiceContainer Instance => _instance.Value;
        
        // Private constructor - dışarıdan instance oluşturulamaz
        private ServiceContainer() 
        {
            _registrations = new Dictionary<Type, ServiceRegistration>();
            _singletonInstances = new Dictionary<Type, object>();
        }
        
        #endregion

        #region Private Fields
        
        // Servis kayıtlarını tutan dictionary
        // Key: Interface tipi, Value: Kayıt bilgisi
        private readonly Dictionary<Type, ServiceRegistration> _registrations;
        
        // Singleton olarak işaretlenmiş servislerin instance'larını cache'ler
        private readonly Dictionary<Type, object> _singletonInstances;
        
        // Thread safety için lock objesi
        private readonly object _lock = new object();
        
        #endregion

        #region Registration Classes
        
        /// <summary>
        /// Servis yaşam döngüsü seçenekleri
        /// </summary>
        public enum ServiceLifetime
        {
            /// <summary>Her çağrıda yeni instance oluşturulur</summary>
            Transient,
            
            /// <summary>Uygulama boyunca tek instance kullanılır</summary>
            Singleton
        }
        
        /// <summary>
        /// Bir servis kaydının detaylarını tutar
        /// </summary>
        private class ServiceRegistration
        {
            public Type ImplementationType { get; set; }
            public ServiceLifetime Lifetime { get; set; }
            public Func<object> Factory { get; set; }
        }
        
        #endregion

        #region Public Registration Methods
        
        /// <summary>
        /// Bir interface'i implementation'a bağlar.
        /// Varsayılan olarak Transient (her çağrıda yeni instance) lifetime kullanır.
        /// </summary>
        /// <typeparam name="TInterface">Servis interface'i</typeparam>
        /// <typeparam name="TImplementation">Concrete implementation</typeparam>
        /// <param name="lifetime">Servis yaşam döngüsü</param>
        public void Register<TInterface, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TImplementation : class, TInterface, new()
        {
            lock (_lock)
            {
                var interfaceType = typeof(TInterface);
                
                _registrations[interfaceType] = new ServiceRegistration
                {
                    ImplementationType = typeof(TImplementation),
                    Lifetime = lifetime,
                    Factory = () => new TImplementation()
                };
                
                Logger.Debug($"Servis kaydedildi: {interfaceType.Name} -> {typeof(TImplementation).Name} ({lifetime})");
            }
        }
        
        /// <summary>
        /// Bir interface'i factory fonksiyonu ile bağlar.
        /// Constructor parametresi olan servisler için kullanılır.
        /// </summary>
        /// <typeparam name="TInterface">Servis interface'i</typeparam>
        /// <param name="factory">Instance oluşturan factory fonksiyonu</param>
        /// <param name="lifetime">Servis yaşam döngüsü</param>
        public void Register<TInterface>(Func<TInterface> factory, ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TInterface : class
        {
            lock (_lock)
            {
                var interfaceType = typeof(TInterface);
                
                _registrations[interfaceType] = new ServiceRegistration
                {
                    ImplementationType = null, // Factory kullanıldığında tip bilgisi yok
                    Lifetime = lifetime,
                    Factory = () => factory()
                };
                
                Logger.Debug($"Servis (factory ile) kaydedildi: {interfaceType.Name} ({lifetime})");
            }
        }
        
        /// <summary>
        /// Önceden oluşturulmuş bir instance'ı singleton olarak kaydeder.
        /// Dışarıda oluşturulmuş objeler için kullanılır.
        /// </summary>
        /// <typeparam name="TInterface">Servis interface'i</typeparam>
        /// <param name="instance">Kullanılacak instance</param>
        public void RegisterInstance<TInterface>(TInterface instance)
            where TInterface : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance), "Instance null olamaz.");
            
            lock (_lock)
            {
                var interfaceType = typeof(TInterface);
                
                _registrations[interfaceType] = new ServiceRegistration
                {
                    ImplementationType = instance.GetType(),
                    Lifetime = ServiceLifetime.Singleton,
                    Factory = () => instance
                };
                
                // Direkt singleton cache'e ekle
                _singletonInstances[interfaceType] = instance;
                
                Logger.Debug($"Instance kaydedildi: {interfaceType.Name} -> {instance.GetType().Name}");
            }
        }
        
        #endregion

        #region Public Resolution Methods
        
        /// <summary>
        /// Kayıtlı bir servisi çözümler ve instance döndürür.
        /// </summary>
        /// <typeparam name="TInterface">İstenen servis interface'i</typeparam>
        /// <returns>Servis instance'ı</returns>
        /// <exception cref="InvalidOperationException">Servis kayıtlı değilse</exception>
        public TInterface Resolve<TInterface>()
            where TInterface : class
        {
            var interfaceType = typeof(TInterface);
            
            lock (_lock)
            {
                // Kayıt kontrolü
                if (!_registrations.TryGetValue(interfaceType, out var registration))
                {
                    var errorMsg = $"Servis bulunamadı: {interfaceType.Name}. Önce Register() ile kaydedin.";
                    Logger.Error(errorMsg);
                    throw new InvalidOperationException(errorMsg);
                }
                
                // Singleton ise cache'den dön
                if (registration.Lifetime == ServiceLifetime.Singleton)
                {
                    if (_singletonInstances.TryGetValue(interfaceType, out var existingInstance))
                    {
                        return (TInterface)existingInstance;
                    }
                    
                    // İlk kez oluştur ve cache'le
                    var newInstance = registration.Factory();
                    _singletonInstances[interfaceType] = newInstance;
                    return (TInterface)newInstance;
                }
                
                // Transient - her seferinde yeni instance
                return (TInterface)registration.Factory();
            }
        }
        
        /// <summary>
        /// Servisin kayıtlı olup olmadığını kontrol eder.
        /// </summary>
        /// <typeparam name="TInterface">Kontrol edilecek interface</typeparam>
        /// <returns>Kayıtlı ise true</returns>
        public bool IsRegistered<TInterface>()
        {
            lock (_lock)
            {
                return _registrations.ContainsKey(typeof(TInterface));
            }
        }
        
        /// <summary>
        /// Servisi çözümlemeye çalışır, başarısız olursa null döner.
        /// Exception fırlatmaz.
        /// </summary>
        /// <typeparam name="TInterface">İstenen servis interface'i</typeparam>
        /// <returns>Servis instance'ı veya null</returns>
        public TInterface TryResolve<TInterface>()
            where TInterface : class
        {
            try
            {
                if (IsRegistered<TInterface>())
                {
                    return Resolve<TInterface>();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
        
        #endregion

        #region Utility Methods
        
        /// <summary>
        /// Tüm kayıtları temizler.
        /// Genellikle test senaryolarında kullanılır.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _registrations.Clear();
                _singletonInstances.Clear();
                Logger.Info("ServiceContainer temizlendi.");
            }
        }
        
        /// <summary>
        /// Kayıtlı servis sayısını döndürür.
        /// </summary>
        public int RegistrationCount
        {
            get
            {
                lock (_lock)
                {
                    return _registrations.Count;
                }
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// ServiceContainer için extension metodları.
    /// Daha akıcı bir API sağlar.
    /// </summary>
    public static class ServiceContainerExtensions
    {
        /// <summary>
        /// Servisi singleton olarak kaydeder (kısayol metod).
        /// </summary>
        public static void RegisterSingleton<TInterface, TImplementation>(this ServiceContainer container)
            where TImplementation : class, TInterface, new()
        {
            container.Register<TInterface, TImplementation>(ServiceContainer.ServiceLifetime.Singleton);
        }
        
        /// <summary>
        /// Servisi transient olarak kaydeder (kısayol metod).
        /// </summary>
        public static void RegisterTransient<TInterface, TImplementation>(this ServiceContainer container)
            where TImplementation : class, TInterface, new()
        {
            container.Register<TInterface, TImplementation>(ServiceContainer.ServiceLifetime.Transient);
        }
    }
}
