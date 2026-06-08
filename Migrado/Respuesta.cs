using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dem_v2
{
    public class Respuesta
    {
        static StringBuilder rta = new StringBuilder();
        static List<int> ecc = new List<int>();
        static string MMSI = "889944123"; 
        // public strng MMSI {get; set;} = "998844123"// MODIFICABLE SEGUN LA COSTERA 
        static bool VHF = true;

        static public void Decidir(Mensaje msg)
        { 
            switch (msg.Formato) // voy a tener que responder a las rtx de ack en este metodo
            {
                case 112:
                    RespuestaSocorro(msg.data_respuesta);
                    break;
                default:
                    break;
            }
        }
        static public void RespuestaSocorro(List<int> datos_respuesta)
        {
            rta.Clear();
            ecc.Clear();

            // ⚠️ IMPORTANTE: Crear una copia para evitar modificar la lista original
            List<int> datos_local = new List<int>(datos_respuesta);
            Geografica.EliminarPosicionesImpares(datos_local);

            Convertir.ConvertirNumero(116, rta); Convertir.ConvertirNumero(116, rta); ecc.Add(116);
            Convertir.ConvertirNumero(112, rta); ecc.Add(112);
            Convertir.MMSI(rta, ecc, MMSI);
            Convertir.ConvertirNumero(110, rta); ecc.Add(110);
            for (int i = 0; i < datos_local.Count; i++)
            {
                Convertir.ConvertirNumero(datos_local[i], rta); ecc.Add(datos_local[i]);
            }
            Convertir.ConvertirNumero(127, rta); ecc.Add(127);
            Convertir.ConvertirNumero(Convertir.Mod2Sum7Bits(ecc), rta);
            Convertir.ConvertirNumero(127, rta); Convertir.ConvertirNumero(127, rta);
            EOS();

        }

        static public void MensajeIndividual(string mmsi_rx, int categoria, int tipo_msg_ind, bool acuse, int canal, int motivo)
        {
            rta.Clear();
            ecc.Clear();
            Convertir.ConvertirNumero(120, rta); Convertir.ConvertirNumero(120, rta); ecc.Add(120);
            Funcionalidades.MMSI(rta, ecc, mmsi_rx);
            Convertir.ConvertirNumero(categoria, rta); ecc.Add(categoria);
            Funcionalidades.MMSI(rta, ecc, MMSI);
            Convertir.ConvertirNumero(tipo_msg_ind, rta); ecc.Add(tipo_msg_ind); // Primer telemando
            // Segundo telemando 
            if (tipo_msg_ind == 104)
            {
                Convertir.ConvertirNumero(motivo, rta); ecc.Add(motivo);
            }
            else
            {
                Convertir.ConvertirNumero(126, rta); ecc.Add(126); // Segundo telemando 
            }
            // Frecuencia de canal
            if (tipo_msg_ind == 121 || tipo_msg_ind == 118)
            {
                Convertir.ConvertirNumero(126, rta); ecc.Add(126); Convertir.ConvertirNumero(126, rta); ecc.Add(126);
                Convertir.ConvertirNumero(126, rta); ecc.Add(126); Convertir.ConvertirNumero(126, rta); ecc.Add(126);
                Convertir.ConvertirNumero(126, rta); ecc.Add(126); Convertir.ConvertirNumero(126, rta); ecc.Add(126);
            }
            else
            {
                string canal_norma = (901000 + canal).ToString(); // Norma para mayoria simplex
                General.Frec(rta, ecc, canal_norma);
                General.Frec(rta, ecc, canal_norma);
            }
            // EOS
            if (acuse)
            {
                Convertir.ConvertirNumero(122, rta); ecc.Add(122);
                Convertir.ConvertirNumero(Convertir.Mod2Sum7Bits(ecc), rta);
                Convertir.ConvertirNumero(122, rta); Convertir.ConvertirNumero(122, rta);
            }
            else
            {
                Convertir.ConvertirNumero(117, rta); ecc.Add(117);
                Convertir.ConvertirNumero(Convertir.Mod2Sum7Bits(ecc), rta);
                Convertir.ConvertirNumero(117, rta); Convertir.ConvertirNumero(117, rta);
            }
            EOS();
        }

        static public void MensajeAllShips(int categoria, int canal)
        {
            rta.Clear();
            ecc.Clear();
            Convertir.ConvertirNumero(116, rta); Convertir.ConvertirNumero(116, rta); ecc.Add(116);
            Convertir.ConvertirNumero(categoria, rta); ecc.Add(categoria);
            Funcionalidades.MMSI(rta, ecc, MMSI);
            Convertir.ConvertirNumero(100, rta); ecc.Add(100); // Primer telemando
            Convertir.ConvertirNumero(126, rta); ecc.Add(126); // Segundo telemando
            string canal_norma = (901000 + canal).ToString(); // Norma para mayoria simplex
            General.Frec(rta, ecc, canal_norma);
            General.Frec(rta, ecc, canal_norma);
            Convertir.ConvertirNumero(127, rta); ecc.Add(127);
            Convertir.ConvertirNumero(Convertir.Mod2Sum7Bits(ecc), rta);
            Convertir.ConvertirNumero(127, rta); Convertir.ConvertirNumero(127, rta);
            EOS();
        }

        static public void MensajeGrupos(int sig_com, string mmsi_rx, int canal)
        {
            rta.Clear();
            ecc.Clear();
            Convertir.ConvertirNumero(114, rta); Convertir.ConvertirNumero(114, rta); ecc.Add(114);
            Funcionalidades.MMSI(rta, ecc, mmsi_rx);
            Convertir.ConvertirNumero(100, rta); ecc.Add(100); // RUTINA
            Funcionalidades.MMSI(rta, ecc, MMSI);
            Convertir.ConvertirNumero(sig_com, rta); ecc.Add(sig_com); // Primer telemando
            Convertir.ConvertirNumero(126, rta); ecc.Add(126); // Segundo telemando
            string canal_norma = (901000 + canal).ToString(); // Norma para mayoria simplex
            General.Frec(rta, ecc, canal_norma);
            General.Frec(rta, ecc, canal_norma);
            Convertir.ConvertirNumero(127, rta); ecc.Add(127);
            Convertir.ConvertirNumero(Convertir.Mod2Sum7Bits(ecc), rta);
            Convertir.ConvertirNumero(127, rta); Convertir.ConvertirNumero(127, rta);
            EOS();
        }

        static public void MensajeGeografico(int sig_com, int canal, int zona)
        { 
            // DESARROLLAR
        }
        static public void EOS()
        {
            List<int> phasignseq = new List<int> { 125, 111, 125, 110, 125, 109, 125, 108, 125, 107, 125, 106 }; 
            StringBuilder pss = new StringBuilder();

            foreach (int ps in phasignseq)
            {
                Convertir.ConvertirNumero(ps, pss);
            }

            List<int> inicio_rx = new List<int> { 105, 104 }; // agrego los 105 y 104 al inicio del Rx
            StringBuilder rx = new StringBuilder();
            foreach (int pf in inicio_rx)
            {
                Convertir.ConvertirNumero(pf, rx);
            }
            rx.Append(rta); // armo los Rx 

            StringBuilder resultado = new StringBuilder();

            for (int i = 0; i < rta.Length; i += 10)
            {
                // Extraer 10 caracteres de resultadoConChequeo (o menos en la última iteración)
                int longitud = Math.Min(10, rta.Length - i);
                string aux = rta.ToString(i, longitud);
                resultado.Append(aux);

                // Extraer 10 caracteres de rx (o menos en la última iteración)
                longitud = Math.Min(10, rx.Length - i);
                string aux2 = rx.ToString(i, longitud);
                resultado.Append(aux2);
            }

            pss.Append(resultado);
            StringBuilder dot = new StringBuilder();

            for (int i = 0; i <= 20; i += 1)
            {
                dot.Append(i % 2 == 0 ? "0" : "1");
            }
            dot.Append(pss);

            string rutadesalida = AppDomain.CurrentDomain.BaseDirectory;
            string archivoFinal = Path.Combine(rutadesalida, "respuesta.txt");
            string archivoWav = Path.Combine(rutadesalida, "respuesta.wav");

            //File.WriteAllText(archivoFinal, pss.ToString().TrimEnd());
            // CON DOT
            File.WriteAllText(archivoFinal, dot.ToString().TrimEnd());

            // MODULACION
            BFSKModulator.GenerateWav(archivoFinal, archivoWav, VHF);

            AudioPlayer.Play(archivoWav);


            ecc.Clear();
            rta.Clear();
            pss.Clear();
            rx.Clear();
            resultado.Clear();
            dot.Clear();
        }

        /// <summary>
        /// Reproduce un archivo de audio y captura simultáneamente el audio del altavoz (loopback).
        /// Demodula el audio capturado y lo procesa automáticamente.
        /// </summary>
        static private async Task PlayAndCaptureAsync(string wavFile, Procesamiento procesamiento, string ondaBinaria)
        {
            try
            {
                using (var loopback = new LoopbackAudioCapture())
                {
                    // Iniciar captura antes de reproducir
                    loopback.StartCapture();

                    // Reproducir audio sin bloquear
                    var playTask = AudioPlayer.PlayAsync(wavFile);

                    // Esperar a que termine la reproducción
                    await playTask;

                    // Dar un margen para que se capture todo el audio
                    await Task.Delay(500);

                    // Detener captura
                    byte[] capturedAudio = loopback.StopCapture();

                    if (capturedAudio.Length > 0)
                    {
                        // Demodular el audio capturado
                        var demodulator = new BFSKDemodulator(VHF);
                        string[] decodedBits = demodulator.ProcessAudio(capturedAudio, capturedAudio.Length);

                        // Usar la primera fase que tenga datos
                        string resultado = decodedBits.FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? ondaBinaria;

                        // Procesar la onda demodulada
                        if (!string.IsNullOrEmpty(resultado))
                        {
                            procesamiento.Procesar(resultado);
                        }
                    }
                    else
                    {
                        // Si la captura de loopback falló, usar la onda binaria generada
                        procesamiento.Procesar(ondaBinaria);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en PlayAndCaptureAsync: {ex.Message}");
                // Fallback: procesar la onda binaria directamente
                procesamiento.Procesar(ondaBinaria);
            }
        }
        internal class Convertir
        {
            public static void ConvertirNumero(int leido, StringBuilder resultadoConChequeo)
            {

                if (leido >= 0 && leido <= 127)
                {
                    // Paso 1: Convertir a binario (7 bits)
                    string binario = Convert.ToString(leido, 2).PadLeft(7, '0');

                    // Paso 2: Invertir el orden de los bits (MSB ↔ LSB)
                    string binarioInvertido = InvertirBits(binario);

                    // Paso 3: Contar ceros en la secuencia invertida
                    int cantidadCeros = ContarCeros(binarioInvertido);

                    // Paso 4: Convertir cantidad de ceros a binario de 3 bits
                    string bitsChequeo = Convert.ToString(cantidadCeros, 2).PadLeft(3, '0');

                    // Paso 5: Agregar bits de chequeo al final (nuevo LSB)
                    string binarioFinal = binarioInvertido + bitsChequeo;


                    //resultadoConChequeo.Append(binarioFinal + " "); // Agregar espacio después de cada número convertido me mueve los indices
                    resultadoConChequeo.Append(binarioFinal);

                }
                else
                {
                    Console.WriteLine($"✗ Advertencia: {leido} está fuera del rango (0-127)");
                }

                // Función para invertir los bits
                static string InvertirBits(string binario)
                {
                    return new string(binario.Reverse().ToArray());
                }

                // Función para contar los ceros en una cadena binaria
                static int ContarCeros(string binario)
                {
                    return binario.Count(c => c == '0');
                }

            }

            static public void MMSI(StringBuilder rta, List<int> ECC, string numero)
            {
                List<int> mmsi = new List<int>();

                // Agrupar de 2 en 2
                List<string> grupos = AgruparDeDosEnDos(numero);
                foreach (string grupo in grupos)
                {
                    int value = Convert.ToInt32(grupo, 10);
                    mmsi.Add(value);
                }

                foreach (int mm in mmsi)
                {
                    ECC.Add(mm);
                    ConvertirNumero(mm, rta);
                }

            }

            static List<string> AgruparDeDosEnDos(string numero)
            {
                List<string> grupos = new List<string>();

                // Si es impar, agregar '0' al final
                if (numero.Length % 2 != 0)
                {
                    numero += "0"; // se le agrega un 0 al final para completar el ultimo grupo de 2 (NORMA)
                }

                // Agrupar de 2 en 2
                for (int i = 0; i < numero.Length; i += 2)
                {
                    string grupo = numero.Substring(i, 2);
                    grupos.Add(grupo);
                }

                return grupos;
            }

            public static int Mod2Sum7Bits(List<int> values)
            {
                if (values == null || values.Count == 0)
                    return 0;

                int result = 0;

                foreach (int v in values)
                {
                    result ^= v; // XOR acumulativo (suma módulo 2)
                }

                // Nos quedamos con los 7 bits menos significativos
                result &= 0x7F;

                return result;
            }

        }
        internal class Funcionalidades
        {
            static public void MMSI(StringBuilder resultadoConChequeo, List<int> ECC, string numero)
            {
                List<int> mmsi = new List<int>();

                // Agrupar de 2 en 2
                List<string> grupos = AgruparDeDosEnDos(numero);
                foreach (string grupo in grupos)
                {
                    int value = Convert.ToInt32(grupo, 10);
                    mmsi.Add(value);
                }

                foreach (int mm in mmsi)
                {
                    ECC.Add(mm);
                    Convertir.ConvertirNumero(mm, rta);
                }

            }

            static List<string> AgruparDeDosEnDos(string numero)
            {
                List<string> grupos = new List<string>();

                // Si es impar, agregar '0' al final
                if (numero.Length % 2 != 0)
                {
                    numero += "0"; // se le agrega un 0 al final para completar el ultimo grupo de 2 (NORMA)
                }

                // Agrupar de 2 en 2
                for (int i = 0; i < numero.Length; i += 2)
                {
                    string grupo = numero.Substring(i, 2);
                    grupos.Add(grupo);
                }

                return grupos;
            }

            public static void Posicion(StringBuilder resultadoConChequeo, List<int> ECC)
            {
                // -38.04248790955501, -57.545178158600976  MDP
                Convertir.ConvertirNumero(33, rta); ECC.Add(33);
                Convertir.ConvertirNumero(80, rta); ECC.Add(80);
                Convertir.ConvertirNumero(40, rta); ECC.Add(40);
                Convertir.ConvertirNumero(57, rta); ECC.Add(57);
                Convertir.ConvertirNumero(54, rta); ECC.Add(54);

                // Obtener zona horaria de Argentina
                TimeZoneInfo argentinaZone = TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");

                // Convertir hora UTC a hora Argentina
                DateTime argentinaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, argentinaZone);

                int hora = argentinaTime.Hour;
                int minutos = argentinaTime.Minute;
                Convertir.ConvertirNumero(hora, rta); ECC.Add(hora);
                Convertir.ConvertirNumero(minutos, rta); ECC.Add(minutos);
            }
        }

        internal class General
        {
            static public void Frec(StringBuilder resultadoConChequeo, List<int> ECC, string numero)
            {
                List<int> frec_canal = new List<int>();

                // Agrupar de 2 en 2
                List<string> grupos = Agrupar_2(numero);
                foreach (string grupo in grupos)
                {
                    int value = Convert.ToInt32(grupo, 10);
                    frec_canal.Add(value);
                }

                foreach (int fc in frec_canal)
                {
                    ECC.Add(fc);
                    Convertir.ConvertirNumero(fc, rta);
                }

            }

            static List<string> Agrupar_2(string numero)
            {
                List<string> grupos = new List<string>();

                // Si es impar, agregar '3' al inicio
                //if (numero.Length % 2 != 0)
                //{
                //    numero = "3" + numero; // se le agrega un 3 al inicio
                //}

                // Agrupar de 2 en 2
                for (int i = 0; i < numero.Length; i += 2)
                {
                    string grupo = numero.Substring(i, 2);
                    grupos.Add(grupo);
                }

                return grupos;
            }
        }
    }
}
