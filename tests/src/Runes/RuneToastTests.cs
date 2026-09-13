using RuneshapePriceChecker.App;
using RuneshapePriceChecker.Overlay;
using RuneshapePriceChecker.Runes;
using Xunit;

namespace RuneshapePriceChecker.Tests.Runes;

/// <summary>
/// The mark toast (RUNE-20) is the only immediate sign that a hotkey press did anything, so its
/// words for each outcome, and the shape of its rise-and-fade, are pinned here.
/// </summary>
public class RuneToastTests
{
    [Theory]
    [InlineData(RuneMarkResult.Marked, "Power Rune", "Power Rune saved", RuneToastKind.Saved)]
    [InlineData(RuneMarkResult.Unmarked, "Power Rune", "Power Rune dismissed", RuneToastKind.Dismissed)]
    [InlineData(RuneMarkResult.Marked, null, "Rune saved", RuneToastKind.Saved)]
    [InlineData(RuneMarkResult.NoRuneUnderCursor, null, "No rune here", RuneToastKind.Miss)]
    [InlineData(RuneMarkResult.NothingOnScreen, null, "No rune here", RuneToastKind.Miss)]
    public void PanelResultsHaveWords(RuneMarkResult result, string? name, string text, RuneToastKind kind)
    {
        var toast = RuneToast.For(result, name);
        Assert.NotNull(toast);
        Assert.Equal(text, toast!.Value.Text);
        Assert.Equal(kind, toast.Value.Kind);
    }

    [Theory]
    [InlineData(RuneTooltipMarkResult.Marked, "Death Rune", "Death Rune saved", RuneToastKind.Saved)]
    [InlineData(RuneTooltipMarkResult.Unmarked, "Death Rune", "Death Rune dismissed", RuneToastKind.Dismissed)]
    [InlineData(RuneTooltipMarkResult.NoTooltip, null, "No rune here", RuneToastKind.Miss)]
    [InlineData(RuneTooltipMarkResult.Unavailable, null, "Can't read the tooltip", RuneToastKind.Miss)]
    public void TooltipResultsHaveWords(RuneTooltipMarkResult result, string? name, string text, RuneToastKind kind)
    {
        var toast = RuneToast.For(result, name);
        Assert.NotNull(toast);
        Assert.Equal(text, toast!.Value.Text);
        Assert.Equal(kind, toast.Value.Kind);
    }

    [Fact]
    public void NothingIsShownWhenTheGameIsNotInFrontOrAReadIsRunning()
    {
        Assert.Null(RuneToast.For(RuneTooltipMarkResult.NoGameWindow, null));
        Assert.Null(RuneToast.For(RuneTooltipMarkResult.Busy, null));
    }

    [Fact]
    public void FrameStartsStillAndOpaqueAndEndsRisenAndGone()
    {
        var (rise0, opacity0) = RuneToast.Frame(0);
        Assert.Equal(0f, rise0, 3);
        Assert.Equal(1f, opacity0, 3);

        var (rise1, opacity1) = RuneToast.Frame(1);
        Assert.Equal(RuneToast.RisePixels, rise1, 3);
        Assert.Equal(0f, opacity1, 3);
    }

    [Fact]
    public void FrameHoldsFullOpacityFirstThenFadesMonotonically()
    {
        var (_, heldOpacity) = RuneToast.Frame(RuneToast.HoldFraction * 0.9);
        Assert.Equal(1f, heldOpacity, 3);

        var lastRise = -1f;
        var lastOpacity = 2f;
        for (var i = 0; i <= 20; i++)
        {
            var (rise, opacity) = RuneToast.Frame(i / 20.0);
            Assert.True(rise >= lastRise, $"rise fell at step {i}");
            Assert.True(opacity <= lastOpacity, $"opacity rose at step {i}");
            lastRise = rise;
            lastOpacity = opacity;
        }
    }

    [Fact]
    public void FrameClampsOutOfRangeProgress()
    {
        Assert.Equal(RuneToast.Frame(0), RuneToast.Frame(-0.5));
        Assert.Equal(RuneToast.Frame(1), RuneToast.Frame(7));
    }
}
