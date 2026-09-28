using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Demodulador_WinForm_1.Migrado
{
    internal sealed class Procesamiento2
    {
        private readonly RichTextBox? _mainDisplay;

        public Procesamiento2(RichTextBox? mainDisplay)
        {
            _mainDisplay = mainDisplay;
        }

        public void IniciarNuevaCaptura()
        {
            ActualizarDisplay(string.Empty);
        }

        public void MostrarLista(IReadOnlyList<int> caracteres)
        {
            ArgumentNullException.ThrowIfNull(caracteres);
            ActualizarDisplay($"[{string.Join(", ", caracteres)}]{Environment.NewLine}");
        }

        // Entrada para las próximas secciones del postprocesamiento.
        public void Procesar(List<int> caracteres)
        {
            ArgumentNullException.ThrowIfNull(caracteres);
            MostrarLista(caracteres);
        }

        private void ActualizarDisplay(string texto)
        {
            if (_mainDisplay == null || _mainDisplay.IsDisposed || !_mainDisplay.IsHandleCreated)
                return;

            try
            {
                if (_mainDisplay.InvokeRequired)
                    _mainDisplay.BeginInvoke(() => _mainDisplay.Text = texto);
                else
                    _mainDisplay.Text = texto;
            }
            catch (InvalidOperationException) { } // El formulario se cerró durante la captura.
        }
    }
}
