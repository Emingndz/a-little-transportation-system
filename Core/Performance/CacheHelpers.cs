using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prolab_4.Core.Performance
{
    /// <summary>
    /// Thread-safe lazy loader with async support.
    /// İlk erişimde veriyi yükler ve cache'ler.
    /// </summary>
    /// <typeparam name="T">Yüklenecek veri tipi</typeparam>
    public class AsyncLazy<T>
    {
        #region Fields
        
        private readonly Func<Task<T>> _factory;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private T _value;
        private bool _isLoaded;
        private Exception _loadException;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Veri yüklendi mi.
        /// </summary>
        public bool IsLoaded => _isLoaded;
        
        /// <summary>
        /// Yükleme hatası (varsa).
        /// </summary>
        public Exception LoadException => _loadException;
        
        #endregion

        #region Constructor
        
        /// <summary>
        /// AsyncLazy oluşturur.
        /// </summary>
        /// <param name="factory">Veri yükleme fonksiyonu</param>
        public AsyncLazy(Func<Task<T>> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }
        
        /// <summary>
        /// Sync factory ile AsyncLazy oluşturur.
        /// </summary>
        /// <param name="factory">Senkron veri yükleme fonksiyonu</param>
        public AsyncLazy(Func<T> factory) : this(() => Task.FromResult(factory()))
        {
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Veriyi getirir. İlk çağrıda yükler.
        /// </summary>
        /// <returns>Yüklenen veri</returns>
        public async Task<T> GetValueAsync()
        {
            if (_isLoaded)
            {
                if (_loadException != null)
                {
                    throw _loadException;
                }
                return _value;
            }
            
            await _semaphore.WaitAsync();
            try
            {
                if (_isLoaded)
                {
                    if (_loadException != null)
                    {
                        throw _loadException;
                    }
                    return _value;
                }
                
                try
                {
                    _value = await _factory();
                    _isLoaded = true;
                }
                catch (Exception ex)
                {
                    _loadException = ex;
                    _isLoaded = true;
                    throw;
                }
                
                return _value;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        /// <summary>
        /// Veriyi sıfırlar. Sonraki erişimde tekrar yüklenir.
        /// </summary>
        public void Reset()
        {
            _semaphore.Wait();
            try
            {
                _isLoaded = false;
                _value = default;
                _loadException = null;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        /// <summary>
        /// Veriyi yeniden yükler.
        /// </summary>
        public async Task<T> RefreshAsync()
        {
            Reset();
            return await GetValueAsync();
        }
        
        #endregion
    }
    
    /// <summary>
    /// Zamana dayalı cache wrapper.
    /// Belirli süre sonra otomatik expire olur.
    /// </summary>
    /// <typeparam name="T">Cache'lenecek veri tipi</typeparam>
    public class TimedCache<T>
    {
        #region Fields
        
        private readonly Func<Task<T>> _factory;
        private readonly TimeSpan _duration;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        
        private T _value;
        private DateTime _lastLoadTime = DateTime.MinValue;
        private bool _isLoaded;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Cache süresi doldu mu.
        /// </summary>
        public bool IsExpired => DateTime.Now - _lastLoadTime > _duration;
        
        /// <summary>
        /// Veri yüklü ve geçerli mi.
        /// </summary>
        public bool IsValid => _isLoaded && !IsExpired;
        
        #endregion

        #region Constructor
        
        /// <summary>
        /// TimedCache oluşturur.
        /// </summary>
        /// <param name="factory">Veri yükleme fonksiyonu</param>
        /// <param name="duration">Cache süresi</param>
        public TimedCache(Func<Task<T>> factory, TimeSpan duration)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _duration = duration;
        }
        
        /// <summary>
        /// Dakika cinsinden cache süresi ile TimedCache oluşturur.
        /// </summary>
        public TimedCache(Func<Task<T>> factory, int durationMinutes) 
            : this(factory, TimeSpan.FromMinutes(durationMinutes))
        {
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Veriyi getirir. Expire olmuşsa yeniden yükler.
        /// </summary>
        public async Task<T> GetValueAsync()
        {
            if (IsValid)
            {
                return _value;
            }
            
            await _semaphore.WaitAsync();
            try
            {
                if (IsValid)
                {
                    return _value;
                }
                
                _value = await _factory();
                _lastLoadTime = DateTime.Now;
                _isLoaded = true;
                
                return _value;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        /// <summary>
        /// Cache'i temizler.
        /// </summary>
        public void Invalidate()
        {
            _semaphore.Wait();
            try
            {
                _isLoaded = false;
                _value = default;
                _lastLoadTime = DateTime.MinValue;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        /// <summary>
        /// Veriyi yeniden yükler.
        /// </summary>
        public async Task<T> RefreshAsync()
        {
            Invalidate();
            return await GetValueAsync();
        }
        
        #endregion
    }
    
    /// <summary>
    /// Object pool for reusable objects.
    /// Memory allocation'ı azaltmak için kullanılır.
    /// </summary>
    /// <typeparam name="T">Pool'lanacak obje tipi</typeparam>
    public class ObjectPool<T> where T : class
    {
        #region Fields
        
        private readonly Func<T> _factory;
        private readonly Action<T> _reset;
        private readonly int _maxSize;
        private readonly System.Collections.Concurrent.ConcurrentBag<T> _pool;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Pool'daki mevcut obje sayısı.
        /// </summary>
        public int Count => _pool.Count;
        
        #endregion

        #region Constructor
        
        /// <summary>
        /// ObjectPool oluşturur.
        /// </summary>
        /// <param name="factory">Yeni obje oluşturma fonksiyonu</param>
        /// <param name="reset">Objeyi sıfırlama fonksiyonu (opsiyonel)</param>
        /// <param name="maxSize">Maximum pool boyutu</param>
        public ObjectPool(Func<T> factory, Action<T> reset = null, int maxSize = 100)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _reset = reset;
            _maxSize = maxSize;
            _pool = new System.Collections.Concurrent.ConcurrentBag<T>();
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Pool'dan obje alır veya yeni oluşturur.
        /// </summary>
        public T Rent()
        {
            if (_pool.TryTake(out var item))
            {
                return item;
            }
            return _factory();
        }
        
        /// <summary>
        /// Objeyi pool'a geri döndürür.
        /// </summary>
        public void Return(T item)
        {
            if (item == null) return;
            
            if (_pool.Count < _maxSize)
            {
                _reset?.Invoke(item);
                _pool.Add(item);
            }
        }
        
        /// <summary>
        /// Pool'u temizler.
        /// </summary>
        public void Clear()
        {
            while (_pool.TryTake(out _)) { }
        }
        
        #endregion
    }
}
