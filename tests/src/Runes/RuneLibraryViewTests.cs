using RuneshapePriceChecker.App.Dashboard;
using Xunit;

namespace RuneshapePriceChecker.Tests.Runes;

/// <summary>
/// The Rune Library row binds to these properties, so they are pinned here: a blank weight box or
/// a missing subtitle is a broken row in a window the tests cannot open (RUNE-5).
/// </summary>
public class RuneLibraryViewTests
{
    [Theory]
    [InlineData(3.0, "3")]
    [InlineData(2.0, "2")]
    [InlineData(1.0, "1")]
    [InlineData(0.5, "0.5")]
    [InlineData(0.0, "0")]
    [InlineData(1.25, "1.25")]
    public void WeightTextRendersTheValueSetByThePresenter(double weight, string expected)
    {
        // Exactly how RuneLibraryPresenter builds a row: an object initializer, then binding reads.
        var view = new RuneLibraryEntryView { Id = "opulent", DisplayName = "Opulent Rune", Weight = weight };

        Assert.Equal(expected, view.WeightText);
        Assert.False(string.IsNullOrWhiteSpace(view.WeightText));
    }

    [Fact]
    public void WeightTextSurvivesTheDefaultWeightOfOne()
    {
        // 1.0 is the shipped weight for most runes and equals the field's default-adjacent value,
        // so a setter that skips "unchanged" writes would leave the box blank for 27 of 34 rows.
        var view = new RuneLibraryEntryView { Id = "bond", Weight = 1.0 };
        Assert.Equal("1", view.WeightText);
    }

    [Theory]
    [InlineData("gold", false, 4, "gold · seen 4×")]
    [InlineData("", false, 0, "tier unknown · not seen yet")]
    [InlineData("purple", true, 2, "purple · rare · seen 2×")]
    public void SubtitleCarriesTierRarityAndSightings(string tier, bool rare, int seen, string expected)
    {
        var view = new RuneLibraryEntryView { Id = "x", Tier = tier, Rare = rare, SeenCount = seen };
        Assert.Equal(expected, view.SubtitleText);
    }

    [Fact]
    public void EditingWeightTextRoundTripsThroughWeight()
    {
        var view = new RuneLibraryEntryView { Id = "power", Weight = 2.0 };

        view.WeightText = "4.5";
        Assert.Equal(4.5, view.Weight);

        view.WeightText = "not a number"; // rejected, previous value kept
        Assert.Equal(4.5, view.Weight);
    }

    [Fact]
    public void UnboundSpriteSummaryNamesTheColourAndScore()
    {
        var view = new UnboundSpriteView { BindingId = "k-1", SeenCount = 3, ColourHint = "purple", CurrentWeight = 1.0 };
        Assert.Equal("seen 3× · purple glyph · scoring 1 until bound", view.Summary);

        var dark = new UnboundSpriteView { BindingId = "k-2", SeenCount = 1, ColourHint = "", CurrentWeight = 0.5 };
        Assert.Equal("seen 1× · dark glyph · scoring 0.5 until bound", dark.Summary);
    }
}
