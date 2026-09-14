using RuneshapePriceChecker.Overlay;
using RuneshapePriceChecker.Rumours;
using Xunit;

namespace RuneshapePriceChecker.Tests.Rumours;

/// <summary>
/// Placement and colouring of the rumour marks (RUNE-27), on plain rectangles so the rules hold
/// without a game window. The panel is drawn beside whichever node was clicked, so a mark has to
/// cope with that node being hard against either edge of the screen.
/// </summary>
public class RumourBadgePainterTests
{
    private static readonly Size Screen = new(2560, 1440);
    private static readonly Size Badge = new(300, 40);

    [Fact]
    public void PutsTheMarksToTheRightOfTheLines()
    {
        Rectangle[] lines = [new(1000, 400, 200, 30), new(1000, 440, 160, 30)];

        var boxes = RumourBadgePainter.LayoutColumn(lines, [Badge, Badge], Screen);

        Assert.All(boxes, b => Assert.True(b.Left > lines.Max(l => l.Right) - 1, "the marks should sit clear of the text"));
        Assert.Equal(lines[0].Top + ((lines[0].Height - Badge.Height) / 2), boxes[0].Top);
    }

    [Fact]
    public void FlipsToTheLeftWhenTheRightSideHasNoRoom()
    {
        Rectangle[] lines = [new(2300, 400, 200, 30)];

        var boxes = RumourBadgePainter.LayoutColumn(lines, [Badge], Screen);

        Assert.True(boxes[0].Right <= lines[0].Left, "the mark should sit clear of the text on the other side");
        Assert.True(boxes[0].Left >= 0);
    }

    [Fact]
    public void KeepsEveryMarkOnTheSameSide()
    {
        // A panel is only as wide as its longest rumour, so a short line beside a long one has
        // room on the right when the long one does not. Deciding per line would alternate sides.
        Rectangle[] lines = [new(2000, 400, 400, 30), new(2000, 440, 90, 30)];

        var boxes = RumourBadgePainter.LayoutColumn(lines, [Badge, Badge], Screen);

        Assert.Equal(boxes[0].Left, boxes[1].Left);
    }

    [Fact]
    public void SharesOneEdgeSoTheMarksReadAsAColumn()
    {
        Rectangle[] lines = [new(400, 100, 300, 30), new(400, 140, 180, 30)];
        Size[] sizes = [new(260, 40), new(200, 40)];

        var boxes = RumourBadgePainter.LayoutColumn(lines, sizes, Screen);

        Assert.Equal(boxes[0].Left, boxes[1].Left);
        Assert.True(boxes[0].Top < boxes[1].Top);
    }

    [Fact]
    public void KeepsTheMarkOnScreenWhenNeitherSideHasRoom()
    {
        var narrow = new Size(320, 200);
        Rectangle[] lines = [new(40, 100, 240, 30)];

        var box = RumourBadgePainter.LayoutColumn(lines, [Badge], narrow)[0];

        Assert.True(box.Left >= 0);
        Assert.True(box.Right <= narrow.Width);
        Assert.True(box.Top >= 0);
        Assert.True(box.Bottom <= narrow.Height);
    }

    [Fact]
    public void ClampsAMarkAgainstTheTopOfTheScreen()
    {
        Rectangle[] lines = [new(600, 2, 200, 30)];

        var boxes = RumourBadgePainter.LayoutColumn(lines, [Badge], Screen);

        Assert.True(boxes[0].Top >= 0);
    }

    [Theory]
    [InlineData("S+")]
    [InlineData("A+")]
    [InlineData("A")]
    [InlineData("B")]
    [InlineData("C")]
    [InlineData("D")]
    public void GivesEveryTierItsOwnColour(string rating)
    {
        var others = new[] { "S+", "A+", "A", "B", "C", "D" }
            .Where(r => r != rating)
            .Select(RumourBadgePainter.ColourFor);

        Assert.DoesNotContain(RumourBadgePainter.ColourFor(rating), others);
    }

    [Fact]
    public void MarksAnUnknownRatingAsUnknown()
        => Assert.Equal(RumourBadgePainter.UnknownColour, RumourBadgePainter.ColourFor("Z"));

    [Fact]
    public void StarsTheBestRumourAndOnlyThatOne()
    {
        var panel = new RumourPanel(new Rectangle(0, 0, 400, 300),
        [
            Reading("It's Dry At Least", 100),
            Reading("Cold as ice", 140),
            Reading("Something Fishy", 180),
        ]);

        var badges = RumourBadgePainter.FromPanel(panel);

        Assert.Equal(["D", "A+", "B"], badges.Select(b => b.Rating).ToArray());
        Assert.Equal([false, true, false], badges.Select(b => b.IsBest).ToArray());
    }

    [Fact]
    public void MarksALineNoRowMatchedAsUnrecognised()
    {
        var panel = new RumourPanel(new Rectangle(0, 0, 400, 300),
        [
            new RumourReading("Sonoedoiw' evdirelb wew", new Rectangle(20, 100, 200, 30), null, -1),
        ]);

        var badge = Assert.Single(RumourBadgePainter.FromPanel(panel));

        Assert.Equal("?", badge.Rating);
        Assert.Equal(RumourBadgePainter.UnknownColour, badge.Colour);
        Assert.Contains("Sonoedoiw' evdirelb wew", badge.Detail, StringComparison.Ordinal);
        Assert.False(badge.IsBest);
    }

    [Fact]
    public void ScalesTheTextWithTheLineButNeverBelowLegible()
    {
        Assert.True(RumourBadgePainter.FontHeight(45) > RumourBadgePainter.FontHeight(30));
        Assert.True(RumourBadgePainter.FontHeight(4) >= 11f);
    }

    private static RumourReading Reading(string name, int top)
    {
        var table = new RumourTable(RumourTable.LoadShippedJson());
        return new RumourReading(name, new Rectangle(20, top, 200, 30), table.Match(name, out var d), d);
    }
}
