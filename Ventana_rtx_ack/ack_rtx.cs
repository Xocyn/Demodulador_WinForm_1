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
        private readonly Mensaje _mensaje;
        private CapturaDatos _capturaDatos;
        private readonly Procesamiento _procesamiento;
        bool rtx = false;
        public ack_rtx(Mensaje msg)
        {
            InitializeComponent();
            _procesamiento = new Procesamiento();
            _capturaDatos = new CapturaDatos(_procesamiento);
            _mensaje = msg;
            _capturaDatos.Pause();
        }

        private void btn_ack_Click(object sender, EventArgs e)
        {
            rtx = false;
            Respuesta.Decidir(_mensaje, rtx);
            _capturaDatos.Resume();
        }

        private void btn_rtx_Click(object sender, EventArgs e)
        {
            rtx = true;
            Respuesta.Decidir(_mensaje, rtx);
            _capturaDatos.Resume();
            btn_rtx.Enabled = false;
            btn_all.Checked = false;
            btn_ind.Checked = false;
            label_mssirx.Visible = false;
            text_mmsi_rx.Visible = false;
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

            // Limitar a 6 caracteres
            if (text_mmsi_rx.Text.Length >= 9 && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
            }

            VerificarCondiciones();
        }

        private void VerificarCondiciones()
        {
            bool isIndividualSelected = btn_ind.Checked;
            bool isAllSelected = btn_all.Checked;
            if (isIndividualSelected && text_mmsi_rx.Text.Length == 9)
            { 
                btn_rtx.Enabled = true;
                _mensaje.formato_rtx = 120;
                _mensaje.MMSI_RX = text_mmsi_rx.Text;
            }
            else if (isAllSelected)
            {
                btn_rtx.Enabled = true;
                _mensaje.formato_rtx = 116;
            }
        }

    }
}
