using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Windows.Forms;

namespace Demodulador_WinForm_1.Migrado
{
    internal sealed class Procesamiento2
    {
        private readonly RichTextBox? _mainDisplay;
        private List<int>? _mensajeValidado;

        private readonly record struct EstadoEcc(int PrimerValor, bool FormatoDuplicado, int Xor);

        private sealed record NodoMensaje(NodoMensaje? Anterior, int Valor);

        private sealed class RutasEcc
        {
            public BigInteger Cantidad { get; set; }
            public NodoMensaje? Ejemplo { get; set; }
            public int SeleccionesRx { get; set; }

            public RutasEcc(BigInteger cantidad, NodoMensaje? ejemplo, int seleccionesRx)
            {
                Cantidad = cantidad;
                Ejemplo = ejemplo;
                SeleccionesRx = seleccionesRx;
            }
        }

        public Procesamiento2(RichTextBox? mainDisplay)
        {
            _mainDisplay = mainDisplay;
        }

        public void IniciarNuevaCaptura()
        {
            _mensajeValidado = null;
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
            _mensajeValidado = null;

            if (caracteres.Count == 0)
            {
                MostrarLista(caracteres);
                return false;
            }

            var rutas = new Dictionary<EstadoEcc, RutasEcc>
            {
                [new EstadoEcc(-1, false, 0)] = new RutasEcc(BigInteger.One, null, 0)
            };
            int paresInspeccionados = 0;
            int diferencias = 0;
            BigInteger combinacionesVerificadas = BigInteger.Zero;
            BigInteger combinacionesValidas = BigInteger.Zero;
            NodoMensaje? mejorMensaje = null;
            int mejoresSeleccionesRx = int.MaxValue;
            int mejorEcc = -1;
            int mejorEccDx = -1;
            int mejorEccRx = -1;
            bool finEncontrado = false;
            bool eccDisponible = false;

            // Los originales ocupan las posiciones pares de la trama intercalada.
            // La retransmisión de cada original está cinco posiciones más adelante.
            // Agrupar rutas con el mismo XOR permite probar todas las combinaciones
            // sin construir 2^N listas cuando hay muchas discrepancias.
            for (int i = 0; i + 5 < caracteres.Count && rutas.Count > 0; i += 2)
            {
                int original = caracteres[i];
                int retransmitido = caracteres[i + 5];
                if (original != retransmitido)
                    diferencias++;
                paresInspeccionados++;

                var siguientes = new Dictionary<EstadoEcc, RutasEcc>();
                foreach (var (estado, ruta) in rutas)
                {
                    int opciones = original == retransmitido ? 1 : 2;
                    for (int opcion = 0; opcion < opciones; opcion++)
                    {
                        bool usarRx = opcion == 1;
                        int valor = usarRx ? retransmitido : original;
                        if (valor < 0 ||
                            (i == 0 && Dem_v2.FormatSpecifier.Formato(valor) ==
                                Dem_v2.FormatSpecifier.ValorNoReconocido))
                            continue;

                        EstadoEcc nuevoEstado;
                        if (i == 0)
                        {
                            nuevoEstado = new EstadoEcc(valor, false, valor);
                        }
                        else if (i == 2)
                        {
                            bool duplicado = valor == estado.PrimerValor;
                            nuevoEstado = new EstadoEcc(
                                estado.PrimerValor, duplicado,
                                duplicado ? valor : estado.Xor ^ valor);
                        }
                        else
                        {
                            nuevoEstado = estado with { Xor = estado.Xor ^ valor };
                        }

                        var nodo = new NodoMensaje(ruta.Ejemplo, valor);
                        int seleccionesRx = ruta.SeleccionesRx + (usarRx ? 1 : 0);

                        if (EsFinDeSecuencia(valor))
                        {
                            finEncontrado = true;
                            int eccDx = i + 2 < caracteres.Count ? caracteres[i + 2] : -1;
                            int eccRx = i + 7 < caracteres.Count ? caracteres[i + 7] : -1;
                            if (eccDx < 0 && eccRx < 0)
                                continue;

                            eccDisponible = true;
                            combinacionesVerificadas += ruta.Cantidad;
                            int calculado = nuevoEstado.Xor & 0x7F;
                            if (CoincideEcc(calculado, eccDx, eccRx))
                            {
                                combinacionesValidas += ruta.Cantidad;
                                if (seleccionesRx < mejoresSeleccionesRx)
                                {
                                    mejorMensaje = nodo;
                                    mejoresSeleccionesRx = seleccionesRx;
                                    mejorEcc = calculado;
                                    mejorEccDx = eccDx;
                                    mejorEccRx = eccRx;
                                }
                            }

                            continue;
                        }

                        if (siguientes.TryGetValue(nuevoEstado, out RutasEcc? acumuladas))
                        {
                            acumuladas.Cantidad += ruta.Cantidad;
                            if (seleccionesRx < acumuladas.SeleccionesRx)
                            {
                                acumuladas.Ejemplo = nodo;
                                acumuladas.SeleccionesRx = seleccionesRx;
                            }
                        }
                        else
                        {
                            siguientes.Add(nuevoEstado,
                                new RutasEcc(ruta.Cantidad, nodo, seleccionesRx));
                        }
                    }
                }

                rutas = siguientes;
            }

            var resultado = new StringBuilder();
            resultado.AppendLine($"Lista desde formato: [{string.Join(", ", caracteres)}]");
            resultado.AppendLine($"Pares inspeccionados: {paresInspeccionados}; diferencias: {diferencias}");
            resultado.AppendLine($"Combinaciones verificadas con ECC: {combinacionesVerificadas}; válidas: {combinacionesValidas}");
            if (mejorMensaje != null)
            {
                _mensajeValidado = ReconstruirMensaje(mejorMensaje);
                resultado.AppendLine($"Mensaje válido: [{string.Join(", ", _mensajeValidado)}]");
                resultado.AppendLine($"Fin: {Dem_v2.General.ACK(_mensajeValidado[^1])}");
                resultado.AppendLine($"ECC calculado={mejorEcc}; recibido DX={mejorEccDx}, RX={mejorEccRx}");
            }
            else if (!finEncontrado)
                resultado.AppendLine("Fin de secuencia no encontrado; mensaje inválido.");
            else if (!eccDisponible)
                resultado.AppendLine("ECC no recibido; mensaje inválido.");
            else
                resultado.AppendLine("Ninguna combinación verifica el ECC; mensaje inválido.");

            ActualizarDisplay(resultado.ToString());
            return mejorMensaje != null;
        }

        private static bool EsFinDeSecuencia(int valor) =>
            Dem_v2.General.ACK(valor) != "¿?";

        private static List<int> ReconstruirMensaje(NodoMensaje ultimo)
        {
            var mensaje = new List<int>();
            for (NodoMensaje? nodo = ultimo; nodo != null; nodo = nodo.Anterior)
                mensaje.Add(nodo.Valor);
            mensaje.Reverse();
            return mensaje;
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

    public class Mensaje_2
    {
        public List<int> Mensaje_List { get; set; }
        public DateTime Fecha_recepcion { get; set; }
        public string ack { get; set; } = string.Empty;
        public List<int> data_respuesta { get; set; }
        public int Formato { get; set; }
        public bool extension { get; set; }
        public string categoria { get; set; } = string.Empty;
        public List<int>? Mensaje_ext { get; set; } // Acepta NULLs
        public string MMSI_RX { get; set; } = string.Empty;
        public int formato_rtx { get; set; }
        public int primer_telemando { get; set; }
    }
}
