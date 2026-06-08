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

        private string formato = "";
        public envios_rtas(bool vhf)
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

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

            dataGridView1.DataSource = dt; dataGridView2.DataSource = dt; dataGridView3.DataSource = dt;
            dataGridView1.AllowUserToAddRows = false; dataGridView2.AllowUserToAddRows = false; dataGridView3.AllowUserToAddRows = false;
            dataGridView1.RowHeadersVisible = false; dataGridView2.RowHeadersVisible = false; dataGridView3.RowHeadersVisible = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; dataGridView2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; dataGridView3.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.ReadOnly = true; dataGridView2.ReadOnly = true; dataGridView3.ReadOnly = true;
            dataGridView1.AllowUserToAddRows = false; dataGridView2.AllowUserToAddRows = false; dataGridView3.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false; dataGridView2.AllowUserToDeleteRows = false; dataGridView3.AllowUserToDeleteRows = false;
            dataGridView1.AllowUserToResizeColumns = false; dataGridView2.AllowUserToResizeColumns = false; dataGridView3.AllowUserToResizeColumns = false;
            dataGridView1.AllowUserToResizeRows = false; dataGridView2.AllowUserToResizeRows = false; dataGridView3.AllowUserToResizeRows = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect; dataGridView2.SelectionMode = DataGridViewSelectionMode.FullRowSelect; dataGridView3.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false; dataGridView2.MultiSelect = false; dataGridView3.MultiSelect = false;
        }
        private void formato_selec_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Verificar si hay algo seleccionado
            if (formato_selec.SelectedItem == null)
                return;

            string seleccion = formato_selec.SelectedItem.ToString();

            if (vhf && seleccion == "GEOGRAFICA")
            {
                MessageBox.Show("Opción deshabilitada para VHF");
                formato_selec.ClearSelected();
                return; // Importante: salir después de ClearSelected()
            }

            if (seleccion == "INDIVIDUAL")
            {
                Mostrar("INDIVIDUAL");
            }
            else if (seleccion == "ALL SHIPS")
            {
                Mostrar("ALL SHIPS");
            }
            else if (seleccion == "GRUPOS")
            {
                Mostrar("GRUPOS");
            }
            else if (seleccion == "GEOGRAFICA" && !vhf)
            {
                Mostrar("GEOGRAFICA");
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
            box_all.Visible = false;
            box_ind.Visible = false;
            box_grupos.Visible = false;

            if (eleccion == "INDIVIDUAL")
            {
                box_ind.Visible = true;
                combox_tipo_msg_ind.Enabled = false;
            }
            if (eleccion == "ALL SHIPS")
            {
                box_all.Visible = true;
            }
            if (eleccion == "GRUPOS")
            {
                box_grupos.Visible = true;
            }
            if (eleccion == "GEOGRAFICA")
            {
                box_geo.Visible = true;
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
            combox_tipo_msg_ind.SelectedIndex = -1;

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
        private void combox_cat_all_SelectedIndexChanged(object sender, EventArgs e)
        {
            VerificarCondiciones();
        }

        private void combox_sig_com_SelectedIndexChanged(object sender, EventArgs e)
        {
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
                formato = "INDIVIDUAL";
            }
            if (formato_selec.SelectedItem != null && formato_selec.SelectedItem.ToString() == "ALL SHIPS")
            {
                bool canal_all = !string.IsNullOrEmpty(text_canal_all.Text) && text_canal_all.Text.All(char.IsDigit);
                bool cat_all = combox_cat_all.SelectedIndex != -1;
                boton_enviar_ind.Visible = canal_all && cat_all;
                formato = "ALL SHIPS";
            }
            if (formato_selec.SelectedItem != null && formato_selec.SelectedItem.ToString() == "GRUPOS")
            {
                bool canal_group = !string.IsNullOrEmpty(text_canal_group.Text) && text_canal_group.Text.All(char.IsDigit);
                bool cat_group = combox_sig_com.SelectedIndex != -1;
                boton_enviar_ind.Visible = canal_group && cat_group && mmsirxValido;
                formato = "GRUPOS";
            }
            if (formato_selec.SelectedItem != null && formato_selec.SelectedItem.ToString() == "GEOGRAFICA")
            {
                bool canal_geo = !string.IsNullOrEmpty(text_canal_hf.Text) && text_canal_hf.Text.All(char.IsDigit);
                bool cat_geo = combox_sig_com_geo.SelectedIndex != -1;
                boton_enviar_ind.Visible = canal_geo && cat_geo;
                formato = "GEOGRAFICA";
            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) // Evita que se ejecute al pulsar el encabezado
            {
                text_canal.Text = dataGridView1.Rows[e.RowIndex].Cells[0].Value?.ToString();
            }
        }

        private void dataGridView2_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) // Evita que se ejecute al pulsar el encabezado
            {
                text_canal_all.Text = dataGridView2.Rows[e.RowIndex].Cells[0].Value?.ToString();
            }
        }

        private void dataGridView3_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) // Evita que se ejecute al pulsar el encabezado
            {
                text_canal_group.Text = dataGridView3.Rows[e.RowIndex].Cells[0].Value?.ToString();
            }
        }

        private void text_canal_TextChanged(object sender, EventArgs e)
        {
            VerificarCondiciones();
        }

        private void boton_enviar_ind_Click(object sender, EventArgs e)
        {
            switch (formato)
            {
                case "INDIVIDUAL":
                    ProtocoloInd();
                    break;
                case "ALL SHIPS":
                    ProtocoloAll();
                    break;
                case "GRUPOS":
                    ProtocoloGrupos();
                    break;
                case "GEOGRAFICA":
                    ProtocoloGeografica();
                    break;
            }

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

        private void ProtocoloInd()
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

        private void ProtocoloAll()
        {
            int categoria;
            int canal = int.Parse(text_canal_all.Text);
            switch (combox_cat_all.SelectedIndex)
            {
                case 0: // SEGURIDAD
                    categoria = 108;
                    break;
                case 1: // URGENCIA
                    categoria = 110;
                    break;
                default:
                    categoria = 0;
                    break;
            }
            Respuesta.MensajeAllShips(categoria, canal);
            combox_cat_all.SelectedIndex = -1;
        }

        private void ProtocoloGrupos()
        {
            int sig_com;
            int canal = int.Parse(text_canal_group.Text);
            switch (combox_sig_com.SelectedIndex)
            {
                case 0: // RT all modes
                    sig_com = 100;
                    break;
                case 1: // J3E
                    sig_com = 109;
                    break;
                case 2: // FEC - TTY
                    sig_com = 113;
                    break;
                default:
                    sig_com = 126;
                    break;
            }
            Respuesta.MensajeGrupos(sig_com, MMSI_rx.Text, canal);
            combox_sig_com.SelectedIndex = -1;

        }

        private void ProtocoloGeografica()
        {
            int sig_com;
            int canal = int.Parse(text_canal_hf.Text);
            switch (combox_sig_com_geo.SelectedIndex)
            {
                case 0: // RT all modes
                    sig_com = 100;
                    break;
                case 1: // J3E
                    sig_com = 109;
                    break;
                case 2: // FEC - TTY
                    sig_com = 113;
                    break;
                default:
                    sig_com = 126;
                    break;
            }
            Respuesta.MensajeGeografico(sig_com, canal, 5);
            combox_sig_com_geo.SelectedIndex = -1;
        }

    }
}
