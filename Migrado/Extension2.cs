using System;
using System.Collections.Generic;
using System.Text;
using Dem_v2;

namespace Demodulador_WinForm_1.Migrado
{
    internal static class Extension2
    {
        public static string Decodificar(IReadOnlyList<int> caracteres)
        {
            ArgumentNullException.ThrowIfNull(caracteres);
            if (caracteres.Count == 0 || General.ACK(caracteres[^1]) == "¿?")
                return "Extensión incompleta: falta un carácter de fin válido.";

            int fin = caracteres.Count - 1;
            int indice = 0;
            var resultado = new StringBuilder();
            resultado.AppendLine("Extensión:");

            while (indice < fin)
            {
                int especificador = caracteres[indice++];
                if (especificador is < 100 or > 106)
                    return resultado.AppendLine($"Especificador no reconocido: {especificador}.").ToString();
                if (indice >= fin)
                    return resultado.AppendLine($"Datos incompletos para el especificador {especificador}.").ToString();

                if (caracteres[indice] is 110 or 126)
                {
                    string estado = caracteres[indice++] == 110
                        ? "Petición de datos" : "Ningún dato disponible";
                    resultado.AppendLine($"{Nombre(especificador)}: {estado}");
                    continue;
                }

                int cantidad = especificador switch
                {
                    100 => 4,
                    101 => 3,
                    102 or 103 or 106 => 2,
                    104 => 10,
                    105 => 12,
                    _ => 0
                };
                if (indice + cantidad > fin)
                    return resultado.AppendLine($"Datos incompletos para el especificador {especificador}.").ToString();

                switch (especificador)
                {
                    case 100:
                    {
                        string latitud = Digitos(caracteres, indice, 2);
                        string longitud = Digitos(caracteres, indice + 2, 2);
                        resultado.AppendLine($"Mejora de Latitud: {latitud}''");
                        resultado.AppendLine($"Mejora de Longitud: {longitud}''");
                        break;
                    }
                    case 101:
                    {
                        string dispositivo = caracteres[indice] switch
                        {
                            0 => "NO VÁLIDO",
                            1 => "GPS diferencial",
                            2 => "GPS sin corregir",
                            3 => "LORAN-C diferencial",
                            4 => "LORAN-C sin corregir",
                            5 => "GLONASS",
                            6 => "Punto de referencia de radar",
                            7 => "DECCA",
                            8 => "Otra referencia",
                            _ => "¿?"
                        };
                        string referencia = caracteres[indice + 2] switch
                        {
                            0 => "WGS-84",
                            1 => "WGS-72",
                            2 => "Otro",
                            _ => "¿?"
                        };
                        string precision = caracteres[indice + 1] is >= 0 and <= 99
                            ? caracteres[indice + 1].ToString("D2") : "¿?";
                        resultado.AppendLine($"Datos de posición procedentes de: {dispositivo}");
                        resultado.AppendLine($"Precisión del punto de referencia: {precision[0]},{precision[1]}");
                        resultado.AppendLine($"Punto de referencia: {referencia}");
                        break;
                    }
                    case 102:
                        resultado.AppendLine($"Velocidad actual del barco: {DecimalConComa(caracteres, indice)} nudos");
                        break;
                    case 103:
                        resultado.AppendLine($"Ruta actual del barco: {DecimalConComa(caracteres, indice)} grados");
                        break;
                    case 104:
                    {
                        var identificador = new StringBuilder();
                        for (int i = indice; i < indice + 10; i++)
                            identificador.Append(Caracter(caracteres[i]));
                        resultado.AppendLine($"Identificador adicional: {identificador}");
                        break;
                    }
                    case 105:
                    {
                        resultado.AppendLine($"Mejora de Latitud: ,{Digitos(caracteres, indice, 2)}''");
                        resultado.AppendLine($"Mejora de Longitud: ,{Digitos(caracteres, indice + 2, 2)}''");
                        resultado.AppendLine($"Resolución adicional ventana vertical: {Digitos(caracteres, indice + 4, 2)}");
                        resultado.AppendLine($"Resolución adicional ventana horizontal: {Digitos(caracteres, indice + 6, 2)}");
                        resultado.AppendLine($"Velocidad actual del barco: {ValorAmpliado(caracteres, indice + 8, "nudos")}");
                        resultado.AppendLine($"Trayectoria actual del barco: {ValorAmpliado(caracteres, indice + 10, "grados")}");
                        break;
                    }
                    case 106:
                        resultado.AppendLine($"Número de personas a bordo: {Digitos(caracteres, indice, 2)}");
                        break;
                }

                indice += cantidad;
            }

            resultado.AppendLine($"Fin de extensión: {General.ACK(caracteres[fin])} ({caracteres[fin]})");
            return resultado.ToString();
        }

        private static string Nombre(int especificador) => especificador switch
        {
            100 => "Resolución mejorada de posición",
            101 => "Origen y punto de referencia de posición",
            102 => "Velocidad actual del barco",
            103 => "Ruta actual del barco",
            104 => "Identificador adicional de la estación",
            105 => "Zona geográfica ampliada",
            106 => "Número de personas a bordo",
            _ => "Instrucción"
        };

        private static string Digitos(IReadOnlyList<int> caracteres, int inicio, int cantidad)
        {
            var resultado = new StringBuilder(cantidad * 2);
            for (int i = inicio; i < inicio + cantidad; i++)
            {
                if (caracteres[i] is < 0 or > 99)
                    return "¿?";
                resultado.Append(caracteres[i].ToString("D2"));
            }
            return resultado.ToString();
        }

        private static string DecimalConComa(IReadOnlyList<int> caracteres, int inicio)
        {
            string digitos = Digitos(caracteres, inicio, 2);
            return digitos.Length == 4 ? $"{digitos[..3]},{digitos[3]}" : "¿?";
        }

        private static string ValorAmpliado(IReadOnlyList<int> caracteres, int inicio, string unidad)
        {
            if (caracteres[inicio] == 126 || caracteres[inicio + 1] == 126)
                return "Sin información";
            return $"{DecimalConComa(caracteres, inicio)} {unidad}";
        }

        private static string Caracter(int valor) => valor switch
        {
            >= 0 and <= 9 => valor.ToString(),
            10 => "Sin utilizar",
            >= 11 and <= 36 => ((char)('A' + valor - 11)).ToString(),
            37 => ".",
            38 => ",",
            39 => "-",
            40 => "/",
            41 => " ",
            _ => "¿¿??"
        };
    }
}
