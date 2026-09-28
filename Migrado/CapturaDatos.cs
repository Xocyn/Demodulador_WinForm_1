using Dem_v2;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Demodulador_WinForm_1
{
    public class CapturaDatos
    {
        private const int CaptureSampleRate = 48000;
        private const int RealtimeDisplaySamples = 256;
        private const int RealtimeDisplayIntervalMs = 200;
        private const int NoiseCalibrationMilliseconds = 2000;
        private const double NoiseGateOpenMarginDb = 10.0;
        private const double NoiseGateCloseMarginDb = 6.0;
        private const double NoiseGateAttackMilliseconds = 40.0;
        private const double NoiseGateReleaseMilliseconds = 250.0;

        private enum Estado
        {
            EsperandoInicio,
            Grabando,
            Cooldown
        }

        private readonly Demodulador_DSC _form;

        // Cola de mensajes DSC demodulados — el thread de demodulación deposita aquí,
        // el thread de procesamiento consume.
        private readonly ConcurrentQueue<string> _mensajesCapturados = new();

        // Cola de bloques de audio crudos — DataAvailable deposita aquí (copia mínima),
        // el thread de demodulación consume. BlockingCollection permite Wait() sin spinning.
        private BlockingCollection<(long sequence, byte[] buffer, int bytesRecorded, long captureTicks)> _audioQueue;

        // CancellationToken para detener los threads limpiamente al salir.
        // ⚠️ NOTA: Se crea de nuevo cada vez que se inicia captura, NO es readonly
        private CancellationTokenSource _cts;

        private Thread _demodThread;  // consume _audioQueue, produce _mensajesCapturados

        // Lock para proteger las variables de estado compartidas entre el thread de audio
        // y el thread principal (cambio de modo con M).
        private readonly object _lock = new();

        private WaveInEvent _waveIn;
        private IDscDemodulator _demod;
        private Thread _processingThread;
        private bool _isRunning = false;
        private readonly Procesamiento _procesamiento;  // ← agregar campo

        private bool pausa = false;

        private long _audioCallbackSequence;
        private long _audioBlocksReceived;
        private long _audioBlocksEnqueued;
        private long _audioBlocksDropped;
        private long _audioBytesTotal;
        private long _audioCallbackLastTicks;
        private long _audioCallbackMaxGapTicks;

        private sealed class StreamingLowPassFilter
        {
            private static readonly double[] Butterworth8Q =
            {
                0.5097955791,
                0.6013448869,
                0.8999762231,
                2.5629154477
            };

            private readonly int _sampleRate;
            private double _cutoffHz = -1.0;
            private BiquadLowPass[] _sections = Array.Empty<BiquadLowPass>();

            public StreamingLowPassFilter(int sampleRate)
            {
                _sampleRate = sampleRate;
            }

            public void ProcessInPlace(byte[] buffer, int bytesRecorded, double cutoffHz)
            {
                if (bytesRecorded <= 0)
                    return;

                cutoffHz = Math.Clamp(cutoffHz, 20.0, _sampleRate * 0.45);
                if (_sections.Length == 0 || Math.Abs(cutoffHz - _cutoffHz) >= 1.0)
                    Configure(cutoffHz);

                Span<short> samples = MemoryMarshal.Cast<byte, short>(buffer.AsSpan(0, bytesRecorded));
                for (int i = 0; i < samples.Length; i++)
                {
                    double value = samples[i];
                    foreach (var section in _sections)
                        value = section.Process(value);

                    samples[i] = (short)Math.Clamp(Math.Round(value), short.MinValue, short.MaxValue);
                }
            }

            private void Configure(double cutoffHz)
            {
                _cutoffHz = cutoffHz;
                _sections = Butterworth8Q
                    .Select(q => new BiquadLowPass(_sampleRate, cutoffHz, q))
                    .ToArray();
            }

            private sealed class BiquadLowPass
            {
                private readonly double _b0;
                private readonly double _b1;
                private readonly double _b2;
                private readonly double _a1;
                private readonly double _a2;
                private double _z1;
                private double _z2;

                public BiquadLowPass(int sampleRate, double cutoffHz, double q)
                {
                    double omega = 2.0 * Math.PI * cutoffHz / sampleRate;
                    double sin = Math.Sin(omega);
                    double cos = Math.Cos(omega);
                    double alpha = sin / (2.0 * q);

                    double b0 = (1.0 - cos) / 2.0;
                    double b1 = 1.0 - cos;
                    double b2 = (1.0 - cos) / 2.0;
                    double a0 = 1.0 + alpha;
                    double a1 = -2.0 * cos;
                    double a2 = 1.0 - alpha;

                    _b0 = b0 / a0;
                    _b1 = b1 / a0;
                    _b2 = b2 / a0;
                    _a1 = a1 / a0;
                    _a2 = a2 / a0;
                }

                public double Process(double sample)
                {
                    double output = _b0 * sample + _z1;
                    _z1 = _b1 * sample - _a1 * output + _z2;
                    _z2 = _b2 * sample - _a2 * output;
                    return output;
                }
            }
        }

        public CapturaDatos(Procesamiento procesamiento, Demodulador_DSC form = null)  // ← agregar parámetro
        {
            _form = form;
            _procesamiento = procesamiento;  // ← guardar referencia
        }
        private void LogToDisplay(string message)
        {
            try
            {
                if (_form?.InvokeRequired == true)
                {
                    _form.Invoke(() => _form.DISPLAYSECUNDARIO.AppendText(message));
                }
                else
                {
                    _form?.DISPLAYSECUNDARIO.AppendText(message);
                }
            }
            catch (Exception) { }
        }

        private void ClearDisplay()
        {
            if (_form?.InvokeRequired == true)
            {
                _form.Invoke(() => _form.DISPLAYSECUNDARIO.Clear());
            }
            else
            {
                _form?.DISPLAYSECUNDARIO.Clear();
            }
        }

        private void ClearMAIN()
        {
            if (_form?.InvokeRequired == true)
            {
                _form.Invoke(() => _form.MAINDISPLAY.Clear());
            }
            else
            {
                _form?.MAINDISPLAY.Clear();
            }
        }

        private static double TicksToMilliseconds(long ticks)
        {
            return ticks * 1000.0 / Stopwatch.Frequency;
        }

        private static void UpdateMax(ref long target, long value)
        {
            long current;
            while (value > (current = Interlocked.Read(ref target)))
            {
                if (Interlocked.CompareExchange(ref target, value, current) == current)
                    break;
            }
        }

        private static short[] CreateDisplaySamples(ReadOnlySpan<short> sourceSamples)
        {
            int displayCount = Math.Min(RealtimeDisplaySamples, sourceSamples.Length);
            short[] displaySamples = new short[displayCount];

            for (int i = 0; i < displayCount; i++)
            {
                int sourceIndex = i * sourceSamples.Length / displayCount;
                displaySamples[i] = sourceSamples[sourceIndex];
            }

            return displaySamples;
        }

        private void ResetAudioDiagnostics()
        {
            Interlocked.Exchange(ref _audioCallbackSequence, 0);
            Interlocked.Exchange(ref _audioBlocksReceived, 0);
            Interlocked.Exchange(ref _audioBlocksEnqueued, 0);
            Interlocked.Exchange(ref _audioBlocksDropped, 0);
            Interlocked.Exchange(ref _audioBytesTotal, 0);
            Interlocked.Exchange(ref _audioCallbackLastTicks, 0);
            Interlocked.Exchange(ref _audioCallbackMaxGapTicks, 0);
        }

        private static bool TryLooksLikeValidDsc(string input, out string detail)
        {
            detail = string.Empty;
            if (string.IsNullOrEmpty(input) || input.Length < 80)
            {
                detail = $"bits insuficientes ({input?.Length ?? 0})";
                return false;
            }

            List<(int Index, int Value)> encontrados = new();
            int i = 0;
            bool sincronizado = false;

            while (!sincronizado)
            {
                if (i + 10 > input.Length) break;

                string ventana = input.Substring(i, 10);
                int mensajeInt = Convert.ToInt32(ventana, 2);

                if (Decodificador.TryDecodificarMensaje(mensajeInt, out int valor))
                {
                    if (PhasingSequence.TryCaracter(valor))
                    {
                        encontrados.Add((i, valor));
                        i += 10;

                        if (encontrados.Count >= 3 &&
                            PhasingSequence.TryDetect(encontrados, out _))
                        {
                            sincronizado = true;
                        }
                    }
                    else
                    {
                        i += 1;
                    }
                }
                else
                {
                    i += 1;
                }
            }

            if (!sincronizado)
            {
                detail = "sin phasing valido";
                return false;
            }

            bool formatConfirmed = false;
            int form = 0;

            while (!formatConfirmed)
            {
                if (i + 10 > input.Length) break;

                string ventana = input.Substring(i, 10);
                int mensajeInt = Convert.ToInt32(ventana, 2);
                Decodificador.TryDecodificarMensaje(mensajeInt, out int valor);

                form = FormatSpecifier.Filtro2(valor, out int j);
                bool esBroadcast = form == 112 || form == 116;
                bool dxrxConfirmed = esBroadcast || Decodificador.DxRx(input, i);

                i += 10;

                if (j == 1 && dxrxConfirmed)
                    formatConfirmed = true;
            }

            if (!formatConfirmed)
            {
                detail = "sin format specifier valido";
                return false;
            }

            i -= 10;
            if (i < 0 || i + 10 > input.Length)
            {
                detail = "indice de format specifier invalido";
                return false;
            }

            DecodeDscMessageStrict(input, i, out List<int> message, out List<int> invalidSymbolIndexes);
            if (message.Count < 6)
            {
                detail = $"mensaje demasiado corto ({message.Count} simbolos)";
                return false;
            }

            if (!IsSupportedFormat(message[0]))
            {
                detail = $"formato no soportado ({message[0]})";
                return false;
            }

            List<int> mainMessage = message;
            if (Procesamiento.Extension(message))
            {
                int first127 = message.IndexOf(127);
                int first122 = message.IndexOf(122);
                int first117 = message.IndexOf(117);
                int firstEos = new[] { first127, first122, first117 }
                    .Where(x => x >= 0)
                    .DefaultIfEmpty(-1)
                    .Min();

                if (firstEos < 0 || firstEos + 8 > message.Count)
                {
                    detail = "extension incompleta";
                    return false;
                }

                mainMessage = message.GetRange(0, firstEos + 8);
            }

            if (invalidSymbolIndexes.Any(index => index < mainMessage.Count))
            {
                detail = $"simbolos invalidos antes de ECC ({invalidSymbolIndexes.Count})";
                return false;
            }

            List<int> ecc = PrepareEcc(mainMessage);
            if (!VerifyEcc(mainMessage, ecc))
            {
                detail = $"ECC invalido ({mainMessage.Count} simbolos)";
                return false;
            }

            detail = $"formato={mainMessage[0]}, simbolos={mainMessage.Count}";
            return true;
        }

        private static string SelectBestDscCandidate(string[] candidates, int preferredPhase, out int selectedPhase, out string detail)
        {
            selectedPhase = -1;
            detail = "sin candidato valido";
            var filteredCandidates = new string[candidates.Length];
            var filterDetails = new string[candidates.Length];

            for (int phase = 0; phase < candidates.Length; phase++)
            {
                filteredCandidates[phase] =
                    DscFrameTrimmer.TrimAfterFinalEos(candidates[phase], out filterDetails[phase]);
            }

            if (preferredPhase >= 0 && preferredPhase < filteredCandidates.Length &&
                TryLooksLikeValidDsc(filteredCandidates[preferredPhase], out string preferredDetail))
            {
                selectedPhase = preferredPhase;
                detail = $"fase preferida valida: {preferredDetail}; {filterDetails[preferredPhase]}";
                return filteredCandidates[preferredPhase];
            }

            int bestLength = -1;
            string bestBits = string.Empty;
            int fallbackPhase = -1;
            int fallbackLength = -1;
            string fallbackBits = string.Empty;
            string fallbackDetail = string.Empty;

            for (int phase = 0; phase < filteredCandidates.Length; phase++)
            {
                string bits = filteredCandidates[phase] ?? string.Empty;
                if (bits.Length > fallbackLength)
                {
                    fallbackPhase = phase;
                    fallbackLength = bits.Length;
                    fallbackBits = bits;
                    TryLooksLikeValidDsc(bits, out fallbackDetail);
                }

                if (TryLooksLikeValidDsc(bits, out string candidateDetail) && bits.Length > bestLength)
                {
                    selectedPhase = phase;
                    bestLength = bits.Length;
                    bestBits = bits;
                    detail = $"fase alternativa valida: {candidateDetail}; {filterDetails[phase]}";
                }
            }

            if (selectedPhase >= 0)
                return bestBits;

            if (preferredPhase >= 0 && preferredPhase < filteredCandidates.Length &&
                !string.IsNullOrEmpty(filteredCandidates[preferredPhase]))
            {
                selectedPhase = preferredPhase;
                TryLooksLikeValidDsc(filteredCandidates[preferredPhase], out string preferredFailure);
                detail =
                    $"fallback fase preferida sin ECC/estructura valida: {preferredFailure}; " +
                    filterDetails[preferredPhase];
                return filteredCandidates[preferredPhase];
            }

            selectedPhase = fallbackPhase;
            detail =
                $"fallback fase mas larga sin ECC/estructura valida: {fallbackDetail}; " +
                (fallbackPhase >= 0 ? filterDetails[fallbackPhase] : "sin fase");
            return fallbackBits;
        }

        private static bool IsSupportedFormat(int format)
        {
            return format == 102 || format == 112 || format == 114 || format == 116 || format == 120;
        }

        private static List<int> PrepareEcc(List<int> message)
        {
            if (message.Count < 4)
                return new List<int>();

            return message
                .Skip(1)
                .Take(message.Count - 4)
                .ToList();
        }

        private static bool VerifyEcc(List<int> message, List<int> ecc)
        {
            if (message.Count < 6)
                return false;

            int eccDx = message[message.Count - 6];
            int eccRx = message[message.Count - 1];
            int calculated = 0;

            foreach (int value in ecc)
                calculated ^= value;

            calculated &= 0x7F;
            return calculated == eccDx || calculated == eccRx;
        }

        private static void DecodeDscMessageStrict(string input, int startIndex, out List<int> message, out List<int> invalidSymbolIndexes)
        {
            message = new List<int>();
            invalidSymbolIndexes = new List<int>();
            int symbolIndex = 0;

            for (int k = startIndex; k + 10 <= input.Length; k += 10)
            {
                string ventana = input.Substring(k, 10);
                if (Decodificador.TryDeco(ventana, out int value))
                {
                    message.Add(value);
                    symbolIndex++;
                    continue;
                }

                int rxIndex = k + 50;
                if (rxIndex + 10 <= input.Length &&
                    Decodificador.TryDeco(input.Substring(rxIndex, 10), out int rxValue))
                {
                    message.Add(rxValue);
                    symbolIndex++;
                    continue;
                }

                message.Add(0);
                invalidSymbolIndexes.Add(symbolIndex);
                symbolIndex++;
            }
        }

        // ── Detector de silencio ─────────────────────────────────────────────────
        // Acumula la duración del silencio continuo en el audio crudo.
        // Opera sobre los bytes del callback DataAvailable, antes de cualquier
        // demodulación, para detectar ausencia de portadora lo antes posible.
        //
        // Uso:
        //   var sd = new SilenceDetector(umbralEnergia, silencioRequeridoMs);
        //   if (sd.Actualizar(buffer, bytesRecorded))  → silencio sostenido detectado
        //   sd.Reset()                                  → reiniciar al comenzar a grabar
        private sealed class SilenceDetector
        {
            // Energía media por muestra a partir de la cual se considera "señal presente".
            // Se calcula igual que en BFSKDemodulator: (short.MaxValue × 0.01)²
            // Usamos energía por muestra para que sea independiente del tamaño del bloque.
            private readonly double _umbralEnergiaPorMuestra;

            // Milisegundos consecutivos de silencio necesarios para disparar el evento.
            private readonly double _silencioRequeridoMs;

            // Acumulador de silencio continuo (se resetea si llega señal).
            private double _silencioAcumuladoMs;

            // Tasa de muestreo para convertir muestras → ms.
            private readonly int _sampleRate;

            public SilenceDetector(double umbralEnergiaPorMuestra, double silencioRequeridoMs, int sampleRate = CaptureSampleRate)
            {
                _umbralEnergiaPorMuestra = umbralEnergiaPorMuestra;
                _silencioRequeridoMs = silencioRequeridoMs;
                _sampleRate = sampleRate;
            }

            // Devuelve true si el silencio acumulado superó el umbral requerido.
            // buffer: bytes crudos de 16-bit PCM mono (little-endian).
            public bool Actualizar(byte[] buffer, int bytesRecorded)
            {
                if (bytesRecorded <= 0) return false;

                int sampleCount = bytesRecorded / 2;
                double energiaTotal = 0.0;

                for (int i = 0; i < sampleCount; i++)
                {
                    short muestra = BitConverter.ToInt16(buffer, i * 2);
                    energiaTotal += (double)muestra * muestra;
                }

                double energiaPorMuestra = energiaTotal / sampleCount;
                double duracionBloqueMs = (sampleCount * 1000.0) / _sampleRate;

                if (energiaPorMuestra < _umbralEnergiaPorMuestra)
                {
                    // Silencio: acumular duración
                    _silencioAcumuladoMs += duracionBloqueMs;
                    return _silencioAcumuladoMs >= _silencioRequeridoMs;
                }
                else
                {
                    // Señal presente: reiniciar el contador
                    _silencioAcumuladoMs = 0.0;
                    return false;
                }
            }

            // Reiniciar el acumulador (llamar al inicio de cada grabación).
            public void Reset() => _silencioAcumuladoMs = 0.0;

            // Milisegundos de silencio acumulados hasta ahora (útil para logs).
            public double SilencioAcumuladoMs => _silencioAcumuladoMs;
        }

        public void IniciarCaptura()
        {
            if (_isRunning)
            {
                LogToDisplay("[Advertencia] Captura ya en progreso.\n");
                return;
            }

            // Elevar prioridad del proceso para que el SO asigne más tiempo de CPU
            // a esta aplicación sobre el resto. High es el máximo seguro —
            // RealTime puede congelar el sistema operativo completo.
            try
            {
                System.Diagnostics.Process.GetCurrentProcess().PriorityClass =
                    System.Diagnostics.ProcessPriorityClass.High;
            }
            catch (Exception ex)
            {
                LogToDisplay($"[Advertencia] No se pudo elevar prioridad del proceso: {ex.Message}\n");
            }

            _isRunning = true;

            // ⚠️ IMPORTANTE: Crear un NUEVO CancellationTokenSource para cada captura
            // El anterior fue cancelado y no se puede reutilizar
            _cts = new CancellationTokenSource();
            ResetAudioDiagnostics();

            // Cola de audio crudo con capacidad acotada: si el thread de demodulación
            // no da abasto, Add() bloqueará el callback — señal de que el sistema está
            // sobrecargado. 32 bloques × ~50ms = ~1.6s de buffer máximo.
            _audioQueue = new BlockingCollection<(long, byte[], int, long)>(boundedCapacity: 32);

            bool vhfMode = _form.combox_hf_vhf.SelectedIndex == 1;
            double rxLowPassHz = _form.ObtenerFiltroPasabajosRxHz();

            _waveIn = new WaveInEvent();
            _waveIn.DeviceNumber = _form.combox_dispositivos.SelectedIndex;
            _waveIn.WaveFormat = new WaveFormat(CaptureSampleRate, 16, 1);
            LogToDisplay($"[AudioDiag] WaveIn BufferMilliseconds={_waveIn.BufferMilliseconds}, formato={CaptureSampleRate} Hz / 16 bit / mono\n");

            bool usarCorrelacion = _form?.UsarDemodulacionCorrelacion() == true;
            _demod = usarCorrelacion
                ? new CorrelationBFSKDemodulator(vhfMode)
                : new BFSKDemodulator(vhfMode);
            LogToDisplay(
                $"[Demod] Metodo={(usarCorrelacion ? "Correlacion" : "Actual")}, " +
                $"banda={(vhfMode ? "VHF" : "MF/HF")}, filtroRx={rxLowPassHz:F0} Hz\n");
            LogToDisplay(
                $"[Nivel] Calibrando ruido durante {NoiseCalibrationMilliseconds} ms; " +
                $"apertura=piso+{NoiseGateOpenMarginDb:F0} dB, " +
                $"cierre=piso+{NoiseGateCloseMarginDb:F0} dB.\n");
            LogToDisplay(
                $"[Rendimiento] Visualización de onda limitada a " +
                $"{RealtimeDisplaySamples} muestras cada {RealtimeDisplayIntervalMs} ms.\n");

            // Instanciar Procesamiento con referencias a los controles del formulario
            //var procesamiento = new Procesamiento(_form.MAINDISPLAY, _form);


            // ── Thread de demodulación ───────────────────────────────────────────────
            // Consume bloques de audio crudos de _audioQueue y ejecuta toda la lógica
            // de demodulación, detección de patrones y silencio.
            // Al separarlo del callback DataAvailable, el thread de audio de NAudio
            // queda libre para recibir el siguiente bloque sin esperar.
            _demodThread = new Thread(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                // Estado local del thread — mismo código que estaba en DataAvailable,
                // sin ningún cambio de lógica.
                int PhaseCount = _demod.PhaseCount;
                var syncBuffers = new StringBuilder[PhaseCount];
                for (int p = 0; p < PhaseCount; p++) syncBuffers[p] = new StringBuilder();

                int lockedPhase = -1;
                int triggerPhase = -1;
                var bitAccumulators = new StringBuilder[PhaseCount];
                for (int p = 0; p < PhaseCount; p++) bitAccumulators[p] = new StringBuilder();
                const string startPattern = "01010101010101010101";
                Estado estado = Estado.EsperandoInicio;
                double duracionGrabacionMs = vhfMode ? 2000.0 : 10000.0;
                double tiempoRearmeMs = vhfMode ? 700.0 : 1200.0;
                long inicioGrabacionTicks = 0;
                long inicioRearmeTicks = 0;
                var rxLowPassFilter = new StreamingLowPassFilter(CaptureSampleRate);
                var noiseGate = new AdaptiveNoiseGate(
                    CaptureSampleRate,
                    NoiseCalibrationMilliseconds,
                    NoiseGateOpenMarginDb,
                    NoiseGateCloseMarginDb,
                    NoiseGateAttackMilliseconds,
                    NoiseGateReleaseMilliseconds);
                bool calibrationWasActive = true;
                bool lastGateOpen = false;
                long nextLevelDisplayTicks = 0;
                long expectedSequence = 1;
                long sequenceGaps = 0;
                long processedBlocks = 0;
                long maxProcessingTicks = 0;
                long maxQueueLatencyTicks = 0;
                int maxQueueDepth = 0;
                long lastDiagnosticsTicks = Stopwatch.GetTimestamp();
                long lastLoggedDropped = 0;

                try
                {
                    foreach (var (sequence, buffer, bytesRecorded, captureTicks) in _audioQueue.GetConsumingEnumerable(_cts.Token))
                    {
                        long blockStartTicks = Stopwatch.GetTimestamp();
                        try
                        {
                            if (sequence != expectedSequence)
                            {
                                long missing = sequence - expectedSequence;
                                if (missing > 0)
                                {
                                    sequenceGaps += missing;
                                    LogToDisplay($"[AudioDiag] Gap de secuencia: esperado={expectedSequence}, recibido={sequence}, faltan={missing} bloque(s)\n");
                                }

                                expectedSequence = sequence + 1;
                            }
                            else
                            {
                                expectedSequence++;
                            }

                            processedBlocks++;
                            maxQueueDepth = Math.Max(maxQueueDepth, _audioQueue?.Count ?? 0);
                            maxQueueLatencyTicks = Math.Max(maxQueueLatencyTicks, blockStartTicks - captureTicks);

                            rxLowPassFilter.ProcessInPlace(buffer, bytesRecorded, rxLowPassHz);

                            ReadOnlySpan<short> levelSamples =
                                MemoryMarshal.Cast<byte, short>(buffer.AsSpan(0, bytesRecorded));
                            AudioLevelSnapshot level = noiseGate.Update(levelSamples);
                            long nowTicks = Stopwatch.GetTimestamp();

                            if (nowTicks >= nextLevelDisplayTicks)
                            {
                                _form?.MostrarMuestras(CreateDisplaySamples(levelSamples));
                                _form?.MostrarNivelAudio(level);
                                nextLevelDisplayTicks = nowTicks +
                                    Stopwatch.Frequency * RealtimeDisplayIntervalMs / 1000;
                            }

                            if (calibrationWasActive && !level.IsCalibrating)
                            {
                                _demod.SetMinRmsThresholdDbFs(level.CloseThresholdDbFs);
                                _demod.ResetAll();
                                foreach (var sb in syncBuffers) sb.Clear();
                                LogToDisplay(
                                    $"[Nivel] Calibración completa: piso={level.NoiseFloorDbFs:F1} dBFS, " +
                                    $"abre={level.OpenThresholdDbFs:F1} dBFS, " +
                                    $"cierra={level.CloseThresholdDbFs:F1} dBFS.\n");
                            }

                            if (!level.IsCalibrating && level.IsGateOpen != lastGateOpen)
                            {
                                LogToDisplay(
                                    level.IsGateOpen
                                        ? $"[Nivel] Gate ABIERTO: RMS={level.RmsDbFs:F1} dBFS.\n"
                                        : $"[Nivel] Gate CERRADO: RMS={level.RmsDbFs:F1} dBFS.\n");

                                if (!level.IsGateOpen && estado == Estado.EsperandoInicio)
                                {
                                    _demod.ResetAll();
                                    foreach (var sb in syncBuffers) sb.Clear();
                                }
                            }

                            calibrationWasActive = level.IsCalibrating;
                            lastGateOpen = level.IsGateOpen;

                            if (level.IsCalibrating)
                                continue;

                            // ── Reposo entre recepciones ───────────────────────────────────
                            // En esta etapa descartamos temporalmente el audio antes de volver
                            // a buscar una nueva trama. El rearme también es temporal para no
                            // depender de que un canal ruidoso alcance un silencio ideal.
                            if (estado == Estado.Cooldown)
                            {
                                double tiempoEnRearmeMs =
                                    TicksToMilliseconds(nowTicks - inicioRearmeTicks);
                                if (tiempoEnRearmeMs >= tiempoRearmeMs)
                                {
                                    LimpiarEstadoEntreRecepciones(descartarPendientes: false, resetearSecuencia: false);
                                    estado = Estado.EsperandoInicio;
                                    LogToDisplay($"[Reposo] Rearme completado tras {tiempoEnRearmeMs:F0} ms. Escuchando...\n");
                                }

                                continue;
                            }

                            if (estado == Estado.EsperandoInicio && !level.IsGateOpen)
                                continue;

                            string[] bitsByPhase;

                            bitsByPhase = _demod.ProcessAudio(buffer, bytesRecorded);

                            // ── PASO 1: Acumular bits del bloque ──────────────────────────
                            int phaseStart, phaseEnd;
                            if (estado == Estado.Grabando)
                            {
                                phaseStart = 0;
                                phaseEnd = PhaseCount;
                            }
                            else
                            {
                                phaseStart = (lockedPhase >= 0) ? lockedPhase : 0;
                                phaseEnd = (lockedPhase >= 0) ? lockedPhase + 1 : PhaseCount;
                            }

                            for (int ph = phaseStart; ph < phaseEnd; ph++)
                            {
                                foreach (char bit in bitsByPhase[ph])
                                {
                                    Estado estadoActual;
                                    { estadoActual = estado; }

                                    if (estadoActual == Estado.EsperandoInicio)
                                    {

                                        syncBuffers[ph].Append(bit);
                                        if (syncBuffers[ph].Length > startPattern.Length)
                                            syncBuffers[ph].Remove(0, 1);

                                        if (syncBuffers[ph].ToString().EndsWith(startPattern))
                                        {
                                            ClearDisplay();
                                            LogToDisplay($"DOT PATTERN detectado (fase {ph})");
                                            IniciarGrabacion(ph);
                                        }
                                        else if (syncBuffers[ph].Length >= 10)
                                        {
                                            string sync = syncBuffers[ph].ToString();
                                            string sub = sync.Substring(sync.Length - 10, 10);
                                            if (Decodificador.TryDeco(sub, out int v) && v == 125)
                                            {
                                                ClearDisplay();
                                                LogToDisplay($"Valor 125 detectado sin DOT PATTERN (fase {ph})");
                                                IniciarGrabacion(ph);
                                            }
                                        }

                                    }
                                    else if (estadoActual == Estado.Grabando)
                                    {
                                        { bitAccumulators[ph].Append(bit); }
                                    }
                                }
                            }

                            // ── PASO 2: Finalizar por tiempo desde el bloqueo de fase ─────
                            if (estado == Estado.Grabando)
                            {
                                double tiempoGrabandoMs =
                                    TicksToMilliseconds(Stopwatch.GetTimestamp() - inicioGrabacionTicks);
                                if (tiempoGrabandoMs >= duracionGrabacionMs)
                                {
                                    LogToDisplay($"[Tiempo] {tiempoGrabandoMs:F0} ms desde el bloqueo de fase → finalizando captura");
                                    FinalizarCaptura("TIEMPO");
                                }
                            }


                            void IniciarGrabacion(int ph)
                            {
                                ClearMAIN();
                                triggerPhase = ph;
                                lockedPhase = ph;
                                // No bloqueamos el demodulador a una sola fase: acumulamos
                                // todas y al final elegimos la que pasa estructura/ECC DSC.
                                estado = Estado.Grabando;
                                inicioGrabacionTicks = Stopwatch.GetTimestamp();
                                for (int p = 0; p < PhaseCount; p++)
                                {
                                    bitAccumulators[p].Clear();
                                    bitAccumulators[p].Append(syncBuffers[p].ToString());
                                }
                                LogToDisplay($"[IniciarGrabacion] Fase {ph} bloqueada. Límite={duracionGrabacionMs:F0} ms.");
                            }

                            void LimpiarEstadoEntreRecepciones(bool descartarPendientes, bool resetearSecuencia)
                            {
                                int descartados = 0;
                                long ultimaSecuenciaDescartada = sequence;
                                var queue = _audioQueue;
                                if (descartarPendientes && queue != null)
                                {
                                    while (queue.TryTake(out var descartado))
                                    {
                                        descartados++;
                                        ultimaSecuenciaDescartada = descartado.sequence;
                                    }
                                }

                                _demod.ResetAll();
                                rxLowPassFilter = new StreamingLowPassFilter(CaptureSampleRate);
                                inicioGrabacionTicks = 0;

                                for (int p = 0; p < PhaseCount; p++)
                                {
                                    syncBuffers[p].Clear();
                                    bitAccumulators[p].Clear();
                                }

                                lockedPhase = -1;
                                triggerPhase = -1;
                                if (resetearSecuencia)
                                    expectedSequence = ultimaSecuenciaDescartada + 1;

                                if (descartados > 0)
                                    LogToDisplay($"[ResetRecepcion] buffers pendientes descartados={descartados}\n");
                            }

                            void FinalizarCaptura(string motivo)
                            {
                                string[] candidates = bitAccumulators
                                    .Select(sb => sb.ToString())
                                    .ToArray();
                                string capturado = SelectBestDscCandidate(candidates, triggerPhase, out int selectedPhase, out string selectionDetail);
                                LogToDisplay($"[FinalizarCaptura - {motivo}] fase={selectedPhase}, {capturado.Length} bits capturados ({selectionDetail})");
                                if (capturado.Length > 0)
                                {
                                    _mensajesCapturados.Enqueue(capturado);
                                }
                                else
                                {
                                    LogToDisplay("[Advertencia] No se encoló mensaje: cadena vacía\n");
                                    _form?.RegistrarMensajeRecibido(false);
                                }
                                LimpiarEstadoEntreRecepciones(descartarPendientes: true, resetearSecuencia: true);
                                estado = Estado.Cooldown;
                                inicioRearmeTicks = Stopwatch.GetTimestamp();
                                LogToDisplay($"[Reposo] Descartando audio durante {tiempoRearmeMs:F0} ms.\n");
                            }
                        }
                        catch (OperationCanceledException) { break; }
                        catch (Exception ex)
                        {
                            LogToDisplay($"[Error en DSC-Demodulator] {ex.Message}");
                        }
                        finally
                        {
                            long elapsedTicks = Stopwatch.GetTimestamp() - blockStartTicks;
                            maxProcessingTicks = Math.Max(maxProcessingTicks, elapsedTicks);

                            long nowTicks = Stopwatch.GetTimestamp();
                            long dropped = Interlocked.Read(ref _audioBlocksDropped);
                            bool timeToLog = nowTicks - lastDiagnosticsTicks >= Stopwatch.Frequency * 2;
                            bool droppedChanged = dropped != lastLoggedDropped;

                            if (timeToLog || droppedChanged)
                            {
                                long received = Interlocked.Read(ref _audioBlocksReceived);
                                long enqueued = Interlocked.Read(ref _audioBlocksEnqueued);
                                long bytesTotal = Interlocked.Read(ref _audioBytesTotal);
                                long callbackMaxGapTicks = Interlocked.Read(ref _audioCallbackMaxGapTicks);
                                double avgBytes = received > 0 ? (double)bytesTotal / received : 0.0;

                                LogToDisplay(
                                    $"[AudioDiag] rx={received}, enq={enqueued}, drop={dropped}, proc={processedBlocks}, " +
                                    $"queue={(_audioQueue?.Count ?? 0)}/{maxQueueDepth}, gaps={sequenceGaps}, " +
                                    $"bytesProm={avgBytes:F0}, cbGapMax={TicksToMilliseconds(callbackMaxGapTicks):F1} ms, " +
                                    $"latMax={TicksToMilliseconds(maxQueueLatencyTicks):F1} ms, procMax={TicksToMilliseconds(maxProcessingTicks):F1} ms\n");

                                lastDiagnosticsTicks = nowTicks;
                                lastLoggedDropped = dropped;
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    LogToDisplay("[Info] Thread de demodulación cancelado normalmente.\n");
                }
            })
            {
                IsBackground = true,
                Name = "DSC-Demodulator"
            };
            _demodThread.Start();

            // ── Thread de procesamiento ──────────────────────────────────────────────
            // Consume mensajes DSC completos demodulados y llama a Procesar().
            _processingThread = new Thread(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                while (!_cts.Token.IsCancellationRequested)
                {
                    if (_mensajesCapturados.TryDequeue(out string bits))
                    {
                        bool correcto = false;
                        try
                        {
                            correcto = _procesamiento.Procesar(bits);
                        }
                        catch (Exception ex)
                        {
                            LogToDisplay($"[Error en ProcesarBits] {ex.Message}");
                        }
                        finally
                        {
                            _form?.RegistrarMensajeRecibido(correcto);
                        }
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
            })
            {
                IsBackground = true,
                Name = "DSC-Processor"
            };
            _processingThread.Start();

            // ── Callback de audio ────────────────────────────────────────────────────
            // Responsabilidad única: elevar prioridad del thread de NAudio en la primera
            // invocación, copiar el buffer y encolar. Sin lógica, sin loops, sin locks.
            bool _audioThreadPrioritySet = false;
            _waveIn.DataAvailable += (s, a) =>
            {
                if (pausa) return;
                if (!_audioThreadPrioritySet)
                {
                    Thread.CurrentThread.Priority = ThreadPriority.Highest;
                    _audioThreadPrioritySet = true;
                }
                if (a.BytesRecorded <= 0) return;

                // Copiar el buffer — NAudio reutiliza el array subyacente en el siguiente
                // callback, así que hay que copiar antes de encolar.
                byte[] copy = new byte[a.BytesRecorded]; 
                Buffer.BlockCopy(a.Buffer, 0, copy, 0, a.BytesRecorded);
                long captureTicks = Stopwatch.GetTimestamp();
                long lastTicks = Interlocked.Exchange(ref _audioCallbackLastTicks, captureTicks);
                if (lastTicks != 0)
                    UpdateMax(ref _audioCallbackMaxGapTicks, captureTicks - lastTicks);

                long sequence = Interlocked.Increment(ref _audioCallbackSequence);
                Interlocked.Increment(ref _audioBlocksReceived);
                Interlocked.Add(ref _audioBytesTotal, a.BytesRecorded);

                if (_audioQueue.TryAdd((sequence, copy, a.BytesRecorded, captureTicks)))
                {
                    Interlocked.Increment(ref _audioBlocksEnqueued);
                }
                else
                {
                    Interlocked.Increment(ref _audioBlocksDropped);
                }
            };

            _waveIn.RecordingStopped += (s, a) =>
            {
                LogToDisplay("Grabación detenida.\n");
            };

            LogToDisplay("\nEscuchando...\n");
            _waveIn.StartRecording();

            // El thread interno de NAudio que dispara DataAvailable no es accesible
            // directamente, pero WaveInEvent usa un thread del ThreadPool con prioridad
            // Normal. Subir el thread de la aplicación a High (proceso) ya le da ventaja
            // frente al resto del sistema. Para el callback en sí, NAudio respeta la
            // prioridad del proceso, así que este ajuste es suficiente.
        }

        public void DetenerCaptura()
        {
            if (!_isRunning)
            {
                LogToDisplay("[Advertencia] Captura no en progreso.\n");
                return;
            }

            _isRunning = false;
            _waveIn?.StopRecording();

            // Marcar la cola de audio como completa ANTES de cancelar el token.
            // Así GetConsumingEnumerable() saldrá limpiamente sin lanzar excepción
            // de cancelación fuera del contexto del try-catch.
            _audioQueue?.CompleteAdding();

            // Cancelar el token de cancelación — detiene DSC-Processor
            _cts?.Cancel();

            // Esperar a que ambos threads terminen
            _demodThread?.Join(2000);
            _processingThread?.Join(2000);

            // Restaurar prioridad del proceso a Normal al detener la captura
            try
            {
                System.Diagnostics.Process.GetCurrentProcess().PriorityClass =
                    System.Diagnostics.ProcessPriorityClass.Normal;
            }
            catch { }

            // Limpiar recursos
            _waveIn?.Dispose();
            _cts?.Dispose();  // ⚠️ Importante: Dispose para liberar recursos
            _cts = null;      // Preparar para la próxima captura

        }

        public void CambiarModo()
        {
            if (!_isRunning)
            {
                LogToDisplay("[Advertencia] Captura no en progreso para cambiar modo.\n");
                return;
            }

            DetenerCaptura();
            Thread.Sleep(500);
            IniciarCaptura();
        }

        public void CambiarDispositivo()
        {
            if (!_isRunning)
            {
                LogToDisplay("[Advertencia] Captura no en progreso para cambiar dispositivo.\n");
                return;
            }

            DetenerCaptura();
            Thread.Sleep(500);
            IniciarCaptura();
        }

        public void END()
        {
            _waveIn.StopRecording();
        }
        public void Pause()
        {
            pausa = true;
        }
        public void Resume()
        {
            pausa = false;
        }

    }
}
