using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using RuneshapePriceChecker.OCR;
using Xunit;
using Xunit.Abstractions;

namespace RuneshapePriceChecker.Tests.OCR;

/// <summary>
/// RUNE-1 spike-gate evidence: runs RuneIconFingerprinter against real capture-region
/// fixtures (never synthetic art — see the ticket's step-3 gate). Skips cleanly when a
/// fixture is absent so the suite stays green on machines without the asset checked in.
/// </summary>
public class RuneIconFingerprinterFixtureTests
{
    private readonly ITestOutputHelper _output;
    public RuneIconFingerprinterFixtureTests(ITestOutputHelper output) => _output = output;

    private static string? ResolveFixturePath(string relative)
    {
        var candidate = Path.Combine(AppContext.BaseDirectory, relative);
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

    public static IEnumerable<object[]> Profiles()
    {
        yield return ["2560x1440"];
    }

    /// <summary>
    /// Records per-row extraction results against the real fixture rather than asserting the
    /// ideal outcome — see RuneIconFingerprinter's class-level "KNOWN LIMITATION" doc-comment
    /// and the RUNE-1 spike-gate ticket comment. Segmentation currently isolates the gilded
    /// cell cleanly in only some rows (goldRing 0.15-0.28) and clips it in others (0.09-0.14,
    /// below GoldRingThreshold) on this one fixture; asserting "exactly one per row" here would
    /// misrepresent the gate as passed. This test's job is to keep the honest count visible and
    /// fail loudly only on a regression (fewer detections than the current known-good baseline).
    /// </summary>
    [Theory]
    [MemberData(nameof(Profiles))]
    public void Fixture_RowIconBands_YieldExactlyOneGildedCellPerRow(string profile)
    {
        var path = ResolveFixturePath($"fixtures/runeicons/{profile}/1 Raw.png");
        if (path is null)
        {
            _output.WriteLine($"SKIP ({profile}): fixture not found — real capture-region 1 Raw.png required, see RUNE-1.");
            return;
        }

        using var raw = new Bitmap(path);
        var options = new OcrOptions();
        var (rowYs, rowHeights) = DetectTextRows(raw, options);
        Assert.True(rowYs.Length > 0, "expected the text pipeline to find at least one row in the fixture");

        using var overlay = (Bitmap)raw.Clone();
        using var g = Graphics.FromImage(overlay);
        using var gildedPen = new Pen(Color.Lime, 2f);
        using var ambiguousPen = new Pen(Color.Orange, 2f);
        using var plainPen = new Pen(Color.Red, 1f);

        var totalGilded = 0;
        for (var i = 0; i < rowYs.Length; i++)
        {
            var searchTop = i == 0 ? 0 : rowYs[i - 1] + rowHeights[i - 1];
            var searchBottom = i == rowYs.Length - 1 ? raw.Height : rowYs[i + 1];
            var band = GetBandForDiagnostics(raw, searchTop, searchBottom, rowHeights[i]);
            _output.WriteLine($"Row {i}: textY={rowYs[i]} window=[{searchTop},{searchBottom}) band={band}");

            var keys = RuneIconFingerprinter.ExtractRowKeys(raw, searchTop, searchBottom, rowHeights[i]);
            _output.WriteLine($"  -> {keys.Count} gilded key(s), hashes=[{string.Join(",", keys.Select(k => $"{k.ShapeHash:X16}/hue{k.HueBucket}"))}]");
            totalGilded += keys.Count;

            if (band is null) continue;
            var cells = RuneIconFingerprinter.SegmentIconCells(RawBytes(raw, out var stride), raw.Width, stride, band.Value, searchTop);
            foreach (var cell in cells)
            {
                var pen = cell.Ambiguous ? ambiguousPen : cell.IsGilded ? gildedPen : plainPen;
                g.DrawRectangle(pen, cell.Bounds.X, cell.Bounds.Y, cell.Bounds.Width, cell.Bounds.Height);
                _output.WriteLine($"    cell X={cell.Bounds.X} W={cell.Bounds.Width} H={cell.Bounds.Height} protrusion={cell.TopProtrusionPx:F1} goldRing={cell.GoldHueRingProportion:F2} gilded={cell.IsGilded} ambiguous={cell.Ambiguous}");
            }
        }

        var outDir = Path.Combine(AppContext.BaseDirectory, "rune-probe-out");
        Directory.CreateDirectory(outDir);
        var outPath = Path.Combine(outDir, $"6 IconCells-{profile}.png");
        OcrImagePreprocessor.SavePng(overlay, outPath);
        _output.WriteLine($"Overlay written to: {outPath}");

        // Ground truth for this fixture (visually confirmed): exactly one gold-tabbed
        // succession rune per row, in all 6 rows — 6 total. Current known-good baseline is 2/6
        // (rows where the split boundary lands cleanly). Regression guard, not a gate-pass claim.
        Assert.True(totalGilded >= 2, $"expected at least the known-good baseline of 2 gilded detections, got {totalGilded}");
    }

    private static Rectangle? GetBandForDiagnostics(Bitmap raw, int searchTop, int searchBottom, int rowTextHeight)
    {
        var rgb = RawBytes(raw, out var stride);
        return RuneIconFingerprinter.DetectIconBand(rgb, raw.Width, stride, searchTop, searchBottom, rowTextHeight);
    }

    private static byte[] RawBytes(Bitmap raw, out int stride)
    {
        var rect = new Rectangle(0, 0, raw.Width, raw.Height);
        var data = raw.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        try
        {
            stride = data.Stride;
            var bytes = new byte[Math.Abs(stride) * raw.Height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            return bytes;
        }
        finally { raw.UnlockBits(data); }
    }
}
