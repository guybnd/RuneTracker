using RuneshapePriceChecker.OCR;

namespace RuneshapePriceChecker.Runes;

/// <summary>
/// Turns the OCR text of a socketed rune's tooltip into a catalog rune, and says where on screen
/// that tooltip is likely to be. Pure functions, so the whole path from "what the OCR engine
/// said" to "which rune" is pinned by tests without a game window.
///
/// The tooltip the game shows over a rune socketed in the remnant bar reads, top to bottom:
/// a glyph and the rune's name ("Power Rune"), then "Runes gain:", then the gained modifier,
/// then a sentence about the Runic Modifier. The name is the only line that matters here. It is
/// found by anchor first — the line just above "Runes gain:" — because the glyph in front of the
/// name usually OCRs to a stray character or two, and an anchor is more robust to that than any
/// pattern on the name itself.
/// </summary>
public static class RuneTooltipReader
{
    /// <summary>Edit distance allowed between a tooltip line and a display name, after normalisation.</summary>
    public const int MaxNameDistance = 3;

    /// <summary>
    /// Picks the catalog rune the tooltip names, or null when no line is close enough to any
    /// display name. Ties go to the smaller distance; an exact match wins outright.
    /// </summary>
    public static RuneDefinition? Match(IReadOnlyList<RuneDefinition> runes, string? ocrText)
    {
        ArgumentNullException.ThrowIfNull(runes);
        if (string.IsNullOrWhiteSpace(ocrText)) return null;

        var lines = OcrImagePreprocessor.SplitAndTrim(ocrText);
        if (lines.Length == 0) return null;

        foreach (var candidate in NameCandidates(lines))
        {
            var hit = BestMatch(runes, candidate);
            if (hit is not null) return hit;
        }
        return null;
    }

    /// <summary>
    /// The lines worth trying as the name, best first: the one above the "Runes gain:" anchor,
    /// then any line ending in "Rune", then the rest top to bottom.
    /// </summary>
    internal static IEnumerable<string> NameCandidates(string[] lines)
    {
        var tried = new HashSet<int>();

        for (var i = 1; i < lines.Length; i++)
        {
            if (!IsGainAnchor(lines[i])) continue;
            if (tried.Add(i - 1)) yield return lines[i - 1];
        }

        for (var i = 0; i < lines.Length; i++)
        {
            if (!EndsWithRune(lines[i])) continue;
            if (tried.Add(i)) yield return lines[i];
        }

        for (var i = 0; i < lines.Length; i++)
        {
            if (tried.Add(i)) yield return lines[i];
        }
    }

    /// <summary>"Runes gain:" — with the punctuation, case and a misread letter or two forgiven.</summary>
    internal static bool IsGainAnchor(string line)
    {
        var normalized = Normalize(line);
        return StrComp.GetEditDistance(normalized, "runes gain", 2) >= 0
            || normalized.EndsWith(" gain", StringComparison.Ordinal);
    }

    private static bool EndsWithRune(string line)
    {
        var normalized = Normalize(line);
        var lastSpace = normalized.LastIndexOf(' ');
        var lastWord = lastSpace < 0 ? normalized : normalized[(lastSpace + 1)..];
        return StrComp.GetEditDistance(lastWord, "rune", 1) >= 0;
    }

    /// <summary>
    /// The closest display name to <paramref name="line"/>, trying the whole line and then the
    /// line with leading tokens dropped one at a time — that shrugs off the glyph's misread.
    /// </summary>
    internal static RuneDefinition? BestMatch(IReadOnlyList<RuneDefinition> runes, string line)
    {
        var words = Normalize(line).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return null;

        RuneDefinition? best = null;
        var bestDistance = int.MaxValue;
        for (var skip = 0; skip < words.Length; skip++)
        {
            var candidate = string.Join(' ', words, skip, words.Length - skip);
            foreach (var rune in runes)
            {
                if (string.IsNullOrWhiteSpace(rune.DisplayName)) continue;
                var distance = StrComp.GetEditDistance(candidate, Normalize(rune.DisplayName), MaxNameDistance);
                if (distance < 0 || distance >= bestDistance) continue;
                best = rune;
                bestDistance = distance;
                if (distance == 0) return best;
            }
        }
        return best;
    }

    /// <summary>Lower-case letters and single spaces only: OCR punctuation noise carries no information here.</summary>
    internal static string Normalize(string text)
    {
        var chars = new char[text.Length];
        var n = 0;
        var pendingSpace = false;
        foreach (var c in text)
        {
            if (char.IsLetter(c))
            {
                if (pendingSpace && n > 0) chars[n++] = ' ';
                pendingSpace = false;
                chars[n++] = char.ToLowerInvariant(c);
            }
            else
            {
                pendingSpace = true;
            }
        }
        return new string(chars, 0, n);
    }

    /// <summary>
    /// Where to look for the tooltip when the cursor is at <paramref name="cursor"/> (screen
    /// coordinates) over the game client described by <paramref name="client"/>.
    ///
    /// The game draws the tooltip above the hovered socket, horizontally centred on it, and slides
    /// it sideways to stay on screen. The box is therefore half the client width centred on the
    /// cursor, from 40% of the client height above the cursor down to just below it, clamped to
    /// the client. That is generous by design: OCR on a region this size still takes well under
    /// a quarter of a second, and missing the tooltip costs a wasted keypress.
    /// </summary>
    public static Rectangle TooltipRegionFor(Point cursor, Rectangle client)
    {
        if (client.Width <= 0 || client.Height <= 0) return Rectangle.Empty;

        var width = Math.Max(1, client.Width / 2);
        var above = (int)(client.Height * 0.40);
        var below = (int)(client.Height * 0.05);

        var box = new Rectangle(cursor.X - (width / 2), cursor.Y - above, width, above + below);
        box.Intersect(client);
        return box;
    }
}
