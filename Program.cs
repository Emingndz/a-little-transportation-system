using Prolab_4.Core.Exceptions;
using Prolab_4.Core.Logging;

namespace Prolab_4
{
    internal static class Program
    {
        /// <summary>
        /// Uygulamanın ana giriş noktası.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Global exception handler'ı aktif et
            GlobalExceptionHandler.Initialize();
            
            // Logger'ı başlat
            Logger.Info("Uygulama başlatılıyor...");
            
            // Windows Forms konfigürasyonu
            ApplicationConfiguration.Initialize();
            
            try
            {
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                GlobalExceptionHandler.HandleException(ex, new ExceptionContext { IsTerminating = true, Source = "Application.Run" });
            }
            finally
            {
                Logger.Info("Uygulama kapatıldı.");
            }
        }
    }
}