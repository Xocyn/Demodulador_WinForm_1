# Guía de Integración: DisplayLogger en Procesamiento.cs

## Resumen de Cambios

Se ha integrado el sistema de persistencia `DisplayLogger` en la clase `Procesamiento.cs` para que cada mensaje DSC decodificado se guarde automáticamente en archivo, además de mostrarse en la interfaz.

## Cambios Realizados

### 1. **Agregado campo DisplayLogger**
```csharp
private readonly DisplayLogger _logger;
```

### 2. **Inicialización en Constructor**
```csharp
public Procesamiento(RichTextBox mainDisplay)
{
    _mainDisplay = mainDisplay;
    _logger = new DisplayLogger(mainDisplay);  // ← NUEVO
    _metodos = new Metodos(LogToDisplay);
    _expansion = new Expansion(LogToDisplay);
}
```

### 3. **Actualizado LogToDisplay**
El método ahora delega al `DisplayLogger`:
```csharp
private void LogToDisplay(string message)
{
    _logger.Log(message);  // ← Escribe en UI Y almacena
}
```

### 4. **Integración en Procesamiento**
Antes de procesar cada mensaje (Fase 5), se:
- Determina el formato del mensaje
- Establece el formato en el logger
- Registra campos básicos (Tipo, ID, Timestamp)
- Al finalizar, llama a `GuardarMensaje()` para persistir

```csharp
// Determinar formato y establecerlo en el logger
string formatoMensaje = DeterminarFormato(MENSAJE[0]);
_logger.EstablecerFormato(formatoMensaje);

// Registrar campos básicos
_logger.RegistrarCampo("Tipo", formatoMensaje);
_logger.RegistrarCampo("Formato ID", MENSAJE[0].ToString());
_logger.RegistrarCampo("Timestamp", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff"));

// ... procesamiento específico del formato ...

// ── Guardar mensaje en archivo ────────────────────────────────────
_logger.GuardarMensaje();
```

### 5. **Nuevo Método Helper**
Agregado método `DeterminarFormato()` para traducir IDs a nombres descriptivos:
```csharp
private string DeterminarFormato(int formatoId)
{
    return formatoId switch
    {
        102 => "GEOGRÁFICA",
        112 => "SOCORRO",
        114 => "GRUPOS",
        116 => "TODOS LOS BARCOS",
        120 => "INDIVIDUAL",
        123 => "SEGURIDAD",
        _ => $"DESCONOCIDO ({formatoId})"
    };
}
```

## Flujo de Datos

```
Audio Capturado
    ↓
CapturaDatos demodula bits
    ↓
Procesamiento.Procesar(bits) ← AQUÍ ESTAMOS
    ↓
LogToDisplay() ← Escribe en UI
    ↓
DisplayLogger.Log() ← Almacena en memoria
    ↓
Al finalizar: GuardarMensaje()
    ↓
MensajeLogger escribe archivo TXT en bin/Mensajes/
    ↓
Archivo: DSC_ddMMyyyy_HHmmss_fff_FORMATO.txt
```

## Archivos Guardados

Los mensajes se guardan en: `bin/Mensajes/`

Ejemplo de archivo generado:
```
DSC_250114_143025_123_SOCORRO.txt
```

Contenido:
```
╔════════════════════════════════════════════════════════════════════╗
║                    MENSAJE DSC DECODIFICADO                        ║
║                        SOCORRO                                      ║
╚════════════════════════════════════════════════════════════════════╝

Tipo:              SOCORRO
Formato ID:        112
Timestamp:         14/01/2025 14:30:25.123
MMSI Transmisor:   123456789
MMSI Receptor:     987654321
...

Registrado:        14/01/2025 14:30:25.123
═══════════════════════════════════════════════════════════════════════
```

## Thread-Safety

- **DisplayLogger.Log()**: usa `Invoke()` para escribir en UI desde cualquier thread
- **Almacenamiento de campos**: protegido con locks internos
- **Guardado de archivo**: operación thread-safe delegada a MensajeLogger

## Próximos Pasos (Opcional)

1. Enriquecer los campos registrados con información específica de cada formato (MMSI, coordenadas, etc.)
2. Agregar botón en UI para abrir la carpeta de mensajes guardados
3. Mostrar notificación visual cuando un mensaje se guardó exitosamente
4. Implementar búsqueda/filtro en archivos guardados

## Validación

✅ Compilación correcta  
✅ DisplayLogger integrado en Procesamiento  
✅ LogToDisplay ahora usa DisplayLogger  
✅ GuardarMensaje() llamado al finalizar decodificación  
✅ Thread-safe mediante callbacks y locks  
