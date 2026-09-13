using System.Drawing.Imaging;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RuneshapePriceChecker.Configuration;
using RuneshapePriceChecker.Contracts;
using RuneshapePriceChecker.Overlay;
using RuneshapePriceChecker.Runes;
using Xunit;

namespace RuneshapePriceChecker.Tests.Runes;

public class RuneRowScorerTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "rpc-scorer-" + Guid.NewGuid().ToString("N"));

    private const string ShippedJson = """
    {"schema":1,"runes":[
      {"id":"opulent","displayName":"Opulent Rune","effect":"","tier":"gold","weight":3.0},
      {"id":"power","displayName":"Power Rune","effect":"","weight":2.0},
      {"id":"bond","displayName":"Bond Rune","effect":"","tier":"purple","weight":1.0}
    ]}
    """;

    private static readonly IOptionsMonitor<RunesOptions> Options = new StaticOptions(new RunesOptions());

    private RuneCatalog NewCatalog() => new(Options, NullLogger.Instance, Path.Combine(_dir, "c.json"), ShippedJson);

    private static RuneKey Key(ulong hash, int x, int y, int hue = 9) => new(hash, hue, new byte[RuneKey.SpriteSize * RuneKey.SpriteSize * 3], new Rectangle(x, y, 45, 45));

    /// <summary>Observes twice so the binding is persisted, then binds it to a rune.</summary>
    private static string Bind(RuneCatalog catalog, RuneKey key, string runeId)
    {
        catalog.Observe(key);
        catalog.Observe(key);
        var id = catalog.FindBinding(key.ShapeHash)!.Id;
        Assert.True(catalog.Bind(id, runeId));
        return id;
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); } catch { }
    }

    [Fact]
    public void ScoresRows_ExcludesCarried_MarksBestAndTopPick()
    {
        using var catalog = NewCatalog();
        // Hashes 32+ bits apart so the shape matcher sees three distinct runes.
        var opulent = Key(0x0F0F0F0F0F0F0F0F, 110, 0);
        var power = Key(0xF0F0F0F0F0F0F0F0, 110, 108);
        var bond = Key(0x00FF00FF00FF00FF, 110, 216);
        Bind(catalog, opulent, "opulent");
        Bind(catalog, power, "power");
        Bind(catalog, bond, "bond");
        catalog.SetCarried("power", true);

        var snapshot = new LeagueWindowSnapshot(["a", "b", "c"], DateTimeOffset.UtcNow, [61, 169, 277],
            RuneRows:
            [
                new RuneRowKeys(61, [opulent]),
                new RuneRowKeys(169, [power]),
                new RuneRowKeys(277, [bond, power with { CellBounds = new Rectangle(170, 216, 45, 45) }])
            ]);

        var sheet = new RuneRowScorer(catalog, Options).Score(snapshot);

        Assert.Equal(3, sheet.Rows.Count);
        Assert.Equal(3.0, sheet.Rows[0].Score);
        Assert.True(sheet.Rows[0].IsBest);
        Assert.Equal(0.0, sheet.Rows[1].Score);
        Assert.False(sheet.Rows[1].IsBest);
        Assert.Equal(1.0, sheet.Rows[2].Score);

        var opulentScore = sheet.Rows[0].Keys.Single();
        Assert.Equal(RuneMarkerKind.HighValue, opulentScore.Marker);
        Assert.True(opulentScore.IsTopPick);

        var powerScore = sheet.Rows[1].Keys.Single();
        Assert.True(powerScore.IsCarried);
        Assert.Equal(RuneMarkerKind.Carried, powerScore.Marker);
        Assert.False(powerScore.IsTopPick);

        var bondScore = sheet.Rows[2].Keys[0];
        Assert.Equal(RuneMarkerKind.Valuable, bondScore.Marker);
        Assert.False(bondScore.IsTopPick);
    }

    [Fact]
    public void DuplicateRuneInOneRowCountsOnce()
    {
        using var catalog = NewCatalog();
        var bond = Key(0x00FF00FF00FF00FF, 110, 216);
        Bind(catalog, bond, "bond");

        var twice = new LeagueWindowSnapshot(["a"], DateTimeOffset.UtcNow, [61],
            RuneRows: [new RuneRowKeys(61, [bond, bond with { CellBounds = new Rectangle(170, 216, 45, 45) }])]);

        var sheet = new RuneRowScorer(catalog, Options).Score(twice);
        Assert.Equal(1.0, sheet.Rows[0].Score);
    }

    [Fact]
    public void UnboundKeyScoresByHueAndIsFlagged()
    {
        using var catalog = NewCatalog();
        var unseen = Key(0x9999, 110, 0, hue: 6);
        var snapshot = new LeagueWindowSnapshot(["a"], DateTimeOffset.UtcNow, [61], RuneRows: [new RuneRowKeys(61, [unseen])]);

        var sheet = new RuneRowScorer(catalog, Options).Score(snapshot);
        var k = sheet.Rows[0].Keys.Single();
        Assert.True(k.IsUnbound);
        Assert.Equal(0.5, k.Weight);
        Assert.Equal(RuneMarkerKind.Valuable, k.Marker);
        Assert.False(k.IsTopPick); // sole new rune: no runner-up to beat, so no badge (RUNE-4)
    }

    [Fact]
    public void EmptyAndNullRuneRowsProduceNoKeys()
    {
        using var catalog = NewCatalog();
        var scorer = new RuneRowScorer(catalog, Options);
        Assert.False(scorer.Score(new LeagueWindowSnapshot(["a"], DateTimeOffset.UtcNow)).HasKeys);
        Assert.False(scorer.Score(new LeagueWindowSnapshot(["a"], DateTimeOffset.UtcNow, [61], RuneRows: [new RuneRowKeys(61, [])])).HasKeys);
    }

    [Fact]
    public void SignatureChangesWithCatalogRevisionAndMarker()
    {
        using var catalog = NewCatalog();
        var bond = Key(0x3000, 110, 216);
        Bind(catalog, bond, "bond");
        var snapshot = new LeagueWindowSnapshot(["a"], DateTimeOffset.UtcNow, [61], RuneRows: [new RuneRowKeys(61, [bond])]);
        var scorer = new RuneRowScorer(catalog, Options);

        var before = scorer.Score(snapshot).Signature();
        catalog.SetCarried("bond", true);
        var after = scorer.Score(snapshot).Signature();

        Assert.NotEqual(before, after);
    }

    [Fact]
    public void PainterFramesEveryCellInItsColourAndNothingElse()
    {
        var markers = new List<RuneMarker>
        {
            new(new Rectangle(20, 20, 45, 45), RuneMarkerKind.Valuable, IsTopPick: false, IsUnbound: false),
            new(new Rectangle(120, 20, 45, 45), RuneMarkerKind.HighValue, IsTopPick: true, IsUnbound: false),
            new(new Rectangle(220, 20, 45, 45), RuneMarkerKind.Carried, IsTopPick: false, IsUnbound: true),
        };

        using var bmp = new Bitmap(320, 120, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Black);
            RuneMarkerPainter.Paint(g, markers, CarriedMarkerStyle.Slash);
        }

        // A pixel just outside each cell's left edge carries that marker's frame colour.
        AssertNear(bmp.GetPixel(18, 42), RuneMarkerPainter.ValuableColor);
        AssertNear(bmp.GetPixel(118, 42), RuneMarkerPainter.TopPickColor); // this marker is the top pick, which out-ranks its tier colour
        AssertNear(bmp.GetPixel(218, 42), RuneMarkerPainter.CarriedColor);

        // Interiors stay untouched (black) away from the slash and badges.
        Assert.Equal(Color.Black.ToArgb(), bmp.GetPixel(30, 30).ToArgb());
        Assert.Equal(Color.Black.ToArgb(), bmp.GetPixel(130, 55).ToArgb());

        // The gap between cells stays untouched.
        Assert.Equal(Color.Black.ToArgb(), bmp.GetPixel(90, 42).ToArgb());
    }

    [Theory]
    [InlineData("slash", CarriedMarkerStyle.Slash)]
    [InlineData("CROSS", CarriedMarkerStyle.Cross)]
    [InlineData("dim", CarriedMarkerStyle.Dim)]
    [InlineData("nonsense", CarriedMarkerStyle.Slash)]
    [InlineData(null, CarriedMarkerStyle.Slash)]
    public void ParseStyle(string? text, CarriedMarkerStyle expected)
    {
        Assert.Equal(expected, RuneMarkerPainter.ParseStyle(text));
    }

    [Fact]
    public void FrameThicknessScalesWithCellAndNeverBelowTwo()
    {
        Assert.Equal(3, RuneMarkerPainter.FrameThickness(new Rectangle(0, 0, 45, 45)));
        Assert.Equal(2, RuneMarkerPainter.FrameThickness(new Rectangle(0, 0, 20, 20)));
        Assert.Equal(6, RuneMarkerPainter.FrameThickness(new Rectangle(0, 0, 90, 90)));
    }

    private static void AssertNear(Color actual, Color expected)
    {
        var d = Math.Abs(actual.R - expected.R) + Math.Abs(actual.G - expected.G) + Math.Abs(actual.B - expected.B);
        Assert.True(d < 60, $"expected ~{expected}, got {actual}");
    }

    private sealed class StaticOptions(RunesOptions value) : IOptionsMonitor<RunesOptions>
    {
        public RunesOptions CurrentValue => value;
        public RunesOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<RunesOptions, string?> listener) => null;
    }
}
