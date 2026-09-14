using System.IO;
using RuneshapePriceChecker.OCR;
using RuneshapePriceChecker.Rumours;
using Xunit;
using Xunit.Abstractions;

namespace RuneshapePriceChecker.Tests.Rumours;

/// <summary>
/// The whole path on real captures (RUNE-27): screen bitmap → Windows OCR → find the panel →
/// name its rumours. Four Uncharted Waters panels, cropped to the top-left 1400x900 of the frame
/// so the map, the act bar and the search box stay in shot as the noise the panel has to be
/// picked out of.
///
/// Between them they cover both list lengths seen so far (two rumours and three), four panel
/// positions, both footer wordings ("Consumes:" when a logbook is held, "Requires:" when not) and
/// a non-16:9 capture. Ground truth is what the panels say, read by eye.
/// </summary>
public class RumourPanelFixtureTests
{
    private readonly ITestOutputHelper _output;
    public RumourPanelFixtureTests(ITestOutputHelper output) => _output = output;

    public static TheoryData<string, string[]> Captures => new()
    {
        { "rumours-2-consumes-left.png",          ["Sulphite!", "Something Fishy"] },
        { "rumours-2-consumes-topleft.png",       ["It's Warm", "Nothing to drink"] },
        { "rumours-3-consumes-centre.png",        ["Cold as ice", "Nothing to drink", "Stardrinker"] },
        { "rumours-3-requires-2292x1392.png",     ["It's Warm", "Something Fishy", "It's Dry At Least"] },
    };

    [Theory]
    [MemberData(nameof(Captures))]
    public void FindsThePanelAndNamesEveryRumour(string fixture, string[] expected)
    {
        var path = FixturePath(fixture);
        if (path is null) { _output.WriteLine($"{fixture} absent — skipped"); return; }

        var panel = ReadPanel(path);

        Assert.NotNull(panel);
        Assert.Equal(expected, panel.Rumours.Select(r => r.Rumour?.Name).ToArray());

        foreach (var reading in panel.Rumours)
            _output.WriteLine($"  \"{reading.Text}\" -> {reading.Rumour?.Name} (d={reading.Distance}) at {reading.Bounds}");
    }

    [Theory]
    [MemberData(nameof(Captures))]
    public void MarksEachRumourWhereItIsDrawn(string fixture, string[] expected)
    {
        var path = FixturePath(fixture);
        if (path is null) { _output.WriteLine($"{fixture} absent — skipped"); return; }

        var panel = ReadPanel(path);
        Assert.NotNull(panel);

        // Every line sits inside the panel, and they come back in the order they are drawn.
        var previousBottom = int.MinValue;
        foreach (var reading in panel.Rumours)
        {
            Assert.True(panel.Bounds.Contains(reading.Bounds), $"{reading.Text} is outside the panel");
            Assert.True(reading.Bounds.Top > previousBottom, $"{reading.Text} is out of order");
            previousBottom = reading.Bounds.Top;
        }

        Assert.Equal(expected.Length, panel.Rumours.Count);
    }

    [Fact]
    public void PicksTheBestRatedRumourOnThePanel()
    {
        // Cold as ice (A+) over Nothing to drink (A) and Stardrinker (A).
        var path = FixturePath("rumours-3-consumes-centre.png");
        if (path is null) { _output.WriteLine("fixture absent — skipped"); return; }

        var panel = ReadPanel(path);
        Assert.NotNull(panel);
        Assert.Equal("Cold as ice", panel.Rumours[panel.BestIndex].Rumour?.Name);
    }

    [Fact]
    public void FindsNoPanelInACaptureThatHasNone()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures/runeicons/2560x1440/1 Raw.png");
        if (!File.Exists(path)) { _output.WriteLine("fixture absent — skipped"); return; }

        Assert.Null(ReadPanel(path));
    }

    private static RumourPanel? ReadPanel(string path)
    {
        using var bitmap = new Bitmap(path);
        var engine = new WindowsOcrEngine("eng");
        var lines = engine.RecognizeLines(bitmap);
        return RumourPanelReader.Read(lines, new RumourTable(RumourTable.LoadShippedJson()));
    }

    private static string? FixturePath(string name)
    {
        var candidate = Path.Combine(AppContext.BaseDirectory, "fixtures/rumours", name);
        return File.Exists(candidate) ? candidate : null;
    }
}
