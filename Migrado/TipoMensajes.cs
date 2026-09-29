using System;
using System.Collections.Generic;
using Dem_v2;
using System.Text;

namespace Demodulador_WinForm_1.Migrado
{
    internal static class TipoMensajes
    {
        private readonly record struct CampoFrecuencia(string Descripcion, int Consumidos, bool EsCanal, bool EsPosicion);

        public static string Decodificar(IReadOnlyList<int> mensaje)
        {
            ArgumentNullException.ThrowIfNull(mensaje);
            if (mensaje.Count == 0)
                return "Mensaje vacío.";

            return mensaje[0] switch
            {
                120 => DecodificarIndividual(mensaje),
                102 => DecodificarGeografica(mensaje),
                112 => DecodificarSocorro(mensaje),
                114 => DecodificarGrupo(mensaje),
                116 => DecodificarAllShips(mensaje),
                123 => DecodificarAutomatica(mensaje),
                _ => "Formato no reconocido"
            };
        }

        private static string DecodificarIndividual(IReadOnlyList<int> mensaje)
        {
            int fin = mensaje.Count - 1;
            if (fin < 0 || General.ACK(mensaje[fin]) == "¿?")
                return "INDIVIDUAL (120): falta un carácter de fin válido.";

            // Algunas tramas repiten el formato al comienzo. Aquí ya no están
            // intercalados los RX: cada índice representa un carácter original.
            int inicioMmsiReceptor = mensaje.Count > 1 && mensaje[1] == 120 ? 2 : 1;
            int indiceCategoria = inicioMmsiReceptor + 5;
            int inicioMmsiTransmisor = indiceCategoria + 1;
            int indicePrimerTelemando = inicioMmsiTransmisor + 5;
            if (indicePrimerTelemando >= fin ||
                !TryMmsi(mensaje, inicioMmsiReceptor, fin, out string mmsiReceptor) ||
                !TryMmsi(mensaje, inicioMmsiTransmisor, fin, out string mmsiTransmisor))
                return $"INDIVIDUAL (120): encabezado incompleto o MMSI inválido ({mensaje.Count} caracteres).";

            int categoria = mensaje[indiceCategoria];
            int primerTelemando = mensaje[indicePrimerTelemando];
            string primerTexto = General.PrimerTelemando(primerTelemando, out bool solicitaPosicion);
            var resultado = new StringBuilder();
            resultado.AppendLine($"Formato: {FormatSpecifier.Formato(120)}");
            resultado.AppendLine($"MMSI Receptor: {mmsiReceptor}");
            resultado.AppendLine($"Categoría: {General.Categoria(categoria)} ({categoria})");
            resultado.AppendLine($"MMSI Transmisor: {mmsiTransmisor}");
            resultado.AppendLine($"Primer Telemando: {primerTexto} ({primerTelemando})");

            if (categoria == 112)
            {
                int inicioSocorro = indicePrimerTelemando + 1;
                if (inicioSocorro + 13 >= fin ||
                    !TryMmsi(mensaje, inicioSocorro, fin, out string mmsiSocorro))
                    return resultado.AppendLine("Datos de socorro incompletos.").ToString();

                resultado.AppendLine($"MMSI Socorro: {mmsiSocorro}");
                resultado.AppendLine($"Tipo de emergencia: {Socorro.Peligro(mensaje[inicioSocorro + 5])}");
                resultado.AppendLine($"Coordenadas: {Geografica.Posicion(Tomar(mensaje, inicioSocorro + 6, 5))}");
                resultado.AppendLine($"UTC: {Utc(mensaje[inicioSocorro + 11], mensaje[inicioSocorro + 12])}");
                resultado.AppendLine($"Siguiente Comunicación: {Socorro.PosteriorCom(mensaje[inicioSocorro + 13])}");
            }
            else
            {
                int indiceSegundoTelemando = indicePrimerTelemando + 1;
                if (indiceSegundoTelemando >= fin)
                    return resultado.AppendLine("Segundo telemando y datos incompletos.").ToString();

                resultado.AppendLine($"Segundo Telemando: {General.SegundoTelemando(mensaje[indiceSegundoTelemando])} ({mensaje[indiceSegundoTelemando]})");
                int inicioDatos = indiceSegundoTelemando + 1;
                if (solicitaPosicion)
                {
                    if (inicioDatos + 6 == fin && mensaje[fin] == 117)
                    {
                        resultado.AppendLine("Posición: Solicitud de posición");
                    }
                    else if (inicioDatos + 7 < fin)
                    {
                        List<int> posicion = Tomar(mensaje, inicioDatos, 7);
                        resultado.AppendLine($"Posición: {Geografica.Posicion(posicion)}");
                        resultado.AppendLine($"UTC: {Utc(mensaje[inicioDatos + 6], mensaje[inicioDatos + 7])}");
                    }
                    else
                    {
                        resultado.AppendLine("Datos de posición incompletos.");
                    }
                }
                else
                {
                    CampoFrecuencia? primero = LeerFrecuencia(mensaje, inicioDatos, fin);
                    if (primero is null)
                    {
                        resultado.AppendLine("Primer canal o frecuencia incompleto.");
                    }
                    else if (primero.Value.EsPosicion)
                    {
                        resultado.AppendLine($"Posición: {primero.Value.Descripcion}");
                    }
                    else
                    {
                        resultado.AppendLine($"{(primero.Value.EsCanal ? "Canal" : "Frecuencia")} Rx: {primero.Value.Descripcion}");
                        CampoFrecuencia? segundo = LeerFrecuencia(
                            mensaje, inicioDatos + primero.Value.Consumidos, fin);
                        if (segundo is null)
                            resultado.AppendLine("Segundo canal o frecuencia incompleto.");
                        else
                            resultado.AppendLine($"{(segundo.Value.EsCanal ? "Canal" : "Frecuencia")} Tx: {segundo.Value.Descripcion}");
                    }
                }
            }

            resultado.AppendLine($"Fin: {General.ACK(mensaje[fin])} ({mensaje[fin]})");
            return resultado.ToString();
        }

        private static CampoFrecuencia? LeerFrecuencia(IReadOnlyList<int> mensaje, int inicio, int fin)
        {
            if (inicio + 3 > fin)
                return null;
            if (mensaje[inicio] == 126)
                return new CampoFrecuencia("Sin información", 3, false, false);
            if (mensaje[inicio] == 55)
            {
                if (inicio + 6 > fin)
                    return null;
                return new CampoFrecuencia(
                    Geografica.Posicion(Tomar(mensaje, inicio + 1, 5)), 6, false, true);
            }

            List<int> valores = Tomar(mensaje, inicio, 3);
            if (valores.Exists(valor => valor > 99))
                return new CampoFrecuencia($"Datos no reconocidos [{string.Join(", ", valores)}]", 3, false, false);
            List<int> digitos = General.Separar(valores);
            if (digitos[0] == 4)
            {
                if (inicio + 4 > fin || mensaje[inicio + 3] > 99)
                    return null;
                digitos = General.Separar(Tomar(mensaje, inicio, 4));
                return new CampoFrecuencia(
                    $"{digitos[1]}{digitos[2]}{digitos[3]}{digitos[4]}{digitos[5]}.{digitos[6]}{digitos[7]} kHz",
                    4, false, false);
            }

            string descripcion = digitos[0] switch
            {
                0 or 1 or 2 => $"{digitos[0]}{digitos[1]}{digitos[2]}{digitos[3]}{digitos[4]}.{digitos[5]} kHz",
                3 or 8 => $"{digitos[1]}{digitos[2]}{digitos[3]}{digitos[4]}{digitos[5]}",
                9 => $"{digitos[2] switch { 0 => "RR", 1 => "Barco", 2 => "Costera", _ => "Desconocido" }}: {digitos[3]}{digitos[4]}{digitos[5]}",
                _ => $"Datos no reconocidos [{string.Join(", ", valores)}]"
            };
            return new CampoFrecuencia(descripcion, 3, digitos[0] is 3 or 8 or 9, false);
        }

        private static bool TryMmsi(IReadOnlyList<int> mensaje, int inicio, int fin, out string mmsi)
        {
            mmsi = string.Empty;
            if (inicio + 5 > fin)
                return false;
            var resultado = new StringBuilder(10);
            for (int i = inicio; i < inicio + 5; i++)
            {
                if (mensaje[i] is < 0 or > 99)
                    return false;
                resultado.Append(mensaje[i].ToString("D2"));
            }
            mmsi = resultado.ToString();
            return true;
        }

        private static List<int> Tomar(IReadOnlyList<int> mensaje, int inicio, int cantidad)
        {
            var valores = new List<int>(cantidad);
            for (int i = inicio; i < inicio + cantidad; i++)
                valores.Add(mensaje[i]);
            return valores;
        }

        private static string Utc(int horas, int minutos) => $"{horas:D2}:{minutos:D2}";

        private static string AreaGeografica(IReadOnlyList<int> mensaje, int inicio)
        {
            var digitos = new StringBuilder(10);
            for (int i = inicio; i < inicio + 5; i++)
            {
                if (mensaje[i] is < 0 or > 99)
                    return "Datos de área no reconocidos";
                digitos.Append(mensaje[i].ToString("D2"));
            }

            string area = digitos.ToString();
            string referencia = area[0] switch
            {
                '0' => "NE",
                '1' => "NW",
                '2' => "SE",
                '3' => "SW",
                _ => "??"
            };
            string latitud = area.Substring(1, 2);
            string longitud = area.Substring(3, 3);
            int limiteLatitud = int.Parse(latitud) + int.Parse(area.Substring(6, 2));
            int limiteLongitud = int.Parse(longitud) + int.Parse(area.Substring(8, 2));
            return $"{referencia} - Latitud {latitud} .. {limiteLatitud} ° - Longitud {longitud} .. {limiteLongitud} °";
        }

        private static string DecodificarSocorro(IReadOnlyList<int> mensaje)
        {
            int fin = mensaje.Count - 1;
            if (fin < 0 || General.ACK(mensaje[fin]) == "¿?")
                return "SOCORRO (112): falta un carácter de fin válido.";

            // La lista validada por ECC contiene sólo los caracteres originales,
            // sin los caracteres retransmitidos que intercalaba Procesamiento.
            int inicioMmsi = mensaje.Count > 1 && mensaje[1] == 112 ? 2 : 1;
            int indicePeligro = inicioMmsi + 5;
            int inicioCoordenadas = indicePeligro + 1;
            int indiceHoras = inicioCoordenadas + 5;
            int indiceComunicacion = indiceHoras + 2;
            if (indiceComunicacion >= fin ||
                !TryMmsi(mensaje, inicioMmsi, fin, out string mmsi))
                return $"SOCORRO (112): datos incompletos o MMSI inválido ({mensaje.Count} caracteres).";

            var resultado = new StringBuilder();
            resultado.AppendLine($"Formato: {FormatSpecifier.Formato(112)}");
            resultado.AppendLine($"MMSI: {mmsi}");
            resultado.AppendLine($"Tipo de Emergencia: {Socorro.Peligro(mensaje[indicePeligro])} ({mensaje[indicePeligro]})");
            resultado.AppendLine($"Coordenadas: {Geografica.Posicion(Tomar(mensaje, inicioCoordenadas, 5))}");
            resultado.AppendLine($"UTC: {Utc(mensaje[indiceHoras], mensaje[indiceHoras + 1])}");
            resultado.AppendLine($"Siguiente Comunicación: {Socorro.PosteriorCom(mensaje[indiceComunicacion])} ({mensaje[indiceComunicacion]})");
            resultado.AppendLine($"Fin: {General.ACK(mensaje[fin])} ({mensaje[fin]})");
            return resultado.ToString();
        }

        private static string DecodificarAllShips(IReadOnlyList<int> mensaje)
        {
            int fin = mensaje.Count - 1;
            if (fin < 0 || General.ACK(mensaje[fin]) == "¿?")
                return "ALLSHIPS (116): falta un carácter de fin válido.";

            // La lista validada por ECC no contiene los caracteres retransmitidos.
            int indiceCategoria = mensaje.Count > 1 && mensaje[1] == 116 ? 2 : 1;
            int inicioMmsi = indiceCategoria + 1;
            int indicePrimerTelemando = inicioMmsi + 5;
            if (indicePrimerTelemando >= fin ||
                !TryMmsi(mensaje, inicioMmsi, fin, out string mmsi))
                return $"ALLSHIPS (116): encabezado incompleto o MMSI inválido ({mensaje.Count} caracteres).";

            int categoria = mensaje[indiceCategoria];
            int primerTelemando = mensaje[indicePrimerTelemando];
            var resultado = new StringBuilder();
            resultado.AppendLine($"Formato: {FormatSpecifier.Formato(116)}");
            resultado.AppendLine($"MMSI: {mmsi}");
            resultado.AppendLine($"Categoría: {General.Categoria(categoria)} ({categoria})");
            resultado.AppendLine($"Primer Telemando: {General.PrimerTelemando(primerTelemando, out _)} ({primerTelemando})");

            if (categoria == 112)
            {
                int inicioMmsiSocorro = indicePrimerTelemando + 1;
                int indicePeligro = inicioMmsiSocorro + 5;
                int inicioCoordenadas = indicePeligro + 1;
                int indiceHoras = inicioCoordenadas + 5;
                int indiceComunicacion = indiceHoras + 2;
                if (indiceComunicacion >= fin ||
                    !TryMmsi(mensaje, inicioMmsiSocorro, fin, out string mmsiSocorro))
                    return resultado.AppendLine("Datos de socorro incompletos o MMSI inválido.").ToString();

                resultado.AppendLine($"MMSI Socorro: {mmsiSocorro}");
                resultado.AppendLine($"Tipo de emergencia: {Socorro.Peligro(mensaje[indicePeligro])} ({mensaje[indicePeligro]})");
                resultado.AppendLine($"Coordenadas: {Geografica.Posicion(Tomar(mensaje, inicioCoordenadas, 5))}");
                resultado.AppendLine($"UTC: {Utc(mensaje[indiceHoras], mensaje[indiceHoras + 1])}");
                resultado.AppendLine($"Siguiente Comunicación: {Socorro.PosteriorCom(mensaje[indiceComunicacion])} ({mensaje[indiceComunicacion]})");
            }
            else
            {
                int indiceSegundoTelemando = indicePrimerTelemando + 1;
                if (indiceSegundoTelemando >= fin)
                    return resultado.AppendLine("Segundo telemando y datos incompletos.").ToString();

                resultado.AppendLine($"Segundo Telemando: {General.SegundoTelemando(mensaje[indiceSegundoTelemando])} ({mensaje[indiceSegundoTelemando]})");
                int inicioDatos = indiceSegundoTelemando + 1;
                CampoFrecuencia? primero = LeerFrecuencia(mensaje, inicioDatos, fin);
                if (primero is null)
                {
                    resultado.AppendLine("Primer canal o frecuencia incompleto.");
                }
                else
                {
                    resultado.AppendLine($"{(primero.Value.EsCanal ? "Canal" : "Frecuencia")} Rx: {primero.Value.Descripcion}");
                    CampoFrecuencia? segundo = LeerFrecuencia(mensaje, inicioDatos + primero.Value.Consumidos, fin);
                    if (segundo is null)
                        resultado.AppendLine("Segundo canal o frecuencia incompleto.");
                    else
                        resultado.AppendLine($"{(segundo.Value.EsCanal ? "Canal" : "Frecuencia")} Tx: {segundo.Value.Descripcion}");
                }
            }

            resultado.AppendLine($"Fin: {General.ACK(mensaje[fin])} ({mensaje[fin]})");
            return resultado.ToString();
        }

        private static string DecodificarGeografica(IReadOnlyList<int> mensaje)
        {
            int fin = mensaje.Count - 1;
            if (General.ACK(mensaje[fin]) == "¿?")
                return "GEOGRÁFICA (102): falta un carácter de fin válido.";

            int inicioArea = mensaje.Count > 1 && mensaje[1] == 102 ? 2 : 1;
            int indiceCategoria = inicioArea + 5;
            int inicioMmsi = indiceCategoria + 1;
            int indicePrimerTelemando = inicioMmsi + 5;
            if (indicePrimerTelemando >= fin ||
                !TryMmsi(mensaje, inicioMmsi, fin, out string mmsi))
                return $"GEOGRÁFICA (102): encabezado incompleto o MMSI inválido ({mensaje.Count} caracteres).";

            int categoria = mensaje[indiceCategoria];
            int primerTelemando = mensaje[indicePrimerTelemando];
            var resultado = new StringBuilder();
            resultado.AppendLine($"Formato: {FormatSpecifier.Formato(102)}");
            resultado.AppendLine($"Área Geográfica: {AreaGeografica(mensaje, inicioArea)}");
            resultado.AppendLine($"Categoría: {General.Categoria(categoria)} ({categoria})");
            resultado.AppendLine($"MMSI: {mmsi}");
            resultado.AppendLine($"Primer Telemando: {General.PrimerTelemando(primerTelemando, out _)} ({primerTelemando})");

            if (categoria == 112)
            {
                int inicioMmsiSocorro = indicePrimerTelemando + 1;
                int indicePeligro = inicioMmsiSocorro + 5;
                int inicioCoordenadas = indicePeligro + 1;
                int indiceHoras = inicioCoordenadas + 5;
                int indiceComunicacion = indiceHoras + 2;
                if (indiceComunicacion >= fin ||
                    !TryMmsi(mensaje, inicioMmsiSocorro, fin, out string mmsiSocorro))
                    return resultado.AppendLine("Datos de socorro incompletos o MMSI inválido.").ToString();

                resultado.AppendLine($"MMSI Socorro: {mmsiSocorro}");
                resultado.AppendLine($"Tipo de emergencia: {Socorro.Peligro(mensaje[indicePeligro])} ({mensaje[indicePeligro]})");
                resultado.AppendLine($"Coordenadas: {Geografica.Posicion(Tomar(mensaje, inicioCoordenadas, 5))}");
                resultado.AppendLine($"UTC: {Utc(mensaje[indiceHoras], mensaje[indiceHoras + 1])}");
                resultado.AppendLine($"Siguiente Comunicación: {Socorro.PosteriorCom(mensaje[indiceComunicacion])} ({mensaje[indiceComunicacion]})");
            }
            else
            {
                int indiceSegundoTelemando = indicePrimerTelemando + 1;
                if (indiceSegundoTelemando >= fin)
                    return resultado.AppendLine("Segundo telemando y datos incompletos.").ToString();

                resultado.AppendLine($"Segundo Telemando: {General.SegundoTelemando(mensaje[indiceSegundoTelemando])} ({mensaje[indiceSegundoTelemando]})");
                int inicioDatos = indiceSegundoTelemando + 1;
                CampoFrecuencia? primero = LeerFrecuencia(mensaje, inicioDatos, fin);
                if (primero is null)
                {
                    resultado.AppendLine("Primer canal o frecuencia incompleto.");
                }
                else
                {
                    resultado.AppendLine($"{(primero.Value.EsCanal ? "Canal" : "Frecuencia")} Rx: {primero.Value.Descripcion}");
                    CampoFrecuencia? segundo = LeerFrecuencia(mensaje, inicioDatos + primero.Value.Consumidos, fin);
                    if (segundo is null)
                        resultado.AppendLine("Segundo canal o frecuencia incompleto.");
                    else
                        resultado.AppendLine($"{(segundo.Value.EsCanal ? "Canal" : "Frecuencia")} Tx: {segundo.Value.Descripcion}");
                }
            }

            resultado.AppendLine($"Fin: {General.ACK(mensaje[fin])} ({mensaje[fin]})");
            return resultado.ToString();
        }

        private static string DecodificarGrupo(IReadOnlyList<int> mensaje)
        {
            int fin = mensaje.Count - 1;
            if (General.ACK(mensaje[fin]) == "¿?")
                return "GRUPOS (114): falta un carácter de fin válido.";

            int inicioMmsiGrupo = mensaje.Count > 1 && mensaje[1] == 114 ? 2 : 1;
            int indiceCategoria = inicioMmsiGrupo + 5;
            int inicioMmsiTransmisor = indiceCategoria + 1;
            int indicePrimerTelemando = inicioMmsiTransmisor + 5;
            if (indicePrimerTelemando >= fin ||
                !TryMmsi(mensaje, inicioMmsiGrupo, fin, out string mmsiGrupo) ||
                !TryMmsi(mensaje, inicioMmsiTransmisor, fin, out string mmsiTransmisor))
                return $"GRUPOS (114): encabezado incompleto o MMSI inválido ({mensaje.Count} caracteres).";

            int categoria = mensaje[indiceCategoria];
            int primerTelemando = mensaje[indicePrimerTelemando];
            var resultado = new StringBuilder();
            resultado.AppendLine($"Formato: {FormatSpecifier.Formato(114)}");
            resultado.AppendLine($"MMSI: {mmsiGrupo}");
            resultado.AppendLine($"Categoría: {General.Categoria(categoria)} ({categoria})");
            resultado.AppendLine($"MMSI Transmisor: {mmsiTransmisor}");
            resultado.AppendLine($"Primer Telemando: {General.PrimerTelemando(primerTelemando, out _)} ({primerTelemando})");

            if (categoria == 112)
            {
                int inicioMmsiSocorro = indicePrimerTelemando + 1;
                int indicePeligro = inicioMmsiSocorro + 5;
                int inicioCoordenadas = indicePeligro + 1;
                int indiceHoras = inicioCoordenadas + 5;
                int indiceComunicacion = indiceHoras + 2;
                if (indiceComunicacion >= fin ||
                    !TryMmsi(mensaje, inicioMmsiSocorro, fin, out string mmsiSocorro))
                    return resultado.AppendLine("Datos de socorro incompletos o MMSI inválido.").ToString();

                resultado.AppendLine($"MMSI Socorro: {mmsiSocorro}");
                resultado.AppendLine($"Tipo de emergencia: {Socorro.Peligro(mensaje[indicePeligro])} ({mensaje[indicePeligro]})");
                resultado.AppendLine($"Coordenadas: {Geografica.Posicion(Tomar(mensaje, inicioCoordenadas, 5))}");
                resultado.AppendLine($"UTC: {Utc(mensaje[indiceHoras], mensaje[indiceHoras + 1])}");
                resultado.AppendLine($"Siguiente Comunicación: {Socorro.PosteriorCom(mensaje[indiceComunicacion])} ({mensaje[indiceComunicacion]})");
            }
            else
            {
                int indiceSegundoTelemando = indicePrimerTelemando + 1;
                if (indiceSegundoTelemando >= fin)
                    return resultado.AppendLine("Segundo telemando y datos incompletos.").ToString();

                resultado.AppendLine($"Segundo Telemando: {General.SegundoTelemando(mensaje[indiceSegundoTelemando])} ({mensaje[indiceSegundoTelemando]})");
                int inicioDatos = indiceSegundoTelemando + 1;
                CampoFrecuencia? primero = LeerFrecuencia(mensaje, inicioDatos, fin);
                if (primero is null)
                {
                    resultado.AppendLine("Primer canal o frecuencia incompleto.");
                }
                else
                {
                    resultado.AppendLine($"{(primero.Value.EsCanal ? "Canal" : "Frecuencia")} Rx: {primero.Value.Descripcion}");
                    CampoFrecuencia? segundo = LeerFrecuencia(mensaje, inicioDatos + primero.Value.Consumidos, fin);
                    if (segundo is null)
                        resultado.AppendLine("Segundo canal o frecuencia incompleto.");
                    else
                        resultado.AppendLine($"{(segundo.Value.EsCanal ? "Canal" : "Frecuencia")} Tx: {segundo.Value.Descripcion}");
                }
            }

            resultado.AppendLine($"Fin: {General.ACK(mensaje[fin])} ({mensaje[fin]})");
            return resultado.ToString();
        }

        // Próximas migraciones: estos formatos ya tienen una rama de decisión.
        private static string DecodificarAutomatica(IReadOnlyList<int> mensaje) => Pendiente(mensaje[0]);

        private static string Pendiente(int formato) =>
            $"Formato: {FormatSpecifier.Formato(formato)}{Environment.NewLine}Decodificación pendiente.";
    }
}
