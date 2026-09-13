using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RuneshapePriceChecker.OCR;
using RuneshapePriceChecker.Runes;

namespace RuneshapePriceChecker.App;

/// <summary>What a tooltip read did, for logging and for the hotkey handler to report.</summary>
public enum RuneTooltipMarkResult
{
    /// <summary>The rune the tooltip names is now in the magazine.</summary>
    Marked,

    /// <summary>It was already in the magazine and has been taken back out.</summary>
    Unmarked,

    /// <summary>The region around the cursor held no text naming a catalog rune.</summary>
    NoTooltip,

    /// <summary>The game window is not in front, or its position is not known yet.</summary>
    NoGameWindow,

    /// <summary>Capture or OCR is not available; the log says why.</summary>
    Unavailable,

    /// <summary>A previous read is still running; this press was dropped.</summary>
    Busy
}

public static class RuneTooltipMarkResultExtensions
{
    public static string Describe(this RuneTooltipMarkResult result) => result switch
    {
        RuneTooltipMarkResult.Marked => "marked as taken from its tooltip",
        RuneTooltipMarkResult.Unmarked => "removed from the magazine from its tooltip",
        RuneTooltipMarkResult.NoTooltip => "no rune under the cursor and no rune tooltip near it",
        RuneTooltipMarkResult.NoGameWindow => "the game is not in front",
        RuneTooltipMarkResult.Busy => "a tooltip read is already running",
        _ => "tooltip reading is unavailable"
    };
}

/// <summary>
/// The second half of the mark hotkey: when the cursor is not over a rune in the Combinations
/// panel, read the tooltip of whatever it is over instead. Once runes are socketed into the
/// remnant the panel is closed, but hovering a socket shows a tooltip whose first line is the
/// rune's name — so the name is OCR'd and matched against the catalog, and that rune is toggled
/// in the magazine exactly as a panel click would.
///
/// This marks by rune id, not by sprite, so it needs no binding step. The flip side is that a
/// rune whose panel sprite is still unbound will not show the grey "carried" slash on the panel
/// until the user names that sprite once in the Rune Library.
///
/// It owns its own Windows OCR engine rather than borrowing the panel reader's: the two run on
/// different threads at unrelated moments and the engine is not meant to be shared.
/// </summary>
public sealed class RuneTooltipMarkService(
    IOptionsMonitor<OcrOptions> ocrOptions,
    IPoe2WindowResolutionProvider windowResolution,
    RuneCatalog catalog,
    ILoggerFactory loggerFactory,
    ILogger<RuneTooltipMarkService> logger)
{
    private static readonly bool WindowsOcrSupported = Environment.OSVersion.Version.Build >= 17763;

    private readonly OcrCaptureStrategy _capture = new(loggerFactory.CreateLogger<OcrCaptureStrategy>());
    private readonly object _engineSync = new();
    private WindowsOcrEngine? _engine;
    private string? _engineLanguage;
    private bool _engineFailed;
    private int _busy;

    /// <summary>Reads the cursor position. Replaced in tests.</summary>
    internal Func<Point>? CursorProvider { get; set; }

    /// <summary>Captures and recognises the text in a screen rectangle. Replaced in tests.</summary>
    internal Func<Rectangle, string?>? TextReader { get; set; }

    /// <summary>
    /// Reads the tooltip under the cursor and toggles the rune it names. Synchronous and a few
    /// hundred milliseconds at most; callers on a message thread should hand it to a worker.
    /// Only one read runs at a time — a second press while one is in flight is dropped rather
    /// than queued, since a queued press would act on whatever the cursor is over later.
    /// </summary>
    public RuneTooltipMarkResult TryToggleFromTooltip() => TryToggleFromTooltip(out _);

    /// <inheritdoc cref="TryToggleFromTooltip()"/>
    /// <param name="runeName">The display name of the rune toggled, for feedback; null when none was.</param>
    public RuneTooltipMarkResult TryToggleFromTooltip(out string? runeName)
    {
        runeName = null;
        if (Interlocked.Exchange(ref _busy, 1) == 1) return RuneTooltipMarkResult.Busy;
        try
        {
            return ToggleCore(out runeName);
        }
        finally
        {
            _ = Interlocked.Exchange(ref _busy, 0);
        }
    }

    private RuneTooltipMarkResult ToggleCore(out string? runeName)
    {
        runeName = null;
        var context = windowResolution.CurrentWindowCaptureContext;
        if (context is null || !windowResolution.IsPoe2WindowForeground)
            return RuneTooltipMarkResult.NoGameWindow;

        var client = new Rectangle(context.ClientX, context.ClientY, context.ClientWidth, context.ClientHeight);
        var cursor = (CursorProvider ?? (() => Cursor.Position))();
        var region = RuneTooltipReader.TooltipRegionFor(cursor, client);
        if (region.Width <= 0 || region.Height <= 0)
        {
            logger.LogDebug("Tooltip-mark: cursor at {X},{Y} is outside the game client {Client}", cursor.X, cursor.Y, client);
            return RuneTooltipMarkResult.NoGameWindow;
        }

        var sw = Stopwatch.StartNew();
        string? text;
        try
        {
            text = (TextReader ?? ReadText)(region);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Tooltip-mark: reading the screen failed: {Context}", ErrorContext.FromException(ex));
            return RuneTooltipMarkResult.Unavailable;
        }
        if (text is null) return RuneTooltipMarkResult.Unavailable;

        var rune = RuneTooltipReader.Match(catalog.Runes, text);
        if (rune is null)
        {
            logger.LogInformation("Tooltip-mark: no rune name in the {W}x{H} region above the cursor ({Ms} ms)", region.Width, region.Height, sw.ElapsedMilliseconds);
            logger.LogDebug("Tooltip-mark: text was {Text}", text.ReplaceLineEndings(" | "));
            return RuneTooltipMarkResult.NoTooltip;
        }

        runeName = rune.DisplayName;
        var nowCarried = !catalog.IsCarried(rune.Id);
        catalog.SetCarried(rune.Id, nowCarried);
        logger.LogInformation("Tooltip-mark: {Name} {State} ({Ms} ms)", rune.DisplayName, nowCarried ? "added to the magazine" : "removed from the magazine", sw.ElapsedMilliseconds);
        return nowCarried ? RuneTooltipMarkResult.Marked : RuneTooltipMarkResult.Unmarked;
    }

    /// <summary>Captures <paramref name="region"/> from the game window and runs Windows OCR on it, unscaled — tooltip text is large.</summary>
    private string? ReadText(Rectangle region)
    {
        var options = ocrOptions.CurrentValue;
        var engine = GetEngine(options);
        if (engine is null) return null;

        var capture = _capture.Capture(new OcrCaptureRegion(region.X, region.Y, region.Width, region.Height), windowResolution.CurrentWindowCaptureContext, options);
        if (capture is null)
        {
            logger.LogWarning("Tooltip-mark: no capture frame for the region above the cursor");
            return null;
        }

        using var bitmap = capture.Bitmap;
        if (options.SaveDebugImages) SaveDebugImage(bitmap, options);
        return engine.Recognize(bitmap, out _, upscaleFactor: 1);
    }

    private WindowsOcrEngine? GetEngine(OcrOptions options)
    {
        if (!WindowsOcrSupported)
        {
            WarnOnce("Tooltip-mark needs Windows OCR (Windows 10 build 1809 or later); the tooltip hotkey is unavailable");
            return null;
        }

        var language = string.IsNullOrWhiteSpace(options.Language) ? "eng" : options.Language;
        lock (_engineSync)
        {
            if (_engine is not null && string.Equals(_engineLanguage, language, StringComparison.OrdinalIgnoreCase))
                return _engine;
            if (_engineFailed && _engine is null) return null;

            try
            {
                _engine = new WindowsOcrEngine(language, logger);
                _engineLanguage = language;
                _engineFailed = false;
                return _engine;
            }
            catch (Exception ex)
            {
                _engineFailed = true;
                logger.LogWarning(ex, "Tooltip-mark: Windows OCR engine could not be created; the tooltip hotkey is unavailable: {Context}", ErrorContext.FromException(ex));
                return null;
            }
        }
    }

    private bool _warned;

    private void WarnOnce(string message)
    {
        if (_warned) return;
        _warned = true;
        logger.LogWarning("{Message}", message);
    }

    private void SaveDebugImage(Bitmap bitmap, OcrOptions options)
    {
        try
        {
            var directory = string.IsNullOrWhiteSpace(options.DebugImageDirectory)
                ? Path.Combine(AppContext.BaseDirectory, "ocr", "windows", "images")
                : Path.IsPathRooted(options.DebugImageDirectory)
                    ? options.DebugImageDirectory
                    : Path.Combine(AppContext.BaseDirectory, options.DebugImageDirectory);
            _ = Directory.CreateDirectory(directory);
            OcrImagePreprocessor.SavePng(bitmap, Path.Combine(directory, "rune-tooltip.png"));
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Tooltip-mark: could not save the debug image");
        }
    }
}
