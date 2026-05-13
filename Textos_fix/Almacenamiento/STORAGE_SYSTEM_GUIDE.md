# 📁 Sistema de Almacenamiento de Mensajes DSC

## 🎯 Resumen de la Solución

Se ha implementado un **sistema de logging dual** que guarda automáticamente los mensajes en **dos lugares**:
1. **Pantalla** (MAINDISPLAY - RichTextBox)
2. **Archivo** (Carpeta `Mensajes` en el directorio de la aplicación)

---

## 📦 Componentes Implementados

### 1. **Almacenamiento.cs** (Mejorado)

Ahora contiene:

#### Clase `Almacenamiento` (Instancia)
```csharp
public class Almacenamiento
{
    // Gestiona campos en memoria hasta que se guarden
    public void AgregarCampo(string clave, string valor)
    public void EstablecerFormato(string formato)
    public void GuardarMensaje()
    public void Limpiar()
}
```

#### Clase `MensajeLogger` (Estática)
```csharp
public static class MensajeLogger
{
    // Guarda los mensajes en archivos .txt con formato mejorado
    public static void Guardar(string formato, List<(string, string)> campos)
}
```

### 2. **DisplayLogger.cs** (Nuevo)

Clase principal que coordina pantalla + archivo:

```csharp
public class DisplayLogger
{
    public void Log(string message)              // Escribe en pantalla
    public void RegistrarCampo(string clave, string valor)  // Almacena campo
    public void EstablecerFormato(string formato)           // Establece tipo
    public void GuardarMensaje()                 // Guarda en archivo
    public void LimpiarDisplay()                 // Limpia pantalla
}
```

---

## 🏗️ Arquitectura

```
┌────────────────────────────────────────────────────────────┐
│                    DisplayLogger                           │
│  (Coordina pantalla + archivo)                             │
└────────────────────────────────────────────────────────────┘
         ↓                                    ↓
    ┌──────────────┐              ┌──────────────────────┐
    │ MAINDISPLAY  │              │  Almacenamiento      │
    │ (RichTextBox)│              │  (Campos en memoria) │
    └──────────────┘              └──────────────────────┘
    Thread-safe con                       ↓
    Invoke()                    ┌──────────────────────┐
                                │  MensajeLogger       │
                                │  (Escribe archivos)  │
                                └──────────────────────┘
                                        ↓
                              ┌──────────────────────┐
                              │  Carpeta "Mensajes"  │
                              │  (En bin/Debug)      │
                              └──────────────────────┘
```

---

## 📝 Flujo de Uso

### Flujo Normal

```
1. Mensaje llega → Procesamiento.Procesar()
2. Se decodifica → Se obtienen campos
3. displayLogger.Log(output) → Aparece en pantalla + se prepara para guardar
4. displayLogger.RegistrarCampo("clave", "valor") × N → Almacena campos
5. displayLogger.EstablecerFormato("SOCORRO") → Establece tipo
6. displayLogger.GuardarMensaje() → Guarda en archivo
7. displayLogger.LimpiarDisplay() → Limpia pantalla para siguiente
```

### Estructura de Archivo Generado

```
╔═══════════════════════════════════════════════════════╗
║        MENSAJE DSC - 11/05/2025 14:30:22.451        ║
╚═══════════════════════════════════════════════════════╝

Formato: SOCORRO
─────────────────────────────────────────────────────────

MMSI                     : 215217000
Categoría                : Buque de cabotaje
Tipo de emergencia       : Incendio
Coordenadas              : 36°30'N 003°00'W
UTC                      : 14:30:22
Siguiente Comunicación   : VHF Canal 16

─────────────────────────────────────────────────────────
Guardado: 11/05/2025 14:30:22.451
```

### Nombre del Archivo

```
DSC_ddMMyyyy_HHmmss_fff_TIPO.txt

Ejemplo:
DSC_11052025_143022_451_SOCORRO.txt
     ││││││││││││││││
     DD MM YYYY HH MM SS 0.001s FORMATO
```

---

## 💻 Cómo Usar en Procesamiento.cs

### Opción 1: Reemplazar LogToDisplay directamente

```csharp
public class Procesamiento
{
    private readonly DisplayLogger _logger;

    public Procesamiento(RichTextBox mainDisplay)
    {
        _logger = new DisplayLogger(mainDisplay);
    }

    private void LogToDisplay(string message)
    {
        _logger.Log(message);  // ← Escribe en pantalla
    }

    // Al final de cada mensaje:
    public void CompletarMensaje(string formato)
    {
        _logger.EstablecerFormato(formato);
        _logger.GuardarMensaje();  // ← Guarda en archivo
    }
}
```

### Opción 2: Registrar campos estructurados

```csharp
// En Metodos.cs o Expansion.cs
_logger.RegistrarCampo("MMSI", mmsi);
_logger.RegistrarCampo("Categoría", categoria);
_logger.RegistrarCampo("Coordenadas", coordenadas);
// ...
_logger.GuardarMensaje();  // Guarda todo junto
```

### Opción 3: Híbrida (Recomendada)

```csharp
// Mostrar en pantalla
_logger.Log($"MMSI: {mmsi}\n");

// Guardar de forma estructurada
_logger.RegistrarCampo("MMSI", mmsi);
```

---

## 🔒 Thread-Safety

### DisplayLogger
- ✅ Usa `lock` para acceso a MAINDISPLAY
- ✅ Usa `Invoke()` para marshaling al thread de UI
- ✅ Thread-safe desde cualquier thread

### Almacenamiento
- ✅ Usa `lock` para acceso a lista interna
- ✅ Seguro para acceso concurrente

### MensajeLogger
- ✅ Sin estado compartido (métodos estáticos)
- ✅ Maneja excepciones sin interrumpir

---

## 🎯 Ventajas de Esta Solución

✅ **Separación de Responsabilidades**
- DisplayLogger: Coordina UI + almacenamiento
- Almacenamiento: Gestiona memoria
- MensajeLogger: Escribe archivos

✅ **Thread-Safe**
- Usa locks y Invoke() donde es necesario
- Seguro desde thread de audio

✅ **Flexible**
- Puede usarse solo para pantalla
- Puede usarse solo para archivo
- Puede usarse para ambos

✅ **Mantenible**
- Código limpio y comentado
- Fácil de extender
- Patrón consistente

✅ **Robusto**
- Manejo de errores
- Creación automática de carpeta
- No interrumpe flujo si falla el guardado

---

## 📊 Ejemplo Completo

```csharp
// En Procesamiento.cs
public class Procesamiento
{
    private readonly DisplayLogger _logger;

    public Procesamiento(RichTextBox mainDisplay)
    {
        _logger = new DisplayLogger(mainDisplay);
    }

    public void Procesar(string input)
    {
        // ... decodificación ...

        _logger.LimpiarDisplay();  // Limpiar para nuevo mensaje
        _logger.Log($"╔════════════════════════════════════╗\n");
        _logger.Log($"║  MENSAJE DSC DECODIFICADO         ║\n");
        _logger.Log($"╚════════════════════════════════════╝\n\n");

        string formato = "SOCORRO";
        _logger.EstablecerFormato(formato);

        // Extraer datos...
        string mmsi = "215217000";
        string categoria = "Buque de cabotaje";

        // Escribir en pantalla
        _logger.Log($"MMSI: {mmsi}\n");
        _logger.Log($"Categoría: {categoria}\n");

        // Registrar para archivo
        _logger.RegistrarCampo("MMSI", mmsi);
        _logger.RegistrarCampo("Categoría", categoria);

        // Guardar en archivo
        _logger.GuardarMensaje();
    }
}
```

---

## 📂 Estructura de Carpeta de Mensajes

```
C:\...\bin\Debug\
├── net10.0\
│   ├── Demodulador_WinForm_1.exe
│   └── Mensajes\                    ← Carpeta generada automáticamente
│       ├── DSC_11052025_143022_451_SOCORRO.txt
│       ├── DSC_11052025_143045_123_INDIVIDUAL.txt
│       ├── DSC_11052025_143156_789_TODOS.txt
│       └── ...
```

---

## 🔧 Configuración y Ajustes

### Cambiar ubicación de carpeta

En `Almacenamiento.cs`, línea ~59:
```csharp
private static readonly string CarpetaBase =
    Path.Combine(AppContext.BaseDirectory, "MiCarpeta");  // ← Cambiar aquí
```

### Cambiar formato de archivo

En `MensajeLogger.cs`, línea ~45:
```csharp
string nombreArchivo = $"DSC_{ahora:ddMMyyyy_HHmmss_fff}_{SanitizarFormato(formato)}.txt";
// Cambiar formato de fecha si es necesario
```

### Cambiar contenido guardado

En `MensajeLogger.cs`, línea ~56-62:
```csharp
sb.AppendLine($"╔═══════════════════════════════════════════════════════╗");
// Personalizar encabezado, pie, etc.
```

---

## 🚀 Próximas Mejoras (Opcionales)

1. **Estadísticas**
   - Contar mensajes por tipo
   - Mostrar carpeta de mensajes

2. **Búsqueda**
   - Cargar archivo anterior
   - Buscar por MMSI
   - Filtrar por fecha

3. **Exportación**
   - Exportar a CSV/Excel
   - Generar reportes

4. **Base de Datos**
   - Guardar en SQLite en lugar de archivos
   - Queries para análisis

---

## ✅ Compilación

```
Status:      ✅ EXITOSO
Errores:     0
Warnings:    0
```

---

**Versión**: 1.0
**Plataforma**: .NET 10, C# 14.0
**Estado**: ✅ COMPLETADO Y LISTO PARA USAR
