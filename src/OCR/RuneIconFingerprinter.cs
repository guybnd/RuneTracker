using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using RuneshapePriceChecker.Contracts;

namespace RuneshapePriceChecker.OCR;

/// <summary>
/// Extracts and fingerprints gilded (succession) runes from the icon strip in each
/// Combinations row.
///
/// Geometry model, confirmed pixel-by-pixel against a real 2560x1440 capture
/// (tests/fixtures/runeicons/2560x1440/1 Raw.png): every icon cell is a near-square box
/// drawn with thin (2px) dark vertical and horizontal border lines; cells are left-packed at
/// a fixed pitch with a parchment gap between them; the gilded cell carries a thicker gold
/// border plus three small tabs on its top edge; the first cell in a row may carry a thick
/// blue border. The panel uses TWO row layouts: wide rows (5-6 icons) wrap their name text
/// onto a line BELOW the icon strip, narrower rows (2-4 icons) keep the text BESIDE the icons.
/// In both layouts the icons sit vertically within a few text-heights of that row's text line.
///
/// Segmentation keys on the structural border lines, not on ink density thresholds:
/// <list type="number">
/// <item>The icon row's vertical extent is found from the vertical border lines — columns in the
/// icon strip zone whose contiguous ink run is roughly one icon tall. Horizontal separator lines
/// between row bars never form such runs, and the dark shading under a separator is too short.
/// The median top/bottom of those runs is the row's icon extent (<see cref="LocateIconRow"/>).</item>
/// <item>Within that extent, border columns are those inked over nearly their whole height;
/// glyph strokes are inset from the border and never reach the top and bottom edge at once.
/// Border runs are paired left-to-right into cells of roughly square width
/// (<see cref="SegmentIconCells"/>). A thick gold or blue border simply yields a wider run.</item>
/// <item>The row's bottom is pinned on the cells' dark bevel bottom line, which the beige shadow
/// under the icons (saturated enough to count as ink in wide rows) never matches.</item>
/// <item>A gilded cell's box is widened to its own gold border rows, which sit a few px outside
/// the plain cells' shared extent; cells without a gold border keep the shared extent.</item>
/// </list>
///
/// The earlier approach — first dark band in the window, capped to 1.9x text height, then split
/// fused column-density segments by an estimated width — anchored on the dark separator line
/// ABOVE the icons in every row but the first, so every cell rectangle sat 7-20px too high and
/// the gold-ring metric sampled glyph pixels instead of the border. That was the whole (b)
/// spike-gate gap; the metric itself was never the problem.
///
/// Pure by design: bitmap in, keys out. No OcrResolutionProfiles entry, no capture path
/// changes — every constant below is a ratio of a value measured on the frame each cycle (the
/// row's own text height, the located icon-row height, the capture width), never an absolute
/// pixel offset.
/// </summary>
internal static class RuneIconFingerprinter
{
    internal const int MinCellPx = 24;

    /// <summary>
    /// Gold hue-ring proportion at/above which a cell is classified gilded. On the real
    /// 2560x1440 fixture, with cell boxes landing on the true border, gilded cells measure
    /// 0.26-0.30 and every non-gilded cell (including the plain-bordered rune whose glyph itself
    /// is yellow) measures 0.00-0.07. The plan's original "tabs poke above the row's typical
    /// top" structural signal was tried first and discarded: the tabs are three small nubs and
    /// do not reliably move the cell's measured top.
    /// </summary>
    internal const double GoldRingThreshold = 0.15;
    internal const double AmbiguousGoldRingLow = 0.09;

    /// <summary>
    /// Expected icon-cell height as a multiple of that row's own detected text-line height.
    /// Icon art and text glyphs are rendered by the same UI at the same scale, so the ratio is
    /// resolution-independent. Used only to bound what counts as a plausible border run
    /// (see <see cref="BorderRunMinRatio"/>/<see cref="BorderRunMaxRatio"/>), never as the
    /// cell's size — that is measured. Calibrated ~1.9x on the real 2560x1440 fixture.
    /// </summary>
    internal const double IconToTextHeightRatio = 1.9;

    /// <summary>
    /// Vertical search zone around the row's text line, in text heights: the icons sit either
    /// just above the text (wide rows) or beside it (narrow rows), never further away than this.
    /// Bounding the zone keeps the previous row's icons, the next row's separator shading and
    /// the panel's decorative footer out of the border-run search.
    /// </summary>
    internal const double ZoneAboveTextRatio = 3.0;
    internal const double ZoneBelowTextRatio = 2.0;

    /// <summary>
    /// Icons never reach past ~46% of the panel width even on the widest observed row (RUNE-1
    /// fixture findings); capping the column scan at 50% keeps name-text glyphs out of the
    /// icon-column scan without hardcoding a per-resolution offset — it is a ratio of the
    /// already-known capture width, same idiom as OcrOptions.PanelLeftFraction.
    /// </summary>
    internal const double MaxIconStripFraction = 0.50;

    /// <summary>A vertical border line's ink run must be this fraction of the expected icon height (min/max).</summary>
    internal const double BorderRunMinRatio = 0.6;
    internal const double BorderRunMaxRatio = 1.35;

    /// <summary>Fewer plausible border columns than this in the zone means no icon row (fail-soft).</summary>
    internal const int MinBorderColumns = 4;

    /// <summary>A column is a border column when inked over at least this fraction of the icon-row height.</summary>
    internal const double BorderColumnCoverage = 0.85;

    /// <summary>
    /// The cells' bottom border is drawn as a dark bevel line (HSV value below
    /// <see cref="DarkLineMaxValue"/>) that the beige parchment and its shadow never reach, so it
    /// pins the icon row's true bottom even where the shadow under the icons is saturated enough
    /// to count as ink. A row is that line when this fraction of the border columns is dark there.
    /// </summary>
    internal const double DarkLineMaxValue = 0.25;
    internal const double DarkLineCoverage = 0.5;

    /// <summary>
    /// A gilded cell's gold border sits a few px outside the plain cells' shared extent (and its
    /// top edge carries the tabs), so each cell's box is widened to the outermost rows, within
    /// this fraction of the row height, where at least <see cref="GoldLineCoverage"/> of its
    /// columns are gold. Cells without a gold border keep the shared extent — a yellow glyph never
    /// reaches that coverage at the cell's edge.
    /// </summary>
    internal const double GoldRefineRatio = 0.2;
    internal const double GoldLineCoverage = 0.4;

    /// <summary>
    /// Gold widening walks outward from the paired border and stops after this many consecutive
    /// non-gold lines: the gold bottom line sits 2px below the plain cells' dark bevel line, while
    /// a neighbour's gold glow begins only 3px past a plain cell's border — so 2 reaches the
    /// former and never the latter.
    /// </summary>
    internal const int GoldGapTolerance = 2;

    /// <summary>Border runs closer than this are one (thick or anti-aliased) border line. Kept tight: the gilded border's outer glow starts only ~3px after its neighbour's border.</summary>
    internal const int BorderMergeGapPx = 2;

    /// <summary>Cells are roughly square: a left/right border pair is accepted when its width falls in this range of the icon-row height.</summary>
    internal const double CellWidthMinRatio = 0.85;
    internal const double CellWidthMaxRatio = 1.35;

    /// <summary>A gap wider than this (in icon-row heights) after the last cell is where the name text starts — stop there.</summary>
    internal const double StripEndGapRatio = 1.5;

    /// <summary>
    /// The identity hash and hue are computed on the glyph interior — the cell's lattice box
    /// (<see cref="IconCell.GlyphBounds"/>) inset by this fraction on every side, which removes
    /// the border ring, the tabs and the parchment gap. Measured on the real fixture: hashing the
    /// whole gilded sprite puts the same rune in two rows 11-12 bits apart (border and texture
    /// gradients dominate the sign bits); hashing the interior puts it 1-3 bits apart while
    /// distinct runes stay 22+ bits apart. The full-cell sprite is still carried on the key for
    /// display.
    /// </summary>
    internal const double GlyphInsetRatio = 0.2;

    /// <summary>
    /// One detected icon cell. <see cref="Bounds"/> is the cell as drawn — for a gilded cell that
    /// includes its thicker gold frame — and is what the gold-ring metric and the display sprite
    /// use. <see cref="GlyphBounds"/> is the cell's slot on the row's lattice: every slot is one
    /// icon-row-height square at a fixed pitch, so this box is pixel-identical for the same rune
    /// wherever it appears, independent of how anti-aliasing lands on the gold frame's outer
    /// edge (which moved a plain box edge by 1px between two rows on the real fixture and pushed
    /// an otherwise identical rune 14-23 bits apart). Identity hashing uses GlyphBounds only.
    /// </summary>
    internal readonly record struct IconCell(Rectangle Bounds, Rectangle GlyphBounds, double GoldHueRingProportion, bool IsGilded, bool Ambiguous);

    /// <summary>
    /// Runs the full pipeline for one Combinations row: locate its icon row, segment cells,
    /// classify gilded, and hash the survivors. Never throws — a skip predicate firing logs
    /// once and yields no keys for the row.
    /// </summary>
    /// <param name="rawBitmap">The raw (unpreprocessed) captured frame.</param>
    /// <param name="searchTop">Bottom Y of the previous text row (or 0 for the first row).</param>
    /// <param name="searchBottomExclusive">Top Y of the next text row (or image height for the last row).</param>
    /// <param name="rowTextY">This row's detected text-line top Y — anchors the vertical search zone.</param>
    /// <param name="rowTextHeight">This row's detected text-line height — scales the expected icon height.</param>
    /// <param name="logger">Optional logger for skip-predicate diagnostics.</param>
    public static IReadOnlyList<RuneKey> ExtractRowKeys(
        Bitmap rawBitmap, int searchTop, int searchBottomExclusive, int rowTextY, int rowTextHeight, ILogger? logger = null)
    {
        if (rawBitmap is null) return [];
        var width = rawBitmap.Width;
        var height = rawBitmap.Height;
        var rgb = CopyPixels(rawBitmap, out var stride);

        var cells = DetectCells(rgb, width, height, stride, searchTop, searchBottomExclusive, rowTextY, rowTextHeight, logger);
        if (cells.Count == 0) return [];

        var keys = new List<RuneKey>();
        foreach (var cell in cells)
        {
            if (cell.Bounds.Width < MinCellPx || cell.Bounds.Height < MinCellPx)
                continue; // degenerate segment (noise) — never counted as a real icon

            if (cell.Ambiguous)
            {
                logger?.LogDebug(
                    "RuneIconFingerprinter: cell @X={X} gold ring ambiguous ({G:F2}) — dropped as non-gilded",
                    cell.Bounds.X, cell.GoldHueRingProportion);
                continue;
            }

            if (!cell.IsGilded) continue;

            var sprite = NormalizeTo32x32(rgb, width, stride, cell.Bounds);
            var glyph = NormalizeTo32x32(rgb, width, stride, Inset(cell.GlyphBounds, GlyphInsetRatio), marginRatio: 0);
            var hash = ComputeDHash(glyph);
            var hue = DominantGlyphHueBucket(glyph);
            keys.Add(new RuneKey(hash, hue, sprite));
        }

        return keys;
    }

    /// <summary>
    /// Locates and segments every icon cell for one row (gilded or not) — the shared front half
    /// of <see cref="ExtractRowKeys"/>, exposed so debug overlays and fixture tests draw exactly
    /// what the extractor saw. Returns an empty list (never throws) when any skip predicate fires.
    /// </summary>
    internal static IReadOnlyList<IconCell> DetectCells(
        byte[] rgb, int width, int height, int stride, int searchTop, int searchBottomExclusive, int rowTextY, int rowTextHeight, ILogger? logger = null)
    {
        if (rowTextHeight <= 0)
        {
            logger?.LogDebug("RuneIconFingerprinter: skip — row text height {H} is not usable", rowTextHeight);
            return [];
        }

        var expectedIconHeight = (int)Math.Round(rowTextHeight * IconToTextHeightRatio);
        var zoneTop = Math.Max(Math.Max(0, searchTop), rowTextY - (int)Math.Round(rowTextHeight * ZoneAboveTextRatio));
        var zoneBottom = Math.Min(Math.Min(height, searchBottomExclusive), rowTextY + (int)Math.Round(rowTextHeight * ZoneBelowTextRatio));
        if (zoneBottom - zoneTop < MinCellPx)
        {
            logger?.LogDebug("RuneIconFingerprinter: skip — search zone [{Top},{Bottom}) is under MinCellPx", zoneTop, zoneBottom);
            return [];
        }

        var iconRow = LocateIconRow(rgb, width, stride, zoneTop, zoneBottom, expectedIconHeight);
        if (iconRow is null)
        {
            logger?.LogDebug("RuneIconFingerprinter: skip — no icon row (vertical border lines) found in [{Top},{Bottom})", zoneTop, zoneBottom);
            return [];
        }

        var cells = SegmentIconCells(rgb, width, height, stride, iconRow.Value.Top, iconRow.Value.Bottom);
        if (cells.Count == 0)
            logger?.LogDebug("RuneIconFingerprinter: skip — icon row [{Top},{Bottom}] yielded no cell border pairs", iconRow.Value.Top, iconRow.Value.Bottom);
        return cells;
    }

    /// <summary>
    /// Finds the icon row's vertical extent inside a zone from its vertical border lines: every
    /// column in the icon strip zone whose contiguous ink run is roughly one icon tall
    /// (<see cref="BorderRunMinRatio"/>..<see cref="BorderRunMaxRatio"/> of
    /// <paramref name="expectedIconHeight"/>) is a border-line candidate. Horizontal separator
    /// lines and their shading are only a few px tall, glyph strokes are shorter than a cell, and a
    /// thick border that merges with a separator above or below overshoots the max — none of them
    /// qualify. The median top/bottom of the candidates (after discarding those that do not mostly
    /// overlap the median) is the row's icon extent; the bottom is then pinned on the dark bevel
    /// line (see <see cref="DarkLineMaxValue"/>) so shadow under the icons cannot stretch it.
    /// Returns null when too few candidates exist.
    /// </summary>
    internal static (int Top, int Bottom)? LocateIconRow(byte[] rgb, int width, int stride, int zoneTop, int zoneBottomExclusive, int expectedIconHeight)
    {
        var zoneHeight = zoneBottomExclusive - zoneTop;
        if (zoneHeight < MinCellPx || expectedIconHeight <= 0) return null;

        var minRun = Math.Max(MinCellPx / 2, (int)(expectedIconHeight * BorderRunMinRatio));
        var maxRun = (int)Math.Ceiling(expectedIconHeight * BorderRunMaxRatio);
        var scanWidth = Math.Max(1, Math.Min(width, (int)(width * MaxIconStripFraction)));

        var candidates = new List<(int X, int Top, int Bottom)>();
        for (var x = 0; x < scanWidth; x++)
        {
            var runStart = -1;
            for (var y = zoneTop; y <= zoneBottomExclusive; y++)
            {
                var isInk = y < zoneBottomExclusive && IsInkAt(rgb, stride, x, y);
                if (isInk)
                {
                    if (runStart < 0) runStart = y;
                    continue;
                }
                if (runStart >= 0)
                {
                    var len = y - runStart;
                    if (len >= minRun && len <= maxRun)
                        candidates.Add((x, runStart, y - 1));
                    runStart = -1;
                }
            }
        }

        if (candidates.Count < MinBorderColumns) return null;

        var top = Median(candidates.Select(c => c.Top));
        var bottom = Median(candidates.Select(c => c.Bottom));
        var rowHeight = bottom - top + 1;
        if (rowHeight < MinCellPx) return null;

        var kept = candidates
            .Where(c => Math.Min(c.Bottom, bottom) - Math.Max(c.Top, top) + 1 >= 0.8 * rowHeight)
            .ToList();
        if (kept.Count < MinBorderColumns) return null;

        top = Median(kept.Select(c => c.Top));
        bottom = Median(kept.Select(c => c.Bottom));

        // In wide rows the parchment under the left cells is shadowed just enough to read as ink,
        // which drags those border runs a few px past the real bottom. The bottom border itself
        // is a dark bevel line across every cell, so walk up from the median bottom to it.
        var columns = kept.Select(c => c.X).Distinct().ToArray();
        var requiredDark = (int)Math.Ceiling(columns.Length * DarkLineCoverage);
        for (var y = bottom; y >= top + minRun - 1; y--)
        {
            var dark = columns.Count(x => IsDarkAt(rgb, stride, x, y));
            if (dark >= requiredDark)
            {
                bottom = y;
                break;
            }
        }

        return bottom - top + 1 < MinCellPx ? null : (top, bottom);
    }

    /// <summary>
    /// Segments the icon row [<paramref name="rowTop"/>, <paramref name="rowBottom"/>] into cells
    /// from its vertical border lines: a border column is inked over at least
    /// <see cref="BorderColumnCoverage"/> of the row height (glyph strokes are inset and never
    /// span top to bottom); adjacent border columns merge into one border run (thick gold/blue
    /// borders are just wider runs). Runs are paired left-to-right — a left run and the first run
    /// far enough right to make a roughly square cell — and the walk stops at the first gap wide
    /// enough to be the start of the name text. Each cell's box is then widened to its own gold
    /// border rows when it has them (see <see cref="GoldRefineRatio"/>).
    /// </summary>
    internal static IReadOnlyList<IconCell> SegmentIconCells(byte[] rgb, int width, int height, int stride, int rowTop, int rowBottom)
    {
        var rowHeight = rowBottom - rowTop + 1;
        if (rowHeight < MinCellPx || rowTop < 0 || rowBottom >= height) return [];

        var scanWidth = Math.Max(1, Math.Min(width, (int)(width * MaxIconStripFraction)));
        var requiredInk = (int)Math.Ceiling(rowHeight * BorderColumnCoverage);
        var isBorder = new bool[scanWidth];
        for (var x = 0; x < scanWidth; x++)
        {
            var count = 0;
            for (var y = rowTop; y <= rowBottom; y++)
            {
                if (IsInkAt(rgb, stride, x, y)) count++;
            }
            isBorder[x] = count >= requiredInk;
        }

        var runs = FindRuns(isBorder, BorderMergeGapPx);
        if (runs.Count < 2) return [];

        var minWidth = (int)Math.Ceiling(rowHeight * CellWidthMinRatio);
        var maxWidth = (int)Math.Floor(rowHeight * CellWidthMaxRatio);
        var stripEndGap = rowHeight * StripEndGapRatio;

        var cells = new List<IconCell>();
        var k = 0;
        while (k < runs.Count)
        {
            var (leftStart, _) = runs[k];
            var found = -1;
            for (var j = k + 1; j < runs.Count; j++)
            {
                var cellWidth = runs[j].End - leftStart + 1;
                if (cellWidth < minWidth) continue; // an interior glyph stroke, not the right border
                if (cellWidth <= maxWidth) found = j;
                break;
            }

            if (found < 0)
            {
                k++; // unpaired run: stray stroke or a cell we cannot bound — fail-soft, skip it
                continue;
            }

            if (cells.Count > 0 && leftStart - (cells[^1].Bounds.Right - 1) > stripEndGap)
                break; // the name text starts here

            var x0 = leftStart;
            var x1 = runs[found].End;
            (x0, x1) = WidenToGoldColumns(rgb, width, stride, x0, x1, rowTop, rowBottom);
            var (y0, y1) = RefineToGoldBorder(rgb, width, height, stride, x0, x1, rowTop, rowBottom);
            var bounds = new Rectangle(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
            var goldRing = GoldHueRingProportion(rgb, width, stride, bounds);
            var (isGilded, ambiguous) = ClassifyGoldRing(goldRing);
            cells.Add(new IconCell(bounds, bounds, goldRing, isGilded, ambiguous));

            // A gold border's anti-aliased outer edge can register as a separate hairline run
            // just past the paired right border — inside the widened cell or within the gold gap
            // tolerance of it. It is part of this cell, never the next one's left border.
            k = found + 1;
            while (k < runs.Count && runs[k].Start <= x1 + GoldGapTolerance) k++;
        }

        return AssignLatticeGlyphBounds(cells, rowTop, rowHeight);
    }

    /// <summary>
    /// Places every cell on the row's lattice. Plain cells are drawn exactly on their slot, so
    /// their right edges give the pitch (median difference between consecutive plain right edges,
    /// divided by how many slots apart they are) and the slot size is the icon-row height (cells
    /// are square). A gilded cell's slot is then one pitch from its nearest plain neighbour; with
    /// no usable neighbour the slot is centred on its own frame. Non-gilded cells keep their drawn
    /// box, which already is the slot.
    /// </summary>
    private static List<IconCell> AssignLatticeGlyphBounds(List<IconCell> cells, int rowTop, int rowHeight)
    {
        if (cells.Count == 0) return cells;

        var plain = cells.Select((c, i) => (Cell: c, Index: i)).Where(t => !t.Cell.IsGilded && !t.Cell.Ambiguous).ToList();
        var pitches = new List<int>();
        for (var i = 1; i < plain.Count; i++)
        {
            var slots = plain[i].Index - plain[i - 1].Index;
            var right = plain[i].Cell.Bounds.Right - plain[i - 1].Cell.Bounds.Right;
            if (slots > 0 && right > 0) pitches.Add(right / slots);
        }
        var pitch = pitches.Count > 0 ? Median(pitches) : 0;

        for (var i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            if (!cell.IsGilded && !cell.Ambiguous) continue;

            int right;
            var prevPlain = plain.LastOrDefault(t => t.Index < i);
            var nextPlain = plain.FirstOrDefault(t => t.Index > i);
            if (pitch > 0 && prevPlain.Cell.Bounds.Width > 0)
                right = prevPlain.Cell.Bounds.Right + (pitch * (i - prevPlain.Index));
            else if (pitch > 0 && nextPlain.Cell.Bounds.Width > 0)
                right = nextPlain.Cell.Bounds.Right - (pitch * (nextPlain.Index - i));
            else
                right = cell.Bounds.X + ((cell.Bounds.Width + rowHeight) / 2); // centre the slot on the frame

            var glyph = new Rectangle(right - rowHeight, rowTop, rowHeight, rowHeight);
            cells[i] = cell with { GlyphBounds = glyph };
        }

        return cells;
    }

    /// <summary>
    /// Widens a cell to its own gold border rows: walks outward from the shared top and bottom,
    /// up to <see cref="GoldRefineRatio"/> of the row height, over rows where at least
    /// <see cref="GoldLineCoverage"/> of the cell's columns are gold, tolerating
    /// <see cref="GoldGapTolerance"/> non-gold rows in between. A cell with no gold border keeps
    /// the shared extent, and a yellow glyph inside the cell is never reached.
    /// </summary>
    private static (int Top, int Bottom) RefineToGoldBorder(byte[] rgb, int width, int height, int stride, int x0, int x1, int rowTop, int rowBottom)
    {
        var rowHeight = rowBottom - rowTop + 1;
        var reach = Math.Max(1, (int)Math.Ceiling(rowHeight * GoldRefineRatio));
        var required = (int)Math.Ceiling((x1 - x0 + 1) * GoldLineCoverage);
        bool IsGoldRow(int y)
        {
            return y >= 0 && y < height && GoldCount(rgb, width, stride, x0, x1, y) >= required;
        }

        var top = ExtendWhileGold(rowTop, -1, reach, IsGoldRow);
        var bottom = ExtendWhileGold(rowBottom, +1, reach, IsGoldRow);
        return (top, bottom);
    }

    /// <summary>
    /// Horizontal twin of <see cref="RefineToGoldBorder"/>: widens a cell over contiguous gold
    /// columns just outside its paired border runs, so a gilded cell's thick frame is bounded by
    /// its true outer edge even when anti-aliasing split that edge off the border run. A plain
    /// cell next to a gilded one cannot creep over its neighbour's frame: the parchment gap
    /// between them is wider than <see cref="GoldGapTolerance"/>, and the frame's stray edge run
    /// is consumed by the gilded cell (see the caller).
    /// </summary>
    private static (int X0, int X1) WidenToGoldColumns(byte[] rgb, int width, int stride, int x0, int x1, int rowTop, int rowBottom)
    {
        var rowHeight = rowBottom - rowTop + 1;
        var reach = Math.Max(1, (int)Math.Ceiling(rowHeight * GoldRefineRatio));
        var required = (int)Math.Ceiling(rowHeight * GoldLineCoverage);
        bool IsGoldColumn(int x)
        {
            return x >= 0 && x < width && GoldColumnCount(rgb, stride, x, rowTop, rowBottom) >= required;
        }

        var left = ExtendWhileGold(x0, -1, reach, IsGoldColumn);
        var right = ExtendWhileGold(x1, +1, reach, IsGoldColumn);
        return (left, right);
    }

    /// <summary>Walks from <paramref name="start"/> in <paramref name="step"/> direction and returns the last gold line reached before <see cref="GoldGapTolerance"/> consecutive misses.</summary>
    private static int ExtendWhileGold(int start, int step, int reach, Func<int, bool> isGoldLine)
    {
        var last = start;
        var misses = 0;
        for (var i = 1; i <= reach; i++)
        {
            var pos = start + (step * i);
            if (isGoldLine(pos))
            {
                last = pos;
                misses = 0;
            }
            else if (++misses >= GoldGapTolerance)
            {
                break;
            }
        }
        return last;
    }

    private static int GoldCount(byte[] rgb, int width, int stride, int x0, int x1, int y)
    {
        var count = 0;
        for (var x = Math.Max(0, x0); x <= Math.Min(width - 1, x1); x++)
        {
            var idx = (y * stride) + (x * 3);
            if (idx + 2 < rgb.Length && IsGoldPixel(rgb[idx + 2], rgb[idx + 1], rgb[idx])) count++;
        }
        return count;
    }

    private static int GoldColumnCount(byte[] rgb, int stride, int x, int y0, int y1)
    {
        var count = 0;
        for (var y = Math.Max(0, y0); y <= y1; y++)
        {
            var idx = (y * stride) + (x * 3);
            if (idx + 2 < rgb.Length && IsGoldPixel(rgb[idx + 2], rgb[idx + 1], rgb[idx])) count++;
        }
        return count;
    }

    /// <summary>Contiguous true-runs of <paramref name="flags"/>, with runs closer than <paramref name="mergeGap"/> merged.</summary>
    internal static List<(int Start, int End)> FindRuns(bool[] flags, int mergeGap)
    {
        var runs = new List<(int Start, int End)>();
        var start = -1;
        for (var i = 0; i <= flags.Length; i++)
        {
            var on = i < flags.Length && flags[i];
            if (on)
            {
                if (start < 0) start = i;
                continue;
            }
            if (start < 0) continue;

            if (runs.Count > 0 && start - runs[^1].End <= mergeGap)
                runs[^1] = (runs[^1].Start, i - 1);
            else
                runs.Add((start, i - 1));
            start = -1;
        }
        return runs;
    }

    private static int Median(IEnumerable<int> values)
    {
        var sorted = values.OrderBy(v => v).ToArray();
        return sorted[sorted.Length / 2];
    }

    private static bool IsInkAt(byte[] rgb, int stride, int x, int y)
    {
        var idx = (y * stride) + (x * 3);
        return idx >= 0 && idx + 2 < rgb.Length && IsIconInk(rgb[idx + 2], rgb[idx + 1], rgb[idx]);
    }

    private static bool IsDarkAt(byte[] rgb, int stride, int x, int y)
    {
        var idx = (y * stride) + (x * 3);
        if (idx < 0 || idx + 2 >= rgb.Length) return false;
        var (_, _, v) = ToHsv(rgb[idx + 2], rgb[idx + 1], rgb[idx]);
        return v < DarkLineMaxValue;
    }

    /// <summary>Gold hue band (35-58 degrees) at meaningful saturation and brightness — the gilded border colour.</summary>
    internal static bool IsGoldPixel(byte r, byte g, byte b)
    {
        var (h, s, v) = ToHsv(r, g, b);
        return h is >= 35 and <= 58 && s > 0.35 && v > 0.35;
    }

    internal static byte[] CopyPixels(Bitmap bitmap, out int stride)
    {
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        try
        {
            stride = data.Stride;
            var rgb = new byte[Math.Abs(stride) * bitmap.Height];
            Marshal.Copy(data.Scan0, rgb, 0, rgb.Length);
            return rgb;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    /// <summary>
    /// Classifies a cell's measured <see cref="GoldHueRingProportion"/> against the gilded and
    /// ambiguous-band thresholds — split out from <see cref="SegmentIconCells"/> so the boundary
    /// values (<see cref="AmbiguousGoldRingLow"/>, <see cref="GoldRingThreshold"/>) are directly
    /// unit-testable without needing a rendered cell that happens to measure in the band.
    /// </summary>
    internal static (bool IsGilded, bool Ambiguous) ClassifyGoldRing(double goldHueRingProportion)
    {
        var isGilded = goldHueRingProportion >= GoldRingThreshold;
        var ambiguous = goldHueRingProportion is >= AmbiguousGoldRingLow and < GoldRingThreshold;
        return (isGilded, ambiguous);
    }

    /// <summary>Proportion of the cell's outer border ring falling inside a gold hue+saturation range.</summary>
    internal static double GoldHueRingProportion(byte[] rgb, int width, int stride, Rectangle cell)
    {
        var ringPx = Math.Max(1, (int)(Math.Min(cell.Width, cell.Height) * 0.12));
        var total = 0;
        var gold = 0;
        for (var y = cell.Y; y < cell.Y + cell.Height; y++)
        {
            if (y < 0) continue;
            var nearTop = y - cell.Y < ringPx;
            var nearBottom = cell.Y + cell.Height - 1 - y < ringPx;
            var rowOffset = y * stride;
            for (var x = cell.X; x < cell.X + cell.Width; x++)
            {
                var nearLeft = x - cell.X < ringPx;
                var nearRight = cell.X + cell.Width - 1 - x < ringPx;
                if (!(nearTop || nearBottom || nearLeft || nearRight)) continue;
                if (x < 0 || x >= width) continue;

                var idx = rowOffset + (x * 3);
                if (idx + 2 >= rgb.Length) continue;
                total++;
                if (IsGoldPixel(rgb[idx + 2], rgb[idx + 1], rgb[idx]))
                    gold++;
            }
        }
        return total == 0 ? 0 : (double)gold / total;
    }

    /// <summary>Shrinks a rectangle by <paramref name="ratio"/> of its size on every side.</summary>
    internal static Rectangle Inset(Rectangle rect, double ratio)
    {
        var dx = (int)Math.Round(rect.Width * ratio);
        var dy = (int)Math.Round(rect.Height * ratio);
        return new Rectangle(rect.X + dx, rect.Y + dy, Math.Max(1, rect.Width - (2 * dx)), Math.Max(1, rect.Height - (2 * dy)));
    }

    /// <summary>
    /// Box-filter downscale of a region (plus <paramref name="marginRatio"/> of its size around
    /// it, so a full cell keeps its whole border ring) into a 32x32 RGB sprite. Correctness
    /// requirement, not an optimisation — hashes must be resolution-independent.
    /// </summary>
    internal static byte[] NormalizeTo32x32(byte[] rgb, int width, int stride, Rectangle cell, double marginRatio = 0.08)
    {
        const int dst = 32;
        var margin = marginRatio <= 0 ? 0 : Math.Max(1, (int)(Math.Min(cell.Width, cell.Height) * marginRatio));
        var srcX = Math.Max(0, cell.X - margin);
        var srcY = Math.Max(0, cell.Y - margin);
        var srcW = Math.Min(width - srcX, cell.Width + (margin * 2));
        var srcH = cell.Height + (margin * 2);

        var output = new byte[dst * dst * 3];
        for (var dy = 0; dy < dst; dy++)
        {
            var sy0 = srcY + (dy * srcH / dst);
            var sy1 = Math.Max(sy0 + 1, srcY + ((dy + 1) * srcH / dst));
            for (var dx = 0; dx < dst; dx++)
            {
                var sx0 = srcX + (dx * srcW / dst);
                var sx1 = Math.Max(sx0 + 1, srcX + ((dx + 1) * srcW / dst));

                long r = 0, g = 0, b = 0;
                var n = 0;
                for (var sy = sy0; sy < sy1; sy++)
                {
                    var rowOffset = sy * stride;
                    for (var sx = sx0; sx < sx1; sx++)
                    {
                        var idx = rowOffset + (sx * 3);
                        if (idx + 2 >= rgb.Length) continue;
                        b += rgb[idx];
                        g += rgb[idx + 1];
                        r += rgb[idx + 2];
                        n++;
                    }
                }

                var di = ((dy * dst) + dx) * 3;
                if (n > 0)
                {
                    output[di] = (byte)(r / n);
                    output[di + 1] = (byte)(g / n);
                    output[di + 2] = (byte)(b / n);
                }
            }
        }
        return output;
    }

    /// <summary>
    /// Greyscale 8x8 difference-hash (64 bits) of a 32x32 RGB sprite produced by
    /// <see cref="NormalizeTo32x32"/> — for identity, the glyph-interior sprite (see
    /// <see cref="GlyphInsetRatio"/>), never the full-cell sprite.
    /// </summary>
    internal static ulong ComputeDHash(byte[] sprite32Rgb)
    {
        const int srcDim = 32;
        const int dstW = 9;
        const int dstH = 8;

        var gray = new byte[dstW * dstH];
        for (var dy = 0; dy < dstH; dy++)
        {
            var sy0 = dy * srcDim / dstH;
            var sy1 = Math.Max(sy0 + 1, (dy + 1) * srcDim / dstH);
            for (var dx = 0; dx < dstW; dx++)
            {
                var sx0 = dx * srcDim / dstW;
                var sx1 = Math.Max(sx0 + 1, (dx + 1) * srcDim / dstW);

                long sum = 0;
                var n = 0;
                for (var sy = sy0; sy < sy1; sy++)
                {
                    for (var sx = sx0; sx < sx1; sx++)
                    {
                        var idx = ((sy * srcDim) + sx) * 3;
                        var r = sprite32Rgb[idx];
                        var g = sprite32Rgb[idx + 1];
                        var b = sprite32Rgb[idx + 2];
                        sum += ((77 * r) + (150 * g) + (29 * b)) >> 8;
                        n++;
                    }
                }
                gray[(dy * dstW) + dx] = n > 0 ? (byte)(sum / n) : (byte)0;
            }
        }

        ulong hash = 0;
        var bit = 0;
        for (var y = 0; y < dstH; y++)
        {
            for (var x = 0; x < dstW - 1; x++)
            {
                if (gray[(y * dstW) + x] < gray[(y * dstW) + x + 1])
                    hash |= 1UL << bit;
                bit++;
            }
        }
        return hash;
    }

    /// <summary>
    /// Dominant hue bucket (12 buckets of 30 degrees) among glyph-stroke pixels of the
    /// glyph-interior sprite (see <see cref="GlyphInsetRatio"/>) — pixels that stand out from the
    /// surrounding cell fill, not the whole cell (which is mostly parchment and would otherwise
    /// resolve to beige for every rune). A glyph drawn in near-black ink votes only through its
    /// anti-aliased edges, which carry the parchment's own warm hue (bucket 1) — consistent, since
    /// the parchment colour is fixed, but not a property of the rune itself.
    /// </summary>
    internal static int DominantGlyphHueBucket(byte[] sprite32Rgb)
    {
        const int dim = 32;
        const int buckets = 12;
        var counts = new int[buckets];

        // The caller passes the glyph interior, so only a thin edge is excluded here as a guard
        // against a border pixel surviving the inset on a small cell.
        var margin = (int)(dim * 0.05);
        for (var y = margin; y < dim - margin; y++)
        {
            for (var x = margin; x < dim - margin; x++)
            {
                var idx = ((y * dim) + x) * 3;
                byte r = sprite32Rgb[idx], g = sprite32Rgb[idx + 1], b = sprite32Rgb[idx + 2];
                if (!IsIconInk(r, g, b)) continue;

                var (h, s, v) = ToHsv(r, g, b);
                if (s < 0.15 || v < 0.05) continue; // near-black/near-grey ink carries no usable hue
                var bucket = ((int)(h / 30.0)) % buckets;
                counts[bucket]++;
            }
        }

        var best = 0;
        for (var i = 1; i < buckets; i++)
            if (counts[i] > counts[best]) best = i;
        return best;
    }

    /// <summary>
    /// True for pixels that stand out from the pale, low-saturation parchment background:
    /// either saturated colour (icon art / gilded tabs) or notably dark (glyph ink, border
    /// lines). Deliberately resolution- and profile-independent — no absolute pixel offsets.
    /// </summary>
    internal static bool IsIconInk(byte r, byte g, byte b)
    {
        var (_, s, v) = ToHsv(r, g, b);
        return s > 0.28 || v < 0.35;
    }

    internal static (double Hue, double Saturation, double Value) ToHsv(byte r, byte g, byte b)
    {
        double rd = r / 255.0, gd = g / 255.0, bd = b / 255.0;
        var max = Math.Max(rd, Math.Max(gd, bd));
        var min = Math.Min(rd, Math.Min(gd, bd));
        var delta = max - min;

        double hue;
        if (delta < 1e-9) hue = 0;
        else if (max == rd) hue = 60 * (((gd - bd) / delta) % 6);
        else if (max == gd) hue = 60 * (((bd - rd) / delta) + 2);
        else hue = 60 * (((rd - gd) / delta) + 4);
        if (hue < 0) hue += 360;

        var saturation = max < 1e-9 ? 0 : delta / max;
        return (hue, saturation, max);
    }
}
