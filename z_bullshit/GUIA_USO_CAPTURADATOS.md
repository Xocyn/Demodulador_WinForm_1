# Guía de Uso - CapturaDatos Refactorizado

## 📌 Inicialización

```csharp
// En tu formulario principal (Demodulador_DSC)
private CapturaDatos _capturaDatos;

public Demodulador_DSC()
{
	InitializeComponent();

	// Crear instancia con referencias necesarias
	var procesamiento = new Procesamiento(MAINDISPLAY, this);
	_capturaDatos = new CapturaDatos(procesamiento, this);
}
```

## 🎬 Comenzar Captura

```csharp
private void btnIniciarCaptura_Click(object sender, EventArgs e)
{
	try
	{
		_capturaDatos.IniciarCaptura();

		// El log mostrará:
		// [Iniciada captura de audio]
		// Esperando señal...
	}
	catch (Exception ex)
	{
		MessageBox.Show($"Error al iniciar captura: {ex.Message}");
	}
}
```

**Qué sucede internamente:**
1. Se crea `_audioBufferQueue` (cola de buffers)
2. Se crea `audioProcessingThread` (thread de procesamiento)
3. Se inicia grabación de audio con NAudio
4. El callback comienza a depositar buffers en la cola

## ⏸️ Pausar Captura

```csharp
private void btnPausar_Click(object sender, EventArgs e)
{
	_capturaDatos.PausarCaptura(true);
	// Log: [Captura pausada]
}

private void btnReanudar_Click(object sender, EventArgs e)
{
	_capturaDatos.PausarCaptura(false);
	// Log: [Captura reanudada]
}
```

**Nota**: La pausa **no pierde buffers**. Cuando se reanuda, todo continúa normalmente.

## 🛑 Detener Captura

```csharp
private void btnDetener_Click(object sender, EventArgs e)
{
	_capturaDatos.DetenerCaptura();

	// El método hace:
	// 1. Señala cancelación con CancellationToken
	// 2. Detiene NAudio
	// 3. Libera cola de buffers
	// 4. Espera a threads (máx 2 segundos)
	// 5. Log: [Detenida captura de audio]
}
```

## 📨 Procesar Mensajes Capturados

```csharp
private void btnProcesarMensajes_Click(object sender, EventArgs e)
{
	var mensajes = _capturaDatos.ObtenerMensajes();

	foreach (var mensaje in mensajes)
	{
		MAINDISPLAY.AppendText($"Mensaje de {mensaje.Length} bits\n");
		// El mensaje ya fue enviado al thread de procesamiento
	}
}
```

## 🔍 Entender el Flujo de Ejecución

### Timeline de una Captura Exitosa:

```
t=0ms  → [Iniciada captura de audio]

t=1ms  → NAudio comienza grabación
		 callback DataAvailable es llamado cada ~10ms

t=10ms → callback recibe buffer #1
		 → lo copia
		 → lo deposita en _audioBufferQueue (~1ms total)

t=12ms → audioProcessingThread toma buffer #1
		 → demodula audio
		 → acumula bits
		 → busca patrón de sincronización

t=20ms → callback recibe buffer #2
		 → lo copia y encola

t=50ms → audioProcessingThread detecta DOT PATTERN
		 → [IniciarGrabacion] Fase 0 bloqueada

t=100ms → audioProcessingThread acumula bits correctamente
t=200ms → audioProcessingThread detecta silencio
		 → [FinalizarCaptura - SILENCIO] 1200 bits capturados
		 → mensaje encolado en _mensajesCapturados

t=300ms → cooldown: esperando a que termine

t=400ms → [MessageProcessingThread] Procesando mensaje de 1200 bits
		 → _procesamiento.ProcesarBits(mensaje)
```

## ⚙️ Configuración Personalizable

### Tamaño de la Cola de Buffers
Si necesitas cambiar el tamaño máximo:

```csharp
// En IniciarCaptura(), línea ~210:
_audioBufferQueue = new BlockingCollection<AudioBufferData>(maxSize: 2000); // Aumentado
```

### Tiempo de Silencio Requerido
```csharp
// En IniciarCaptura(), línea ~270:
double silencioRequeridoMs = vhfMode ? 300.0 : 800.0;

// Cambiar a:
double silencioRequeridoMs = vhfMode ? 500.0 : 1200.0; // Más tiempo
```

### Cooldown Entre Mensajes
```csharp
// En IniciarCaptura(), línea ~265:
int cooldownMs = 100; // Cambiar este valor

// Ejemplo: 250ms entre mensajes
int cooldownMs = 250;
```

## 🐛 Debugging y Logs

### Habilitar Logs Detallados
```csharp
// En LogToDisplay(), verifica que se invoque correctamente
// Los logs deberían aparecer en DISPLAYSECUNDARIO

// Si no ves logs:
// 1. Verifica que _form no sea null
// 2. Verifica que DISPLAYSECUNDARIO existe
// 3. Comprueba que no hay excepciones silenciosas
```

### Monitorear CPU
```csharp
// El callback DataAvailable debe tomar <1ms
// audioProcessingThread debe usar 10-30% de un core

// Si ves CPU alta en el thread de NAudio:
// ❌ Significa que la refactorización no funcionó
// ✅ Debe estar baja (solo copia de buffers)
```

### Ver Cola de Buffers
Para diagnosticar problemas, puedes agregar:

```csharp
// En audioProcessingThread (opcional):
if (_audioBufferQueue.Count > 900)
{
	LogToDisplay($"[ADVERTENCIA] Cola casi llena: {_audioBufferQueue.Count} buffers\n");
}
```

## 🚨 Posibles Problemas y Soluciones

| Problema | Causa | Solución |
|----------|-------|----------|
| No se reciben mensajes | Cola vacía | Verifica que el patrón sea detectado |
| CPU alta en NAudio | ❌ Refactorización falló | Revisa el callback (debe ser rápido) |
| Fallos aleatorios | Buffers perdidos | Aumenta tamaño de cola |
| Threads no se cierran | ❌ No se llama DetenerCaptura() | Llama DetenerCaptura() en onClosing |
| OutOfMemoryException | Cola muy grande | Reduce maxSize en BlockingCollection |

## ✅ Checklist de Integración

- [ ] `CapturaDatos` está inicializado con `Procesamiento` y formulario
- [ ] Se llama `IniciarCaptura()` cuando se presiona "Grabar"
- [ ] Se llama `DetenerCaptura()` cuando se presiona "Detener"
- [ ] Se llama `DetenerCaptura()` en `Form_Closing`
- [ ] Los logs aparecen en `DISPLAYSECUNDARIO`
- [ ] No hay excepciones en la ventana de Output
- [ ] CPU del callback es baja (<5%)
- [ ] CPU total del programa es baja (<30%)

## 📚 Referencia Rápida de Métodos

```csharp
// Iniciar captura
_capturaDatos.IniciarCaptura();

// Pausar (mantiene buffers en cola)
_capturaDatos.PausarCaptura(true);

// Reanudar
_capturaDatos.PausarCaptura(false);

// Detener captura completamente
_capturaDatos.DetenerCaptura();

// Obtener mensajes procesados
List<string> mensajes = _capturaDatos.ObtenerMensajes();
```

---
**Documento de referencia para CapturaDatos refactorizado**
Actualizado: 2024
