using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using RuneshapePriceChecker.OCR;
using Xunit;
using Xunit.Abstractions;

namespace RuneshapePriceChecker.Tests.OCR;

/// <summary>
/// RUNE-12 regression: rows with only two icons used to be placed about half a cell too low, so
/// their gilded rune was missed or a plain cell was called gilded instead. The fixture is the
/// user's own capture of the panel that showed it.
///
/// Ground truth, confirmed by eye at 4x on <c>2 Raw.png</c>: five rows of 5, 5, 2, 2 and 2 icons,
/// with the gilded (gold-framed, three-tabbed) rune first in <b>every</b> row. Row 3 is hovered,
/// so it also exercises the gold-wash path from RUNE-4.
/// </summary>
public class RuneNarrowRowFixtureTests
{
    private readonly ITestOutputHelper _output;
    public RuneNarrowRowFixtureTests(ITestOutputHelper output) => _output = output;

    private static readonly int[] IconCounts = [5, 5, 2, 2, 2];
    private const int GildedIndex = 0;

    /// <summary>Gilded cells measured 0.28-0.47 and every plain cell 0.02 or less after the fix.</summary>
    private const double MinGildedRing = 0.20;
    private const double MaxPlainRing = 0.08;

    private static string? FixturePath()
    {
        var candidate = Path.Combine(AppContext.BaseDirectory, "fixtures/runeicons/2560x1440/2 Raw.png");
        return File.Exists(candidate) ? candidate : null;
    }

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
            pixelBytes = new byte[stride * preprocessed.Height];
            Marshal.Copy(srcData.Scan0, pixelBytes, 0, pixelBytes.Length);
        }
        finally
        {
            preprocessed.UnlockBits(srcData);
        }

        var crop = OcrImagePreprocessor.FindContentBounds(pixelBytes, preprocessed.Width, preprocessed.Height, stride);
        var textColX = (int)(preprocessed.Width * options.PanelLeftFraction);
        if (crop.HasValue)
        {
            var newX = Math.Max(crop.Value.X, textColX);
            crop = new Rectangle(newX, crop.Value.Y, Math.Max(1, crop.Value.Right - newX), crop.Value.Height);
        }

        return OcrPipeline.DetectRowPositions(pixelBytes, preprocessed.Width, preprocessed.Height, stride, crop);
    }

    [Fact]
    public void EveryRowFindsItsGildedRune_IncludingTheTwoIconRows()
    {
        var path = FixturePath();
        if (path is null) { _output.WriteLine("fixture absent — skipped"); return; }

        using var raw = new Bitmap(path);
        var options = new OcrOptions();
        var (rowYs, rowHeights) = DetectTextRows(raw, options);
        var rgb = RuneIconFingerprinter.CopyPixels(raw, out var stride);

        Assert.Equal(IconCounts.Length, rowYs.Length);

        var gildedRings = new List<double>();
        var plainRings = new List<double>();
        var failures = new List<string>();

        for (var i = 0; i < rowYs.Length; i++)
        {
            var searchTop = i == 0 ? 0 : rowYs[i - 1] + rowHeights[i - 1];
            var searchBottom = i == rowYs.Length - 1 ? raw.Height : rowYs[i + 1];
            var cells = RuneIconFingerprinter.DetectCells(rgb, raw.Width, raw.Height, stride, searchTop, searchBottom, rowYs[i], rowHeights[i]);

            _output.WriteLine($"row {i}: {cells.Count} cell(s), expected {IconCounts[i]}");
            if (cells.Count != IconCounts[i])
                failures.Add($"row {i}: {cells.Count} cells, expected {IconCounts[i]}");

            for (var j = 0; j < cells.Count; j++)
            {
                var cell = cells[j];
                var state = cell.IsGilded ? "GILDED" : cell.Ambiguous ? "ambig" : "plain";
                _output.WriteLine($"        [{j}] {state} {cell.Bounds.Width}x{cell.Bounds.Height} ring={cell.GoldHueRingProportion:F3}");

                if (j == GildedIndex)
                {
                    if (!cell.IsGilded) failures.Add($"row {i} cell {j}: expected gilded, was {state}");
                    gildedRings.Add(cell.GoldHueRingProportion);
                }
                else
                {
                    if (cell.IsGilded) failures.Add($"row {i} cell {j}: expected plain, was gilded");
                    plainRings.Add(cell.GoldHueRingProportion);
                }
            }

            // The row's icon extent must be a plausible cell height, not the half-height band the
            // old median-of-ink-runs placement settled on (it returned 29-41 where 45 was right).
            var height = cells.Count > 0 ? cells.Max(c => c.Bounds.Height) : 0;
            if (cells.Count > 0 && height is < 38 or > 60)
                failures.Add($"row {i}: icon height {height} is not a plausible cell");
        }

        _output.WriteLine($"gilded rings: {string.Join(", ", gildedRings.Select(r => r.ToString("F3")))}");
        _output.WriteLine($"plain rings max: {(plainRings.Count > 0 ? plainRings.Max() : 0):F3}");

        Assert.Empty(failures);
        Assert.Equal(IconCounts.Length, gildedRings.Count);
        Assert.True(gildedRings.Min() >= MinGildedRing, $"gilded ring min {gildedRings.Min():F2} is under {MinGildedRing}");
        Assert.True(plainRings.Max() <= MaxPlainRing, $"plain ring max {plainRings.Max():F2} is over {MaxPlainRing}");

        // Zero overlap is the property the classifier actually rests on.
        Assert.True(gildedRings.Min() > plainRings.Max(), "gilded and plain gold rings overlap");
    }
}
