namespace RuneshapePriceChecker.Contracts;

/// <summary>
/// Stable per-rune identity: glyph shape (dHash) plus tier colour (hue bucket).
/// Carries its normalised sprite so a caller (RUNE-2) can render a newly discovered rune
/// without re-cropping the original capture, and the cell's drawn rectangle (gold frame
/// included) in capture-region coordinates so an overlay can mark the rune in place on
/// the panel.
/// </summary>
/// <param name="ShapeHash">dHash of the glyph crop — the identity the library matches on.</param>
/// <param name="HueBucket">Dominant glyph hue in 30-degree buckets, or -1 for plain ink. Advisory.</param>
/// <param name="SpriteRgb">
/// <see cref="SpriteSize"/>-square RGB24 crop of the rune's plate — the frame and the parchment
/// around it are excluded. Those are identical on every rune, so including them spent most of the
/// thumbnail on what the runes have in common and left the glyph small and off-centre in the one
/// place it has to be read by eye.
/// </param>
/// <param name="CellBounds">The cell as drawn, in capture-region coordinates.</param>
/// <param name="CellIndex">
/// Zero-based position of the cell in its row's icon strip, or -1 when unknown. With
/// <paramref name="CellCount"/> and the row's name this names the rune from the Combinations
/// table (RUNE-24) without any image matching.
/// </param>
/// <param name="CellCount">How many icon cells the row has, or 0 when unknown.</param>
public sealed record RuneKey(ulong ShapeHash, int HueBucket, byte[] SpriteRgb, Rectangle CellBounds = default, int CellIndex = -1, int CellCount = 0)
{
    /// <summary>
    /// Side of the stored display sprite. Larger than the 32px the identity hash uses: dHash
    /// reduces to 8x8 whatever it is given, but a human naming an unlabelled glyph needs pixels.
    /// </summary>
    public const int SpriteSize = 64;
}

/// <summary>
/// The gilded (succession) rune keys found in one Combinations row, anchored to the
/// row's text Y position so callers can join on row identity instead of list position.
/// </summary>
/// <param name="ItemName">The row's OCR text as read (e.g. "Skill Level 20: Skyfall"), or null when it was not joined to a text row.</param>
public sealed record RuneRowKeys(int RowY, IReadOnlyList<RuneKey> Keys, string? ItemName = null);
