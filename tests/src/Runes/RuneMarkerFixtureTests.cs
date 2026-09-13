using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RuneshapePriceChecker.Configuration;
using RuneshapePriceChecker.Contracts;
using RuneshapePriceChecker.OCR;
using RuneshapePriceChecker.Overlay;
using RuneshapePriceChecker.Runes;
using Xunit;
using Xunit.Abstractions;

namespace RuneshapePriceChecker.Tests.Runes;

/// <summary>
/// End to end on the real 2560x1440 capture: detector → catalog (bind, carry) → scorer → marker
/// painter. Asserts the markers land on the gilded cells and writes the painted frame next to the
/// test output for eyeballing. Skips when the fixture is absent.
/// </summary>
public class RuneMarkerFixtureTests(ITestOutputHelper output) : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "rpc-marker-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); } catch { }
    }

    [Fact]
    public void Fixture_MarkersFrameEveryGildedCell_AndTopPickIsOpulent()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures", "runeicons", "2560x1440", "1 Raw.png");
        if (!File.Exists(path))
        {
            output.WriteLine("SKIP: fixture not found");
            return;
        }

        using var raw = new Bitmap(path);
        var (rowYs, rowHeights) = DetectTextRows(raw, new OcrOptions());
        var runeRows = new List<RuneRowKeys>();
        for (var i = 0; i < rowYs.Length; i++)
        {
            var searchTop = i == 0 ? 0 : rowYs[i - 1] + rowHeights[i - 1];
            var searchBottom = i == rowYs.Length - 1 ? raw.Height : rowYs[i + 1];
            runeRows.Add(new RuneRowKeys(rowYs[i], RuneIconFingerprinter.ExtractRowKeys(raw, searchTop, searchBottom, rowYs[i], rowHeights[i])));
        }
        Assert.Equal(6, runeRows.Sum(r => r.Keys.Count));
        Assert.All(runeRows.SelectMany(r => r.Keys), k => Assert.True(k.CellBounds.Width > 30 && k.CellBounds.Height > 30, $"cell bounds missing: {k.CellBounds}"));

        var options = new StaticOptions(new RunesOptions());
        using var catalog = new RuneCatalog(options, NullLogger.Instance, Path.Combine(_dir, "c.json"), RuneCatalog.LoadShippedJson());

        // Two sightings persist the bindings; then bind the two recurring runes and carry one.
        foreach (var key in runeRows.SelectMany(r => r.Keys)) { catalog.Observe(key); }
        foreach (var key in runeRows.SelectMany(r => r.Keys)) { catalog.Observe(key); }
        Assert.Equal(4, catalog.Bindings.Count); // four distinct gilded runes on this capture

        var rows13 = catalog.FindBinding(runeRows[1].Keys[0].ShapeHash)!;
        var rows24 = catalog.FindBinding(runeRows[2].Keys[0].ShapeHash)!;
        Assert.Equal(rows13.Id, catalog.FindBinding(runeRows[3].Keys[0].ShapeHash)!.Id);
        Assert.Equal(rows24.Id, catalog.FindBinding(runeRows[4].Keys[0].ShapeHash)!.Id);
        Assert.True(catalog.Bind(rows13.Id, "power"));
        Assert.True(catalog.Bind(rows24.Id, "opulent"));
        catalog.SetCarried("power", true);

        var snapshot = new LeagueWindowSnapshot(
            Enumerable.Range(0, rowYs.Length).Select(i => $"row {i}").ToList(),
            DateTimeOffset.UtcNow,
            rowYs,
            RuneRows: runeRows);
        var sheet = new RuneRowScorer(catalog, options).Score(snapshot);

        var markers = RuneMarkerPainter.FromSheet(sheet);
        Assert.Equal(6, markers.Count);
        Assert.Equal(2, markers.Count(m => m.Kind == RuneMarkerKind.Carried));   // rows 1 and 3: Power, carried
        Assert.Equal(2, markers.Count(m => m.Kind == RuneMarkerKind.HighValue)); // rows 2 and 4: Opulent (3)
        Assert.Equal(2, markers.Count(m => m.Kind == RuneMarkerKind.Valuable));  // rows 0 and 5: unbound, weight 1
        Assert.Equal(2, markers.Count(m => m.IsTopPick));
        Assert.All(markers.Where(m => m.IsTopPick), m => Assert.Equal(RuneMarkerKind.HighValue, m.Kind));
        Assert.Equal(2, markers.Count(m => m.IsUnbound));

        using var painted = (Bitmap)raw.Clone();
        using (var g = Graphics.FromImage(painted))
            RuneMarkerPainter.Paint(g, markers, CarriedMarkerStyle.Slash);

        // Each frame's left band sits immediately outside its cell.
        foreach (var m in markers)
        {
            var probe = painted.GetPixel(m.Cell.X - 2, m.Cell.Y + (m.Cell.Height / 2));
            var expected = RuneMarkerPainter.ColorFor(m.Kind, CarriedMarkerStyle.Slash);
            var d = Math.Abs(probe.R - expected.R) + Math.Abs(probe.G - expected.G) + Math.Abs(probe.B - expected.B);
            Assert.True(d < 90, $"frame colour missing left of cell {m.Cell}: got {probe}, expected ~{expected}");
        }

        var outDir = Path.Combine(AppContext.BaseDirectory, "rune-probe-out");
        _ = Directory.CreateDirectory(outDir);
        var outPath = Path.Combine(outDir, "7 RuneMarkers-2560x1440.png");
        painted.Save(outPath, ImageFormat.Png);
        output.WriteLine($"Marker preview written to: {outPath}");
        foreach (var row in sheet.Rows)
            output.WriteLine($"row y={row.RowY} score={row.Score} best={row.IsBest} keys=[{string.Join(", ", row.Keys.Select(k => $"{k.DisplayName}:{k.Marker}{(k.IsTopPick ? "★" : "")}"))}]");
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

    private sealed class StaticOptions(RunesOptions value) : IOptionsMonitor<RunesOptions>
    {
        public RunesOptions CurrentValue => value;
        public RunesOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<RunesOptions, string?> listener) => null;
    }
}
