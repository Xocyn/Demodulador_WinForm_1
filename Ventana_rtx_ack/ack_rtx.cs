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

        }
    }
}
