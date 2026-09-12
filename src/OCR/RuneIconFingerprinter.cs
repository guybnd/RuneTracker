using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using RuneshapePriceChecker.Contracts;

namespace RuneshapePriceChecker.OCR;

/// <summary>
/// Extracts and fingerprints gilded (succession) runes from the icon strip in each
/// Combinations row. Confirmed against a real 2560x1440 capture
/// (tests/fixtures/runeicons/2560x1440/1 Raw.png) that the panel actually uses TWO
/// different row layouts, not one: wide rows (5-6 icons) wrap their name text onto a
/// separate line BELOW the icon strip, while narrower rows (2-4 icons) keep the name
/// text BESIDE the icons on the same line. The row-text pipeline's rowY/rowHeight
/// describe only the text glyphs (scanned right of PanelLeftFraction) in both cases,
/// so they cannot be trusted as the icon cell's own Y bounds either way — they are
/// used here purely to bound a search window between consecutive text rows, in which
/// the icon strip is then located directly from pixels.
///
/// Pure by design: bitmap in, keys out. No OcrResolutionProfiles entry, no capture
/// path changes — icon geometry is detected from pixels every cycle, never assumed.
///
/// KNOWN LIMITATION (spike evidence, not yet resolved): on the one real fixture available,
/// the gold-hue-ring metric cleanly separates gilded from non-gilded in rows where cell
/// segmentation lands exactly on the icon's true boundary (measured 0.15-0.28 for gilded vs
/// 0.00-0.08 for non-gilded), but in rows where a fused multi-icon segment gets split by the
/// band-height-based estimate, the split boundary can clip enough of the gold ring that the
/// measured proportion drops to 0.09-0.14 — inside, or below, GoldRingThreshold. This is a
/// segmentation-precision gap, not a metric-validity gap. Fail-soft holds either way (a missed
/// gilded cell under-reports; it never mis-reports a wrong key), but the ticket's (b) gate
/// (zero overlap, >=30 samples/class, >=2 profiles) is NOT met yet — see the RUNE-1 ticket's
/// spike-gate comment for the honest per-check verdict.
/// </summary>
internal static class RuneIconFingerprinter
{
    internal const int MinCellPx = 24;

    /// <summary>
    /// Gold hue-ring proportion at/above which a cell is classified gilded. Calibrated
    /// against the real 2560x1440 fixture (tests/fixtures/runeicons): isolated gold-tabbed
    /// cells measured 0.23-0.24, non-gilded cells 0.00-0.07. The plan's original "tabs poke
    /// above the row's typical top" structural signal was tried first and DISCARDED — real
    /// pixel data showed it doesn't discriminate reliably (the true gilded cell sometimes
    /// measured a *lower* top than its row-mates); the hue-ring proportion is what actually
    /// separates the classes cleanly, so it is now the sole gate. TopProtrusionPx is still
    /// computed and carried on IconCell for diagnostics, not classification.
    /// </summary>
    internal const double GoldRingThreshold = 0.15;
    internal const double AmbiguousGoldRingLow = 0.09;

    /// <summary>See the <c>rowTextHeight</c> parameter doc on <see cref="DetectIconBand"/>.</summary>
    internal const double IconToTextHeightRatio = 1.9;

    internal readonly record struct IconCell(Rectangle Bounds, double TopProtrusionPx, double GoldHueRingProportion, bool IsGilded, bool Ambiguous);

    /// <summary>
    /// Runs the full pipeline for the gap between two consecutive text rows: locate the
    /// icon band somewhere in that gap (covering both the "icons above text" and "icons
    /// beside text" layouts), segment cells, classify gilded, and hash the survivors.
    /// Never throws — a skip predicate firing logs once and yields no keys for the row.
    /// </summary>
    /// <param name="rawBitmap">The raw (unpreprocessed) captured frame.</param>
    /// <param name="searchTop">Bottom Y of the previous text row (or 0 for the first row).</param>
    /// <param name="searchBottomExclusive">Top Y of the next text row (or image height for the last row).</param>
    /// <param name="rowTextHeight">This row's own detected text-line height — used only to cap
    /// the icon band height (see <see cref="IconToTextHeightRatio"/>), never as the icon's bounds.</param>
    /// <param name="logger">Optional logger for skip-predicate diagnostics.</param>
    public static IReadOnlyList<RuneKey> ExtractRowKeys(
        Bitmap rawBitmap, int searchTop, int searchBottomExclusive, int rowTextHeight, ILogger? logger = null)
    {
        if (rawBitmap is null) return [];
        var width = rawBitmap.Width;
        var height = rawBitmap.Height;
        var clampedTop = Math.Max(0, Math.Min(searchTop, height));
        var clampedBottom = Math.Max(clampedTop, Math.Min(searchBottomExclusive, height));
        var windowHeight = clampedBottom - clampedTop;
        if (windowHeight < MinCellPx)
        {
            logger?.LogDebug("RuneIconFingerprinter: skip — search window [{Top},{Bottom}) is {H}px < MinCellPx", clampedTop, clampedBottom, windowHeight);
            return [];
        }

        var rect = new Rectangle(0, 0, width, height);
        var data = rawBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        byte[] rgb;
        int stride;
        try
        {
            stride = data.Stride;
            rgb = new byte[Math.Abs(stride) * height];
            Marshal.Copy(data.Scan0, rgb, 0, rgb.Length);
        }
        finally
        {
            rawBitmap.UnlockBits(data);
        }

        var band = DetectIconBand(rgb, width, stride, clampedTop, clampedBottom, rowTextHeight);
        if (band is null || band.Value.Height < MinCellPx)
        {
            logger?.LogDebug("RuneIconFingerprinter: skip — no icon band found in [{Top},{Bottom})", clampedTop, clampedBottom);
            return [];
        }

        var cells = SegmentIconCells(rgb, width, stride, band.Value, clampedTop);
        if (cells.Count == 0) return [];

        var keys = new List<RuneKey>();
        foreach (var cell in cells)
        {
            if (cell.Bounds.Width < MinCellPx || cell.Bounds.Height < MinCellPx)
                continue; // degenerate segment (noise) — never counted as a real icon

            if (cell.Bounds.Width > band.Value.Height * 1.8)
            {
                logger?.LogDebug("RuneIconFingerprinter: dropping fused cell @X={X} W={W} — wider than 1.8x band height", cell.Bounds.X, cell.Bounds.Width);
                continue;
            }

            if (cell.Ambiguous)
            {
                logger?.LogDebug(
                    "RuneIconFingerprinter: cell @X={X} border metric ambiguous (protrusion={P:F1}px, goldRing={G:F2}) — dropped as non-gilded",
                    cell.Bounds.X, cell.TopProtrusionPx, cell.GoldHueRingProportion);
                continue;
            }

            if (!cell.IsGilded) continue;

            var sprite = NormalizeTo32x32(rgb, width, stride, cell.Bounds);
            var hash = ComputeDHash(sprite);
            var hue = DominantGlyphHueBucket(sprite);
            keys.Add(new RuneKey(hash, hue, sprite));
        }

        return keys;
    }

    /// <summary>
    /// Finds the FIRST (topmost) contiguous band of "icon ink" (see <see cref="IsIconInk"/>)
    /// across the full row width within [searchTop, searchBottomExclusive). The window spans
    /// the whole gap between two consecutive text rows, so it may contain either just the
    /// icon strip (icons-above-text layout — a gap, then the text row, follow) or the icon
    /// strip fused with its own name text (icons-beside-text layout, where both live on one
    /// line) — either way the icon strip itself is always the first band encountered scanning
    /// top-to-bottom, so taking the first region (not a naive first-hit-to-last-hit span) is
    /// what keeps the "icons above" case from swallowing that row's own text line too.
    /// Returns null when no such band exists in the window (degenerate row, no icons this cycle).
    /// </summary>
    /// <param name="rgb">Raw BGR pixel bytes of the captured frame (GDI+ Format24bppRgb layout).</param>
    /// <param name="width">Frame width in pixels.</param>
    /// <param name="stride">Row stride in bytes, as reported by LockBits.</param>
    /// <param name="searchTop">Inclusive top Y of the search window.</param>
    /// <param name="searchBottomExclusive">Exclusive bottom Y of the search window.</param>
    /// <param name="rowTextHeight">
    /// This row's own detected text-line height, or 0 to skip capping. Icon art and text glyphs
    /// are rendered by the same UI at the same scale, so the icon cell's true height is a
    /// roughly fixed multiple of that row's OWN text height regardless of resolution — this is
    /// what keeps the "icons beside text" layout (where the ink region found above doesn't stop
    /// until the text row itself, since both live on one line with no clean gap between them)
    /// from returning an oversized band that then wrecks every downstream ink-density threshold
    /// (all of which scale off band height). Calibrated to ~1.9x on the real 2560x1440 fixture.
    /// </param>
    internal static Rectangle? DetectIconBand(byte[] rgb, int width, int stride, int searchTop, int searchBottomExclusive, int rowTextHeight = 0)
    {
        var windowHeight = searchBottomExclusive - searchTop;
        if (windowHeight <= 0) return null;

        var widthThreshold = Math.Max(2, (int)(width * 0.02));

        var inkCounts = new int[windowHeight];
        for (var y = 0; y < windowHeight; y++)
        {
            var rowOffset = (searchTop + y) * stride;
            var count = 0;
            for (var x = 0; x < width; x++)
            {
                var idx = rowOffset + (x * 3);
                if (IsIconInk(rgb[idx + 2], rgb[idx + 1], rgb[idx]))
                    count++;
            }
            inkCounts[y] = count;
        }

        List<(int Start, int End)> regions = [];
        var inRegion = false;
        var regionStart = 0;
        for (var y = 0; y < windowHeight; y++)
        {
            if (inkCounts[y] >= widthThreshold)
            {
                if (!inRegion) { regionStart = y; inRegion = true; }
            }
            else if (inRegion)
            {
                regions.Add((regionStart, y - 1));
                inRegion = false;
            }
        }
        if (inRegion) regions.Add((regionStart, windowHeight - 1));
        if (regions.Count == 0) return null;

        // Merge regions separated by a hairline gap (paper-texture noise between icon rows and
        // shadow lines), same 10px rule OcrPipeline.DetectRowPositions uses for text rows.
        var merged = new List<(int Start, int End)> { regions[0] };
        for (var r = 1; r < regions.Count; r++)
        {
            if (regions[r].Start - merged[^1].End <= 10)
                merged[^1] = (merged[^1].Start, regions[r].End);
            else
                merged.Add(regions[r]);
        }

        var first = merged[0];
        var firstHeight = first.End - first.Start + 1;
        if (rowTextHeight > 0)
        {
            var cap = (int)Math.Round(rowTextHeight * IconToTextHeightRatio);
            if (firstHeight > cap) firstHeight = cap;
        }
        return new Rectangle(0, searchTop + first.Start, width, firstHeight);
    }

    /// <summary>
    /// Icons never reach past ~46% of the panel width even on the widest observed row (RUNE-1
    /// fixture findings); capping the column scan at 50% keeps a text glyph or an oversized
    /// (icons-beside-text) band from ever entering the icon-column scan, without hardcoding a
    /// per-resolution offset — it is a ratio of the already-known capture width, same idiom as
    /// OcrOptions.PanelLeftFraction.
    /// </summary>
    internal const double MaxIconStripFraction = 0.50;

    /// <summary>
    /// Segments the icon band into left-to-right cell candidates via column ink-density,
    /// then recomputes each cell's OWN top ink Y (independent of the shared band top) so a
    /// gilded cell's tabs — which protrude above its row-mates' top edge — are visible as a
    /// per-cell outlier rather than absorbed into the row's union bounding box.
    /// </summary>
    internal static IReadOnlyList<IconCell> SegmentIconCells(byte[] rgb, int width, int stride, Rectangle band, int searchTop)
    {
        var scanWidth = Math.Max(1, (int)(width * MaxIconStripFraction));
        var colInk = new int[width];
        for (var x = 0; x < scanWidth; x++)
        {
            var count = 0;
            for (var y = band.Y; y < band.Y + band.Height; y++)
            {
                var idx = (y * stride) + (x * 3);
                if (IsIconInk(rgb[idx + 2], rgb[idx + 1], rgb[idx]))
                    count++;
            }
            colInk[x] = count;
        }

        var colThreshold = Math.Max(1, (int)(band.Height * 0.25));
        List<(int Start, int End)> segments = [];
        var inSeg = false;
        var segStart = 0;
        for (var x = 0; x < width; x++)
        {
            if (colInk[x] >= colThreshold)
            {
                if (!inSeg) { segStart = x; inSeg = true; }
            }
            else if (inSeg)
            {
                segments.Add((segStart, x - 1));
                inSeg = false;
            }
        }
        if (inSeg) segments.Add((segStart, width - 1));

        // Merge segments separated by a hairline gap (anti-aliased cell border shadow).
        var rawMerged = new List<(int Start, int End)>();
        foreach (var seg in segments)
        {
            if (rawMerged.Count > 0 && seg.Start - rawMerged[^1].End <= 3)
                rawMerged[^1] = (rawMerged[^1].Start, seg.End);
            else
                rawMerged.Add(seg);
        }

        // Icons are left-packed at a fixed pitch with only a hairline gap between them; the
        // name text (whether beside the icons on the same line, or on the row's own text line
        // captured inside this same widened band) starts after a much larger blank gap. Walk
        // segments left-to-right and stop at the first oversized gap — this is what keeps
        // individual text-glyph strokes from ever being considered as cell candidates,
        // regardless of the icons-above-text vs icons-beside-text layout.
        var stripEndGapPx = Math.Max(MinCellPx, band.Height * 0.6);
        var strip = new List<(int Start, int End)>();
        foreach (var seg in rawMerged)
        {
            if (strip.Count > 0 && seg.Start - strip[^1].End > stripEndGapPx)
                break;
            strip.Add(seg);
        }

        // Cells are roughly square, so a real cell's width tracks the band height, not an
        // absolute pixel constant. Drop anything too thin to plausibly be one cell (paper
        // texture, a stray glyph descender that snuck under the gap threshold).
        var minCellWidth = Math.Max(MinCellPx / 2, (int)(band.Height * 0.6));
        var candidates = strip.Where(s => s.End - s.Start + 1 >= minCellWidth).ToList();
        if (candidates.Count == 0) return [];

        // Two adjacent cells occasionally fuse into one column-ink segment (a bright glyph
        // near a shared edge keeps the gap column above threshold), sometimes more than one
        // pair — so no fixed count of "expected fusions" can be assumed, and inferring a
        // typical width from the noisy candidate widths themselves (first/narrowest/median/
        // clustered) proved unreliable across rows. Cells are roughly square, and the band
        // height is now a trustworthy estimate of the true cell size (capped off the row's own
        // text height in DetectIconBand) — use it directly as the split unit instead.
        var typicalWidth = band.Height;

        var merged = new List<(int Start, int End)>();
        foreach (var (start, end) in candidates)
        {
            var segWidth = end - start + 1;
            var pieces = typicalWidth > 0 ? Math.Max(1, (int)Math.Round(segWidth / (double)typicalWidth)) : 1;
            if (pieces <= 1 || segWidth < typicalWidth * 1.4)
            {
                merged.Add((start, end));
                continue;
            }
            var pieceWidth = segWidth / (double)pieces;
            for (var p = 0; p < pieces; p++)
            {
                var pStart = start + (int)Math.Round(p * pieceWidth);
                var pEnd = p == pieces - 1 ? end : start + (int)Math.Round((p + 1) * pieceWidth) - 1;
                merged.Add((pStart, pEnd));
            }
        }

        // Per-cell own top/bottom ink Y, searched over the FULL window (not clipped to the
        // shared band) so a tabbed cell's protrusion above the row's typical top is captured.
        var ownTops = new int[merged.Count];
        var ownBottoms = new int[merged.Count];
        for (var i = 0; i < merged.Count; i++)
        {
            var (x0, x1) = merged[i];
            var top = -1;
            var bottom = -1;
            for (var y = searchTop; y < band.Y + band.Height; y++)
            {
                var rowOffset = y * stride;
                var any = false;
                for (var x = x0; x <= x1; x++)
                {
                    var idx = rowOffset + (x * 3);
                    if (IsIconInk(rgb[idx + 2], rgb[idx + 1], rgb[idx])) { any = true; break; }
                }
                if (any)
                {
                    if (top < 0) top = y;
                    bottom = y;
                }
            }
            ownTops[i] = top < 0 ? band.Y : top;
            ownBottoms[i] = bottom < 0 ? band.Y + band.Height - 1 : bottom;
        }

        var sortedTops = (int[])ownTops.Clone();
        Array.Sort(sortedTops);
        var medianTop = sortedTops[sortedTops.Length / 2];

        var cells = new List<IconCell>(merged.Count);
        for (var i = 0; i < merged.Count; i++)
        {
            var (x0, x1) = merged[i];
            var cellHeight = ownBottoms[i] - ownTops[i] + 1;
            var boundsRect = new Rectangle(x0, ownTops[i], x1 - x0 + 1, cellHeight);

            var protrusion = medianTop - ownTops[i]; // diagnostic only — see GoldRingThreshold doc-comment
            var goldRing = GoldHueRingProportion(rgb, width, stride, boundsRect);

            var ambiguous = goldRing >= AmbiguousGoldRingLow && goldRing < GoldRingThreshold;
            var isGilded = goldRing >= GoldRingThreshold;

            cells.Add(new IconCell(boundsRect, protrusion, goldRing, isGilded, ambiguous));
        }

        return cells;
    }

    /// <summary>Proportion of the cell's outer border ring falling inside a gold hue+saturation range.</summary>
    internal static double GoldHueRingProportion(byte[] rgb, int width, int stride, Rectangle cell)
    {
        var ringPx = Math.Max(1, (int)(Math.Min(cell.Width, cell.Height) * 0.12));
        var total = 0;
        var gold = 0;
        for (var y = cell.Y; y < cell.Y + cell.Height; y++)
        {
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
                var (h, s, v) = ToHsv(rgb[idx + 2], rgb[idx + 1], rgb[idx]);
                if (h is >= 35 and <= 58 && s > 0.35 && v > 0.35)
                    gold++;
            }
        }
        return total == 0 ? 0 : (double)gold / total;
    }

    /// <summary>
    /// Box-filter downscale of the cell (plus a small margin so the full border ring survives)
    /// into a 32x32 RGB sprite. Correctness requirement, not an optimisation — hashes must be
    /// resolution-independent.
    /// </summary>
    internal static byte[] NormalizeTo32x32(byte[] rgb, int width, int stride, Rectangle cell)
    {
        const int dst = 32;
        var margin = Math.Max(1, (int)(Math.Min(cell.Width, cell.Height) * 0.08));
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

    /// <summary>Greyscale 8x8 difference-hash (64 bits) of a 32x32 RGB sprite produced by <see cref="NormalizeTo32x32"/>.</summary>
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
    /// Dominant hue bucket (12 buckets of 30 degrees) among glyph-stroke pixels — pixels that
    /// stand out from the surrounding cell fill — not the whole cell (which is mostly parchment
    /// and would otherwise resolve to beige for every rune).
    /// </summary>
    internal static int DominantGlyphHueBucket(byte[] sprite32Rgb)
    {
        const int dim = 32;
        const int buckets = 12;
        var counts = new int[buckets];

        // Exclude the outer ~15% (border ring) — only the interior carries glyph strokes.
        var margin = (int)(dim * 0.15);
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
