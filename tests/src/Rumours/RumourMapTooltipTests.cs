using System.IO;
using RuneshapePriceChecker.OCR;
using RuneshapePriceChecker.Overlay;
using RuneshapePriceChecker.Rumours;
using Xunit;
using Xunit.Abstractions;

namespace RuneshapePriceChecker.Tests.Rumours;

/// <summary>
/// Naming a charted node from its own tooltip (RUNE-27). Once the logbook is spent the rumour
/// wording is gone and the node shows an ordinary map tooltip, but the tier list pairs each rumour
/// with exactly one map, so the title still identifies it — and the title is block small caps,
/// which reads far more cleanly than the handwriting ever did.
/// </summary>
public class RumourMapTooltipTests
{
    private readonly ITestOutputHelper _output;
    public RumourMapTooltipTests(ITestOutputHelper output) => _output = output;

    private static RumourTable Table => new(RumourTable.LoadShippedJson());

    [Fact]
    public void NamesTheMapInARealCapture()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures/rumours/map-tooltip-sloughed-gully.png");
        if (!File.Exists(path)) { _output.WriteLine("fixture absent — skipped"); return; }

        using var bitmap = new Bitmap(path);
        var lines = new WindowsOcrEngine("eng").RecognizeLines(bitmap);

        var tooltip = RumourMapTooltipReader.Read(lines, Table);

        Assert.NotNull(tooltip);
        _output.WriteLine($"\"{tooltip.Title}\" -> {tooltip.Rumour?.Name} (d={tooltip.Distance}) at {tooltip.Bounds}");
        Assert.Equal("It's Dry At Least", tooltip.Rumour?.Name);
        Assert.Equal("Sloughed Gully", tooltip.Rumour?.Map);
        Assert.Equal("D", tooltip.Rumour?.Rating);
    }

    [Fact]
    public void MarksTheMapWithItsTierAndMods()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures/rumours/map-tooltip-sloughed-gully.png");
        if (!File.Exists(path)) { _output.WriteLine("fixture absent — skipped"); return; }

        using var bitmap = new Bitmap(path);
        var lines = new WindowsOcrEngine("eng").RecognizeLines(bitmap);
        var tooltip = RumourMapTooltipReader.Read(lines, Table);
        Assert.NotNull(tooltip);

        var badge = Assert.Single(RumourBadgePainter.FromMap(tooltip));
        Assert.Equal("D", badge.Rating);
        Assert.Contains("It's Dry At Least", badge.Detail, StringComparison.Ordinal);
        Assert.Contains("Monster effectiveness", badge.Detail, StringComparison.Ordinal);
        Assert.Equal(RumourBadgePainter.DColour, badge.Colour);
    }

    [Fact]
    public void FindsNoMapTooltipInARumourPanelCapture()
    {
        // The rumour list has no "Biome:" line, so the two readers cannot mistake each other's panel.
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures/rumours/rumours-3-consumes-centre.png");
        if (!File.Exists(path)) { _output.WriteLine("fixture absent — skipped"); return; }

        using var bitmap = new Bitmap(path);
        var lines = new WindowsOcrEngine("eng").RecognizeLines(bitmap);

        Assert.Null(RumourMapTooltipReader.Read(lines, Table));
    }

    [Theory]
    [InlineData("Sloughed Gully", "It's Dry At Least")]
    [InlineData("SLOUGHED GULLY", "It's Dry At Least")]
    [InlineData("Frigid Bluffs", "Cold as ice")]
    [InlineData("Lake of Kalandra", "Reflective Waters")]
    [InlineData("Secluded Temple", "Stardrinker")]
    public void NamesTheRumourEachMapBelongsTo(string title, string expected)
        => Assert.Equal(expected, Table.MatchMap(title)?.Name);

    [Theory]
    [InlineData("Biome: Ocean")]
    [InlineData("Requires: Waystone (Tier 6) or higher")]
    [InlineData("The past lies piled under dirt")]
    public void RefusesTooltipTextThatIsNotAMapName(string text) => Assert.Null(Table.MatchMap(text));

    [Fact]
    public void HoldsMapNamesToATighterBarThanRumourLines()
    {
        // Block small caps read all but perfectly, so a loose match here would be a wrong answer
        // rather than a rescued one — unlike the handwritten rumour lines.
        Assert.True(RumourTable.MaxMapDistance < RumourTable.MaxDistance);
        Assert.Null(Table.MatchMap("Sloughed Gullyxxxxxx"));
    }
}
