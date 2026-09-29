using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Text;
using Demodulador_WinForm_1.Migrado;

namespace Dem_v2
{
    public class Respuesta
    {
        static StringBuilder rta = new StringBuilder();
        static List<int> ecc = new List<int>();
        static string MMSI = "889944123"; 
        // public strng MMSI {get; set;} = "998844123"// MODIFICABLE SEGUN LA COSTERA 
        static bool VHF = true;

        static public void Decidir(Mensaje_2 msg, bool rtx)
        {
            ArgumentNullException.ThrowIfNull(msg);
            if (msg.Formato != 112)
                throw new ArgumentException("La respuesta requiere un mensaje de socorro.", nameof(msg));

            if (rtx)
                RetransmisionSocorro(msg);
            else
                RespuestaSocorro(msg);
        }

        static public void RespuestaSocorro(Mensaje_2 msg) =>
            Enviar(PrepararRespuestaSocorro(msg));

        static public void RetransmisionSocorro(Mensaje_2 msg) =>
            Enviar(PrepararRetransmisionSocorro(msg));

        static public void ACKRTX(Mensaje_2 msg) =>
            Enviar(PrepararAckRetransmision(msg));

        public static bool PuedeResponderSocorro(Mensaje_2 msg)
        {
            if (msg?.Mensaje_List == null || msg.Formato != 112)
                return false;
            IReadOnlyList<int> caracteres = msg.Mensaje_List;
            int repetido = caracteres.Count > 1 && caracteres[1] == 112 ? 1 : 0;
            return DatosSocorroCompletos(caracteres, 1 + repetido);
        }

        public static bool PuedeAcusarRetransmision(Mensaje_2 msg)
        {
            if (msg?.Mensaje_List == null || msg.Formato is not (114 or 116 or 120))
                return false;

            IReadOnlyList<int> caracteres = msg.Mensaje_List;
            int repetido = caracteres.Count > 1 && caracteres[1] == msg.Formato ? 1 : 0;
            int indiceCategoria = msg.Formato == 116 ? 1 + repetido : 6 + repetido;
            int indiceTelemando = msg.Formato == 116 ? 7 + repetido : 12 + repetido;
            return indiceTelemando < caracteres.Count &&
                   caracteres[^1] != 122 &&
                   caracteres[indiceCategoria] == 112 && caracteres[indiceTelemando] == 112 &&
                   DatosSocorroCompletos(caracteres, indiceTelemando + 1);
        }

        internal static List<int> PrepararRespuestaSocorro(Mensaje_2 msg)
        {
            List<int> datos = DatosSocorro(msg);
            var caracteres = new List<int> { 116, 116, 112 };
            caracteres.AddRange(CodificarMmsi(MMSI));
            caracteres.Add(110);
            caracteres.AddRange(datos);
            caracteres.Add(127);
            return caracteres;
        }

        internal static List<int> PrepararRetransmisionSocorro(Mensaje_2 msg)
        {
            List<int> datos = DatosSocorro(msg);
            int formato = msg.formato_rtx;
            if (formato is not (116 or 120))
                throw new InvalidOperationException("Seleccione AllShips o Individual para la retransmisión.");

            var caracteres = new List<int> { formato, formato };
            if (formato == 120)
                caracteres.AddRange(CodificarMmsi(msg.MMSI_RX));
            caracteres.Add(112);
            caracteres.AddRange(CodificarMmsi(MMSI));
            caracteres.Add(112);
            caracteres.AddRange(datos);
            caracteres.Add(formato == 120 ? 117 : 127);
            return caracteres;
        }

        internal static List<int> PrepararAckRetransmision(Mensaje_2 msg)
        {
            if (!PuedeAcusarRetransmision(msg))
                throw new ArgumentException("El mensaje no contiene una retransmisión de socorro completa.", nameof(msg));

            List<int> datos = DatosSocorro(msg);
            var caracteres = new List<int> { msg.Formato, msg.Formato };
            if (msg.Formato != 116)
                caracteres.AddRange(CodificarMmsi(MmsiTransmisor(msg)));
            caracteres.Add(112);
            caracteres.AddRange(CodificarMmsi(MMSI));
            caracteres.Add(112);
            caracteres.AddRange(datos);
            caracteres.Add(122);
            return caracteres;
        }

        private static List<int> DatosSocorro(Mensaje_2 msg)
        {
            ArgumentNullException.ThrowIfNull(msg);
            IReadOnlyList<int> caracteres = msg.Mensaje_List;
            if (caracteres.Count == 0 || caracteres[0] != msg.Formato)
                throw new ArgumentException("El mensaje recibido no contiene caracteres validados.", nameof(msg));

            int repetido = caracteres.Count > 1 && caracteres[1] == msg.Formato ? 1 : 0;
            int inicio = msg.Formato switch
            {
                112 => 1 + repetido,
                114 or 120 => 13 + repetido,
                116 => 8 + repetido,
                _ => throw new ArgumentException("Formato sin datos de socorro para responder.", nameof(msg))
            };
            const int cantidad = 14; // MMSI, peligro, posición, UTC y siguiente comunicación.
            if (!DatosSocorroCompletos(caracteres, inicio))
                throw new ArgumentException("Datos de socorro incompletos.", nameof(msg));

            var datos = new List<int>(cantidad);
            for (int i = inicio; i < inicio + cantidad; i++)
            {
                if (caracteres[i] is < 0 or > 127)
                    throw new ArgumentException("Datos de socorro inválidos.", nameof(msg));
                datos.Add(caracteres[i]);
            }
            return datos;
        }

        private static bool DatosSocorroCompletos(IReadOnlyList<int> caracteres, int inicio)
        {
            const int cantidad = 14;
            if (caracteres.Count != inicio + cantidad + 1 ||
                Dem_v2.General.ACK(caracteres[^1]) == "¿?")
                return false;

            for (int i = inicio; i < inicio + cantidad; i++)
                if (caracteres[i] is < 0 or > 127 ||
                    (i < inicio + 5 && caracteres[i] > 99))
                    return false;
            return true;
        }

        private static string MmsiTransmisor(Mensaje_2 msg)
        {
            IReadOnlyList<int> caracteres = msg.Mensaje_List;
            int repetido = caracteres.Count > 1 && caracteres[1] == msg.Formato ? 1 : 0;
            int inicio = 7 + repetido;
            if (inicio + 5 >= caracteres.Count)
                throw new ArgumentException("MMSI transmisor incompleto.", nameof(msg));

            var mmsi = new StringBuilder(10);
            for (int i = inicio; i < inicio + 5; i++)
            {
                if (caracteres[i] is < 0 or > 99)
                    throw new ArgumentException("MMSI transmisor inválido.", nameof(msg));
                mmsi.Append(caracteres[i].ToString("D2"));
            }
            return mmsi.ToString();
        }

        private static List<int> CodificarMmsi(string mmsi)
        {
            if (mmsi == null || (mmsi.Length != 9 && mmsi.Length != 10))
                throw new ArgumentException("El MMSI debe tener 9 o 10 dígitos.", nameof(mmsi));
            foreach (char digito in mmsi)
                if (!char.IsAsciiDigit(digito))
                    throw new ArgumentException("El MMSI sólo puede contener dígitos.", nameof(mmsi));

            if (mmsi.Length == 9)
                mmsi += "0";
            var caracteres = new List<int>(5);
            for (int i = 0; i < mmsi.Length; i += 2)
                caracteres.Add(int.Parse(mmsi.Substring(i, 2)));
            return caracteres;
        }

        private static void Enviar(IReadOnlyList<int> caracteres)
        {
            rta.Clear();
            ecc.Clear();
            rta.Append(CodificarRespuesta(caracteres));
            EOS();
        }

        internal static string CodificarRespuesta(IReadOnlyList<int> caracteres)
        {
            ArgumentNullException.ThrowIfNull(caracteres);
            if (caracteres.Count < 3 || caracteres[0] != caracteres[1] ||
                Dem_v2.General.ACK(caracteres[^1]) == "¿?")
                throw new ArgumentException("Trama de respuesta incompleta.", nameof(caracteres));

            var bits = new StringBuilder();
            var valoresEcc = new List<int>(caracteres.Count - 1);
            for (int i = 0; i < caracteres.Count; i++)
            {
                if (caracteres[i] is < 0 or > 127)
                    throw new ArgumentException("Carácter de respuesta inválido.", nameof(caracteres));
                Convertir.ConvertirNumero(caracteres[i], bits);
                if (i != 1) // La segunda copia del formato no participa en el ECC.
                    valoresEcc.Add(caracteres[i]);
            }

            int fin = caracteres[^1];
            Convertir.ConvertirNumero(Convertir.Mod2Sum7Bits(valoresEcc), bits);
            Convertir.ConvertirNumero(fin, bits);
            Convertir.ConvertirNumero(fin, bits);
            return bits.ToString();
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
