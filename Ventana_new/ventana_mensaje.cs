using Dem_v2;
using Demodulador_WinForm_1.Migrado;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Demodulador_WinForm_1.Ventana_new
{
    public partial class ventana_mensaje : Form
    {
        public ventana_mensaje(Mensaje_2 msg)
        {
            ArgumentNullException.ThrowIfNull(msg);
            InitializeComponent();
            txt_msj.ReadOnly = true;
            txt_msj.BackColor = Color.White;
            txt_msj.Text = $"Recibido: {msg.Fecha_recepcion:dd/MM/yyyy HH:mm:ss}{Environment.NewLine}" +
                $"ECC: correcto{Environment.NewLine}{Environment.NewLine}" +
                msg.TextoDecodificado + Environment.NewLine +
                $"Caracteres validados: [{string.Join(", ", msg.Mensaje_List)}]";
            if (msg.Mensaje_ext != null)
                txt_msj.AppendText($"{Environment.NewLine}Caracteres de extensión validados: " +
                    $"[{string.Join(", ", msg.Mensaje_ext)}]");
        }

        private void ventana_mensaje_Load(object sender, EventArgs e)
        {

        }
    }
}
