using Dem_v2;
using Demodulador_WinForm_1.Migrado;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Demodulador_WinForm_1.Ventana_new
{
    public partial class ventana_mensaje : Form
    {
        private readonly Metodos _metodos;
        private readonly Mensaje _mensaje;
        private readonly DisplayLogger _logger;
        private readonly Expansion _expansion;
        public ventana_mensaje(Mensaje msg)
        {
            InitializeComponent();
            _mensaje = msg;

            // Inicializar DisplayLogger con el RichTextBox
            _logger = new DisplayLogger(txt_msj);

            // Pasar LogToDisplay y DisplayLogger a Metodos
            _metodos = new Metodos(LogToDisplay, _logger);

            _expansion = new Expansion(LogToDisplay, _logger);

            if (_mensaje != null)
            {
                switch (msg.Formato)
                {
                    case 102:
                        _metodos.MGeografica(msg.Mensaje_List);
                        break;

                    case 112:
                        _metodos.MSocorro(msg.Mensaje_List);
                        break;

                    case 114:
                        _metodos.MGrupos(msg.Mensaje_List);
                        break;

                    case 116:
                        _metodos.MAllShips(msg.Mensaje_List);
                        break;

                    case 120:
                        _metodos.MIndividual(msg.Mensaje_List);
                        break;

                    case 123:
                        LogToDisplay("Formato 123 detectado (no implementado)\n");
                        break;

                    default:
                        LogToDisplay($"Formato desconocido: {msg.Formato}\n");
                        break;
                }

                LogToDisplay("\n");

                if (msg.extension && msg.Mensaje_ext != null)
                {
                    _expansion.Decodificar(msg.Mensaje_ext); // NO ME DEJA DECOFICIAR
                }
            }
        }

        /// <summary>
        /// Método helper que escribe en el txt_msj de forma thread-safe.
        /// </summary>
        private void LogToDisplay(string message)
        {
            _logger.Log(message);
        }

        private void ventana_mensaje_Load(object sender, EventArgs e)
        {

        }
    }
}
