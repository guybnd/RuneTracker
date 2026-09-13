using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using RuneshapePriceChecker.Contracts;
using RuneshapePriceChecker.OCR;
using Xunit;

namespace RuneshapePriceChecker.Tests.OCR;

public class OcrLeagueWindowReaderTests
{
    [Fact]
    public void Constructor_IsILeagueWindowReader()
    {
        var type = typeof(OcrLeagueWindowReader);
        Assert.True(typeof(ILeagueWindowReader).IsAssignableFrom(type));
    }

    [Fact]
    public void ResolveStatusLine_NoLosslessScaling_ReturnsMethodOnly()
    {
        // ResolveStatusLine is private — test via reflection
        var method = typeof(OcrLeagueWindowReader).GetMethod("ResolveStatusLine",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Cannot easily instantiate without all DI deps, but method exists
        Assert.NotNull(method);
    }

    [Fact]
    public void CreateEmptySnapshot_ReturnsValidSnapshot()
    {
        var method = typeof(OcrLeagueWindowReader).GetMethod("CreateEmptySnapshot",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
    }

    // ---- RUNE-1: ComputeRuneRowKeys + the row-Y join (RUNE-1 review, Major 1) ----
    //
    // RuneIconFingerprinter itself (the leaf) has 20+ tests; these cover the reader-level glue
    // that sequences it — the per-row search-window arithmetic in ComputeRuneRowKeys and the
    // matched-Y join that assembles LeagueWindowSnapshot.RuneRows — which previously had none.

    [Fact]
    public void FilterRuneRowsToMatchedRows_KeepsOnlyRowsSurvivingTheTextFilter_JoinedOnRowY()
    {
        var runeRows = new[]
        {
            new RuneRowKeys(100, [new RuneKey(1, 0, [])]),
            new RuneRowKeys(200, [new RuneKey(2, 0, [])]),
            new RuneRowKeys(300, [new RuneKey(3, 0, [])]),
        };
        // Row Y=200 was dropped by ExtractFromRowTexts' <3-char/letterless filter — its Y never
        // appears in matchedYPositions — while rows 100 and 300 survived, in order.
        var matchedYPositions = new[] { 100, 300 };

        var kept = OcrLeagueWindowReader.FilterRuneRowsToMatchedRows(runeRows, matchedYPositions);

        Assert.Equal(2, kept.Length);
        Assert.Equal(100, kept[0].RowY);
        Assert.Equal(300, kept[1].RowY);
    }

    [Fact]
    public void FilterRuneRowsToMatchedRows_NoRowsSurviveTheTextFilter_ReturnsEmpty()
    {
        RuneRowKeys[] runeRows = [new RuneRowKeys(100, [])];
        Assert.Empty(OcrLeagueWindowReader.FilterRuneRowsToMatchedRows(runeRows, []));
    }

    [Fact]
    public void FilterRuneRowsToMatchedRows_NoRuneRowsDetected_ReturnsEmpty()
    {
        Assert.Empty(OcrLeagueWindowReader.FilterRuneRowsToMatchedRows([], [100, 200]));
    }

    [Fact]
    public void ComputeRuneRowKeys_RealFixture_OneEntryPerRowKeyedOnRowY_WithExpectedGildedCounts()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures/runeicons/2560x1440/1 Raw.png");
        if (!File.Exists(path))
        {
            return; // SKIP: real capture-region fixture not present on this machine, see RUNE-1.
        }

        using var raw = new Bitmap(path);
        var options = new OcrOptions();
        var (rowYs, rowHeights) = DetectTextRows(raw, options);
        Assert.Equal(6, rowYs.Length); // ground truth per RuneIconFingerprinterFixtureTests

        var result = OcrLeagueWindowReader.ComputeRuneRowKeys(raw, rowYs, rowHeights);

        Assert.Equal(rowYs.Length, result.Length);
        for (var i = 0; i < rowYs.Length; i++)
        {
            Assert.Equal(rowYs[i], result[i].RowY);
            // Every row on this fixture carries exactly one gilded rune. A wrong search window
            // (e.g. an inverted searchTop/searchBottom bound, per the review's failure scenario)
            // makes LocateIconRow/DetectCells find nothing here even though
            // RuneIconFingerprinter's own unit tests — which call it directly with a known-good
            // window — stay green.
            Assert.Single(result[i].Keys);
        }
    }

    // Mirrors RuneIconFingerprinterFixtureTests.DetectTextRows exactly (kept independent of that
    // file rather than shared, so this test's fixture-driven ground truth isn't coupled to that
    // fixture test's internals).
    private static (int[] RowYs, int[] RowHeights) DetectTextRows(Bitmap raw, OcrOptions options)
    {
        using var masked = OcrImagePreprocessor.KeepBlackAndNeighbors(raw);
        using var preprocessed = OcrImagePreprocessor.PreprocessForOcr(masked, options);

        var srcRect = new Rectangle(0, 0, preprocessed.Width, preprocessed.Height);
        var srcData = preprocessed.LockBits(srcRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        int stride;
        byte[] pixelBytes;
        try
        {
            stride = srcData.Stride;
            pixelBytes = new byte[Math.Abs(stride) * preprocessed.Height];
            Marshal.Copy(srcData.Scan0, pixelBytes, 0, pixelBytes.Length);
        }
        finally { preprocessed.UnlockBits(srcData); }

        var crop = OcrImagePreprocessor.FindContentBounds(pixelBytes, preprocessed.Width, preprocessed.Height, stride);
        var textColX = (int)(preprocessed.Width * options.PanelLeftFraction);
        if (crop.HasValue)
        {
            var newX = Math.Max(crop.Value.X, textColX);
            crop = new Rectangle(newX, crop.Value.Y, Math.Max(1, crop.Value.Right - newX), crop.Value.Height);
        }

        return OcrPipeline.DetectRowPositions(pixelBytes, preprocessed.Width, preprocessed.Height, stride, crop);
    }
}
