# Ejemplo de Flujo Integrado: DisplayLogger + Procesamiento

## Escenario: Recepción de Mensaje de SOCORRO (Formato 112)

### Etapa 1: Captura de Audio
```
Audio RAW → BFSKDemodulator → Bit String
Ejemplo: "1010001100110101..."
```

### Etapa 2: Enqueue a Cola de Procesamiento
```csharp
_mensajesCapturados.Enqueue(capturado);
// En CapturaDatos.cs
```

### Etapa 3: Thread de Procesamiento Ejecuta Procesamiento.Procesar()
```csharp
procesamiento.Procesar("1010001100110101...", extensionDetected: true);
```

### Etapa 4: Fase 1-4 (Decodificación Interna)
```
Búsqueda Phasing → Extracción Formato → Decodificación → Verificación ECC
[DEBUG en MAINDISPLAY: "✓ ECC correcto"]
```

### Etapa 5: Determinación de Formato
```csharp
// Procesamiento.cs línea ~232
string formatoMensaje = DeterminarFormato(112);
// formatoMensaje = "SOCORRO"

_logger.EstablecerFormato("SOCORRO");

_logger.RegistrarCampo("Tipo", "SOCORRO");
_logger.RegistrarCampo("Formato ID", "112");
_logger.RegistrarCampo("Timestamp", "14/01/2025 14:30:25.123");
```

### Etapa 6: Procesamiento Específico de Formato
```csharp
case 112:
    datos_respuesta = _metodos.MSocorro(MENSAJE);
    // MSocorro() extrae MMSI, coordenadas, tipo emergencia, etc.
    // Cada LogToDisplay() en MSocorro ahora también almacena en DisplayLogger

    LogToDisplay("MMSI Transmisor: 123456789\n");
    LogToDisplay("MMSI Receptor: 987654321\n");
    LogToDisplay("Tipo Emergencia: FUEGO\n");
    // ↓
    // DisplayLogger almacena cada línea para posterior guardado
```

### Etapa 7: Guardado del Mensaje
```csharp
_logger.GuardarMensaje();
// ↓
// Almacenamiento.GuardarMensaje()
// ↓
// MensajeLogger.Guardar(formato, campos)
// ↓
// Crea archivo: DSC_140125_143025_123_SOCORRO.txt
```

### Etapa 8: Archivo Generado
```
Ruta: bin/Mensajes/DSC_140125_143025_123_SOCORRO.txt

Contenido:
╔════════════════════════════════════════════════════════════════════╗
║                    MENSAJE DSC DECODIFICADO                        ║
║                        SOCORRO                                      ║
╚════════════════════════════════════════════════════════════════════╝

Tipo:                 SOCORRO
Formato ID:           112
Timestamp:            14/01/2025 14:30:25.123
MMSI Transmisor:      123456789
MMSI Receptor:        987654321
Tipo Emergencia:      FUEGO
Coordenadas:          40°N 10°E
UTC:                  12:35
Comunicaciones:       RADIOTELÉFONO
ACK Requerido:        SÍ

Registrado:           14/01/2025 14:30:25.123
═══════════════════════════════════════════════════════════════════════
```

## Trace Completo de Métodos Llamados

```
CapturaDatos.DataAvailable()
  ├─ BFSKDemodulator.ProcessAudio()
  ├─ _mensajesCapturados.Enqueue(bits)
  └─ [Thread de Procesamiento detecta nuevos datos]
     │
     ├─ Procesamiento.Procesar(bits, extensionDetected)
     │  ├─ [Fase 1-4: Decodificación interna]
     │  ├─ LogToDisplay("✓ ECC correcto\n")
     │  │  └─ DisplayLogger.Log("✓ ECC correcto\n")
     │  │     └─ RichTextBox.AppendText() [UI actualizado]
     │  │     └─ Almacenamiento.AgregarCampo() [Guardado en memoria]
     │  │
     │  ├─ DeterminarFormato(112)  → "SOCORRO"
     │  ├─ _logger.EstablecerFormato("SOCORRO")
     │  ├─ _logger.RegistrarCampo("Tipo", "SOCORRO")
     │  │
     │  ├─ [Switch: case 112]
     │  ├─ _metodos.MSocorro(MENSAJE)
     │  │  ├─ LogToDisplay("MMSI Transmisor: 123456789\n")
     │  │  │  └─ DisplayLogger.Log() [varias veces]
     │  │  └─ return datos_respuesta
     │  │
     │  ├─ _logger.GuardarMensaje()
     │  │  └─ Almacenamiento.GuardarMensaje()
     │  │     └─ MensajeLogger.Guardar(formato, campos)
     │  │        └─ File.WriteAllText(ruta, contenido) [Archivo persistido]
     │  │
     │  └─ [Fase 6: Si hay extensión, procesa _expansion.Decodificar()]
     │
     └─ [Siguiente mensaje en cola si existe]
```

## Puntos Clave de Integración

### ✅ Esto funciona automáticamente:
1. **UI actualizada en tiempo real** → `LogToDisplay()` → `DisplayLogger.Log()`
2. **Campos almacenados** → Cada `LogToDisplay()` registra datos
3. **Archivo generado** → `GuardarMensaje()` escribe TXT
4. **Thread-safe** → Uso de `Invoke()` y locks internos

### ⚡ Sin cambios necesarios en:
- `Metodos.cs` → Sigue usando callback `LogToDisplay`
- `Expansion.cs` → Sigue usando callback `LogToDisplay`
- `CapturaDatos.cs` → Sigue encolando mensajes

### 🔄 Lo que cambió:
- `Procesamiento.cs` → Ahora integra `DisplayLogger`
- `LogToDisplay()` → Ahora delega a `DisplayLogger.Log()`
- Fase 5 (Procesamiento) → Ahora registra formato y guarda

## Testing Manual

1. **Abrir aplicación**
2. **Seleccionar dispositivo de audio (VHF o MF/HF)**
3. **Iniciar captura** → Demodulador procesa señal
4. **Esperar decodificación** → MAINDISPLAY muestra resultado
5. **Verificar archivo** → `bin/Mensajes/DSC_ddMMyyyy_HHmmss_fff_FORMATO.txt`

### Validación de Guardado:
- ✅ Archivo existe en carpeta Mensajes
- ✅ Contenido incluye Tipo, Formato ID, Timestamp
- ✅ Datos específicos del formato (MMSI, coords, etc.)
- ✅ Timestamp coincide con decodificación

## Estructura de Datos en Flujo

```
┌─────────────────────────────────────────────────┐
│ RichTextBox MAINDISPLAY (UI)                    │
│ Actualizado en tiempo real por DisplayLogger    │
└─────────────────────────────────────────────────┘
                      ↑
                      │ (Invoke si necesario)
                      │
┌─────────────────────────────────────────────────┐
│ DisplayLogger                                   │
│ ├─ Escribe en MAINDISPLAY                       │
│ └─ Almacena campos en Almacenamiento            │
└─────────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────┐
│ Almacenamiento                                  │
│ ├─ _campos: Dictionary<string, string>          │
│ ├─ _formato: string                             │
│ └─ Proporciona datos a MensajeLogger            │
└─────────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────┐
│ MensajeLogger (static)                          │
│ └─ Guarda archivo en bin/Mensajes/              │
└─────────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────┐
│ Sistema de Archivos                             │
│ DSC_140125_143025_123_SOCORRO.txt               │
└─────────────────────────────────────────────────┘
```
