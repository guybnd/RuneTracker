using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using RuneshapePriceChecker.OCR;
using Xunit;

namespace RuneshapePriceChecker.Tests.OCR;

/// <summary>
/// Unit tests for RuneIconFingerprinter's pure math (dHash, hue bucketing, HSV conversion,
/// fail-soft predicates) using synthetic bitmaps with known properties — per the RUNE-1 plan,
/// these do not require real game captures (that's RuneIconFingerprinterFixtureTests' job).
/// </summary>
public class RuneIconFingerprinterTests
{
    [Theory]
    [InlineData(255, 0, 0, 0, 1.0, 1.0)]     // pure red
    [InlineData(0, 255, 0, 120, 1.0, 1.0)]   // pure green
    [InlineData(0, 0, 255, 240, 1.0, 1.0)]   // pure blue
    [InlineData(255, 255, 255, 0, 0.0, 1.0)] // white: hue undefined, reported as 0
    [InlineData(0, 0, 0, 0, 0.0, 0.0)]       // black
    public void ToHsv_KnownColors_MatchExpected(byte r, byte g, byte b, double expectedHue, double expectedSat, double expectedVal)
    {
        var (h, s, v) = RuneIconFingerprinter.ToHsv(r, g, b);
        Assert.Equal(expectedHue, h, 0.01);
        Assert.Equal(expectedSat, s, 0.01);
        Assert.Equal(expectedVal, v, 0.01);
    }

    [Fact]
    public void IsIconInk_PaleParchmentColor_ReturnsFalse()
    {
        // Desaturated, mid-bright parchment tone — not ink.
        Assert.False(RuneIconFingerprinter.IsIconInk(210, 195, 165));
    }

    [Fact]
    public void IsIconInk_SaturatedColor_ReturnsTrue()
    {
        // Saturated purple glyph stroke.
        Assert.True(RuneIconFingerprinter.IsIconInk(140, 40, 200));
    }

    [Fact]
    public void IsIconInk_NearBlack_ReturnsTrue()
    {
        Assert.True(RuneIconFingerprinter.IsIconInk(20, 18, 15));
    }

    [Fact]
    public void ComputeDHash_IdenticalSprites_ProduceIdenticalHash()
    {
        var sprite = MakeCheckerSprite();
        var a = RuneIconFingerprinter.ComputeDHash(sprite);
        var b = RuneIconFingerprinter.ComputeDHash((byte[])sprite.Clone());
        Assert.Equal(a, b);
    }

    [Fact]
    public void ComputeDHash_VisiblyDifferentSprites_HammingDistanceAtLeast10()
    {
        // Acceptance check (c3): two visibly different glyphs must hash far apart, otherwise
        // "stable" is trivially satisfiable by a degenerate always-equal hash.
        var checker = MakeCheckerSprite();
        var solid = MakeSolidSprite(200, 200, 200);

        var hashA = RuneIconFingerprinter.ComputeDHash(checker);
        var hashB = RuneIconFingerprinter.ComputeDHash(solid);

        Assert.True(HammingDistance(hashA, hashB) >= 10,
            $"expected Hamming distance >= 10 for visibly different sprites, got {HammingDistance(hashA, hashB)}");
    }

    [Fact]
    public void ComputeDHash_MinorNoisePerturbation_HammingDistanceSmall()
    {
        // Acceptance check style (c1): a near-identical sprite (few pixels perturbed) should
        // hash close to the original — dHash is a *difference* hash, robust to small noise.
        var baseSprite = MakeCheckerSprite();
        var noisy = (byte[])baseSprite.Clone();
        // Perturb a handful of pixels slightly (simulates capture-to-capture sensor noise).
        for (var i = 0; i < 30; i += 3)
            noisy[i] = (byte)Math.Clamp(noisy[i] + 5, 0, 255);

        var hashA = RuneIconFingerprinter.ComputeDHash(baseSprite);
        var hashB = RuneIconFingerprinter.ComputeDHash(noisy);

        Assert.True(HammingDistance(hashA, hashB) <= 2,
            $"expected Hamming distance <= 2 for a lightly-perturbed sprite, got {HammingDistance(hashA, hashB)}");
    }

    [Fact]
    public void DominantGlyphHueBucket_BluePatchOverParchment_ReturnsBlueBucket()
    {
        var sprite = MakeSolidSprite(214, 200, 175); // parchment background
        // Paint a saturated blue patch in the interior (glyph stroke stand-in).
        PaintInteriorPatch(sprite, 0, 80, 200);

        var bucket = RuneIconFingerprinter.DominantGlyphHueBucket(sprite);
        var (expectedHue, _, _) = RuneIconFingerprinter.ToHsv(0, 80, 200);
        var expectedBucket = (int)(expectedHue / 30.0) % 12;
        Assert.Equal(expectedBucket, bucket);
    }

    [Fact]
    public void GoldHueRingProportion_AllGoldBorder_NearOne()
    {
        using var bmp = new Bitmap(40, 40, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp))
            g.Clear(Color.FromArgb(230, 190, 40)); // gold hue/sat/val within the gate's range

        var rgb = ToRgbBytes(bmp, out var stride);
        var proportion = RuneIconFingerprinter.GoldHueRingProportion(rgb, bmp.Width, stride, new Rectangle(0, 0, 40, 40));
        Assert.True(proportion > 0.9, $"expected near-1.0 gold ring proportion for an all-gold cell, got {proportion}");
    }

    [Fact]
    public void GoldHueRingProportion_PlainBrownBorder_NearZero()
    {
        using var bmp = new Bitmap(40, 40, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp))
            g.Clear(Color.FromArgb(120, 90, 70)); // plain thin orange-brown border, outside the gold hue band

        var rgb = ToRgbBytes(bmp, out var stride);
        var proportion = RuneIconFingerprinter.GoldHueRingProportion(rgb, bmp.Width, stride, new Rectangle(0, 0, 40, 40));
        Assert.True(proportion < 0.1, $"expected near-0.0 gold ring proportion for a plain brown cell, got {proportion}");
    }

    [Theory]
    [InlineData(0.05, false, false)]  // clearly plain
    [InlineData(0.09, false, true)]   // AmbiguousGoldRingLow — lower boundary, inclusive
    [InlineData(0.12, false, true)]   // mid-band
    [InlineData(0.15, true, false)]   // GoldRingThreshold — upper boundary, inclusive on the gilded side
    [InlineData(0.30, true, false)]   // clearly gilded
    public void ClassifyGoldRing_BoundaryValues_MatchThresholds(double ratio, bool expectedGilded, bool expectedAmbiguous)
    {
        var (isGilded, ambiguous) = RuneIconFingerprinter.ClassifyGoldRing(ratio);
        Assert.Equal(expectedGilded, isGilded);
        Assert.Equal(expectedAmbiguous, ambiguous);
    }

    [Fact]
    public void SegmentIconCells_DullPartialGoldBorder_IsAmbiguousNotGilded_AndYieldsNoKey()
    {
        // Middle cell gets the same plain dark border as its neighbours, plus a thin gold band
        // covering only 2 of the ring metric's ~5px-deep band (not the full ring), landing its
        // measured GoldHueRingProportion in the [0.09, 0.15) ambiguous band rather than at the
        // near-1.0 an all-gold border produces (see GoldHueRingProportion_AllGoldBorder_NearOne).
        using var bmp = new Bitmap(400, 120, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.FromArgb(225, 210, 180));
            DrawPlainCellBorder(g, 10, 30, 44, 44);
            DrawPlainCellBorder(g, 118, 30, 44, 44);
            DrawPlainCellBorder(g, 64, 30, 44, 44);
            using var dullGold = new SolidBrush(Color.FromArgb(220, 170, 40));
            g.FillRectangle(dullGold, 64, 32, 44, 2);
        }
        var rgb = ToRgbBytes(bmp, out var stride);

        var row = RuneIconFingerprinter.LocateIconRow(rgb, bmp.Width, stride, 0, bmp.Height, 45);
        Assert.NotNull(row);
        var cells = RuneIconFingerprinter.SegmentIconCells(rgb, bmp.Width, bmp.Height, stride, row.Value.Top, row.Value.Bottom);
        Assert.Equal(3, cells.Count);

        Assert.False(cells[0].Ambiguous);
        Assert.False(cells[0].IsGilded);
        Assert.False(cells[2].Ambiguous);
        Assert.False(cells[2].IsGilded);

        Assert.InRange(cells[1].GoldHueRingProportion, 0.09, 0.15 - 1e-9);
        Assert.True(cells[1].Ambiguous, $"expected the dull-gold cell's ring ({cells[1].GoldHueRingProportion:F3}) to fall in the ambiguous band");
        Assert.False(cells[1].IsGilded, "an ambiguous ring must never also be classified gilded");

        // The row's only candidate is dropped, not surfaced as a low-confidence gilded key.
        var keys = RuneIconFingerprinter.ExtractRowKeys(bmp, 0, bmp.Height, rowTextY: 51, rowTextHeight: 24);
        Assert.Empty(keys);
    }

    [Fact]
    public void SegmentIconCells_GildedCellAtIndexZero_ExtrapolatesBackwardFromNextPlainCell()
    {
        // The gilded cell sits first in the row with no preceding plain cell — the ticket's own
        // geometry notes call out a first-cell special frame as a real layout — so
        // AssignLatticeGlyphBounds must take its "extrapolate backward from the following plain
        // cell" arm (RuneIconFingerprinter.cs:429), which every other test leaves unexercised.
        using var bmp = new Bitmap(400, 120, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.FromArgb(225, 210, 180));
            using var dark = new Pen(Color.FromArgb(60, 40, 30), 2f);
            using var goldPen = new Pen(Color.FromArgb(220, 170, 40), 5f);
            for (var i = 0; i < 3; i++)
            {
                var rect = new Rectangle(10 + (i * 54), 30, 44, 44);
                g.DrawRectangle(i == 0 ? goldPen : dark, rect);
            }
        }
        var rgb = ToRgbBytes(bmp, out var stride);

        var row = RuneIconFingerprinter.LocateIconRow(rgb, bmp.Width, stride, 0, bmp.Height, 45);
        Assert.NotNull(row);
        var cells = RuneIconFingerprinter.SegmentIconCells(rgb, bmp.Width, bmp.Height, stride, row.Value.Top, row.Value.Bottom);
        Assert.Equal(3, cells.Count);
        Assert.True(cells[0].IsGilded);
        Assert.False(cells[1].IsGilded);
        Assert.False(cells[2].IsGilded);

        var pitch = cells[2].Bounds.Right - cells[1].Bounds.Right;
        Assert.True(pitch > 0, $"expected a positive measured pitch between the two plain cells, got {pitch}");
        var expectedGlyphRight = cells[1].Bounds.Right - pitch;
        Assert.Equal(expectedGlyphRight, cells[0].GlyphBounds.Right);

        var rowHeight = row.Value.Bottom - row.Value.Top + 1;
        Assert.Equal(rowHeight, cells[0].GlyphBounds.Width);
        Assert.Equal(rowHeight, cells[0].GlyphBounds.Height);
    }

    [Fact]
    public void SegmentIconCells_SingleGildedCellRow_CentresGlyphBoundsOnOwnFrame()
    {
        // No plain cell exists anywhere in the row, so the measured pitch is 0 and
        // AssignLatticeGlyphBounds must take its "centre the slot on the cell's own frame"
        // fallback arm (RuneIconFingerprinter.cs:431) — otherwise unexercised by every other
        // test, which always has at least one plain neighbour.
        using var bmp = new Bitmap(200, 120, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.FromArgb(225, 210, 180));
            using var goldPen = new Pen(Color.FromArgb(220, 170, 40), 5f);
            g.DrawRectangle(goldPen, new Rectangle(10, 30, 44, 44));
        }
        var rgb = ToRgbBytes(bmp, out var stride);

        var row = RuneIconFingerprinter.LocateIconRow(rgb, bmp.Width, stride, 0, bmp.Height, 45);
        Assert.NotNull(row);
        var cells = RuneIconFingerprinter.SegmentIconCells(rgb, bmp.Width, bmp.Height, stride, row.Value.Top, row.Value.Bottom);
        Assert.Single(cells);
        Assert.True(cells[0].IsGilded);

        var rowHeight = row.Value.Bottom - row.Value.Top + 1;
        var expectedRight = cells[0].Bounds.X + ((cells[0].Bounds.Width + rowHeight) / 2);
        Assert.Equal(expectedRight, cells[0].GlyphBounds.Right);
        Assert.Equal(rowHeight, cells[0].GlyphBounds.Width);
        Assert.Equal(rowHeight, cells[0].GlyphBounds.Height);
    }

    [Fact]
    public void ExtractRowKeys_DegenerateWindow_ReturnsEmptyWithoutThrowing()
    {
        using var bmp = new Bitmap(100, 100, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp)) g.Clear(Color.White);

        // searchTop == searchBottomExclusive: zero-height window, must fail soft.
        var keys = RuneIconFingerprinter.ExtractRowKeys(bmp, 50, 50, 60, 26);
        Assert.Empty(keys);
    }

    [Fact]
    public void ExtractRowKeys_BlankBitmap_ReturnsEmptyWithoutThrowing()
    {
        using var bmp = new Bitmap(200, 200, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp)) g.Clear(Color.White);

        var keys = RuneIconFingerprinter.ExtractRowKeys(bmp, 0, 100, 60, 26);
        Assert.Empty(keys);
    }

    [Fact]
    public void LocateIconRow_BlankZone_ReturnsNull()
    {
        using var bmp = new Bitmap(200, 200, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp)) g.Clear(Color.White);
        var rgb = ToRgbBytes(bmp, out var stride);

        var row = RuneIconFingerprinter.LocateIconRow(rgb, bmp.Width, stride, 0, 100, 50);
        Assert.Null(row);
    }

    [Fact]
    public void LocateIconRow_HorizontalSeparatorOnly_ReturnsNull()
    {
        // A full-width dark separator line — what the original band detector anchored on in
        // every row but the first — forms no vertical border runs, so it is not an icon row.
        using var bmp = new Bitmap(400, 200, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            g.FillRectangle(Brushes.Black, 0, 40, 400, 3);
        }
        var rgb = ToRgbBytes(bmp, out var stride);

        Assert.Null(RuneIconFingerprinter.LocateIconRow(rgb, bmp.Width, stride, 0, 200, 50));
    }

    [Fact]
    public void SegmentIconCells_SyntheticBorderedCells_FindsEachCellAndFlagsTheGoldOne()
    {
        // Three 45px cells on a 54px pitch with thin dark borders, the middle one with a thick
        // gold border, over parchment — the lattice the real panel draws. Synthetic art is fine
        // here because this exercises the border-pairing logic, not the gate's pixel statistics.
        using var bmp = new Bitmap(400, 120, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.FromArgb(225, 210, 180));
            using var dark = new Pen(Color.FromArgb(60, 40, 30), 2f);
            using var goldPen = new Pen(Color.FromArgb(220, 170, 40), 5f);
            for (var i = 0; i < 3; i++)
            {
                var rect = new Rectangle(10 + (i * 54), 30, 44, 44);
                g.DrawRectangle(i == 1 ? goldPen : dark, rect);
            }
        }
        var rgb = ToRgbBytes(bmp, out var stride);

        var row = RuneIconFingerprinter.LocateIconRow(rgb, bmp.Width, stride, 0, bmp.Height, 45);
        Assert.NotNull(row);

        var cells = RuneIconFingerprinter.SegmentIconCells(rgb, bmp.Width, bmp.Height, stride, row.Value.Top, row.Value.Bottom);
        Assert.Equal(3, cells.Count);
        Assert.False(cells[0].IsGilded);
        Assert.True(cells[1].IsGilded, $"gold-bordered cell read ring={cells[1].GoldHueRingProportion:F2}");
        Assert.False(cells[2].IsGilded);
        Assert.All(cells, c => Assert.InRange(c.Bounds.Width, 40, 60));
    }

    [Fact]
    public void FindRuns_MergesRunsWithinGap_AndKeepsFartherOnesApart()
    {
        var flags = new bool[40];
        foreach (var i in new[] { 2, 3, 6, 7, 20, 21, 30 }) flags[i] = true;

        var runs = RuneIconFingerprinter.FindRuns(flags, 4);

        Assert.Equal([(2, 7), (20, 21), (30, 30)], runs);
    }

    // ---- helpers ----

    private static byte[] MakeCheckerSprite()
    {
        var sprite = new byte[32 * 32 * 3];
        for (var y = 0; y < 32; y++)
        {
            for (var x = 0; x < 32; x++)
            {
                var idx = ((y * 32) + x) * 3;
                var on = ((x / 4) + (y / 4)) % 2 == 0;
                var v = on ? (byte)220 : (byte)30;
                sprite[idx] = v;
                sprite[idx + 1] = v;
                sprite[idx + 2] = v;
            }
        }
        return sprite;
    }

    // NormalizeTo32x32's output is genuine RGB order (index 0=R,1=G,2=B) despite consuming
    // GDI+'s BGR-order raw pixel bytes internally — see its accumulation vs. write-back order.
    private static byte[] MakeSolidSprite(byte r, byte g, byte b)
    {
        var sprite = new byte[32 * 32 * 3];
        for (var i = 0; i < sprite.Length; i += 3)
        {
            sprite[i] = r;
            sprite[i + 1] = g;
            sprite[i + 2] = b;
        }
        return sprite;
    }

    private static void PaintInteriorPatch(byte[] sprite32Rgb, byte r, byte g, byte b)
    {
        for (var y = 10; y < 22; y++)
        {
            for (var x = 10; x < 22; x++)
            {
                var idx = ((y * 32) + x) * 3;
                sprite32Rgb[idx] = r;
                sprite32Rgb[idx + 1] = g;
                sprite32Rgb[idx + 2] = b;
            }
        }
    }

    /// <summary>Draws a 2px dark rectangular border with sharp (non-anti-aliased) fills, for pixel-exact synthetic geometry.</summary>
    private static void DrawPlainCellBorder(Graphics g, int x, int y, int w, int h)
    {
        using var dark = new SolidBrush(Color.FromArgb(20, 20, 20));
        g.FillRectangle(dark, x, y, w, 2);
        g.FillRectangle(dark, x, y + h - 2, w, 2);
        g.FillRectangle(dark, x, y, 2, h);
        g.FillRectangle(dark, x + w - 2, y, 2, h);
    }

    private static int HammingDistance(ulong a, ulong b) => System.Numerics.BitOperations.PopCount(a ^ b);

    private static byte[] ToRgbBytes(Bitmap bmp, out int stride)
    {
        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        try
        {
            stride = data.Stride;
            var bytes = new byte[Math.Abs(stride) * bmp.Height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            return bytes;
        }
        finally { bmp.UnlockBits(data); }
    }
}
