using System.ComponentModel.DataAnnotations;

namespace RuneshapePriceChecker.OCR;

public sealed class OcrOptions
{
    public string TesseractDataPath { get; set; } = string.Empty;
    [Required]
    public string Language { get; set; } = "eng";
    public bool EnableImagePreprocessing { get; set; } = true;
    [Range(0, 255)]
    public int BinarizationThreshold { get; set; } = 145;
    public bool EnableTextColorFiltering { get; set; } = true;
    [Range(0, 255)]
    public int TextColorTargetR { get; set; } = 50;
    [Range(0, 255)]
    public int TextColorTargetG { get; set; } = 42;
    [Range(0, 255)]
    public int TextColorTargetB { get; set; } = 34;
    [Range(0, 255)]
    public int TextColorTolerance { get; set; } = 47;
    [Range(0, 255)]
    public int TextColorMaxLuminance { get; set; } = 145;
    [Range(0, 255)]
    public int TextColorMaxChannelSpread { get; set; } = 29;
    public bool SaveDebugImages { get; set; }
    [Range(1, 30)]
    public int DebugImageIntervalSeconds { get; set; } = 15;
    public string DebugImageDirectory { get; set; } = string.Empty;

    /// <summary>
    /// How many past captures to keep under <c>history/</c>, newest kept, oldest pruned. Zero
    /// disables it.
    ///
    /// The numbered debug images all use fixed filenames and are overwritten every cycle, so by
    /// the time anyone looks at them the panel that went wrong is long gone — a report and its
    /// evidence cannot be collected at the same moment. Keeping a short rolling history means the
    /// user can play, say what looked wrong, and the frame is still there.
    /// </summary>
    [Range(0, 200)]
    public int DebugFrameHistory { get; set; } = 40;
    public bool DebugOverlay { get; set; }
    public bool HideDebugOverlayWhenInterfaceNotDetected { get; set; }
    [Range(0, 2)]
    public int OcrEngineMode { get; set; } = 2;

    public string OcrBackend { get; set; } = "windows";
    public string CaptureMode { get; set; } = "printwindow";
    [Range(50, 200)]
    public int ScanIntervalMs { get; set; } = 100;
    public bool BypassOcrCache { get; set; }
    public int PerfMetricsInterval { get; set; }


    // Overlay scale (auto when null, manual override when set)
    public float? OverlayScale { get; set; }

    // League panel detection thresholds
    [Range(0.0, 1.0)]
    public double PanelLeftFraction { get; set; } = 0.30;
    [Range(0.0, 1.0)]
    public double PanelRightFraction { get; set; } = 0.98;
    [Range(0.0, 1.0)]
    public double PanelTopRowFraction { get; set; } = 0.26;
    [Range(1, 765)]
    public int PanelBrightnessThreshold { get; set; } = 120;
    [Range(1, 765)]
    public int PanelBlackPixelMaxSum { get; set; } = 20;
    [Range(1, 10000)]
    public int PanelMinBlackPixels { get; set; } = 60;
}
