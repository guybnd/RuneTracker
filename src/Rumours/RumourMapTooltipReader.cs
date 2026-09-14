using RuneshapePriceChecker.OCR;
using RuneshapePriceChecker.Runes;

namespace RuneshapePriceChecker.Rumours;

/// <summary>A charted map's tooltip that was found on screen.</summary>
/// <param name="Title">The map name as OCR read it.</param>
/// <param name="Bounds">The title's box, where a mark draws.</param>
/// <param name="Rumour">The table row this map belongs to, or null when the name matched none.</param>
/// <param name="Distance">Edits between the title and the map name it matched; -1 when unmatched.</param>
public sealed record RumourMapTooltip(string Title, Rectangle Bounds, RumourDefinition? Rumour, int Distance);

/// <summary>
/// Names the map whose tooltip is on screen, so a node that has already been charted carries the
/// same tier as the rumour that offered it.
///
/// Once a logbook is used the rumour wording is gone — the node shows an ordinary map tooltip
/// ("Sloughed Gully", biome, mods, flavour, waystone requirement) and nothing on it says which
/// rumour it came from. The map name does, because the tier list pairs every rumour with exactly
/// one map. And unlike the rumour list this reads easily: the title is in the game's block small
/// caps, not the handwriting.
///
/// The title is found by anchor rather than by pattern — it is the line above "Biome:" — which is
/// the same trick <see cref="RuneTooltipReader"/> uses for a socketed rune's name, and for the
/// same reason: the surrounding words are fixed and the interesting one is not.
/// </summary>
public static class RumourMapTooltipReader
{
    /// <summary>How far above the anchor a title may sit, as a multiple of the anchor's height.</summary>
    private const double TitleGapFactor = 3.0;

    /// <summary>
    /// The map tooltip in <paramref name="lines"/>, or null when none is there. Pure: the boxes
    /// come back in the coordinates of whatever bitmap the lines were read from.
    /// </summary>
    public static RumourMapTooltip? Read(IReadOnlyList<OcrLine> lines, RumourTable table)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(table);

        foreach (var anchor in lines.Where(IsBiomeAnchor))
        {
            var title = TitleAbove(lines, anchor);
            if (title is not { } line) continue;

            var rumour = table.MatchMap(line.Text, out var distance);
            return new RumourMapTooltip(line.Text, line.Bounds, rumour, distance);
        }

        return null;
    }

    /// <summary>"Biome: Ocean" — the line whose own text starts the tooltip's body.</summary>
    internal static bool IsBiomeAnchor(OcrLine line)
    {
        var normalized = RuneTooltipReader.Normalize(line.Text);
        return normalized.StartsWith("biome", StringComparison.Ordinal);
    }

    /// <summary>
    /// The nearest line above the anchor that overlaps it horizontally — the tooltip's title. The
    /// horizontal test is what keeps a map name apart from whatever else the world map has drawn
    /// at the same height somewhere else on screen.
    /// </summary>
    private static OcrLine? TitleAbove(IReadOnlyList<OcrLine> lines, OcrLine anchor)
    {
        OcrLine? best = null;
        foreach (var line in lines)
        {
            if (line.Bounds.Bottom > anchor.Bounds.Top) continue;
            if (anchor.Bounds.Top - line.Bounds.Bottom > anchor.Bounds.Height * TitleGapFactor) continue;
            if (!OverlapsHorizontally(line.Bounds, anchor.Bounds)) continue;
            if (best is null || line.Bounds.Bottom > best.Value.Bounds.Bottom) best = line;
        }
        return best;
    }

    private static bool OverlapsHorizontally(Rectangle a, Rectangle b) =>
        a.Left < b.Right && b.Left < a.Right;
}
