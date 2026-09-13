using RuneshapePriceChecker.App.Dashboard;
using Xunit;

namespace RuneshapePriceChecker.Tests.Runes;

/// <summary>
/// The priority ladder that replaced the named levels (Ignore / Low / Normal / Wanted / Must
/// have). The levels could not express "Death before Bond" — two runes on one level are
/// indistinguishable, which left the scorer no basis to recommend one row over another.
/// </summary>
public class RuneRankingTests
{
    /// <summary>The order the catalog ships, which is the order the user asked for.</summary>
    private static readonly string[] Shipped =
        ["opulent", "power", "rebirth", "death", "bond", "life", "soul", "time", "oath"];

    private static RuneLibraryEntryView Rune(string id, double weight) =>
        new() { Id = id, DisplayName = char.ToUpperInvariant(id[0]) + id[1..] + " Rune", Weight = weight };

    /// <summary>The full library: the shipped ladder plus three unranked runes at the baseline.</summary>
    private static List<RuneLibraryEntryView> Library()
    {
        var runes = Shipped.Select((id, i) => Rune(id, RuneRanking.WeightAt(i, Shipped.Length))).ToList();
        runes.AddRange(["wisdom", "adaptive", "moon"], w => Rune(w, RuneRanking.BaseWeight));
        return runes;
    }

    [Fact]
    public void EachRungIsOneStepAboveTheOneBelow_AndTheBottomClearsTheBaseline()
    {
        var weights = Enumerable.Range(0, Shipped.Length).Select(i => RuneRanking.WeightAt(i, Shipped.Length)).ToList();

        Assert.Equal([20.0, 18.0, 16.0, 14.0, 12.0, 10.0, 8.0, 6.0, 4.0], weights);
        for (var i = 1; i < weights.Count; i++)
            Assert.Equal(RuneRanking.Step, weights[i - 1] - weights[i]);

        // The gap matters as much as the order: a row of unranked runes must not out-score the
        // rune at the bottom of the ladder just by holding more of them.
        Assert.True(weights[^1] > RuneRanking.BaseWeight * 1.5);
        Assert.True(RuneRanking.IsRanked(weights[^1]));
        Assert.False(RuneRanking.IsRanked(RuneRanking.BaseWeight));
    }

    [Fact]
    public void LegacyLevelWeightsReadAsUnranked()
    {
        // The old levels were 0, 0.5, 1, 2 and 3. None of them may be mistaken for a rung, or a
        // stale "Must have" would look like a deliberate ladder position.
        foreach (var legacy in new[] { 0.0, 0.5, 1.0, 2.0, 3.0 })
            Assert.False(RuneRanking.IsRanked(legacy), $"{legacy} must not read as ranked");
    }

    [Fact]
    public void TheLadderIsReadBackFromWeightsInOrder()
    {
        Assert.Equal(Shipped, RuneRanking.LadderOf(Library()));
    }

    [Fact]
    public void SortShowsTheLadderFirstThenTheRestAlphabetically()
    {
        var sorted = RuneRanking.Sort(Library()).Select(r => r.Id).ToList();

        Assert.Equal(Shipped, sorted.Take(Shipped.Length));
        Assert.Equal(["Adaptive Rune", "Moon Rune", "Wisdom Rune"],
            RuneRanking.Sort(Library()).Skip(Shipped.Length).Select(r => r.DisplayName));
    }

    [Fact]
    public void MovingUpSwapsWithTheRungAbove_AndTheTopRungStaysPut()
    {
        var ladder = RuneRanking.LadderOf(Library());

        var promoted = RuneRanking.MoveUp(ladder, "rebirth");
        Assert.Equal(["opulent", "rebirth", "power", "death", "bond", "life", "soul", "time", "oath"], promoted);

        Assert.Equal(ladder, RuneRanking.MoveUp(ladder, "opulent"));
    }

    [Fact]
    public void AnUnrankedRuneJoinsTheBottomOfTheLadder()
    {
        var ladder = RuneRanking.LadderOf(Library());
        var joined = RuneRanking.MoveUp(ladder, "wisdom");

        Assert.Equal("wisdom", joined[^1]);
        Assert.Equal(Shipped.Length + 1, joined.Count);

        // Everything already on the ladder shifts up a rung, because the ladder got deeper.
        var weights = RuneRanking.WeightsFor(joined);
        Assert.Equal(22.0, weights["opulent"]);
        Assert.Equal(4.0, weights["wisdom"]);
    }

    [Fact]
    public void TheBottomRungFallsOffTheLadderRatherThanStickingThere()
    {
        var ladder = RuneRanking.LadderOf(Library());

        var demoted = RuneRanking.MoveDown(ladder, "oath");
        Assert.DoesNotContain("oath", demoted);
        Assert.Equal(Shipped.Length - 1, demoted.Count);

        // Off the ladder it is worth the baseline.
        var changes = RuneRanking.Diff(ladder, demoted);
        Assert.Equal(RuneRanking.BaseWeight, changes.Single(c => c.Id == "oath").Weight);

        // The rungs above it keep their order and each drops one step, because the bottom rung is
        // anchored: however deep the ladder is, its lowest rune stays a full step clear of the
        // unranked crowd instead of drifting down into it.
        var weights = RuneRanking.WeightsFor(demoted);
        Assert.Equal(demoted, weights.OrderByDescending(w => w.Value).Select(w => w.Key));
        Assert.Equal(RuneRanking.BaseWeight + RuneRanking.Step, weights[demoted[^1]]);
        Assert.Equal(RuneRanking.BaseWeight + RuneRanking.Step, RuneRanking.WeightsFor(ladder)[ladder[^1]]);

        // An unranked rune cannot be pushed further down.
        Assert.Equal(demoted, RuneRanking.MoveDown(demoted, "oath"));
    }

    [Fact]
    public void AMoveWithinTheLadderRewritesExactlyTwoWeights()
    {
        var ladder = RuneRanking.LadderOf(Library());
        var moved = RuneRanking.MoveUp(ladder, "death");

        // One click must not turn into 34 catalog saves and 34 re-renders.
        var changes = RuneRanking.Diff(ladder, moved).OrderBy(c => c.Id, StringComparer.Ordinal).ToList();
        Assert.Equal(2, changes.Count);
        Assert.Equal(("death", 16.0), changes[0]);
        Assert.Equal(("rebirth", 14.0), changes[1]);
    }

    [Fact]
    public void RankLabelCountsFromOne_AndTheCrowdGetsADash()
    {
        var ladder = RuneRanking.LadderOf(Library());

        Assert.Equal("1", RuneRanking.RankLabel(ladder, "opulent"));
        Assert.Equal("9", RuneRanking.RankLabel(ladder, "oath"));
        Assert.Equal("—", RuneRanking.RankLabel(ladder, "wisdom"));
    }

    [Fact]
    public void EqualWeightsStillProduceOneStableOrder()
    {
        // A hand-edited file, or one saved by an older build, can hold duplicate weights. The
        // ladder must not flicker between renders, so ties break on display name.
        List<RuneLibraryEntryView> tied = [Rune("soul", 8.0), Rune("death", 8.0), Rune("bond", 8.0)];

        Assert.Equal(["bond", "death", "soul"], RuneRanking.LadderOf(tied));
        Assert.Equal(RuneRanking.LadderOf(tied), RuneRanking.LadderOf(tied.AsEnumerable().Reverse()));
    }

    [Fact]
    public void ReorderingIsReversible()
    {
        var ladder = RuneRanking.LadderOf(Library());
        var there = RuneRanking.MoveUp(ladder, "life");
        var back = RuneRanking.MoveDown(there, "life");

        Assert.Equal(ladder, back);
        Assert.Empty(RuneRanking.Diff(ladder, back));
    }
}

internal static class RuneRankingTestExtensions
{
    public static void AddRange<T>(this List<T> list, IEnumerable<string> ids, Func<string, T> make)
    {
        foreach (var id in ids) list.Add(make(id));
    }
}
