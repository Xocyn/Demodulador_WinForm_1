using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Demodulador_WinForm_1.Migrado
{
    internal sealed class Procesamiento2
    {
        private readonly RichTextBox? _mainDisplay;
        private List<int> _mensajePrincipal = new();
        private List<int>? _mensajeAlternativo;

        public Procesamiento2(RichTextBox? mainDisplay)
        {
            _mainDisplay = mainDisplay;
        }

        public void IniciarNuevaCaptura()
        {
            _mensajePrincipal.Clear();
            _mensajeAlternativo = null;
            ActualizarDisplay(string.Empty);
        }

        public void MostrarLista(IReadOnlyList<int> caracteres)
        {
            ArgumentNullException.ThrowIfNull(caracteres);
            ActualizarDisplay($"[{string.Join(", ", caracteres)}]{Environment.NewLine}");
        }

        public bool Procesar(List<int> caracteres)
        {
            ArgumentNullException.ThrowIfNull(caracteres);

            int inicioMensaje = 0;
            while (inicioMensaje < caracteres.Count &&
                   Dem_v2.FormatSpecifier.Formato(caracteres[inicioMensaje]) ==
                       Dem_v2.FormatSpecifier.ValorNoReconocido)
            {
                inicioMensaje++;
            }

            caracteres.RemoveRange(0, inicioMensaje);
            _mensajePrincipal = new List<int>();
            _mensajeAlternativo = null;

            if (caracteres.Count == 0)
            {
                MostrarLista(caracteres);
                return false;
            }

            int comparados = 0;
            int diferencias = 0;
            int indiceFin = -1;

            // Los originales ocupan las posiciones pares de la trama intercalada.
            // La retransmisión de cada original está cinco posiciones más adelante.
            for (int i = 0; i + 5 < caracteres.Count; i += 2)
            {
                int original = caracteres[i];
                int retransmitido = caracteres[i + 5];

                if (original != retransmitido)
                {
                    // Clonar lo ya recorrido una sola vez y conservar los RX en la alternativa.
                    _mensajeAlternativo ??= new List<int>(_mensajePrincipal);
                    diferencias++;
                }

                _mensajePrincipal.Add(original);
                _mensajeAlternativo?.Add(original == retransmitido ? original : retransmitido);
                comparados++;

                if (EsFinDeSecuencia(original) || EsFinDeSecuencia(retransmitido))
                {
                    indiceFin = i;
                    break;
                }
            }

            var resultado = new StringBuilder();
            resultado.AppendLine($"Lista desde formato: [{string.Join(", ", caracteres)}]");
            resultado.AppendLine($"Caracteres originales: [{string.Join(", ", _mensajePrincipal)}]");
            if (_mensajeAlternativo != null)
                resultado.AppendLine($"Alternativa retransmitida: [{string.Join(", ", _mensajeAlternativo)}]");
            resultado.AppendLine($"Comparados: {comparados}; diferencias: {diferencias}");

            if (indiceFin < 0)
            {
                resultado.AppendLine("EOS no encontrado o retransmisión incompleta; ECC sin verificar.");
                ActualizarDisplay(resultado.ToString());
                return false;
            }

            // En la trama intercalada, el ECC original sigue al EOS en +2;
            // la copia retransmitida del ECC está en +7.
            int eccOriginal = indiceFin + 2 < caracteres.Count ? caracteres[indiceFin + 2] : -1;
            int eccRetransmitido = indiceFin + 7 < caracteres.Count ? caracteres[indiceFin + 7] : -1;
            int calculadoPrincipal = CalcularEcc(_mensajePrincipal);
            bool principalCorrecto = EsFinDeSecuencia(_mensajePrincipal[^1]) &&
                                     CoincideEcc(calculadoPrincipal, eccOriginal, eccRetransmitido);
            bool alternativoCorrecto = false;

            int simboloFinal = EsFinDeSecuencia(caracteres[indiceFin])
                ? caracteres[indiceFin]
                : caracteres[indiceFin + 5];
            resultado.AppendLine($"Fin: {Dem_v2.General.ACK(simboloFinal)}");
            resultado.AppendLine($"ECC recibido: DX={eccOriginal}, RX={eccRetransmitido}");
            resultado.AppendLine($"ECC originales: calculado={calculadoPrincipal}, {(principalCorrecto ? "correcto" : "incorrecto")}");

            if (_mensajeAlternativo != null)
            {
                int calculadoAlternativo = CalcularEcc(_mensajeAlternativo);
                alternativoCorrecto = EsFinDeSecuencia(_mensajeAlternativo[^1]) &&
                                      CoincideEcc(calculadoAlternativo, eccOriginal, eccRetransmitido);
                resultado.AppendLine($"ECC alternativa: calculado={calculadoAlternativo}, {(alternativoCorrecto ? "correcto" : "incorrecto")}");
            }

            ActualizarDisplay(resultado.ToString());
            return principalCorrecto || alternativoCorrecto;
        }

        private static bool EsFinDeSecuencia(int valor) =>
            Dem_v2.General.ACK(valor) != "¿?";

        private static int CalcularEcc(IReadOnlyList<int> mensaje)
        {
            // Respuesta.cs incluye el formato en el XOR; cuando lo transmite dos
            // veces al comienzo, sólo incluye la segunda copia.
            int inicio = mensaje.Count > 1 && mensaje[0] == mensaje[1] ? 1 : 0;
            int ecc = 0;
            for (int i = inicio; i < mensaje.Count; i++)
            {
                if (mensaje[i] < 0)
                    return -1;
                ecc ^= mensaje[i];
            }

            return ecc & 0x7F;
        }

        private static bool CoincideEcc(int calculado, int original, int retransmitido) =>
            calculado >= 0 && (calculado == original || calculado == retransmitido);

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
