using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Prolab_4.Core.UI
{
    /// <summary>
    /// Yükleme göstergesi formu.
    /// Uzun işlemler sırasında kullanıcıya geri bildirim verir.
    /// </summary>
    public class LoadingOverlay : Form
    {
        #region Fields
        
        private readonly Label _messageLabel;
        private readonly ProgressBar _progressBar;
        private readonly Label _percentLabel;
        private readonly Button _cancelButton;
        private CancellationTokenSource _cts;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// İptal butonu görünür mü.
        /// </summary>
        public bool AllowCancel
        {
            get => _cancelButton.Visible;
            set => _cancelButton.Visible = value;
        }
        
        /// <summary>
        /// İşlem iptal edildi mi.
        /// </summary>
        public bool IsCancelled => _cts?.IsCancellationRequested ?? false;
        
        /// <summary>
        /// İptal token'ı.
        /// </summary>
        public CancellationToken CancellationToken => _cts?.Token ?? CancellationToken.None;
        
        #endregion

        #region Constructor
        
        /// <summary>
        /// LoadingOverlay oluşturur.
        /// </summary>
        /// <param name="message">Gösterilecek mesaj</param>
        /// <param name="allowCancel">İptal butonu gösterilsin mi</param>
        public LoadingOverlay(string message = "Lütfen bekleyin...", bool allowCancel = false)
        {
            _cts = new CancellationTokenSource();
            
            // Form ayarları
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Size = new Size(350, 140);
            ShowInTaskbar = false;
            TopMost = true;
            
            // Gölge efekti için border
            Padding = new Padding(2);
            
            // Panel (içerik)
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20)
            };
            Controls.Add(panel);
            
            // Mesaj label
            _messageLabel = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                ForeColor = Color.FromArgb(64, 64, 64),
                Location = new Point(20, 20),
                Size = new Size(310, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(_messageLabel);
            
            // Progress bar
            _progressBar = new ProgressBar
            {
                Location = new Point(20, 55),
                Size = new Size(260, 25),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30
            };
            panel.Controls.Add(_progressBar);
            
            // Yüzde label
            _percentLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(285, 57),
                Size = new Size(45, 20),
                TextAlign = ContentAlignment.MiddleRight,
                Visible = false
            };
            panel.Controls.Add(_percentLabel);
            
            // İptal butonu
            _cancelButton = new Button
            {
                Text = "İptal",
                Font = new Font("Segoe UI", 9),
                Location = new Point(135, 95),
                Size = new Size(80, 30),
                Visible = allowCancel,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(220, 220, 220)
            };
            _cancelButton.Click += (s, e) => Cancel();
            panel.Controls.Add(_cancelButton);
            
            // Form border
            Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(200, 200, 200), 2);
                e.Graphics.DrawRectangle(pen, 1, 1, Width - 2, Height - 2);
            };
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Mesajı günceller.
        /// </summary>
        /// <param name="message">Yeni mesaj</param>
        public void UpdateMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateMessage(message)));
                return;
            }
            _messageLabel.Text = message;
        }
        
        /// <summary>
        /// İlerleme durumunu günceller.
        /// </summary>
        /// <param name="percent">Yüzde (0-100)</param>
        public void UpdateProgress(int percent)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateProgress(percent)));
                return;
            }
            
            if (_progressBar.Style != ProgressBarStyle.Continuous)
            {
                _progressBar.Style = ProgressBarStyle.Continuous;
                _progressBar.Size = new Size(260, 25);
                _percentLabel.Visible = true;
            }
            
            _progressBar.Value = Math.Clamp(percent, 0, 100);
            _percentLabel.Text = $"%{percent}";
        }
        
        /// <summary>
        /// İşlemi iptal eder.
        /// </summary>
        public void Cancel()
        {
            _cts?.Cancel();
            _cancelButton.Enabled = false;
            _cancelButton.Text = "İptal ediliyor...";
        }
        
        #endregion

        #region Static Helper
        
        /// <summary>
        /// Yükleme göstergesi ile async işlem çalıştırır.
        /// </summary>
        /// <typeparam name="T">Sonuç tipi</typeparam>
        /// <param name="owner">Parent form</param>
        /// <param name="message">Gösterilecek mesaj</param>
        /// <param name="operation">Çalıştırılacak işlem</param>
        /// <param name="allowCancel">İptal izni</param>
        /// <returns>İşlem sonucu</returns>
        public static async Task<T> RunAsync<T>(
            Form owner, 
            string message, 
            Func<LoadingOverlay, Task<T>> operation,
            bool allowCancel = false)
        {
            using var overlay = new LoadingOverlay(message, allowCancel);
            
            T result = default;
            Exception error = null;
            
            // İşlemi arka planda çalıştır
            var task = Task.Run(async () =>
            {
                try
                {
                    result = await operation(overlay);
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            });
            
            // Overlay'i modally göster
            overlay.Shown += async (s, e) =>
            {
                await task;
                overlay.Close();
            };
            
            overlay.ShowDialog(owner);
            
            if (error != null)
            {
                throw error;
            }
            
            return result;
        }
        
        /// <summary>
        /// Basit yükleme göstergesi ile işlem çalıştırır.
        /// </summary>
        /// <param name="owner">Parent form</param>
        /// <param name="message">Mesaj</param>
        /// <param name="operation">İşlem</param>
        public static async Task RunAsync(
            Form owner, 
            string message, 
            Func<Task> operation)
        {
            await RunAsync<object>(owner, message, async (overlay) =>
            {
                await operation();
                return null;
            });
        }
        
        #endregion

        #region Dispose
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cts?.Dispose();
            }
            base.Dispose(disposing);
        }
        
        #endregion
    }
}
