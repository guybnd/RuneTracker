using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RuneshapePriceChecker.App;
using RuneshapePriceChecker.Configuration;
using RuneshapePriceChecker.OCR;
using RuneshapePriceChecker.Rumours;

using Xunit;

namespace RuneshapePriceChecker.Tests.Rumours;

/// <summary>
/// Reading around the cursor instead of the whole client (RUNE-27), which is what makes scanning
/// without a hotkey affordable: 50 ms a read rather than 230 ms at 2560x1440.
/// </summary>
public class RumourWatchTests
{
    private static readonly Rectangle Client = new(0, 0, 2560, 1440);

    [Fact]
    public void ReadsAGenerousBoxAroundTheCursor()
    {
        var region = RumourReadService.CursorRegion(new Point(1280, 720), Client);

        Assert.Equal(1536, region.Width);
        Assert.Equal(1080, region.Height);
        Assert.Equal(1280, region.Left + (region.Width / 2));
    }

    [Fact]
    public void ReachesFurtherAboveTheCursorThanBelowIt()
    {
        // The rumour list is drawn well above the node — half a screen, for a node low on the map.
        // A charted node's tooltip sits just below it. Both have to fit.
        var cursor = new Point(1280, 720);
        var region = RumourReadService.CursorRegion(cursor, Client);

        var above = cursor.Y - region.Top;
        var below = region.Bottom - cursor.Y;

        Assert.True(above > below, "the panel above the node needs the room");
        Assert.True(above >= 700, $"only {above}px above the cursor — a high panel would be cut off");
        Assert.True(region.Contains(new Point(1280, 180)), "a panel above the node is inside the read region");
        Assert.True(region.Contains(new Point(1280, 1000)), "a tooltip below the node is inside the read region");
    }

    [Fact]
    public void CostsMeaningfullyLessThanReadingTheWholeClient()
    {
        var region = RumourReadService.CursorRegion(new Point(1280, 720), Client);

        var fraction = (double)(region.Width * region.Height) / (Client.Width * Client.Height);
        Assert.True(fraction < 0.5, $"the read region is {fraction:P0} of the client — too close to reading all of it");
    }

    [Fact]
    public void StaysInsideTheClientNearACorner()
    {
        var region = RumourReadService.CursorRegion(new Point(40, 30), Client);

        Assert.True(Client.Contains(region));
        Assert.True(region.Width > 0 && region.Height > 0);
    }

    [Fact]
    public void ReadsNothingWithoutAClient()
        => Assert.Equal(Rectangle.Empty, RumourReadService.CursorRegion(new Point(1, 1), Rectangle.Empty));

    [Fact]
    public void AsksForTheRegionAroundTheCursorAndReturnsBoxesInClientCoordinates()
    {
        var window = new FakeWindow { Context = new WindowCaptureContext(IntPtr.Zero, 0, 0, 2560, 1440), Foreground = true };
        var cursor = new Point(1800, 900);
        var expected = RumourReadService.CursorRegion(cursor, Client);

        Rectangle? asked = null;
        var service = new RumourReadService(
            new StaticOptionsMonitor<OcrOptions>(new OcrOptions()),
            window,
            new RumourTable(RumourTable.LoadShippedJson()),
            NullLoggerFactory.Instance,
            NullLogger<RumourReadService>.Instance)
        {
            CursorProvider = () => cursor,
            LineReader = r => { asked = r; return PanelLines(); }
        };

        var result = service.Read(aroundCursor: true);

        Assert.Equal(expected, asked);
        Assert.Equal(RumourReadOutcome.Read, result.Outcome);
        Assert.NotNull(result.Panel);

        // The reader works on a bitmap of the region, so its boxes start at (0,0). They are only
        // useful to an overlay covering the client once they have been moved back.
        var first = result.Panel.Rumours[0];
        Assert.Equal("Cold as ice", first.Rumour?.Name);
        Assert.Equal(expected.X + 100, first.Bounds.X);
        Assert.Equal(expected.Y + 140, first.Bounds.Y);
        Assert.True(result.Panel.Bounds.Contains(first.Bounds));
    }

    [Fact]
    public void LeavesBoxesAloneWhenTheWholeClientIsRead()
    {
        var window = new FakeWindow { Context = new WindowCaptureContext(IntPtr.Zero, 0, 0, 2560, 1440), Foreground = true };
        var service = new RumourReadService(
            new StaticOptionsMonitor<OcrOptions>(new OcrOptions()),
            window,
            new RumourTable(RumourTable.LoadShippedJson()),
            NullLoggerFactory.Instance,
            NullLogger<RumourReadService>.Instance)
        {
            LineReader = _ => PanelLines()
        };

        var result = service.Read();

        Assert.Equal(100, result.Panel!.Rumours[0].Bounds.X);
        Assert.Equal(140, result.Panel.Rumours[0].Bounds.Y);
    }

    [Fact]
    public void TheSameSightingTwiceIsNotNews()
    {
        var a = Result(new Rectangle(100, 140, 200, 30));
        var b = Result(new Rectangle(100, 140, 200, 30));
        var moved = Result(new Rectangle(100, 400, 200, 30));

        Assert.Equal(RumourWatchService.Signature(a), RumourWatchService.Signature(b));
        Assert.NotEqual(RumourWatchService.Signature(a), RumourWatchService.Signature(moved));
    }

    [Fact]
    public void SaysWhatItFoundOnceRatherThanOncePerReRead()
    {
        // The watch re-reads a panel it is already marking, several times a second, to notice when
        // it closes. Every one of those used to write the full list to the log again.
        var window = new FakeWindow { Context = new WindowCaptureContext(IntPtr.Zero, 0, 0, 2560, 1440), Foreground = true };
        var log = new CountingLogger();
        var service = new RumourReadService(
            new StaticOptionsMonitor<OcrOptions>(new OcrOptions()),
            window,
            new RumourTable(RumourTable.LoadShippedJson()),
            NullLoggerFactory.Instance,
            log)
        {
            LineReader = _ => PanelLines()
        };

        _ = service.Read();
        var afterFirst = log.Informations;
        _ = service.Read();
        _ = service.Read();

        Assert.True(afterFirst > 0, "the first sighting should be reported");
        Assert.Equal(afterFirst, log.Informations);
    }

    [Fact]
    public void SaysItAgainOnceTheScreenHasChanged()
    {
        var window = new FakeWindow { Context = new WindowCaptureContext(IntPtr.Zero, 0, 0, 2560, 1440), Foreground = true };
        var log = new CountingLogger();
        var lines = PanelLines();
        var service = new RumourReadService(
            new StaticOptionsMonitor<OcrOptions>(new OcrOptions()),
            window,
            new RumourTable(RumourTable.LoadShippedJson()),
            NullLoggerFactory.Instance,
            log)
        {
            LineReader = _ => lines
        };

        _ = service.Read();
        var afterFirst = log.Informations;

        lines = [.. PanelLines().Select(l => new OcrLine(l.Text, l.Bounds with { Y = l.Bounds.Y + 200 }))];
        _ = service.Read();

        Assert.True(log.Informations > afterFirst, "a panel that has moved is a new sighting");
    }

    [Fact]
    public void NothingOnScreenHasNoSignature()
        => Assert.Equal("", RumourWatchService.Signature(
            new RumourReadResult(RumourReadOutcome.NoPanel, null, null, Client)));

    [Fact]
    public void AutoScanAndTheHotkeyAreBothOnByDefault()
    {
        var options = new RumoursOptions();

        Assert.True(options.AutoScan);
        Assert.False(string.IsNullOrWhiteSpace(options.ReadHotkey));
        Assert.InRange(options.SettleMs, 50, 1000);
    }

    private static RumourReadResult Result(Rectangle bounds)
    {
        var table = new RumourTable(RumourTable.LoadShippedJson());
        var panel = new RumourPanel(bounds, [new RumourReading("Cold as ice...", bounds, table.Match("Cold as ice..."), 0)]);
        return new RumourReadResult(RumourReadOutcome.Read, panel, null, Client);
    }

    /// <summary>A minimal Uncharted Waters panel, in the coordinates of the captured region.</summary>
    private static IReadOnlyList<OcrLine> PanelLines() =>
    [
        new("USE A LOGBOOK TO CHART THE AREA", new Rectangle(80, 60, 340, 24)),
        new("ISLAND RUMOURS", new Rectangle(120, 100, 220, 26)),
        new("Cold as ice...", new Rectangle(100, 140, 200, 30)),
        new("no&viw' to drink..", new Rectangle(100, 190, 220, 30)),
        new("CONSUMES:", new Rectangle(140, 250, 160, 24)),
        new("Expedition Logbook", new Rectangle(110, 290, 240, 26)),
    ];

    /// <summary>Counts Information lines, which is all these tests need to know about logging.</summary>
    private sealed class CountingLogger : ILogger<RumourReadService>
    {
        public int Informations { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Information) Informations++;
        }
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
