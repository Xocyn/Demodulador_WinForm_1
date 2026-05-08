# 📚 Ejemplos - Clase Expansion

## Uso Básico

### En Procesamiento.cs

La clase `Expansion` se utiliza automáticamente cuando se detecta una extensión en el mensaje DSC:

```csharp
public class Procesamiento
{
    private readonly Expansion _expansion;

    public Procesamiento(RichTextBox mainDisplay)
    {
        _expansion = new Expansion(LogToDisplay);
    }

    public void Procesar(string input, bool ext)
    {
        // ... procesamiento de mensaje ...

        // Si hay extensión, se decodifica automáticamente
        if (extension)
        {
            _expansion.Decodificar(MENSAJE_EXT);
        }
    }
}
```

---

## Ejemplo 1: Decodificación de Resolución Mejorada (Tipo 100)

### Datos de Entrada
```
MENSAJE_EXT = [100, 15, 30, 45, 60, 127]
              ^^^^^ Tipo 100 (Resolución mejorada)
```

### Salida en UI
```
Mejora de Latitud 15304560'' 
Mejora de Longitud 45601530'' 
```

### Código
```csharp
var extension = new Expansion(Console.Write);
var testData = new List<int> { 100, 15, 30, 45, 60, 127 };
extension.Decodificar(testData);
```

---

## Ejemplo 2: Decodificación de Velocidad Actual (Tipo 102)

### Datos de Entrada
```
MENSAJE_EXT = [102, 25, 30, 127]
              ^^^^^ Tipo 102 (Velocidad)
```

### Salida en UI
```
Velocidad actual del barco: 25,30 nudos
```

### Código
```csharp
var extension = new Expansion(msg => textBox.AppendText(msg));
var testData = new List<int> { 102, 25, 30, 127 };
extension.Decodificar(testData);
```

---

## Ejemplo 3: Múltiples Extensiones Consecutivas

### Datos de Entrada
```
MENSAJE_EXT = [100, 10, 20, 30, 40,   // Tipo 100: Resolución mejorada
               102, 25, 30,             // Tipo 102: Velocidad
               127]                      // EOS (End Of Sequence)
```

### Salida en UI
```
Mejora de Latitud 10203040'' 
Mejora de Longitud 30401020'' 
Velocidad actual del barco: 25,30 nudos
```

### Código
```csharp
var extension = new Expansion(LogToDisplay);
var testData = new List<int> { 
    100, 10, 20, 30, 40,
    102, 25, 30,
    127 
};
extension.Decodificar(testData);
```

---

## Ejemplo 4: Petición de Datos (Código 110)

Cuando se envía el código 110, significa que se solicitan los datos pero no se envía el contenido:

### Datos de Entrada
```
MENSAJE_EXT = [100, 110, 127]
              ^^^^^ ^^^ Petición de datos
```

### Salida en UI
```
Peticion de datos
```

### Código
```csharp
var extension = new Expansion(LogToDisplay);
var testData = new List<int> { 100, 110, 127 };
extension.Decodificar(testData);
```

---

## Ejemplo 5: Sin Datos Disponibles (Código 126)

Cuando se envía el código 126, significa que no hay datos disponibles para esa extensión:

### Datos de Entrada
```
MENSAJE_EXT = [102, 126, 127]
              ^^^^^ ^^^ Sin datos disponibles
```

### Salida en UI
```
Ningun dato disponible
```

### Código
```csharp
var extension = new Expansion(LogToDisplay);
var testData = new List<int> { 102, 126, 127 };
extension.Decodificar(testData);
```

---

## Ejemplo 6: Identificador Adicional (Tipo 104)

### Datos de Entrada
```
MENSAJE_EXT = [104, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 127]
              ^^^^^ Tipo 104 (Identificador adicional, 10 caracteres)
```

### Salida en UI
```
Identificador adicional: A B C D E F G H I J
```

### Código
```csharp
var extension = new Expansion(LogToDisplay);
var testData = new List<int> { 104, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 127 };
extension.Decodificar(testData);
```

---

## Ejemplo 7: Zona Geográfica Ampliada (Tipo 105)

La zona geográfica ampliada incluye información de velocidad y trayectoria:

### Datos de Entrada
```
MENSAJE_EXT = [105, 10, 20, 30, 40, 50, 60, 70, 80, 25, 30, 45, 50, 127]
              ^^^^^ Tipo 105 (12 caracteres)
```

### Salida en UI
```
Mejora de Latitud ,10203040'' 
Mejora de Longitud ,50607080'' 
Resolucion adicional ventana vertical: 25304550
Resolucion adicional ventana horizontal: ...
Velocidad actual del barco: ...
Trayectoria actual del barco: ...
```

---

## Integración en Test Unitario

```csharp
[TestFixture]
public class ExpansionTests
{
    [Test]
    public void TestResolucionMejorada()
    {
        var messages = new List<string>();
        var expansion = new Expansion(msg => messages.Add(msg));

        var testData = new List<int> { 100, 15, 30, 45, 60, 127 };
        expansion.Decodificar(testData);

        Assert.That(messages, Does.Contain("Mejora de Latitud"));
        Assert.That(messages, Does.Contain("Mejora de Longitud"));
    }

    [Test]
    public void TestPeticionDatos()
    {
        var messages = new List<string>();
        var expansion = new Expansion(msg => messages.Add(msg));

        var testData = new List<int> { 102, 110, 127 };
        expansion.Decodificar(testData);

        Assert.That(messages, Does.Contain("Peticion de datos"));
    }

    [Test]
    public void TestMultipleExtensions()
    {
        var messages = new List<string>();
        var expansion = new Expansion(msg => messages.Add(msg));

        var testData = new List<int> { 100, 10, 20, 30, 40, 102, 25, 30, 127 };
        expansion.Decodificar(testData);

        Assert.That(messages.Count, Is.GreaterThan(2));
    }
}
```

---

## Integración con Callback Thread-Safe

```csharp
public class UIUpdater
{
    private readonly RichTextBox _textBox;
    private readonly Expansion _expansion;

    public UIUpdater(RichTextBox textBox)
    {
        _textBox = textBox;
        // Crear expansion con callback thread-safe
        _expansion = new Expansion(LogToDisplay);
    }

    private void LogToDisplay(string message)
    {
        if (_textBox.InvokeRequired)
        {
            _textBox.Invoke(() => _textBox.AppendText(message));
        }
        else
        {
            _textBox.AppendText(message);
        }
    }

    public void ProcessExtension(List<int> data)
    {
        _expansion.Decodificar(data);
    }
}
```

---

## Manejo de Errores

```csharp
public void ProcessExtensionSafely(List<int> extensionData)
{
    try
    {
        if (extensionData == null || extensionData.Count == 0)
        {
            LogToDisplay("Error: Datos de extensión vacíos\n");
            return;
        }

        _expansion.Decodificar(extensionData);
    }
    catch (ArgumentOutOfRangeException ex)
    {
        LogToDisplay($"Error en decodificación de extensión: {ex.Message}\n");
    }
    catch (Exception ex)
    {
        LogToDisplay($"Error inesperado: {ex.Message}\n");
    }
}
```

---

## Monitoreo y Logging

```csharp
public class ExpansionMonitor
{
    private int _extensionsProcessed = 0;
    private readonly Expansion _expansion;

    public ExpansionMonitor(Action<string> logCallback)
    {
        _expansion = new Expansion(msg =>
        {
            logCallback(msg);
            if (msg.Contains("Identificador adicional") || 
                msg.Contains("Velocidad actual"))
            {
                _extensionsProcessed++;
            }
        });
    }

    public int ExtensionsProcessed => _extensionsProcessed;

    public void Process(List<int> data)
    {
        _expansion.Decodificar(data);
    }
}
```

---

## Configuración Avanzada

### Crear wrapper personalizado

```csharp
public class ExpansionWithLogging
{
    private readonly Expansion _expansion;
    private readonly Action<string> _log;

    public ExpansionWithLogging(Action<string> logCallback)
    {
        _log = msg =>
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            logCallback($"[{timestamp}] EXT: {msg}");
        };

        _expansion = new Expansion(_log);
    }

    public void Process(List<int> data)
    {
        _log($"Procesando extensión con {data.Count} elementos\n");
        _expansion.Decodificar(data);
    }
}
```

---

**Versión**: 1.0
**Última actualización**: $(date)
