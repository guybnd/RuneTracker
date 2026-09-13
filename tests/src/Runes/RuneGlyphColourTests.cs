using RuneshapePriceChecker.OCR;
using Xunit;

namespace RuneshapePriceChecker.Tests.Runes;

/// <summary>
/// The glyph's tier colour. The user's point was that Opulent's gold is unmistakable and should
/// be the easiest rune to recognise — and it was being thrown away: every rune's glyph is drawn
/// in dark brown ink, which is saturated and sits at hue 30-45, the same 30-degree bucket as
/// gold. Counting it made 32 of the 45 sprites in the user's catalog read as bucket 1, Opulent
/// among them, indistinguishable from tidal, rebirth, moon, cold, ward, rage and fire.
/// </summary>
public class RuneGlyphColourTests
{
    private const int Dim = 32;

    /// <summary>A 32x32 glyph on parchment, with <paramref name="count"/> pixels of one colour.</summary>
    private static byte[] Glyph((byte R, byte G, byte B) colour, int count, (byte R, byte G, byte B)? inkColour = null, int inkCount = 0)
    {
        var sprite = new byte[Dim * Dim * 3];
        // Parchment: pale, barely saturated.
        for (var i = 0; i < sprite.Length; i += 3)
        {
            sprite[i] = 232; sprite[i + 1] = 220; sprite[i + 2] = 196;
        }

        var written = 0;
        var ink = inkColour ?? (70, 48, 30); // the dark brown every glyph is drawn in
        for (var i = 0; i < sprite.Length && written < inkCount; i += 3, written++)
        {
            sprite[i] = ink.R; sprite[i + 1] = ink.G; sprite[i + 2] = ink.B;
        }

        written = 0;
        for (var i = inkCount * 3; i < sprite.Length && written < count; i += 3, written++)
        {
            sprite[i] = colour.R; sprite[i + 1] = colour.G; sprite[i + 2] = colour.B;
        }

        return sprite;
    }

    [Fact]
    public void PlainBrownInkHasNoTierColour()
    {
        // The common case: most runes are brown on parchment and genuinely have no tier colour.
        var brownOnly = Glyph((70, 48, 30), count: 300);
        Assert.Equal(RuneIconFingerprinter.NoColourBucket, RuneIconFingerprinter.DominantGlyphHueBucket(brownOnly));
    }

    [Fact]
    public void GoldSurvivesTheBrownInkItUsedToLoseTo()
    {
        // This is the regression. Gold pixels outnumbered three to one by brown ink still read as
        // gold, because brown no longer votes at all.
        var gold = Glyph((235, 190, 60), count: 100, inkColour: (70, 48, 30), inkCount: 300);
        Assert.Equal(RuneIconFingerprinter.GoldHueBucket, RuneIconFingerprinter.DominantGlyphHueBucket(gold));
    }

    [Theory]
    [InlineData(235, 190, 60, RuneIconFingerprinter.GoldHueBucket)] // gold / yellow
    [InlineData(150, 90, 230, 8)]                                   // purple
    [InlineData(70, 150, 240, 7)]                                   // blue
    public void BrightSaturatedGlyphsLandInTheirOwnBucket(byte r, byte g, byte b, int expected)
    {
        Assert.Equal(expected, RuneIconFingerprinter.DominantGlyphHueBucket(Glyph((r, g, b), count: 200)));
    }

    [Fact]
    public void AHandfulOfColouredPixelsIsNoise()
    {
        // Anti-aliasing on a brown glyph can throw a few bright pixels; that is not a tier.
        var speckled = Glyph((235, 190, 60), count: 4, inkColour: (70, 48, 30), inkCount: 300);
        Assert.Equal(RuneIconFingerprinter.NoColourBucket, RuneIconFingerprinter.DominantGlyphHueBucket(speckled));
    }

    [Fact]
    public void BareParchmentHasNoColour()
    {
        Assert.Equal(RuneIconFingerprinter.NoColourBucket, RuneIconFingerprinter.DominantGlyphHueBucket(Glyph((0, 0, 0), count: 0)));
    }

    [Fact]
    public void TheGoldBucketIsTheOneGoldActuallyFallsIn()
    {
        // Guards the constant against the bucket arithmetic changing underneath it: gold and
        // yellow are hue 40-60, which is bucket 1 at 30 degrees per bucket.
        var (h, _, _) = RuneIconFingerprinter.ToHsv(235, 190, 60);
        Assert.InRange(h, 30, 60);
        Assert.Equal(RuneIconFingerprinter.GoldHueBucket, (int)(h / 30.0) % 12);
    }
}
