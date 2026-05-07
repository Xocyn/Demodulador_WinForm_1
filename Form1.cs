using NAudio.Wave;

namespace Demodulador_WinForm_1
{
    public partial class Demodulador_DSC : Form
    {
        private CapturaDatos _capturaDatos;
        private bool _isCapturing = false;

        public Demodulador_DSC()
        {
            InitializeComponent();
            _capturaDatos = new CapturaDatos(this);

            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                var caps = WaveInEvent.GetCapabilities(i);
                combox_dispositivos.Items.Add($"{i}: {caps.ProductName}");
            }

            this.FormClosing += (s, e) =>
            {
                if (_isCapturing)
                {
                    _capturaDatos.DetenerCaptura();
                }
            };
        }

        private void combox_hf_vhf_SelectedIndexChanged(object sender, EventArgs e)
        {
            MAINDISPLAY.Clear();
            combox_hf_vhf.Enabled = false;
            string banda = combox_hf_vhf.SelectedIndex == 1 ? "VHF" : "MF/HF";
            DISPLAYSECUNDARIO.AppendText($"Banda seleccionada: {banda}\n");

            if (_isCapturing)
            {
                DISPLAYSECUNDARIO.AppendText("Cambiando modo de captura...\n");
                _capturaDatos.CambiarModo();
            }
        }

        private void combox_dispositivos_SelectedIndexChanged(object sender, EventArgs e)
        {
            string dispositivo = combox_dispositivos.SelectedItem?.ToString() ?? "Desconocido";
            DISPLAYSECUNDARIO.AppendText($"Dispositivo seleccionado: {dispositivo}\n");
            combox_dispositivos.Enabled = false;

            if (!_isCapturing && combox_hf_vhf.SelectedIndex >= 0)
            {
                DISPLAYSECUNDARIO.AppendText("Iniciando captura de datos...\n");
                _capturaDatos.IniciarCaptura();
                _isCapturing = true;
            }
        }

    }
}
