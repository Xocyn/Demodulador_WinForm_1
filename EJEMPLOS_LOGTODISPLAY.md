# Ejemplos Prácticos: Cómo Usar LogToDisplay()

## Ejemplo 1: Uso Básico

### ❌ INCORRECTO

```csharp
public class Procesamiento
{
    private readonly RichTextBox _mainDisplay;

    public void Procesar(string input)
    {
        // Esto crash si se llama desde thread diferente
        _mainDisplay.AppendText("Procesando...\n");
    }
}
```

### ✅ CORRECTO

```csharp
public class Procesamiento
{
    private readonly RichTextBox _mainDisplay;

    public Procesamiento(RichTextBox mainDisplay)
    {
        _mainDisplay = mainDisplay;
    }

    private void LogToDisplay(string message)
    {
        if (_mainDisplay?.InvokeRequired == true)
            _mainDisplay.Invoke(() => _mainDisplay.AppendText(message));
        else if (_mainDisplay != null)
            _mainDisplay.AppendText(message);
    }

    public void Procesar(string input)
    {
        LogToDisplay("Procesando...\n");  // ✅ Seguro desde cualquier thread
    }
}
```

---

## Ejemplo 2: Mostrar Información Estructurada

```csharp
public class Metodos
{
    private readonly Action<string> _log;

    public Metodos(Action<string> logAction)
    {
        _log = logAction;
    }

    public void MGeografica(List<int> mensaje)
    {
        _log("\n╔════════════════════════════════╗\n");
        _log("║   MENSAJE GEOGRÁFICO (102)      ║\n");
        _log("╚════════════════════════════════╝\n");

        string mmsi = "123456789";
        _log($"MMSI: {mmsi}\n");

        string area = "España";
        _log($"Área: {area}\n");

        _log("\n");
    }
}
```

**Output en MAINDISPLAY:**
```
╔════════════════════════════════╗
║   MENSAJE GEOGRÁFICO (102)      ║
╚════════════════════════════════╝

MMSI: 123456789
Área: España

```

---

## Ejemplo 3: Mostrar Progreso

```csharp
public class Procesamiento
{
    private readonly RichTextBox _mainDisplay;

    public void ProcesoLargo()
    {
        LogToDisplay("Iniciando proceso largo...\n");

        for (int i = 0; i <= 100; i += 10)
        {
            // Simular operación
            Thread.Sleep(100);

            LogToDisplay($"Progreso: {i}%\n");
        }

        LogToDisplay("✓ Proceso completado\n");
    }
}
```

**Output en MAINDISPLAY:**
```
Iniciando proceso largo...
Progreso: 0%
Progreso: 10%
Progreso: 20%
Progreso: 30%
...
✓ Proceso completado
```

---

## Ejemplo 4: Mostrar Errores y Excepciones

```csharp
public class Procesamiento
{
    public void ProcesarConErrorHandling(string input)
    {
        try
        {
            LogToDisplay("Decodificando mensaje...\n");

            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Input vacío");

            int valor = int.Parse(input);
            LogToDisplay($"Valor decodificado: {valor}\n");
        }
        catch (ArgumentException ex)
        {
            LogToDisplay($"❌ Error de argumento: {ex.Message}\n");
        }
        catch (FormatException ex)
        {
            LogToDisplay($"❌ Error de formato: {ex.Message}\n");
        }
        catch (Exception ex)
        {
            LogToDisplay($"❌ Error inesperado: {ex.Message}\n");
        }
    }
}
```

**Output en MAINDISPLAY:**
```
Decodificando mensaje...
❌ Error de argumento: Input vacío
```

---

## Ejemplo 5: Patrón con Callback (Como en el Proyecto)

```csharp
// En CapturaDatos.cs
public void IniciarCaptura()
{
    // Instanciar Procesamiento con referencia a MAINDISPLAY
    var procesamiento = new Procesamiento(_form.MAINDISPLAY);

    // Thread de procesamiento
    _processingThread = new Thread(() =>
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            if (_mensajesCapturados.TryDequeue(out string bits))
            {
                // Esto se ejecuta en el thread de procesamiento
                procesamiento.Procesar(bits, false);
                // Pero escribe en UI de forma segura vía LogToDisplay()
            }
        }
    })
    { IsBackground = true };

    _processingThread.Start();
}

// En Procesamiento.cs
public class Procesamiento
{
    private readonly RichTextBox _mainDisplay;
    private readonly Metodos _metodos;

    public Procesamiento(RichTextBox mainDisplay)
    {
        _mainDisplay = mainDisplay;
        _metodos = new Metodos(LogToDisplay);  // Pasar callback
    }

    private void LogToDisplay(string message)
    {
        if (_mainDisplay?.InvokeRequired == true)
            _mainDisplay.Invoke(() => _mainDisplay.AppendText(message));
        else if (_mainDisplay != null)
            _mainDisplay.AppendText(message);
    }

    public void Procesar(string input, bool ext)
    {
        LogToDisplay("═══════════════════════════════\n");
        LogToDisplay("Procesando mensaje DSC\n");
        _metodos.MGeografica(/*...*/);
    }
}

// En Metodos.cs
public class Metodos
{
    private readonly Action<string> _log;

    public Metodos(Action<string> logAction)
    {
        _log = logAction;
    }

    public void MGeografica(List<int> mensaje)
    {
        _log("Extrayendo información geográfica...\n");
        // Todas las _log() van a través del callback
        // El callback es LogToDisplay()
        // LogToDisplay() detecta threading y usa Invoke()
        // ✅ Todo thread-safe
    }
}
```

---

## Ejemplo 6: Formateo Avanzado

```csharp
public class Metodos
{
    private readonly Action<string> _log;

    public Metodos(Action<string> logAction) => _log = logAction;

    public void MIndividual(List<int> mensaje)
    {
        _log("\n┌────────────────────────────┐\n");
        _log("│  MENSAJE INDIVIDUAL (120)  │\n");
        _log("└────────────────────────────┘\n");

        _log($"│ Formato: Individual\n");
        _log($"│ Bits: {mensaje.Count}\n");
        _log($"│ Timestamp: {DateTime.Now:HH:mm:ss.fff}\n");

        if (mensaje[14] == 112)
        {
            _log($"│ ⚠️  ALERTA: Mensaje de Socorro\n");
        }

        _log("└────────────────────────────┘\n");
        _log("\n");
    }
}
```

**Output en MAINDISPLAY:**
```
┌────────────────────────────┐
│  MENSAJE INDIVIDUAL (120)  │
└────────────────────────────┘

│ Formato: Individual
│ Bits: 456
│ Timestamp: 14:23:45.123
│ ⚠️  ALERTA: Mensaje de Socorro

└────────────────────────────┘

```

---

## Ejemplo 7: Control de Múltiples Threads

```csharp
public class Procesamiento
{
    private readonly RichTextBox _mainDisplay;
    private int _messageCount = 0;
    private readonly object _countLock = new();

    public Procesamiento(RichTextBox mainDisplay)
    {
        _mainDisplay = mainDisplay;
    }

    private void LogToDisplay(string message)
    {
        if (_mainDisplay?.InvokeRequired == true)
            _mainDisplay.Invoke(() => _mainDisplay.AppendText(message));
        else if (_mainDisplay != null)
            _mainDisplay.AppendText(message);
    }

    public void ProcesarDesdeMultiplesThreads(string threadName, int mensajeId)
    {
        lock (_countLock)
        {
            _messageCount++;
        }

        // LogToDisplay() maneja el threading automáticamente
        LogToDisplay($"[{threadName}] Procesando mensaje #{mensajeId}\n");
    }
}

// Uso:
Thread t1 = new Thread(() => 
    procesamiento.ProcesarDesdeMultiplesThreads("Thread-1", 1));
Thread t2 = new Thread(() => 
    procesamiento.ProcesarDesdeMultiplesThreads("Thread-2", 2));
Thread t3 = new Thread(() => 
    procesamiento.ProcesarDesdeMultiplesThreads("Thread-3", 3));

t1.Start(); t2.Start(); t3.Start();
```

**Output en MAINDISPLAY (puede variar el orden):**
```
[Thread-1] Procesando mensaje #1
[Thread-3] Procesando mensaje #3
[Thread-2] Procesando mensaje #2
```

---

## Ejemplo 8: Operación Asincrónica con Reportes

```csharp
public class Procesamiento
{
    private readonly RichTextBox _mainDisplay;

    public void ProcesarAsincronamente()
    {
        Task.Run(() =>
        {
            LogToDisplay("▶ Iniciando procesamiento asincrónico...\n");

            try
            {
                for (int i = 0; i < 5; i++)
                {
                    LogToDisplay($"  ⏳ Fase {i + 1}/5\n");
                    Thread.Sleep(1000);  // Simular operación
                }

                LogToDisplay("✓ Procesamiento completado exitosamente\n");
            }
            catch (Exception ex)
            {
                LogToDisplay($"✗ Error: {ex.Message}\n");
            }
        });
    }
}
```

**Output en MAINDISPLAY:**
```
▶ Iniciando procesamiento asincrónico...
  ⏳ Fase 1/5
  ⏳ Fase 2/5
  ⏳ Fase 3/5
  ⏳ Fase 4/5
  ⏳ Fase 5/5
✓ Procesamiento completado exitosamente
```

---

## Patrón General

**Siempre que quieras escribir en un control WinForms desde otro thread:**

```csharp
// ❌ NUNCA hagas esto:
control.AppendText("Algo");

// ✅ SIEMPRE haz esto:
private void LogToControl(string message)
{
    if (control?.InvokeRequired == true)
        control.Invoke(() => control.AppendText(message));
    else if (control != null)
        control.AppendText(message);
}

// Y luego usa:
LogToControl("Algo\n");
```

---

## Checklist de Implementación

- [ ] ¿Tienes un thread que escribe en UI? → Necesitas `LogToDisplay()`
- [ ] ¿Usas `Thread`, `Task`, `BackgroundWorker` o `async`? → Necesitas `LogToDisplay()`
- [ ] ¿Quieres evitar `InvalidOperationException`? → Usa `LogToDisplay()`
- [ ] ¿Quieres que la UI sea responsiva? → `LogToDisplay()` es la forma

