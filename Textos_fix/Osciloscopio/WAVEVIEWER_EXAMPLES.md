# Ejemplos de Uso - WaveViewerControl

## Uso Básico

### Desde un formulario

```csharp
// En el constructor o Form_Load
var waveViewer = new WaveViewerControl();
waveViewer.Location = new Point(10, 10);
waveViewer.Size = new Size(400, 100);
this.Controls.Add(waveViewer);
```

### Agregar muestras de audio

```csharp
// Desde un callback de audio
private void AudioDevice_DataAvailable(object sender, WaveInEventArgs e)
{
    // Convertir bytes a shorts (16-bit)
    int sampleCount = e.BytesRecorded / 2;
    short[] samples = new short[sampleCount];
    Buffer.BlockCopy(e.Buffer, 0, samples, 0, e.BytesRecorded);

    // Actualizar visualización
    waveViewer1.AddSamples(samples);
}
```

### Usando WaveDisplayManager para control avanzado

```csharp
// Crear el gestor
var displayManager = new WaveDisplayManager(
    updateDisplay: (samples) => 
    {
        waveViewer1.AddSamples(samples);
    },
    targetSamples: 2048,      // Mostrar 2048 muestras
    updateIntervalMs: 33      // ~30 FPS
);

// Agregar muestras (desde thread de audio)
displayManager.AddSamples(audioSamples);

// Limpiar cuando termine
displayManager.Clear();
```

## Configuraciones Predefinidas

### Configuración Responsiva (Baja Latencia)
```csharp
_waveDisplayManager = new WaveDisplayManager(
    updateDisplay,
    targetSamples: 2048,      // Menos datos = más responsivo
    updateIntervalMs: 20      // Actualizar frecuentemente
);
```
✅ **Mejor para:** Visualización interactiva, respuesta rápida
❌ **Problema:** Más carga CPU

### Configuración Equilibrada (Recomendada)
```csharp
_waveDisplayManager = new WaveDisplayManager(
    updateDisplay,
    targetSamples: 4096,      // Balance
    updateIntervalMs: 50      // 20 FPS
);
```
✅ **Mejor para:** Uso general, balance CPU/responsividad

### Configuración Eficiente (Bajo Consumo)
```csharp
_waveDisplayManager = new WaveDisplayManager(
    updateDisplay,
    targetSamples: 8192,      // Más datos = menos actualizaciones
    updateIntervalMs: 100     // 10 FPS
);
```
✅ **Mejor para:** Servidores, bajo consumo
❌ **Problema:** Menos responsivo

## Personalización de Colores

Para cambiar los colores, editar en `WaveViewerControl.cs`:

```csharp
private readonly Color _waveColor = Color.Lime;          // Color de onda
private readonly Color _backgroundColor = Color.Black;   // Fondo

// Cambiar a:
private readonly Color _waveColor = Color.Cyan;          // Cian
private readonly Color _backgroundColor = Color.DarkGray; // Gris oscuro
```

### Esquemas de Color Predefinidos

**Terminal Verde Clásico:**
```csharp
_waveColor = Color.Lime;
_backgroundColor = Color.Black;
```

**Estilo Azul Moderno:**
```csharp
_waveColor = Color.DeepSkyBlue;
_backgroundColor = Color.Navy;
```

**Estilo Naranja:**
```csharp
_waveColor = Color.OrangeRed;
_backgroundColor = Color.DarkOrange.WithAlpha(20); // Semi-transparente
```

**Estilo Científico (Blanco sobre Negro):**
```csharp
_waveColor = Color.White;
_backgroundColor = Color.Black;
```

## Medición de Performance

### Monitorear uso de CPU
```csharp
var stopwatch = Stopwatch.StartNew();
displayManager.AddSamples(samples);
stopwatch.Stop();

Console.WriteLine($"Tiempo de actualización: {stopwatch.ElapsedMilliseconds}ms");
```

### Monitorear muestras en buffer
```csharp
int bufferedSamples = displayManager.BufferedSampleCount;
Console.WriteLine($"Muestras en buffer: {bufferedSamples}");

// Esperar a que se consuma
while (displayManager.BufferedSampleCount > 0)
{
    Thread.Sleep(10);
}
```

## Integración con Diferentes Fuentes de Audio

### Con NAudio.Wave.WaveInEvent
```csharp
var waveIn = new WaveInEvent();
waveIn.DataAvailable += (s, e) =>
{
    int sampleCount = e.BytesRecorded / 2;
    short[] samples = new short[sampleCount];
    Buffer.BlockCopy(e.Buffer, 0, samples, 0, e.BytesRecorded);
    waveViewer1.AddSamples(samples);
};
```

### Con NAudio.Wave.WaveFileReader
```csharp
var reader = new WaveFileReader("audio.wav");
byte[] buffer = new byte[44100 * 2]; // 1 segundo a 44.1kHz

int bytesRead;
while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
{
    int sampleCount = bytesRead / 2;
    short[] samples = new short[sampleCount];
    Buffer.BlockCopy(buffer, 0, samples, 0, bytesRead);
    waveViewer1.AddSamples(samples);
}
```

### Con análisis sintetizado
```csharp
// Generar onda senoidal de prueba
var frequency = 1000.0; // 1 kHz
var sampleRate = 44100;
var duration = 1.0; // 1 segundo

var samples = new short[(int)(sampleRate * duration)];
for (int i = 0; i < samples.Length; i++)
{
    double time = (double)i / sampleRate;
    double value = Math.Sin(2 * Math.PI * frequency * time);
    samples[i] = (short)(value * short.MaxValue);
}

waveViewer1.AddSamples(samples);
```

## Troubleshooting

### La onda no se actualiza
```csharp
// Verificar que AddSamples se está llamando
displayManager.AddSamples(samples);

// Verificar que el buffer tiene datos
Debug.WriteLine($"Buffer: {displayManager.BufferedSampleCount}");

// Verificar que el control es visible
Debug.WriteLine($"Visible: {waveViewer1.Visible}");
Debug.WriteLine($"Size: {waveViewer1.Size}");
```

### La onda se ve cortada o distorsionada
```csharp
// Aumentar targetSamples
_waveDisplayManager = new WaveDisplayManager(
    updateDisplay,
    targetSamples: 8192,  // Aumentar de 4096 a 8192
    updateIntervalMs: 50
);
```

### Alto uso de CPU
```csharp
// Reducir frecuencia de actualización
_waveDisplayManager = new WaveDisplayManager(
    updateDisplay,
    targetSamples: 4096,
    updateIntervalMs: 100  // Aumentar de 50 a 100 (10 FPS en lugar de 20)
);
```

## Ejemplo Completo - Aplicación Independiente

```csharp
public partial class AudioVisualizerForm : Form
{
    private WaveInEvent _waveIn;
    private WaveDisplayManager _displayManager;

    public AudioVisualizerForm()
    {
        InitializeComponent();
    }

    private void Form_Load(object sender, EventArgs e)
    {
        _waveIn = new WaveInEvent();
        _waveIn.WaveFormat = new WaveFormat(44100, 16, 1);
        _waveIn.DataAvailable += WaveIn_DataAvailable;
        _waveIn.RecordingStopped += WaveIn_RecordingStopped;

        // Crear gestor de visualización
        _displayManager = new WaveDisplayManager(
            updateDisplay: (samples) =>
            {
                if (waveViewer1.InvokeRequired)
                {
                    waveViewer1.Invoke(() => waveViewer1.AddSamples(samples));
                }
                else
                {
                    waveViewer1.AddSamples(samples);
                }
            },
            targetSamples: 4096,
            updateIntervalMs: 50
        );

        _waveIn.StartRecording();
    }

    private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
    {
        int sampleCount = e.BytesRecorded / 2;
        short[] samples = new short[sampleCount];
        Buffer.BlockCopy(e.Buffer, 0, samples, 0, e.BytesRecorded);
        _displayManager.AddSamples(samples);
    }

    private void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
    {
        _displayManager?.Clear();
    }

    private void Form_FormClosing(object sender, FormClosingEventArgs e)
    {
        _waveIn?.StopRecording();
        _waveIn?.Dispose();
    }
}
```
