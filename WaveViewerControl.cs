using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Demodulador_WinForm_1
{
    /// <summary>
    /// Control personalizado para visualizar ondas de audio en tiempo real.
    /// Dibuja las muestras como una línea continua.
    /// </summary>
    public class WaveViewerControl : Control
    {
        private short[] _samples = Array.Empty<short>();
        private readonly object _lock = new object();
        private readonly Color _waveColor = Color.Lime;
        private readonly Color _backgroundColor = Color.Black;

        public WaveViewerControl()
        {
            DoubleBuffered = true;
            BackColor = _backgroundColor;
            ForeColor = _waveColor;
            Margin = new Padding(0);
        }

        /// <summary>
        /// Agrega muestras de audio para visualizar.
        /// </summary>
        public void AddSamples(short[] samples)
        {
            if (samples == null || samples.Length == 0)
                return;

            lock (_lock)
            {
                _samples = (short[])samples.Clone();
            }

            // Solicitar redibujado desde el thread de UI
            if (InvokeRequired)
            {
                try
                {
                    Invoke(() => Invalidate());
                }
                catch
                {
                    // Control puede haber sido destruido
                }
            }
            else
            {
                Invalidate();
            }
        }

        /// <summary>
        /// Limpia las muestras visualizadas.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _samples = Array.Empty<short>();
            }

            if (InvokeRequired)
            {
                try
                {
                    Invoke(() => Invalidate());
                }
                catch
                {
                    // Control puede haber sido destruido
                }
            }
            else
            {
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.Clear(_backgroundColor);

            if (Width <= 0 || Height <= 0)
                return;

            short[] samples;
            lock (_lock)
            {
                if (_samples.Length == 0)
                    return;

                samples = _samples;
            }

            // Dibujar la onda
            DrawWave(e.Graphics, samples);
        }

        private void DrawWave(Graphics g, short[] samples)
        {
            if (samples.Length < 2)
                return;

            int width = Width;
            int height = Height;
            float centerY = height / 2f;
            float scaleX = (float)width / samples.Length;
            float scaleY = height / (2f * short.MaxValue);

            using (var pen = new Pen(_waveColor, 1f))
            {
                for (int i = 0; i < samples.Length - 1; i++)
                {
                    float x1 = i * scaleX;
                    float y1 = centerY - (samples[i] * scaleY);

                    float x2 = (i + 1) * scaleX;
                    float y2 = centerY - (samples[i + 1] * scaleY);

                    // Asegurar que los valores estén dentro del rango visible
                    y1 = Math.Max(0, Math.Min(height - 1, y1));
                    y2 = Math.Max(0, Math.Min(height - 1, y2));

                    g.DrawLine(pen, x1, y1, x2, y2);
                }
            }

            // Dibujar línea central
            using (var centerPen = new Pen(Color.DimGray, 1f))
            {
                centerPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                g.DrawLine(centerPen, 0, centerY, width, centerY);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // No llamar base para evitar parpadeo
            e.Graphics.Clear(_backgroundColor);
        }
    }
}
