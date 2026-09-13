using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using RuneshapePriceChecker.OCR;
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
            var iconRow = RuneIconFingerprinter.LocateIconRow(rgb, raw.Width, stride, zoneTop, zoneBottom, expectedIconHeight);

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
