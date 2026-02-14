using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Prolab_4.Core.UI
{
    /// <summary>
    /// Profesyonel UI Tema Sistemi - Modern, temiz ve kullanıcı dostu tasarım.
    /// </summary>
    public static class ProfessionalTheme
    {
        #region Color Palette - Metro/Fluent Inspired

        // Primary - Ana marka rengi
        public static Color Primary => Color.FromArgb(0, 122, 204);        // Microsoft Blue
        public static Color PrimaryDark => Color.FromArgb(0, 84, 166);
        public static Color PrimaryLight => Color.FromArgb(51, 153, 255);
        public static Color PrimaryTransparent => Color.FromArgb(30, 0, 122, 204);

        // Secondary - İkincil vurgu rengi
        public static Color Secondary => Color.FromArgb(45, 137, 239);
        public static Color SecondaryDark => Color.FromArgb(25, 117, 219);

        // Accent Colors - Durum renkleri
        public static Color Success => Color.FromArgb(16, 185, 129);       // Emerald Green
        public static Color Warning => Color.FromArgb(245, 158, 11);       // Amber
        public static Color Error => Color.FromArgb(239, 68, 68);          // Red
        public static Color Info => Color.FromArgb(59, 130, 246);          // Blue

        // Transport Colors - Araç renkleri
        public static Color TaxiColor => Color.FromArgb(251, 191, 36);     // Sarı/Turuncu
        public static Color BusColor => Color.FromArgb(59, 130, 246);      // Mavi
        public static Color TramColor => Color.FromArgb(16, 185, 129);     // Yeşil
        public static Color WalkColor => Color.FromArgb(139, 92, 246);     // Mor
        public static Color TransferColor => Color.FromArgb(236, 72, 153); // Pembe

        // Background Colors - Arka plan renkleri
        public static Color Background => Color.FromArgb(249, 250, 251);   // Çok açık gri
        public static Color BackgroundDark => Color.FromArgb(31, 41, 55);  // Koyu tema için
        public static Color Surface => Color.White;
        public static Color SurfaceHover => Color.FromArgb(243, 244, 246);
        public static Color SurfaceActive => Color.FromArgb(229, 231, 235);

        // Border Colors
        public static Color Border => Color.FromArgb(229, 231, 235);
        public static Color BorderLight => Color.FromArgb(243, 244, 246);
        public static Color BorderDark => Color.FromArgb(156, 163, 175);
        public static Color BorderFocus => Primary;

        // Text Colors
        public static Color TextPrimary => Color.FromArgb(17, 24, 39);
        public static Color TextSecondary => Color.FromArgb(107, 114, 128);
        public static Color TextMuted => Color.FromArgb(156, 163, 175);
        public static Color TextOnPrimary => Color.White;
        public static Color TextLink => Primary;

        // Shadow Colors
        public static Color Shadow => Color.FromArgb(30, 0, 0, 0);
        public static Color ShadowMedium => Color.FromArgb(50, 0, 0, 0);
        public static Color ShadowDark => Color.FromArgb(80, 0, 0, 0);

        #endregion

        #region Typography

        public static Font FontHeading1 => new Font("Segoe UI", 24, FontStyle.Bold);
        public static Font FontHeading2 => new Font("Segoe UI", 18, FontStyle.Bold);
        public static Font FontHeading3 => new Font("Segoe UI", 14, FontStyle.Bold);
        public static Font FontBody => new Font("Segoe UI", 10, FontStyle.Regular);
        public static Font FontBodyBold => new Font("Segoe UI", 10, FontStyle.Bold);
        public static Font FontCaption => new Font("Segoe UI", 9, FontStyle.Regular);
        public static Font FontButton => new Font("Segoe UI Semibold", 10, FontStyle.Bold);
        public static Font FontSmall => new Font("Segoe UI", 8, FontStyle.Regular);

        #endregion

        #region Spacing & Sizing

        public static int SpacingXS => 4;
        public static int SpacingS => 8;
        public static int SpacingM => 16;
        public static int SpacingL => 24;
        public static int SpacingXL => 32;

        public static int BorderRadius => 8;
        public static int BorderRadiusSmall => 4;
        public static int BorderRadiusLarge => 12;

        public static int ButtonHeight => 44;
        public static int InputHeight => 40;
        public static int CardPadding => 20;

        #endregion

        #region Apply Methods

        /// <summary>
        /// Forma profesyonel tema uygular.
        /// </summary>
        public static void ApplyToForm(Form form, string title = null)
        {
            form.BackColor = Background;
            form.Font = FontBody;
            form.FormBorderStyle = FormBorderStyle.FixedSingle;
            form.StartPosition = FormStartPosition.CenterScreen;

            if (!string.IsNullOrEmpty(title))
                form.Text = title;
        }

        /// <summary>
        /// Panel'e kart stili uygular.
        /// </summary>
        public static void ApplyCardStyle(Panel panel, bool withShadow = true)
        {
            panel.BackColor = Surface;
            panel.Padding = new Padding(CardPadding);

            if (withShadow)
            {
                panel.Paint += (s, e) =>
                {
                    var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                    using (var path = CreateRoundedRectangle(rect, BorderRadius))
                    using (var pen = new Pen(Border, 1))
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        e.Graphics.DrawPath(pen, path);
                    }
                };
            }
        }

        /// <summary>
        /// Butona profesyonel stil uygular.
        /// </summary>
        public static void ApplyButtonStyle(Button button, ButtonStyle style = ButtonStyle.Primary)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = FontButton;
            button.Cursor = Cursors.Hand;
            button.Height = ButtonHeight;
            button.Padding = new Padding(SpacingM, 0, SpacingM, 0);

            switch (style)
            {
                case ButtonStyle.Primary:
                    button.BackColor = Primary;
                    button.ForeColor = TextOnPrimary;
                    button.FlatAppearance.MouseOverBackColor = PrimaryDark;
                    button.FlatAppearance.MouseDownBackColor = PrimaryLight;
                    break;

                case ButtonStyle.Secondary:
                    button.BackColor = Surface;
                    button.ForeColor = TextPrimary;
                    button.FlatAppearance.BorderSize = 1;
                    button.FlatAppearance.BorderColor = Border;
                    button.FlatAppearance.MouseOverBackColor = SurfaceHover;
                    button.FlatAppearance.MouseDownBackColor = SurfaceActive;
                    break;

                case ButtonStyle.Success:
                    button.BackColor = Success;
                    button.ForeColor = TextOnPrimary;
                    button.FlatAppearance.MouseOverBackColor = Color.FromArgb(14, 165, 115);
                    break;

                case ButtonStyle.Danger:
                    button.BackColor = Error;
                    button.ForeColor = TextOnPrimary;
                    button.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 50, 50);
                    break;

                case ButtonStyle.Warning:
                    button.BackColor = Warning;
                    button.ForeColor = TextOnPrimary;
                    button.FlatAppearance.MouseOverBackColor = Color.FromArgb(225, 140, 10);
                    break;
            }

            // Rounded corners paint
            button.Paint += (s, e) => DrawRoundedButton(button, e, BorderRadius);
        }

        /// <summary>
        /// ComboBox'a modern stil uygular.
        /// </summary>
        public static void ApplyComboBoxStyle(ComboBox comboBox)
        {
            comboBox.FlatStyle = FlatStyle.Flat;
            comboBox.BackColor = Surface;
            comboBox.ForeColor = TextPrimary;
            comboBox.Font = FontBody;
            comboBox.Height = InputHeight;
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        /// <summary>
        /// TextBox'a modern stil uygular.
        /// </summary>
        public static void ApplyTextBoxStyle(TextBox textBox)
        {
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.BackColor = Surface;
            textBox.ForeColor = TextPrimary;
            textBox.Font = FontBody;
            textBox.Height = InputHeight;

            textBox.Enter += (s, e) =>
            {
                textBox.BackColor = Color.FromArgb(255, 255, 255);
            };

            textBox.Leave += (s, e) =>
            {
                textBox.BackColor = Surface;
            };
        }

        /// <summary>
        /// Label'a stil uygular.
        /// </summary>
        public static void ApplyLabelStyle(Label label, LabelStyle style = LabelStyle.Body)
        {
            label.BackColor = Color.Transparent;

            switch (style)
            {
                case LabelStyle.Heading1:
                    label.Font = FontHeading1;
                    label.ForeColor = TextPrimary;
                    break;
                case LabelStyle.Heading2:
                    label.Font = FontHeading2;
                    label.ForeColor = TextPrimary;
                    break;
                case LabelStyle.Heading3:
                    label.Font = FontHeading3;
                    label.ForeColor = TextPrimary;
                    break;
                case LabelStyle.Body:
                    label.Font = FontBody;
                    label.ForeColor = TextPrimary;
                    break;
                case LabelStyle.Caption:
                    label.Font = FontCaption;
                    label.ForeColor = TextSecondary;
                    break;
                case LabelStyle.Muted:
                    label.Font = FontBody;
                    label.ForeColor = TextMuted;
                    break;
            }
        }

        /// <summary>
        /// RadioButton'a modern stil uygular.
        /// </summary>
        public static void ApplyRadioButtonStyle(RadioButton radioButton)
        {
            radioButton.Font = FontBody;
            radioButton.ForeColor = TextPrimary;
            radioButton.BackColor = Color.Transparent;
            radioButton.Cursor = Cursors.Hand;
            radioButton.Padding = new Padding(SpacingXS);
        }

        /// <summary>
        /// DataGridView'a profesyonel stil uygular.
        /// </summary>
        public static void ApplyDataGridViewStyle(DataGridView dgv)
        {
            // Genel ayarlar
            dgv.BorderStyle = BorderStyle.None;
            dgv.BackgroundColor = Surface;
            dgv.GridColor = BorderLight;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.EnableHeadersVisualStyles = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToResizeRows = false;
            dgv.RowHeadersVisible = false;

            // Header stili
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Primary,
                ForeColor = TextOnPrimary,
                Font = FontBodyBold,
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(SpacingM, SpacingS, SpacingM, SpacingS),
                SelectionBackColor = Primary,
                SelectionForeColor = TextOnPrimary
            };
            dgv.ColumnHeadersHeight = 48;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            // Satır stili
            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Surface,
                ForeColor = TextPrimary,
                Font = FontBody,
                SelectionBackColor = PrimaryTransparent,
                SelectionForeColor = TextPrimary,
                Padding = new Padding(SpacingM, SpacingS, SpacingM, SpacingS)
            };
            dgv.RowTemplate.Height = 44;

            // Alternatif satır rengi
            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(249, 250, 251),
                ForeColor = TextPrimary,
                SelectionBackColor = PrimaryTransparent,
                SelectionForeColor = TextPrimary
            };

            // Hover efekti
            dgv.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor = SurfaceHover;
                }
            };

            dgv.CellMouseLeave += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor =
                        e.RowIndex % 2 == 0 ? Surface : Color.FromArgb(249, 250, 251);
                }
            };
        }

        /// <summary>
        /// GMapControl'e stil uygular.
        /// </summary>
        public static void ApplyMapControlStyle(Control mapControl)
        {
            // Harita kontrolü etrafına border ekle
            var parent = mapControl.Parent;
            if (parent != null)
            {
                mapControl.Margin = new Padding(1);
            }
        }

        #endregion

        #region Helper Methods

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

        private static void DrawRoundedButton(Button button, PaintEventArgs e, int radius)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (var path = CreateRoundedRectangle(new Rectangle(0, 0, button.Width, button.Height), radius))
            {
                button.Region = new Region(path);
            }
        }

        #endregion

        #region Enums

        public enum ButtonStyle
        {
            Primary,
            Secondary,
            Success,
            Danger,
            Warning
        }

        public enum LabelStyle
        {
            Heading1,
            Heading2,
            Heading3,
            Body,
            Caption,
            Muted
        }

        #endregion
    }

    #region Professional Components

    /// <summary>
    /// Profesyonel kart panel komponenti.
    /// </summary>
    public class ProfessionalCard : Panel
    {
        private int _borderRadius = 12;
        private Color _shadowColor = Color.FromArgb(20, 0, 0, 0);
        private bool _hasShadow = true;
        private string _title = "";
        private Color _headerColor = ProfessionalTheme.Primary;

        public ProfessionalCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);

            BackColor = ProfessionalTheme.Surface;
            Padding = new Padding(ProfessionalTheme.CardPadding);
        }

        public int BorderRadius
        {
            get => _borderRadius;
            set { _borderRadius = value; Invalidate(); }
        }

        public bool HasShadow
        {
            get => _hasShadow;
            set { _hasShadow = value; Invalidate(); }
        }

        public string Title
        {
            get => _title;
            set { _title = value; Invalidate(); }
        }

        public Color HeaderColor
        {
            get => _headerColor;
            set { _headerColor = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(2, 2, Width - 5, Height - 5);

            // Shadow
            if (_hasShadow)
            {
                using (var shadowPath = CreateRoundedRectPath(new Rectangle(rect.X + 2, rect.Y + 2, rect.Width, rect.Height), _borderRadius))
                using (var shadowBrush = new SolidBrush(_shadowColor))
                {
                    e.Graphics.FillPath(shadowBrush, shadowPath);
                }
            }

            // Main card
            using (var path = CreateRoundedRectPath(rect, _borderRadius))
            using (var brush = new SolidBrush(BackColor))
            using (var pen = new Pen(ProfessionalTheme.Border, 1))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }

            // Header with title
            if (!string.IsNullOrEmpty(_title))
            {
                var headerRect = new Rectangle(rect.X, rect.Y, rect.Width, 50);
                using (var headerPath = CreateRoundedRectPath(headerRect, _borderRadius, true))
                using (var headerBrush = new SolidBrush(_headerColor))
                {
                    e.Graphics.FillPath(headerBrush, headerPath);
                }

                using (var titleBrush = new SolidBrush(ProfessionalTheme.TextOnPrimary))
                {
                    var titleRect = new RectangleF(rect.X + 16, rect.Y + 12, rect.Width - 32, 30);
                    e.Graphics.DrawString(_title, ProfessionalTheme.FontHeading3, titleBrush, titleRect);
                }
            }
        }

        private GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius, bool topOnly = false)
        {
            var path = new GraphicsPath();
            var diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);

            if (topOnly)
            {
                path.AddLine(rect.Right, rect.Bottom, rect.Left, rect.Bottom);
            }
            else
            {
                path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            }

            path.CloseFigure();
            return path;
        }
    }

    /// <summary>
    /// Transport badge - araç tipi göstergesi.
    /// </summary>
    public class TransportBadge : Label
    {
        private TransportType _transportType = TransportType.Bus;
        private int _borderRadius = 6;

        public TransportBadge()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);

            AutoSize = false;
            Size = new Size(120, 28);
            TextAlign = ContentAlignment.MiddleCenter;
            Font = ProfessionalTheme.FontCaption;
        }

        public TransportType Type
        {
            get => _transportType;
            set
            {
                _transportType = value;
                UpdateAppearance();
                Invalidate();
            }
        }

        private void UpdateAppearance()
        {
            switch (_transportType)
            {
                case TransportType.Taxi:
                    Text = "🚖 Taksi";
                    BackColor = ProfessionalTheme.TaxiColor;
                    ForeColor = Color.Black;
                    break;
                case TransportType.Bus:
                    Text = "🚌 Otobüs";
                    BackColor = ProfessionalTheme.BusColor;
                    ForeColor = Color.White;
                    break;
                case TransportType.Tram:
                    Text = "🚋 Tramvay";
                    BackColor = ProfessionalTheme.TramColor;
                    ForeColor = Color.White;
                    break;
                case TransportType.Walk:
                    Text = "🚶 Yürüyüş";
                    BackColor = ProfessionalTheme.WalkColor;
                    ForeColor = Color.White;
                    break;
                case TransportType.Transfer:
                    Text = "🔄 Aktarma";
                    BackColor = ProfessionalTheme.TransferColor;
                    ForeColor = Color.White;
                    break;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = CreateRoundedRectPath(rect, _borderRadius))
            using (var brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillPath(brush, path);
            }

            // Text
            var textSize = e.Graphics.MeasureString(Text, Font);
            var textX = (Width - textSize.Width) / 2;
            var textY = (Height - textSize.Height) / 2;

            using (var textBrush = new SolidBrush(ForeColor))
            {
                e.Graphics.DrawString(Text, Font, textBrush, textX, textY);
            }
        }

        private GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
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

    public enum TransportType
    {
        Taxi,
        Bus,
        Tram,
        Walk,
        Transfer
    }

    /// <summary>
    /// Info panel - rota bilgisi göstergesi.
    /// </summary>
    public class RouteInfoPanel : Panel
    {
        private string _fromLocation = "";
        private string _toLocation = "";
        private string _duration = "";
        private string _cost = "";
        private string _distance = "";

        public RouteInfoPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);

            BackColor = ProfessionalTheme.Surface;
            Height = 80;
        }

        public void SetRouteInfo(string from, string to, string duration, string cost, string distance)
        {
            _fromLocation = from;
            _toLocation = to;
            _duration = duration;
            _cost = cost;
            _distance = distance;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var padding = 16;
            var y = padding;

            // From -> To
            using (var brush = new SolidBrush(ProfessionalTheme.TextPrimary))
            using (var iconBrush = new SolidBrush(ProfessionalTheme.Primary))
            {
                e.Graphics.DrawString("📍", ProfessionalTheme.FontBody, iconBrush, padding, y);
                e.Graphics.DrawString(_fromLocation, ProfessionalTheme.FontBodyBold, brush, padding + 24, y);

                e.Graphics.DrawString("→", ProfessionalTheme.FontBody, brush, padding + 150, y);

                e.Graphics.DrawString("🎯", ProfessionalTheme.FontBody, iconBrush, padding + 180, y);
                e.Graphics.DrawString(_toLocation, ProfessionalTheme.FontBodyBold, brush, padding + 204, y);
            }

            y += 30;

            // Stats
            using (var mutedBrush = new SolidBrush(ProfessionalTheme.TextSecondary))
            {
                e.Graphics.DrawString($"⏱️ {_duration}", ProfessionalTheme.FontCaption, mutedBrush, padding, y);
                e.Graphics.DrawString($"💰 {_cost}", ProfessionalTheme.FontCaption, mutedBrush, padding + 120, y);
                e.Graphics.DrawString($"📏 {_distance}", ProfessionalTheme.FontCaption, mutedBrush, padding + 240, y);
            }
        }
    }

    /// <summary>
    /// Legend panel - harita lejant paneli.
    /// </summary>
    public class MapLegendPanel : Panel
    {
        public MapLegendPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);

            BackColor = Color.FromArgb(240, 255, 255, 255);
            Size = new Size(600, 36);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var items = new[]
            {
                ("🚖 Taksi", ProfessionalTheme.TaxiColor),
                ("🚌 Otobüs", ProfessionalTheme.BusColor),
                ("🚋 Tramvay", ProfessionalTheme.TramColor),
                ("🚶 Yürüyüş", ProfessionalTheme.WalkColor),
                ("🔄 Aktarma", ProfessionalTheme.TransferColor)
            };

            var x = 12;
            var y = 8;
            var itemWidth = 110;

            foreach (var (text, color) in items)
            {
                // Color indicator
                using (var brush = new SolidBrush(color))
                {
                    e.Graphics.FillEllipse(brush, x, y + 2, 14, 14);
                }

                // Text
                using (var textBrush = new SolidBrush(ProfessionalTheme.TextPrimary))
                {
                    e.Graphics.DrawString(text, ProfessionalTheme.FontCaption, textBrush, x + 20, y);
                }

                x += itemWidth;
            }
        }
    }

    /// <summary>
    /// Stat card - istatistik kartı.
    /// </summary>
    public class StatCard : Panel
    {
        private string _title = "Başlık";
        private string _value = "0";
        private string _icon = "📊";
        private Color _accentColor = ProfessionalTheme.Primary;

        public StatCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);

            BackColor = ProfessionalTheme.Surface;
            Size = new Size(150, 80);
        }

        public string Title
        {
            get => _title;
            set { _title = value; Invalidate(); }
        }

        public string Value
        {
            get => _value;
            set { _value = value; Invalidate(); }
        }

        public string Icon
        {
            get => _icon;
            set { _icon = value; Invalidate(); }
        }

        public Color AccentColor
        {
            get => _accentColor;
            set { _accentColor = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // Background
            using (var path = CreateRoundedRectPath(rect, 8))
            using (var brush = new SolidBrush(BackColor))
            using (var pen = new Pen(ProfessionalTheme.Border, 1))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }

            // Accent bar
            using (var accentBrush = new SolidBrush(_accentColor))
            {
                e.Graphics.FillRectangle(accentBrush, 0, 0, 4, Height);
            }

            // Icon
            using (var iconBrush = new SolidBrush(_accentColor))
            {
                e.Graphics.DrawString(_icon, new Font("Segoe UI", 16), iconBrush, 12, 12);
            }

            // Value
            using (var valueBrush = new SolidBrush(ProfessionalTheme.TextPrimary))
            {
                e.Graphics.DrawString(_value, ProfessionalTheme.FontHeading2, valueBrush, 12, 40);
            }

            // Title
            using (var titleBrush = new SolidBrush(ProfessionalTheme.TextSecondary))
            {
                var titleSize = e.Graphics.MeasureString(_title, ProfessionalTheme.FontCaption);
                e.Graphics.DrawString(_title, ProfessionalTheme.FontCaption, titleBrush, Width - titleSize.Width - 12, Height - titleSize.Height - 8);
            }
        }

        private GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
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
}
