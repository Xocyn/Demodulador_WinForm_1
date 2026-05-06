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

        public CapturaDatos(Demodulador_DSC form)
        {
            _form = form;
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

        public void IniciarCaptura()
        {
            if (_isRunning)
            {
                LogToDisplay("[Advertencia] Captura ya en progreso.\n");
                return;
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
            var procesamiento = new Procesamiento(_form.MAINDISPLAY);

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

            const int PhaseCount = 4;
            var syncBuffers = new StringBuilder[PhaseCount];
            for (int p = 0; p < PhaseCount; p++) syncBuffers[p] = new StringBuilder();

            int lockedPhase = -1;
            StringBuilder decodeBuffer = new StringBuilder();
            StringBuilder bitAccumulator = new StringBuilder();

            const string startPattern = "01010101010101010101"; // 20 bits
            int phasingStartOffset = 0;
            bool extensionDetected = false;

            Estado estado = Estado.EsperandoInicio;
            int cooldownMs = 250; // 1000 (original)
            DateTime cooldownHasta = DateTime.MinValue;
            DateTime inicioGrabacion = DateTime.MinValue;

            // VHF: ~0.45 s por mensaje → timeout 2 s
            // HF:  ~5.4 s por mensaje → timeout 10 s
            int maxGrabacionSeg = vhfMode ? 2 : 10;

            // ── Thread de procesamiento ──────────────────────────────────────────────
            // Consume mensajes de la cola y llama a ProcesarBits sin tocar el thread de audio.
            _processingThread = new Thread(() =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    if (_mensajesCapturados.TryDequeue(out string bits))
                    {
                        try
                        {
                            procesamiento.Procesar(bits, extensionDetected);
                            extensionDetected = false;
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
                // ── Capturar muestras para visualización ──────────────────────────────
                // Convertir bytes a shorts para el waveViewer
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
                            for (int p = 0; p < PhaseCount; p++) syncBuffers[p].Clear();
                        }
                        return;
                    }
                }

                // ── Chequeo de timeout ────────────────────────────────────────────────
                lock (_lock)
                {
                    if (estado == Estado.Grabando)
                    {
                        if ((DateTime.Now - inicioGrabacion).TotalSeconds > maxGrabacionSeg)
                        {
                            LogToDisplay("Timeout de grabación\n");
                            FinalizarCaptura("TIMEOUT");
                        }
                    }
                }

                bool debeFinalizarLoop = false;

                int phaseStart, phaseEnd;
                lock (_lock) { phaseStart = (lockedPhase >= 0) ? lockedPhase : 0; }
                lock (_lock) { phaseEnd = (lockedPhase >= 0) ? lockedPhase + 1 : PhaseCount; }

                for (int ph = phaseStart; ph < phaseEnd && !debeFinalizarLoop; ph++)
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

                        // ── ESTADO: Grabando ──────────────────────────────────────────
                        else if (estadoActual == Estado.Grabando)
                        {
                            lock (_lock)
                            {
                                bitAccumulator.Append(bit);
                                decodeBuffer.Append(bit);

                                // Detección de EOS CONSECUTIVOS (dos valores 127 seguidos)
                                if (decodeBuffer.Length >= 20)
                                {
                                    for (int w = 0; w <= decodeBuffer.Length - 40; w++)
                                    {
                                        string ventana1 = decodeBuffer.ToString(w, 10);
                                        string ventana2 = decodeBuffer.ToString(w + 10, 10);

                                        bool es127_1 = Decodificador.TryDeco(ventana1, out int val1) && (val1 == 127 || val1 == 117 || val1 == 122);
                                        bool es127_2 = Decodificador.TryDeco(ventana2, out int val2) && (val2 == 127 || val2 == 117 || val2 == 122);

                                        if (es127_1 && es127_2)
                                        {
                                            FinalizarCaptura("EOS");
                                            debeFinalizarLoop = true;
                                            break;
                                        }
                                    }

                                    if (decodeBuffer.Length > 1000)
                                    {
                                        decodeBuffer.Remove(0, 1);
                                    }
                                }
                            }
                        }

                        if (debeFinalizarLoop) break;
                    }
                }

                void IniciarGrabacion(int ph)
                {
                    ClearMAIN();
                    lockedPhase = ph;
                    _demod.LockPhase(ph);
                    inicioGrabacion = DateTime.Now;
                    estado = Estado.Grabando;
                    phasingStartOffset = 0;
                    decodeBuffer.Clear();
                    bitAccumulator.Clear();
                    LogToDisplay($"[IniciarGrabacion] Fase {ph} bloqueada.\n");
                }

                void FinalizarCaptura(string motivo)
                {
                    int offset = Math.Max(0, Math.Min(phasingStartOffset, bitAccumulator.Length));
                    string capturado = bitAccumulator.ToString(offset, bitAccumulator.Length - offset);

                    LogToDisplay($"[FinalizarCaptura - {motivo}] Bits acumulados: {bitAccumulator.Length}, offset: {offset}, capturado: {capturado.Length} bits\n");

                    if (capturado.Length > 0)
                    {
                        _mensajesCapturados.Enqueue(capturado);
                    }
                    else
                    {
                        LogToDisplay("[Advertencia] No se encoló mensaje: cadena vacía\n");
                    }

                    decodeBuffer.Clear();
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
    }
}