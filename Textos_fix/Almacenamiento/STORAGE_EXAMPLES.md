# 💡 Ejemplos - Sistema de Almacenamiento

## Ejemplo 1: Uso Básico en Procesamiento

```csharp
public class Procesamiento
{
    private readonly DisplayLogger _logger;
    private readonly Metodos _metodos;
    private readonly Expansion _expansion;

    public Procesamiento(RichTextBox mainDisplay)
    {
        _logger = new DisplayLogger(mainDisplay);
        _metodos = new Metodos(_logger.Log);
        _expansion = new Expansion(_logger.Log);
    }

    public void Procesar(string input, bool ext)
    {
        try
        {
            // ... decodificación ...

            _logger.EstablecerFormato(FormatSpecifier.Formato(format));

            // Mensajes de estado
            _logger.Log($"MMSI: {mmsi}\n");
            _logger.Log($"Categoría: {categoria}\n");

            // Guardar en archivo
            _logger.GuardarMensaje();
        }
        catch (Exception ex)
        {
            _logger.Log($"❌ Error: {ex.Message}\n");
        }
    }
}
```

---

## Ejemplo 2: Mensajes de Socorro (Tipo 112)

```csharp
// En Metodos.cs
private void MSocorro(List<int> mensaje)
{
    // ... decodificación ...

    string formato = "SOCORRO";
    _log($"═══════════════════════════════════\n");
    _log($"ALERTA DE S.O.S DETECTADA\n");
    _log($"═══════════════════════════════════\n");
    _log($"MMSI: {mmsi}\n");
    _log($"Tipo de emergencia: {tipoEmergencia}\n");
    _log($"Coordenadas: {coordenadas}\n");

    // Registrar campos para archivo
    if (_logger is DisplayLogger dl)
    {
        dl.EstablecerFormato(formato);
        dl.RegistrarCampo("MMSI", mmsi);
        dl.RegistrarCampo("Emergencia", tipoEmergencia);
        dl.RegistrarCampo("Coordenadas", coordenadas);
        dl.GuardarMensaje();
    }
}
```

---

## Ejemplo 3: Mensajes Individuales (Tipo 120)

```csharp
private void MIndividual(List<int> mensaje)
{
    // ... decodificación ...

    _log($"╔════════════════════════════════╗\n");
    _log($"║   MENSAJE INDIVIDUAL RECIBIDO   ║\n");
    _log($"╚════════════════════════════════╝\n\n");

    _log($"MMSI Origen: {mmsiOrigen}\n");
    _log($"MMSI Destino: {mmsiDestino}\n");
    _log($"Telemando: {telemando}\n");

    // Guardar
    if (_logger is DisplayLogger dl)
    {
        dl.EstablecerFormato("INDIVIDUAL");
        dl.RegistrarCampo("MMSI Origen", mmsiOrigen);
        dl.RegistrarCampo("MMSI Destino", mmsiDestino);
        dl.RegistrarCampo("Telemando", telemando);
        dl.GuardarMensaje();
    }
}
```

---

## Ejemplo 4: Mensaje Geográfico (Tipo 102)

```csharp
private void MGeografica(List<int> mensaje)
{
    // ... decodificación ...

    _log($"╔════════════════════════════════╗\n");
    _log($"║   INFORMACIÓN GEOGRÁFICA        ║\n");
    _log($"╚════════════════════════════════╝\n\n");

    _log($"MMSI: {mmsi}\n");
    _log($"Posición: {latitud} / {longitud}\n");
    _log($"Fecha: {fecha}\n");
    _log($"Hora: {hora}\n");

    // Guardar con todos los campos
    if (_logger is DisplayLogger dl)
    {
        dl.EstablecerFormato("GEOGRAFICA");
        dl.RegistrarCampo("MMSI", mmsi);
        dl.RegistrarCampo("Latitud", latitud);
        dl.RegistrarCampo("Longitud", longitud);
        dl.RegistrarCampo("Fecha", fecha);
        dl.RegistrarCampo("Hora", hora);
        dl.RegistrarCampo("Altitud", altitud);
        dl.RegistrarCampo("Velocidad", velocidad);
        dl.GuardarMensaje();
    }
}
```

---

## Ejemplo 5: Logging Extensión (Tipo 100-106)

```csharp
// En Expansion.cs
public void Decodificar(List<int> EXTENSION)
{
    _log($"\n╔════════════════════════════════╗\n");
    _log($"║   EXTENSIÓN DETECTADA          ║\n");
    _log($"╚════════════════════════════════╝\n\n");

    // ... decodificación ...

    // Registrar campos si es disponible
    if (valor != "Sin datos")
    {
        _log($"Dato: {valor}\n");
        // Nota: Para guardar extensiones, sería mejor en una clase separada
    }
}
```

---

## Ejemplo 6: Vista de Folder de Mensajes

```csharp
// Agregar en Form1.cs
private void AbrirCarpetaMensajes()
{
    try
    {
        string carpetaMensajes = Path.Combine(
            AppContext.BaseDirectory, 
            "Mensajes"
        );

        if (!Directory.Exists(carpetaMensajes))
        {
            MessageBox.Show("Aún no hay mensajes guardados.", "Información");
            return;
        }

        // Abrir carpeta en explorador
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo()
            {
                FileName = carpetaMensajes,
                UseShellExecute = true,
                Verb = "open"
            }
        );
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error al abrir carpeta: {ex.Message}", "Error");
    }
}
```

---

## Ejemplo 7: Estadísticas de Mensajes

```csharp
public class EstadisticasMensajes
{
    public static void MostrarEstadisticas()
    {
        try
        {
            string carpetaMensajes = Path.Combine(
                AppContext.BaseDirectory,
                "Mensajes"
            );

            if (!Directory.Exists(carpetaMensajes))
                return;

            var archivos = Directory.GetFiles(carpetaMensajes, "*.txt");
            var porTipo = new Dictionary<string, int>();

            foreach (var archivo in archivos)
            {
                string nombre = Path.GetFileNameWithoutExtension(archivo);
                string tipo = nombre.Split('_').Last();  // Último underscore

                if (porTipo.ContainsKey(tipo))
                    porTipo[tipo]++;
                else
                    porTipo[tipo] = 1;
            }

            Console.WriteLine("\n═══ ESTADÍSTICAS DE MENSAJES ═══");
            Console.WriteLine($"Total: {archivos.Length}");
            foreach (var kvp in porTipo)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }
            Console.WriteLine("═════════════════════════════════\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
```

---

## Ejemplo 8: Carga de Archivo Anterior

```csharp
public class CargadorMensajes
{
    public static string CargarUltimoMensaje()
    {
        try
        {
            string carpetaMensajes = Path.Combine(
                AppContext.BaseDirectory,
                "Mensajes"
            );

            if (!Directory.Exists(carpetaMensajes))
                return null;

            var archivos = Directory.GetFiles(carpetaMensajes, "*.txt")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .FirstOrDefault();

            if (archivos == null)
                return null;

            return File.ReadAllText(archivos, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }

    public static List<string> CargarTodosMensajes(string tipoFiltro = null)
    {
        try
        {
            string carpetaMensajes = Path.Combine(
                AppContext.BaseDirectory,
                "Mensajes"
            );

            var archivos = Directory.GetFiles(carpetaMensajes, "*.txt");

            if (!string.IsNullOrEmpty(tipoFiltro))
            {
                archivos = archivos
                    .Where(f => f.Contains(tipoFiltro.ToUpper()))
                    .ToArray();
            }

            var mensajes = new List<string>();
            foreach (var archivo in archivos)
            {
                mensajes.Add(File.ReadAllText(archivo, Encoding.UTF8));
            }

            return mensajes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return new List<string>();
        }
    }
}
```

---

## Ejemplo 9: Búsqueda por MMSI

```csharp
public class BuscadorMensajes
{
    public static List<string> BuscarPorMMSI(string mmsi)
    {
        try
        {
            string carpetaMensajes = Path.Combine(
                AppContext.BaseDirectory,
                "Mensajes"
            );

            var archivos = Directory.GetFiles(carpetaMensajes, "*.txt");
            var resultados = new List<string>();

            foreach (var archivo in archivos)
            {
                string contenido = File.ReadAllText(archivo, Encoding.UTF8);

                if (contenido.Contains(mmsi))
                {
                    resultados.Add(contenido);
                }
            }

            return resultados;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return new List<string>();
        }
    }

    public static List<string> BuscarPorFecha(DateTime fecha)
    {
        try
        {
            string carpetaMensajes = Path.Combine(
                AppContext.BaseDirectory,
                "Mensajes"
            );

            string patronFecha = fecha.ToString("ddMMyyyy");
            var archivos = Directory.GetFiles(carpetaMensajes, $"*{patronFecha}*.txt");

            var resultados = new List<string>();
            foreach (var archivo in archivos)
            {
                resultados.Add(File.ReadAllText(archivo, Encoding.UTF8));
            }

            return resultados;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return new List<string>();
        }
    }
}
```

---

## Ejemplo 10: Exportar a CSV

```csharp
public class ExportadorCSV
{
    public static void ExportarACSV(string rutaDestino)
    {
        try
        {
            string carpetaMensajes = Path.Combine(
                AppContext.BaseDirectory,
                "Mensajes"
            );

            var archivos = Directory.GetFiles(carpetaMensajes, "*.txt");

            using (var writer = new StreamWriter(rutaDestino, false, Encoding.UTF8))
            {
                // Encabezado
                writer.WriteLine("Fecha,Hora,MMSI,Formato,Archivo");

                foreach (var archivo in archivos)
                {
                    string nombre = Path.GetFileNameWithoutExtension(archivo);
                    string[] partes = nombre.Split('_');

                    if (partes.Length >= 5)
                    {
                        string fecha = $"{partes[1]}/{partes[2]}/{partes[3]}";  // dd/MM/yyyy
                        string hora = $"{partes[4].Substring(0, 2)}:{partes[4].Substring(2, 2)}:{partes[4].Substring(4, 2)}";
                        string formato = string.Join("_", partes.Skip(5));

                        // Leer MMSI del archivo
                        string contenido = File.ReadAllText(archivo);
                        string mmsi = "N/A";

                        var lineas = contenido.Split('\n');
                        foreach (var linea in lineas)
                        {
                            if (linea.Contains("MMSI"))
                            {
                                var partesMmsi = linea.Split(':');
                                if (partesMmsi.Length > 1)
                                    mmsi = partesMmsi[1].Trim();
                                break;
                            }
                        }

                        writer.WriteLine($"\"{fecha}\",\"{hora}\",\"{mmsi}\",\"{formato}\",\"{nombre}.txt\"");
                    }
                }
            }

            MessageBox.Show($"Exportado a: {rutaDestino}", "Éxito");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error");
        }
    }
}
```

---

## Uso en Form1.cs

```csharp
public partial class Demodulador_DSC : Form
{
    private CapturaDatos _capturaDatos;
    private DisplayLogger _displayLogger;

    public Demodulador_DSC()
    {
        InitializeComponent();
        _displayLogger = new DisplayLogger(MAINDISPLAY);
        _capturaDatos = new CapturaDatos(this, _displayLogger);
    }

    // Botón para abrir carpeta de mensajes
    private void btnAbrirMensajes_Click(object sender, EventArgs e)
    {
        string carpeta = Path.Combine(AppContext.BaseDirectory, "Mensajes");
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
        {
            FileName = carpeta,
            UseShellExecute = true
        });
    }

    // Botón para mostrar estadísticas
    private void btnEstadisticas_Click(object sender, EventArgs e)
    {
        EstadisticasMensajes.MostrarEstadisticas();
    }
}
```

---

**Versión**: 1.0
**Ejemplos**: 10 casos de uso
**Status**: ✅ LISTOS PARA USAR
