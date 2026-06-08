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
            SuspendLayout();
            // 
            // testo
            // 
            testo.AutoSize = true;
            testo.Location = new Point(62, 80);
            testo.Name = "testo";
            testo.Size = new Size(268, 20);
            testo.TabIndex = 0;
            testo.Text = "¿Enviar ACK o Retransmitir SOCORRO? ";
            // 
            // btn_ack
            // 
            btn_ack.BackColor = Color.Gray;
            btn_ack.Location = new Point(62, 163);
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
            btn_rtx.Location = new Point(214, 163);
            btn_rtx.Name = "btn_rtx";
            btn_rtx.Size = new Size(116, 57);
            btn_rtx.TabIndex = 2;
            btn_rtx.Text = "Retransmitir";
            btn_rtx.UseVisualStyleBackColor = false;
            btn_rtx.Click += btn_rtx_Click;
            // 
            // ack_rtx
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(387, 325);
            Controls.Add(btn_rtx);
            Controls.Add(btn_ack);
            Controls.Add(testo);
            Name = "ack_rtx";
            Text = "Respuesta";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label testo;
        private Button btn_ack;
        private Button btn_rtx;
    }
}