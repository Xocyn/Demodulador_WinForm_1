namespace Demodulador_WinForm_1
{
    partial class Demodulador_DSC
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Demodulador_DSC));
            label_banda = new Label();
            combox_hf_vhf = new ComboBox();
            dispositivos = new Label();
            combox_dispositivos = new ComboBox();
            splitContainer1 = new SplitContainer();
            DISPLAYSECUNDARIO = new RichTextBox();
            MAINDISPLAY = new RichTextBox();
            waveViewer1 = new WaveViewerControl();
            dataGridView1 = new DataGridView();
            detener = new Button();
            columna_formato = new DataGridViewTextBoxColumn();
            categoria = new DataGridViewTextBoxColumn();
            columna_hora = new DataGridViewTextBoxColumn();
            column_ecc = new DataGridViewTextBoxColumn();
            columna_rta = new DataGridViewTextBoxColumn();
            see_msg = new DataGridViewButtonColumn();
            rta_msg = new DataGridViewButtonColumn();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // label_banda
            // 
            label_banda.AutoSize = true;
            label_banda.Location = new Point(42, 56);
            label_banda.Name = "label_banda";
            label_banda.Size = new Size(127, 20);
            label_banda.TabIndex = 0;
            label_banda.Text = "Banda a escuchar:";
            // 
            // combox_hf_vhf
            // 
            combox_hf_vhf.FormattingEnabled = true;
            combox_hf_vhf.Items.AddRange(new object[] { "MF/HF", "VHF" });
            combox_hf_vhf.Location = new Point(183, 55);
            combox_hf_vhf.Name = "combox_hf_vhf";
            combox_hf_vhf.Size = new Size(115, 28);
            combox_hf_vhf.TabIndex = 1;
            combox_hf_vhf.SelectedIndexChanged += combox_hf_vhf_SelectedIndexChanged;
            // 
            // dispositivos
            // 
            dispositivos.AutoSize = true;
            dispositivos.Location = new Point(42, 100);
            dispositivos.Name = "dispositivos";
            dispositivos.Size = new Size(174, 20);
            dispositivos.TabIndex = 2;
            dispositivos.Text = "Dispositivos Disponibles:";
            // 
            // combox_dispositivos
            // 
            combox_dispositivos.FormattingEnabled = true;
            combox_dispositivos.Location = new Point(222, 97);
            combox_dispositivos.Name = "combox_dispositivos";
            combox_dispositivos.Size = new Size(245, 28);
            combox_dispositivos.TabIndex = 3;
            combox_dispositivos.SelectedIndexChanged += combox_dispositivos_SelectedIndexChanged;
            // 
            // splitContainer1
            // 
            splitContainer1.Location = new Point(446, 211);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(DISPLAYSECUNDARIO);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(MAINDISPLAY);
            splitContainer1.Size = new Size(984, 414);
            splitContainer1.SplitterDistance = 328;
            splitContainer1.TabIndex = 4;
            // 
            // DISPLAYSECUNDARIO
            // 
            DISPLAYSECUNDARIO.Location = new Point(3, 3);
            DISPLAYSECUNDARIO.Name = "DISPLAYSECUNDARIO";
            DISPLAYSECUNDARIO.Size = new Size(322, 408);
            DISPLAYSECUNDARIO.TabIndex = 0;
            DISPLAYSECUNDARIO.Text = "";
            // 
            // MAINDISPLAY
            // 
            MAINDISPLAY.Location = new Point(-1, 0);
            MAINDISPLAY.Name = "MAINDISPLAY";
            MAINDISPLAY.Size = new Size(649, 411);
            MAINDISPLAY.TabIndex = 0;
            MAINDISPLAY.Text = "";
            // 
            // waveViewer1
            // 
            waveViewer1.BackColor = Color.Black;
            waveViewer1.ForeColor = Color.Lime;
            waveViewer1.Location = new Point(496, 9);
            waveViewer1.Margin = new Padding(0);
            waveViewer1.Name = "waveViewer1";
            waveViewer1.Size = new Size(1419, 157);
            waveViewer1.TabIndex = 5;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AllowUserToResizeColumns = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { columna_formato, categoria, columna_hora, column_ecc, columna_rta, see_msg, rta_msg });
            dataGridView1.Location = new Point(446, 666);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 51;
            dataGridView1.Size = new Size(978, 188);
            dataGridView1.TabIndex = 6;
            dataGridView1.CellContentClick += dataGridView1_CellContentClick;
            // 
            // detener
            // 
            detener.BackColor = Color.Red;
            detener.Location = new Point(1621, 214);
            detener.Name = "detener";
            detener.Size = new Size(153, 99);
            detener.TabIndex = 7;
            detener.Text = "DETENER";
            detener.UseVisualStyleBackColor = false;
            detener.Click += detener_Click;
            // 
            // columna_formato
            // 
            columna_formato.HeaderText = "Formato";
            columna_formato.MinimumWidth = 6;
            columna_formato.Name = "columna_formato";
            columna_formato.ReadOnly = true;
            columna_formato.Width = 125;
            // 
            // categoria
            // 
            categoria.HeaderText = "Categoria";
            categoria.MinimumWidth = 6;
            categoria.Name = "categoria";
            categoria.ReadOnly = true;
            categoria.Width = 125;
            // 
            // columna_hora
            // 
            columna_hora.HeaderText = "Hora";
            columna_hora.MinimumWidth = 6;
            columna_hora.Name = "columna_hora";
            columna_hora.ReadOnly = true;
            columna_hora.Width = 125;
            // 
            // column_ecc
            // 
            column_ecc.HeaderText = "ECC";
            column_ecc.MinimumWidth = 6;
            column_ecc.Name = "column_ecc";
            column_ecc.ReadOnly = true;
            column_ecc.Width = 125;
            // 
            // columna_rta
            // 
            columna_rta.HeaderText = "Responder";
            columna_rta.MinimumWidth = 6;
            columna_rta.Name = "columna_rta";
            columna_rta.ReadOnly = true;
            columna_rta.Width = 125;
            // 
            // see_msg
            // 
            see_msg.HeaderText = "Ver";
            see_msg.MinimumWidth = 6;
            see_msg.Name = "see_msg";
            see_msg.ReadOnly = true;
            see_msg.Width = 125;
            // 
            // rta_msg
            // 
            rta_msg.HeaderText = "Respuesta";
            rta_msg.MinimumWidth = 6;
            rta_msg.Name = "rta_msg";
            rta_msg.ReadOnly = true;
            rta_msg.Width = 125;
            // 
            // Demodulador_DSC
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1924, 990);
            Controls.Add(detener);
            Controls.Add(dataGridView1);
            Controls.Add(waveViewer1);
            Controls.Add(splitContainer1);
            Controls.Add(combox_dispositivos);
            Controls.Add(dispositivos);
            Controls.Add(combox_hf_vhf);
            Controls.Add(label_banda);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimizeBox = false;
            Name = "Demodulador_DSC";
            Text = "Demodulador DSC";
            WindowState = FormWindowState.Maximized;
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label_banda;
        public ComboBox combox_hf_vhf;
        private Label dispositivos;
        public ComboBox combox_dispositivos;
        private SplitContainer splitContainer1;
        public RichTextBox DISPLAYSECUNDARIO;
        public RichTextBox MAINDISPLAY;
        public WaveViewerControl waveViewer1;
        private DataGridView dataGridView1;
        private Button detener;
        private DataGridViewTextBoxColumn columna_formato;
        private DataGridViewTextBoxColumn categoria;
        private DataGridViewTextBoxColumn columna_hora;
        private DataGridViewTextBoxColumn column_ecc;
        private DataGridViewTextBoxColumn columna_rta;
        private DataGridViewButtonColumn see_msg;
        private DataGridViewButtonColumn rta_msg;
    }
}
