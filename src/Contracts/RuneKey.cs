namespace RuneshapePriceChecker.Contracts;

/// <summary>
/// Stable per-rune identity: glyph shape (dHash) plus tier colour (hue bucket).
/// Carries its normalised 32x32 sprite so a caller (RUNE-2) can render a newly
/// discovered rune without re-cropping the original capture.
/// </summary>
public sealed record RuneKey(ulong ShapeHash, int HueBucket, byte[] Sprite32Rgb);

/// <summary>
/// The gilded (succession) rune keys found in one Combinations row, anchored to the
/// row's text Y position so callers can join on row identity instead of list position.
/// </summary>
public sealed record RuneRowKeys(int RowY, IReadOnlyList<RuneKey> Keys);
