using Dem_v2;
using Demodulador_WinForm_1.Migrado;
using Demodulador_WinForm_1.Ventana_new;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Demodulador_WinForm_1.Ventana_rtx_ack
{
    public partial class ack_rtx : Form
    {
        private readonly Mensaje_2 _mensaje;
        private readonly CapturaDatos _capturaDatos;
        bool rtx = false;
        public ack_rtx(Mensaje_2 msg, CapturaDatos capturaDatos)
        {
            InitializeComponent();
            _mensaje = msg ?? throw new ArgumentNullException(nameof(msg));
            _capturaDatos = capturaDatos ?? throw new ArgumentNullException(nameof(capturaDatos));
            _capturaDatos.Pause();
            FormClosed += (_, _) => _capturaDatos.Resume();
        }

        private void btn_ack_Click(object sender, EventArgs e)
        {
            rtx = false;
            EnviarRespuesta();
        }

        private void btn_rtx_Click(object sender, EventArgs e)
        {
            rtx = true;
            if (!EnviarRespuesta())
                return;
            btn_rtx.Enabled = false;
            btn_all.Checked = false;
            btn_ind.Checked = false;
            label_mssirx.Visible = false;
            text_mmsi_rx.Visible = false;
        }

        private bool EnviarRespuesta()
        {
            _capturaDatos.Pause();
            try
            {
                Respuesta.Decidir(_mensaje, rtx);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo enviar la respuesta: {ex.Message}", "Respuesta");
                return false;
            }
            finally
            {
                _capturaDatos.Resume();
            }
        }

        private void btn_all_CheckedChanged(object sender, EventArgs e)
        {
            label_mssirx.Visible = false;
            text_mmsi_rx.Visible = false;
            VerificarCondiciones();
        }

        private void btn_ind_CheckedChanged(object sender, EventArgs e)
        {
            label_mssirx.Visible = true;
            text_mmsi_rx.Visible = true;
            VerificarCondiciones();
        }

        private void text_mmsi_rx_TextChanged(object sender, EventArgs e)
        {
            VerificarCondiciones();
        }

        private void text_mmsi_rx_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Permitir solo números o tecla de borrar
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
            }

            // El MMSI tiene 9 dígitos.
            if (text_mmsi_rx.Text.Length >= 9 && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
            }

            VerificarCondiciones();
        }

        private void VerificarCondiciones()
        {
            bool mmsiValido = text_mmsi_rx.Text.Length == 9 &&
                text_mmsi_rx.Text.All(char.IsAsciiDigit);
            btn_rtx.Enabled = btn_all.Checked || (btn_ind.Checked && mmsiValido);
            if (btn_ind.Checked && mmsiValido)
            {
                _mensaje.formato_rtx = 120;
                _mensaje.MMSI_RX = text_mmsi_rx.Text;
            }
            else if (btn_all.Checked)
            {
                _mensaje.formato_rtx = 116;
                _mensaje.MMSI_RX = string.Empty;
            }
            else
            {
                _mensaje.formato_rtx = 0;
                _mensaje.MMSI_RX = string.Empty;
            }
        }

    }
}
