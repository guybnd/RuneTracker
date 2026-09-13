using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using RuneshapePriceChecker.OCR;
using RuneshapePriceChecker.Contracts;
using Xunit;
using Xunit.Abstractions;

namespace RuneshapePriceChecker.Tests.OCR;

/// <summary>
/// Prints the per-row geometry the extractor derives, for comparing against what is visibly on
/// the capture. Written for RUNE-12: on <c>2 Raw.png</c> the two-icon rows' cell boxes sit about
/// half a cell too low and the clipped first row's sit right and low, while the one wide
/// unclipped row is exact.
/// </summary>
public class RuneRowGeometryDiagnostics
{
    private readonly ITestOutputHelper _output;
    public RuneRowGeometryDiagnostics(ITestOutputHelper output) => _output = output;

    private static string? FixturePath(string name)
    {
        var candidate = Path.Combine(AppContext.BaseDirectory, "fixtures/runeicons/2560x1440", name);
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

    [Theory]
    [InlineData("1 Raw.png")]
    [InlineData("2 Raw.png")]
    public void DumpGlyphColour(string fixture)
    {
        // How many bright saturated pixels does each gilded glyph actually have, and in which
        // hue bucket? The fixture says the same rune reads gold in one row and colourless in
        // another, so the count must be sitting on the threshold.
        var path = FixturePath(fixture);
        if (path is null) { _output.WriteLine($"{fixture}: absent — skipped"); return; }

        using var raw = new Bitmap(path);
        var options = new OcrOptions();
        var (rowYs, rowHeights) = DetectTextRows(raw, options);

        _output.WriteLine($"{fixture}: bucket counts per gilded glyph (s>={RuneIconFingerprinter.ColourSaturationMin}, v>={RuneIconFingerprinter.ColourValueMin})");

        for (var i = 0; i < rowYs.Length; i++)
        {
            var searchTop = i == 0 ? 0 : rowYs[i - 1] + rowHeights[i - 1];
            var searchBottom = i == rowYs.Length - 1 ? raw.Height : rowYs[i + 1];
            foreach (var key in RuneIconFingerprinter.ExtractRowKeys(raw, searchTop, searchBottom, rowYs[i], rowHeights[i]))
            {
                var counts = new int[12];
                var sprite = key.SpriteRgb;
                for (var p = 0; p + 2 < sprite.Length; p += 3)
                {
                    var (h, s, v) = RuneIconFingerprinter.ToHsv(sprite[p], sprite[p + 1], sprite[p + 2]);
                    if (s < RuneIconFingerprinter.ColourSaturationMin || v < RuneIconFingerprinter.ColourValueMin) continue;
                    counts[((int)(h / 30.0)) % 12]++;
                }
                var total = sprite.Length / 3;
                var top = string.Join(" ", counts.Select((c, b) => (c, b)).Where(t => t.c > 0).OrderByDescending(t => t.c).Take(4).Select(t => $"b{t.b}={t.c}"));
                _output.WriteLine($"  row {i}: hue={key.HueBucket,2}  of {total}px: {(string.IsNullOrEmpty(top) ? "(none)" : top)}");
            }
        }
    }

    [Fact]
    public void DumpHorizontalStructure()
    {
        // RUNE-12: LocateIconRow's candidate vote is swamped by cell-interior ink in rows with few
        // icons. The proposed replacement is to bound the zone by the row bar's own horizontal
        // rules, so first: are those rules actually there, and how strongly?
        var path = FixturePath("2 Raw.png");
        if (path is null) { _output.WriteLine("absent — skipped"); return; }

        using var raw = new Bitmap(path);
        var rgb = RuneIconFingerprinter.CopyPixels(raw, out var stride);
        var width = Math.Min(raw.Width, 420);

        _output.WriteLine("y: dark% across the panel width (rows over 55% marked)");
        for (var y = 0; y < 330; y++)
        {
            var dark = 0;
            for (var x = 0; x < width; x++)
            {
                var idx = (y * stride) + (x * 3);
                if (idx + 2 >= rgb.Length) continue;
                var (_, _, v) = RuneIconFingerprinter.ToHsv(rgb[idx + 2], rgb[idx + 1], rgb[idx]);
                if (v < RuneIconFingerprinter.DarkLineMaxValue) dark++;
            }
            var pct = (int)Math.Round(100.0 * dark / width);
            if (pct >= 20) _output.WriteLine($"  y={y,3}: {pct,3}%{(pct >= 55 ? "  <<<" : "")}");
        }
    }

    [Theory]
    [InlineData("1 Raw.png")]
    [InlineData("2 Raw.png")]
    public void DumpRowGeometry(string fixture)
    {
        var path = FixturePath(fixture);
        if (path is null) { _output.WriteLine($"{fixture}: absent — skipped"); return; }

        using var raw = new Bitmap(path);
        var options = new OcrOptions();
        var (rowYs, rowHeights) = DetectTextRows(raw, options);
        var rgb = RuneIconFingerprinter.CopyPixels(raw, out var stride);

        _output.WriteLine($"{fixture}: {raw.Width}x{raw.Height}, {rowYs.Length} text row(s)");

        for (var i = 0; i < rowYs.Length; i++)
        {
            var searchTop = i == 0 ? 0 : rowYs[i - 1] + rowHeights[i - 1];
            var searchBottom = i == rowYs.Length - 1 ? raw.Height : rowYs[i + 1];

            var expectedIconHeight = (int)Math.Round(rowHeights[i] * RuneIconFingerprinter.IconToTextHeightRatio);
            var zoneTop = Math.Max(Math.Max(0, searchTop), rowYs[i] - (int)Math.Round(rowHeights[i] * RuneIconFingerprinter.ZoneAboveTextRatio));
            var zoneBottom = Math.Min(Math.Min(raw.Height, searchBottom), rowYs[i] + (int)Math.Round(rowHeights[i] * RuneIconFingerprinter.ZoneBelowTextRatio));
            var iconRow = RuneIconFingerprinter.LocateIconRow(rgb, raw.Width, raw.Height, stride, zoneTop, zoneBottom, expectedIconHeight);

            _output.WriteLine(
                $"row {i}: textY={rowYs[i],3} textH={rowHeights[i],2} expIcon={expectedIconHeight,3} " +
                $"search=[{searchTop,3},{searchBottom,3}) zone=[{zoneTop,3},{zoneBottom,3}) " +
                $"iconRow={(iconRow is null ? "NULL" : $"[{iconRow.Value.Top},{iconRow.Value.Bottom}] h={iconRow.Value.Bottom - iconRow.Value.Top + 1}")}");

            var cells = RuneIconFingerprinter.DetectCells(rgb, raw.Width, raw.Height, stride, searchTop, searchBottom, rowYs[i], rowHeights[i]);
            foreach (var cell in cells)
            {
                var state = cell.IsGilded ? "GILDED" : cell.Ambiguous ? "ambig " : "plain ";
                _output.WriteLine($"        {state} {cell.Bounds.X,3},{cell.Bounds.Y,3} {cell.Bounds.Width,2}x{cell.Bounds.Height,2} ring={cell.GoldHueRingProportion:F3}");
            }
        }
    }
}
