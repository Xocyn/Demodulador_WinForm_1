using System;
using System.Collections.Generic;
using System.Text;

namespace Demodulador_WinForm_1.Migrado
{
    /// <summary>
    /// Almacena mensajes DSC en memoria y los guarda en archivo cuando se completan.
    /// Thread-safe con locks para acceso desde múltiples threads.
    /// </summary>
    internal class Almacenamiento
    {
        private readonly List<(string Clave, string Valor)> _camposActuales = new();
        private readonly object _lock = new object();
        private string _formatoActual = "DESCONOCIDO";

        /// <summary>
        /// Agrega un campo al mensaje actual que se está construyendo.
        /// </summary>
        public void AgregarCampo(string clave, string valor)
        {
            lock (_lock)
            {
                _camposActuales.Add((clave, valor));
            }
        }

        /// <summary>
        /// Establece el formato del mensaje actual.
        /// </summary>
        public void EstablecerFormato(string formato)
        {
            lock (_lock)
            {
                _formatoActual = formato;
            }
        }

        /// <summary>
        /// Obtiene todos los campos del mensaje actual en orden.
        /// </summary>
        public List<(string Clave, string Valor)> ObtenerCampos()
        {
            lock (_lock)
            {
                return new List<(string, string)>(_camposActuales);
            }
        }

        /// <summary>
        /// Obtiene el formato actual del mensaje.
        /// </summary>
        public string ObtenerFormato()
        {
            lock (_lock)
            {
                return _formatoActual;
            }
        }

        /// <summary>
        /// Limpia el almacenamiento después de guardar o cuando termina un mensaje.
        /// </summary>
        public void Limpiar()
        {
            lock (_lock)
            {
                _camposActuales.Clear();
                _formatoActual = "DESCONOCIDO";
            }
        }

        /// <summary>
        /// Guarda el mensaje actual en archivo.
        /// </summary>
        public void GuardarMensaje()
        {
            lock (_lock)
            {
                if (_camposActuales.Count > 0)
                {
                    MensajeLogger.Guardar(_formatoActual, new List<(string, string)>(_camposActuales));
                }
            }
        }
    }

    /// <summary>
    /// Guarda cada mensaje DSC decodificado como un archivo .txt
    /// en la carpeta "Mensajes" dentro del directorio del ejecutable.
    /// 
    /// Nombre de archivo: DSC_ddMMyyyy_HHmmss_fff_FORMATO.txt
    /// Ejemplo:           DSC_11052025_143022_451_SOCORRO.txt
    /// </summary>
    internal static class MensajeLogger
    {
        // ── Carpeta destino ───────────────────────────────────────────────────────
        // AppContext.BaseDirectory apunta al directorio del .exe (bin/Debug o bin/Release).
        private static readonly string CarpetaBase =
            Path.Combine(AppContext.BaseDirectory, "Mensajes");

        // ── EnsureFolder ─────────────────────────────────────────────────────────
        // Crea la carpeta si no existe. Se llama en cada escritura por si fue borrada.
        private static void EnsureFolder()
        {
            if (!Directory.Exists(CarpetaBase))
                Directory.CreateDirectory(CarpetaBase);
        }

        // ── Guardar ───────────────────────────────────────────────────────────────
        /// <summary>
        /// Escribe el mensaje decodificado en un archivo .txt.
        /// </summary>
        /// <param name="formato">Nombre legible del formato DSC (ej: "SOCORRO", "INDIVIDUAL").</param>
        /// <param name="campos">
        ///   Pares clave-valor con los datos decodificados en el orden en que deben aparecer.
        ///   Usar un List para preservar el orden de inserción.
        /// </param>
        public static void Guardar(string formato, List<(string Clave, string Valor)> campos)
        {
            try
            {
                EnsureFolder();

                DateTime ahora = DateTime.Now;

                // ── Nombre de archivo ─────────────────────────────────────────────
                // Incluye milisegundos para evitar colisiones si llegan mensajes juntos.
                string nombreArchivo = $"DSC_{ahora:dd_MM_yyyy_HH_mm_ss}_{SanitizarFormato(formato)}.txt";
                string rutaCompleta = Path.Combine(CarpetaBase, nombreArchivo);

                // ── Contenido ─────────────────────────────────────────────────────
                var sb = new StringBuilder();

                foreach (var (clave, valor) in campos)
                    sb.AppendLine($"{clave,-25}: {valor}");

                sb.AppendLine();
                sb.AppendLine($"─────────────────────────────────────────────────────────");
                sb.AppendLine($"Guardado: {DateTime.Now:dd/MM/yyyy HH:mm:ss.fff}");

                File.WriteAllText(rutaCompleta, sb.ToString(), Encoding.UTF8);

#if DEBUG
                Console.WriteLine($"[Logger] Mensaje guardado → {nombreArchivo}");
#endif
            }
            catch (Exception ex)
            {
                // No interrumpir el flujo principal si el guardado falla.
                Console.WriteLine($"[Logger] Error al guardar mensaje: {ex.Message}");
            }
        }

        // ── SanitizarFormato ──────────────────────────────────────────────────────
        // Elimina caracteres no válidos en nombres de archivo.
        private static string SanitizarFormato(string formato)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                formato = formato.Replace(c, '_');
            return formato.Replace(' ', '_').ToUpper();
        }
    }
}
