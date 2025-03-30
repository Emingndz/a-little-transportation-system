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
            label1.Location = new Point(47, 302);
            label1.Name = "label1";
            label1.Size = new Size(139, 20);
            label1.TabIndex = 4;
            label1.Text = "Kartınızın Durumu : ";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(45, 214);
            label2.Name = "label2";
            label2.Size = new Size(249, 40);
            label2.TabIndex = 5;
            label2.Text = "Nereye gitmek istiyorsunuz\r\n(Harita üzerinden de seçebilirsiniz) : ";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(45, 123);
            label3.Name = "label3";
            label3.Size = new Size(249, 40);
            label3.TabIndex = 6;
            label3.Text = "Konumunuz \r\n(Harita üzerinden de seçebilirsiniz) : ";
            // 
            // cmbKartDurumu
            // 
            cmbKartDurumu.FormattingEnabled = true;
            cmbKartDurumu.Location = new Point(201, 302);
            cmbKartDurumu.Name = "cmbKartDurumu";
            cmbKartDurumu.Size = new Size(151, 28);
            cmbKartDurumu.TabIndex = 7;
            // 
            // cmbHedef
            // 
            cmbHedef.FormattingEnabled = true;
            cmbHedef.Location = new Point(300, 222);
            cmbHedef.Name = "cmbHedef";
            cmbHedef.Size = new Size(151, 28);
            cmbHedef.TabIndex = 8;
            // 
            // cmbBaslangic
            // 
            cmbBaslangic.FormattingEnabled = true;
            cmbBaslangic.Location = new Point(299, 127);
            cmbBaslangic.Name = "cmbBaslangic";
            cmbBaslangic.Size = new Size(151, 28);
            cmbBaslangic.TabIndex = 9;
            // 
            // button1
            // 
            button1.Location = new Point(234, 399);
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
            label4.Location = new Point(119, 34);
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
            btnKonumSifirla.Location = new Point(45, 397);
            btnKonumSifirla.Name = "btnKonumSifirla";
            btnKonumSifirla.Size = new Size(132, 62);
            btnKonumSifirla.TabIndex = 13;
            btnKonumSifirla.Text = "Haritayı sıfırla";
            btnKonumSifirla.UseVisualStyleBackColor = true;
            btnKonumSifirla.Click += btnKonumSifirla_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(255, 192, 128);
            ClientSize = new Size(1291, 797);
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
            Text = "Form1";
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
    }
}
