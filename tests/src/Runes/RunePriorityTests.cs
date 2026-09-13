using RuneshapePriceChecker.App.Dashboard;
using Xunit;

namespace RuneshapePriceChecker.Tests.Runes;

/// <summary>
/// The priority picker replaced a bare weight box, because a number gives no way to know whether
/// "most important" means 3 or 30. These pin the mapping in both directions — a level that does
/// not round-trip silently changes what the user asked for.
/// </summary>
public class RunePriorityTests
{
    [Theory]
    [InlineData(RunePriority.MustHave, 3.0)]
    [InlineData(RunePriority.Wanted, 2.0)]
    [InlineData(RunePriority.Normal, 1.0)]
    [InlineData(RunePriority.Low, 0.5)]
    [InlineData(RunePriority.Ignore, 0.0)]
    public void LevelsRoundTripThroughTheirWeights(RunePriority priority, double weight)
    {
        Assert.Equal(weight, RunePriorities.WeightOf(priority));
        Assert.Equal(priority, RunePriorities.FromWeight(weight));
        Assert.Equal(weight, RunePriorities.WeightForLabel(RunePriorities.Label(priority)));
    }

    [Fact]
    public void TheShippedWeightsAllLandOnNamedLevels()
    {
        // ocr/rune-catalog.json ships 3.0 (Opulent), 2.0 (Power), 1.0 (27 runes) and 0.5 (blue
        // tier). If any of those read as Custom, most of the library would open showing "Custom".
        foreach (var shipped in new[] { 3.0, 2.0, 1.0, 0.5 })
            Assert.NotEqual(RunePriority.Custom, RunePriorities.FromWeight(shipped));
    }

    [Fact]
    public void AHandTypedWeightIsShownRatherThanRounded()
    {
        Assert.Equal(RunePriority.Custom, RunePriorities.FromWeight(1.25));
        Assert.Equal("Custom (1.25)", RunePriorities.LabelForWeight(1.25));
        Assert.Null(RunePriorities.WeightForLabel("Custom (1.25)"));
    }

    [Fact]
    public void ThePickerOffersTheCustomValueOnlyWhenTheRuneIsOnOne()
    {
        var onALevel = RunePriorities.ChoicesFor(2.0);
        Assert.Equal(5, onALevel.Count);
        Assert.Equal("Must have", onALevel[0]);
        Assert.DoesNotContain(onALevel, c => c.StartsWith("Custom", StringComparison.Ordinal));

        var custom = RunePriorities.ChoicesFor(1.25);
        Assert.Equal(6, custom.Count);
        Assert.Equal("Custom (1.25)", custom[0]);
    }

    [Fact]
    public void LevelsAreListedMostWantedFirst()
    {
        var weights = RunePriorities.All.Select(RunePriorities.WeightOf).ToList();
        Assert.Equal(weights.OrderByDescending(w => w), weights);
    }

    [Fact]
    public void IgnoreMeansZeroSoTheRuneAddsNothingToARowScore()
    {
        // RuneRowScorer sums weights of the runes a row offers; Ignore has to contribute nothing
        // or "ignore" would still pull a row up the ranking.
        Assert.Equal(0.0, RunePriorities.WeightOf(RunePriority.Ignore));
    }

    [Fact]
    public void TheEntryViewReportsItsOwnLevelAndChoices()
    {
        var view = new RuneLibraryEntryView { Id = "opulent", Weight = 3.0 };
        Assert.Equal("Must have", view.PriorityLabel);
        Assert.Contains("Must have", view.PriorityChoices);

        view.Weight = 0.5;
        Assert.Equal("Low", view.PriorityLabel);
    }
}
