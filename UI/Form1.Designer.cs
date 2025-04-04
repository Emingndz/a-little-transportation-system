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
            gMapControl1 = new GMap.NET.WindowsForms.GMapControl();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            cmbKartDurumu = new ComboBox();
            cmbHedef = new ComboBox();
            cmbBaslangic = new ComboBox();
            button1 = new Button();
            label4 = new Label();
            dataGridView1 = new DataGridView();
            btnKonumSifirla = new Button();
            labelodeme = new Label();
            nakitrbutton = new RadioButton();
            kentkartrbutton = new RadioButton();
            kredikartırbutton = new RadioButton();
            lblYurume = new Label();
            lblTramvay = new Label();
            lblOtobus = new Label();
            lblTaksi = new Label();
            lblAktarma = new Label();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // gMapControl1
            // 
            gMapControl1.Bearing = 0F;
            gMapControl1.CanDragMap = true;
            gMapControl1.EmptyTileColor = Color.Navy;
            gMapControl1.GrayScaleMode = false;
            gMapControl1.HelperLineOption = GMap.NET.WindowsForms.HelperLineOptions.DontShow;
            gMapControl1.LevelsKeepInMemory = 5;
            gMapControl1.Location = new Point(492, 12);
            gMapControl1.MarkersEnabled = true;
            gMapControl1.MaxZoom = 2;
            gMapControl1.MinZoom = 2;
            gMapControl1.MouseWheelZoomEnabled = true;
            gMapControl1.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionAndCenter;
            gMapControl1.Name = "gMapControl1";
            gMapControl1.NegativeMode = false;
            gMapControl1.PolygonsEnabled = true;
            gMapControl1.RetryLoadTile = 0;
            gMapControl1.RoutesEnabled = true;
            gMapControl1.ScaleMode = GMap.NET.WindowsForms.ScaleModes.Integer;
            gMapControl1.SelectedAreaFillColor = Color.FromArgb(33, 65, 105, 225);
            gMapControl1.ShowTileGridLines = false;
            gMapControl1.Size = new Size(776, 479);
            gMapControl1.TabIndex = 0;
            gMapControl1.Zoom = 2D;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(26, 258);
            label1.Name = "label1";
            label1.Size = new Size(139, 20);
            label1.TabIndex = 4;
            label1.Text = "Kartınızın Durumu : ";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(26, 180);
            label2.Name = "label2";
            label2.Size = new Size(249, 40);
            label2.TabIndex = 5;
            label2.Text = "Nereye gitmek istiyorsunuz\r\n(Harita üzerinden de seçebilirsiniz) : ";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(26, 99);
            label3.Name = "label3";
            label3.Size = new Size(249, 40);
            label3.TabIndex = 6;
            label3.Text = "Konumunuz \r\n(Harita üzerinden de seçebilirsiniz) : ";
            // 
            // cmbKartDurumu
            // 
            cmbKartDurumu.FormattingEnabled = true;
            cmbKartDurumu.Items.AddRange(new object[] { "Genel", "Öğrenci", "65+" });
            cmbKartDurumu.Location = new Point(202, 255);
            cmbKartDurumu.Name = "cmbKartDurumu";
            cmbKartDurumu.Size = new Size(151, 28);
            cmbKartDurumu.TabIndex = 7;
            // 
            // cmbHedef
            // 
            cmbHedef.FormattingEnabled = true;
            cmbHedef.Location = new Point(300, 192);
            cmbHedef.Name = "cmbHedef";
            cmbHedef.Size = new Size(151, 28);
            cmbHedef.TabIndex = 8;
            // 
            // cmbBaslangic
            // 
            cmbBaslangic.FormattingEnabled = true;
            cmbBaslangic.Location = new Point(300, 111);
            cmbBaslangic.Name = "cmbBaslangic";
            cmbBaslangic.Size = new Size(151, 28);
            cmbBaslangic.TabIndex = 9;
            // 
            // button1
            // 
            button1.Location = new Point(247, 418);
            button1.Name = "button1";
            button1.Size = new Size(196, 60);
            button1.TabIndex = 10;
            button1.Text = "Rota Oluştur";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 20F);
            label4.Location = new Point(122, 12);
            label4.Name = "label4";
            label4.Size = new Size(194, 46);
            label4.TabIndex = 11;
            label4.Text = "Hoşgeldiniz";
            // 
            // dataGridView1
            // 
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(-6, 513);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 51;
            dataGridView1.Size = new Size(1299, 292);
            dataGridView1.TabIndex = 12;
            dataGridView1.CellClick += dataGridView1_CellClick;
            // 
            // btnKonumSifirla
            // 
            btnKonumSifirla.Location = new Point(45, 418);
            btnKonumSifirla.Name = "btnKonumSifirla";
            btnKonumSifirla.Size = new Size(132, 62);
            btnKonumSifirla.TabIndex = 13;
            btnKonumSifirla.Text = "Haritayı sıfırla";
            btnKonumSifirla.UseVisualStyleBackColor = true;
            btnKonumSifirla.Click += btnKonumSifirla_Click;
            // 
            // labelodeme
            // 
            labelodeme.AutoSize = true;
            labelodeme.Location = new Point(26, 335);
            labelodeme.Name = "labelodeme";
            labelodeme.Size = new Size(189, 20);
            labelodeme.TabIndex = 14;
            labelodeme.Text = "Ödeme Yöntemini Seçiniz : ";
            // 
            // nakitrbutton
            // 
            nakitrbutton.AutoSize = true;
            nakitrbutton.Location = new Point(247, 305);
            nakitrbutton.Name = "nakitrbutton";
            nakitrbutton.Size = new Size(65, 24);
            nakitrbutton.TabIndex = 15;
            nakitrbutton.TabStop = true;
            nakitrbutton.Text = "Nakit";
            nakitrbutton.UseVisualStyleBackColor = true;
            // 
            // kentkartrbutton
            // 
            kentkartrbutton.AutoSize = true;
            kentkartrbutton.Location = new Point(247, 335);
            kentkartrbutton.Name = "kentkartrbutton";
            kentkartrbutton.Size = new Size(91, 24);
            kentkartrbutton.TabIndex = 16;
            kentkartrbutton.TabStop = true;
            kentkartrbutton.Text = "Kent Kart";
            kentkartrbutton.UseVisualStyleBackColor = true;
            // 
            // kredikartırbutton
            // 
            kredikartırbutton.AutoSize = true;
            kredikartırbutton.Location = new Point(247, 365);
            kredikartırbutton.Name = "kredikartırbutton";
            kredikartırbutton.Size = new Size(100, 24);
            kredikartırbutton.TabIndex = 17;
            kredikartırbutton.TabStop = true;
            kredikartırbutton.Text = "Kredi Kartı";
            kredikartırbutton.UseVisualStyleBackColor = true;
            // 
            // lblYurume
            // 
            lblYurume.AutoSize = true;
            lblYurume.BackColor = Color.White;
            lblYurume.Location = new Point(947, 32);
            lblYurume.Name = "lblYurume";
            lblYurume.Size = new Size(165, 20);
            lblYurume.TabIndex = 3;
            lblYurume.Text = "🚶 Yürüyüş: Kahverengi";
            // 
            // lblTramvay
            // 
            lblTramvay.AutoSize = true;
            lblTramvay.BackColor = Color.White;
            lblTramvay.Location = new Point(795, 32);
            lblTramvay.Name = "lblTramvay";
            lblTramvay.Size = new Size(125, 20);
            lblTramvay.TabIndex = 2;
            lblTramvay.Text = "🚋 Tramvay: Yeşil";
            // 
            // lblOtobus
            // 
            lblOtobus.AutoSize = true;
            lblOtobus.BackColor = Color.White;
            lblOtobus.Location = new Point(654, 32);
            lblOtobus.Name = "lblOtobus";
            lblOtobus.Size = new Size(125, 20);
            lblOtobus.TabIndex = 1;
            lblOtobus.Text = "🚌 Otobüs : Mavi";
            // 
            // lblTaksi
            // 
            lblTaksi.AutoSize = true;
            lblTaksi.BackColor = Color.White;
            lblTaksi.Location = new Point(504, 32);
            lblTaksi.Name = "lblTaksi";
            lblTaksi.Size = new Size(128, 20);
            lblTaksi.TabIndex = 0;
            lblTaksi.Text = "🚖 Taksi : Turuncu";
            // 
            // lblAktarma
            // 
            lblAktarma.AutoSize = true;
            lblAktarma.BackColor = Color.White;
            lblAktarma.Location = new Point(1133, 32);
            lblAktarma.Name = "lblAktarma";
            lblAktarma.Size = new Size(103, 20);
            lblAktarma.TabIndex = 18;
            lblAktarma.Text = "Aktarma : Mor";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(255, 192, 128);
            ClientSize = new Size(1291, 769);
            Controls.Add(lblAktarma);
            Controls.Add(lblYurume);
            Controls.Add(lblTramvay);
            Controls.Add(kredikartırbutton);
            Controls.Add(lblOtobus);
            Controls.Add(kentkartrbutton);
            Controls.Add(lblTaksi);
            Controls.Add(nakitrbutton);
            Controls.Add(labelodeme);
            Controls.Add(btnKonumSifirla);
            Controls.Add(dataGridView1);
            Controls.Add(label4);
            Controls.Add(button1);
            Controls.Add(cmbBaslangic);
            Controls.Add(cmbHedef);
            Controls.Add(cmbKartDurumu);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(gMapControl1);
            Name = "Form1";
            Text = "Navigation System";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private GMap.NET.WindowsForms.GMapControl gMapControl1;
        private Label label1;
        private Label label2;
        private Label label3;
        private ComboBox cmbKartDurumu;
        private ComboBox cmbHedef;
        private ComboBox cmbBaslangic;
        private Button button1;
        private Label label4;
        private DataGridView dataGridView1;
        private Button btnKonumSifirla;
        private Label labelodeme;
        private RadioButton nakitrbutton;
        private RadioButton kentkartrbutton;
        private RadioButton kredikartırbutton;
        private Label lblYurume;
        private Label lblTramvay;
        private Label lblOtobus;
        private Label lblTaksi;
        private Label lblAktarma;
    }
}
