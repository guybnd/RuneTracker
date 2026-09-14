using RuneshapePriceChecker.Rumours;
using Xunit;
using Xunit.Abstractions;

namespace RuneshapePriceChecker.Tests.Rumours;

/// <summary>
/// Matching the Island Rumours list against the tier table (RUNE-27).
///
/// The interesting cases are not clean strings — the rumour lines are handwritten and Windows OCR
/// makes a mess of them. Every sample below is a line the engine really returned for one of the
/// four captures, at 1x and at 2x, kept verbatim. They are the regression bar: the table has to
/// keep naming all of them, because the feature is worth nothing if it only works on the lines
/// that happened to read cleanly.
/// </summary>
public class RumourTableTests
{
    private readonly ITestOutputHelper _output;
    public RumourTableTests(ITestOutputHelper output) => _output = output;

    private static RumourTable Table => new(RumourTable.LoadShippedJson());

    public static TheoryData<string, string> ObservedLines => new()
    {
        { "Sulphite!",            "Sulphite!" },
        { "Some&viw' fishy...",   "Something Fishy" },
        { "Somedoiw' fisbv...",   "Something Fishy" },
        { "Sovuedoiw' fisbv...",  "Something Fishy" },
        { "Waru but risklå...",   "It's Warm" },
        { "Wanw but risklå...",   "It's Warm" },
        { "Wanw but riskb...",    "It's Warm" },
        { "now' to drink..",      "Nothing to drink" },
        { "no&viw' to drink..",   "Nothing to drink" },
        { "Lodoiw' to c(riwk..",  "Nothing to drink" },
        { "Cold as ice...",       "Cold as ice" },
        { "Stardriwker...",       "Stardrinker" },
        { "Starc(riwker...",      "Stardrinker" },
        { "Lt's at least...",     "It's Dry At Least" },
        { "It's at least...",     "It's Dry At Least" },
    };

    [Theory]
    [MemberData(nameof(ObservedLines))]
    public void NamesEveryLineTheEngineReturnedFromTheCaptures(string ocr, string expected)
    {
        var match = Table.Match(ocr, out var distance);

        Assert.NotNull(match);
        Assert.Equal(expected, match.Name);
        _output.WriteLine($"\"{ocr}\" -> {match.Name} (d={distance})");
    }

    [Fact]
    public void ReadsEveryRowOfTheShippedTable()
    {
        var table = Table;

        Assert.Equal(19, table.Rumours.Count);
        Assert.All(table.Rumours, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.Id));
            Assert.False(string.IsNullOrWhiteSpace(r.Map));
            Assert.False(string.IsNullOrWhiteSpace(r.Mods));
            Assert.True(RumourTable.RatingRank(r.Rating) < 9, $"{r.Name} has an unknown rating '{r.Rating}'");
        });
    }

    [Fact]
    public void NamesEveryRowFromItsOwnCanonicalText()
    {
        var table = Table;
        foreach (var rumour in table.Rumours)
        {
            var match = table.Match(rumour.Name, out var distance);
            Assert.Equal(rumour.Id, match?.Id);
            Assert.Equal(0, distance);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Expedition Logbook")]
    [InlineData("USE A LOGBOOK TO CHART THE AREA")]
    [InlineData("2,265/2,783")]
    public void RefusesTextThatIsNotARumour(string text) => Assert.Null(Table.Match(text));

    [Fact]
    public void RefusesRatherThanGuessWhenTwoRowsAreEquallyClose()
    {
        // Halfway between "Last To Fall" and "Fallen Stars" — no row owns it, so no row is named.
        Assert.Null(Table.Match("Fall"));
    }

    [Fact]
    public void RatingRankOrdersTheTiersBestFirst()
    {
        Assert.True(RumourTable.RatingRank("S+") < RumourTable.RatingRank("A+"));
        Assert.True(RumourTable.RatingRank("A+") < RumourTable.RatingRank("A"));
        Assert.True(RumourTable.RatingRank("A") < RumourTable.RatingRank("B"));
        Assert.True(RumourTable.RatingRank("B") < RumourTable.RatingRank("D"));
        Assert.True(RumourTable.RatingRank("D") < RumourTable.RatingRank("nonsense"));
    }
}
