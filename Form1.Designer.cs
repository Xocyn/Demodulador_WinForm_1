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
            label_banda = new Label();
            combox_hf_vhf = new ComboBox();
            dispositivos = new Label();
            combox_dispositivos = new ComboBox();
            splitContainer1 = new SplitContainer();
            DISPLAYSECUNDARIO = new RichTextBox();
            MAINDISPLAY = new RichTextBox();
            waveViewer1 = new NAudio.Gui.WaveViewer();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // label_banda
            // 
            label_banda.AutoSize = true;
            label_banda.Location = new Point(23, 17);
            label_banda.Name = "label_banda";
            label_banda.Size = new Size(127, 20);
            label_banda.TabIndex = 0;
            label_banda.Text = "Banda a escuchar:";
            // 
            // combox_hf_vhf
            // 
            combox_hf_vhf.FormattingEnabled = true;
            combox_hf_vhf.Items.AddRange(new object[] { "MF/HF", "VHF" });
            combox_hf_vhf.Location = new Point(164, 16);
            combox_hf_vhf.Name = "combox_hf_vhf";
            combox_hf_vhf.Size = new Size(115, 28);
            combox_hf_vhf.TabIndex = 1;
            combox_hf_vhf.SelectedIndexChanged += combox_hf_vhf_SelectedIndexChanged;
            // 
            // dispositivos
            // 
            dispositivos.AutoSize = true;
            dispositivos.Location = new Point(23, 61);
            dispositivos.Name = "dispositivos";
            dispositivos.Size = new Size(174, 20);
            dispositivos.TabIndex = 2;
            dispositivos.Text = "Dispositivos Disponibles:";
            // 
            // combox_dispositivos
            // 
            combox_dispositivos.FormattingEnabled = true;
            combox_dispositivos.Location = new Point(203, 58);
            combox_dispositivos.Name = "combox_dispositivos";
            combox_dispositivos.Size = new Size(245, 28);
            combox_dispositivos.TabIndex = 3;
            combox_dispositivos.SelectedIndexChanged += combox_dispositivos_SelectedIndexChanged;
            // 
            // splitContainer1
            // 
            splitContainer1.Location = new Point(23, 109);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(DISPLAYSECUNDARIO);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(MAINDISPLAY);
            splitContainer1.Size = new Size(894, 357);
            splitContainer1.SplitterDistance = 298;
            splitContainer1.TabIndex = 4;
            // 
            // DISPLAYSECUNDARIO
            // 
            DISPLAYSECUNDARIO.Location = new Point(3, 3);
            DISPLAYSECUNDARIO.Name = "DISPLAYSECUNDARIO";
            DISPLAYSECUNDARIO.Size = new Size(292, 351);
            DISPLAYSECUNDARIO.TabIndex = 0;
            DISPLAYSECUNDARIO.Text = "";
            // 
            // MAINDISPLAY
            // 
            MAINDISPLAY.Location = new Point(3, 3);
            MAINDISPLAY.Name = "MAINDISPLAY";
            MAINDISPLAY.Size = new Size(586, 351);
            MAINDISPLAY.TabIndex = 0;
            MAINDISPLAY.Text = "";
            // 
            // waveViewer1
            // 
            waveViewer1.Location = new Point(454, 6);
            waveViewer1.Name = "waveViewer1";
            waveViewer1.SamplesPerPixel = 128;
            waveViewer1.Size = new Size(463, 100);
            waveViewer1.StartPosition = 0L;
            waveViewer1.TabIndex = 5;
            waveViewer1.WaveStream = null;
            waveViewer1.Load += waveViewer1_Load;
            // 
            // Demodulador_DSC
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(946, 490);
            Controls.Add(waveViewer1);
            Controls.Add(splitContainer1);
            Controls.Add(combox_dispositivos);
            Controls.Add(dispositivos);
            Controls.Add(combox_hf_vhf);
            Controls.Add(label_banda);
            Name = "Demodulador_DSC";
            Text = "Demodulador DSC";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
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
        private NAudio.Gui.WaveViewer waveViewer1;
    }
}
