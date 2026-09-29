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
        private readonly List<Mensaje_2> _historial = new();
        private readonly object _historialLock = new();

        public event Action<Mensaje_2>? MensajeAgregado;

        public IReadOnlyList<Mensaje_2> ObtenerHistorial()
        {
            lock (_historialLock)
                return _historial.ToArray();
        }

        private readonly record struct EstadoEcc(int PrimerValor, bool FormatoDuplicado, int Xor);

        private sealed record NodoMensaje(NodoMensaje? Anterior, int Valor);

        private sealed record ResultadoExtension(List<int>? Caracteres, string Diagnostico);

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
            int mejorIndiceFin = -1;
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
                                    mejorIndiceFin = i;
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

            Mensaje_2? mensajeParaHistorial = null;
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
                resultado.AppendLine();
                string textoDecodificado = TipoMensajes.Decodificar(_mensajeValidado);
                resultado.Append(textoDecodificado);
                mensajeParaHistorial = new Mensaje_2
                {
                    Mensaje_List = new List<int>(_mensajeValidado),
                    Fecha_recepcion = DateTime.Now,
                    Formato = _mensajeValidado[0],
                    categoria = CategoriaMensaje(_mensajeValidado),
                    ack = Dem_v2.General.ACK(_mensajeValidado[^1]),
                    TextoDecodificado = textoDecodificado
                };

                int inicioExtension = mejorIndiceFin + 8;
                if (HayExtension(caracteres, inicioExtension))
                {
                    ResultadoExtension extension = ValidarExtension(caracteres, inicioExtension);
                    resultado.AppendLine();
                    resultado.AppendLine(extension.Diagnostico);
                    mensajeParaHistorial.extension = true;
                    if (extension.Caracteres != null)
                    {
                        string textoExtension = Extension2.Decodificar(extension.Caracteres);
                        resultado.Append(textoExtension);
                        mensajeParaHistorial.Mensaje_ext = extension.Caracteres;
                        mensajeParaHistorial.TextoDecodificado +=
                            Environment.NewLine + textoExtension;
                    }
                    else
                    {
                        mensajeParaHistorial.TextoDecodificado +=
                            Environment.NewLine + extension.Diagnostico;
                    }
                }
            }
            else if (!finEncontrado)
                resultado.AppendLine("Fin de secuencia no encontrado; mensaje inválido.");
            else if (!eccDisponible)
                resultado.AppendLine("ECC no recibido; mensaje inválido.");
            else
                resultado.AppendLine("Ninguna combinación verifica el ECC; mensaje inválido.");

            ActualizarDisplay(resultado.ToString());
            if (mensajeParaHistorial != null)
            {
                lock (_historialLock)
                    _historial.Insert(0, mensajeParaHistorial);
                MensajeAgregado?.Invoke(mensajeParaHistorial);
            }
            return mejorMensaje != null;
        }

        private static string CategoriaMensaje(IReadOnlyList<int> mensaje)
        {
            if (mensaje[0] == 112)
                return Dem_v2.General.Categoria(112);

            int formatoRepetido = mensaje.Count > 1 && mensaje[1] == mensaje[0] ? 1 : 0;
            int indice = mensaje[0] switch
            {
                102 or 114 or 120 => 6 + formatoRepetido,
                116 => 1 + formatoRepetido,
                _ => -1
            };
            return indice >= 0 && indice < mensaje.Count - 1
                ? Dem_v2.General.Categoria(mensaje[indice])
                : "¿?";
        }

        private static bool EsFinDeSecuencia(int valor) =>
            Dem_v2.General.ACK(valor) != "¿?";

        private static bool EsEspecificadorExtension(int valor) => valor is >= 100 and <= 106;

        private static bool HayExtension(IReadOnlyList<int> caracteres, int inicio) =>
            inicio + 5 < caracteres.Count &&
            (EsEspecificadorExtension(caracteres[inicio]) ||
             EsEspecificadorExtension(caracteres[inicio + 5]));

        private static ResultadoExtension ValidarExtension(IReadOnlyList<int> caracteres, int inicio)
        {
            var rutas = new Dictionary<int, RutasEcc>
            {
                [0] = new RutasEcc(BigInteger.One, null, 0)
            };
            NodoMensaje? mejorMensaje = null;
            int mejoresSeleccionesRx = int.MaxValue;
            int mejorEcc = -1;
            int mejorEccDx = -1;
            int mejorEccRx = -1;
            int paresInspeccionados = 0;
            int diferencias = 0;
            BigInteger combinacionesVerificadas = BigInteger.Zero;
            BigInteger combinacionesValidas = BigInteger.Zero;
            bool finEncontrado = false;
            bool eccDisponible = false;

            // La extensión tiene un ECC propio: se aplica XOR a todos sus caracteres,
            // incluido su especificador y su carácter de fin.
            for (int i = inicio; i + 5 < caracteres.Count && rutas.Count > 0; i += 2)
            {
                int original = caracteres[i];
                int retransmitido = caracteres[i + 5];
                if (original != retransmitido)
                    diferencias++;
                paresInspeccionados++;

                var siguientes = new Dictionary<int, RutasEcc>();
                foreach (var (xor, ruta) in rutas)
                {
                    int opciones = original == retransmitido ? 1 : 2;
                    for (int opcion = 0; opcion < opciones; opcion++)
                    {
                        bool usarRx = opcion == 1;
                        int valor = usarRx ? retransmitido : original;
                        if (valor < 0 || (i == inicio && !EsEspecificadorExtension(valor)))
                            continue;

                        int nuevoXor = xor ^ valor;
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
                            int calculado = nuevoXor & 0x7F;
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

                        if (siguientes.TryGetValue(nuevoXor, out RutasEcc? acumuladas))
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
                            siguientes.Add(nuevoXor,
                                new RutasEcc(ruta.Cantidad, nodo, seleccionesRx));
                        }
                    }
                }
                rutas = siguientes;
            }

            string resumen = $"Extensión: pares inspeccionados={paresInspeccionados}; diferencias={diferencias}; " +
                $"combinaciones verificadas con ECC={combinacionesVerificadas}; válidas={combinacionesValidas}.";
            if (mejorMensaje != null)
            {
                List<int> validada = ReconstruirMensaje(mejorMensaje);
                return new ResultadoExtension(validada,
                    $"{resumen}{Environment.NewLine}ECC de extensión correcto: " +
                    $"calculado={mejorEcc}; recibido DX={mejorEccDx}, RX={mejorEccRx}.{Environment.NewLine}" +
                    $"Caracteres de extensión: [{string.Join(", ", validada)}]");
            }

            string motivo = !finEncontrado ? "fin no encontrado" :
                !eccDisponible ? "ECC no recibido" : "ninguna combinación verifica el ECC";
            return new ResultadoExtension(null, $"{resumen} Extensión inválida: {motivo}.");
        }

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
        public List<int> Mensaje_List { get; set; } = new();
        public DateTime Fecha_recepcion { get; set; }
        public string ack { get; set; } = string.Empty;
        public string TextoDecodificado { get; set; } = string.Empty;
        public int Formato { get; set; }
        public bool extension { get; set; }
        public string categoria { get; set; } = string.Empty;
        public List<int>? Mensaje_ext { get; set; } // Acepta NULLs
        public string MMSI_RX { get; set; } = string.Empty;
        public int formato_rtx { get; set; }
    }
}
