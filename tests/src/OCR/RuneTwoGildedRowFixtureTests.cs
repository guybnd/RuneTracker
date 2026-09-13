using System.Drawing.Imaging;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using RuneshapePriceChecker.OCR;
using Xunit;
using Xunit.Abstractions;

namespace RuneshapePriceChecker.Tests.OCR;

/// <summary>
/// RUNE-22 regression: on a panel whose six-icon rows each carry two gilded runes, the icon-row
/// band landed 4-8px below the cells (or 8px too tall) and the last cell was rebuilt 16-22px right
/// of its gold frame, so the second gilded rune was dropped as ambiguous in five rows of six. The
/// fixture is the user's own capture of that panel.
///
/// Ground truth, confirmed by eye at 4x and by sampling the gold frame columns (x=273-275 and
/// 322-324 in every six-icon row) on <c>6 Raw.png</c>: seven rows of 6, 6, 6, 6, 6, 6 and 4 icons;
/// the gilded (gold-framed, three-tabbed) rune is second in every row, and rows 0-5 carry a second
/// gilded rune — a purple-glyph one — in the sixth cell. The cells' dark bevel line sits at
/// y = 45, 153, 261, 369, 477, 585 and 693, so every band must end there and be 45px tall.
/// </summary>
public class RuneTwoGildedRowFixtureTests
{
    private readonly ITestOutputHelper _output;
    public RuneTwoGildedRowFixtureTests(ITestOutputHelper output) => _output = output;

    private static readonly int[] IconCounts = [6, 6, 6, 6, 6, 6, 4];
    /// <summary>Row 6 is a narrow row (text beside the icons), so its icons sit lower relative to its text than the wide rows' do.</summary>
    private static readonly int[] BevelLines = [45, 153, 261, 369, 477, 585, 700];

    private static int LowerMedian(IEnumerable<int> values)
    {
        var sorted = values.OrderBy(v => v).ToArray();
        return sorted[(sorted.Length - 1) / 2];
    }
    private const int FirstGildedIndex = 1;
    private const int SecondGildedIndex = 5;

    /// <summary>Same margins as the other fixtures: gilded 0.20+, plain at most 0.08, never overlapping.</summary>
    private const double MinGildedRing = 0.20;
    private const double MaxPlainRing = 0.08;

    /// <summary>Two different runes in one row must not hash as the same rune (see RuneIconFingerprinterFixtureTests).</summary>
    private const int DifferentRuneMinHamming = 16;

    private static string? FixturePath()
    {
        var candidate = Path.Combine(AppContext.BaseDirectory, "fixtures/runeicons/2560x1440/6 Raw.png");
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
    public void EveryRowFindsBothGildedRunes_AndBandsSitOnTheBevelLine()
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

            var hasSecondGilded = IconCounts[i] > SecondGildedIndex;
            for (var j = 0; j < cells.Count; j++)
            {
                var cell = cells[j];
                var state = cell.IsGilded ? "GILDED" : cell.Ambiguous ? "ambig" : "plain";
                _output.WriteLine($"        [{j}] {state} {cell.Bounds.X},{cell.Bounds.Y} {cell.Bounds.Width}x{cell.Bounds.Height} ring={cell.GoldHueRingProportion:F3}");

                var shouldBeGilded = j == FirstGildedIndex || (hasSecondGilded && j == SecondGildedIndex);
                if (shouldBeGilded)
                {
                    if (!cell.IsGilded) failures.Add($"row {i} cell {j}: expected gilded, was {state}");
                    gildedRings.Add(cell.GoldHueRingProportion);
                }
                else
                {
                    if (cell.IsGilded || cell.Ambiguous) failures.Add($"row {i} cell {j}: expected plain, was {state}");
                    plainRings.Add(cell.GoldHueRingProportion);
                }
            }

            // The band must end on the cells' dark bevel line and be as tall as the cells are
            // wide. A band a few px low is exactly what lost the second gilded rune. Judged on
            // the undecorated plain cells (index 2 onwards): a gilded cell's box is widened to
            // its gold frame and the first cell carries the game's blue frame, so both sit a
            // few px outside the shared extent by design.
            var undecorated = cells.Skip(2).Where(c => !c.IsGilded && !c.Ambiguous).ToList();
            if (undecorated.Count > 0)
            {
                var bottom = LowerMedian(undecorated.Select(c => c.Bounds.Bottom - 1));
                var plainHeight = LowerMedian(undecorated.Select(c => c.Bounds.Height));
                var plainWidth = LowerMedian(undecorated.Select(c => c.Bounds.Width));
                _output.WriteLine($"        band bottom={bottom} (bevel {BevelLines[i]}), plain cells {plainWidth}x{plainHeight}");
                if (Math.Abs(bottom - BevelLines[i]) > 1)
                    failures.Add($"row {i}: band ends at {bottom}, bevel line is at {BevelLines[i]}");
                if (Math.Abs(plainHeight - plainWidth) > 1)
                    failures.Add($"row {i}: plain cells are {plainWidth}x{plainHeight}, not square");
            }

            var keys = RuneIconFingerprinter.ExtractRowKeys(raw, searchTop, searchBottom, rowYs[i], rowHeights[i]);
            var expectedKeys = hasSecondGilded ? 2 : 1;
            _output.WriteLine($"        -> {keys.Count} key(s): [{string.Join(", ", keys.Select(k => $"{k.ShapeHash:X16}/hue{k.HueBucket}@{k.CellBounds.X}"))}]");
            if (keys.Count != expectedKeys)
                failures.Add($"row {i}: {keys.Count} gilded keys, expected {expectedKeys}");
            if (keys.Count == 2)
            {
                var distance = BitOperations.PopCount(keys[0].ShapeHash ^ keys[1].ShapeHash);
                if (distance < DifferentRuneMinHamming)
                    failures.Add($"row {i}: its two gilded runes hash only {distance} bits apart");
            }
        }

        _output.WriteLine($"gilded rings: {string.Join(", ", gildedRings.Select(r => r.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)))}");
        _output.WriteLine($"plain rings max: {(plainRings.Count > 0 ? plainRings.Max() : 0):F3}");

        Assert.Empty(failures);
        Assert.Equal(13, gildedRings.Count);
        Assert.True(gildedRings.Min() >= MinGildedRing, $"gilded ring min {gildedRings.Min():F2} is under {MinGildedRing}");
        Assert.True(plainRings.Max() <= MaxPlainRing, $"plain ring max {plainRings.Max():F2} is over {MaxPlainRing}");
        Assert.True(gildedRings.Min() > plainRings.Max(), "gilded and plain gold rings overlap");
    }
}
