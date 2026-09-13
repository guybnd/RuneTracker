using RuneshapePriceChecker.Contracts;
using RuneshapePriceChecker.Overlay;
using Xunit;

namespace RuneshapePriceChecker.Tests.Runes;

/// <summary>
/// The panel is read once and held while it stays open. Hovering a row makes the game tint it
/// gold, which collapses the contrast the gilded test needs — so pointing at a rune to press the
/// hotkey is what used to lose it. These pin the latch's rules.
/// </summary>
public class RuneLatchTests
{
    private static RuneRowKeys Row(int y, params ulong[] hashes) =>
        new(y, hashes.Select(h => new RuneKey(h, 9, [], new Rectangle(10, y, 40, 40))).ToList());

    // Mirrors LeaguePricingWorker.LatchRuneRows. Kept here as an executable statement of the rule;
    // the worker itself cannot be constructed in a test without the whole OCR stack.
    private sealed class Latch
    {
        private string _key = "";
        private IReadOnlyList<RuneRowKeys>? _rows;

        public IReadOnlyList<RuneRowKeys>? Apply(IReadOnlyList<string> itemNames, IReadOnlyList<RuneRowKeys>? fresh)
        {
            var key = string.Join("", itemNames);
            if (string.IsNullOrEmpty(key)) { _key = ""; _rows = null; return fresh; }

            var freshCount = fresh?.Sum(r => r.Keys.Count) ?? 0;
            if (key != _key)
            {
                _key = key;
                _rows = freshCount > 0 ? fresh : null;
                return _rows ?? fresh;
            }

            var latchedCount = _rows?.Sum(r => r.Keys.Count) ?? 0;
            if (freshCount > latchedCount) { _rows = fresh; return fresh; }
            return _rows ?? fresh;
        }
    }

    private static readonly string[] PanelA = ["Rune of Cruelty", "Rune of Winter"];
    private static readonly string[] PanelB = ["Lesser Stone Rune"];

    [Fact]
    public void AHoveredRowDoesNotLoseItsRunes()
    {
        // The hover case: same panel text, but the gilded cell in the hovered row stops being
        // classified. Without the latch that rune vanishes from under the cursor.
        var latch = new Latch();
        var full = new[] { Row(10, 1), Row(60, 2) };

        Assert.Same(full, latch.Apply(PanelA, full));

        var hovered = new[] { Row(10, 1) };   // second row washed out
        var held = latch.Apply(PanelA, hovered);
        Assert.Equal(2, held!.Sum(r => r.Keys.Count));
    }

    [Fact]
    public void ABetterReadOfTheSamePanelReplacesTheLatch()
    {
        // A first read taken while a row happened to be hovered must repair itself rather than
        // stick for as long as the panel stays open.
        var latch = new Latch();
        var partial = new[] { Row(10, 1) };
        _ = latch.Apply(PanelA, partial);

        var full = new[] { Row(10, 1), Row(60, 2) };
        Assert.Same(full, latch.Apply(PanelA, full));
    }

    [Fact]
    public void ADifferentPanelClearsTheLatch()
    {
        var latch = new Latch();
        _ = latch.Apply(PanelA, [Row(10, 1), Row(60, 2)]);

        var next = new[] { Row(10, 3) };
        var held = latch.Apply(PanelB, next);
        Assert.Same(next, held);
        Assert.Equal(1, held!.Sum(r => r.Keys.Count));
    }

    [Fact]
    public void ClosingThePanelDropsTheLatch()
    {
        // No row text means no panel. Holding runes from a closed panel would let the hotkey mark
        // something that is no longer on screen.
        var latch = new Latch();
        _ = latch.Apply(PanelA, [Row(10, 1)]);

        Assert.Null(latch.Apply([], null));
        Assert.Null(latch.Apply([], null));
    }

    [Fact]
    public void APanelWithNoRunesLatchesNothingAndKeepsLooking()
    {
        var latch = new Latch();
        Assert.Null(latch.Apply(PanelA, null));

        var found = new[] { Row(10, 1) };
        Assert.Same(found, latch.Apply(PanelA, found));
    }
}

/// <summary>
/// Layout of the magazine column. It sits against the game window's left edge, so the arithmetic
/// that keeps it inside the window is worth pinning — an overflowing strip draws off-screen.
/// </summary>
public class RuneMagazinePainterTests
{
    [Fact]
    public void HeightGrowsByARowAndHasNoTrailingGap()
    {
        var one = RuneMagazinePainter.HeightFor(1);
        var two = RuneMagazinePainter.HeightFor(2);
        Assert.Equal(RuneMagazinePainter.IconSize + RuneMagazinePainter.RowGap, two - one);

        // The last row must not leave a gap below it, or the panel looks bottom-heavy.
        Assert.Equal(RuneMagazinePainter.PadY + RuneMagazinePainter.HeaderHeight + RuneMagazinePainter.IconSize + RuneMagazinePainter.PadY, one);
    }

    [Fact]
    public void AnEmptyMagazineIsNotTallerThanItsChrome()
    {
        Assert.True(RuneMagazinePainter.HeightFor(0) <= RuneMagazinePainter.HeightFor(1));
    }

    [Fact]
    public void RowsAreEvenlySpacedFromTheTop()
    {
        var first = RuneMagazinePainter.IconOrigin(0);
        var second = RuneMagazinePainter.IconOrigin(1);
        Assert.Equal(first.X, second.X);
        Assert.Equal(RuneMagazinePainter.IconSize + RuneMagazinePainter.RowGap, second.Y - first.Y);
    }

    [Theory]
    [InlineData(1440)]
    [InlineData(1080)]
    [InlineData(720)]
    public void WhatFitsActuallyFits(int windowHeight)
    {
        var rows = RuneMagazinePainter.MaxRows(windowHeight);
        Assert.True(rows > 0);
        Assert.True(RuneMagazinePainter.HeightFor(rows) <= windowHeight,
            $"{rows} rows is {RuneMagazinePainter.HeightFor(rows)}px, over a {windowHeight}px window");
        Assert.True(RuneMagazinePainter.HeightFor(rows + 1) > windowHeight, "one more row should not have fitted");
    }

    [Fact]
    public void AWindowTooShortForEvenOneRowFitsNone()
    {
        Assert.Equal(0, RuneMagazinePainter.MaxRows(20));
    }
}
