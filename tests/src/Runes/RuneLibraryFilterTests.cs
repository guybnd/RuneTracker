using RuneshapePriceChecker.App.Dashboard;
using Xunit;

namespace RuneshapePriceChecker.Tests.Runes;

/// <summary>
/// The library's filter and progress line drive what the user sees while doing the one-off
/// job of naming sprites, so they are pinned here — the window itself cannot be opened in a test.
/// </summary>
public class RuneLibraryFilterTests
{
    private static RuneLibraryEntryView Entry(string id, int seen = 0, bool carried = false, string? bindingId = null) =>
        new() { Id = id, DisplayName = id, SeenCount = seen, IsCarried = carried, BindingId = bindingId };

    private static List<RuneLibraryEntryView> Sample() =>
    [
        Entry("opulent", seen: 4, carried: true, bindingId: "k-1"),
        Entry("power", seen: 2, bindingId: "k-2"),
        Entry("bond", seen: 1),
        Entry("bait")
    ];

    [Fact]
    public void AllFilterKeepsEveryRuneInOrder()
    {
        var result = RuneLibraryFilters.Apply(Sample(), RuneLibraryFilter.All);
        Assert.Equal(["opulent", "power", "bond", "bait"], result.Select(r => r.Id));
    }

    [Fact]
    public void SeenAndUnseenPartitionTheCatalog()
    {
        var all = Sample();
        var seen = RuneLibraryFilters.Apply(all, RuneLibraryFilter.Seen);
        var unseen = RuneLibraryFilters.Apply(all, RuneLibraryFilter.Unseen);

        Assert.Equal(["opulent", "power", "bond"], seen.Select(r => r.Id));
        Assert.Equal(["bait"], unseen.Select(r => r.Id));
        Assert.Equal(all.Count, seen.Count + unseen.Count);
    }

    [Fact]
    public void CarriedFilterShowsOnlyRunesTakenThisRun()
    {
        var result = RuneLibraryFilters.Apply(Sample(), RuneLibraryFilter.Carried);
        Assert.Equal(["opulent"], result.Select(r => r.Id));
    }

    [Fact]
    public void SeenCountsSightingsNotBindings()
    {
        // "bond" has been seen but never named. It belongs under Seen, not Unseen — the whole
        // point of the filter is to find sprites that are waiting to be named.
        var bond = Entry("bond", seen: 1);
        Assert.True(RuneLibraryFilters.IsMatch(bond, RuneLibraryFilter.Seen));
        Assert.False(RuneLibraryFilters.IsMatch(bond, RuneLibraryFilter.Unseen));
        Assert.False(bond.IsBound);
    }

    [Fact]
    public void EveryFilterHasALabelAndAnEmptyMessage()
    {
        // An empty list under a filter is a normal state; each one must explain itself rather
        // than leaving a blank panel that reads as a bug.
        foreach (var filter in RuneLibraryFilters.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(RuneLibraryFilters.Label(filter)));
            Assert.False(string.IsNullOrWhiteSpace(RuneLibraryFilters.EmptyMessage(filter)));
        }

        Assert.Equal(4, RuneLibraryFilters.All.Count);
        Assert.Equal(RuneLibraryFilter.All, RuneLibraryFilters.All[0]);
    }

    [Fact]
    public void SummaryCountsBoundRunesAndCarriedOnes()
    {
        Assert.Equal("2 of 4 bound · 1 carried this run", RuneLibrarySummary.Describe(Sample()));
    }

    [Fact]
    public void SummarySaysNoneRatherThanZeroWhenNothingIsCarried()
    {
        var entries = new[] { Entry("bond", seen: 3, bindingId: "k-3"), Entry("bait") };
        Assert.Equal("1 of 2 bound · none carried yet", RuneLibrarySummary.Describe(entries));
    }

    [Fact]
    public void SummaryHandlesAnEmptyCatalog()
    {
        Assert.Equal("0 of 0 bound · none carried yet", RuneLibrarySummary.Describe([]));
    }

    [Fact]
    public void SeenButUnboundRunesDoNotCountAsProgress()
    {
        // A sighting is not setup progress — the user still has to say which rune it is.
        var entries = new[] { Entry("bond", seen: 9) };
        Assert.Equal("0 of 1 bound · none carried yet", RuneLibrarySummary.Describe(entries));
    }
}
