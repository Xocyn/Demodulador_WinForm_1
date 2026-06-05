using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Dem_v2;

namespace Demodulador_WinForm_1.Ventana_rtas
{
    public partial class envios_rtas : Form
    {
        private bool vhf;
        public string mmsi_rx;

        private HashSet<int> indicesBloqueados =
         new HashSet<int>();

        private int ultimoIndiceValido = 0;
        public envios_rtas(bool vhf)
        {
            InitializeComponent();
            IniciarTabla();

            this.vhf = vhf;

            combox_tipo_msg_ind.DrawMode =
                DrawMode.OwnerDrawFixed;

            combox_tipo_msg_ind.DrawItem +=
                combox_tipo_msg_ind_DrawItem;

        }

        private void IniciarTabla()
        {
            // Cargar datos del CSV a la tabla

            DataTable dt = new DataTable();

            string csvPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..",
                "Ventana_rtas",
                "Tabla_frecuencias_2.csv"
            );
            csvPath = Path.GetFullPath(csvPath);

            string[] lineas = File.ReadAllLines(csvPath);

            string[] columnas = lineas[0].Split(',');

            foreach (string col in columnas)
            {
                dt.Columns.Add(col);
            }

            for (int i = 1; i < lineas.Length; i++)
            {
                dt.Rows.Add(lineas[i].Split(','));
            }

            dataGridView1.DataSource = dt;
            //this.WindowState = FormWindowState.Maximized;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.ReadOnly = true;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AllowUserToResizeColumns = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
        }
        private void formato_selec_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (vhf && formato_selec.SelectedIndex != -1 && formato_selec.SelectedItem.ToString() == "GEOGRAFICA")
            {
                MessageBox.Show("Opción deshabilitada para VHF");
                formato_selec.ClearSelected();
            }

            if (formato_selec.SelectedItem.ToString() == "INDIVIDUAL")
            {
                Mostrar("INDIVIDUAL");
            }
        }

        private void MMSI_rx_TextChanged(object sender, EventArgs e)
        {
            mmsi_rx = MMSI_rx.Text;
            VerificarCondiciones();
        }

        private void MMSI_rx_KeyPress_1(object sender, KeyPressEventArgs e)
        {
            // Permitir solo números o tecla de borrar
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
            }

            // Limitar a 9 caracteres
            if (MMSI_rx.Text.Length >= 9 && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void Mostrar(string eleccion)
        {
            if (eleccion == "INDIVIDUAL")
            {
                box_ind.Visible = true;
                combox_tipo_msg_ind.Enabled = false;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (indicesBloqueados.Contains(combox_tipo_msg_ind.SelectedIndex))
            {
                combox_tipo_msg_ind.SelectedIndex = ultimoIndiceValido;
            }
            else
            {
                ultimoIndiceValido = combox_tipo_msg_ind.SelectedIndex;
            }
            if (combox_tipo_msg_ind.SelectedIndex == 2)
            {
                label_motivo.Visible = true;
                combox_motivo.Visible = true;
            }
            VerificarCondiciones();
        }

        private void combox_categoria_SelectedIndexChanged(object sender, EventArgs e)
        {
            indicesBloqueados.Clear();

            string categoria = combox_categoria.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(categoria))
                return;

            // SEGURIDAD o URGENCIA
            if (categoria == "SEGURIDAD" || categoria == "URGENCIA")
            {
                for (int i = 6; i <= 9; i++)
                {
                    indicesBloqueados.Add(i);
                }
            }

            // RUTINA
            if (categoria == "RUTINA")
            {
                for (int i = 3; i <= 5; i++)
                {
                    indicesBloqueados.Add(i);
                }
            }

            // Redibujar ComboBox
            combox_tipo_msg_ind.Enabled = true;
            combox_tipo_msg_ind.Invalidate();
            VerificarCondiciones();
        }
        private void combox_tipo_msg_ind_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0)
                return;

            e.DrawBackground();

            string texto =
                combox_tipo_msg_ind.Items[e.Index].ToString();

            bool bloqueado =
                indicesBloqueados.Contains(e.Index);

            Brush brush =
                bloqueado
                ? Brushes.Gray
                : Brushes.Black;

            e.Graphics.DrawString(
                texto,
                e.Font,
                brush,
                e.Bounds);

            e.DrawFocusRectangle();
        }

        private void VerificarCondiciones()
        {
            bool mmsirxValido = MMSI_rx.Text.Length == 9 && MMSI_rx.Text.All(char.IsDigit);

            if (formato_selec.SelectedItem != null && formato_selec.SelectedItem.ToString() == "INDIVIDUAL")
            {
                bool canal_ind = !string.IsNullOrEmpty(text_canal.Text) && text_canal.Text.All(char.IsDigit);
                bool cat_ind = combox_categoria.SelectedIndex != -1 && combox_tipo_msg_ind.SelectedIndex != -1;
                boton_enviar_ind.Visible = mmsirxValido && canal_ind && cat_ind;
            }


        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) // Evita que se ejecute al pulsar el encabezado
            {
                text_canal.Text = dataGridView1.Rows[e.RowIndex].Cells[0].Value?.ToString();
            }
        }

        private void text_canal_TextChanged(object sender, EventArgs e)
        {
            VerificarCondiciones();
        }

        private void boton_enviar_ind_Click(object sender, EventArgs e)
        {
            int categoria;
            int tipo_msg_ind;
            bool acuse = false;
            int canal = int.Parse(text_canal.Text);
            int motivo = combox_motivo.SelectedIndex + 100;
            if (combox_motivo.SelectedIndex == -1)
            {
                motivo = 100;
            }
            switch (combox_categoria.SelectedIndex)
            {
                case 0: // RUTINA
                    categoria = 100;
                    break;
                case 1: // SEGURIDAD
                    categoria = 108;
                    break;
                case 2: // URGENCIA
                    categoria = 110;
                    break;
                default:
                    categoria = 0;
                    break;
            }
            switch (combox_tipo_msg_ind.SelectedIndex)
            {
                case 0: // RT ALL MODES
                    tipo_msg_ind = 100;
                    break;
                case 1: // ACUSE RT
                    tipo_msg_ind = 100;
                    acuse = true;
                    break;
                case 2: // IMPOSIBLE DAR ACUSE
                    tipo_msg_ind = 104;
                    acuse = true;
                    break;
                case 3: // SOLICITUD DE POSICION
                    tipo_msg_ind = 121;
                    break;
                case 4: // PRUEBA
                    tipo_msg_ind = 118;
                    break;
                case 5: // ACUSE PRUEBA
                    tipo_msg_ind = 118;
                    acuse = true;
                    break;
                case 6: // DATOS
                    tipo_msg_ind = 106;
                    break;
                case 7: // ACUSE DATOS
                    tipo_msg_ind = 107;
                    acuse = true;
                    break;
                case 8: // INTERROGACION SECUENCIAL
                    tipo_msg_ind = 103;
                    break;
                case 9: // ACUSE INTERROGACION SECUENCIAL
                    tipo_msg_ind = 103;
                    acuse = true;
                    break;
                default:
                    tipo_msg_ind = 0;
                    break;
            }
            Respuesta.MensajeIndividual(MMSI_rx.Text, categoria, tipo_msg_ind, acuse, canal, motivo);
            combox_categoria.SelectedIndex = -1;
            combox_tipo_msg_ind.SelectedIndex = -1;
            label_motivo.Visible = false;
            combox_motivo.Visible = false;
            combox_tipo_msg_ind.Enabled = false;
        }

        private void envios_rtas_Load(object sender, EventArgs e)
        {

        }

        private void text_canal_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
            }

            // Limitar a 3 caracteres
            if (text_canal.Text.Length >= 3 && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
            }
        }
    }
}
