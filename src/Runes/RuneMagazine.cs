using Microsoft.Extensions.Logging;
using RuneshapePriceChecker.Contracts;

namespace RuneshapePriceChecker.Runes;

/// <summary>What a mark-carried keypress did, for logging and for the caller to report.</summary>
public enum RuneMarkResult
{
    /// <summary>The rune under the cursor is now in the magazine.</summary>
    Marked,

    /// <summary>It was already in the magazine and has been taken back out.</summary>
    Unmarked,

    /// <summary>The cursor was not over any marked rune.</summary>
    NoRuneUnderCursor,

    /// <summary>No panel has been read yet, or the capture region is unknown.</summary>
    NothingOnScreen
}

public static class RuneMarkResultExtensions
{
    public static string Describe(this RuneMarkResult result) => result switch
    {
        RuneMarkResult.Marked => "marked as taken",
        RuneMarkResult.Unmarked => "removed from the magazine",
        RuneMarkResult.NoRuneUnderCursor => "no rune under the cursor",
        _ => "no panel on screen"
    };
}

/// <summary>
/// Hit-testing for the "mark the rune under the cursor as taken" hotkey, kept free of Win32 and
/// of the catalog so it can be tested directly.
/// </summary>
public static class RuneHitTester
{
    /// <summary>
    /// The rune whose cell contains <paramref name="pointInCapture"/>, or null. Cells never
    /// overlap on a real panel, but a box that has drifted could: the smallest match wins, since
    /// a wrong-but-tight box is a likelier read than a wrong-and-huge one.
    /// </summary>
    public static RuneKeyScore? FindAt(RuneScoreSheet? sheet, Point pointInCapture) =>
        FindAmong(sheet?.AllKeys ?? [], pointInCapture);

    /// <inheritdoc cref="FindAt(RuneScoreSheet?, Point)"/>
    public static RuneKeyScore? FindAmong(IEnumerable<RuneKeyScore> keys, Point pointInCapture)
    {
        ArgumentNullException.ThrowIfNull(keys);

        RuneKeyScore? best = null;
        var bestArea = int.MaxValue;
        foreach (var key in keys)
        {
            var cell = key.Key.CellBounds;
            if (cell.Width <= 0 || cell.Height <= 0) continue;
            if (!cell.Contains(pointInCapture)) continue;

            var area = cell.Width * cell.Height;
            if (area >= bestArea) continue;
            best = key;
            bestArea = area;
        }
        return best;
    }

    /// <summary>Screen point to capture-region coordinates, which is what <see cref="RuneKey.CellBounds"/> is in.</summary>
    public static Point ToCaptureSpace(Point screenPoint, int regionX, int regionY) =>
        new(screenPoint.X - regionX, screenPoint.Y - regionY);
}

/// <summary>
/// The player's rune magazine: the set of succession runes taken during the current run. Holds
/// the last scored panel so the mark-carried hotkey can resolve the cursor to a rune.
///
/// This is the half of the tracker that was missing. The carried set is what makes the grey
/// "already taken" marker mean anything, and until now the only way to fill it was to alt-tab to
/// the dashboard and tick one of 34 checkboxes — unusable mid-run.
/// </summary>
public sealed class RuneMagazine(RuneCatalog catalog, ILogger<RuneMagazine> logger)
{
    /// <summary>
    /// How long a rune stays markable after it was last read. This exists because of the hover
    /// wash: the game tints a whole row gold under the cursor, which collapses the gold-ring
    /// contrast the gilded test needs (see <c>RuneIconFingerprinter.MinGoldContrast</c>), so the
    /// rune you are pointing at is exactly the one that stops being detected. Without a memory,
    /// hovering to press the hotkey is what makes the target disappear.
    /// </summary>
    internal TimeSpan Memory { get; set; } = TimeSpan.FromSeconds(8);

    private readonly object _sync = new();
    private readonly Dictionary<Rectangle, (RuneKeyScore Key, DateTimeOffset SeenAt)> _recent = [];
    private Rectangle _captureRegion;

    /// <summary>Reads the cursor position. Replaced in tests.</summary>
    internal Func<Point>? CursorProvider { get; set; }

    /// <summary>Supplies "now". Replaced in tests.</summary>
    internal Func<DateTimeOffset>? Clock { get; set; }

    /// <summary>
    /// Records what is currently on screen, from the render pass. Keys are remembered by cell,
    /// so a cell that reappears with a different rune replaces the old entry rather than
    /// accumulating beside it.
    /// </summary>
    public void SetSheet(RuneScoreSheet? sheet, Rectangle captureRegion)
    {
        var now = (Clock ?? (() => DateTimeOffset.UtcNow))();
        lock (_sync)
        {
            // A moved or resized capture region means the old cell coordinates mean nothing.
            if (_captureRegion != captureRegion)
            {
                _recent.Clear();
                _captureRegion = captureRegion;
            }

            if (sheet is not null)
            {
                foreach (var key in sheet.AllKeys)
                {
                    var cell = key.Key.CellBounds;
                    if (cell.Width > 0 && cell.Height > 0)
                        _recent[cell] = (key, now);
                }
            }

            foreach (var cell in _recent.Where(e => now - e.Value.SeenAt > Memory).Select(e => e.Key).ToList())
                _ = _recent.Remove(cell);
        }
    }

    /// <summary>
    /// Toggles the rune under the cursor. Toggling rather than only adding is deliberate: a
    /// mis-press on the wrong cell has to be undoable without opening the dashboard.
    /// </summary>
    public RuneMarkResult ToggleAtCursor() => ToggleAtCursor(out _);

    /// <inheritdoc cref="ToggleAtCursor()"/>
    /// <param name="runeName">The display name of the rune toggled, for feedback; null when nothing was.</param>
    public RuneMarkResult ToggleAtCursor(out string? runeName)
    {
        runeName = null;
        var now = (Clock ?? (() => DateTimeOffset.UtcNow))();
        List<RuneKeyScore> keys;
        Rectangle region;
        lock (_sync)
        {
            region = _captureRegion;
            keys = _recent.Values.Where(e => now - e.SeenAt <= Memory).Select(e => e.Key).ToList();
        }

        if (keys.Count == 0 || region.Width <= 0 || region.Height <= 0)
            return RuneMarkResult.NothingOnScreen;

        var cursor = (CursorProvider ?? (() => Cursor.Position))();
        var inCapture = RuneHitTester.ToCaptureSpace(cursor, region.X, region.Y);
        var hit = RuneHitTester.FindAmong(keys, inCapture);
        if (hit is null)
        {
            logger.LogDebug("Mark-carried: cursor at {X},{Y} (capture {CX},{CY}) is not over a rune", cursor.X, cursor.Y, inCapture.X, inCapture.Y);
            return RuneMarkResult.NoRuneUnderCursor;
        }

        // A rune that has not been named yet is carried under its binding id; RuneCatalog.Bind
        // migrates the flag if the user names it later, so marking now is never wasted.
        var id = hit.RuneId ?? hit.BindingId;
        var nowCarried = !catalog.IsCarried(id);
        catalog.SetCarried(id, nowCarried);
        runeName = hit.RuneId is null ? "Unbound rune" : hit.DisplayName;
        logger.LogInformation("Mark-carried: {Name} {State}", hit.DisplayName, nowCarried ? "added to the magazine" : "removed from the magazine");
        return nowCarried ? RuneMarkResult.Marked : RuneMarkResult.Unmarked;
    }
}
