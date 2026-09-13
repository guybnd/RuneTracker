using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using RuneshapePriceChecker.App;
using RuneshapePriceChecker.Configuration;
using RuneshapePriceChecker.OCR;
using RuneshapePriceChecker.Runes;
using Xunit;

namespace RuneshapePriceChecker.Tests.Runes;

/// <summary>
/// The tooltip hotkey (RUNE-18) marks a socketed rune by the name in its tooltip. Everything
/// between the OCR engine's text and the catalog id is pinned here, with the kind of noise the
/// engine actually produces on the game's tooltip: the glyph in front of the name read as a
/// stray character, punctuation dropped, a letter swapped.
/// </summary>
public class RuneTooltipReaderTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "rpc-tip-" + Guid.NewGuid().ToString("N"));

    private static readonly IReadOnlyList<RuneDefinition> Runes =
    [
        new() { Id = "power", DisplayName = "Power Rune" },
        new() { Id = "opulent", DisplayName = "Opulent Rune" },
        new() { Id = "fire", DisplayName = "Fire Rune" },
        new() { Id = "cold", DisplayName = "Cold Rune" },
        new() { Id = "bond", DisplayName = "Bond Rune" },
        new() { Id = "bloodletting", DisplayName = "Bloodletting Rune" },
        new() { Id = "death", DisplayName = "Death Rune" },
        new() { Id = "earth", DisplayName = "Earth Rune" },
    ];

    private const string PowerTooltip = """
        @Power Rune
        Runes gain:
        Empowered
        The Runic Modifier in this slot will be added
        to all Monsters unearthed after this Remnant
        """;

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); } catch { }
    }

    [Fact]
    public void ReadsTheNameAboveTheGainAnchor()
    {
        Assert.Equal("power", RuneTooltipReader.Match(Runes, PowerTooltip)?.Id);
    }

    [Theory]
    [InlineData("&% Opulent Rune\nRunes gain:\nWealth", "opulent")]     // glyph read as two symbols
    [InlineData("Fire Rune\nRunes gain\nBurning", "fire")]               // colon dropped
    [InlineData("Co1d Rune\nRunes gain:\nChilled", "cold")]              // one misread letter
    [InlineData("Bond Rune\nRunas gain:\nBonded", "bond")]               // anchor itself misread
    [InlineData("O Deatb Rune\nRunes gain:\nDeadly", "death")]           // glyph as a letter plus a typo
    public void ForgivesOcrNoise(string text, string expected)
    {
        Assert.Equal(expected, RuneTooltipReader.Match(Runes, text)?.Id);
    }

    [Fact]
    public void FallsBackToALineEndingInRuneWhenTheAnchorIsMissing()
    {
        Assert.Equal("earth", RuneTooltipReader.Match(Runes, "GAME PAUSED\n# Earth Rune\nConjures Earthly Spires")?.Id);
    }

    [Fact]
    public void DoesNotInventARuneFromUnrelatedText()
    {
        Assert.Null(RuneTooltipReader.Match(Runes, "GAME PAUSED\nDetonate Explosives\n3x Perfect Orb of Transmutation"));
        Assert.Null(RuneTooltipReader.Match(Runes, ""));
        Assert.Null(RuneTooltipReader.Match(Runes, null));
    }

    [Fact]
    public void DoesNotConfuseTwoRunesThatShareAWord()
    {
        // "Rune" alone is common to every name; the distinguishing word has to match.
        Assert.Null(RuneTooltipReader.Match(Runes, "Rune\nRunes gain:\nSomething"));
        Assert.Equal("bloodletting", RuneTooltipReader.Match(Runes, "Bloodleting Rune\nRunes gain:\nLeech")?.Id);
    }

    [Fact]
    public void ExactNameBeatsANearMiss()
    {
        IReadOnlyList<RuneDefinition> close =
        [
            new() { Id = "bold", DisplayName = "Bold Rune" },
            new() { Id = "cold", DisplayName = "Cold Rune" },
        ];
        Assert.Equal("cold", RuneTooltipReader.Match(close, "Cold Rune\nRunes gain:\nx")?.Id);
        Assert.Equal("bold", RuneTooltipReader.Match(close, "Bold Rune\nRunes gain:\nx")?.Id);
    }

    [Fact]
    public void NormalizeKeepsLettersAndSingleSpaces()
    {
        Assert.Equal("power rune", RuneTooltipReader.Normalize("  @Power  Rune: "));
        Assert.Equal("runes gain", RuneTooltipReader.Normalize("Runes gain:"));
        Assert.Equal("", RuneTooltipReader.Normalize("123 ***"));
    }

    [Fact]
    public void TooltipRegionSitsAboveTheCursorAndInsideTheClient()
    {
        var client = new Rectangle(0, 0, 2560, 1440);
        var region = RuneTooltipReader.TooltipRegionFor(new Point(1280, 900), client);

        Assert.Equal(1280, region.Width);
        Assert.Equal(640, region.X);
        Assert.True(region.Top < 900 && region.Bottom > 900, "the box straddles the cursor row with most of it above");
        Assert.True(region.Top >= 900 - 576 - 1 && region.Top <= 900 - 576 + 1);
    }

    [Fact]
    public void TooltipRegionIsClampedToTheClientAndEmptyOutsideIt()
    {
        var client = new Rectangle(100, 50, 1920, 1080);

        var nearCorner = RuneTooltipReader.TooltipRegionFor(new Point(120, 80), client);
        Assert.Equal(client.Left, nearCorner.Left);
        Assert.Equal(client.Top, nearCorner.Top);
        Assert.True(nearCorner.Width > 0 && nearCorner.Height > 0);

        var outside = RuneTooltipReader.TooltipRegionFor(new Point(5000, 5000), client);
        Assert.True(outside.Width <= 0 || outside.Height <= 0);

        Assert.Equal(Rectangle.Empty, RuneTooltipReader.TooltipRegionFor(new Point(1, 1), Rectangle.Empty));
    }

    [Fact]
    public void ServiceTogglesTheNamedRuneAndRefusesWithoutTheGameInFront()
    {
        const string shipped = """
        {"schema":1,"runes":[
          {"id":"power","displayName":"Power Rune","weight":2.0},
          {"id":"opulent","displayName":"Opulent Rune","weight":3.0}
        ]}
        """;
        using var catalog = new RuneCatalog(new StaticOptionsMonitor<RunesOptions>(new RunesOptions()), NullLogger.Instance, Path.Combine(_dir, "rune-catalog.json"), shipped);
        var window = new FakeWindow { Context = new WindowCaptureContext(IntPtr.Zero, 0, 0, 1920, 1080), Foreground = true };
        var service = new RuneTooltipMarkService(
            new StaticOptionsMonitor<OcrOptions>(new OcrOptions()), window, catalog, NullLoggerFactory.Instance, NullLogger<RuneTooltipMarkService>.Instance)
        {
            CursorProvider = () => new Point(960, 800),
            TextReader = _ => PowerTooltip
        };

        Assert.Equal(RuneTooltipMarkResult.Marked, service.TryToggleFromTooltip());
        Assert.True(catalog.IsCarried("power"));
        Assert.False(catalog.IsCarried("opulent"));

        Assert.Equal(RuneTooltipMarkResult.Unmarked, service.TryToggleFromTooltip());
        Assert.False(catalog.IsCarried("power"));

        service.TextReader = _ => "GAME PAUSED";
        Assert.Equal(RuneTooltipMarkResult.NoTooltip, service.TryToggleFromTooltip());

        service.TextReader = _ => null;
        Assert.Equal(RuneTooltipMarkResult.Unavailable, service.TryToggleFromTooltip());

        window.Foreground = false;
        service.TextReader = _ => PowerTooltip;
        Assert.Equal(RuneTooltipMarkResult.NoGameWindow, service.TryToggleFromTooltip());
        Assert.False(catalog.IsCarried("power"));
    }

    [Fact]
    public void ServiceReadsTheRegionAboveTheCursor()
    {
        using var catalog = new RuneCatalog(new StaticOptionsMonitor<RunesOptions>(new RunesOptions()), NullLogger.Instance, Path.Combine(_dir, "rune-catalog.json"), """{"schema":1,"runes":[{"id":"power","displayName":"Power Rune"}]}""");
        var window = new FakeWindow { Context = new WindowCaptureContext(IntPtr.Zero, 0, 0, 1920, 1080), Foreground = true };
        Rectangle? asked = null;
        var service = new RuneTooltipMarkService(
            new StaticOptionsMonitor<OcrOptions>(new OcrOptions()), window, catalog, NullLoggerFactory.Instance, NullLogger<RuneTooltipMarkService>.Instance)
        {
            CursorProvider = () => new Point(1290, 527),
            TextReader = r => { asked = r; return PowerTooltip; }
        };

        _ = service.TryToggleFromTooltip();

        Assert.NotNull(asked);
        Assert.Equal(RuneTooltipReader.TooltipRegionFor(new Point(1290, 527), new Rectangle(0, 0, 1920, 1080)), asked!.Value);
        Assert.True(asked.Value.Contains(new Point(1290, 400)), "the tooltip drawn above the socket is inside the read region");
    }

    private sealed class FakeWindow : IPoe2WindowResolutionProvider
    {
        public OcrCaptureRegion? CurrentCaptureRegion => null;
        public OcrResolutionProfile? CurrentResolutionProfile => null;
        public string? CurrentResolutionKey => null;
        public WindowCaptureContext? Context { get; set; }
        public WindowCaptureContext? CurrentWindowCaptureContext => Context;
        public bool Foreground { get; set; }
        public bool IsPoe2WindowForeground => Foreground;
    }
}
