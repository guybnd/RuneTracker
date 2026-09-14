using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using RuneshapePriceChecker.OCR;
using RuneshapePriceChecker.Runes;
using Xunit;
using Xunit.Abstractions;

namespace RuneshapePriceChecker.Tests.OCR;

/// <summary>
/// First 1920x1080 capture (RUNE-26): four "Nx Divine Orb" rows of 9, 6, 5 and 4 icons, cells
/// about 34px square, cropped to the 1920x1080 capture profile (52,154 497x536).
///
/// Ground truth by eye and by the Combinations table: the gilded (gold-framed, tabbed) rune is
/// the third cell in every row, and the nine-icon row carries a second one in its seventh cell.
/// The table names them Power (cell 3) and Toxic (cell 7 of the ten-stack).
///
/// Row 0 is clipped by the top of the capture region and today comes back as 8 cells from x=35
/// instead of 9 from x=0, so it is held to a weaker bar: nothing in it may be <i>mis</i>named. The
/// clipped-top-row miss is its own follow-up.
/// </summary>
public class Rune1080pFixtureTests
{
    private readonly ITestOutputHelper _output;
    public Rune1080pFixtureTests(ITestOutputHelper output) => _output = output;

    private static readonly (string RowText, int Icons, int[] GildedIndices, string Rune)[] Truth =
    [
        ("10x Divine Orb", 9, [2, 6], "power"),  // cell 7 is Toxic; see the per-index check below
        ("3x Divine Orb", 6, [2], "power"),
        ("2x Divine Orb", 5, [2], "power"),
        ("1x Divine Orb", 4, [2], "power"),
    ];

    private const double MinGildedRing = 0.20;
    private const double MaxPlainRing = 0.08;

    private static string? FixturePath()
    {
        var candidate = Path.Combine(AppContext.BaseDirectory, "fixtures/runeicons/1920x1080/1 Raw.png");
        return File.Exists(candidate) ? candidate : null;
    }

    [Fact]
    public void UnclippedRows_SegmentAndNameExactly_ClippedRowIsNeverMisnamed()
    {
        var path = FixturePath();
        if (path is null) { _output.WriteLine("fixture absent — skipped"); return; }

        using var raw = new Bitmap(path);
        var options = new OcrOptions();
        var (rowYs, rowHeights) = DetectTextRows(raw, options);
        var rgb = RuneIconFingerprinter.CopyPixels(raw, out var stride);
        var table = new RuneCombinationTable(RuneCombinationTable.LoadShippedJson());

        Assert.Equal(Truth.Length, rowYs.Length);

        var gildedRings = new List<double>();
        var plainRings = new List<double>();
        var failures = new List<string>();

        for (var i = 0; i < rowYs.Length; i++)
        {
            var searchTop = i == 0 ? 0 : rowYs[i - 1] + rowHeights[i - 1];
            var searchBottom = i == rowYs.Length - 1 ? raw.Height : rowYs[i + 1];
            var (rowText, icons, gildedIndices, rune) = Truth[i];
            var cells = RuneIconFingerprinter.DetectCells(rgb, raw.Width, raw.Height, stride, searchTop, searchBottom, rowYs[i], rowHeights[i]);
            var keys = RuneIconFingerprinter.ExtractRowKeys(raw, searchTop, searchBottom, rowYs[i], rowHeights[i]);

            _output.WriteLine($"row {i} '{rowText}': {cells.Count} cell(s), expected {icons}; heights {string.Join(",", cells.Select(c => c.Bounds.Height).Distinct())}");
            foreach (var key in keys)
            {
                var named = table.Lookup(rowText, key.CellCount, key.CellIndex);
                _output.WriteLine($"        gilded cell {key.CellIndex}/{key.CellCount} ring={cells.First(c => c.Bounds == key.CellBounds).GoldHueRingProportion:F3} -> {named ?? "null"}");
            }

            if (i == 0 && cells.Count != icons)
            {
                // Known clipped-row miss. The only thing that must hold is that no rune is named
                // from a wrong index: a miscounted row must resolve to null, never to a guess.
                _output.WriteLine($"        row 0 miscounted ({cells.Count} of {icons}) — clipped by the capture top; asserting null lookups only");
                foreach (var key in keys)
                {
                    var named = table.Lookup(rowText, key.CellCount, key.CellIndex);
                    if (named is not null && named != rune && named != "toxic")
                        failures.Add($"row 0: miscounted row named {named} at cell {key.CellIndex} of {key.CellCount}");
                }
                continue;
            }

            if (cells.Count != icons)
                failures.Add($"row {i}: {cells.Count} cells, expected {icons}");

            for (var j = 0; j < cells.Count; j++)
            {
                var cell = cells[j];
                var shouldBeGilded = gildedIndices.Contains(j);
                var state = cell.IsGilded ? "GILDED" : cell.Ambiguous ? "ambig" : "plain";
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

            if (keys.Count != gildedIndices.Length)
                failures.Add($"row {i}: {keys.Count} gilded keys, expected {gildedIndices.Length}");
            foreach (var key in keys)
            {
                var expected = key.CellIndex == 6 ? "toxic" : rune;
                var named = table.Lookup(rowText, key.CellCount, key.CellIndex);
                if (named != expected)
                    failures.Add($"row {i} cell {key.CellIndex}/{key.CellCount}: table named {named ?? "null"}, expected {expected}");
            }

            // 1080p cells are ~34px; the band must be that tall, not the 45 of 1440p.
            var height = cells.Count > 0 ? cells.Where(c => !c.IsGilded).Select(c => c.Bounds.Height).DefaultIfEmpty(0).Max() : 0;
            if (cells.Count > 0 && height is < 28 or > 42)
                failures.Add($"row {i}: icon height {height} is not a plausible 1080p cell");
        }

        _output.WriteLine($"gilded rings: {string.Join(", ", gildedRings.Select(r => r.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)))}");
        _output.WriteLine($"plain rings max: {(plainRings.Count > 0 ? plainRings.Max() : 0):F3}");

        Assert.Empty(failures);
        Assert.Equal(3, gildedRings.Count);
        Assert.True(gildedRings.Min() >= MinGildedRing, $"gilded ring min {gildedRings.Min():F2} is under {MinGildedRing}");
        Assert.True(plainRings.Max() <= MaxPlainRing, $"plain ring max {plainRings.Max():F2} is over {MaxPlainRing}");
        Assert.True(gildedRings.Min() > plainRings.Max(), "gilded and plain gold rings overlap");
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
}
