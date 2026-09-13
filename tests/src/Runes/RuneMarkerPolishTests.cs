using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RuneshapePriceChecker.Configuration;
using RuneshapePriceChecker.Contracts;
using RuneshapePriceChecker.OCR;
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

    private static RuneKey Key(ulong hash, int x, int hue = 9) => new(hash, hue, new byte[32 * 32 * 3], new Rectangle(x, 10, 45, 45));

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
}
