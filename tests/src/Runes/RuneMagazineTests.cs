using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RuneshapePriceChecker.App;
using RuneshapePriceChecker.Configuration;
using RuneshapePriceChecker.Contracts;
using RuneshapePriceChecker.Runes;
using Xunit;

namespace RuneshapePriceChecker.Tests.Runes;

/// <summary>
/// The mark-carried hotkey (RUNE-11) is the only way to fill the magazine mid-run, and it acts on
/// whatever the cursor is over — so the mapping from a screen point to a rune is pinned here.
/// </summary>
public class RuneMagazineTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "rpc-mag-" + Guid.NewGuid().ToString("N"));

    private const string ShippedJson = """
    {"schema":1,"runes":[
      {"id":"opulent","displayName":"Opulent Rune","tier":"gold","weight":3.0},
      {"id":"power","displayName":"Power Rune","weight":2.0}
    ]}
    """;

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); } catch { }
    }

    private RuneCatalog NewCatalog() =>
        new(new FixedOptions(new RunesOptions()), NullLogger.Instance, Path.Combine(_dir, "rune-catalog.json"), ShippedJson);

    private sealed class FixedOptions(RunesOptions value) : IOptionsMonitor<RunesOptions>
    {
        public RunesOptions CurrentValue => value;
        public RunesOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<RunesOptions, string?> listener) => null;
    }

    private static RuneKeyScore Score(string bindingId, string? runeId, Rectangle cell) =>
        new(new RuneKey(1, 9, [], cell), bindingId, runeId, runeId ?? "unnamed", 1.0, false, runeId is null, false, RuneMarkerKind.Valuable);

    private static RuneScoreSheet Sheet(params RuneKeyScore[] keys) =>
        new([new RuneRowScore(0, keys, 1.0, false)], 1);

    [Fact]
    public void FindAtPicksTheCellContainingThePoint()
    {
        var sheet = Sheet(
            Score("k-1", "opulent", new Rectangle(10, 10, 40, 40)),
            Score("k-2", "power", new Rectangle(60, 10, 40, 40)));

        Assert.Equal("opulent", RuneHitTester.FindAt(sheet, new Point(30, 30))?.RuneId);
        Assert.Equal("power", RuneHitTester.FindAt(sheet, new Point(80, 30))?.RuneId);
        Assert.Null(RuneHitTester.FindAt(sheet, new Point(55, 30)));   // the gap between them
        Assert.Null(RuneHitTester.FindAt(sheet, new Point(30, 200)));  // below the row
        Assert.Null(RuneHitTester.FindAt(null, new Point(30, 30)));
    }

    [Fact]
    public void FindAtIncludesTheCellsOwnEdgesButNotTheFarOnes()
    {
        // Rectangle.Contains is inclusive on left/top and exclusive on right/bottom. Pinned so a
        // cursor resting on a shared boundary resolves to exactly one cell, never two or none.
        var sheet = Sheet(Score("k-1", "opulent", new Rectangle(10, 10, 40, 40)));

        Assert.NotNull(RuneHitTester.FindAt(sheet, new Point(10, 10)));
        Assert.NotNull(RuneHitTester.FindAt(sheet, new Point(49, 49)));
        Assert.Null(RuneHitTester.FindAt(sheet, new Point(50, 50)));
        Assert.Null(RuneHitTester.FindAt(sheet, new Point(9, 10)));
    }

    [Fact]
    public void OverlappingCellsResolveToTheSmaller()
    {
        // Cells never overlap on a real panel, but a drifted box could. A tight wrong box is a
        // likelier read than a huge one, so the smaller wins rather than list order deciding.
        var sheet = Sheet(
            Score("k-big", "opulent", new Rectangle(0, 0, 200, 200)),
            Score("k-small", "power", new Rectangle(10, 10, 40, 40)));

        Assert.Equal("power", RuneHitTester.FindAt(sheet, new Point(20, 20))?.RuneId);
    }

    [Fact]
    public void ZeroSizedCellsAreNeverHit()
    {
        var sheet = Sheet(Score("k-1", "opulent", new Rectangle(10, 10, 0, 0)));
        Assert.Null(RuneHitTester.FindAt(sheet, new Point(10, 10)));
    }

    [Fact]
    public void ToCaptureSpaceSubtractsTheRegionOrigin()
    {
        // CellBounds are in capture-region coordinates; the cursor is in screen coordinates.
        Assert.Equal(new Point(41, 95), RuneHitTester.ToCaptureSpace(new Point(110, 300), 69, 205));
    }

    [Fact]
    public void TogglingAddsThenRemoves()
    {
        using var catalog = NewCatalog();
        var magazine = new RuneMagazine(catalog, NullLogger<RuneMagazine>.Instance)
        {
            CursorProvider = () => new Point(69 + 30, 205 + 30)
        };
        magazine.SetSheet(Sheet(Score("k-1", "opulent", new Rectangle(10, 10, 40, 40))), new Rectangle(69, 205, 663, 715));

        Assert.Equal(RuneMarkResult.Marked, magazine.ToggleAtCursor());
        Assert.True(catalog.IsCarried("opulent"));

        Assert.Equal(RuneMarkResult.Unmarked, magazine.ToggleAtCursor());
        Assert.False(catalog.IsCarried("opulent"));
    }

    [Fact]
    public void AnUnnamedRuneIsCarriedUnderItsBindingId()
    {
        // Marking must work before the user has named the sprite — that is most of a first run.
        using var catalog = NewCatalog();
        var magazine = new RuneMagazine(catalog, NullLogger<RuneMagazine>.Instance)
        {
            CursorProvider = () => new Point(100, 250)
        };
        magazine.SetSheet(Sheet(Score("k-abc", null, new Rectangle(10, 10, 60, 60))), new Rectangle(69, 205, 663, 715));

        Assert.Equal(RuneMarkResult.Marked, magazine.ToggleAtCursor());
        Assert.True(catalog.IsCarried("k-abc"));
    }

    [Fact]
    public void MissingTheCellsAndHavingNoPanelAreDifferentAnswers()
    {
        using var catalog = NewCatalog();
        var magazine = new RuneMagazine(catalog, NullLogger<RuneMagazine>.Instance)
        {
            CursorProvider = () => new Point(5000, 5000)
        };

        Assert.Equal(RuneMarkResult.NothingOnScreen, magazine.ToggleAtCursor());

        magazine.SetSheet(Sheet(Score("k-1", "opulent", new Rectangle(10, 10, 40, 40))), new Rectangle(69, 205, 663, 715));
        Assert.Equal(RuneMarkResult.NoRuneUnderCursor, magazine.ToggleAtCursor());
        Assert.Empty(catalog.Carried);
    }

    [Fact]
    public void EveryResultHasWordingForTheLog()
    {
        foreach (var result in Enum.GetValues<RuneMarkResult>())
            Assert.False(string.IsNullOrWhiteSpace(result.Describe()));
    }

    [Theory]
    [InlineData("Alt+V", 0x0001u, (uint)'V')]
    [InlineData("alt + v", 0x0001u, (uint)'V')]
    public void TheDefaultMarkHotkeyParses(string text, uint modifiers, uint vk)
    {
        Assert.True(GlobalHotkeyService.TryParse(text, out var m, out var k));
        Assert.Equal(modifiers, m);
        Assert.Equal(vk, k);
    }

    [Fact]
    public void TheTwoDefaultHotkeysAreDifferent()
    {
        var o = new RunesOptions();
        Assert.NotEqual(o.ResetHotkey, o.MarkCarriedHotkey);
        Assert.True(GlobalHotkeyService.TryParse(o.MarkCarriedHotkey, out _, out _));
    }
}
