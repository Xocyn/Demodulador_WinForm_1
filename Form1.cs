using Dem_v2;
using Demodulador_WinForm_1.Ventana_new;
using Demodulador_WinForm_1.Ventana_rtas;
using NAudio.Wave;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Demodulador_WinForm_1
{
    public partial class Demodulador_DSC : Form
    {
        private CapturaDatos _capturaDatos;
        private bool _isCapturing = false;
        private readonly Procesamiento _procesamiento;
        public bool vhf => combox_hf_vhf.SelectedIndex == 1;
        public Demodulador_DSC()
        {
            InitializeComponent();
            MAINDISPLAY.ReadOnly = true;
            MAINDISPLAY.BackColor = Color.White;

            DISPLAYSECUNDARIO.ReadOnly = true;
            DISPLAYSECUNDARIO.BackColor = Color.White;

            _procesamiento = new Procesamiento(MAINDISPLAY, this);

            //this.WindowState = FormWindowState.Maximized;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            _capturaDatos = new CapturaDatos(this, _procesamiento);

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

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        /// <summary>
        /// Agrega una fila a la tabla de forma thread-safe.
        /// Detecta si estamos en el thread de UI y usa Invoke() si es necesario.
        /// </summary>
        public void AgregarFila(string formato, string categoria, string hora, string ecc, string rta)
        {
            if (dataGridView1.InvokeRequired)
            {
                // Estamos en un thread diferente, usar Invoke para actualizar UI
                this.Invoke(() => AgregarFila(formato, categoria, hora, ecc, rta));
            }
            else
            {
                // Estamos en el thread de UI, actualizar directamente
                dataGridView1.Rows.Insert(0, formato, categoria, hora, ecc, rta);
            }
        }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            Mensaje msg = null;
            lock (_procesamiento.HistorialLock)
            {
                if (e.RowIndex < _procesamiento.HISTORIAL.Count)
                    msg = _procesamiento.HISTORIAL[e.RowIndex];
            }

            if (msg == null) return;

            if (dataGridView1.Columns[e.ColumnIndex].Name == "see_msg")
            {
                var ventana = new ventana_mensaje(msg);

                this.Enabled = false;

                ventana.FormClosed += (s, args) =>
                {
                    this.Enabled = true;
                };

                ventana.Show();
            }

            if (dataGridView1.Columns[e.ColumnIndex].Name == "rta_msg")
            {
                _capturaDatos.Pause();
                Respuesta.Decidir(msg);
                _capturaDatos.Resume();
            }
        }

        private void detener_Click(object sender, EventArgs e)
        {
            DialogResult resultado = MessageBox.Show(
             "¿DETENER LA CAPTURA DE DATOS?",
             "",
             MessageBoxButtons.YesNo,
             MessageBoxIcon.Warning
            );

            if (resultado == DialogResult.Yes)
            {
                _capturaDatos.END();
            }
        }

        private void enviar_btn_Click(object sender, EventArgs e)
        {
            var ventanaEnvio = new envios_rtas(vhf);
            ventanaEnvio.Show();
        }
    }

}

