namespace Demodulador_WinForm_1.Ventana_new
{
    partial class ventana_mensaje
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ventana_mensaje));
            txt_msj = new RichTextBox();
            SuspendLayout();
            // 
            // txt_msj
            // 
            txt_msj.Location = new Point(12, 12);
            txt_msj.Name = "txt_msj";
            txt_msj.Size = new Size(568, 519);
            txt_msj.TabIndex = 0;
            txt_msj.Text = "";
            // 
            // ventana_mensaje
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(592, 543);
            Controls.Add(txt_msj);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "ventana_mensaje";
            Load += ventana_mensaje_Load;
            ResumeLayout(false);
        }

        #endregion

        private RichTextBox txt_msj;
    }
}