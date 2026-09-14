using RuneshapePriceChecker.OCR;
using RuneshapePriceChecker.Runes;

namespace RuneshapePriceChecker.Rumours;

/// <summary>One line of the Island Rumours list: what was read, where, and which rumour it is.</summary>
/// <param name="Text">The line as OCR returned it, kept for logging the strings no row matched.</param>
/// <param name="Bounds">The line's box in the captured bitmap, where a marker draws.</param>
/// <param name="Rumour">The matched row, or null when nothing was close enough to call.</param>
/// <param name="Distance">Edits between the line and the row it matched; -1 when unmatched.</param>
public sealed record RumourReading(string Text, Rectangle Bounds, RumourDefinition? Rumour, int Distance);

/// <summary>An Uncharted Waters panel that was found on screen.</summary>
/// <param name="Bounds">The panel's text block, from the subtitle down to the footer.</param>
/// <param name="Rumours">Its rumour lines, top to bottom.</param>
public sealed record RumourPanel(Rectangle Bounds, IReadOnlyList<RumourReading> Rumours)
{
    /// <summary>Index of the best-rated rumour on the panel, or -1 when none matched.</summary>
    public int BestIndex
    {
        get
        {
            var best = -1;
            var bestRank = int.MaxValue;
            for (var i = 0; i < Rumours.Count; i++)
            {
                if (Rumours[i].Rumour is not { } rumour) continue;
                var rank = RumourTable.RatingRank(rumour.Rating);
                if (rank >= bestRank) continue;
                best = i;
                bestRank = rank;
            }
            return best;
        }
    }
}

/// <summary>
/// Finds the Uncharted Waters panel in a screen's worth of OCR'd lines and names its rumours.
///
/// The panel is drawn wherever the charted node is — centred above it, or shoved into a corner
/// when the node is near the screen edge — so nothing about its position can be assumed. What can
/// be assumed is its text, and the parts of it that matter are in the game's block font, which
/// OCRs cleanly even when the handwritten rumour lines below do not: the subtitle "Use a logbook
/// to chart the area", the "Island Rumours" header, and a "Consumes:"/"Requires:" footer. The
/// rumours are every line between the header and the footer.
///
/// The subtitle does double duty as the panel's width: it is the widest line in the panel and read
/// perfectly in every capture so far, whereas the header sometimes comes back clipped ("'ISLA N D
/// RU"), which would make a span measured from it too narrow. Lines are taken by geometry rather
/// than by the order the engine returns them, because that order is reading order over the whole
/// screen and puts the map's other text wherever it likes.
/// </summary>
public static class RumourPanelReader
{
    /// <summary>How far outside the subtitle's span a rumour line's centre may sit, as a fraction of that span.</summary>
    private const double SpanTolerance = 0.25;

    /// <summary>Most rumours a panel is believed to list; more than this means the region was misread.</summary>
    private const int MaxRumours = 5;

    /// <summary>
    /// The panel in <paramref name="lines"/>, or null when no panel is there. Pure: give it the
    /// lines from any bitmap and the boxes come back in that bitmap's coordinates.
    /// </summary>
    public static RumourPanel? Read(IReadOnlyList<OcrLine> lines, RumourTable table)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(table);

        var header = lines.FirstOrDefault(IsRumoursHeader);
        if (header.Bounds.IsEmpty) return null;

        var subtitle = lines.FirstOrDefault(l => IsSubtitle(l) && l.Bounds.Bottom <= header.Bounds.Bottom);
        var span = subtitle.Bounds.IsEmpty ? Inflate(header.Bounds, 0.5) : subtitle.Bounds;

        var footer = lines
            .Where(l => l.Bounds.Top >= header.Bounds.Bottom && IsFooter(l) && WithinSpan(l, span))
            .OrderBy(l => l.Bounds.Top)
            .FirstOrDefault();
        if (footer.Bounds.IsEmpty) return null;

        var body = lines
            .Where(l => l.Bounds.Top >= header.Bounds.Bottom
                     && l.Bounds.Bottom <= footer.Bounds.Top
                     && WithinSpan(l, span))
            .OrderBy(l => l.Bounds.Top)
            .ToList();
        if (body.Count > MaxRumours) return null;

        var readings = new List<RumourReading>(body.Count);
        foreach (var line in body)
        {
            var rumour = table.Match(line.Text, out var distance);
            readings.Add(new RumourReading(line.Text, line.Bounds, rumour, distance));
        }

        var bounds = Rectangle.Union(subtitle.Bounds.IsEmpty ? header.Bounds : subtitle.Bounds, footer.Bounds);
        return new RumourPanel(bounds, readings);
    }

    /// <summary>
    /// "Island Rumours", which the engine returns anywhere from clean to "'ISLA N D RU". Spaces
    /// are dropped before the test because the header's letter-spacing makes the engine break it
    /// into words at arbitrary points; a prefix test then survives whatever it lost off the end.
    /// </summary>
    internal static bool IsRumoursHeader(OcrLine line) =>
        Compact(line.Text).StartsWith("island", StringComparison.Ordinal);

    /// <summary>"Use a logbook to chart the area", matched on its distinctive middle.</summary>
    internal static bool IsSubtitle(OcrLine line) =>
        Compact(line.Text).Contains("logbookto", StringComparison.Ordinal)
        || Compact(line.Text).Contains("tochartthearea", StringComparison.Ordinal);

    /// <summary>"Consumes:" when you hold a logbook, "Requires:" when you do not.</summary>
    internal static bool IsFooter(OcrLine line)
    {
        var compact = Compact(line.Text);
        return StrComp.GetEditDistance(compact, "consumes", 2) >= 0
            || StrComp.GetEditDistance(compact, "requires", 2) >= 0
            || compact.Contains("expeditionlogbook", StringComparison.Ordinal);
    }

    private static bool WithinSpan(OcrLine line, Rectangle span)
    {
        var tolerance = (int)(span.Width * SpanTolerance);
        var centre = line.Bounds.Left + (line.Bounds.Width / 2);
        return centre >= span.Left - tolerance && centre <= span.Right + tolerance;
    }

    private static Rectangle Inflate(Rectangle box, double fraction)
    {
        var grow = (int)(box.Width * fraction);
        return Rectangle.FromLTRB(box.Left - grow, box.Top, box.Right + grow, box.Bottom);
    }

    /// <summary>Letters only, lower case, no spaces at all.</summary>
    private static string Compact(string text) =>
        RuneTooltipReader.Normalize(text).Replace(" ", "", StringComparison.Ordinal);
}
