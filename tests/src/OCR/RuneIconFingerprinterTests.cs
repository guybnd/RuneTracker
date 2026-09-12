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

    [Fact]
    public void ExtractRowKeys_DegenerateWindow_ReturnsEmptyWithoutThrowing()
    {
        using var bmp = new Bitmap(100, 100, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp)) g.Clear(Color.White);

        // searchTop == searchBottomExclusive: zero-height window, must fail soft.
        var keys = RuneIconFingerprinter.ExtractRowKeys(bmp, 50, 50, 26);
        Assert.Empty(keys);
    }

    [Fact]
    public void ExtractRowKeys_BlankBitmap_ReturnsEmptyWithoutThrowing()
    {
        using var bmp = new Bitmap(200, 200, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp)) g.Clear(Color.White);

        var keys = RuneIconFingerprinter.ExtractRowKeys(bmp, 0, 100, 26);
        Assert.Empty(keys);
    }

    [Fact]
    public void DetectIconBand_NoIconWindow_ReturnsNull()
    {
        using var bmp = new Bitmap(200, 200, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp)) g.Clear(Color.White);
        var rgb = ToRgbBytes(bmp, out var stride);

        var band = RuneIconFingerprinter.DetectIconBand(rgb, bmp.Width, stride, 0, 100);
        Assert.Null(band);
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
