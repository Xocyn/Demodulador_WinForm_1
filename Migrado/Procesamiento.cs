using MathNet.Numerics;
using System;
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

        public Procesamiento(RichTextBox mainDisplay)
        {
            _mainDisplay = mainDisplay;
            _metodos = new Metodos(LogToDisplay);
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

                string mensaje_string = string.Join(" ", MENSAJE.Select(x => x.ToString("D2")));
                LogToDisplay($"MENSAJE: {mensaje_string}\n");

                Geografica.EliminarPosicionesImpares(ECC); // Obtengo los DX

                ECC = PrepararECC(ECC);

                // ── Fase 4: Verificación de ECC ────────────────────────────────────
                if (VerificarECC(MENSAJE, ECC))
                {
                    LogToDisplay("✓ ECC correcto\n");
                }
                else
                {
                    LogToDisplay("✗ Error en ECC\n");
                    return;
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
            }
            catch (Exception ex)
            {
                LogToDisplay($"❌ Error en Procesar: {ex.Message}\n");
            }
        }

        /// <summary>
        /// Verifica el ECC (Error Correcting Code) del mensaje
        /// </summary>
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

        /// <summary>
        /// Prepara el ECC eliminando el primer elemento y los últimos 2
        /// </summary>
        public List<int> PrepararECC(List<int> list)
        {
            if (list.Count < 4)
            {
                LogToDisplay("MENSAJE demasiado corto para preparar ECC.\n");
                return new List<int>();
            }

            List<int> ecc = list
                .Skip(1)
                .Take(list.Count - 4)
                .ToList();

            return ecc;
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
}
