using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Demodulador_WinForm_1.Migrado
{
    /// <summary>
    /// Gestor de logging dual que escribe simultáneamente en:
    /// 1. MAINDISPLAY (RichTextBox para la UI)
    /// 2. Archivo de disco (en carpeta "Mensajes")
    /// 
    /// Thread-safe mediante Invoke() para UI y locks para almacenamiento.
    /// </summary>
    public class DisplayLogger
    {
        private readonly RichTextBox _mainDisplay;
        private readonly Almacenamiento _almacenamiento;
        private readonly object _displayLock = new object();

        public DisplayLogger(RichTextBox mainDisplay)
        {
            _mainDisplay = mainDisplay ?? throw new ArgumentNullException(nameof(mainDisplay));
            _almacenamiento = new Almacenamiento();
        }

        /// <summary>
        /// Escribe un mensaje en pantalla y lo almacena en archivo.
        /// Thread-safe: usa Invoke() si es necesario.
        /// </summary>
        /// <param name="message">Mensaje a escribir</param>
        public void Log(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            lock (_displayLock)
            {
                // Escribir en pantalla de forma thread-safe
                if (_mainDisplay?.InvokeRequired == true)
                {
                    _mainDisplay.Invoke(() => _mainDisplay.AppendText(message));
                }
                else if (_mainDisplay != null)
                {
                    _mainDisplay.AppendText(message);
                }
            }
        }

        /// <summary>
        /// Registra un campo clave-valor para el mensaje actual.
        /// Estos campos se guardarán en archivo cuando se complete el mensaje.
        /// </summary>
        public void RegistrarCampo(string clave, string valor)
        {
            _almacenamiento.AgregarCampo(clave, valor);
        }

        /// <summary>
        /// Establece el formato del mensaje actual (ej: "SOCORRO", "INDIVIDUAL").
        /// </summary>
        public void EstablecerFormato(string formato)
        {
            _almacenamiento.EstablecerFormato(formato);
        }

        /// <summary>
        /// Guarda el mensaje actual en archivo y limpia el almacenamiento.
        /// </summary>
        public void GuardarMensaje()
        {
            _almacenamiento.GuardarMensaje();
            _almacenamiento.Limpiar();
        }

        /// <summary>
        /// Limpia el display de la pantalla.
        /// </summary>
        public void LimpiarDisplay()
        {
            lock (_displayLock)
            {
                if (_mainDisplay?.InvokeRequired == true)
                {
                    _mainDisplay.Invoke(() => _mainDisplay.Clear());
                }
                else if (_mainDisplay != null)
                {
                    _mainDisplay.Clear();
                }
            }
        }

        /// <summary>
        /// Obtiene una copia de los campos actuales (para debug).
        /// </summary>
        public List<(string Clave, string Valor)> ObtenerCamposActuales()
        {
            return _almacenamiento.ObtenerCampos();
        }

        /// <summary>
        /// Obtiene el formato actual (para debug).
        /// </summary>
        public string ObtenerFormatoActual()
        {
            return _almacenamiento.ObtenerFormato();
        }
    }
}
