namespace Prolab_4
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            
            // Ana kontroller
            pnlSidebar = new Panel();
            pnlContent = new Panel();
            pnlHeader = new Panel();
            pnlMap = new Panel();
            pnlLegend = new Panel();
            pnlRouteInfo = new Panel();
            
            // Header kontrolleri
            lblTitle = new Label();
            lblSubtitle = new Label();
            
            // Sidebar kontrolleri
            pnlLocationCard = new Panel();
            pnlPaymentCard = new Panel();
            pnlActionsCard = new Panel();
            
            // Konum kontrolleri
            lblLocationTitle = new Label();
            lblBaslangicLabel = new Label();
            lblHedefLabel = new Label();
            lblKartLabel = new Label();
            cmbBaslangic = new ComboBox();
            cmbHedef = new ComboBox();
            cmbKartDurumu = new ComboBox();
            
            // Ödeme kontrolleri
            lblPaymentTitle = new Label();
            pnlPaymentOptions = new Panel();
            nakitrbutton = new RadioButton();
            kentkartrbutton = new RadioButton();
            kredikartirbutton = new RadioButton();
            
            // Action butonları
            btnRotaOlustur = new Button();
            btnHaritaSifirla = new Button();
            
            // Legend kontrolleri
            lblTaksi = new Label();
            lblOtobus = new Label();
            lblTramvay = new Label();
            lblYurume = new Label();
            lblAktarma = new Label();
            
            // Harita
            gMapControl1 = new GMap.NET.WindowsForms.GMapControl();
            
            // DataGridView
            dataGridView1 = new DataGridView();
            
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            pnlSidebar.SuspendLayout();
            pnlContent.SuspendLayout();
            pnlHeader.SuspendLayout();
            pnlMap.SuspendLayout();
            pnlLegend.SuspendLayout();
            pnlLocationCard.SuspendLayout();
            pnlPaymentCard.SuspendLayout();
            pnlActionsCard.SuspendLayout();
            SuspendLayout();
            
            // ============================================
            // FORM ANA AYARLARI
            // ============================================
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(249, 250, 251);
            ClientSize = new Size(1400, 850);
            Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "🚌 İzmit Ulaşım Sistemi";
            
            // ============================================
            // HEADER PANEL
            // ============================================
            pnlHeader.BackColor = Color.FromArgb(37, 99, 235);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 80;
            pnlHeader.Padding = new Padding(24, 0, 24, 0);
            
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(24, 14);
            lblTitle.Text = "🚌 İzmit Ulaşım Sistemi";
            
            lblSubtitle.AutoSize = true;
            lblSubtitle.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            lblSubtitle.ForeColor = Color.FromArgb(220, 255, 255, 255);
            lblSubtitle.Location = new Point(26, 52);
            lblSubtitle.Text = "En uygun rotanızı hesaplayın • Otobüs • Tramvay • Taksi • Yürüyüş";
            
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            
            // ============================================
            // SIDEBAR PANEL (Sol Panel)
            // ============================================
            pnlSidebar.BackColor = Color.FromArgb(249, 250, 251);
            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Width = 380;
            pnlSidebar.Padding = new Padding(16);
            
            // ============================================
            // KONUM KARTI
            // ============================================
            pnlLocationCard.BackColor = Color.White;
            pnlLocationCard.Location = new Point(16, 16);
            pnlLocationCard.Size = new Size(348, 260);
            pnlLocationCard.Padding = new Padding(20);
            
            lblLocationTitle.AutoSize = true;
            lblLocationTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblLocationTitle.ForeColor = Color.FromArgb(17, 24, 39);
            lblLocationTitle.Location = new Point(20, 16);
            lblLocationTitle.Text = "📍 Rota Bilgileri";
            
            lblBaslangicLabel.AutoSize = true;
            lblBaslangicLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            lblBaslangicLabel.ForeColor = Color.FromArgb(107, 114, 128);
            lblBaslangicLabel.Location = new Point(20, 52);
            lblBaslangicLabel.Text = "Nereden? (Başlangıç noktası)";
            
            cmbBaslangic.BackColor = Color.White;
            cmbBaslangic.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbBaslangic.FlatStyle = FlatStyle.Flat;
            cmbBaslangic.Font = new Font("Segoe UI", 10F);
            cmbBaslangic.ForeColor = Color.FromArgb(17, 24, 39);
            cmbBaslangic.Location = new Point(20, 74);
            cmbBaslangic.Size = new Size(308, 32);
            
            lblHedefLabel.AutoSize = true;
            lblHedefLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            lblHedefLabel.ForeColor = Color.FromArgb(107, 114, 128);
            lblHedefLabel.Location = new Point(20, 116);
            lblHedefLabel.Text = "Nereye? (Hedef noktası)";
            
            cmbHedef.BackColor = Color.White;
            cmbHedef.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbHedef.FlatStyle = FlatStyle.Flat;
            cmbHedef.Font = new Font("Segoe UI", 10F);
            cmbHedef.ForeColor = Color.FromArgb(17, 24, 39);
            cmbHedef.Location = new Point(20, 138);
            cmbHedef.Size = new Size(308, 32);
            
            lblKartLabel.AutoSize = true;
            lblKartLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            lblKartLabel.ForeColor = Color.FromArgb(107, 114, 128);
            lblKartLabel.Location = new Point(20, 180);
            lblKartLabel.Text = "Yolcu Tipi";
            
            cmbKartDurumu.BackColor = Color.White;
            cmbKartDurumu.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbKartDurumu.FlatStyle = FlatStyle.Flat;
            cmbKartDurumu.Font = new Font("Segoe UI", 10F);
            cmbKartDurumu.ForeColor = Color.FromArgb(17, 24, 39);
            cmbKartDurumu.Items.AddRange(new object[] { "👤 Genel", "🎓 Öğrenci", "👴 65+" });
            cmbKartDurumu.Location = new Point(20, 202);
            cmbKartDurumu.Size = new Size(308, 32);
            
            pnlLocationCard.Controls.Add(lblLocationTitle);
            pnlLocationCard.Controls.Add(lblBaslangicLabel);
            pnlLocationCard.Controls.Add(cmbBaslangic);
            pnlLocationCard.Controls.Add(lblHedefLabel);
            pnlLocationCard.Controls.Add(cmbHedef);
            pnlLocationCard.Controls.Add(lblKartLabel);
            pnlLocationCard.Controls.Add(cmbKartDurumu);
            
            // ============================================
            // ÖDEME KARTI
            // ============================================
            pnlPaymentCard.BackColor = Color.White;
            pnlPaymentCard.Location = new Point(16, 292);
            pnlPaymentCard.Size = new Size(348, 160);
            pnlPaymentCard.Padding = new Padding(20);
            
            lblPaymentTitle.AutoSize = true;
            lblPaymentTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblPaymentTitle.ForeColor = Color.FromArgb(17, 24, 39);
            lblPaymentTitle.Location = new Point(20, 16);
            lblPaymentTitle.Text = "💳 Ödeme Yöntemi";
            
            pnlPaymentOptions.Location = new Point(20, 48);
            pnlPaymentOptions.Size = new Size(308, 100);
            pnlPaymentOptions.BackColor = Color.Transparent;
            
            nakitrbutton.AutoSize = true;
            nakitrbutton.Font = new Font("Segoe UI", 10F);
            nakitrbutton.ForeColor = Color.FromArgb(17, 24, 39);
            nakitrbutton.Location = new Point(0, 0);
            nakitrbutton.Text = "💵 Nakit";
            nakitrbutton.Cursor = Cursors.Hand;
            
            kentkartrbutton.AutoSize = true;
            kentkartrbutton.Font = new Font("Segoe UI", 10F);
            kentkartrbutton.ForeColor = Color.FromArgb(17, 24, 39);
            kentkartrbutton.Location = new Point(0, 32);
            kentkartrbutton.Text = "🎫 Kent Kart";
            kentkartrbutton.Checked = true;
            kentkartrbutton.Cursor = Cursors.Hand;
            
            kredikartirbutton.AutoSize = true;
            kredikartirbutton.Font = new Font("Segoe UI", 10F);
            kredikartirbutton.ForeColor = Color.FromArgb(17, 24, 39);
            kredikartirbutton.Location = new Point(0, 64);
            kredikartirbutton.Text = "💳 Kredi Kartı";
            kredikartirbutton.Cursor = Cursors.Hand;
            
            pnlPaymentOptions.Controls.Add(nakitrbutton);
            pnlPaymentOptions.Controls.Add(kentkartrbutton);
            pnlPaymentOptions.Controls.Add(kredikartirbutton);
            
            pnlPaymentCard.Controls.Add(lblPaymentTitle);
            pnlPaymentCard.Controls.Add(pnlPaymentOptions);
            
            // ============================================
            // BUTONLAR KARTI
            // ============================================
            pnlActionsCard.BackColor = Color.Transparent;
            pnlActionsCard.Location = new Point(16, 468);
            pnlActionsCard.Size = new Size(348, 120);
            
            btnRotaOlustur.BackColor = Color.FromArgb(34, 197, 94);
            btnRotaOlustur.Cursor = Cursors.Hand;
            btnRotaOlustur.FlatAppearance.BorderSize = 0;
            btnRotaOlustur.FlatStyle = FlatStyle.Flat;
            btnRotaOlustur.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnRotaOlustur.ForeColor = Color.White;
            btnRotaOlustur.Location = new Point(0, 0);
            btnRotaOlustur.Size = new Size(348, 54);
            btnRotaOlustur.Text = "🔍 Rota Hesapla";
            btnRotaOlustur.Click += button1_Click;
            
            btnHaritaSifirla.BackColor = Color.White;
            btnHaritaSifirla.Cursor = Cursors.Hand;
            btnHaritaSifirla.FlatAppearance.BorderColor = Color.FromArgb(229, 231, 235);
            btnHaritaSifirla.FlatAppearance.BorderSize = 1;
            btnHaritaSifirla.FlatStyle = FlatStyle.Flat;
            btnHaritaSifirla.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            btnHaritaSifirla.ForeColor = Color.FromArgb(107, 114, 128);
            btnHaritaSifirla.Location = new Point(0, 60);
            btnHaritaSifirla.Size = new Size(348, 44);
            btnHaritaSifirla.Text = "🔄 Haritayı Sıfırla";
            btnHaritaSifirla.Click += btnKonumSifirla_Click;
            
            pnlActionsCard.Controls.Add(btnRotaOlustur);
            pnlActionsCard.Controls.Add(btnHaritaSifirla);
            
            // Sidebar'a kartları ekle
            pnlSidebar.Controls.Add(pnlLocationCard);
            pnlSidebar.Controls.Add(pnlPaymentCard);
            pnlSidebar.Controls.Add(pnlActionsCard);
            
            // ============================================
            // CONTENT PANEL (Sağ taraf)
            // ============================================
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.Padding = new Padding(16, 16, 16, 16);
            pnlContent.BackColor = Color.FromArgb(249, 250, 251);
            
            // ============================================
            // LEGEND PANEL (Harita üstü lejant)
            // ============================================
            pnlLegend.BackColor = Color.FromArgb(250, 255, 255, 255);
            pnlLegend.Location = new Point(16, 16);
            pnlLegend.Size = new Size(700, 36);
            pnlLegend.Padding = new Padding(12, 8, 12, 8);
            
            lblTaksi.AutoSize = true;
            lblTaksi.Font = new Font("Segoe UI", 9F);
            lblTaksi.ForeColor = Color.FromArgb(251, 191, 36);
            lblTaksi.Location = new Point(12, 8);
            lblTaksi.Text = "● Taksi";
            
            lblOtobus.AutoSize = true;
            lblOtobus.Font = new Font("Segoe UI", 9F);
            lblOtobus.ForeColor = Color.FromArgb(59, 130, 246);
            lblOtobus.Location = new Point(90, 8);
            lblOtobus.Text = "● Otobüs";
            
            lblTramvay.AutoSize = true;
            lblTramvay.Font = new Font("Segoe UI", 9F);
            lblTramvay.ForeColor = Color.FromArgb(16, 185, 129);
            lblTramvay.Location = new Point(180, 8);
            lblTramvay.Text = "● Tramvay";
            
            lblYurume.AutoSize = true;
            lblYurume.Font = new Font("Segoe UI", 9F);
            lblYurume.ForeColor = Color.FromArgb(139, 92, 246);
            lblYurume.Location = new Point(280, 8);
            lblYurume.Text = "● Yürüyüş";
            
            lblAktarma.AutoSize = true;
            lblAktarma.Font = new Font("Segoe UI", 9F);
            lblAktarma.ForeColor = Color.FromArgb(236, 72, 153);
            lblAktarma.Location = new Point(375, 8);
            lblAktarma.Text = "● Aktarma";
            
            pnlLegend.Controls.Add(lblTaksi);
            pnlLegend.Controls.Add(lblOtobus);
            pnlLegend.Controls.Add(lblTramvay);
            pnlLegend.Controls.Add(lblYurume);
            pnlLegend.Controls.Add(lblAktarma);
            
            // ============================================
            // MAP PANEL
            // ============================================
            pnlMap.BackColor = Color.White;
            pnlMap.Location = new Point(16, 16);
            pnlMap.Size = new Size(968, 450);
            pnlMap.Padding = new Padding(1);
            
            gMapControl1.Bearing = 0F;
            gMapControl1.CanDragMap = true;
            gMapControl1.Dock = DockStyle.Fill;
            gMapControl1.EmptyTileColor = Color.FromArgb(240, 240, 240);
            gMapControl1.GrayScaleMode = false;
            gMapControl1.HelperLineOption = GMap.NET.WindowsForms.HelperLineOptions.DontShow;
            gMapControl1.LevelsKeepInMemory = 5;
            gMapControl1.MarkersEnabled = true;
            gMapControl1.MaxZoom = 20;
            gMapControl1.MinZoom = 2;
            gMapControl1.MouseWheelZoomEnabled = true;
            gMapControl1.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.ViewCenter;
            gMapControl1.NegativeMode = false;
            gMapControl1.PolygonsEnabled = true;
            gMapControl1.RetryLoadTile = 0;
            gMapControl1.RoutesEnabled = true;
            gMapControl1.ScaleMode = GMap.NET.WindowsForms.ScaleModes.Integer;
            gMapControl1.SelectedAreaFillColor = Color.FromArgb(33, 0, 122, 204);
            gMapControl1.ShowTileGridLines = false;
            gMapControl1.Zoom = 13D;
            
            pnlMap.Controls.Add(gMapControl1);
            pnlMap.Controls.Add(pnlLegend);
            pnlLegend.BringToFront();
            
            // ============================================
            // ROUTE INFO PANEL
            // ============================================
            pnlRouteInfo.BackColor = Color.White;
            pnlRouteInfo.Location = new Point(16, 482);
            pnlRouteInfo.Size = new Size(968, 60);
            pnlRouteInfo.Visible = false;
            
            // ============================================
            // DATAGRIDVIEW (Alt kısım)
            // ============================================
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.BackgroundColor = Color.White;
            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridView1.ColumnHeadersHeight = 48;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.GridColor = Color.FromArgb(243, 244, 246);
            dataGridView1.Location = new Point(16, 482);
            dataGridView1.MultiSelect = false;
            dataGridView1.ReadOnly = true;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.RowTemplate.Height = 44;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.Size = new Size(968, 260);
            dataGridView1.CellClick += dataGridView1_CellClick;
            
            // DataGridView stilleri
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(37, 99, 235);
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            dataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridView1.ColumnHeadersDefaultCellStyle.Padding = new Padding(16, 0, 0, 0);
            dataGridView1.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(37, 99, 235);
            
            dataGridView1.DefaultCellStyle.BackColor = Color.White;
            dataGridView1.DefaultCellStyle.ForeColor = Color.FromArgb(17, 24, 39);
            dataGridView1.DefaultCellStyle.Font = new Font("Segoe UI", 10F);
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(30, 0, 122, 204);
            dataGridView1.DefaultCellStyle.SelectionForeColor = Color.FromArgb(17, 24, 39);
            dataGridView1.DefaultCellStyle.Padding = new Padding(12, 0, 0, 0);
            
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
            
            // Content'e kontrolleri ekle
            pnlContent.Controls.Add(pnlMap);
            pnlContent.Controls.Add(dataGridView1);
            
            // Form'a panelleri ekle
            Controls.Add(pnlContent);
            Controls.Add(pnlSidebar);
            Controls.Add(pnlHeader);
            
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            pnlSidebar.ResumeLayout(false);
            pnlContent.ResumeLayout(false);
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            pnlMap.ResumeLayout(false);
            pnlLegend.ResumeLayout(false);
            pnlLegend.PerformLayout();
            pnlLocationCard.ResumeLayout(false);
            pnlLocationCard.PerformLayout();
            pnlPaymentCard.ResumeLayout(false);
            pnlPaymentCard.PerformLayout();
            pnlActionsCard.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        // Panels
        private Panel pnlSidebar;
        private Panel pnlContent;
        private Panel pnlHeader;
        private Panel pnlMap;
        private Panel pnlLegend;
        private Panel pnlRouteInfo;
        private Panel pnlLocationCard;
        private Panel pnlPaymentCard;
        private Panel pnlActionsCard;
        private Panel pnlPaymentOptions;

        // Header
        private Label lblTitle;
        private Label lblSubtitle;

        // Location Card
        private Label lblLocationTitle;
        private Label lblBaslangicLabel;
        private Label lblHedefLabel;
        private Label lblKartLabel;
        private ComboBox cmbBaslangic;
        private ComboBox cmbHedef;
        private ComboBox cmbKartDurumu;

        // Payment Card
        private Label lblPaymentTitle;
        private RadioButton nakitrbutton;
        private RadioButton kentkartrbutton;
        private RadioButton kredikartirbutton;

        // Action Buttons
        private Button btnRotaOlustur;
        private Button btnHaritaSifirla;

        // Legend
        private Label lblTaksi;
        private Label lblOtobus;
        private Label lblTramvay;
        private Label lblYurume;
        private Label lblAktarma;

        // Map
        private GMap.NET.WindowsForms.GMapControl gMapControl1;

        // DataGridView
        private DataGridView dataGridView1;
    }
}
