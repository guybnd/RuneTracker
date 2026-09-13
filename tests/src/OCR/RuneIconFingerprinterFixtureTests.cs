using System.Drawing.Imaging;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using RuneshapePriceChecker.Contracts;
using RuneshapePriceChecker.OCR;
using Xunit;
using Xunit.Abstractions;

namespace RuneshapePriceChecker.Tests.OCR;

/// <summary>
/// RUNE-1 spike-gate evidence: runs RuneIconFingerprinter against real capture-region
/// fixtures (never synthetic art — see the ticket's step-3 gate). Skips cleanly when a
/// fixture is absent so the suite stays green on machines without the asset checked in.
/// Every assertion here is against visually confirmed ground truth for that fixture; the
/// measured values are also written to the test output so the ticket's evidence comment can
/// quote them.
/// </summary>
public class RuneIconFingerprinterFixtureTests
{
    private readonly ITestOutputHelper _output;
    public RuneIconFingerprinterFixtureTests(ITestOutputHelper output) => _output = output;

    /// <summary>
    /// Per-fixture ground truth, confirmed by eye on the capture (gilded cells compared side by
    /// side at 4x): icon count per Combinations row (top to bottom), the 0-based index of the
    /// single gilded cell in every row, and the rows grouped by which gilded rune they show —
    /// rows in one group carry the same rune and must produce near-identical keys; rows in
    /// different groups carry different runes and must not. On 2560x1440 the rows show four
    /// distinct gilded runes: row 0 alone, rows 1 and 3, rows 2 and 4, row 5 alone.
    /// </summary>
    private static readonly Dictionary<string, (int[] IconCounts, int GildedIndex, int[][] RuneGroups)> GroundTruth = new()
    {
        ["2560x1440"] = ([6, 6, 6, 4, 4, 4], 2, [[0], [1, 3], [2, 4], [5]]),
    };

    /// <summary>Regression margins around the gate's threshold, from the measured 0.26-0.31 gilded vs 0.00-0.05 non-gilded rings.</summary>
    private const double MinGildedRing = 0.20;
    private const double MaxPlainRing = 0.08;

    /// <summary>Same rune, same resolution: measured 1-3 bits apart; distinct runes measured 22+ bits apart.</summary>
    private const int SameRuneMaxHamming = 8;
    private const int DifferentRuneMinHamming = 16;

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
    /// Spike-gate checks (a2) and (b) on one real fixture: every row's cells land on the icons
    /// (exact icon count, gilded cell at the ground-truth position), and the gold-ring metric
    /// separates gilded from non-gilded with zero overlap. Writes the colour-coded cell overlay
    /// next to the test output for eyeballing.
    /// </summary>
    [Theory]
    [MemberData(nameof(Profiles))]
    public void Fixture_EveryRow_IsolatesExactlyOneGildedCell_WithZeroRingOverlap(string profile)
    {
        var path = ResolveFixturePath($"fixtures/runeicons/{profile}/1 Raw.png");
        if (path is null)
        {
            _output.WriteLine($"SKIP ({profile}): fixture not found — real capture-region 1 Raw.png required, see RUNE-1.");
            return;
        }

        var truth = GroundTruth[profile];
        using var raw = new Bitmap(path);
        var options = new OcrOptions();
        var (rowYs, rowHeights) = DetectTextRows(raw, options);
        Assert.Equal(truth.IconCounts.Length, rowYs.Length);

        var rgb = RuneIconFingerprinter.CopyPixels(raw, out var stride);
        using var overlay = (Bitmap)raw.Clone();
        using var g = Graphics.FromImage(overlay);
        using var gildedPen = new Pen(Color.Lime, 2f);
        using var ambiguousPen = new Pen(Color.Orange, 2f);
        using var plainPen = new Pen(Color.Red, 1f);

        var gildedRings = new List<double>();
        var plainRings = new List<double>();
        var keys = new List<RuneKey>();
        var failures = new List<string>();

        for (var i = 0; i < rowYs.Length; i++)
        {
            var searchTop = i == 0 ? 0 : rowYs[i - 1] + rowHeights[i - 1];
            var searchBottom = i == rowYs.Length - 1 ? raw.Height : rowYs[i + 1];

            var cells = RuneIconFingerprinter.DetectCells(rgb, raw.Width, raw.Height, stride, searchTop, searchBottom, rowYs[i], rowHeights[i]);
            _output.WriteLine($"Row {i}: textY={rowYs[i]} textH={rowHeights[i]} window=[{searchTop},{searchBottom}) -> {cells.Count} cell(s)");
            for (var j = 0; j < cells.Count; j++)
            {
                var cell = cells[j];
                var pen = cell.Ambiguous ? ambiguousPen : cell.IsGilded ? gildedPen : plainPen;
                g.DrawRectangle(pen, cell.Bounds.X, cell.Bounds.Y, cell.Bounds.Width, cell.Bounds.Height);
                _output.WriteLine($"    cell[{j}] X={cell.Bounds.X} Y={cell.Bounds.Y} W={cell.Bounds.Width} H={cell.Bounds.Height} glyph=({cell.GlyphBounds.X},{cell.GlyphBounds.Y},{cell.GlyphBounds.Width},{cell.GlyphBounds.Height}) goldRing={cell.GoldHueRingProportion:F2} gilded={cell.IsGilded} ambiguous={cell.Ambiguous}");
                (j == truth.GildedIndex ? gildedRings : plainRings).Add(cell.GoldHueRingProportion);
            }

            if (cells.Count != truth.IconCounts[i])
                failures.Add($"row {i}: expected {truth.IconCounts[i]} cells, got {cells.Count}");
            for (var j = 0; j < cells.Count; j++)
            {
                var shouldBeGilded = j == truth.GildedIndex;
                if (cells[j].IsGilded != shouldBeGilded || (!shouldBeGilded && cells[j].Ambiguous))
                    failures.Add($"row {i} cell[{j}]: gilded={cells[j].IsGilded} ambiguous={cells[j].Ambiguous}, expected gilded={shouldBeGilded}");
            }

            var rowKeys = RuneIconFingerprinter.ExtractRowKeys(raw, searchTop, searchBottom, rowYs[i], rowHeights[i]);
            _output.WriteLine($"  -> {rowKeys.Count} gilded key(s): [{string.Join(", ", rowKeys.Select(k => $"{k.ShapeHash:X16}/hue{k.HueBucket}"))}]");
            if (rowKeys.Count != 1)
                failures.Add($"row {i}: expected exactly 1 gilded key, got {rowKeys.Count}");
            keys.Add(rowKeys.Count > 0 ? rowKeys[0] : default!);
        }

        var outDir = Path.Combine(AppContext.BaseDirectory, "rune-probe-out");
        Directory.CreateDirectory(outDir);
        var outPath = Path.Combine(outDir, $"6 IconCells-{profile}.png");
        OcrImagePreprocessor.SavePng(overlay, outPath);
        _output.WriteLine($"Overlay written to: {outPath}");

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));

        // Gate (b) on this fixture: zero overlap between the classes, with a regression margin.
        _output.WriteLine($"gold ring — gilded: min={gildedRings.Min():F2} max={gildedRings.Max():F2} (n={gildedRings.Count}); non-gilded: min={plainRings.Min():F2} max={plainRings.Max():F2} (n={plainRings.Count})");
        Assert.True(gildedRings.Min() > plainRings.Max(), $"gold-ring classes overlap: gilded min {gildedRings.Min():F2} <= non-gilded max {plainRings.Max():F2}");
        Assert.True(gildedRings.Min() >= MinGildedRing, $"gilded ring min {gildedRings.Min():F2} fell below the {MinGildedRing:F2} regression margin");
        Assert.True(plainRings.Max() <= MaxPlainRing, $"non-gilded ring max {plainRings.Max():F2} rose above the {MaxPlainRing:F2} regression margin");

        // Key identity across rows: the same rune must hash the same, different runes must not.
        foreach (var group in truth.RuneGroups)
        {
            for (var a = 0; a < group.Length; a++)
            {
                for (var b = a + 1; b < group.Length; b++)
                {
                    var distance = BitOperations.PopCount(keys[group[a]].ShapeHash ^ keys[group[b]].ShapeHash);
                    _output.WriteLine($"same rune rows {group[a]}/{group[b]}: hamming={distance} hue {keys[group[a]].HueBucket}/{keys[group[b]].HueBucket}");
                    Assert.True(distance <= SameRuneMaxHamming, $"rows {group[a]} and {group[b]} show the same gilded rune but hash {distance} bits apart");

                    // Hue equality is NOT asserted, and that is a known gap rather than an
                    // oversight. Identity (the hash) is stable across rows; the glyph's measured
                    // colour is not — rows 1 and 3 here are the same rune and count 162 and 72
                    // gold pixels, landing either side of the threshold. Hue is advisory only: it
                    // is excluded from matching, and RuneCatalog.UnboundWeight deliberately does
                    // not score gold until this is stable. Tracked as RUNE-16.
                    if (keys[group[a]].HueBucket != keys[group[b]].HueBucket)
                        _output.WriteLine($"  NOTE: same rune, different colour reading ({keys[group[a]].HueBucket} vs {keys[group[b]].HueBucket}) — RUNE-16");
                }
            }
        }
        for (var ga = 0; ga < truth.RuneGroups.Length; ga++)
        {
            for (var gb = ga + 1; gb < truth.RuneGroups.Length; gb++)
            {
                var distance = BitOperations.PopCount(keys[truth.RuneGroups[ga][0]].ShapeHash ^ keys[truth.RuneGroups[gb][0]].ShapeHash);
                _output.WriteLine($"different runes rows {truth.RuneGroups[ga][0]}/{truth.RuneGroups[gb][0]}: hamming={distance}");
                Assert.True(distance >= DifferentRuneMinHamming, $"distinct gilded runes hash only {distance} bits apart");
            }
        }
    }
}
