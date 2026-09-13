using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging.Abstractions;
using RuneshapePriceChecker.Configuration;
using RuneshapePriceChecker.Contracts;
using RuneshapePriceChecker.OCR;
using RuneshapePriceChecker.Runes;
using Xunit;
using Xunit.Abstractions;

namespace RuneshapePriceChecker.Tests.Runes;

/// <summary>
/// End to end on the real six-icon capture (RUNE-22's <c>6 Raw.png</c>): detector → Combinations
/// table → catalog → scorer, with the row names as the game prints them. Every gilded rune must
/// come out named, with nothing bound by hand, and the library must have named its sprites.
/// </summary>
public sealed class RuneCombinationFixtureTests(ITestOutputHelper output) : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "rpc-combo-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); } catch { }
    }

    /// <summary>The panel's rows, top to bottom, as the game prints them and as poe2db lists them.</summary>
    private static readonly (string RowText, string[] Gilded)[] Truth =
    [
        ("Skill Level 20: Skyfall", ["celestial", "oath"]),
        ("Skill Level 20: Triskelion Cascade", ["celestial", "bond"]),
        ("Skill Level 20: Refutation", ["prismatic", "death"]),
        ("Skill Level 20: Runic Reprieve", ["prismatic", "soul"]),
        ("Skill Level 20: Leylines", ["celestial", "life"]),
        ("Skill Level 20: Animus Exchange", ["prismatic", "oath"]),
        ("Support: Healing Runes", ["prismatic"]),
    ];

    [Fact]
    public void Fixture_EveryGildedRuneIsNamedFromTheTable_AndTheLibraryLearnsTheSprites()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures", "runeicons", "2560x1440", "6 Raw.png");
        if (!File.Exists(path)) { output.WriteLine("SKIP: fixture not found"); return; }

        using var raw = new Bitmap(path);
        var (rowYs, rowHeights) = DetectTextRows(raw, new OcrOptions());
        Assert.Equal(Truth.Length, rowYs.Length);

        var rows = new List<RuneRowKeys>();
        for (var i = 0; i < rowYs.Length; i++)
        {
            var searchTop = i == 0 ? 0 : rowYs[i - 1] + rowHeights[i - 1];
            var searchBottom = i == rowYs.Length - 1 ? raw.Height : rowYs[i + 1];
            var keys = RuneIconFingerprinter.ExtractRowKeys(raw, searchTop, searchBottom, rowYs[i], rowHeights[i]);
            rows.Add(new RuneRowKeys(rowYs[i], keys, Truth[i].RowText));
        }

        var options = new StaticOptionsMonitor<RunesOptions>(new RunesOptions());
        using var catalog = new RuneCatalog(options, NullLogger.Instance, Path.Combine(_dir, "c.json"), RuneCatalog.LoadShippedJson());
        var table = new RuneCombinationTable(RuneCombinationTable.LoadShippedJson());
        var scorer = new RuneRowScorer(catalog, options, table);

        // Two sightings persist the sprites, as in the app; the table then names them on scoring.
        foreach (var key in rows.SelectMany(r => r.Keys)) catalog.Observe(key);
        foreach (var key in rows.SelectMany(r => r.Keys)) catalog.Observe(key);
        Assert.All(catalog.Bindings, b => Assert.False(b.IsBound));

        var sheet = scorer.Score(rows);

        var failures = new List<string>();
        for (var i = 0; i < Truth.Length; i++)
        {
            var scored = sheet.Rows[i].Keys;
            output.WriteLine($"row {i} '{Truth[i].RowText}': {string.Join(", ", scored.Select(k => $"cell{k.Key.CellIndex}/{k.Key.CellCount}={k.RuneId ?? "?"}"))}");
            if (scored.Count != Truth[i].Gilded.Length)
                failures.Add($"row {i}: {scored.Count} gilded keys, expected {Truth[i].Gilded.Length}");
            for (var j = 0; j < Math.Min(scored.Count, Truth[i].Gilded.Length); j++)
            {
                if (!string.Equals(scored[j].RuneId, Truth[i].Gilded[j], StringComparison.OrdinalIgnoreCase))
                    failures.Add($"row {i} key {j}: resolved {scored[j].RuneId ?? "unbound"}, table says {Truth[i].Gilded[j]}");
                if (scored[j].IsUnbound) failures.Add($"row {i} key {j}: still unbound");
            }
        }
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));

        // The library named every persisted sprite, and each sprite hash maps to one rune.
        Assert.NotEmpty(catalog.Bindings);
        Assert.All(catalog.Bindings, b => Assert.True(b.IsBound, $"sprite {b.Id} is still unbound"));
        var runesNamed = catalog.Bindings.Select(b => b.RuneId!).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(r => r).ToList();
        output.WriteLine($"library named: {string.Join(", ", runesNamed)}");
        Assert.Superset(new HashSet<string> { "celestial", "prismatic", "oath", "bond", "death", "soul", "life" }, runesNamed.ToHashSet());
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
