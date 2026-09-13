using RuneshapePriceChecker.Contracts;
using RuneshapePriceChecker.Overlay;
using Xunit;

namespace RuneshapePriceChecker.Tests.Runes;

/// <summary>
/// The panel is read once per session and frozen until it closes. Hovering a row makes the game
/// tint it gold, which collapses the contrast the gilded test needs — so pointing at a rune to
/// press the hotkey is what used to lose it, and an earlier version that re-latched on changed
/// row text kept re-mangling the first row because OCR text jitters between reads.
/// </summary>
public class RuneLatchTests
{
    private static RuneRowKeys Row(int y, params ulong[] hashes) =>
        new(y, hashes.Select(h => new RuneKey(h, 9, [], new Rectangle(10, y, 40, 40))).ToList());

    // Mirrors LeaguePricingWorker.LatchRuneRows. Kept here as an executable statement of the rule;
    // the worker itself cannot be constructed in a test without the whole OCR stack.
    private sealed class Latch(TimeSpan settling)
    {
        private bool _open;
        private DateTimeOffset _openedAt;
        private IReadOnlyList<RuneRowKeys>? _rows;

        public IReadOnlyList<RuneRowKeys>? Apply(bool panelPresent, IReadOnlyList<RuneRowKeys>? fresh, DateTimeOffset now)
        {
            if (!panelPresent) { _open = false; _rows = null; return null; }

            var freshCount = fresh?.Sum(r => r.Keys.Count) ?? 0;
            if (!_open)
            {
                _open = true;
                _openedAt = now;
                _rows = freshCount > 0 ? fresh : null;
                return _rows;
            }

            var latchedCount = _rows?.Sum(r => r.Keys.Count) ?? 0;
            var settlingNow = now - _openedAt <= settling;
            if (freshCount > 0 && (_rows is null || (settlingNow && freshCount > latchedCount)))
                _rows = fresh;

            return _rows;
        }
    }

    private static Latch NewLatch() => new(TimeSpan.FromSeconds(1.5));
    private static readonly DateTimeOffset T0 = new(2026, 9, 13, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AHoveredRowDoesNotLoseItsRunes()
    {
        var latch = NewLatch();
        var full = new[] { Row(10, 1), Row(60, 2) };
        Assert.Same(full, latch.Apply(true, full, T0));

        // Hovering washes out the second row: the fresh read has fewer runes.
        var hovered = new[] { Row(10, 1) };
        var held = latch.Apply(true, hovered, T0.AddSeconds(0.3));
        Assert.Equal(2, held!.Sum(r => r.Keys.Count));
    }

    [Fact]
    public void ABetterReadWhileSettlingReplacesTheLatch()
    {
        var latch = NewLatch();
        _ = latch.Apply(true, [Row(10, 1)], T0);

        var full = new[] { Row(10, 1), Row(60, 2) };
        Assert.Same(full, latch.Apply(true, full, T0.AddSeconds(1.0)));
    }

    [Fact]
    public void AfterSettlingEvenABetterReadIsIgnored()
    {
        // This is the rule the user asked for: parse once on load, never re-parse. A later read
        // finding "more" runes is how the first row was getting re-mangled mid-session.
        var latch = NewLatch();
        var first = new[] { Row(10, 1) };
        _ = latch.Apply(true, first, T0);

        var more = new[] { Row(10, 1), Row(60, 2) };
        var held = latch.Apply(true, more, T0.AddSeconds(5));
        Assert.Same(first, held);
    }

    [Fact]
    public void OcrTextJitterDoesNotReLatch()
    {
        // The session is bounded by the panel being present, not by its text, precisely so that a
        // wobbling OCR read cannot start a new session and re-read the panel.
        var latch = NewLatch();
        var first = new[] { Row(10, 1), Row(60, 2) };
        _ = latch.Apply(true, first, T0);

        var mangled = new[] { Row(10, 9) };
        Assert.Same(first, latch.Apply(true, mangled, T0.AddSeconds(4)));
        Assert.Same(first, latch.Apply(true, mangled, T0.AddSeconds(9)));
    }

    [Fact]
    public void ClosingThePanelDropsEverything()
    {
        var latch = NewLatch();
        _ = latch.Apply(true, [Row(10, 1)], T0);

        Assert.Null(latch.Apply(false, null, T0.AddSeconds(2)));

        // Reopening starts a fresh session and takes the new read.
        var next = new[] { Row(10, 3), Row(60, 4) };
        Assert.Same(next, latch.Apply(true, next, T0.AddSeconds(3)));
    }

    [Fact]
    public void APanelOpeningBeforeItsIconsDrawStillLatchesWhenTheyArrive()
    {
        var latch = NewLatch();
        Assert.Null(latch.Apply(true, null, T0));

        // Even well past the settling window: nothing latched is not the same as frozen.
        var found = new[] { Row(10, 1) };
        Assert.Same(found, latch.Apply(true, found, T0.AddSeconds(30)));
    }
}

/// <summary>
/// Geometry of the magazine column. It sits against the game window's left edge and is clicked
/// on, so the mapping from a point to a row and the scroll limits are worth pinning.
/// </summary>
public class RuneMagazinePainterTests
{
    private const int StripHeight = 1000;

    [Fact]
    public void RowsAreEvenlySpacedBelowTheResetButton()
    {
        var first = RuneMagazinePainter.IconAt(0, scroll: 0);
        var second = RuneMagazinePainter.IconAt(1, scroll: 0);

        Assert.Equal(RuneMagazinePainter.ContentTop, first.Top);
        Assert.Equal(first.Left, second.Left);
        Assert.Equal(RuneMagazinePainter.IconSize + RuneMagazinePainter.RowGap, second.Top - first.Top);
        Assert.False(first.IntersectsWith(RuneMagazinePainter.ResetButton));
    }

    [Fact]
    public void ScrollingMovesRowsUp()
    {
        Assert.Equal(
            RuneMagazinePainter.IconAt(0, scroll: 0).Top - 30,
            RuneMagazinePainter.IconAt(0, scroll: 30).Top);
    }

    [Fact]
    public void PointsMapBackToTheRowTheyAreOver()
    {
        var second = RuneMagazinePainter.IconAt(1, scroll: 0);
        var centre = new Point(second.Left + (second.Width / 2), second.Top + (second.Height / 2));

        Assert.Equal(1, RuneMagazinePainter.RowAt(centre, count: 5, scroll: 0, StripHeight));

        // The gap between two icons belongs to neither.
        var gap = new Point(centre.X, second.Top - 3);
        Assert.Equal(-1, RuneMagazinePainter.RowAt(gap, count: 5, scroll: 0, StripHeight));
    }

    [Fact]
    public void TheResetButtonIsNeverMistakenForARow()
    {
        var reset = RuneMagazinePainter.ResetButton;
        var centre = new Point(reset.Left + (reset.Width / 2), reset.Top + (reset.Height / 2));
        Assert.Equal(-1, RuneMagazinePainter.RowAt(centre, count: 5, scroll: 0, StripHeight));
    }

    [Fact]
    public void RowsScrolledOutOfTheViewportAreNotClickable()
    {
        // A row dragged above the viewport is still a rectangle; without the viewport check a
        // click on the reset button's row could land on it.
        var point = new Point(RuneMagazinePainter.SidePad + 5, RuneMagazinePainter.ContentTop - 2);
        Assert.Equal(-1, RuneMagazinePainter.RowAt(point, count: 20, scroll: 200, StripHeight));
    }

    [Theory]
    [InlineData(1440)]
    [InlineData(1080)]
    [InlineData(720)]
    public void ScrollStopsAtTheEndOfTheList(int windowHeight)
    {
        var viewport = RuneMagazinePainter.ViewportHeight(windowHeight);

        // A list that fits needs no scrolling at all.
        Assert.Equal(0, RuneMagazinePainter.MaxScroll(1, windowHeight));

        var many = 40;
        var max = RuneMagazinePainter.MaxScroll(many, windowHeight);
        Assert.True(max > 0);
        Assert.Equal(RuneMagazinePainter.ContentHeight(many) - viewport, max);

        // At full scroll the last icon's bottom sits exactly at the viewport's bottom.
        var last = RuneMagazinePainter.IconAt(many - 1, max);
        Assert.Equal(RuneMagazinePainter.ContentTop + viewport, last.Bottom);
    }

    [Fact]
    public void TheLabelGutterIsOutsideThePaintedStrip()
    {
        // Everything past StripWidth stays chroma-keyed so clicks pass through to the game.
        Assert.True(RuneMagazinePainter.WindowWidth > RuneMagazinePainter.StripWidth);
        Assert.Equal(RuneMagazinePainter.StripWidth + RuneMagazinePainter.LabelWidth, RuneMagazinePainter.WindowWidth);
        Assert.True(RuneMagazinePainter.ResetButton.Right <= RuneMagazinePainter.StripWidth);
        Assert.True(RuneMagazinePainter.IconAt(0, 0).Right <= RuneMagazinePainter.StripWidth);
    }

    [Fact]
    public void AStripTooShortForAnythingHasNoViewport()
    {
        Assert.Equal(0, RuneMagazinePainter.ViewportHeight(10));
        Assert.Equal(-1, RuneMagazinePainter.RowAt(new Point(10, 10), count: 3, scroll: 0, stripHeight: 10));
    }
}
