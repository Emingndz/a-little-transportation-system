using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Prolab_4.Core.UI
{
    #region Modern Theme

    /// <summary>
    /// Modern UI tema renkleri.
    /// </summary>
    public static class ModernTheme
    {
        // Primary Colors
        public static Color Primary => Color.FromArgb(0, 120, 215);
        public static Color PrimaryDark => Color.FromArgb(0, 84, 150);
        public static Color PrimaryLight => Color.FromArgb(102, 178, 255);
        
        // Accent Colors
        public static Color Accent => Color.FromArgb(0, 153, 76);
        public static Color AccentDark => Color.FromArgb(0, 102, 51);
        public static Color Warning => Color.FromArgb(255, 152, 0);
        public static Color Error => Color.FromArgb(211, 47, 47);
        public static Color Success => Color.FromArgb(46, 125, 50);
        
        // Neutral Colors
        public static Color Background => Color.FromArgb(250, 250, 250);
        public static Color Surface => Color.White;
        public static Color SurfaceHover => Color.FromArgb(245, 245, 245);
        public static Color Border => Color.FromArgb(224, 224, 224);
        public static Color BorderDark => Color.FromArgb(189, 189, 189);
        
        // Text Colors
        public static Color TextPrimary => Color.FromArgb(33, 33, 33);
        public static Color TextSecondary => Color.FromArgb(117, 117, 117);
        public static Color TextDisabled => Color.FromArgb(189, 189, 189);
        public static Color TextOnPrimary => Color.White;
        
        // Shadows
        public static Color Shadow => Color.FromArgb(40, 0, 0, 0);
        public static Color ShadowLight => Color.FromArgb(20, 0, 0, 0);
        
        // Fonts
        public static Font TitleFont => new Font("Segoe UI", 14, FontStyle.Bold);
        public static Font SubtitleFont => new Font("Segoe UI", 11, FontStyle.Regular);
        public static Font BodyFont => new Font("Segoe UI", 10, FontStyle.Regular);
        public static Font CaptionFont => new Font("Segoe UI", 9, FontStyle.Regular);
        public static Font ButtonFont => new Font("Segoe UI", 10, FontStyle.Bold);
        
        /// <summary>
        /// Forma tema uygular.
        /// </summary>
        public static void ApplyToForm(Form form)
        {
            form.BackColor = Background;
            form.Font = BodyFont;
        }
        
        /// <summary>
        /// Panel'e kart stili uygular.
        /// </summary>
        public static void ApplyCardStyle(Panel panel)
        {
            panel.BackColor = Surface;
            panel.Padding = new Padding(16);
        }
    }

    #endregion

    #region Modern Button

    /// <summary>
    /// Modern stil buton.
    /// </summary>
    public class ModernButton : Button
    {
        private Color _hoverColor;
        private Color _pressColor;
        private bool _isHovering;
        private bool _isPressed;
        private int _cornerRadius = 6;

        public ModernButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = ModernTheme.Primary;
            ForeColor = ModernTheme.TextOnPrimary;
            Font = ModernTheme.ButtonFont;
            Cursor = Cursors.Hand;
            Size = new Size(120, 40);
            
            _hoverColor = ModernTheme.PrimaryDark;
            _pressColor = Color.FromArgb(0, 70, 130);
            
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.OptimizedDoubleBuffer, true);
        }

        [Category("Appearance")]
        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = value; Invalidate(); }
        }

        [Category("Appearance")]
        public Color HoverColor
        {
            get => _hoverColor;
            set => _hoverColor = value;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            var color = _isPressed ? _pressColor : (_isHovering ? _hoverColor : BackColor);
            
            if (!Enabled)
            {
                color = ModernTheme.TextDisabled;
            }
            
            using (var path = CreateRoundedRectangle(rect, _cornerRadius))
            using (var brush = new SolidBrush(color))
            {
                e.Graphics.FillPath(brush, path);
            }
            
            // Text
            TextRenderer.DrawText(e.Graphics, Text, Font, rect, 
                Enabled ? ForeColor : ModernTheme.TextDisabled,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovering = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovering = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _isPressed = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _isPressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        private GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
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

    /// <summary>
    /// Outline stil modern buton.
    /// </summary>
    public class ModernOutlineButton : ModernButton
    {
        public ModernOutlineButton()
        {
            BackColor = ModernTheme.Surface;
            ForeColor = ModernTheme.Primary;
            HoverColor = ModernTheme.SurfaceHover;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // Border çiz
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = CreateRoundedPath(rect, CornerRadius))
            using (var pen = new Pen(ModernTheme.Primary, 2))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawPath(pen, path);
            }
        }

        private GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
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

    #region Modern TextBox

    /// <summary>
    /// Modern stil TextBox.
    /// </summary>
    public class ModernTextBox : UserControl
    {
        private readonly TextBox _textBox;
        private readonly Label _labelPlaceholder;
        private readonly Label _labelError;
        private Color _borderColor = ModernTheme.Border;
        private Color _focusBorderColor = ModernTheme.Primary;
        private bool _isFocused;
        private string _errorMessage;
        private int _cornerRadius = 6;

        public ModernTextBox()
        {
            Height = 50;
            
            _textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = ModernTheme.BodyFont,
                Location = new Point(12, 15),
                Width = Width - 24,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            
            _labelPlaceholder = new Label
            {
                Text = "Placeholder",
                Font = ModernTheme.BodyFont,
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(12, 15),
                AutoSize = true,
                Cursor = Cursors.IBeam
            };
            
            _labelError = new Label
            {
                Text = "",
                Font = ModernTheme.CaptionFont,
                ForeColor = ModernTheme.Error,
                Location = new Point(12, 35),
                AutoSize = true,
                Visible = false
            };
            
            Controls.Add(_textBox);
            Controls.Add(_labelPlaceholder);
            Controls.Add(_labelError);
            
            _textBox.GotFocus += (s, e) =>
            {
                _isFocused = true;
                UpdatePlaceholder();
                Invalidate();
            };
            
            _textBox.LostFocus += (s, e) =>
            {
                _isFocused = false;
                UpdatePlaceholder();
                Invalidate();
            };
            
            _textBox.TextChanged += (s, e) =>
            {
                UpdatePlaceholder();
                OnTextChanged(e);
            };
            
            _labelPlaceholder.Click += (s, e) => _textBox.Focus();
            
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        [Category("Appearance")]
        public string Placeholder
        {
            get => _labelPlaceholder.Text;
            set => _labelPlaceholder.Text = value;
        }

        [Category("Appearance")]
        public override string Text
        {
            get => _textBox.Text;
            set => _textBox.Text = value;
        }

        [Category("Appearance")]
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                _labelError.Text = value;
                _labelError.Visible = !string.IsNullOrEmpty(value);
                Invalidate();
            }
        }

        [Category("Behavior")]
        public bool IsPassword
        {
            get => _textBox.UseSystemPasswordChar;
            set => _textBox.UseSystemPasswordChar = value;
        }

        [Category("Behavior")]
        public bool IsMultiline
        {
            get => _textBox.Multiline;
            set
            {
                _textBox.Multiline = value;
                if (value) Height = 100;
            }
        }

        private void UpdatePlaceholder()
        {
            _labelPlaceholder.Visible = string.IsNullOrEmpty(_textBox.Text) && !_isFocused;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            var rect = new Rectangle(0, 0, Width - 1, Height - (_labelError.Visible ? 20 : 1));
            var borderColor = !string.IsNullOrEmpty(_errorMessage) ? ModernTheme.Error :
                              (_isFocused ? _focusBorderColor : _borderColor);
            
            // Background
            using (var path = CreateRoundedRectangle(rect, _cornerRadius))
            using (var brush = new SolidBrush(ModernTheme.Surface))
            {
                e.Graphics.FillPath(brush, path);
            }
            
            // Border
            using (var path = CreateRoundedRectangle(rect, _cornerRadius))
            using (var pen = new Pen(borderColor, _isFocused ? 2 : 1))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _textBox.Width = Width - 24;
        }

        private GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
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

    #region Modern Card

    /// <summary>
    /// Modern kart paneli.
    /// </summary>
    public class ModernCard : Panel
    {
        private int _cornerRadius = 8;
        private int _shadowDepth = 3;
        private Color _shadowColor = ModernTheme.Shadow;
        private bool _showShadow = true;

        public ModernCard()
        {
            BackColor = ModernTheme.Surface;
            Padding = new Padding(16);
            
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        [Category("Appearance")]
        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = value; Invalidate(); }
        }

        [Category("Appearance")]
        public int ShadowDepth
        {
            get => _shadowDepth;
            set { _shadowDepth = value; Invalidate(); }
        }

        [Category("Appearance")]
        public bool ShowShadow
        {
            get => _showShadow;
            set { _showShadow = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            var shadowOffset = _showShadow ? _shadowDepth : 0;
            var rect = new Rectangle(shadowOffset, shadowOffset, 
                Width - 1 - shadowOffset * 2, Height - 1 - shadowOffset * 2);
            
            // Shadow
            if (_showShadow)
            {
                for (int i = _shadowDepth; i > 0; i--)
                {
                    var shadowRect = new Rectangle(rect.X + i, rect.Y + i, rect.Width, rect.Height);
                    var alpha = (int)(40.0 / _shadowDepth * (_shadowDepth - i + 1));
                    using (var path = CreateRoundedRectangle(shadowRect, _cornerRadius))
                    using (var brush = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0)))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }
            }
            
            // Card background
            using (var path = CreateRoundedRectangle(rect, _cornerRadius))
            using (var brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillPath(brush, path);
            }
            
            // Border
            using (var path = CreateRoundedRectangle(rect, _cornerRadius))
            using (var pen = new Pen(ModernTheme.Border, 1))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        private GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
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

    #region Progress Ring

    /// <summary>
    /// Modern circular progress indicator.
    /// </summary>
    public class ProgressRing : Control
    {
        private int _value;
        private int _thickness = 4;
        private Color _progressColor = ModernTheme.Primary;
        private Color _trackColor = ModernTheme.Border;
        private bool _isIndeterminate;
        private int _animationAngle;
        private System.Windows.Forms.Timer _animationTimer;

        public ProgressRing()
        {
            Size = new Size(40, 40);
            
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            
            _animationTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _animationTimer.Tick += (s, e) =>
            {
                _animationAngle = (_animationAngle + 10) % 360;
                Invalidate();
            };
        }

        [Category("Appearance")]
        public int Value
        {
            get => _value;
            set
            {
                _value = Math.Clamp(value, 0, 100);
                Invalidate();
            }
        }

        [Category("Appearance")]
        public int Thickness
        {
            get => _thickness;
            set { _thickness = value; Invalidate(); }
        }

        [Category("Appearance")]
        public Color ProgressColor
        {
            get => _progressColor;
            set { _progressColor = value; Invalidate(); }
        }

        [Category("Behavior")]
        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            set
            {
                _isIndeterminate = value;
                if (value)
                    _animationTimer.Start();
                else
                    _animationTimer.Stop();
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            var rect = new Rectangle(_thickness, _thickness, 
                Width - _thickness * 2, Height - _thickness * 2);
            
            // Track
            using (var pen = new Pen(_trackColor, _thickness))
            {
                e.Graphics.DrawEllipse(pen, rect);
            }
            
            // Progress
            using (var pen = new Pen(_progressColor, _thickness))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                
                if (_isIndeterminate)
                {
                    e.Graphics.DrawArc(pen, rect, _animationAngle, 90);
                }
                else if (_value > 0)
                {
                    var sweepAngle = (int)(360.0 * _value / 100);
                    e.Graphics.DrawArc(pen, rect, -90, sweepAngle);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    #endregion

    #region Status Bar

    /// <summary>
    /// Modern status bar.
    /// </summary>
    public class ModernStatusBar : Panel
    {
        private readonly Label _statusLabel;
        private readonly Label _infoLabel;
        private readonly ProgressRing _progressRing;
        private readonly System.Windows.Forms.Timer _autoHideTimer;

        public ModernStatusBar()
        {
            Height = 32;
            Dock = DockStyle.Bottom;
            BackColor = ModernTheme.Surface;
            BorderStyle = BorderStyle.None;
            Padding = new Padding(12, 0, 12, 0);
            
            // Border top
            var borderTop = new Panel
            {
                Height = 1,
                Dock = DockStyle.Top,
                BackColor = ModernTheme.Border
            };
            Controls.Add(borderTop);
            
            // Progress ring
            _progressRing = new ProgressRing
            {
                Size = new Size(20, 20),
                Location = new Point(12, 6),
                IsIndeterminate = false,
                Visible = false
            };
            Controls.Add(_progressRing);
            
            // Status label
            _statusLabel = new Label
            {
                Text = "Hazır",
                Font = ModernTheme.CaptionFont,
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(40, 8),
                AutoSize = true
            };
            Controls.Add(_statusLabel);
            
            // Info label (sağ taraf)
            _infoLabel = new Label
            {
                Text = "",
                Font = ModernTheme.CaptionFont,
                ForeColor = ModernTheme.TextSecondary,
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoSize = true
            };
            Controls.Add(_infoLabel);
            _infoLabel.Location = new Point(Width - _infoLabel.Width - 12, 8);
            
            _autoHideTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            _autoHideTimer.Tick += (s, e) =>
            {
                SetStatus("Hazır");
                _autoHideTimer.Stop();
            };
        }

        public void SetStatus(string message, bool showProgress = false, int autoHideSeconds = 0)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetStatus(message, showProgress, autoHideSeconds)));
                return;
            }
            
            _statusLabel.Text = message;
            _statusLabel.Location = new Point(showProgress ? 40 : 12, 8);
            _progressRing.Visible = showProgress;
            _progressRing.IsIndeterminate = showProgress;
            
            if (autoHideSeconds > 0)
            {
                _autoHideTimer.Interval = autoHideSeconds * 1000;
                _autoHideTimer.Start();
            }
            else
            {
                _autoHideTimer.Stop();
            }
        }

        public void SetInfo(string info)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetInfo(info)));
                return;
            }
            
            _infoLabel.Text = info;
            _infoLabel.Location = new Point(Width - _infoLabel.Width - 12, 8);
        }

        public void SetProgress(int percent)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetProgress(percent)));
                return;
            }
            
            _progressRing.Visible = true;
            _progressRing.IsIndeterminate = false;
            _progressRing.Value = percent;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _infoLabel.Location = new Point(Width - _infoLabel.Width - 12, 8);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _autoHideTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    #endregion

    #region Confirmation Dialog

    /// <summary>
    /// Modern onay dialog.
    /// </summary>
    public class ModernDialog : Form
    {
        public enum DialogType
        {
            Info,
            Warning,
            Error,
            Question,
            Success
        }

        private DialogType _type;
        private string _message;
        private string[] _buttonTexts;

        public int SelectedButton { get; private set; } = -1;

        private ModernDialog(string title, string message, DialogType type, params string[] buttons)
        {
            _type = type;
            _message = message;
            _buttonTexts = buttons.Length > 0 ? buttons : new[] { "Tamam" };

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = ModernTheme.Surface;
            Size = new Size(400, 180);
            ShowInTaskbar = false;
            
            InitializeComponents(title);
        }

        private void InitializeComponents(string title)
        {
            // Title
            var titleLabel = new Label
            {
                Text = title,
                Font = ModernTheme.SubtitleFont,
                ForeColor = ModernTheme.TextPrimary,
                Location = new Point(60, 20),
                AutoSize = true
            };
            Controls.Add(titleLabel);

            // Icon
            var iconLabel = new Label
            {
                Text = GetIconText(_type),
                Font = new Font("Segoe UI", 24),
                ForeColor = GetIconColor(_type),
                Location = new Point(16, 10),
                Size = new Size(40, 40)
            };
            Controls.Add(iconLabel);

            // Message
            var messageLabel = new Label
            {
                Text = _message,
                Font = ModernTheme.BodyFont,
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(20, 60),
                Size = new Size(360, 60),
                MaximumSize = new Size(360, 0),
                AutoSize = true
            };
            Controls.Add(messageLabel);

            // Adjust height based on message
            Height = Math.Max(180, messageLabel.Bottom + 70);

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Location = new Point(20, Height - 60),
                Size = new Size(360, 40),
                Padding = new Padding(0)
            };
            Controls.Add(buttonPanel);

            for (int i = _buttonTexts.Length - 1; i >= 0; i--)
            {
                var index = i;
                var btn = new ModernButton
                {
                    Text = _buttonTexts[i],
                    Size = new Size(100, 36),
                    Margin = new Padding(8, 0, 0, 0)
                };

                if (i == 0) // Primary button
                {
                    btn.BackColor = ModernTheme.Primary;
                    btn.ForeColor = ModernTheme.TextOnPrimary;
                }
                else
                {
                    btn.BackColor = ModernTheme.SurfaceHover;
                    btn.ForeColor = ModernTheme.TextPrimary;
                }

                btn.Click += (s, e) =>
                {
                    SelectedButton = index;
                    DialogResult = index == 0 ? DialogResult.OK : DialogResult.Cancel;
                    Close();
                };

                buttonPanel.Controls.Add(btn);
            }

            // Border
            Paint += (s, e) =>
            {
                using var pen = new Pen(ModernTheme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            };
        }

        private string GetIconText(DialogType type) => type switch
        {
            DialogType.Info => "ℹ",
            DialogType.Warning => "⚠",
            DialogType.Error => "✕",
            DialogType.Question => "?",
            DialogType.Success => "✓",
            _ => "ℹ"
        };

        private Color GetIconColor(DialogType type) => type switch
        {
            DialogType.Info => ModernTheme.Primary,
            DialogType.Warning => ModernTheme.Warning,
            DialogType.Error => ModernTheme.Error,
            DialogType.Question => ModernTheme.Primary,
            DialogType.Success => ModernTheme.Success,
            _ => ModernTheme.Primary
        };

        #region Static Methods

        public static DialogResult ShowInfo(Form owner, string title, string message)
        {
            using var dialog = new ModernDialog(title, message, DialogType.Info, "Tamam");
            return dialog.ShowDialog(owner);
        }

        public static DialogResult ShowWarning(Form owner, string title, string message)
        {
            using var dialog = new ModernDialog(title, message, DialogType.Warning, "Tamam");
            return dialog.ShowDialog(owner);
        }

        public static DialogResult ShowError(Form owner, string title, string message)
        {
            using var dialog = new ModernDialog(title, message, DialogType.Error, "Tamam");
            return dialog.ShowDialog(owner);
        }

        public static DialogResult ShowSuccess(Form owner, string title, string message)
        {
            using var dialog = new ModernDialog(title, message, DialogType.Success, "Tamam");
            return dialog.ShowDialog(owner);
        }

        public static DialogResult ShowQuestion(Form owner, string title, string message)
        {
            using var dialog = new ModernDialog(title, message, DialogType.Question, "Evet", "Hayır");
            return dialog.ShowDialog(owner);
        }

        public static DialogResult ShowConfirm(Form owner, string title, string message, 
            string confirmText = "Onayla", string cancelText = "İptal")
        {
            using var dialog = new ModernDialog(title, message, DialogType.Question, confirmText, cancelText);
            return dialog.ShowDialog(owner);
        }

        #endregion
    }

    #endregion

    #region Badge

    /// <summary>
    /// Bildirim badge'i.
    /// </summary>
    public class Badge : Control
    {
        private int _count;
        private Color _badgeColor = ModernTheme.Error;

        public Badge()
        {
            Size = new Size(20, 20);
            
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            
            BackColor = Color.Transparent;
        }

        [Category("Appearance")]
        public int Count
        {
            get => _count;
            set
            {
                _count = value;
                Visible = value > 0;
                Invalidate();
            }
        }

        [Category("Appearance")]
        public Color BadgeColor
        {
            get => _badgeColor;
            set { _badgeColor = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_count <= 0) return;
            
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            var text = _count > 99 ? "99+" : _count.ToString();
            var textSize = TextRenderer.MeasureText(text, Font);
            var width = Math.Max(Height, textSize.Width + 8);
            
            Width = width;
            
            // Background
            using (var brush = new SolidBrush(_badgeColor))
            {
                e.Graphics.FillEllipse(brush, 0, 0, Height - 1, Height - 1);
                if (width > Height)
                {
                    e.Graphics.FillRectangle(brush, Height / 2, 0, width - Height, Height);
                    e.Graphics.FillEllipse(brush, width - Height, 0, Height - 1, Height - 1);
                }
            }
            
            // Text
            TextRenderer.DrawText(e.Graphics, text, Font, 
                new Rectangle(0, 0, width, Height), 
                Color.White, 
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    #endregion
}
