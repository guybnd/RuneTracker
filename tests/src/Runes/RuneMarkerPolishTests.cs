using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RuneshapePriceChecker.Configuration;
using RuneshapePriceChecker.Contracts;
using RuneshapePriceChecker.OCR;
using RuneshapePriceChecker.Overlay;
using RuneshapePriceChecker.Runes;
using Xunit;

namespace RuneshapePriceChecker.Tests.Runes;

/// <summary>
/// Fixes from the first live test (RUNE-4): the top-pick badge must discriminate, and the game's
/// gold hover highlight over a whole combination row must not read as a row full of succession runes.
/// </summary>
public class RuneMarkerPolishTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "rpc-polish-" + Guid.NewGuid().ToString("N"));

    private const string ShippedJson = """
    {"schema":1,"runes":[
      {"id":"opulent","displayName":"Opulent Rune","effect":"","tier":"gold","weight":3.0},
      {"id":"bond","displayName":"Bond Rune","effect":"","tier":"purple","weight":1.0}
    ]}
    """;

    private static readonly IOptionsMonitor<RunesOptions> Options = new StaticOptions(new RunesOptions());

    private RuneCatalog NewCatalog() => new(Options, NullLogger.Instance, Path.Combine(_dir, "c.json"), ShippedJson);

    private static RuneKey Key(ulong hash, int x, int hue = 9) => new(hash, hue, new byte[RuneKey.SpriteSize * RuneKey.SpriteSize * 3], new Rectangle(x, 10, 45, 45));

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); } catch { }
    }

    [Fact]
    public void NoTopPickWhenEveryNewRuneWeighsTheSame()
    {
        using var catalog = NewCatalog();
        // Nothing bound: three sprites all score UnknownRuneWeight, so no rune beats the others.
        var snapshot = new LeagueWindowSnapshot(["a", "b"], DateTimeOffset.UtcNow, [10, 80],
            RuneRows:
            [
                new RuneRowKeys(10, [Key(0x0F0F0F0F0F0F0F0F, 10), Key(0xF0F0F0F0F0F0F0F0, 80)]),
                new RuneRowKeys(80, [Key(0x00FF00FF00FF00FF, 10)])
            ]);

        var sheet = new RuneRowScorer(catalog, Options).Score(snapshot);

        Assert.Equal(3, sheet.AllKeys.Count());
        Assert.All(sheet.AllKeys, k => Assert.True(k.IsUnbound));
        Assert.DoesNotContain(sheet.AllKeys, k => k.IsTopPick);
    }

    [Fact]
    public void TopPickAwardedOnlyWhenItBeatsARunnerUp()
    {
        using var catalog = NewCatalog();
        var best = Key(0x0F0F0F0F0F0F0F0F, 10);
        var other = Key(0xF0F0F0F0F0F0F0F0, 80);
        catalog.Observe(best); catalog.Observe(best);
        catalog.Observe(other); catalog.Observe(other);
        Assert.True(catalog.Bind(catalog.FindBinding(best.ShapeHash)!.Id, "opulent")); // weight 3
        // `other` stays unbound at weight 1, so Opulent now has a runner-up to beat.

        var snapshot = new LeagueWindowSnapshot(["a"], DateTimeOffset.UtcNow, [10],
            RuneRows: [new RuneRowKeys(10, [best, other])]);

        var sheet = new RuneRowScorer(catalog, Options).Score(snapshot);
        var starred = sheet.AllKeys.Where(k => k.IsTopPick).ToList();

        var single = Assert.Single(starred);
        Assert.Equal("opulent", single.RuneId);
        Assert.Equal(RuneMarkerKind.HighValue, single.Marker);
    }

    [Fact]
    public void CarriedRunesNeverCountTowardsTheTopPickComparison()
    {
        using var catalog = NewCatalog();
        var carried = Key(0x0F0F0F0F0F0F0F0F, 10);
        var newer = Key(0xF0F0F0F0F0F0F0F0, 80);
        catalog.Observe(carried); catalog.Observe(carried);
        Assert.True(catalog.Bind(catalog.FindBinding(carried.ShapeHash)!.Id, "opulent"));
        catalog.SetCarried("opulent", true);

        var snapshot = new LeagueWindowSnapshot(["a"], DateTimeOffset.UtcNow, [10],
            RuneRows: [new RuneRowKeys(10, [carried, newer])]);

        var sheet = new RuneRowScorer(catalog, Options).Score(snapshot);

        // Only one new rune remains, so there is nothing to compare it against: no badge.
        Assert.DoesNotContain(sheet.AllKeys, k => k.IsTopPick);
        Assert.Equal(RuneMarkerKind.Carried, sheet.AllKeys.First(k => k.RuneId == "opulent").Marker);
    }

    [Fact]
    public void GoldFrameOnParchmentIsGilded_ButAGoldWashedRowIsNot()
    {
        // Two identical cells, one on parchment and one on a row the game has tinted gold under
        // the cursor. The ring metric alone cannot tell them apart; the contrast test can.
        var plainRow = RenderCell(washBackground: false);
        var hoveredRow = RenderCell(washBackground: true);
        var cell = new Rectangle(30, 20, 45, 45);

        var plainRing = RuneIconFingerprinter.GoldHueRingProportion(plainRow.Rgb, plainRow.Width, plainRow.Stride, cell);
        var plainAmbient = RuneIconFingerprinter.AmbientGoldProportion(plainRow.Rgb, plainRow.Width, plainRow.Height, plainRow.Stride, cell);
        var hoverRing = RuneIconFingerprinter.GoldHueRingProportion(hoveredRow.Rgb, hoveredRow.Width, hoveredRow.Stride, cell);
        var hoverAmbient = RuneIconFingerprinter.AmbientGoldProportion(hoveredRow.Rgb, hoveredRow.Width, hoveredRow.Height, hoveredRow.Stride, cell);

        Assert.True(plainRing >= RuneIconFingerprinter.GoldRingThreshold, $"plain ring {plainRing:F2}");
        Assert.True(plainRing - plainAmbient >= RuneIconFingerprinter.MinGoldContrast, $"plain contrast {plainRing - plainAmbient:F2}");

        Assert.True(hoverRing >= RuneIconFingerprinter.GoldRingThreshold, $"hover ring {hoverRing:F2} should still look gold");
        Assert.True(hoverRing - hoverAmbient < RuneIconFingerprinter.MinGoldContrast, $"hover contrast {hoverRing - hoverAmbient:F2} should collapse");
    }

    private static (byte[] Rgb, int Width, int Height, int Stride) RenderCell(bool washBackground)
    {
        const int w = 105, h = 105;
        var parchment = Color.FromArgb(206, 196, 173);
        var gold = Color.FromArgb(220, 170, 40);
        using var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(washBackground ? gold : parchment);
            using var fill = new SolidBrush(parchment);
            g.FillRectangle(fill, 33, 23, 39, 39);          // cell interior stays parchment
            using var pen = new Pen(gold, 5f);
            g.DrawRectangle(pen, 30, 20, 45, 45);           // the gilded frame
        }
        var rgb = RuneIconFingerprinter.CopyPixels(bmp, out var stride);
        return (rgb, w, h, stride);
    }

    private sealed class StaticOptions(RunesOptions value) : IOptionsMonitor<RunesOptions>
    {
        public RunesOptions CurrentValue => value;
        public RunesOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<RunesOptions, string?> listener) => null;
    }

    [Theory]
    [InlineData(52, 41)] // the real shape of it: width off the row's lattice, height off the row band
    [InlineData(52, 53)]
    [InlineData(45, 45)]
    public void MarkerFramesAreSquareWhateverTheMeasuredCellWas(int w, int h)
    {
        // Icon cells are square in game, but the measured box is not: on one capture the same
        // gilded rune came out 52x45, 52x41 and 52x53 in different rows, and framing that
        // directly drew visibly squashed rectangles over square icons.
        var squared = RuneMarkerPainter.SquareUp(new Rectangle(100, 200, w, h));

        Assert.Equal(squared.Width, squared.Height);
        Assert.Equal(Math.Max(w, h), squared.Width);

        // Squaring keeps the centre, so the frame stays on the icon instead of sliding off it.
        // Within a pixel: integer halves cannot land exactly when one side is odd and the other even.
        Assert.InRange(squared.X + (squared.Width / 2), 100 + (w / 2) - 1, 100 + (w / 2) + 1);
        Assert.InRange(squared.Y + (squared.Height / 2), 200 + (h / 2) - 1, 200 + (h / 2) + 1);
    }

    [Fact]
    public void SquaringGrowsToTheLongerSideAndNeverCutsIntoTheIcon()
    {
        var squared = RuneMarkerPainter.SquareUp(new Rectangle(10, 10, 52, 41));
        Assert.True(squared.Width >= 52 && squared.Height >= 41);
        Assert.Equal(new Rectangle(5, 5, 0, 0), RuneMarkerPainter.SquareUp(new Rectangle(5, 5, 0, 0)));
    }

    [Fact]
    public void TierColoursFollowThePathOfExileRarityScale()
    {
        // Ordinary = magic blue, more desirable = rare yellow, best on screen = unique orange.
        // A player reads that scale without being told what the overlay's colours mean.
        var ordinary = RuneMarkerPainter.ColorFor(RuneMarkerKind.Valuable, CarriedMarkerStyle.Slash);
        var better = RuneMarkerPainter.ColorFor(RuneMarkerKind.HighValue, CarriedMarkerStyle.Slash);

        Assert.True(ordinary.B > ordinary.R, "ordinary should read blue");
        Assert.True(better.R > 200 && better.G > 200 && better.B < 150, "more desirable should read yellow");

        // The recommendation out-ranks its own tier: an ordinary rune that is the top pick still
        // takes the orange, because on that panel it is the advice.
        var topOrdinary = RuneMarkerPainter.ColorFor(RuneMarkerKind.Valuable, CarriedMarkerStyle.Slash, isTopPick: true);
        var topBetter = RuneMarkerPainter.ColorFor(RuneMarkerKind.HighValue, CarriedMarkerStyle.Slash, isTopPick: true);
        Assert.Equal(RuneMarkerPainter.TopPickColor, topOrdinary);
        Assert.Equal(RuneMarkerPainter.TopPickColor, topBetter);

        // Carried is never recoloured by the top pick — it is not advice, it is "you have this".
        Assert.Equal(
            RuneMarkerPainter.ColorFor(RuneMarkerKind.Carried, CarriedMarkerStyle.Slash),
            RuneMarkerPainter.ColorFor(RuneMarkerKind.Carried, CarriedMarkerStyle.Slash, isTopPick: true));
    }

    [Fact]
    public void ThePulseBreathesSmoothlyAndNeverGoesOut()
    {
        // A halo that reached zero would read as the detector losing the rune, so it thins
        // instead of blinking.
        var samples = Enumerable.Range(0, 40).Select(i => RuneMarkerPainter.PulseStrength(i / 40.0)).ToList();

        Assert.All(samples, s => Assert.InRange(s, RuneMarkerPainter.GlowFloor - 1e-9, 1 + 1e-9));
        Assert.True(samples.Max() > 0.95, "the pulse should reach full strength");
        Assert.True(samples.Min() <= RuneMarkerPainter.GlowFloor + 1e-9, "and come back down");

        // Continuous across the seam, or the loop would visibly jump once per cycle.
        Assert.Equal(RuneMarkerPainter.PulseStrength(0), RuneMarkerPainter.PulseStrength(1.0), 6);
        Assert.Equal(RuneMarkerPainter.PulseStrength(0.25), RuneMarkerPainter.PulseStrength(1.25), 6);

        // No sudden steps between adjacent frames.
        for (var i = 1; i < samples.Count; i++)
            Assert.True(Math.Abs(samples[i] - samples[i - 1]) < 0.2, $"jump at frame {i}");
    }
}
