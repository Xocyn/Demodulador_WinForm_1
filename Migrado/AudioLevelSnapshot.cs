namespace Demodulador_WinForm_1;

public readonly record struct AudioLevelSnapshot(
    double RmsDbFs,
    double PeakDbFs,
    double NoiseFloorDbFs,
    double OpenThresholdDbFs,
    double CloseThresholdDbFs,
    bool IsGateOpen,
    bool IsCalibrating,
    double CalibrationProgress);
