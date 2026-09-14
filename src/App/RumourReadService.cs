using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RuneshapePriceChecker.OCR;
using RuneshapePriceChecker.Rumours;

namespace RuneshapePriceChecker.App;

/// <summary>What a read of the Uncharted Waters panel did.</summary>
public enum RumourReadOutcome
{
    /// <summary>A panel was found and its rumours named.</summary>
    Read,

    /// <summary>The screen held no Uncharted Waters panel.</summary>
    NoPanel,

    /// <summary>The game window is not in front, or its position is not known yet.</summary>
    NoGameWindow,

    /// <summary>Capture or OCR is not available; the log says why.</summary>
    Unavailable,

    /// <summary>A previous read is still running; this press was dropped.</summary>
    Busy
}

/// <summary>The outcome of one read, and what it found.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="Panel">The rumour list, in client coordinates; null when none was on screen.</param>
/// <param name="Map">A charted node's tooltip, in client coordinates; null when none was on screen.</param>
/// <param name="Client">The game client's screen rectangle, so the overlay can cover the same area.</param>
public sealed record RumourReadResult(RumourReadOutcome Outcome, RumourPanel? Panel, RumourMapTooltip? Map, Rectangle Client)
{
    /// <summary>Whether there is anything to mark.</summary>
    public bool HasSomething => Panel is not null || Map is not null;

    /// <summary>
    /// What makes one sighting the same as the last: the same things named in the same places.
    /// Decides both when to redraw and when a log line would only be repeating itself.
    /// </summary>
    internal string Signature()
    {
        if (Panel is { } panel)
            return string.Join("|", panel.Rumours.Select(r => $"{r.Rumour?.Id ?? r.Text}@{r.Bounds.X},{r.Bounds.Y}"));
        if (Map is { } map)
            return $"{map.Rumour?.Id ?? map.Title}@{map.Bounds.X},{map.Bounds.Y}";
        return "";
    }
}

/// <summary>
/// Reads the Uncharted Waters panel wherever it is on screen and names its rumours.
///
/// Unlike the rune paths there is nothing to aim at: the panel is drawn next to whichever chart
/// node was clicked, which can be anywhere, so the whole client is captured and
/// <see cref="RumourPanelReader"/> finds the panel inside it by its text. A full 2560x1440 frame
/// costs about 150 ms through Windows OCR, which is cheap enough for a keypress and far cheaper
/// than asking the player to put the cursor somewhere exact.
///
/// It owns its own Windows OCR engine rather than borrowing the panel reader's: the two run on
/// different threads at unrelated moments and the engine is not meant to be shared.
/// </summary>
public sealed class RumourReadService(
    IOptionsMonitor<OcrOptions> ocrOptions,
    IPoe2WindowResolutionProvider windowResolution,
    RumourTable table,
    ILoggerFactory loggerFactory,
    ILogger<RumourReadService> logger)
{
    private static readonly bool WindowsOcrSupported = Environment.OSVersion.Version.Build >= 17763;

    private readonly OcrCaptureStrategy _capture = new(loggerFactory.CreateLogger<OcrCaptureStrategy>());
    private readonly object _engineSync = new();
    private WindowsOcrEngine? _engine;
    private string? _engineLanguage;
    private bool _engineFailed;
    private bool _warned;
    private int _busy;
    private string _lastLogged = "";
    private string? _lastPartialWarned;

    /// <summary>Captures and recognises the lines in a screen rectangle. Replaced in tests.</summary>
    internal Func<Rectangle, IReadOnlyList<OcrLine>?>? LineReader { get; set; }

    /// <summary>Reads the cursor position. Replaced in tests.</summary>
    internal Func<Point>? CursorProvider { get; set; }

    /// <summary>
    /// Reads the panel on screen. Synchronous and a few hundred milliseconds at most; callers on a
    /// message thread should hand it to a worker. Only one read runs at a time — a second press
    /// while one is in flight is dropped rather than queued.
    /// </summary>
    /// <param name="aroundCursor">
    /// Read only the part of the screen near the cursor rather than the whole client. Both panels
    /// are drawn against the node under the pointer, so this loses nothing and costs about a fifth
    /// as much — 50 ms against 230 ms at 2560x1440 — which is what makes scanning on its own
    /// affordable.
    /// </param>
    public RumourReadResult Read(bool aroundCursor = false)
    {
        if (Interlocked.Exchange(ref _busy, 1) == 1)
            return new RumourReadResult(RumourReadOutcome.Busy, null, null, Rectangle.Empty);
        try
        {
            return ReadCore(aroundCursor);
        }
        finally
        {
            _ = Interlocked.Exchange(ref _busy, 0);
        }
    }

    /// <summary>
    /// The part of the screen worth reading when the cursor is at <paramref name="cursor"/>.
    ///
    /// Generous on purpose, and lopsided: the Uncharted Waters panel is drawn well above the node
    /// — half the screen's height above it, for a node low on the map — while a charted node's
    /// tooltip sits just below. So half the client's height above the cursor and a quarter below,
    /// and three fifths of its width, which covers both with room to spare and still costs well
    /// under half what reading the whole client does.
    /// </summary>
    public static Rectangle CursorRegion(Point cursor, Rectangle client)
    {
        if (client.Width <= 0 || client.Height <= 0) return Rectangle.Empty;

        var width = Math.Max(1, (int)(client.Width * 0.60));
        var above = (int)(client.Height * 0.50);
        var below = (int)(client.Height * 0.25);

        var box = new Rectangle(cursor.X - (width / 2), cursor.Y - above, width, Math.Max(1, above + below));
        box.Intersect(client);
        return box;
    }

    private RumourReadResult ReadCore(bool aroundCursor)
    {
        var context = windowResolution.CurrentWindowCaptureContext;
        if (context is null || !windowResolution.IsPoe2WindowForeground)
            return new RumourReadResult(RumourReadOutcome.NoGameWindow, null, null, Rectangle.Empty);

        var client = new Rectangle(context.ClientX, context.ClientY, context.ClientWidth, context.ClientHeight);
        if (client.Width <= 0 || client.Height <= 0)
            return new RumourReadResult(RumourReadOutcome.NoGameWindow, null, null, Rectangle.Empty);

        var region = client;
        if (aroundCursor)
        {
            var cursor = (CursorProvider ?? (() => Cursor.Position))();
            region = CursorRegion(cursor, client);
            if (region.Width <= 0 || region.Height <= 0)
                return new RumourReadResult(RumourReadOutcome.NoGameWindow, null, null, client);
        }

        var sw = Stopwatch.StartNew();
        IReadOnlyList<OcrLine>? lines;
        try
        {
            lines = (LineReader ?? ReadLines)(region);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Rumour read: reading the screen failed: {Context}", ErrorContext.FromException(ex));
            return new RumourReadResult(RumourReadOutcome.Unavailable, null, null, client);
        }
        if (lines is null) return new RumourReadResult(RumourReadOutcome.Unavailable, null, null, client);

        // Either surface will do: the rumour list before the logbook is used, the charted node's
        // own tooltip after it is, when the wording is gone but the map name still identifies it.
        var panel = RumourPanelReader.Read(lines, table);
        var map = panel is null ? RumourMapTooltipReader.Read(lines, table) : null;

        if (panel is null && map is null)
        {
            // Auto-scan reads whenever the cursor rests, so "there was nothing there" is the
            // normal state of the world rather than news. At Information it drowned out the log.
            logger.LogDebug("Rumour read: nothing to mark on screen ({Ms} ms, {Lines} lines)", sw.ElapsedMilliseconds, lines.Count);
            _lastLogged = "";

            // A panel that was half in the region is the one failure that looks identical to no
            // panel at all from the outside, and the one most likely to be a bug in where we read
            // rather than in what was on screen. Say so.
            var partial = lines.FirstOrDefault(l => RumourPanelReader.IsRumoursHeader(l) || RumourPanelReader.IsSubtitle(l));
            if (!partial.Bounds.IsEmpty && partial.Text != _lastPartialWarned)
            {
                _lastPartialWarned = partial.Text;
                logger.LogWarning("Rumour read: saw \"{Text}\" at {Bounds} but could not assemble a panel — the read region may be cutting it off (region {Region})",
                    partial.Text, partial.Bounds, region);
            }

            return new RumourReadResult(RumourReadOutcome.NoPanel, null, null, client);
        }

        _lastPartialWarned = null;

        var offset = new Point(region.X - client.X, region.Y - client.Y);
        if (map is not null)
        {
            map = map with { Bounds = Shift(map.Bounds, offset) };
            var mapResult = new RumourReadResult(RumourReadOutcome.Read, null, map, client);

            if (IsRepeat(mapResult))
            {
                logger.LogDebug("Rumour read: same map still on screen ({Ms} ms)", sw.ElapsedMilliseconds);
            }
            else if (map.Rumour is { } charted)
            {
                logger.LogInformation("Rumour read: charted map [{Rating}] {Map} — {Mods} (read \"{Title}\", d={Distance}, {Ms} ms)",
                    charted.Rating, charted.Map, charted.Mods, map.Title, map.Distance, sw.ElapsedMilliseconds);
            }
            else
            {
                logger.LogInformation("Rumour read: map tooltip for \"{Title}\" is not in the table ({Ms} ms)", map.Title, sw.ElapsedMilliseconds);
            }

            return mapResult;
        }

        panel = panel! with
        {
            Bounds = Shift(panel.Bounds, offset),
            Rumours = [.. panel.Rumours.Select(r => r with { Bounds = Shift(r.Bounds, offset) })]
        };

        var result = new RumourReadResult(RumourReadOutcome.Read, panel, null, client);
        if (IsRepeat(result))
        {
            // The watch re-reads a panel it is already marking, to notice when it closes. Saying
            // so every half second is not information.
            logger.LogDebug("Rumour read: same {Count} rumours still on screen ({Ms} ms)", panel.Rumours.Count, sw.ElapsedMilliseconds);
            return result;
        }

        logger.LogInformation("Rumour read: {Count} rumours ({Ms} ms)", panel.Rumours.Count, sw.ElapsedMilliseconds);
        foreach (var reading in panel.Rumours)
        {
            if (reading.Rumour is { } rumour)
            {
                logger.LogInformation("  [{Rating}] {Name} — {Map}, {Mods} (read \"{Text}\", d={Distance})",
                    rumour.Rating, rumour.Name, rumour.Map, rumour.Mods, reading.Text, reading.Distance);
            }
            else
            {
                // The wording of a rumour nobody has captured yet looks exactly like this. Logged
                // at warning so it is easy to find and add to ocr/rumour-tiers.json as an alias.
                logger.LogWarning("  [?] unrecognised rumour — OCR read \"{Text}\"", reading.Text);
            }
        }

        return result;
    }

    /// <summary>
    /// Whether this read found what the last one found. The watch re-reads several times a second
    /// — to catch a panel that appears late, then to notice it closing — so without this every one
    /// of those reads would write the same lines again.
    /// </summary>
    private bool IsRepeat(RumourReadResult result)
    {
        var signature = result.Signature();
        var repeat = signature.Length > 0 && signature == _lastLogged;
        _lastLogged = signature;
        return repeat;
    }

    /// <summary>Moves a box read from the captured region into the client's own coordinates.</summary>
    private static Rectangle Shift(Rectangle box, Point offset) =>
        offset.IsEmpty ? box : new Rectangle(box.X + offset.X, box.Y + offset.Y, box.Width, box.Height);

    /// <summary>Captures the region and runs Windows OCR on it, unscaled — the panel's text is large.</summary>
    private IReadOnlyList<OcrLine>? ReadLines(Rectangle region)
    {
        var options = ocrOptions.CurrentValue;
        var engine = GetEngine(options);
        if (engine is null) return null;

        var capture = _capture.Capture(
            new OcrCaptureRegion(region.X, region.Y, region.Width, region.Height),
            windowResolution.CurrentWindowCaptureContext,
            options);
        if (capture is null)
        {
            logger.LogWarning("Rumour read: no capture frame for the client area");
            return null;
        }

        using var bitmap = capture.Bitmap;
        if (options.SaveDebugImages) SaveDebugImage(bitmap, options);
        return engine.RecognizeLines(bitmap);
    }

    private WindowsOcrEngine? GetEngine(OcrOptions options)
    {
        if (!WindowsOcrSupported)
        {
            WarnOnce("The rumour checker needs Windows OCR (Windows 10 build 1809 or later); the rumour hotkey is unavailable");
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
                logger.LogWarning(ex, "Rumour read: Windows OCR engine could not be created; the rumour hotkey is unavailable: {Context}", ErrorContext.FromException(ex));
                return null;
            }
        }
    }

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
            OcrImagePreprocessor.SavePng(bitmap, Path.Combine(directory, "rumour-panel.png"));
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Rumour read: could not save the debug image");
        }
    }
}
