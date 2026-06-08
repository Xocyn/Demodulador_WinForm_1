namespace Demodulador_WinForm_1.Ventana_rtas
{
    partial class envios_rtas
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            formato_selec = new ListBox();
            label_form = new Label();
            label_MMSI_rx = new Label();
            MMSI_rx = new TextBox();
            label_categoria = new Label();
            combox_categoria = new ComboBox();
            combox_tipo_msg_ind = new ComboBox();
            label_tipo_mensaje_ind = new Label();
            text_canal = new TextBox();
            label_canal_vhf = new Label();
            box_ind = new GroupBox();
            label_motivo = new Label();
            dataGridView1 = new DataGridView();
            combox_motivo = new ComboBox();
            boton_enviar_ind = new Button();
            box_all = new GroupBox();
            dataGridView2 = new DataGridView();
            text_canal_all = new TextBox();
            label_canal_all = new Label();
            label_cat_all = new Label();
            combox_cat_all = new ComboBox();
            box_grupos = new GroupBox();
            dataGridView3 = new DataGridView();
            text_canal_group = new TextBox();
            label_group_vhf = new Label();
            label_sig_com_g = new Label();
            combox_sig_com = new ComboBox();
            box_ind.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            box_all.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView2).BeginInit();
            box_grupos.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView3).BeginInit();
            SuspendLayout();
            // 
            // formato_selec
            // 
            formato_selec.FormattingEnabled = true;
            formato_selec.Items.AddRange(new object[] { "ALL SHIPS", "INDIVIDUAL", "GRUPOS", "GEOGRAFICA" });
            formato_selec.Location = new Point(267, 12);
            formato_selec.Name = "formato_selec";
            formato_selec.Size = new Size(99, 84);
            formato_selec.TabIndex = 0;
            formato_selec.SelectedIndexChanged += formato_selec_SelectedIndexChanged;
            // 
            // label_form
            // 
            label_form.AutoSize = true;
            label_form.Location = new Point(27, 40);
            label_form.Name = "label_form";
            label_form.Size = new Size(222, 20);
            label_form.TabIndex = 1;
            label_form.Text = "Seleccione un formato de envío:";
            // 
            // label_MMSI_rx
            // 
            label_MMSI_rx.AutoSize = true;
            label_MMSI_rx.Location = new Point(402, 40);
            label_MMSI_rx.Name = "label_MMSI_rx";
            label_MMSI_rx.Size = new Size(110, 20);
            label_MMSI_rx.TabIndex = 2;
            label_MMSI_rx.Text = "MMSI receptor:";
            // 
            // MMSI_rx
            // 
            MMSI_rx.Location = new Point(402, 69);
            MMSI_rx.Name = "MMSI_rx";
            MMSI_rx.Size = new Size(223, 27);
            MMSI_rx.TabIndex = 3;
            MMSI_rx.TextChanged += MMSI_rx_TextChanged;
            MMSI_rx.KeyPress += MMSI_rx_KeyPress_1;
            // 
            // label_categoria
            // 
            label_categoria.AutoSize = true;
            label_categoria.Location = new Point(6, 35);
            label_categoria.Name = "label_categoria";
            label_categoria.Size = new Size(180, 20);
            label_categoria.TabIndex = 4;
            label_categoria.Text = "Seleccione una Categoría:";
            // 
            // combox_categoria
            // 
            combox_categoria.FormattingEnabled = true;
            combox_categoria.Items.AddRange(new object[] { "RUTINA", "SEGURIDAD", "URGENCIA" });
            combox_categoria.Location = new Point(192, 32);
            combox_categoria.Name = "combox_categoria";
            combox_categoria.Size = new Size(153, 28);
            combox_categoria.TabIndex = 5;
            combox_categoria.SelectedIndexChanged += combox_categoria_SelectedIndexChanged;
            // 
            // combox_tipo_msg_ind
            // 
            combox_tipo_msg_ind.FormattingEnabled = true;
            combox_tipo_msg_ind.Items.AddRange(new object[] { "RT TODOS LOS MODOS", "ACUSE RT", "IMPOSIBLE DAR ACUSE", "SOLICITUD DE POSICIÓN", "PRUEBA", "ACUSE PRUEBA", "DATOS", "ACUSE DATOS", "INT. SECUENCIAL", "ACUSE INT. SECUENCIAL" });
            combox_tipo_msg_ind.Location = new Point(539, 32);
            combox_tipo_msg_ind.Name = "combox_tipo_msg_ind";
            combox_tipo_msg_ind.Size = new Size(225, 28);
            combox_tipo_msg_ind.TabIndex = 7;
            combox_tipo_msg_ind.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // label_tipo_mensaje_ind
            // 
            label_tipo_mensaje_ind.AutoSize = true;
            label_tipo_mensaje_ind.Location = new Point(416, 35);
            label_tipo_mensaje_ind.Name = "label_tipo_mensaje_ind";
            label_tipo_mensaje_ind.Size = new Size(126, 20);
            label_tipo_mensaje_ind.TabIndex = 6;
            label_tipo_mensaje_ind.Text = "Tipo de Mensaje: ";
            // 
            // text_canal
            // 
            text_canal.Location = new Point(122, 76);
            text_canal.Name = "text_canal";
            text_canal.Size = new Size(223, 27);
            text_canal.TabIndex = 9;
            text_canal.TextChanged += text_canal_TextChanged;
            text_canal.KeyPress += text_canal_KeyPress;
            // 
            // label_canal_vhf
            // 
            label_canal_vhf.AutoSize = true;
            label_canal_vhf.Location = new Point(6, 79);
            label_canal_vhf.Name = "label_canal_vhf";
            label_canal_vhf.Size = new Size(101, 20);
            label_canal_vhf.TabIndex = 8;
            label_canal_vhf.Text = "Canal de VHF:";
            // 
            // box_ind
            // 
            box_ind.Controls.Add(label_motivo);
            box_ind.Controls.Add(dataGridView1);
            box_ind.Controls.Add(label_categoria);
            box_ind.Controls.Add(text_canal);
            box_ind.Controls.Add(combox_categoria);
            box_ind.Controls.Add(combox_motivo);
            box_ind.Controls.Add(label_canal_vhf);
            box_ind.Controls.Add(label_tipo_mensaje_ind);
            box_ind.Controls.Add(combox_tipo_msg_ind);
            box_ind.Location = new Point(21, 102);
            box_ind.Name = "box_ind";
            box_ind.Size = new Size(797, 345);
            box_ind.TabIndex = 12;
            box_ind.TabStop = false;
            box_ind.Text = "INDIVIDUAL";
            box_ind.Visible = false;
            // 
            // label_motivo
            // 
            label_motivo.AutoSize = true;
            label_motivo.Location = new Point(416, 79);
            label_motivo.Name = "label_motivo";
            label_motivo.Size = new Size(59, 20);
            label_motivo.TabIndex = 11;
            label_motivo.Text = "Motivo:";
            label_motivo.Visible = false;
            // 
            // dataGridView1
            // 
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(6, 114);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 51;
            dataGridView1.Size = new Size(785, 225);
            dataGridView1.TabIndex = 10;
            dataGridView1.CellClick += dataGridView1_CellClick;
            // 
            // combox_motivo
            // 
            combox_motivo.FormattingEnabled = true;
            combox_motivo.Items.AddRange(new object[] { "No se indica el motivo", "Congestión en el centro de conmutación marítima ", "Ocupado", "Indicación de cola de espera", "Estación prohibida", "No hay operador disponible", "Operador temporalmente no disponible", "Equipo desconectado", "Incapaz de utilizar el canal propuesto ", "Incapaz de utilizar el modo propuesto" });
            combox_motivo.Location = new Point(481, 75);
            combox_motivo.Name = "combox_motivo";
            combox_motivo.Size = new Size(283, 28);
            combox_motivo.TabIndex = 12;
            combox_motivo.Visible = false;
            // 
            // boton_enviar_ind
            // 
            boton_enviar_ind.BackColor = Color.FromArgb(128, 255, 128);
            boton_enviar_ind.Location = new Point(676, 27);
            boton_enviar_ind.Name = "boton_enviar_ind";
            boton_enviar_ind.Size = new Size(109, 69);
            boton_enviar_ind.TabIndex = 13;
            boton_enviar_ind.Text = "Enviar";
            boton_enviar_ind.UseVisualStyleBackColor = false;
            boton_enviar_ind.Visible = false;
            boton_enviar_ind.Click += boton_enviar_ind_Click;
            // 
            // box_all
            // 
            box_all.Controls.Add(dataGridView2);
            box_all.Controls.Add(text_canal_all);
            box_all.Controls.Add(label_canal_all);
            box_all.Controls.Add(label_cat_all);
            box_all.Controls.Add(combox_cat_all);
            box_all.Location = new Point(21, 102);
            box_all.Name = "box_all";
            box_all.Size = new Size(797, 345);
            box_all.TabIndex = 11;
            box_all.TabStop = false;
            box_all.Text = "ALL SHIPS";
            box_all.Visible = false;
            // 
            // dataGridView2
            // 
            dataGridView2.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView2.Location = new Point(6, 66);
            dataGridView2.Name = "dataGridView2";
            dataGridView2.RowHeadersWidth = 51;
            dataGridView2.Size = new Size(785, 273);
            dataGridView2.TabIndex = 17;
            dataGridView2.CellClick += dataGridView2_CellClick;
            // 
            // text_canal_all
            // 
            text_canal_all.Location = new Point(548, 27);
            text_canal_all.Name = "text_canal_all";
            text_canal_all.Size = new Size(223, 27);
            text_canal_all.TabIndex = 16;
            // 
            // label_canal_all
            // 
            label_canal_all.AutoSize = true;
            label_canal_all.Location = new Point(432, 30);
            label_canal_all.Name = "label_canal_all";
            label_canal_all.Size = new Size(101, 20);
            label_canal_all.TabIndex = 15;
            label_canal_all.Text = "Canal de VHF:";
            // 
            // label_cat_all
            // 
            label_cat_all.AutoSize = true;
            label_cat_all.Location = new Point(6, 30);
            label_cat_all.Name = "label_cat_all";
            label_cat_all.Size = new Size(180, 20);
            label_cat_all.TabIndex = 13;
            label_cat_all.Text = "Seleccione una Categoría:";
            // 
            // combox_cat_all
            // 
            combox_cat_all.FormattingEnabled = true;
            combox_cat_all.Items.AddRange(new object[] { "SEGURIDAD", "URGENCIA" });
            combox_cat_all.Location = new Point(192, 27);
            combox_cat_all.Name = "combox_cat_all";
            combox_cat_all.Size = new Size(153, 28);
            combox_cat_all.TabIndex = 14;
            combox_cat_all.SelectedIndexChanged += combox_cat_all_SelectedIndexChanged;
            // 
            // box_grupos
            // 
            box_grupos.Controls.Add(dataGridView3);
            box_grupos.Controls.Add(text_canal_group);
            box_grupos.Controls.Add(label_group_vhf);
            box_grupos.Controls.Add(label_sig_com_g);
            box_grupos.Controls.Add(combox_sig_com);
            box_grupos.Location = new Point(15, 102);
            box_grupos.Name = "box_grupos";
            box_grupos.Size = new Size(797, 345);
            box_grupos.TabIndex = 14;
            box_grupos.TabStop = false;
            box_grupos.Text = "GRUPOS";
            box_grupos.Visible = false;
            // 
            // dataGridView3
            // 
            dataGridView3.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView3.Location = new Point(6, 66);
            dataGridView3.Name = "dataGridView3";
            dataGridView3.RowHeadersWidth = 51;
            dataGridView3.Size = new Size(785, 273);
            dataGridView3.TabIndex = 17;
            dataGridView3.CellClick += dataGridView3_CellClick;
            // 
            // text_canal_group
            // 
            text_canal_group.Location = new Point(548, 27);
            text_canal_group.Name = "text_canal_group";
            text_canal_group.Size = new Size(223, 27);
            text_canal_group.TabIndex = 16;
            // 
            // label_group_vhf
            // 
            label_group_vhf.AutoSize = true;
            label_group_vhf.Location = new Point(432, 30);
            label_group_vhf.Name = "label_group_vhf";
            label_group_vhf.Size = new Size(101, 20);
            label_group_vhf.TabIndex = 15;
            label_group_vhf.Text = "Canal de VHF:";
            // 
            // label_sig_com_g
            // 
            label_sig_com_g.AutoSize = true;
            label_sig_com_g.Location = new Point(6, 30);
            label_sig_com_g.Name = "label_sig_com_g";
            label_sig_com_g.Size = new Size(170, 20);
            label_sig_com_g.TabIndex = 13;
            label_sig_com_g.Text = "Comunicación siguiente:";
            // 
            // combox_sig_com
            // 
            combox_sig_com.FormattingEnabled = true;
            combox_sig_com.Items.AddRange(new object[] { "RT todos los modos", "J3E TP", "F1B/J2B TTY-FEC " });
            combox_sig_com.Location = new Point(192, 27);
            combox_sig_com.Name = "combox_sig_com";
            combox_sig_com.Size = new Size(153, 28);
            combox_sig_com.TabIndex = 14;
            combox_sig_com.SelectedIndexChanged += combox_sig_com_SelectedIndexChanged;
            // 
            // envios_rtas
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(830, 459);
            Controls.Add(box_grupos);
            Controls.Add(label_MMSI_rx);
            Controls.Add(boton_enviar_ind);
            Controls.Add(MMSI_rx);
            Controls.Add(label_form);
            Controls.Add(formato_selec);
            Controls.Add(box_all);
            Controls.Add(box_ind);
            Name = "envios_rtas";
            Text = "envios_rtas";
            box_ind.ResumeLayout(false);
            box_ind.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            box_all.ResumeLayout(false);
            box_all.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView2).EndInit();
            box_grupos.ResumeLayout(false);
            box_grupos.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView3).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListBox formato_selec;
        private Label label_form;
        private Label label_MMSI_rx;
        private TextBox MMSI_rx;
        private Label label_categoria;
        private ComboBox combox_categoria;
        private ComboBox combox_tipo_msg_ind;
        private Label label_tipo_mensaje_ind;
        private TextBox text_canal;
        private Label label_canal_vhf;
        private GroupBox box_ind;
        private DataGridView dataGridView1;
        private Button boton_enviar_ind;
        private Label label_motivo;
        private ComboBox combox_motivo;
        private GroupBox box_all;
        private DataGridView dataGridView2;
        private Label label_cat_all;
        private TextBox text_canal_all;
        private ComboBox combox_cat_all;
        private Label label_canal_all;
        private GroupBox box_grupos;
        private DataGridView dataGridView3;
        private TextBox text_canal_group;
        private Label label_group_vhf;
        private Label label_sig_com_g;
        private ComboBox combox_sig_com;
    }
}