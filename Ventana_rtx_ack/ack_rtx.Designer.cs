namespace Demodulador_WinForm_1.Ventana_rtx_ack
{
    partial class ack_rtx
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
            testo = new Label();
            btn_ack = new Button();
            btn_rtx = new Button();
            box_rtx = new GroupBox();
            btn_ind = new RadioButton();
            btn_all = new RadioButton();
            text_mmsi_rx = new TextBox();
            label_mssirx = new Label();
            box_rtx.SuspendLayout();
            SuspendLayout();
            // 
            // testo
            // 
            testo.AutoSize = true;
            testo.Location = new Point(34, 30);
            testo.Name = "testo";
            testo.Size = new Size(84, 20);
            testo.TabIndex = 0;
            testo.Text = "Enviar ACK:";
            // 
            // btn_ack
            // 
            btn_ack.BackColor = Color.Gray;
            btn_ack.Location = new Point(214, 12);
            btn_ack.Name = "btn_ack";
            btn_ack.Size = new Size(107, 57);
            btn_ack.TabIndex = 1;
            btn_ack.Text = "ACK";
            btn_ack.UseVisualStyleBackColor = false;
            btn_ack.Click += btn_ack_Click;
            // 
            // btn_rtx
            // 
            btn_rtx.BackColor = Color.Silver;
            btn_rtx.Enabled = false;
            btn_rtx.Location = new Point(215, 136);
            btn_rtx.Name = "btn_rtx";
            btn_rtx.Size = new Size(106, 57);
            btn_rtx.TabIndex = 2;
            btn_rtx.Text = "Retransmitir";
            btn_rtx.UseVisualStyleBackColor = false;
            btn_rtx.Click += btn_rtx_Click;
            // 
            // box_rtx
            // 
            box_rtx.Controls.Add(btn_ind);
            box_rtx.Controls.Add(btn_all);
            box_rtx.Location = new Point(34, 108);
            box_rtx.Name = "box_rtx";
            box_rtx.Size = new Size(133, 99);
            box_rtx.TabIndex = 3;
            box_rtx.TabStop = false;
            box_rtx.Text = "Retransmitir";
            // 
            // btn_ind
            // 
            btn_ind.AutoSize = true;
            btn_ind.Location = new Point(8, 61);
            btn_ind.Name = "btn_ind";
            btn_ind.Size = new Size(111, 24);
            btn_ind.TabIndex = 1;
            btn_ind.TabStop = true;
            btn_ind.Text = "INDIVIDUAL";
            btn_ind.UseVisualStyleBackColor = true;
            btn_ind.CheckedChanged += btn_ind_CheckedChanged;
            // 
            // btn_all
            // 
            btn_all.AutoSize = true;
            btn_all.Location = new Point(8, 31);
            btn_all.Name = "btn_all";
            btn_all.Size = new Size(97, 24);
            btn_all.TabIndex = 0;
            btn_all.TabStop = true;
            btn_all.Text = "ALL SHIPS";
            btn_all.UseVisualStyleBackColor = true;
            btn_all.CheckedChanged += btn_all_CheckedChanged;
            // 
            // text_mmsi_rx
            // 
            text_mmsi_rx.Location = new Point(132, 234);
            text_mmsi_rx.Name = "text_mmsi_rx";
            text_mmsi_rx.Size = new Size(159, 27);
            text_mmsi_rx.TabIndex = 4;
            text_mmsi_rx.Visible = false;
            text_mmsi_rx.TextChanged += text_mmsi_rx_TextChanged;
            text_mmsi_rx.KeyPress += text_mmsi_rx_KeyPress;
            // 
            // label_mssirx
            // 
            label_mssirx.AutoSize = true;
            label_mssirx.Location = new Point(65, 237);
            label_mssirx.Name = "label_mssirx";
            label_mssirx.Size = new Size(67, 20);
            label_mssirx.TabIndex = 5;
            label_mssirx.Text = "MSSI RX:";
            label_mssirx.Visible = false;
            // 
            // ack_rtx
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(387, 325);
            Controls.Add(label_mssirx);
            Controls.Add(text_mmsi_rx);
            Controls.Add(box_rtx);
            Controls.Add(btn_rtx);
            Controls.Add(btn_ack);
            Controls.Add(testo);
            Name = "ack_rtx";
            Text = "Respuesta";
            box_rtx.ResumeLayout(false);
            box_rtx.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label testo;
        private Button btn_ack;
        private Button btn_rtx;
        private GroupBox box_rtx;
        private RadioButton btn_ind;
        private RadioButton btn_all;
        private TextBox text_mmsi_rx;
        private Label label_mssirx;
    }
}