# Checklist de Implementación y Troubleshooting

## ✅ Fase 1: Verificación del Código

### Compilación
- [ ] El proyecto compila sin errores
- [ ] No hay advertencias (warnings) en CapturaDatos.cs
- [ ] Todas las referencias a `BlockingCollection` están disponibles (System.Collections.Concurrent)
- [ ] No hay referencias a métodos obsoletos

### Verificación Visual
```csharp
// Busca estos puntos clave en CapturaDatos.cs:

// 1. Campos (línea ~30)
private BlockingCollection<AudioBufferData> _audioBufferQueue;

// 2. Clase AudioBufferData (línea ~175)
private class AudioBufferData { ... }

// 3. Inicialización en IniciarCaptura() (línea ~210)
_audioBufferQueue = new BlockingCollection<AudioBufferData>(maxSize: 1000);

// 4. Nuevo thread audioProcessingThread (línea ~285)
Thread audioProcessingThread = new Thread(() => { ... });

// 5. Callback simplificado (línea ~405)
_waveIn.DataAvailable += (s, a) => { ... };

// 6. Métodos nuevos (DetenerCaptura, PausarCaptura, ObtenerMensajes)
```

## ✅ Fase 2: Integración en el Formulario

### Form_Load o Constructor
```csharp
public Demodulador_DSC()
{
	InitializeComponent();

	// ✅ Inicializar Procesamiento
	var procesamiento = new Procesamiento(MAINDISPLAY, this);

	// ✅ Inicializar CapturaDatos
	_capturaDatos = new CapturaDatos(procesamiento, this);

	// Verificar que no sea null
	if (_capturaDatos == null)
		throw new NullReferenceException("CapturaDatos no inicializado");
}
```

### Form_Closing
```csharp
private void Demodulador_DSC_FormClosing(object sender, FormClosingEventArgs e)
{
	// ✅ CRÍTICO: Detener captura antes de cerrar
	_capturaDatos?.DetenerCaptura();

	// Esperar a que se liberen recursos
	System.Threading.Thread.Sleep(500);
}
```

## ✅ Fase 3: Testing Básico

### Test 1: Iniciar/Detener
```csharp
[TestMethod]
public void TestIniciarDetener()
{
	var procesamiento = new Procesamiento(display, form);
	var captura = new CapturaDatos(procesamiento, form);

	// Debe NO lanzar excepción
	captura.IniciarCaptura();
	System.Threading.Thread.Sleep(1000);
	captura.DetenerCaptura();

	Assert.Pass("Iniciar/Detener exitoso");
}
```

### Test 2: Pausa
```csharp
[TestMethod]
public void TestPausa()
{
	var captura = new CapturaDatos(procesamiento, form);
	captura.IniciarCaptura();

	captura.PausarCaptura(true);   // Debe mostrar log
	captura.PausarCaptura(false);  // Debe mostrar log

	captura.DetenerCaptura();
	Assert.Pass("Pausa exitosa");
}
```

### Test 3: Crecimiento de Memoria
```csharp
[TestMethod]
public void TestMemoriaEstable()
{
	var captura = new CapturaDatos(procesamiento, form);
	captura.IniciarCaptura();

	var mem1 = GC.GetTotalMemory(true);
	System.Threading.Thread.Sleep(5000);
	var mem2 = GC.GetTotalMemory(true);

	var diff = mem2 - mem1;
	Assert.IsTrue(diff < 50_000_000);  // Menos de 50MB de diferencia

	captura.DetenerCaptura();
}
```

## 🐛 Troubleshooting

### Problema 1: "BlockingCollection no reconocido"
**Error:**
```
CS0246: El nombre de tipo o espacio de nombres 'BlockingCollection' no existe
```

**Solución:**
```csharp
// Agregar using:
using System.Collections.Concurrent;
```

**Verificación:**
```csharp
// En CapturaDatos.cs línea 11 debe estar:
using System.Collections.Concurrent;
```

---

### Problema 2: "AudioBufferData no reconocido"
**Error:**
```
CS0246: El nombre de tipo o espacio de nombres 'AudioBufferData' no existe
```

**Solución:**
Verificar que la clase esté definida dentro de CapturaDatos:

```csharp
public class CapturaDatos
{
	// ✅ Debe estar aquí dentro:
	private class AudioBufferData
	{
		public byte[] Buffer { get; set; }
		public int BytesRecorded { get; set; }
	}
}
```

---

### Problema 3: NullReferenceException en _form
**Error:**
```
System.NullReferenceException: Object reference not set to an instance
en CapturaDatos.LogToDisplay()
```

**Causa:** `_form` es null

**Solución:**
```csharp
// Verificar inicialización:
var captura = new CapturaDatos(procesamiento, this);  // ✅ Pasar 'this'

// NO hacer:
var captura = new CapturaDatos(procesamiento, null);  // ❌ null
var captura = new CapturaDatos(procesamiento);        // ❌ falta parámetro
```

---

### Problema 4: "Captura ya en progreso"
**Síntoma:** Al presionar dos veces el botón "Iniciar", sale advertencia

**Comportamiento:**
```
[Advertencia] Captura ya en progreso.
```

**Verificación:**
```csharp
// Esto es CORRECTO - previene inicios duplicados
if (_isRunning)
{
	LogToDisplay("[Advertencia] Captura ya en progreso.\n");
	return;
}
```

**Solución:** No presionar "Iniciar" dos veces. Es una protección, no un error.

---

### Problema 5: CPU alta en thread de NAudio
**Síntoma:** Task Manager muestra thread de NAudio al 100%

**Causa:** Refactorización falló, callback sigue sobrecargado

**Diagnóstico:**
```csharp
// Agregar log en callback para medir:
var sw = System.Diagnostics.Stopwatch.StartNew();

_waveIn.DataAvailable += (s, a) =>
{
	// ... código ...
	sw.Stop();
	if (sw.ElapsedMilliseconds > 2)
		LogToDisplay($"[WARNING] Callback tardó {sw.ElapsedMilliseconds}ms\n");
};
```

**Debe ser:** <1ms

---

### Problema 6: Threads no se cierran
**Síntoma:** Al presionar "Detener", el programa tarda mucho o cuelga

**Causa:** `DetenerCaptura()` no está siendo llamado

**Solución:**
```csharp
// CRÍTICO en Form_Closing:
private void Form_Closing(object sender, EventArgs e)
{
	_capturaDatos?.DetenerCaptura();  // ✅ NUNCA olvides esto
}

// También en botón Detener:
private void btnDetener_Click(object sender, EventArgs e)
{
	_capturaDatos?.DetenerCaptura();  // ✅ SIEMPRE
}
```

---

### Problema 7: "OutOfMemoryException"
**Síntoma:**
```
System.OutOfMemoryException: Exception of type 'System.OutOfMemoryException' was thrown.
```

**Causa:** Queue de buffers creciendo sin límite

**Diagnóstico:**
```csharp
// En audioProcessingThread:
if (_audioBufferQueue.Count > 950)
{
	LogToDisplay($"[CRITICAL] Queue size: {_audioBufferQueue.Count}\n");
}
```

**Soluciones:**
1. Aumentar prioridad de `audioProcessingThread`
2. Reducir tamaño de maxSize (de 1000 a 500)
3. Hacer más rápido `ProcessAudio()` (problema en demodulador)

```csharp
// Reducir maxSize:
_audioBufferQueue = new BlockingCollection<AudioBufferData>(maxSize: 500);

// Aumentar prioridad del thread:
audioProcessingThread.Priority = ThreadPriority.AboveNormal;
```

---

### Problema 8: No se detecta patrón de sincronización
**Síntoma:**
```
Escuchando...
(Nada sucede)
```

**Causas posibles:**
1. No hay señal de audio
2. Dispositivo de entrada incorrecto
3. Nivel de volumen muy bajo

**Diagnóstico:**
```csharp
// Agregar log en audioProcessingThread:
if (audioData.BytesRecorded > 0)
{
	// Calcular RMS para ver si hay señal
	double rms = 0;
	int sampleCount = audioData.BytesRecorded / 2;
	for (int i = 0; i < sampleCount; i++)
	{
		short sample = BitConverter.ToInt16(audioData.Buffer, i * 2);
		rms += sample * sample;
	}
	rms = Math.Sqrt(rms / sampleCount);

	LogToDisplay($"[DEBUG] RMS: {rms}\n");
}
```

**Debe ser:** >1000 (para detectar señal)

---

### Problema 9: Mensajes incompletos
**Síntoma:**
```
[FinalizarCaptura - SILENCIO] 0 bits capturados
[Advertencia] No se encoló mensaje: cadena vacía
```

**Causa:** No se acumularon bits antes de que terminara

**Soluciones:**
1. Verificar que patrón se esté detectando
2. Aumentar tiempo de silencio requerido
3. Revisar cálculo de silencio

```csharp
// Aumentar umbral de silencio (línea ~270):
double silencioRequeridoMs = vhfMode ? 500.0 : 1200.0;  // Era 300/800
```

---

### Problema 10: Fuga de Memory en DISPLAYSECUNDARIO
**Síntoma:** Después de 1 hora, programa lento

**Causa:** LogToDisplay() acumulando demasiado texto

**Solución:**
```csharp
private void LogToDisplay(string message)
{
	if (_form?.InvokeRequired == true)
	{
		_form.Invoke(() =>
		{
			_form.DISPLAYSECUNDARIO.AppendText(message);

			// ✅ Limitar a 100KB de texto
			if (_form.DISPLAYSECUNDARIO.TextLength > 100_000)
			{
				_form.DISPLAYSECUNDARIO.Clear();
				_form.DISPLAYSECUNDARIO.AppendText("[Log limpiado]\n");
			}
		});
	}
	else
	{
		_form?.DISPLAYSECUNDARIO.AppendText(message);
	}
}
```

## 📋 Performance Checklist

### Después de Implementar

- [ ] Callback DataAvailable tarda <1ms
- [ ] audioProcessingThread usa 10-30% de CPU
- [ ] Thread de NAudio usa <10% de CPU
- [ ] Memoria estable (no crece indefinidamente)
- [ ] No hay OutOfMemoryException
- [ ] Patrones detectados correctamente
- [ ] Mensajes capturados completamente
- [ ] Threads se cierran en <2 segundos
- [ ] No hay DeadLocks
- [ ] No hay NullReferenceExceptions

### Métricas Esperadas

```
Callback DataAvailable:     <1ms
audioProcessingThread:      35-50ms por buffer
MessageProcessingThread:    Variable (depende de ProcesarBits)
Memoria base:               ~50MB
Memoria por buffer en cola: ~2KB
CPU total (en idle):        5-10%
CPU total (procesando):     30-40%
```

## 🔒 Validación Final

Antes de considerar la refactorización completa:

```csharp
// Prueba de estrés (10 segundos de captura):
1. Iniciar captura
2. Esperar 10 segundos
3. Monitorear logs (deben mostrarse continuamente)
4. Verificar CPU (debe ser estable)
5. Detener captura
6. Verificar que se liberen recursos

// Prueba de cierre:
1. Iniciar captura
2. Presionar X (cerrar ventana)
3. Programa debe cerrarse en <3 segundos
4. No debe quedar proceso zombie
```

---
**Checklist Completo - Refactorización CapturaDatos**
Fecha: 2024
Versión: 1.0
