using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Prolab_4.Core.UI
{
    #region Toast Notification (Enhanced)

    /// <summary>
    /// Toast bildirimi tipi.
    /// </summary>
    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// Toast pozisyonu.
    /// </summary>
    public enum ToastPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    /// <summary>
    /// Modern toast notification sistemi.
    /// </summary>
    public class ToastNotificationManager
    {
        #region Singleton

        private static ToastNotificationManager _instance;
        private static readonly object _lock = new object();

        public static ToastNotificationManager Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance ??= new ToastNotificationManager();
                }
            }
        }

        #endregion

        #region Fields

        private Form _containerForm;
        private readonly List<ToastForm> _activeToasts = new List<ToastForm>();
        private ToastPosition _defaultPosition = ToastPosition.BottomRight;
        private int _maxVisibleToasts = 5;
        private int _toastSpacing = 10;

        #endregion

        #region Properties

        public ToastPosition DefaultPosition
        {
            get => _defaultPosition;
            set => _defaultPosition = value;
        }

        public int MaxVisibleToasts
        {
            get => _maxVisibleToasts;
            set => _maxVisibleToasts = value;
        }

        #endregion

        #region Initialization

        public void SetContainer(Form containerForm)
        {
            _containerForm = containerForm;
        }

        #endregion

        #region Show Methods

        public void ShowInfo(string message, string title = null, int durationMs = 3000)
        {
            Show(message, title ?? "Bilgi", ToastType.Info, durationMs);
        }

        public void ShowSuccess(string message, string title = null, int durationMs = 3000)
        {
            Show(message, title ?? "Başarılı", ToastType.Success, durationMs);
        }

        public void ShowWarning(string message, string title = null, int durationMs = 4000)
        {
            Show(message, title ?? "Uyarı", ToastType.Warning, durationMs);
        }

        public void ShowError(string message, string title = null, int durationMs = 5000)
        {
            Show(message, title ?? "Hata", ToastType.Error, durationMs);
        }

        public void Show(string message, string title, ToastType type, int durationMs = 3000, 
            Action onClick = null, Action onClose = null)
        {
            if (_containerForm == null || _containerForm.IsDisposed)
            {
                return;
            }

            if (_containerForm.InvokeRequired)
            {
                _containerForm.BeginInvoke(new Action(() => 
                    Show(message, title, type, durationMs, onClick, onClose)));
                return;
            }

            // Maksimum toast kontrolü
            while (_activeToasts.Count >= _maxVisibleToasts)
            {
                var oldest = _activeToasts.FirstOrDefault();
                if (oldest != null)
                {
                    oldest.Close();
                }
            }

            var toast = new ToastForm(message, title, type, durationMs, onClick, onClose);
            toast.FormClosed += (s, e) =>
            {
                _activeToasts.Remove(toast);
                RepositionToasts();
            };

            _activeToasts.Add(toast);
            PositionToast(toast, _activeToasts.Count - 1);
            toast.Show(_containerForm);
        }

        #endregion

        #region Positioning

        private void PositionToast(ToastForm toast, int index)
        {
            if (_containerForm == null) return;

            var formBounds = _containerForm.Bounds;
            var toastHeight = toast.Height;
            var toastWidth = toast.Width;
            var margin = 20;

            int x, y;

            switch (_defaultPosition)
            {
                case ToastPosition.TopLeft:
                    x = formBounds.Left + margin;
                    y = formBounds.Top + margin + (index * (toastHeight + _toastSpacing));
                    break;
                case ToastPosition.TopCenter:
                    x = formBounds.Left + (formBounds.Width - toastWidth) / 2;
                    y = formBounds.Top + margin + (index * (toastHeight + _toastSpacing));
                    break;
                case ToastPosition.TopRight:
                    x = formBounds.Right - toastWidth - margin;
                    y = formBounds.Top + margin + (index * (toastHeight + _toastSpacing));
                    break;
                case ToastPosition.BottomLeft:
                    x = formBounds.Left + margin;
                    y = formBounds.Bottom - margin - toastHeight - (index * (toastHeight + _toastSpacing));
                    break;
                case ToastPosition.BottomCenter:
                    x = formBounds.Left + (formBounds.Width - toastWidth) / 2;
                    y = formBounds.Bottom - margin - toastHeight - (index * (toastHeight + _toastSpacing));
                    break;
                case ToastPosition.BottomRight:
                default:
                    x = formBounds.Right - toastWidth - margin;
                    y = formBounds.Bottom - margin - toastHeight - (index * (toastHeight + _toastSpacing));
                    break;
            }

            toast.Location = new Point(x, y);
        }

        private void RepositionToasts()
        {
            for (int i = 0; i < _activeToasts.Count; i++)
            {
                PositionToast(_activeToasts[i], i);
            }
        }

        #endregion

        #region Clear

        public void ClearAll()
        {
            foreach (var toast in _activeToasts.ToList())
            {
                toast.Close();
            }
            _activeToasts.Clear();
        }

        #endregion
    }

    /// <summary>
    /// Toast form.
    /// </summary>
    internal class ToastForm : Form
    {
        private readonly System.Windows.Forms.Timer _autoCloseTimer;
        private readonly System.Windows.Forms.Timer _fadeTimer;
        private readonly Action _onClick;
        private readonly Action _onClose;
        private readonly ToastType _type;
        private bool _isClosing;
        private double _opacity = 0;

        public ToastForm(string message, string title, ToastType type, int durationMs, 
            Action onClick, Action onClose)
        {
            _type = type;
            _onClick = onClick;
            _onClose = onClose;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            Size = new Size(320, 90);
            Opacity = 0;

            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);

            InitializeComponents(message, title, type);

            // Auto close timer
            _autoCloseTimer = new System.Windows.Forms.Timer { Interval = durationMs };
            _autoCloseTimer.Tick += (s, e) =>
            {
                _autoCloseTimer.Stop();
                FadeOut();
            };

            // Fade timer
            _fadeTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _fadeTimer.Tick += OnFadeTick;

            // Click handler
            Click += (s, e) =>
            {
                _onClick?.Invoke();
                FadeOut();
            };

            foreach (Control control in Controls)
            {
                control.Click += (s, e) =>
                {
                    _onClick?.Invoke();
                    FadeOut();
                };
            }

            Cursor = onClick != null ? Cursors.Hand : Cursors.Default;
        }

        private void InitializeComponents(string message, string title, ToastType type)
        {
            // Icon
            var iconLabel = new Label
            {
                Text = GetIcon(type),
                Font = new Font("Segoe UI", 20),
                ForeColor = GetColor(type),
                Location = new Point(12, 20),
                Size = new Size(40, 40)
            };
            Controls.Add(iconLabel);

            // Title
            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = ModernTheme.TextPrimary,
                Location = new Point(55, 15),
                Size = new Size(240, 20)
            };
            Controls.Add(titleLabel);

            // Message
            var messageLabel = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 9),
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(55, 38),
                Size = new Size(240, 40)
            };
            Controls.Add(messageLabel);

            // Close button
            var closeButton = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 10),
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(Width - 25, 8),
                Size = new Size(20, 20),
                Cursor = Cursors.Hand
            };
            closeButton.Click += (s, e) => FadeOut();
            closeButton.MouseEnter += (s, e) => closeButton.ForeColor = ModernTheme.TextPrimary;
            closeButton.MouseLeave += (s, e) => closeButton.ForeColor = ModernTheme.TextSecondary;
            Controls.Add(closeButton);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            FadeIn();
        }

        private void FadeIn()
        {
            _isClosing = false;
            _fadeTimer.Start();
        }

        private void FadeOut()
        {
            if (_isClosing) return;
            _isClosing = true;
            _autoCloseTimer.Stop();
            _fadeTimer.Start();
        }

        private void OnFadeTick(object sender, EventArgs e)
        {
            if (_isClosing)
            {
                _opacity -= 0.1;
                if (_opacity <= 0)
                {
                    _fadeTimer.Stop();
                    _onClose?.Invoke();
                    Close();
                    return;
                }
            }
            else
            {
                _opacity += 0.1;
                if (_opacity >= 1)
                {
                    _opacity = 1;
                    _fadeTimer.Stop();
                    _autoCloseTimer.Start();
                }
            }

            Opacity = _opacity;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Background with shadow
            var shadowRect = new Rectangle(3, 3, Width - 6, Height - 6);
            using (var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
            {
                e.Graphics.FillRoundedRectangle(shadowBrush, shadowRect, 8);
            }

            // Main background
            var rect = new Rectangle(0, 0, Width - 6, Height - 6);
            using (var brush = new SolidBrush(ModernTheme.Surface))
            {
                e.Graphics.FillRoundedRectangle(brush, rect, 8);
            }

            // Border
            using (var pen = new Pen(ModernTheme.Border, 1))
            {
                e.Graphics.DrawRoundedRectangle(pen, rect, 8);
            }

            // Left color bar
            using (var brush = new SolidBrush(GetColor(_type)))
            {
                e.Graphics.FillRectangle(brush, 0, 8, 4, Height - 22);
            }
        }

        private string GetIcon(ToastType type) => type switch
        {
            ToastType.Success => "✓",
            ToastType.Warning => "⚠",
            ToastType.Error => "✕",
            _ => "ℹ"
        };

        private Color GetColor(ToastType type) => type switch
        {
            ToastType.Success => ModernTheme.Success,
            ToastType.Warning => ModernTheme.Warning,
            ToastType.Error => ModernTheme.Error,
            _ => ModernTheme.Primary
        };

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _autoCloseTimer?.Dispose();
                _fadeTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    #endregion

    #region Graphics Extensions

    /// <summary>
    /// Graphics extension methods.
    /// </summary>
    public static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle rect, int radius)
        {
            using var path = CreateRoundedRectangle(rect, radius);
            g.FillPath(brush, path);
        }

        public static void DrawRoundedRectangle(this Graphics g, Pen pen, Rectangle rect, int radius)
        {
            using var path = CreateRoundedRectangle(rect, radius);
            g.DrawPath(pen, path);
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            var diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }
    }

    #endregion

    #region Tooltip Manager

    /// <summary>
    /// Modern tooltip yöneticisi.
    /// </summary>
    public class ModernTooltipManager
    {
        private readonly ToolTip _tooltip;
        private readonly Dictionary<Control, string> _registeredControls = new Dictionary<Control, string>();

        public ModernTooltipManager()
        {
            _tooltip = new ToolTip
            {
                AutoPopDelay = 5000,
                InitialDelay = 500,
                ReshowDelay = 100,
                ShowAlways = true,
                BackColor = ModernTheme.Surface,
                ForeColor = ModernTheme.TextPrimary,
                OwnerDraw = true
            };

            _tooltip.Draw += OnTooltipDraw;
            _tooltip.Popup += OnTooltipPopup;
        }

        public void Register(Control control, string text)
        {
            _registeredControls[control] = text;
            _tooltip.SetToolTip(control, text);
        }

        public void Unregister(Control control)
        {
            _registeredControls.Remove(control);
            _tooltip.SetToolTip(control, null);
        }

        private void OnTooltipPopup(object sender, PopupEventArgs e)
        {
            // Minimum genişlik
            e.ToolTipSize = new Size(
                Math.Max(e.ToolTipSize.Width, 100),
                e.ToolTipSize.Height + 10);
        }

        private void OnTooltipDraw(object sender, DrawToolTipEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Background
            using (var brush = new SolidBrush(ModernTheme.Surface))
            {
                e.Graphics.FillRoundedRectangle(brush, e.Bounds, 4);
            }

            // Border
            using (var pen = new Pen(ModernTheme.Border, 1))
            {
                var borderRect = new Rectangle(e.Bounds.X, e.Bounds.Y, 
                    e.Bounds.Width - 1, e.Bounds.Height - 1);
                e.Graphics.DrawRoundedRectangle(pen, borderRect, 4);
            }

            // Text
            var textRect = new Rectangle(
                e.Bounds.X + 8,
                e.Bounds.Y + 5,
                e.Bounds.Width - 16,
                e.Bounds.Height - 10);

            TextRenderer.DrawText(e.Graphics, e.ToolTipText, e.Font,
                textRect, ModernTheme.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }
    }

    #endregion

    #region Notification Center

    /// <summary>
    /// Bildirim merkezi.
    /// </summary>
    public class NotificationCenter
    {
        #region Singleton

        private static NotificationCenter _instance;
        public static NotificationCenter Instance => _instance ??= new NotificationCenter();

        #endregion

        #region Events

        public event EventHandler<NotificationEventArgs> NotificationReceived;
        public event EventHandler<NotificationEventArgs> NotificationClicked;
        public event EventHandler NotificationsCleared;

        #endregion

        #region Fields

        private readonly List<Notification> _notifications = new List<Notification>();
        private readonly object _lock = new object();
        private int _maxNotifications = 100;

        #endregion

        #region Properties

        public IReadOnlyList<Notification> Notifications
        {
            get
            {
                lock (_lock)
                {
                    return _notifications.ToList().AsReadOnly();
                }
            }
        }

        public int UnreadCount
        {
            get
            {
                lock (_lock)
                {
                    return _notifications.Count(n => !n.IsRead);
                }
            }
        }

        #endregion

        #region Methods

        public void Add(Notification notification)
        {
            lock (_lock)
            {
                _notifications.Insert(0, notification);

                while (_notifications.Count > _maxNotifications)
                {
                    _notifications.RemoveAt(_notifications.Count - 1);
                }
            }

            NotificationReceived?.Invoke(this, new NotificationEventArgs(notification));
        }

        public void Add(string title, string message, NotificationType type = NotificationType.Info)
        {
            Add(new Notification
            {
                Id = Guid.NewGuid().ToString(),
                Title = title,
                Message = message,
                Type = type,
                Timestamp = DateTime.Now
            });
        }

        public void MarkAsRead(string notificationId)
        {
            lock (_lock)
            {
                var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
                if (notification != null)
                {
                    notification.IsRead = true;
                }
            }
        }

        public void MarkAllAsRead()
        {
            lock (_lock)
            {
                foreach (var notification in _notifications)
                {
                    notification.IsRead = true;
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _notifications.Clear();
            }

            NotificationsCleared?.Invoke(this, EventArgs.Empty);
        }

        public void Remove(string notificationId)
        {
            lock (_lock)
            {
                _notifications.RemoveAll(n => n.Id == notificationId);
            }
        }

        #endregion
    }

    public class Notification
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public Action OnClick { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error,
        System
    }

    public class NotificationEventArgs : EventArgs
    {
        public Notification Notification { get; }

        public NotificationEventArgs(Notification notification)
        {
            Notification = notification;
        }
    }

    #endregion

    #region Busy Indicator

    /// <summary>
    /// Meşgul göstergesi.
    /// </summary>
    public class BusyIndicator : IDisposable
    {
        private readonly Form _form;
        private readonly Control _control;
        private readonly string _originalText;
        private readonly Cursor _originalCursor;
        private readonly bool _wasEnabled;

        public BusyIndicator(Form form, string busyText = null)
        {
            _form = form;
            _originalCursor = form.Cursor;

            _form.Cursor = Cursors.WaitCursor;

            if (!string.IsNullOrEmpty(busyText))
            {
                // Status bar varsa güncelle
                var statusBar = FindControl<ModernStatusBar>(form);
                statusBar?.SetStatus(busyText, true);
            }
        }

        public BusyIndicator(Control control, string busyText = null)
        {
            _control = control;
            _wasEnabled = control.Enabled;
            _originalText = (control as ButtonBase)?.Text;
            _originalCursor = control.Cursor;

            control.Enabled = false;
            control.Cursor = Cursors.WaitCursor;

            if (!string.IsNullOrEmpty(busyText) && control is ButtonBase button)
            {
                button.Text = busyText;
            }
        }

        private T FindControl<T>(Control parent) where T : Control
        {
            foreach (Control control in parent.Controls)
            {
                if (control is T found) return found;

                var result = FindControl<T>(control);
                if (result != null) return result;
            }
            return null;
        }

        public void Dispose()
        {
            if (_form != null)
            {
                _form.Cursor = _originalCursor;

                var statusBar = FindControl<ModernStatusBar>(_form);
                statusBar?.SetStatus("Hazır", false, 3);
            }

            if (_control != null)
            {
                _control.Enabled = _wasEnabled;
                _control.Cursor = _originalCursor;

                if (!string.IsNullOrEmpty(_originalText) && _control is ButtonBase button)
                {
                    button.Text = _originalText;
                }
            }
        }
    }

    #endregion

    #region Input Validation UI

    /// <summary>
    /// Input validation geri bildirimi.
    /// </summary>
    public class ValidationFeedback
    {
        private readonly ErrorProvider _errorProvider;
        private readonly Dictionary<Control, bool> _validationState = new Dictionary<Control, bool>();

        public ValidationFeedback()
        {
            _errorProvider = new ErrorProvider
            {
                BlinkStyle = ErrorBlinkStyle.NeverBlink
            };
        }

        public void SetError(Control control, string errorMessage)
        {
            _errorProvider.SetError(control, errorMessage);
            _validationState[control] = false;

            if (control is ModernTextBox modernTextBox)
            {
                modernTextBox.ErrorMessage = errorMessage;
            }
        }

        public void ClearError(Control control)
        {
            _errorProvider.SetError(control, null);
            _validationState[control] = true;

            if (control is ModernTextBox modernTextBox)
            {
                modernTextBox.ErrorMessage = null;
            }
        }

        public void ClearAll()
        {
            _errorProvider.Clear();
            _validationState.Clear();
        }

        public bool IsValid => _validationState.Values.All(v => v);

        public bool Validate(Control control, Func<string, bool> validator, string errorMessage)
        {
            var text = control is ModernTextBox mtb ? mtb.Text : control.Text;
            var isValid = validator(text);

            if (isValid)
            {
                ClearError(control);
            }
            else
            {
                SetError(control, errorMessage);
            }

            return isValid;
        }

        public bool ValidateRequired(Control control, string fieldName)
        {
            return Validate(control, 
                text => !string.IsNullOrWhiteSpace(text), 
                $"{fieldName} zorunludur.");
        }

        public bool ValidateEmail(Control control)
        {
            return Validate(control,
                text => string.IsNullOrEmpty(text) || 
                        System.Text.RegularExpressions.Regex.IsMatch(text, 
                            @"^[^@\s]+@[^@\s]+\.[^@\s]+$"),
                "Geçerli bir e-posta adresi girin.");
        }

        public bool ValidateNumeric(Control control)
        {
            return Validate(control,
                text => string.IsNullOrEmpty(text) || double.TryParse(text, out _),
                "Geçerli bir sayı girin.");
        }
    }

    #endregion
}
