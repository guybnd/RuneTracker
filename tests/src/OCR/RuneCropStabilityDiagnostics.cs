using System.Drawing.Imaging;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using RuneshapePriceChecker.OCR;
using Xunit;
using Xunit.Abstractions;

namespace RuneshapePriceChecker.Tests.OCR;

/// <summary>
/// Diagnostic for the "57 unbound sprites when the game has 34 runes" report: measures how far
/// the identity hash moves when the glyph crop shifts by a pixel or two.
///
/// The crop is not anchored to the rune. <c>AssignLatticeGlyphBounds</c> derives it from a
/// neighbouring plain cell's right edge, an integer-divided median pitch, and the OCR text row's
/// top and height — every one of which can land a pixel off between frames. If a 1-2px shift
/// moves the hash further than <c>MatchHammingThreshold</c> (8 bits), the same rune stores as
/// several different sprites, which is exactly what the user is seeing.
/// </summary>
public class RuneCropStabilityDiagnostics
{
    private readonly ITestOutputHelper _output;
    public RuneCropStabilityDiagnostics(ITestOutputHelper output) => _output = output;

    private const int MatchHammingThreshold = 8;

    private static string? FixturePath()
    {
        var candidate = Path.Combine(AppContext.BaseDirectory, "fixtures/runeicons/2560x1440/1 Raw.png");
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
    public void GlyphHashUnderSubPixelCropDrift()
    {
        var path = FixturePath();
        if (path is null) { _output.WriteLine("fixture absent — skipped"); return; }

        using var raw = new Bitmap(path);
        var options = new OcrOptions();
        var (rowYs, rowHeights) = DetectTextRows(raw, options);
        var rgb = RuneIconFingerprinter.CopyPixels(raw, out var stride);

        var worstByShift = new Dictionary<(int dx, int dy), int>();
        var worstPlate = new Dictionary<(int dx, int dy), int>();
        var plateFound = 0;
        var cellCount = 0;

        for (var i = 0; i < rowYs.Length; i++)
        {
            var searchTop = i == 0 ? 0 : rowYs[i - 1] + rowHeights[i - 1];
            var searchBottom = i == rowYs.Length - 1 ? raw.Height : rowYs[i + 1];
            var cells = RuneIconFingerprinter.DetectCells(rgb, raw.Width, raw.Height, stride, searchTop, searchBottom, rowYs[i], rowHeights[i]);

            foreach (var cell in cells)
            {
                if (!cell.IsGilded) continue;
                cellCount++;

                var baseBox = RuneIconFingerprinter.Inset(cell.GlyphBounds, RuneIconFingerprinter.GlyphInsetRatio);
                var baseHash = RuneIconFingerprinter.ComputeDHash(
                    RuneIconFingerprinter.NormalizeTo32x32(rgb, raw.Width, stride, baseBox, marginRatio: 0));

                // The plate crop is re-derived from the image for every shift, which is the whole
                // point: drift in the detected cell box must not move the crop.
                var basePlate = RuneIconFingerprinter.FindPlateBounds(rgb, raw.Width, raw.Height, stride, cell.Bounds);
                if (basePlate is not null) plateFound++;
                var basePlateHash = basePlate is null ? baseHash : RuneIconFingerprinter.ComputeDHash(
                    RuneIconFingerprinter.NormalizeTo32x32(rgb, raw.Width, stride, basePlate.Value, marginRatio: 0));

                for (var dy = -2; dy <= 2; dy++)
                {
                    for (var dx = -2; dx <= 2; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var key = (dx, dy);

                        var shifted = new Rectangle(baseBox.X + dx, baseBox.Y + dy, baseBox.Width, baseBox.Height);
                        var hash = RuneIconFingerprinter.ComputeDHash(
                            RuneIconFingerprinter.NormalizeTo32x32(rgb, raw.Width, stride, shifted, marginRatio: 0));
                        var distance = BitOperations.PopCount(baseHash ^ hash);
                        if (!worstByShift.TryGetValue(key, out var worst) || distance > worst)
                            worstByShift[key] = distance;

                        var shiftedCell = new Rectangle(cell.Bounds.X + dx, cell.Bounds.Y + dy, cell.Bounds.Width, cell.Bounds.Height);
                        var plate = RuneIconFingerprinter.FindPlateBounds(rgb, raw.Width, raw.Height, stride, shiftedCell);
                        var plateHash = plate is null ? 0UL : RuneIconFingerprinter.ComputeDHash(
                            RuneIconFingerprinter.NormalizeTo32x32(rgb, raw.Width, stride, plate.Value, marginRatio: 0));
                        // A shift that loses the plate falls back to the lattice box in production,
                        // so score it as the full 64-bit miss rather than quietly skipping it.
                        var plateDistance = plate is null ? 64 : BitOperations.PopCount(basePlateHash ^ plateHash);
                        if (!worstPlate.TryGetValue(key, out var worstP) || plateDistance > worstP)
                            worstPlate[key] = plateDistance;
                    }
                }
            }
        }

        void Grid(string title, Dictionary<(int dx, int dy), int> data)
        {
            _output.WriteLine(title);
            foreach (var dy in new[] { -2, -1, 0, 1, 2 })
            {
                var cols = new List<string>();
                foreach (var dx in new[] { -2, -1, 0, 1, 2 })
                {
                    if (dx == 0 && dy == 0) { cols.Add("   ."); continue; }
                    cols.Add(data.TryGetValue((dx, dy), out var w) ? w.ToString().PadLeft(4) : "   ?");
                }
                _output.WriteLine($"  dy={dy,2}: {string.Join(" ", cols)}");
            }
        }

        static (int One, int Two) Worst(Dictionary<(int dx, int dy), int> data) =>
            (data.Where(kv => Math.Abs(kv.Key.dx) <= 1 && Math.Abs(kv.Key.dy) <= 1).Max(kv => kv.Value),
             data.Max(kv => kv.Value));

        _output.WriteLine($"gilded cells examined: {cellCount} (plate found on {plateFound})");
        _output.WriteLine($"match threshold: {MatchHammingThreshold} bits (a larger distance stores a NEW sprite)");
        _output.WriteLine("");
        Grid("LATTICE crop (old) — worst Hamming distance from the unshifted hash:", worstByShift);
        _output.WriteLine("");
        Grid("PLATE crop (new) — crop re-derived from the image at each shift:", worstPlate);

        var (latticeOne, latticeTwo) = Worst(worstByShift);
        var (plateOne, plateTwo) = Worst(worstPlate);
        _output.WriteLine("");
        _output.WriteLine($"lattice  worst +/-1px: {latticeOne,3} bits   worst +/-2px: {latticeTwo,3} bits");
        _output.WriteLine($"plate    worst +/-1px: {plateOne,3} bits   worst +/-2px: {plateTwo,3} bits");

        Assert.Equal(cellCount, plateFound);
        Assert.True(plateTwo <= MatchHammingThreshold,
            $"plate crop drifted {plateTwo} bits within +/-2px, over the {MatchHammingThreshold}-bit match threshold");
    }
}
