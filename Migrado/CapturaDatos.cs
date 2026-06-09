using Dem_v2;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Demodulador_WinForm_1
{
    public class CapturaDatos
    {
        private enum Estado
        {
            EsperandoInicio,
            Grabando,
            Cooldown
        }

        private readonly Demodulador_DSC _form;

        // Cola thread-safe: el thread de audio deposita mensajes capturados aquí.
        // El thread de procesamiento los consume de forma independiente.
        // Así el callback DataAvailable nunca bloquea, sin importar cuánto tarde ProcesarBits.
        private readonly ConcurrentQueue<string> _mensajesCapturados = new();

        // CancellationToken para detener el thread de procesamiento limpiamente al salir.
        // ⚠️ NOTA: Se crea de nuevo cada vez que se inicia captura, NO es readonly
        private CancellationTokenSource _cts;

        // Lock para proteger las variables de estado compartidas entre el thread de audio
        // y el thread principal (cambio de modo con M).
        private readonly object _lock = new();

        private WaveInEvent _waveIn;
        private BFSKDemodulator _demod;
        private Thread _processingThread;
        private bool _isRunning = false;
        private WaveDisplayManager _waveDisplayManager;
        private readonly Procesamiento _procesamiento;  // ← agregar campo

        private bool pausa = false;

        public CapturaDatos(Procesamiento procesamiento, Demodulador_DSC form = null)  // ← agregar parámetro
        {
            _form = form;
            _procesamiento = procesamiento;  // ← guardar referencia
        }
        private void LogToDisplay(string message)
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

        private void UpdateWaveDisplay(short[] samples)
        {
            _waveDisplayManager?.AddSamples(samples);
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

            public SilenceDetector(double umbralEnergiaPorMuestra, double silencioRequeridoMs, int sampleRate = 44100)
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

            bool vhfMode = _form.combox_hf_vhf.SelectedIndex == 1;

            _waveIn = new WaveInEvent();
            _waveIn.DeviceNumber = _form.combox_dispositivos.SelectedIndex;
            _waveIn.WaveFormat = new WaveFormat(44100, 16, 1);

            _demod = new BFSKDemodulator(vhfMode);

            // Instanciar Procesamiento con referencias a los controles del formulario
            //var procesamiento = new Procesamiento(_form.MAINDISPLAY, _form);


            // ── Inicializar visualización de onda ────────────────────────────────────
            // Crear callback que actualice el waveViewer1 de forma thread-safe
            _waveDisplayManager = new WaveDisplayManager(
                updateDisplay: (samples) =>
                {
                    if (_form?.InvokeRequired == true)
                    {
                        _form.Invoke(() => _form.waveViewer1.AddSamples(samples));
                    }
                    else
                    {
                        _form?.waveViewer1.AddSamples(samples);
                    }
                },
                targetSamples: 4096,      // Mostrar 4096 muestras
                updateIntervalMs: 50      // Actualizar cada 50ms (~20 FPS)
            );

            const int PhaseCount = 4; // 2 anda joya
            var syncBuffers = new StringBuilder[PhaseCount];
            for (int p = 0; p < PhaseCount; p++) syncBuffers[p] = new StringBuilder();

            int lockedPhase = -1;
            StringBuilder bitAccumulator = new StringBuilder();

            const string startPattern = "01010101010101010101"; // 20 bits

            Estado estado = Estado.EsperandoInicio;
            int cooldownMs = 50; //250 andaba
            DateTime cooldownHasta = DateTime.MinValue;

            // ── Detector de silencio ─────────────────────────────────────────────
            // Umbral: misma fórmula que BFSKDemodulator._energyThreshold pero
            // normalizada por muestra (sin multiplicar por samplesPerSymbol).
            // Esto lo hace independiente del tamaño del bloque de audio.
            double umbralEnergiaPorMuestra = Math.Pow(short.MaxValue * 0.01, 2);

            // VHF (1200 bps): mensaje más corto → silencio más breve para cortar rápido.
            // HF  (100 bps):  símbolo = 10 ms → necesitamos más margen para no cortar
            //                 entre símbolos lentos.
            double silencioRequeridoMs = vhfMode ? 300.0 : 800.0;

            var silenceDetector = new SilenceDetector(umbralEnergiaPorMuestra, silencioRequeridoMs);

            // ── Thread de procesamiento ──────────────────────────────────────────────
            // Consume mensajes de la cola y llama a ProcesarBits sin tocar el thread de audio.
            _processingThread = new Thread(() =>
            {
                // TimeCritical es la prioridad más alta disponible para un thread de usuario.
                // Garantiza que el dequeue y el Procesar() no sean desalojados por threads
                // de menor prioridad (UI, logger, etc.) mientras hay mensajes pendientes.
                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                while (!_cts.Token.IsCancellationRequested)
                {
                    if (_mensajesCapturados.TryDequeue(out string bits))
                    {
                        try
                        {
                            _procesamiento.Procesar(bits);
                        }
                        catch (Exception ex)
                        {
                            LogToDisplay($"[Error en ProcesarBits] {ex.Message}\n");
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
            _waveIn.DataAvailable += (s, a) =>
            {
                // ── Pausa ─────────────────────────────────────────────────────────────
                if (pausa)
                    return;

                // ── Capturar muestras para visualización ──────────────────────────────
                if (a.BytesRecorded > 0)
                {
                    int sampleCount = a.BytesRecorded / 2;
                    short[] samples = new short[sampleCount];
                    Buffer.BlockCopy(a.Buffer, 0, samples, 0, a.BytesRecorded);
                    UpdateWaveDisplay(samples);
                }

                string[] bitsByPhase;
                lock (_lock)
                {
                    bitsByPhase = _demod.ProcessAudio(a.Buffer, a.BytesRecorded);
                }

                // ── Cooldown ─────────────────────────────────────────────────────────
                lock (_lock)
                {
                    if (estado == Estado.Cooldown)
                    {
                        if (DateTime.Now > cooldownHasta)
                        {
                            LogToDisplay("Cooldown terminado. Escuchando...\n");
                            estado = Estado.EsperandoInicio;
                            lockedPhase = -1;
                            _demod.ResetTiming();
                            silenceDetector.Reset();
                            for (int p = 0; p < PhaseCount; p++) syncBuffers[p].Clear();
                        }
                        return;
                    }
                }

                // ── PASO 1: Acumular bits del bloque ─────────────────────────────────
                // Los bits se procesan siempre antes de evaluar cualquier condición de
                // corte, garantizando que ningún bit del bloque actual se pierda.
                int phaseStart, phaseEnd;
                lock (_lock) { phaseStart = (lockedPhase >= 0) ? lockedPhase : 0; }
                lock (_lock) { phaseEnd = (lockedPhase >= 0) ? lockedPhase + 1 : PhaseCount; }

                for (int ph = phaseStart; ph < phaseEnd; ph++)
                {
                    bool shouldProcess;
                    lock (_lock) { shouldProcess = (lockedPhase < 0 || ph == lockedPhase); }
                    if (!shouldProcess) continue;

                    foreach (char bit in bitsByPhase[ph])
                    {
                        Estado estadoActual;
                        lock (_lock) { estadoActual = estado; }

                        // ── ESTADO: EsperandoInicio ───────────────────────────────────
                        if (estadoActual == Estado.EsperandoInicio)
                        {
                            lock (_lock)
                            {
                                syncBuffers[ph].Append(bit);
                                if (syncBuffers[ph].Length > startPattern.Length)
                                    syncBuffers[ph].Remove(0, 1);

                                // Detección por DOT PATTERN (01010101...)
                                if (syncBuffers[ph].ToString().EndsWith(startPattern))
                                {
                                    ClearDisplay();
                                    LogToDisplay($"DOT PATTERN detectado (fase {ph})\n");
                                    IniciarGrabacion(ph);
                                }
                                // Detección alternativa: valor 125 alineado en posición 0..9
                                else if (syncBuffers[ph].Length >= 10)
                                {
                                    string sub = syncBuffers[ph].ToString().Substring(0, 10);
                                    if (Decodificador.TryDeco(sub, out int v) && v == 125)
                                    {
                                        ClearDisplay();
                                        LogToDisplay($"Valor 125 detectado sin DOT PATTERN (fase {ph})\n");
                                        IniciarGrabacion(ph);
                                    }
                                }
                            }
                        }

                        // ── ESTADO: Grabando — solo acumular bits ─────────────────────
                        // No se evalúa ninguna condición de corte aquí.
                        // El silencio se evalúa al final del callback, una vez que
                        // todos los bits del bloque ya están en bitAccumulator.
                        else if (estadoActual == Estado.Grabando)
                        {
                            lock (_lock)
                            {
                                bitAccumulator.Append(bit);
                            }
                        }
                    }
                }

                // ── PASO 2: Evaluar silencio sobre el bloque completo ─────────────────
                // Se ejecuta después de acumular todos los bits del bloque.
                // silenceDetector.Actualizar() mide la energía RMS del audio crudo:
                //   · Si hay señal  → resetea el contador interno y devuelve false.
                //   · Si hay silencio → acumula ms y devuelve true al superar el umbral.
                lock (_lock)
                {
                    if (estado == Estado.Grabando)
                    {
                        if (silenceDetector.Actualizar(a.Buffer, a.BytesRecorded))
                        {
                            LogToDisplay($"[Silencio] {silenceDetector.SilencioAcumuladoMs:F0} ms sin señal → finalizando captura\n");
                            FinalizarCaptura("SILENCIO");
                        }
                    }
                }

                void IniciarGrabacion(int ph)
                {
                    ClearMAIN();
                    lockedPhase = ph;
                    _demod.LockPhase(ph);
                    estado = Estado.Grabando;
                    bitAccumulator.Clear();
                    silenceDetector.Reset();
                    LogToDisplay($"[IniciarGrabacion] Fase {ph} bloqueada.\n");
                }

                void FinalizarCaptura(string motivo)
                {
                    string capturado = bitAccumulator.ToString();

                    LogToDisplay($"[FinalizarCaptura - {motivo}] {capturado.Length} bits capturados\n");

                    if (capturado.Length > 0)
                        _mensajesCapturados.Enqueue(capturado);
                    else
                        LogToDisplay("[Advertencia] No se encoló mensaje: cadena vacía\n");

                    bitAccumulator.Clear();
                    estado = Estado.Cooldown;
                    cooldownHasta = DateTime.Now.AddMilliseconds(cooldownMs);
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

            // Cancelar el token de cancelación
            _cts?.Cancel();

            // Esperar a que el thread de procesamiento termine
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

            // Limpiar visualización de onda
            _waveDisplayManager?.Clear();
            _waveDisplayManager = null;
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