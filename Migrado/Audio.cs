using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

public class BFSKDemodulator
{
    const int SampleRate = 44100;

    private double _freqBit0;
    private double _freqBit1;
    private readonly double _samplesPerSymbol;
    private readonly double _energyThreshold;

    // ── Buffer circular de muestras ───────────────────────────────────────────
    // Array de tamaño fijo en lugar de List<short>:
    //   · Elimina RemoveRange(0, n) que era O(n) y movía toda la memoria en cada callback.
    //   · _writePos apunta al próximo slot de escritura (módulo BufSize).
    //   · _totalSamples es el contador absoluto de muestras escritas desde el inicio.
    //   · Los acumuladores guardan posición absoluta; se mapean a _buf con % BufSize.
    private const int BufSize = 8192; // ~185ms a 44100Hz, potencia de 2
    private readonly short[] _buf = new short[BufSize];
    private int _writePos = 0;
    private long _totalSamples = 0;

    // ── 4 fases en paralelo ───────────────────────────────────────────────────
    private const int PhaseCount = 4;
    private readonly double[] _accumulators = new double[PhaseCount];
    private int _activePhase = -1;
    private bool _phaseLocked = false;

    // ── Coeficientes de Goertzel precalculados ────────────────────────────────
    // coeff = 2·cos(2π·f/Fs) — se calcula una vez en el constructor.
    // phaseInc = 2π·f/Fs — para la corrección de fase con índice absoluto.
    private double _goertzelCoeff0;
    private double _goertzelCoeff1;
    private double _phaseInc0;
    private double _phaseInc1;

    public BFSKDemodulator(bool vhf = false)
    {
        if (vhf)
        {
            _freqBit0 = 2100.0;
            _freqBit1 = 1300.0;
        }
        else
        {
            _freqBit0 = 1785.0;
            _freqBit1 = 1615.0;
        }
        _samplesPerSymbol = (double)SampleRate / (vhf ? 1200 : 100);

        double minRms = short.MaxValue * 0.1; // umbral original: 0.01
        _energyThreshold = minRms * minRms * _samplesPerSymbol;

        // Inicializar los 4 acumuladores con offsets distribuidos uniformemente
        for (int p = 0; p < PhaseCount; p++)
            _accumulators[p] = p * (_samplesPerSymbol / PhaseCount);

        // Precomputar coeficientes de Goertzel y fases — una sola vez para toda la vida del objeto
        _phaseInc0 = 2.0 * Math.PI * _freqBit0 / SampleRate;
        _phaseInc1 = 2.0 * Math.PI * _freqBit1 / SampleRate;
        _goertzelCoeff0 = 2.0 * Math.Cos(_phaseInc0);
        _goertzelCoeff1 = 2.0 * Math.Cos(_phaseInc1);
    }

    // ── ResetTiming: volver a modo detección con 4 fases ─────────────────────
    public void ResetTiming()
    {
        _phaseLocked = false;
        _activePhase = -1;
        // No se limpia _buf ni _totalSamples: el buffer circular simplemente
        // se sobreescribe. Los acumuladores se reinician a posición absoluta
        // actual + offset de fase, para que arranquen alineados al stream real.
        for (int p = 0; p < PhaseCount; p++)
            _accumulators[p] = _totalSamples + p * (_samplesPerSymbol / PhaseCount);
    }

    // ── LockPhase: llamar cuando una fase detectó el dot pattern ─────────────
    public void LockPhase(int phaseIndex)
    {
        _activePhase = phaseIndex;
        _phaseLocked = true;
        // Los otros acumuladores se abandonan; solo el de la fase activa sigue avanzando.
        // No es necesario resetear _totalSamples ni _buf: el circular sigue funcionando.
    }

    // ── ProcessAudio ─────────────────────────────────────────────────────────
    // Mejoras respecto a la versión anterior:
    //   1. WaveBuffer de NAudio: acceso directo a short[] sin copiar ni convertir.
    //   2. Buffer circular _buf[BufSize]: escritura O(1), sin RemoveRange O(n).
    //   3. Goertzel con índice absoluto: un solo loop por símbolo calcula e0 y e1
    //      simultáneamente, con fase coherente con la posición real en el stream.
    //   4. Energía bruta, e0 y e1 en el mismo loop: 1 pasada en vez de 3.
    public string[] ProcessAudio(byte[] buffer, int bytesRecorded)
    {
        // WaveBuffer requiere que el array tenga longitud múltiplo de 4 para que
        // los offsets de la union sean correctos. El buffer copiado en CapturaDatos
        // tiene exactamente bytesRecorded bytes, que puede no ser múltiplo de 4.
        // MemoryMarshal.Cast es la alternativa segura: reinterpreta el span de bytes
        // como span de shorts sin ninguna copia y sin restricciones de alineación.
        var samples = MemoryMarshal.Cast<byte, short>(
            buffer.AsSpan(0, bytesRecorded));
        int sampleCount = samples.Length;

        // Escribir muestras en el buffer circular
        for (int i = 0; i < sampleCount; i++)
        {
            _buf[_writePos] = samples[i];
            _writePos = (_writePos + 1) % BufSize;
            _totalSamples++;
        }

        var results = new StringBuilder[PhaseCount];
        for (int p = 0; p < PhaseCount; p++)
            results[p] = new StringBuilder();

        int pStart = _phaseLocked ? _activePhase : 0;
        int pEnd = _phaseLocked ? _activePhase + 1 : PhaseCount;

        for (int p = pStart; p < pEnd; p++)
        {
            while (_accumulators[p] + _samplesPerSymbol <= _totalSamples)
            {
                // startAbs y endAbs son índices absolutos de muestra
                long startAbs = (long)Math.Round(_accumulators[p]);
                long endAbs = (long)Math.Round(_accumulators[p] + _samplesPerSymbol);
                int length = (int)(endAbs - startAbs);

                // Verificar que las muestras siguen en el buffer circular
                if (_totalSamples - startAbs > BufSize) { _accumulators[p] += _samplesPerSymbol; continue; }
                if (endAbs > _totalSamples) break;

                // ── Un solo loop: energía bruta + Goertzel f0 + Goertzel f1 ──────
                // Goertzel con corrección de fase absoluta:
                //   La señal fue generada con fase continua desde muestra 0.
                //   Usar startAbs como origen garantiza que el correlador evalúa
                //   exactamente la misma fase que el modulador usó para ese símbolo.
                //   Sin esto, los símbolos de 36 vs 37 muestras producen un error
                //   de fase que puede invertir la decisión 0/1 aleatoriamente.
                double rawE = 0;
                double s0_0 = 0, s1_0 = 0, s2_0 = 0; // estado Goertzel f0
                double s0_1 = 0, s1_1 = 0, s2_1 = 0; // estado Goertzel f1

                for (int n = 0; n < length; n++)
                {
                    int bufIdx = (int)((startAbs + n) % BufSize);
                    //double window = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * n / (length - 1))); // ventana Hamming
                    double sample = _buf[bufIdx]; // * window;   

                    // Energía bruta (umbral de portadora)
                    rawE += sample * sample;

                    // Goertzel f0
                    s0_0 = sample + _goertzelCoeff0 * s1_0 - s2_0;
                    s2_0 = s1_0; s1_0 = s0_0;

                    // Goertzel f1
                    s0_1 = sample + _goertzelCoeff1 * s1_1 - s2_1;
                    s2_1 = s1_1; s1_1 = s0_1;
                }

                if (rawE >= _energyThreshold)
                {
                    // Energía espectral Goertzel: s1²+s2²−coeff·s1·s2
                    // Corrección de fase absoluta: rotar el fasor resultante por
                    // la fase acumulada hasta el inicio del símbolo, para que la
                    // comparación e0 vs e1 sea coherente entre símbolos consecutivos.
                    double phase0 = _phaseInc0 * startAbs;
                    double phase1 = _phaseInc1 * startAbs;

                    double e0 = (s1_0 * s1_0) + (s2_0 * s2_0) - (_goertzelCoeff0 * s1_0 * s2_0);
                    double e1 = (s1_1 * s1_1) + (s2_1 * s2_1) - (_goertzelCoeff1 * s1_1 * s2_1);

                    // Ajuste de fase: proyectar sobre el fasor esperado
                    double I0 = s1_0 * Math.Cos(phase0) - s2_0 * Math.Cos(phase0 - _phaseInc0);
                    double Q0 = s1_0 * Math.Sin(phase0) - s2_0 * Math.Sin(phase0 - _phaseInc0);
                    double I1 = s1_1 * Math.Cos(phase1) - s2_1 * Math.Cos(phase1 - _phaseInc1);
                    double Q1 = s1_1 * Math.Sin(phase1) - s2_1 * Math.Sin(phase1 - _phaseInc1);

                    e0 = I0 * I0 + Q0 * Q0;
                    e1 = I1 * I1 + Q1 * Q1;

                    results[p].Append(e1 > e0 ? '1' : '0');
                }

                _accumulators[p] += _samplesPerSymbol;
            }
        }

        // No hay RemoveRange: el buffer circular se sobreescribe naturalmente.
        // Los acumuladores mantienen posición absoluta; no hay offset que restar.
        return results.Select(sb => sb.ToString()).ToArray();
    }
}
public class BFSKModulator
{
    public static void GenerateWav(string inputTxt, string outputWav, bool vhf)
    {
        int bitRate;
        double f0, f1;
        if (vhf)
        {
            // VHF — 1200 bps según ITU-R M.493-16
            bitRate = 1200;
            f0 = 2100.0;     // bit 0
            f1 = 1300.0;     // bit 1
        }
        else
        {
            // HF — 100 bps
            bitRate = 100;
            f0 = 1785.0;     // bit 0
            f1 = 1615.0;     // bit 1
        }

        const int sampleRate = 44100;

        // samplesPerBit como double: 44100/1200 = 36.75 (NO se trunca a 36).
        // Si se usara int, cada símbolo VHF perdería 0.75 muestras →
        // drift de ~3 símbolos en un mensaje de 150 bits.
        double samplesPerBit = (double)sampleRate / bitRate;

        string bitstream = File.ReadAllText(inputTxt);

        var waveFormat = new WaveFormat(sampleRate, 16, 1);
        using var writer = new WaveFileWriter(outputWav, waveFormat);

        double amplitude = 0.85 * short.MaxValue; // original 0.25
        double phase = 0.0;  // acumulador de fase continua (radianes)
        double posAccum = 0.0;  // acumulador de posición para timing exacto

        foreach (char c in bitstream)
        {
            if (c != '0' && c != '1')
                continue;

            double freq = (c == '0') ? f0 : f1;
            double phaseIncrement = 2 * Math.PI * freq / sampleRate;  // incremento de fase por muestra

            // Calcular cuántas muestras le corresponden a este símbolo,
            // alternando entre floor/ceil para mantener promedio exacto en 36.75.
            int startSample = (int)Math.Round(posAccum);
            posAccum += samplesPerBit;
            int endSample = (int)Math.Round(posAccum);
            int nSamples = endSample - startSample;

            for (int n = 0; n < nSamples; n++)
            {
                double sample = amplitude * Math.Sin(phase);
                writer.WriteSample((float)(sample / short.MaxValue));
                phase += phaseIncrement;  // incrementa fase de forma continua y suave
            }
        }
    }
}

/// <summary>
/// Captura el audio del altavoz usando WASAPI Loopback Capture.
/// Permite grabar lo que se está reproduciendo en el altavoz.
/// </summary>
public class LoopbackAudioCapture : IDisposable
{
    private WasapiLoopbackCapture _waveInEvent;
    private List<byte> _capturedAudio = new List<byte>();
    private bool _isRecording = false;
    private TaskCompletionSource<byte[]> _captureTcs;

    public LoopbackAudioCapture()
    {
        _waveInEvent = new WasapiLoopbackCapture();
        _waveInEvent.DataAvailable += WaveInEvent_DataAvailable;
        _waveInEvent.RecordingStopped += WaveInEvent_RecordingStopped;
    }

    private void WaveInEvent_DataAvailable(object sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded > 0)
        {
            _capturedAudio.AddRange(e.Buffer.Take(e.BytesRecorded));
        }
    }

    private void WaveInEvent_RecordingStopped(object sender, StoppedEventArgs e)
    {
        _isRecording = false;
        if (_captureTcs != null && !_captureTcs.Task.IsCompleted)
        {
            _captureTcs.SetResult(_capturedAudio.ToArray());
        }
    }

    /// <summary>
    /// Inicia la captura de audio del loopback.
    /// </summary>
    public void StartCapture()
    {
        _capturedAudio.Clear();
        _isRecording = true;
        _waveInEvent.StartRecording();
    }

    /// <summary>
    /// Detiene la captura y retorna los datos capturados como array de bytes.
    /// </summary>
    public async Task<byte[]> StopCaptureAsync()
    {
        _captureTcs = new TaskCompletionSource<byte[]>();
        _waveInEvent.StopRecording();

        var result = await _captureTcs.Task;
        return result;
    }

    /// <summary>
    /// Detiene la captura de forma síncrona.
    /// </summary>
    public byte[] StopCapture()
    {
        byte[] result = _capturedAudio.ToArray();
        _waveInEvent.StopRecording();
        _isRecording = false;
        return result;
    }

    public bool IsRecording => _isRecording;

    public void Dispose()
    {
        _waveInEvent?.Dispose();
    }
}

public class AudioPlayer
{
    public static void Play(string file)
    {
        using var audioFile = new AudioFileReader(file);
        using var outputDevice = new WaveOutEvent();

        outputDevice.Init(audioFile);
        outputDevice.Play();

        while (outputDevice.PlaybackState == PlaybackState.Playing)
            Thread.Sleep(100);
    }

    /// <summary>
    /// Reproduce un archivo de audio de forma no-bloqueante (asíncrona).
    /// Retorna una Task que se completa cuando termina la reproducción.
    /// </summary>
    public static async Task PlayAsync(string file)
    {
        var tcs = new TaskCompletionSource<bool>();

        var audioFile = new AudioFileReader(file);
        var outputDevice = new WaveOutEvent();

        outputDevice.Init(audioFile);

        EventHandler<StoppedEventArgs> stoppedHandler = null;
        stoppedHandler = (s, e) =>
        {
            outputDevice.PlaybackStopped -= stoppedHandler;
            audioFile?.Dispose();
            outputDevice?.Dispose();
            tcs.SetResult(true);
        };

        outputDevice.PlaybackStopped += stoppedHandler;
        outputDevice.Play();

        await tcs.Task;
    }
}