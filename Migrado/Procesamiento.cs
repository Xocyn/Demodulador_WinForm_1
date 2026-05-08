using MathNet.Numerics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Dem_v2
{
    /// <summary>
    /// Clase de procesamiento que maneja la decodificación de mensajes DSC.
    /// THREAD-SAFE: Recibe una referencia al control RichTextBox del formulario
    /// y usa Invoke() para escribir en UI desde threads diferentes.
    /// </summary>
    public class Procesamiento
    {
        private readonly RichTextBox _mainDisplay;
        private readonly Metodos _metodos;
        private readonly Expansion _expansion;

        public Procesamiento(RichTextBox mainDisplay)
        {
            _mainDisplay = mainDisplay;
            _metodos = new Metodos(LogToDisplay);
            _expansion = new Expansion(LogToDisplay);
        }

        /// <summary>
        /// Método helper que escribe en el MAINDISPLAY de forma thread-safe.
        /// Detecta si estamos en el thread de UI y usa Invoke() si es necesario.
        /// </summary>
        private void LogToDisplay(string message)
        {
            if (_mainDisplay?.InvokeRequired == true)
            {
                _mainDisplay.Invoke(() => _mainDisplay.AppendText(message));
            }
            else if (_mainDisplay != null)
            {
                _mainDisplay.AppendText(message);
            }
        }

        private void ClearDisplay()
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

        /// <summary>
        /// Muestra un menú de confirmación para responder a un mensaje de socorro
        /// </summary>
        private bool MostrarMenuSocorro()
        {
            // En WinForms, usamos MessageBox en lugar de Console
            DialogResult result = MessageBox.Show(
                "¿Desea responder el mensaje de S.O.S?",
                "ALERTA DE SOCORRO",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            return result == DialogResult.Yes;
        }

        /// <summary>
        /// Procesa una cadena de bits decodificada en un mensaje DSC.
        /// Este es el método principal que ejecuta toda la lógica de decodificación.
        /// </summary>
        public void Procesar(string input, bool ext)
        {
            try
            {
                List<(int Index, int Value)> encontrados = new List<(int, int)>();
                int i = 0;
                bool sincronizado = false;

                // ── Fase 1: Búsqueda de Phasing ──────────────────────────────────────
                while (!sincronizado)
                {
                    if (i + 10 > input.Length) break;

                    string ventana = input.Substring(i, 10);
                    int mensajeInt = Convert.ToInt32(ventana, 2);

                    if (Decodificador.TryDecodificarMensaje(mensajeInt, out int valor))
                    {
                        if (PhasingSequence.TryCaracter(valor))
                        {
                            encontrados.Add((i, valor));
                            i += 10;

                            if (encontrados.Count >= 3 &&
                                PhasingSequence.TryDetect(encontrados, out var pattern))
                            {
                                sincronizado = true;
                            }
                        }
                        else
                            i += 1;
                    }
                    else
                        i += 1;
                }

                // ── Fase 2: Format specifier ─────────────────────────────────────────
                bool formatConfirmed = false;
                bool dxrxConfirmed = false;
                int form = 0;

                while (sincronizado && !formatConfirmed)
                {
                    if (i + 10 > input.Length) break;

                    string ventana = input.Substring(i, 10);
                    int mensajeInt = Convert.ToInt32(ventana, 2);
                    Decodificador.TryDecodificarMensaje(mensajeInt, out int valor);

                    form = FormatSpecifier.Filtro2(valor, out int j);

                    bool esBroadcast = (form == 112 || form == 116);
                    dxrxConfirmed = esBroadcast || Decodificador.DxRx(input, i);

                    i += 10;

                    if (j == 1 && dxrxConfirmed)
                    {
                        formatConfirmed = true;
                    }
                }

                i -= 10; // Retroceder para que el switch lea el format specifier

                // ── Fase 3: Extracción de Mensaje ──────────────────────────────────
                Decodificador.Mensaje(input, i, out List<int> MESSAGE);

                List<int> MENSAJE = MESSAGE.ToList();
                List<int> ECC = MESSAGE.ToList();
                List<int> datos_respuesta = new List<int>();
                List<int> MENSAJE_EXT = new();
                List<int> ECC_EXT = new();
                bool extension = false; bool ecc_ext = false;


                // ── Fase 4: Extension?  -────────-──────────────────────────────────

                if (Extension(MENSAJE)) // CON EXTENSION
                {
                    int firstindex127 = MENSAJE.IndexOf(127); int firstindex117 = MENSAJE.IndexOf(117); int firstindex122 = MENSAJE.IndexOf(122);
                    /*
                     * Me faltan considerar los casos en los que el EOS no sea 127
                     * tendría que desarrollar un metodo en el que le pase la lista y en caso de cualquier caracter EOS
                     * me devuelva directamente el int del index
                    */
                    List<int> MENSAJE_OG = MENSAJE.GetRange(0, firstindex127 + 8);
                    MENSAJE_EXT = MENSAJE.GetRange(firstindex127 + 8, MENSAJE.Count - (firstindex127 + 8));
                    string m1 = string.Join(" ", MENSAJE_OG.Select(x => x.ToString("D2")));
                    string m2 = string.Join(" ", MENSAJE_EXT.Select(x => x.ToString("D2")));
                    LogToDisplay($"MENSAJE: {m1}\n");
                    LogToDisplay($"MENSAJE DE EXTENSION: {m2}\n");

                    MENSAJE = MENSAJE_OG;
                    ECC = MENSAJE.ToList();
                    Geografica.EliminarPosicionesImpares(ECC); // Obtengo los DX
                    ECC = PrepararECC(ECC);
                    if (VerificarECC(MENSAJE, ECC))
                    {
                        LogToDisplay("✓ ECC correcto\n");
                    }
                    else
                    {
                        LogToDisplay("✗ Error en ECC\n");
                        return;
                    }

                    /*
                     * no anda bien el Preparar ECC para la extension
                     * revisar mañana
                     * zzzzzzz
                    */
                    ECC_EXT = MENSAJE_EXT.ToList();
                    Geografica.EliminarPosicionesImpares(ECC_EXT);
                    ECC_EXT = PrepararECC_EXT(ECC_EXT);

                    if (VerificarECC(MENSAJE_EXT, ECC_EXT))
                    {
                        LogToDisplay("✓ ECC extension correcto\n");
                        ecc_ext = true;
                    }
                    else
                    {
                        LogToDisplay("✗ Error en ECC extension\n");
                    }

                    extension = true;

                }
                else // SIN EXTENSION
                {
                    Geografica.EliminarPosicionesImpares(ECC); // Obtengo los DX
                    ECC = PrepararECC(ECC);

                    string mensaje_string = string.Join(" ", MENSAJE.Select(x => x.ToString("D2")));
                    LogToDisplay($"MENSAJE: {mensaje_string}\n");

                    if (VerificarECC(MENSAJE, ECC))
                    {
                        LogToDisplay("✓ ECC correcto\n");
                    }
                    else
                    {
                        LogToDisplay("✗ Error en ECC\n");
                        return;
                    }
                }

                // ── Fase 5: Procesamiento según formato del mensaje ────────────────
                LogToDisplay("\n");
                switch (MENSAJE[0])
                {
                    case 102:
                        _metodos.MGeografica(MENSAJE);
                        break;
                    case 112:
                        datos_respuesta = _metodos.MSocorro(MENSAJE);

                        //if (MostrarMenuSocorro())
                        //{
                        //    // TODO: Implementar respuesta automática
                        //    // Respuesta.RespuestaSocorro(datos_respuesta);
                        //    LogToDisplay("Preparando respuesta de socorro...\n");
                        //}
                        break;
                    case 114:
                        _metodos.MGrupos(MENSAJE);
                        break;
                    case 116:
                        _metodos.MAllShips(MENSAJE);
                        break;
                    case 120:
                        _metodos.MIndividual(MENSAJE);
                        break;
                    case 123:
                        LogToDisplay("Formato 123 detectado (no implementado)\n");
                        break;
                    default:
                        LogToDisplay($"Formato desconocido: {MENSAJE[0]}\n");
                        break;
                }

                LogToDisplay("\n");

                // ── Fase 6: Procesamiento extension ────────────────────────────────

                if (extension && ecc_ext)
                {
                    _expansion.Decodificar(MENSAJE_EXT);
                }
            }
            catch (Exception ex)
            {
                LogToDisplay($"❌ Error en Procesar: {ex.Message}\n");
            }
        }

        public bool VerificarECC(List<int> MESSAGE, List<int> ECC)
        {
            if (MESSAGE.Count < 6)
            {
                LogToDisplay("MENSAJE demasiado corto para leer ECC.\n");
                return false;
            }

            int eccDx = MESSAGE[MESSAGE.Count - 6];
            int eccRx = MESSAGE[MESSAGE.Count - 1];

            int calculated = 0;
            foreach (int v in ECC)
                calculated ^= v;
            calculated &= 0x7F;

            if (calculated == eccDx || calculated == eccRx)
                return true;
            else
                return false;
        }

        public List<int> PrepararECC(List<int> list)
        {
            if (list.Count < 4)
            {
                return new List<int>();
            }

            List<int> ecc = list
                .Skip(1)
                .Take(list.Count - 4)
                .ToList();

            return ecc;
        }

        public List<int> PrepararECC_EXT(List<int> list)
        {
            List<int> ecc = list
                .Take(list.Count - 3)
                .ToList();
            return ecc;

        }

        public static bool Extension(List<int> lista)
        {
            foreach (int valor in lista.Distinct())
            {
                int first127 = lista.IndexOf(127); int last127 = lista.LastIndexOf(127);
                int first122 = lista.IndexOf(122); int last122 = lista.LastIndexOf(122);
                int first117 = lista.IndexOf(117); int last117 = lista.LastIndexOf(117);

                if ((last127 - first127) > 10 || (last122 - first122) > 10 || (last117 - first117) > 10)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Clase que contiene los métodos específicos para cada tipo de mensaje DSC.
    /// Recibe un delegate para logging que es thread-safe.
    /// </summary>
    public class Metodos
    {
        private readonly Action<string> _log;

        public Metodos(Action<string> logAction)
        {
            _log = logAction;
        }

        // ── GEOGRAFICA─────────────────────────────────────────────────────
        public void MGeografica(List<int> mensaje)
        {
            string mmsi = string.Empty;
            string area = string.Empty;
            string categoria = string.Empty;
            string primer_tel = string.Empty, segundo_tel = string.Empty;
            string frec_canal_1 = string.Empty, frec_canal_2 = string.Empty;
            bool ocho = false, ocho2 = false;
            bool canal = false, canal2 = false;
            string ack = string.Empty;
            string mmsi_socorro = string.Empty;
            string tipoEmergencia = string.Empty;
            string coordenadas = string.Empty;
            string utc = string.Empty;
            string sig_comunicaciones = string.Empty;

            area = Geografica.Area(mensaje, 4);
            categoria = General.Categoria(mensaje[14]);
            mmsi = General.newMMSI(mensaje, 16);
            primer_tel = General.PrimerTelemando(mensaje[26], out bool sol_posicion);

            if (mensaje[14] == 112)
            {
                mmsi_socorro = General.newMMSI(mensaje, 28);
                tipoEmergencia = Socorro.Peligro(mensaje[38]);
                coordenadas = Geografica.Posicion(Geografica.Coordenadas(mensaje, 40).Item1);
                if (Geografica.Coordenadas(mensaje, 40).Item2)
                {
                    utc = Geografica.newUTC(mensaje, 50);
                }
                else
                {
                    utc = "88:88";
                }
                sig_comunicaciones = Socorro.PosteriorCom(mensaje[54]);
                ack = General.ACK(mensaje[56]);
            }
            else
            {
                segundo_tel = General.SegundoTelemando(mensaje[28]);
                (frec_canal_1, ocho, canal) = General.FrecuenciaCanal(mensaje, 30, out bool _);
                if (ocho)
                {
                    (frec_canal_2, ocho2, canal2) = General.FrecuenciaCanal(mensaje, 38, out bool _);
                    ack = General.ACK(mensaje[44]);
                }
                else
                {
                    (frec_canal_2, ocho2, canal2) = General.FrecuenciaCanal(mensaje, 36, out bool _);
                    ack = General.ACK(mensaje[42]);
                }
            }

            _log($"Formato: {FormatSpecifier.Formato(mensaje[0])}\n");
            _log($"Área Geográfica: {area}\n");
            _log($"Categoría: {categoria}\n");
            _log($"MMSI: {mmsi}\n");
            _log($"Primer Telemando: {primer_tel}\n");

            if (mensaje[14] == 112)
            {
                _log($"MMSI Socorro: {mmsi_socorro}\n");
                _log($"Tipo de emergencia: {tipoEmergencia}\n");
                _log($"Coordenadas: {coordenadas}\n");
                _log($"UTC: {utc}\n");
                _log($"Siguiente Comunicación: {sig_comunicaciones}\n");
                _log($"{ack}\n");
            }
            else
            {
                _log($"Segundo Telemando: {segundo_tel}\n");
                if (canal)
                    _log($"Canal Rx: {frec_canal_1}\n");
                else
                    _log($"Frecuencia Rx: {frec_canal_1}\n");
                if (canal2)
                    _log($"Canal Tx: {frec_canal_2}\n");
                else
                    _log($"Frecuencia Tx: {frec_canal_2}\n");
                _log($"ACK: {ack}\n");
            }
        }

        // ── INDIVIDUAL────────────────────────────────────────────────────
        public void MIndividual(List<int> mensaje)
        {
            int format = mensaje[0];
            string mmsi_receptor, mmsi_transmisor = string.Empty;
            string categoria = string.Empty;
            string primer_tel, segundo_tel = string.Empty;
            string frec_canal_1 = string.Empty, frec_canal_2 = string.Empty;
            bool ocho = false, ocho2 = false;
            bool canal = false, canal2 = false;
            List<int> posicion = new List<int>();
            string utc = string.Empty;
            string posicion_string = string.Empty;
            string ack = string.Empty;
            bool Posicion_2 = false;
            string mmsi_socorro = string.Empty;
            string tipoEmergencia = string.Empty;
            string coordenadas = string.Empty;
            string sig_comunicaciones = string.Empty;

            mmsi_receptor = General.newMMSI(mensaje, 4);
            categoria = General.Categoria(mensaje[14]);
            mmsi_transmisor = General.newMMSI(mensaje, 16);
            primer_tel = General.PrimerTelemando(mensaje[26], out bool sol_posicion);

            if (mensaje[14] == 112)
            {
                mmsi_socorro = General.newMMSI(mensaje, 28);
                tipoEmergencia = Socorro.Peligro(mensaje[38]);
                coordenadas = Geografica.Posicion(Geografica.Coordenadas(mensaje, 40).Item1);
                if (Geografica.Coordenadas(mensaje, 40).Item2)
                {
                    utc = Geografica.newUTC(mensaje, 50);
                }
                else
                {
                    utc = "88:88";
                }
                sig_comunicaciones = Socorro.PosteriorCom(mensaje[54]);
                ack = General.ACK(mensaje[56]);
            }
            else
            {
                segundo_tel = General.SegundoTelemando(mensaje[28]);
                if (sol_posicion)
                {
                    for (int k = 30; k < 44; k += 1)
                    {
                        if (k % 2 == 0)
                        {
                            posicion.Add(mensaje[k]);
                        }
                    }
                    utc = Geografica.newUTC(mensaje, 42);
                    ack = General.ACK(mensaje[46]);
                }
                else
                {
                    (frec_canal_1, ocho, canal) = General.FrecuenciaCanal(mensaje, 30, out bool pos2);
                    if (ocho)
                    {
                        (frec_canal_2, ocho2, canal2) = General.FrecuenciaCanal(mensaje, 38, out bool _);
                        ack = General.ACK(mensaje[44]);
                    }
                    else if (pos2)
                    {
                        ack = General.ACK(mensaje[42]);
                        Posicion_2 = true;
                    }
                    else
                    {
                        (frec_canal_2, ocho2, canal2) = General.FrecuenciaCanal(mensaje, 36, out bool _);
                        ack = General.ACK(mensaje[42]);
                    }
                }
            }

            _log($"Formato: {FormatSpecifier.Formato(format)}\n");
            _log($"MMSI Receptor: {mmsi_receptor}\n");
            _log($"Categoría: {categoria}\n");
            _log($"MMSI Transmisor: {mmsi_transmisor}\n");
            _log($"Primer Telemando: {primer_tel}\n");

            if (mensaje[14] == 112)
            {
                _log($"MMSI Socorro: {mmsi_socorro}\n");
                _log($"Tipo de emergencia: {tipoEmergencia}\n");
                _log($"Coordenadas: {coordenadas}\n");
                _log($"UTC: {utc}\n");
                _log($"Siguiente Comunicación: {sig_comunicaciones}\n");
                _log($"{ack}\n");
            }
            else
            {
                _log($"Segundo Telemando: {segundo_tel}\n");
                if (posicion.Count > 0)
                {
                    if (posicion[posicion.Count - 1] == 117)
                    {
                        posicion_string = "Solicitud de posición";
                        _log($"Posición: {posicion_string}\n");
                    }
                    else
                    {
                        posicion_string = Geografica.Posicion(posicion);
                        _log($"Posición: {posicion_string}\n");
                        _log($"UTC: {utc}\n");
                    }
                }
                else if (Posicion_2)
                {
                    _log($"Posición: {frec_canal_1}\n");
                }
                else
                {
                    if (canal)
                        _log($"Canal Rx: {frec_canal_1}\n");
                    else
                        _log($"Frecuencia Rx: {frec_canal_1}\n");
                    if (canal2)
                        _log($"Canal Tx: {frec_canal_2}\n");
                    else
                        _log($"Frecuencia Tx: {frec_canal_2}\n");
                }
                _log($"{ack}\n");
            }
        }

        // ── SOCORRO ─────────────────────────────────────────────────────
        public List<int> MSocorro(List<int> mensaje)
        {
            int format = 0;
            string mmsi = string.Empty;
            string tipoEmergencia = string.Empty;
            List<int> coords = new List<int>();
            bool sigoutc = false;
            string utc = string.Empty;
            int sig_comunicaciones = 0;
            string ack = string.Empty;

            if (mensaje[0] == mensaje[2])
                format = mensaje[0];
            else
                return new List<int>();

            mmsi = General.newMMSI(mensaje, 4);
            tipoEmergencia = Socorro.Peligro(mensaje[14]);

            (coords, sigoutc) = Geografica.Coordenadas(mensaje, 16);

            if (sigoutc)
            {
                utc = Geografica.newUTC(mensaje, 26);
            }
            else
            {
                utc = "88:88";
            }

            sig_comunicaciones = mensaje[30];
            ack = General.ACK(mensaje[32]);

            _log($"Formato: {FormatSpecifier.Formato(format)}\n");
            _log($"MMSI: {mmsi}\n");
            _log($"Tipo de Emergencia: {tipoEmergencia}\n");
            _log($"Coordenadas: {Geografica.Posicion(coords)}\n");
            _log($"UTC: {utc}\n");
            _log($"Siguiente Comunicación: {Socorro.PosteriorCom(sig_comunicaciones)}\n");
            _log($"{ack}\n");

            List<int> respuesta = mensaje.GetRange(4, 28);
            return respuesta;
        }

        // ── GRUPOS ─────────────────────────────────────────────────────
        public void MGrupos(List<int> mensaje)
        {
            int format = mensaje[0];
            string mmsi = string.Empty;
            string categoria = string.Empty;
            string mmsi_tx = string.Empty;
            string primer_tel = string.Empty;
            string segundo_tel = string.Empty;
            string frec_canal_1 = string.Empty;
            string ack = string.Empty;
            string mmsi_socorro = string.Empty;
            string tipoEmergencia = string.Empty;
            string coordenadas = string.Empty;
            string utc = string.Empty;
            string sig_comunicaciones = string.Empty;

            mmsi = General.newMMSI(mensaje, 4);
            categoria = General.Categoria(mensaje[14]);
            mmsi_tx = General.newMMSI(mensaje, 16);
            primer_tel = General.PrimerTelemando(mensaje[26], out bool sol_posicion);

            if (mensaje[14] == 112)
            {
                mmsi_socorro = General.newMMSI(mensaje, 28);
                tipoEmergencia = Socorro.Peligro(mensaje[38]);
                coordenadas = Geografica.Posicion(Geografica.Coordenadas(mensaje, 40).Item1);
                if (Geografica.Coordenadas(mensaje, 40).Item2)
                {
                    utc = Geografica.newUTC(mensaje, 50);
                }
                else
                {
                    utc = "88:88";
                }
                sig_comunicaciones = Socorro.PosteriorCom(mensaje[54]);
                ack = General.ACK(mensaje[56]);
            }
            else
            {
                segundo_tel = General.SegundoTelemando(mensaje[28]);
                frec_canal_1 = General.FrecuenciaCanal(mensaje, 30, out bool _).Item1;
                ack = General.ACK(mensaje[42]);
            }


            _log($"Formato: {FormatSpecifier.Formato(format)}\n");
            _log($"MMSI: {mmsi}\n");
            _log($"Categoría: {categoria}\n");
            _log($"MMSI Transmisor: {mmsi_tx}\n");
            _log($"Primer Telemando: {primer_tel}\n");

            if (mensaje[14] == 112)
            {
                _log($"MMSI Socorro: {mmsi_socorro}\n");
                _log($"Tipo de emergencia: {tipoEmergencia}\n");
                _log($"Coordenadas: {coordenadas}\n");
                _log($"UTC: {utc}\n");
                _log($"Siguiente Comunicación: {sig_comunicaciones}\n");
                _log($"{ack}\n");
            }
            else
            {
                _log($"Segundo Telemando: {segundo_tel}\n");
                _log($"Frecuencia: {frec_canal_1}\n");
                _log($"{ack}\n");
            }
        }

        // ── ALL SHIPS─────────────────────────────────────────────────────
        public void MAllShips(List<int> mensaje)
        {
            int format = mensaje[0];
            string mmsi = string.Empty;
            string categoria = string.Empty;
            string primer_tel = string.Empty;
            string segundo_tel = string.Empty;
            string frec_canal_1 = string.Empty;
            string frec_canal_2 = string.Empty;
            bool ocho = false; bool ocho2 = false;
            bool canal = false; bool canal2 = false;
            string ack = string.Empty;
            string mmsi_socorro = string.Empty;
            string tipoEmergencia = string.Empty;
            string coordenadas = string.Empty;
            string utc = string.Empty;
            string sig_comunicaciones = string.Empty;

            categoria = General.Categoria(mensaje[4]);
            mmsi = General.newMMSI(mensaje, 6);
            primer_tel = General.PrimerTelemando(mensaje[16], out bool sol_posicion);

            if (mensaje[4] == 112)
            {
                mmsi_socorro = General.newMMSI(mensaje, 18);
                tipoEmergencia = Socorro.Peligro(mensaje[28]);
                coordenadas = Geografica.Posicion(Geografica.Coordenadas(mensaje, 30).Item1);
                if (Geografica.Coordenadas(mensaje, 30).Item2)
                {
                    utc = Geografica.newUTC(mensaje, 40);
                }
                else
                {
                    utc = "88:88";
                }
                sig_comunicaciones = Socorro.PosteriorCom(mensaje[44]);
                ack = General.ACK(mensaje[46]);
            }
            else
            {
                segundo_tel = General.SegundoTelemando(mensaje[18]);
                (frec_canal_1, ocho, canal) = General.FrecuenciaCanal(mensaje, 20, out bool _);
                if (ocho)
                {
                    (frec_canal_2, ocho2, canal2) = General.FrecuenciaCanal(mensaje, 28, out bool _);
                    ack = General.ACK(mensaje[34]);
                }
                else
                {
                    (frec_canal_2, ocho2, canal2) = General.FrecuenciaCanal(mensaje, 26, out bool _);
                    ack = General.ACK(mensaje[32]);
                }
            }


            _log($"Formato: {FormatSpecifier.Formato(format)}\n");
            _log($"MMSI: {mmsi}\n");
            _log($"Categoría: {categoria}\n");
            _log($"Primer Telemando: {primer_tel}\n");

            if (mensaje[4] == 112)
            {
                _log($"MMSI Socorro: {mmsi_socorro}\n");
                _log($"Tipo de emergencia: {tipoEmergencia}\n");
                _log($"Coordenadas: {coordenadas}\n");
                _log($"UTC: {utc}\n");
                _log($"Siguiente Comunicación: {sig_comunicaciones}\n");
                _log($"{ack}\n");
            }
            else
            {
                _log($"Segundo Telemando: {segundo_tel}\n");
                if (canal)
                    _log($"Canal Rx: {frec_canal_1}\n");
                else
                    _log($"Frecuencia Rx: {frec_canal_1}\n");
                if (canal2)
                    _log($"Canal Tx: {frec_canal_2}\n");
                else
                    _log($"Frecuencia Tx: {frec_canal_2}\n");
                _log($"{ack}\n");
            }
        }
    }

    public class Expansion
    {
        private readonly Action<string> _log;

        public Expansion(Action<string> logCallback)
        {
            _log = logCallback ?? throw new ArgumentNullException(nameof(logCallback));
        }

        public void Decodificar(List<int> EXTENSION)
        {
            int i = 0;
            string eos = string.Empty;

            _log("\n");
            Geografica.EliminarPosicionesImpares(EXTENSION);
            switch (EXTENSION[i])
            {
                case 100:
                    //  Resolusion mejorada de la posicion
                    i++;
                    i = res_mejorada(EXTENSION, i);
                    break;
                case 101:
                    // Origen y punto de referencia de posicion 
                    i++;
                    i = origen_punto_ref(EXTENSION, i);
                    break;
                case 102:
                    // Velocidad actual del barco
                    i++;
                    i = velocidad_actual(EXTENSION, i);
                    break;
                case 103:
                    // Ruta actual del barco
                    i++;
                    i = ruta_actual(EXTENSION, i);
                    break;
                case 104:
                    // Identificador adicional de la estacion
                    i++;
                    i = identificador_adicional(EXTENSION, i);
                    break;
                case 105:
                    // Zona geofrafica ampliada
                    i++;
                    i = zona_geografica_ampliada(EXTENSION, i);
                    break;
                case 106:
                    // Numero de personas a bordo
                    i++;
                    i = numero_personas_a_bordo(EXTENSION, i);
                    break;
                default:
                    // No identificado
                    _log("Caracter no identificado\n");
                    return;

            }

            // leer el caracter y si es EOS se va al final y sino es el Mensaje 2
            int valor2 = EXTENSION[i];

            if (valor2 == 127 || valor2 == 122 || valor2 == 117)
            {
                // EOS
                eos = General.ACK(valor2);
                _log(eos);
                return;
            }
            else
            {
                switch (valor2)
                {
                    case 100:
                        //  Resolusion mejorada de la posicion
                        i++;
                        i = res_mejorada(EXTENSION, i);
                        break;
                    case 101:
                        // Origen y punto de referencia de posicion 
                        i++;
                        i = origen_punto_ref(EXTENSION, i);
                        break;
                    case 102:
                        // Velocidad actual del barco
                        i++;
                        i = velocidad_actual(EXTENSION, i);
                        break;
                    case 103:
                        // Ruta actual del barco
                        i++;
                        i = ruta_actual(EXTENSION, i);
                        break;
                    case 104:
                        // Identificador adicional de la estacion
                        i++;
                        i = identificador_adicional(EXTENSION, i);
                        break;
                    case 105:
                        // Zona geofrafica ampliada
                        i++;
                        i = zona_geografica_ampliada(EXTENSION, i);
                        break;
                    case 106:
                        // Numero de personas a bordo
                        i++;
                        i = numero_personas_a_bordo(EXTENSION, i);
                        break;
                    default:
                        // No identificado
                        _log("Caracter no identificado\n");
                        return;
                }
            }

            int valor3 = EXTENSION[i];
            if (valor3 == 127 || valor3 == 122 || valor3 == 117)
            {
                // EOS
                eos = General.ACK(valor3);
                _log(eos);
                return;
            }

            return;
        }

        private int res_mejorada(List<int> EXT, int i)
        {
            // ACA ME FALTA SABER SI ES PETICION O SI NO HAY DATOS 
            // PUEDE SER 1 SOLO CARACTER 
            // o 4 CARACTERES

            if (EXT[i] == 110)
            {
                _log("Peticion de datos\n");
                return i + 1;

            }
            else if (EXT[i] == 126)
            {
                _log("Ningun dato disponible\n");
                return i + 1;
            }

            List<int> res= EXT.GetRange(i, 4);

            List<string> res_I = res
            .Select(x => x.ToString("D2"))
            .ToList();

            List<int> res_D = General.SplitDigits2(res_I);

            _log($"Mejora de Latitud {res_D[0]}{res_D[1]}{res_D[2]}{res_D[3]}'' \n");
            _log($"Mejora de Longitud {res_D[4]}{res_D[5]}{res_D[6]}{res_D[7]}'' \n");

            return i + 4;
        }

        private int origen_punto_ref(List<int> EXT, int i)
        {
            // Se leen 3 caracteres (30 bits) y se decodifican a un origen y punto de referencia de posicion

            if (EXT[i] == 110)
            {
                _log("Peticion de datos\n");
                return i + 1;
            }
            else if (EXT[i] == 126)
            {
                _log("Ningun dato disponible\n");
                return i + 1;
            }

            string dispositivo = string.Empty;
            string punto_ref = string.Empty;


            switch (EXT[i])
            {
                case 0:
                    dispositivo = "NO VALIDO";
                    break;
                case 1:
                    dispositivo = "GPS diferencial";
                    break;
                case 2:
                    dispositivo = "GPS sin corregir";
                    break;
                case 3:
                    dispositivo = "LORAN-C diferencial";
                    break;
                case 4:
                    dispositivo = "LORAN-C sin corregir";
                    break;
                case 5:
                    dispositivo = "GLONASS";
                    break;
                case 6:
                    dispositivo = "PUNTO DE REFERENCIA DE RADAR";
                    break;
                case 7:
                    dispositivo = "DECCA";
                    break;
                case 8:
                    dispositivo = "OTRA REFERENCIA";
                    break;
                default:
                    dispositivo = "¿¿??";
                    break;
            }

            int valor = EXT[i+1];

            List<int> presicion = valor
                .ToString()
                .Select(c => int.Parse(c.ToString()))
                .ToList();

            switch (EXT[i+2])
            {
                case 0:
                    punto_ref = "WGS-84";
                    break;
                case 1:
                    punto_ref = "WGS-72";
                    break;
                case 2:
                    punto_ref = "OTRO";
                    break;
                default:
                    punto_ref = "¿¿??";
                    break;
            }

            _log($"Dato de posicion procedentes de: {dispositivo}\n");
            _log($"Presicion del punto de referencia: {presicion[0]},{presicion[1]}\n");
            _log($"Punto de referencia: {punto_ref}\n");
            return i + 3;
        }

        private int velocidad_actual(List<int> EXT, int i)
        {
            // Se leen 2 caracteres (20 bits) y se decodifican a la velocidad actual del barco

            if (EXT[i] == 110)
            {
                _log("Peticion de datos\n");
                return i + 1;
            }
            else if (EXT[i] == 126)
            {
                _log("Ningun dato disponible\n");
                return i + 1;
            }

            List<int> vel = EXT.GetRange(i, 2);

            List<string> vel_I = vel
            .Select(x => x.ToString("D2"))
            .ToList();

            List<int> vel_d= General.SplitDigits2(vel_I);


            _log($"Velocidad actual del barco: {vel_d[0]}{vel_d[1]}{vel_d[2]},{vel_d[3]} nudos\n");
            return i + 2;
        }

        private int ruta_actual(List<int> EXT, int i)
        {
            // Se leen 2 caracteres (20 bits) y se decodifica la ruta actual del barco

            if (EXT[i] == 110)
            {
                _log("Peticion de datos\n");
                return i + 1;
            }
            else if (EXT[i] == 126)
            {
                _log("Ningun dato disponible\n");
                return i + 1;
            }

            List<int> ruta = EXT.GetRange(i, 2);

            List<string> ruta_I = ruta
            .Select(x => x.ToString("D2"))
            .ToList();

            List<int> ruta_d = General.SplitDigits2(ruta_I);

            _log($"Ruta actual del barco: {ruta_d[0]}{ruta_d[1]}{ruta_d[2]},{ruta_d[3]} grados\n");
            return i + 2;
        }

        private int identificador_adicional(List<int> EXT, int i)
        {
            // Se leen 10 caracteres (100 bits) y se decodifica un identificador adicional de la estacion
            if (EXT[i] == 110)
            {
                _log("Peticion de datos\n");
                return i + 1;
            }
            else if (EXT[i] == 126)
            {
                _log("Ningun dato disponible\n");
                return i + 1;
            }

            List<int> id = EXT.GetRange(i, 10);
            var new_id = new List<string>();

            foreach (int i2 in id)
            {
                new_id.Add(Caracter(i2));
            }

            _log($"Identificador adicional: {string.Join("", new_id)}\n");

            return i + 10;
        }

        private int zona_geografica_ampliada(List<int> EXT, int i)
        {
            // se leen 12 caracteres (120 bits)
            List<int> EXT_2 = EXT.GetRange(i, 12);

            List<string> zona_i = EXT_2
            .Select(x => x.ToString("D2"))
            .ToList();

            // PUEDEN EXISTIR CARACTERES 126
            // SI LEE 126 OCUPA MAS DE 2 DIGITOS

            List<int> zona_d = General.SplitDigits2(zona_i);

            _log($"Mejora de Latitud: ,{zona_d[0]}{zona_d[1]}{zona_d[2]}{zona_d[3]}'' \n");
            _log($"Mejora de Longitud: ,{zona_d[4]}{zona_d[5]}{zona_d[6]}{zona_d[7]}'' \n");

            _log($"Resolucion adicional ventana vertical: {zona_d[8]}{zona_d[9]}{zona_d[10]}{zona_d[11]}\n");
            _log($"Resolucion adicional ventana horizontal: {zona_d[12]}{zona_d[13]}{zona_d[14]}{zona_d[15]}\n");

            if (zona_d[8] == 126 || zona_d[9] == 126)
            {
                _log("No se dispone estimacion de velocidad\n");
            }
            else
            {
                _log($"Velocidad actual del barco: {zona_d[16]}{zona_d[17]}{zona_d[18]},{zona_d[19]} nudos\n");
            }

            if (zona_d[10] == 126 || zona_d[11] == 126)
            {
                _log("No se dispone estimacion de trayectoria\n");
            }
            else
            {
                _log($"Trayectoria actual del barco: {zona_d[20]}{zona_d[21]}{zona_d[22]},{zona_d[23]} grados\n");
            }

            return i + 12;
        }

        private int numero_personas_a_bordo(List<int> EXT, int i)
        {
            // Se leen 2 caracteres (20 bits) y se decodifica el numero de personas a bordo
            if (EXT[i] == 110)
            {
                _log("Peticion de datos\n");
                return i + 1;
            }
            else if (EXT[i] == 126)
            {
                _log("Ningun dato disponible\n");
                return i + 1;
            }

            List<int> personas = EXT.GetRange(i, 2);

            string ppol = string.Join("", personas.Select(x => x.ToString("D2")));
            _log($"Numero de personas a bordo: {ppol}\n");

            return i + 2;
        }

        private static string Caracter(int h)
        {
            return h switch
            {
                0 => "0",
                1 => "1",
                2 => "2",
                3 => "3",
                4 => "4",
                5 => "5",
                6 => "6",
                7 => "7",
                8 => "8",
                9 => "9",
                10 => "Sin utilizar",
                11 => "A",
                12 => "B",
                13 => "C",
                14 => "D",
                15 => "E",
                16 => "F",
                17 => "G",
                18 => "H",
                19 => "I",
                20 => "J",
                21 => "K",
                22 => "L",
                23 => "M",
                24 => "N",
                25 => "O",
                26 => "P",
                27 => "Q",
                28 => "R",
                29 => "S",
                30 => "T",
                31 => "U",
                32 => "V",
                33 => "W",
                34 => "X",
                35 => "Y",
                36 => "Z",
                37 => ".",
                38 => ",",
                39 => "-",
                40 => "/",
                41 => " ",
                _ => "¿¿??"
            };
        }
    }
}
