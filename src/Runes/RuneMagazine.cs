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
    public static RuneKeyScore? FindAt(RuneScoreSheet? sheet, Point pointInCapture)
    {
        if (sheet is null) return null;

        RuneKeyScore? best = null;
        var bestArea = int.MaxValue;
        foreach (var key in sheet.AllKeys)
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
    private readonly object _sync = new();
    private RuneScoreSheet? _sheet;
    private Rectangle _captureRegion;

    /// <summary>Reads the cursor position. Replaced in tests.</summary>
    internal Func<Point>? CursorProvider { get; set; }

    /// <summary>Records what is currently on screen, from the render pass.</summary>
    public void SetSheet(RuneScoreSheet? sheet, Rectangle captureRegion)
    {
        lock (_sync)
        {
            _sheet = sheet;
            _captureRegion = captureRegion;
        }
    }

    /// <summary>
    /// Toggles the rune under the cursor. Toggling rather than only adding is deliberate: a
    /// mis-press on the wrong cell has to be undoable without opening the dashboard.
    /// </summary>
    public RuneMarkResult ToggleAtCursor()
    {
        RuneScoreSheet? sheet;
        Rectangle region;
        lock (_sync)
        {
            sheet = _sheet;
            region = _captureRegion;
        }

        if (sheet is null || region.Width <= 0 || region.Height <= 0)
            return RuneMarkResult.NothingOnScreen;

        var cursor = (CursorProvider ?? (() => Cursor.Position))();
        var inCapture = RuneHitTester.ToCaptureSpace(cursor, region.X, region.Y);
        var hit = RuneHitTester.FindAt(sheet, inCapture);
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
        logger.LogInformation("Mark-carried: {Name} {State}", hit.DisplayName, nowCarried ? "added to the magazine" : "removed from the magazine");
        return nowCarried ? RuneMarkResult.Marked : RuneMarkResult.Unmarked;
    }
}
