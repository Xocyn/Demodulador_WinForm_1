namespace Demodulador_WinForm_1;

internal sealed class AdaptiveNoiseGate
{
    private const double MinimumDbFs = -120.0;
    private const double CalibrationPercentile = 0.30;

    private readonly int _sampleRate;
    private readonly long _calibrationSamplesRequired;
    private readonly double _openMarginDb;
    private readonly double _closeMarginDb;
    private readonly double _attackMilliseconds;
    private readonly double _releaseMilliseconds;
    private readonly List<double> _calibrationLevels = new();

    private long _calibrationSamples;
    private double _noiseFloorDbFs = MinimumDbFs;
    private double _aboveOpenMilliseconds;
    private double _belowCloseMilliseconds;
    private bool _isCalibrating = true;
    private bool _isGateOpen;

    public AdaptiveNoiseGate(
        int sampleRate,
        int calibrationMilliseconds,
        double openMarginDb,
        double closeMarginDb,
        double attackMilliseconds,
        double releaseMilliseconds)
    {
        if (sampleRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        if (calibrationMilliseconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(calibrationMilliseconds));
        if (openMarginDb <= closeMarginDb)
            throw new ArgumentException("El margen de apertura debe ser mayor que el margen de cierre.");
        if (attackMilliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(attackMilliseconds));
        if (releaseMilliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(releaseMilliseconds));

        _sampleRate = sampleRate;
        _calibrationSamplesRequired = (long)Math.Ceiling(sampleRate * calibrationMilliseconds / 1000.0);
        _openMarginDb = openMarginDb;
        _closeMarginDb = closeMarginDb;
        _attackMilliseconds = attackMilliseconds;
        _releaseMilliseconds = releaseMilliseconds;
    }

    public AudioLevelSnapshot Update(ReadOnlySpan<short> samples)
    {
        (double rmsDbFs, double peakDbFs) = CalculateLevels(samples);
        double blockMilliseconds = samples.Length * 1000.0 / _sampleRate;

        if (_isCalibrating)
        {
            _calibrationLevels.Add(rmsDbFs);
            _calibrationSamples += samples.Length;

            if (_calibrationSamples >= _calibrationSamplesRequired)
                CompleteCalibration();

            return CreateSnapshot(rmsDbFs, peakDbFs);
        }

        double openThresholdDbFs = _noiseFloorDbFs + _openMarginDb;
        double closeThresholdDbFs = _noiseFloorDbFs + _closeMarginDb;

        if (!_isGateOpen)
        {
            _belowCloseMilliseconds = 0.0;
            _aboveOpenMilliseconds = rmsDbFs >= openThresholdDbFs
                ? _aboveOpenMilliseconds + blockMilliseconds
                : 0.0;

            if (_aboveOpenMilliseconds >= _attackMilliseconds)
            {
                _isGateOpen = true;
                _aboveOpenMilliseconds = 0.0;
            }
        }
        else
        {
            _aboveOpenMilliseconds = 0.0;
            _belowCloseMilliseconds = rmsDbFs < closeThresholdDbFs
                ? _belowCloseMilliseconds + blockMilliseconds
                : 0.0;

            if (_belowCloseMilliseconds >= _releaseMilliseconds)
            {
                _isGateOpen = false;
                _belowCloseMilliseconds = 0.0;
            }
        }

        return CreateSnapshot(rmsDbFs, peakDbFs);
    }

    private void CompleteCalibration()
    {
        _calibrationLevels.Sort();
        int percentileIndex = (int)Math.Round(
            (_calibrationLevels.Count - 1) * CalibrationPercentile,
            MidpointRounding.AwayFromZero);
        _noiseFloorDbFs = _calibrationLevels[Math.Clamp(percentileIndex, 0, _calibrationLevels.Count - 1)];
        _isCalibrating = false;
        _isGateOpen = false;
        _aboveOpenMilliseconds = 0.0;
        _belowCloseMilliseconds = 0.0;
    }

    private AudioLevelSnapshot CreateSnapshot(double rmsDbFs, double peakDbFs)
    {
        double progress = Math.Clamp(
            _calibrationSamples / (double)_calibrationSamplesRequired,
            0.0,
            1.0);

        return new AudioLevelSnapshot(
            rmsDbFs,
            peakDbFs,
            _noiseFloorDbFs,
            _noiseFloorDbFs + _openMarginDb,
            _noiseFloorDbFs + _closeMarginDb,
            _isGateOpen,
            _isCalibrating,
            progress);
    }

    private static (double RmsDbFs, double PeakDbFs) CalculateLevels(ReadOnlySpan<short> samples)
    {
        if (samples.IsEmpty)
            return (MinimumDbFs, MinimumDbFs);

        double energy = 0.0;
        int peak = 0;

        foreach (short sample in samples)
        {
            energy += (double)sample * sample;
            peak = Math.Max(peak, Math.Abs((int)sample));
        }

        double rms = Math.Sqrt(energy / samples.Length);
        return (ToDbFs(rms), ToDbFs(peak));
    }

    private static double ToDbFs(double amplitude)
    {
        if (amplitude <= 0.0)
            return MinimumDbFs;

        return Math.Max(MinimumDbFs, 20.0 * Math.Log10(amplitude / 32768.0));
    }
}
