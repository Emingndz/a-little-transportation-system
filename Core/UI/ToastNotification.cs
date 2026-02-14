using System;
using System.Drawing;
using System.Windows.Forms;

namespace Prolab_4.Core.UI
{
    /// <summary>
    /// Toast bildirimi (kısa süreli mesaj).
    /// Kullanıcıya anlık geri bildirim vermek için kullanılır.
    /// </summary>
    public class ToastNotification : Form
    {
        #region Enums
        
        /// <summary>
        /// Toast tipi.
        /// </summary>
        public enum ToastType
        {
            Success,
            Error,
            Warning,
            Info
        }
        
        /// <summary>
        /// Toast pozisyonu.
        /// </summary>
        public enum ToastPosition
        {
            TopRight,
            TopCenter,
            BottomRight,
            BottomCenter
        }
        
        #endregion

        #region Static Fields
        
        private static readonly System.Windows.Forms.Timer _autoCloseTimer = new System.Windows.Forms.Timer();
        private static ToastNotification _currentToast;
        
        #endregion

        #region Fields
        
        private readonly Label _iconLabel;
        private readonly Label _messageLabel;
        
        #endregion

        #region Constructor
        
        private ToastNotification(string message, ToastType type)
        {
            // Form ayarları
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            BackColor = GetBackColor(type);
            Size = new Size(320, 50);
            ShowInTaskbar = false;
            TopMost = true;
            Opacity = 0.95;
            
            // Icon label
            _iconLabel = new Label
            {
                Text = GetIcon(type),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(10, 10),
                Size = new Size(30, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(_iconLabel);
            
            // Mesaj label
            _messageLabel = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.White,
                Location = new Point(45, 0),
                Size = new Size(265, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(_messageLabel);
            
            // Kapatma butonu
            var closeBtn = new Label
            {
                Text = "×",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(290, 5),
                Size = new Size(25, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            closeBtn.Click += (s, e) => CloseToast();
            Controls.Add(closeBtn);
            
            // Hover efekti
            closeBtn.MouseEnter += (s, e) => closeBtn.ForeColor = Color.FromArgb(200, 200, 200);
            closeBtn.MouseLeave += (s, e) => closeBtn.ForeColor = Color.White;
            
            // Yuvarlak köşeler
            Region = System.Drawing.Region.FromHrgn(
                CreateRoundRectRgn(0, 0, Width, Height, 8, 8));
        }
        
        #endregion

        #region Static Show Methods
        
        /// <summary>
        /// Başarı bildirimi gösterir.
        /// </summary>
        public static void Success(string message, int durationMs = 3000)
        {
            Show(message, ToastType.Success, durationMs);
        }
        
        /// <summary>
        /// Hata bildirimi gösterir.
        /// </summary>
        public static void Error(string message, int durationMs = 4000)
        {
            Show(message, ToastType.Error, durationMs);
        }
        
        /// <summary>
        /// Uyarı bildirimi gösterir.
        /// </summary>
        public static void Warning(string message, int durationMs = 3500)
        {
            Show(message, ToastType.Warning, durationMs);
        }
        
        /// <summary>
        /// Bilgi bildirimi gösterir.
        /// </summary>
        public static void Info(string message, int durationMs = 3000)
        {
            Show(message, ToastType.Info, durationMs);
        }
        
        /// <summary>
        /// Toast bildirimi gösterir.
        /// </summary>
        public static void Show(string message, ToastType type, int durationMs = 3000, ToastPosition position = ToastPosition.TopRight)
        {
            // Mevcut toast'ı kapat
            CloseCurrentToast();
            
            // Yeni toast oluştur
            _currentToast = new ToastNotification(message, type);
            
            // Pozisyonu ayarla
            SetPosition(_currentToast, position);
            
            // Göster
            _currentToast.Show();
            
            // Otomatik kapatma timer'ı
            _autoCloseTimer.Stop();
            _autoCloseTimer.Interval = durationMs;
            _autoCloseTimer.Tick += (s, e) =>
            {
                _autoCloseTimer.Stop();
                CloseCurrentToast();
            };
            _autoCloseTimer.Start();
        }
        
        #endregion

        #region Private Methods
        
        private static void CloseCurrentToast()
        {
            if (_currentToast != null && !_currentToast.IsDisposed)
            {
                _currentToast.Close();
                _currentToast.Dispose();
                _currentToast = null;
            }
        }
        
        private void CloseToast()
        {
            _autoCloseTimer.Stop();
            Close();
        }
        
        private static void SetPosition(Form toast, ToastPosition position)
        {
            var screen = Screen.PrimaryScreen.WorkingArea;
            int margin = 20;
            
            toast.Location = position switch
            {
                ToastPosition.TopRight => new Point(
                    screen.Right - toast.Width - margin,
                    screen.Top + margin),
                    
                ToastPosition.TopCenter => new Point(
                    (screen.Width - toast.Width) / 2,
                    screen.Top + margin),
                    
                ToastPosition.BottomRight => new Point(
                    screen.Right - toast.Width - margin,
                    screen.Bottom - toast.Height - margin),
                    
                ToastPosition.BottomCenter => new Point(
                    (screen.Width - toast.Width) / 2,
                    screen.Bottom - toast.Height - margin),
                    
                _ => new Point(
                    screen.Right - toast.Width - margin,
                    screen.Top + margin)
            };
        }
        
        private static Color GetBackColor(ToastType type)
        {
            return type switch
            {
                ToastType.Success => Color.FromArgb(46, 125, 50),    // Yeşil
                ToastType.Error => Color.FromArgb(198, 40, 40),      // Kırmızı
                ToastType.Warning => Color.FromArgb(245, 124, 0),    // Turuncu
                ToastType.Info => Color.FromArgb(25, 118, 210),      // Mavi
                _ => Color.FromArgb(66, 66, 66)                       // Gri
            };
        }
        
        private static string GetIcon(ToastType type)
        {
            return type switch
            {
                ToastType.Success => "✓",
                ToastType.Error => "✕",
                ToastType.Warning => "⚠",
                ToastType.Info => "ℹ",
                _ => "●"
            };
        }
        
        #endregion

        #region Win32 API
        
        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect,
            int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);
        
        #endregion
    }
}
