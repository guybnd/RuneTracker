using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using RuneshapePriceChecker.Configuration;
using RuneshapePriceChecker.Contracts;

namespace RuneshapePriceChecker.Runes;

/// <summary>How a gilded rune is marked on the panel.</summary>
public enum RuneMarkerKind
{
    /// <summary>Already carried this run — picking it again is redundant.</summary>
    Carried,
    /// <summary>New and worth having.</summary>
    Valuable,
    /// <summary>New and at or above <see cref="RunesOptions.HighValueWeight"/>.</summary>
    HighValue
}

/// <summary>One detected gilded rune, scored.</summary>
public sealed record RuneKeyScore(
    RuneKey Key,
    string BindingId,
    string? RuneId,
    string DisplayName,
    double Weight,
    bool IsCarried,
    bool IsUnbound,
    bool IsTopPick,
    RuneMarkerKind Marker);

/// <summary>
/// One Combinations row, scored: the sum of the weights of its new (not carried) runes.
/// <see cref="IsBest"/> marks the single recommended row, not every row tied at the top.
/// </summary>
public sealed record RuneRowScore(int RowY, IReadOnlyList<RuneKeyScore> Keys, double Score, bool IsBest);

/// <summary>All rows of one snapshot, scored against the catalog at one revision.</summary>
public sealed record RuneScoreSheet(IReadOnlyList<RuneRowScore> Rows, long CatalogRevision)
{
    public static readonly RuneScoreSheet Empty = new([], 0);

    public IEnumerable<RuneKeyScore> AllKeys => Rows.SelectMany(r => r.Keys);

    public bool HasKeys => Rows.Any(r => r.Keys.Count > 0);

    /// <summary>Cheap change signature for re-render gating: cells, markers, badges and the catalog revision.</summary>
    public string Signature()
    {
        var sb = new StringBuilder();
        _ = sb.Append(CatalogRevision.ToString(CultureInfo.InvariantCulture)).Append('|');
        foreach (var row in Rows)
        {
            _ = sb.Append(row.RowY).Append(':');
            foreach (var k in row.Keys)
            {
                _ = sb.Append(k.Key.CellBounds.X).Append(',').Append(k.Key.CellBounds.Y).Append(',')
                      .Append(k.Key.CellBounds.Width).Append(',').Append(k.Key.CellBounds.Height).Append(',')
                      .Append((int)k.Marker).Append(k.IsTopPick ? 'T' : '-').Append(k.IsUnbound ? '?' : '-').Append(';');
            }
            _ = sb.Append('|');
        }
        return sb.ToString();
    }
}

/// <summary>
/// Scores each row's gilded runes against the catalog and the carried set:
/// <c>rowScore = Σ weight(rune)</c> over runes not already carried (deduped by rune id).
///
/// Exactly one badge is awarded per screen. The game's choice is a row — you take a row whole —
/// so the recommendation is a row first and a rune second: the highest-scoring row, then the
/// rune inside it that earned the score. Ties are broken rather than shared, because two rows
/// worth the same are interchangeable and asking the user to compare them wastes the time the
/// badge exists to save.
/// </summary>
public sealed class RuneRowScorer(RuneCatalog catalog, IOptionsMonitor<RunesOptions> options, RuneCombinationTable? combinations = null)
{
    public RuneScoreSheet Score(LeagueWindowSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return Score(snapshot.RuneRows);
    }

    /// <summary>
    /// Scores rows given directly rather than off a snapshot, so the caller can score the rows it
    /// latched when the panel opened instead of whatever the current frame happened to read. The
    /// scoring itself still runs every cycle: weights and the carried set change under the user's
    /// hands, and those must take effect without waiting for the panel to be re-read.
    /// </summary>
    public RuneScoreSheet Score(IReadOnlyList<RuneRowKeys>? runeRows)
    {
        if (runeRows is null || runeRows.Count == 0)
            return new RuneScoreSheet([], catalog.Revision);

        var high = options.CurrentValue.HighValueWeight;
        var revision = catalog.Revision;

        // First pass: resolve every key.
        var resolved = new List<(int RowY, List<(RuneKey Key, RuneResolution Res)> Keys)>(runeRows.Count);
        foreach (var row in runeRows)
        {
            var keys = new List<(RuneKey, RuneResolution)>(row.Keys.Count);
            foreach (var key in row.Keys)
            {
                // The row's name plus the cell's position names the rune outright (RUNE-24); the
                // hash only decides when the table cannot.
                var known = combinations?.Lookup(row.ItemName, key.CellCount, key.CellIndex);
                keys.Add((key, catalog.Resolve(key, known)));
            }
            resolved.Add((row.RowY, keys));
        }

        // The badge says "better than the alternatives on screen", so it is only awarded when the
        // comparison is informed. With nothing bound yet every sprite scores the same unknown
        // weight, and picking one of them would invent a preference the library does not hold
        // (RUNE-4).
        var newWeights = resolved.SelectMany(r => r.Keys).Where(k => !k.Res.IsCarried).Select(k => k.Res.Weight);
        var informed = newWeights.Select(w => Math.Round(w, 6)).Distinct().Count() >= 2;

        var rows = new List<RuneRowScore>(resolved.Count);
        foreach (var (rowY, keys) in resolved)
        {
            var scored = new List<RuneKeyScore>(keys.Count);
            var counted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var score = 0.0;
            foreach (var (key, res) in keys)
            {
                var marker = res.IsCarried ? RuneMarkerKind.Carried
                    : res.Weight > high ? RuneMarkerKind.HighValue
                    : RuneMarkerKind.Valuable;
                scored.Add(new RuneKeyScore(key, res.BindingId, res.Rune?.Id, res.DisplayName, res.Weight, res.IsCarried, res.IsUnbound, IsTopPick: false, marker));
                if (!res.IsCarried && counted.Add(res.CarriedId))
                    score += res.Weight;
            }
            rows.Add(new RuneRowScore(rowY, scored, score, IsBest: false));
        }

        if (informed) Recommend(rows);
        return new RuneScoreSheet(rows, revision);
    }

    /// <summary>
    /// Marks the one recommended row and badges the one rune in it that earned the recommendation.
    /// No-op when no row has anything new in it.
    /// </summary>
    private static void Recommend(List<RuneRowScore> rows)
    {
        var pick = -1;
        for (var i = 0; i < rows.Count; i++)
            if (rows[i].Score > 0 && (pick < 0 || Beats(rows[i], rows[pick])))
                pick = i;
        if (pick < 0) return;

        var row = rows[pick];
        var badge = -1;
        for (var i = 0; i < row.Keys.Count; i++)
        {
            if (row.Keys[i].IsCarried) continue;
            if (badge < 0 || row.Keys[i].Weight > row.Keys[badge].Weight
                || (Math.Abs(row.Keys[i].Weight - row.Keys[badge].Weight) < 1e-9
                    && row.Keys[i].Key.CellBounds.X < row.Keys[badge].Key.CellBounds.X))
                badge = i;
        }
        if (badge < 0) return;

        var keys = row.Keys.ToArray();
        keys[badge] = keys[badge] with { IsTopPick = true };
        rows[pick] = row with { Keys = keys, IsBest = true };
    }

    /// <summary>
    /// Whether <paramref name="candidate"/> is the better recommendation. Score first; then the
    /// single best rune in it, so a row carrying Opulent wins over one that reached the same total
    /// out of lesser runes; then the topmost row, so the answer is stable frame to frame.
    /// </summary>
    private static bool Beats(RuneRowScore candidate, RuneRowScore current)
    {
        if (Math.Abs(candidate.Score - current.Score) > 1e-9) return candidate.Score > current.Score;
        var candidateBest = BestNewWeight(candidate);
        var currentBest = BestNewWeight(current);
        if (Math.Abs(candidateBest - currentBest) > 1e-9) return candidateBest > currentBest;
        return candidate.RowY < current.RowY;
    }

    private static double BestNewWeight(RuneRowScore row)
    {
        var best = 0.0;
        foreach (var key in row.Keys)
            if (!key.IsCarried && key.Weight > best) best = key.Weight;
        return best;
    }
}
