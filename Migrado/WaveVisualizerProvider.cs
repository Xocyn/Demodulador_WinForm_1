using NAudio.Wave;
using System;
using System.Collections.Generic;

namespace Demodulador_WinForm_1
{
    /// <summary>
    /// Proveedor de visualización de ondas que captura muestras de audio
    /// y las envía a un callback para actualizar el control waveViewer1.
    /// </summary>
    public class WaveVisualizerProvider : IWaveProvider
    {
        private readonly IWaveProvider _sourceProvider;
        private readonly Action<short[]> _onSamplesReady;
        private readonly object _lock = new object();

        public WaveFormat WaveFormat => _sourceProvider.WaveFormat;

        public WaveVisualizerProvider(IWaveProvider sourceProvider, Action<short[]> onSamplesReady)
        {
            _sourceProvider = sourceProvider ?? throw new ArgumentNullException(nameof(sourceProvider));
            _onSamplesReady = onSamplesReady ?? throw new ArgumentNullException(nameof(onSamplesReady));
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = _sourceProvider.Read(buffer, offset, count);

            // Convertir bytes a shorts (16-bit audio)
            if (bytesRead > 0)
            {
                int sampleCount = bytesRead / 2;
                short[] samples = new short[sampleCount];

                lock (_lock)
                {
                    Buffer.BlockCopy(buffer, offset, samples, 0, bytesRead);
                    _onSamplesReady?.Invoke(samples);
                }
            }

            return bytesRead;
        }
    }
}
