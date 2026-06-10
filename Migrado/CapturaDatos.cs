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

        // Cola de mensajes DSC demodulados — el thread de demodulación deposita aquí,
        // el thread de procesamiento consume.
        private readonly ConcurrentQueue<string> _mensajesCapturados = new();

        // Cola de bloques de audio crudos — DataAvailable deposita aquí (copia mínima),
        // el thread de demodulación consume. BlockingCollection permite Wait() sin spinning.
        private BlockingCollection<(byte[] buffer, int bytesRecorded)> _audioQueue;

        // CancellationToken para detener los threads limpiamente al salir.
        // ⚠️ NOTA: Se crea de nuevo cada vez que se inicia captura, NO es readonly
        private CancellationTokenSource _cts;

        private Thread _demodThread;  // consume _audioQueue, produce _mensajesCapturados

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

            // Cola de audio crudo con capacidad acotada: si el thread de demodulación
            // no da abasto, Add() bloqueará el callback — señal de que el sistema está
            // sobrecargado. 32 bloques × ~50ms = ~1.6s de buffer máximo.
            _audioQueue = new BlockingCollection<(byte[], int)>(boundedCapacity: 32);

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
                const int PhaseCount = 4;
                var syncBuffers = new StringBuilder[PhaseCount];
                for (int p = 0; p < PhaseCount; p++) syncBuffers[p] = new StringBuilder();

                int lockedPhase = -1;
                StringBuilder bitAccumulator = new StringBuilder();
                const string startPattern = "01010101010101010101";
                Estado estado = Estado.EsperandoInicio;
                int cooldownMs = 100;
                DateTime cooldownHasta = DateTime.MinValue;
                double umbralEnergiaPorMuestra = Math.Pow(short.MaxValue * 0.01, 2);
                double silencioRequeridoMs = vhfMode ? 300.0 : 800.0;
                var silenceDetector = new SilenceDetector(umbralEnergiaPorMuestra, silencioRequeridoMs);

                foreach (var (buffer, bytesRecorded) in _audioQueue.GetConsumingEnumerable(_cts.Token))
                {
                    try
                    {
                        // ── Visualización de onda ─────────────────────────────────────
                        if (bytesRecorded > 0)
                        {
                            int sampleCount = bytesRecorded / 2;
                            short[] samples = new short[sampleCount];
                            Buffer.BlockCopy(buffer, 0, samples, 0, bytesRecorded);
                            UpdateWaveDisplay(samples);
                        }

                        string[] bitsByPhase;

                        bitsByPhase = _demod.ProcessAudio(buffer, bytesRecorded);


                        // ── Cooldown ──────────────────────────────────────────────────

                        if (estado == Estado.Cooldown)
                        {
                            if (DateTime.Now > cooldownHasta)
                            {
                                LogToDisplay("Cooldown terminado. Escuchando...");
                                estado = Estado.EsperandoInicio;
                                lockedPhase = -1;
                                _demod.ResetTiming();
                                silenceDetector.Reset();
                                for (int p = 0; p < PhaseCount; p++) syncBuffers[p].Clear();
                            }
                            else continue;
                        }

                        // ── PASO 1: Acumular bits del bloque ──────────────────────────
                        int phaseStart, phaseEnd;
                        { phaseStart = (lockedPhase >= 0) ? lockedPhase : 0; }
                        { phaseEnd = (lockedPhase >= 0) ? lockedPhase + 1 : PhaseCount; }

                        for (int ph = phaseStart; ph < phaseEnd; ph++)
                        {
                            bool shouldProcess;
                            { shouldProcess = (lockedPhase < 0 || ph == lockedPhase); }
                            if (!shouldProcess) continue;

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
                                        string sub = syncBuffers[ph].ToString().Substring(0, 10);
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
                                    { bitAccumulator.Append(bit); }
                                }
                            }
                        }

                        // ── PASO 2: Evaluar silencio ──────────────────────────────────


                        if (estado == Estado.Grabando)
                        {
                            if (silenceDetector.Actualizar(buffer, bytesRecorded))
                            {
                                LogToDisplay($"[Silencio] {silenceDetector.SilencioAcumuladoMs:F0} ms sin señal → finalizando captura");
                                FinalizarCaptura("SILENCIO");
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
                            LogToDisplay($"[IniciarGrabacion] Fase {ph} bloqueada.");
                        }

                        void FinalizarCaptura(string motivo)
                        {
                            string capturado = bitAccumulator.ToString();
                            LogToDisplay($"[FinalizarCaptura - {motivo}] {capturado.Length} bits capturados");
                            if (capturado.Length > 0)
                                _mensajesCapturados.Enqueue(capturado);
                            else
                                LogToDisplay("[Advertencia] No se encoló mensaje: cadena vacía");
                            bitAccumulator.Clear();
                            estado = Estado.Cooldown;
                            cooldownHasta = DateTime.Now.AddMilliseconds(cooldownMs);
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        LogToDisplay($"[Error en DSC-Demodulator] {ex.Message}");
                    }
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
                        try
                        {
                            _procesamiento.Procesar(bits);
                        }
                        catch (Exception ex)
                        {
                            LogToDisplay($"[Error en ProcesarBits] {ex.Message}");
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
                _audioQueue.TryAdd((copy, a.BytesRecorded));
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