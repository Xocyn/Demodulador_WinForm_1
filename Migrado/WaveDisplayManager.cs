using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Demodulador_WinForm_1
{
    /// <summary>
    /// Gestor para acumular y actualizar muestras de audio en el waveViewer1.
    /// Realiza downsampling y limita la frecuencia de actualización para no sobrecargar la UI.
    /// </summary>
    public class WaveDisplayManager
    {
        private readonly Action<short[]> _updateDisplay;
        private readonly List<short> _buffer = new List<short>();
        private readonly int _targetSamples;
        private readonly int _updateIntervalMs;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly object _lock = new object();

        /// <summary>
        /// Crea un nuevo gestor de visualización de ondas.
        /// </summary>
        /// <param name="updateDisplay">Callback para actualizar el control con muestras</param>
        /// <param name="targetSamples">Número de muestras a mostrar (ej: 2048)</param>
        /// <param name="updateIntervalMs">Intervalo mínimo entre actualizaciones en ms (ej: 50)</param>
        public WaveDisplayManager(Action<short[]> updateDisplay, int targetSamples = 2048, int updateIntervalMs = 50)
        {
            _updateDisplay = updateDisplay ?? throw new ArgumentNullException(nameof(updateDisplay));
            _targetSamples = targetSamples;
            _updateIntervalMs = updateIntervalMs;
            _stopwatch.Start();
        }

        /// <summary>
        /// Agrega muestras al buffer y actualiza la pantalla si es necesario.
        /// </summary>
        public void AddSamples(short[] samples)
        {
            if (samples == null || samples.Length == 0)
                return;

            lock (_lock)
            {
                _buffer.AddRange(samples);

                // Limitar el buffer a evitar crecer indefinidamente
                if (_buffer.Count > _targetSamples * 4)
                {
                    int toRemove = _buffer.Count - _targetSamples * 2;
                    _buffer.RemoveRange(0, toRemove);
                }

                // Actualizar solo cada X ms para no sobrecargar la UI
                if (_stopwatch.ElapsedMilliseconds >= _updateIntervalMs)
                {
                    if (_buffer.Count >= _targetSamples)
                    {
                        // Tomar las últimas _targetSamples muestras
                        short[] displaySamples = _buffer
                            .Skip(_buffer.Count - _targetSamples)
                            .ToArray();

                        // Ejecutar el callback en thread de UI
                        _updateDisplay?.Invoke(displaySamples);
                    }

                    _stopwatch.Restart();
                }
            }
        }

        /// <summary>
        /// Limpia el buffer de muestras.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _buffer.Clear();
            }
        }

        /// <summary>
        /// Retorna el número actual de muestras en buffer.
        /// </summary>
        public int BufferedSampleCount
        {
            get
            {
                lock (_lock)
                {
                    return _buffer.Count;
                }
            }
        }
    }
}
