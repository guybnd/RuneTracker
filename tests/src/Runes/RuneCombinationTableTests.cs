using System.Text.Json;
using RuneshapePriceChecker.Contracts;
using RuneshapePriceChecker.OCR;
using RuneshapePriceChecker.Runes;
using Xunit;

namespace RuneshapePriceChecker.Tests.Runes;

public class RuneCombinationTableTests
{
    private const string Json = """
        { "combinations": [
          { "name": "Skyfall", "level": 20, "quantity": 0, "tier": "Lv70+", "runes": ["tempest", "celestial", "protective", "ward", "wisdom", "oath"] },
          { "name": "Skyfall", "level": 0, "quantity": 0, "tier": "Lv65-74", "runes": ["tempest", "celestial", "protective", "ward"] },
          { "name": "Healing Runes", "level": 0, "quantity": 0, "tier": "Lv65-74", "runes": ["electrocuting", "prismatic", "protective", "ward"] },
          { "name": "Perfect Chaos Orb", "level": 0, "quantity": 3, "tier": "Lv70+", "runes": ["adaptive", "sky", "oath"] },
          { "name": "Perfect Chaos Orb", "level": 0, "quantity": 3, "tier": "Lv70+", "runes": ["death", "sky", "fire"] },
          { "name": "Divine Orb", "level": 0, "quantity": 10, "tier": "Lv70+", "runes": ["death", "soul", "power", "life", "vision", "celestial", "toxic", "prismatic", "tidal"] },
          { "name": "Divine Orb", "level": 0, "quantity": 3, "tier": "Lv70+", "runes": ["death", "soul", "power", "life", "vision", "celestial"] },
          { "name": "Divine Orb", "level": 0, "quantity": 0, "tier": "Lv65-74", "runes": ["death", "soul", "power", "life"] }
        ] }
        """;

    private static readonly RuneCombinationTable Table = new(Json);

    [Theory]
    [InlineData("Skill Level 20: Skyfall", 6, 1, "celestial")]
    [InlineData("Skill Level 20: Skyfall", 6, 5, "oath")]
    [InlineData("Skyfall", 4, 3, "ward")]                 // the plain variant, picked by icon count
    [InlineData("Skyfall", 6, 1, "celestial")]            // no level in the text — count alone decides
    [InlineData("Skyfall", 0, 1, "celestial")]            // count unknown, both variants agree at cell 1
    [InlineData("Skill Level 20: Skyfa1l", 6, 1, "celestial")] // OCR read 'l' as '1'
    [InlineData("skill level 20 : SKYFALL", 6, 5, "oath")] // case and spacing never matter
    [InlineData("Support: Healing Runes", 4, 1, "prismatic")]
    [InlineData("Perfect Chaos Orb x3", 3, 1, "sky")]     // two recipes, same rune at that cell
    [InlineData("10x Divine Orb", 9, 2, "power")]         // the game's quantity prefix
    [InlineData("10x Divine Orb", 9, 6, "toxic")]
    [InlineData("Divine Orb x10", 9, 6, "toxic")]         // poe2db's quantity suffix
    [InlineData("3x Divine Orb", 6, 2, "power")]
    [InlineData("1x Divine Orb", 4, 2, "power")]          // "1x" is the no-stack recipe
    [InlineData("Divine Orb", 4, 2, "power")]
    public void Lookup_NamesTheRuneAtTheCell(string rowText, int cellCount, int cellIndex, string expected)
    {
        Assert.Equal(expected, Table.Lookup(rowText, cellCount, cellIndex));
    }

    [Theory]
    [InlineData("Perfect Chaos Orb x3", 3, 0)]   // two recipes disagree at that cell — no guess
    [InlineData("Support: Healing Runes", 4, 4)] // past the end of the recipe
    [InlineData("Skyfall", 0, 5)]                // one candidate has no sixth rune
    [InlineData("Skyfall", 5, 1)]                // no five-icon Skyfall: a miscounted row is not guessed at
    [InlineData("10x Divine Orb", 8, 1)]         // the clipped-row case: 8 cells found of 9
    [InlineData("7x Divine Orb", 6, 2)]          // no seven-stack recipe
    [InlineData("Nonsense Name", 4, 1)]
    [InlineData("Skill Level 20: Sk", 6, 1)]     // too little text to match anything safely
    [InlineData("", 6, 1)]
    [InlineData(null, 6, 1)]
    [InlineData("Skyfall", 6, -1)]
    public void Lookup_ReturnsNullRatherThanGuess(string? rowText, int cellCount, int cellIndex)
    {
        Assert.Null(Table.Lookup(rowText, cellCount, cellIndex));
    }

    [Theory]
    [InlineData("Skill Level 20: Skyfall", "skyfall", 20, 0)]
    [InlineData("Skyfall (Level 20)", "skyfall", 20, 0)]
    [InlineData("Support: Healing Runes", "healingrunes", 0, 0)]
    [InlineData("10x Divine Orb", "divineorb", 0, 10)]
    [InlineData("Divine Orb x10", "divineorb", 0, 10)]
    [InlineData("Perfect Chaos Orb ×3", "perfectchaosorb", 0, 3)]
    [InlineData("Greater Exalted Orb (Level 20) x3", "greaterexaltedorb", 20, 3)]
    [InlineData("Aldur's Legacy", "aldurslegacy", 0, 0)]
    public void ParseRowText_StripsPrefixLevelAndQuantity(string rowText, string expectedKey, int expectedLevel, int expectedQuantity)
    {
        var (key, level, quantity) = RuneCombinationTable.ParseRowText(rowText);
        Assert.Equal(expectedKey, key);
        Assert.Equal(expectedLevel, level);
        Assert.Equal(expectedQuantity, quantity);
    }

    [Fact]
    public void ShippedTable_LoadsAndNamesOnlyCatalogRunes()
    {
        var table = new RuneCombinationTable(RuneCombinationTable.LoadShippedJson());
        Assert.True(table.Combinations.Count >= 300, $"only {table.Combinations.Count} shipped combinations — currency recipes missing?");

        using var catalog = JsonDocument.Parse(RuneCatalog.LoadShippedJson());
        var ids = catalog.RootElement.GetProperty("runes").EnumerateArray()
            .Select(r => r.GetProperty("id").GetString()!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknown = table.Combinations.SelectMany(c => c.Runes).Where(r => !ids.Contains(r)).Distinct().ToList();
        Assert.True(unknown.Count == 0, $"rune ids not in the catalog: {string.Join(", ", unknown)}");

        Assert.All(table.Combinations, c => Assert.InRange(c.Runes.Count, 1, 10));

        // Recipes whose icons carry a bare " Celestial Rune" tooltip lost up to nine runes when the
        // parser keyed on the tooltip text; the ten-rune uniques are the canary.
        Assert.Equal(10, table.Combinations.Single(c => c.Name == "Aldur's Legacy").Runes.Count);

        // The rows the feature was built and checked on (6 Raw.png at 1440p, 1 Raw.png at 1080p).
        Assert.Equal("celestial", table.Lookup("Skill Level 20: Skyfall", 6, 1));
        Assert.Equal("oath", table.Lookup("Skill Level 20: Skyfall", 6, 5));
        Assert.Equal("life", table.Lookup("Skill Level 20: Leylines", 6, 5));
        Assert.Equal("prismatic", table.Lookup("Support: Healing Runes", 4, 1));
        Assert.Equal("power", table.Lookup("10x Divine Orb", 9, 2));
        Assert.Equal("toxic", table.Lookup("10x Divine Orb", 9, 6));
        Assert.Equal("power", table.Lookup("3x Divine Orb", 6, 2));
        Assert.Equal("power", table.Lookup("1x Divine Orb", 4, 2));
        Assert.Null(table.Lookup("10x Divine Orb", 8, 1));
    }

    [Fact]
    public void FilterRuneRowsToMatchedRows_AttachesEachRowsText()
    {
        RuneRowKeys[] runeRows =
        [
            new(100, [new RuneKey(1, 0, [])]),
            new(200, [new RuneKey(2, 0, [])]),
            new(300, [new RuneKey(3, 0, [])]),
        ];

        var kept = OcrLeagueWindowReader.FilterRuneRowsToMatchedRows(runeRows, [100, 300], ["Skill Level 20: Skyfall", "Support: Healing Runes"]);

        Assert.Equal(2, kept.Length);
        Assert.Equal("Skill Level 20: Skyfall", kept[0].ItemName);
        Assert.Equal("Support: Healing Runes", kept[1].ItemName);

        // A name list that does not line up with the Y positions is ignored rather than mis-joined.
        var unnamed = OcrLeagueWindowReader.FilterRuneRowsToMatchedRows(runeRows, [100, 300], ["only one"]);
        Assert.All(unnamed, r => Assert.Null(r.ItemName));
    }
}
