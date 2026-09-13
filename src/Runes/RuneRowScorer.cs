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

/// <summary>One Combinations row, scored: the sum of the weights of its new (not carried) runes.</summary>
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
/// <c>rowScore = Σ weight(rune)</c> over runes not already carried (deduped by rune id), the
/// best row(s) are those with the maximum positive score, and the top pick is the
/// highest-weight new rune on screen (ties share the badge).
/// </summary>
public sealed class RuneRowScorer(RuneCatalog catalog, IOptionsMonitor<RunesOptions> options)
{
    public RuneScoreSheet Score(LeagueWindowSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.RuneRows is null || snapshot.RuneRows.Count == 0)
            return new RuneScoreSheet([], catalog.Revision);

        var high = options.CurrentValue.HighValueWeight;
        var revision = catalog.Revision;

        // First pass: resolve every key.
        var resolved = new List<(int RowY, List<(RuneKey Key, RuneResolution Res)> Keys)>(snapshot.RuneRows.Count);
        foreach (var row in snapshot.RuneRows)
        {
            var keys = new List<(RuneKey, RuneResolution)>(row.Keys.Count);
            foreach (var key in row.Keys)
                keys.Add((key, catalog.Resolve(key)));
            resolved.Add((row.RowY, keys));
        }

        // The badge means "better than the alternatives on screen", so it is only awarded when the
        // best new rune actually beats a runner-up. With nothing bound yet every sprite scores the
        // same unknown weight, and starring all of them would say nothing.
        var newWeights = resolved.SelectMany(r => r.Keys).Where(k => !k.Res.IsCarried).Select(k => k.Res.Weight).ToList();
        var distinctWeights = newWeights.Select(w => Math.Round(w, 6)).Distinct().OrderByDescending(w => w).ToList();
        var topWeight = distinctWeights.Count >= 2 ? distinctWeights[0] : 0;

        var rows = new List<RuneRowScore>(resolved.Count);
        foreach (var (rowY, keys) in resolved)
        {
            var scored = new List<RuneKeyScore>(keys.Count);
            var counted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var score = 0.0;
            foreach (var (key, res) in keys)
            {
                var isTop = !res.IsCarried && topWeight > 0 && Math.Abs(res.Weight - topWeight) < 1e-9;
                var marker = res.IsCarried ? RuneMarkerKind.Carried
                    : res.Weight >= high ? RuneMarkerKind.HighValue
                    : RuneMarkerKind.Valuable;
                scored.Add(new RuneKeyScore(key, res.BindingId, res.Rune?.Id, res.DisplayName, res.Weight, res.IsCarried, res.IsUnbound, isTop, marker));
                if (!res.IsCarried && counted.Add(res.CarriedId))
                    score += res.Weight;
            }
            rows.Add(new RuneRowScore(rowY, scored, score, IsBest: false));
        }

        var max = rows.Count > 0 ? rows.Max(r => r.Score) : 0;
        if (max > 0)
        {
            for (var i = 0; i < rows.Count; i++)
                if (Math.Abs(rows[i].Score - max) < 1e-9)
                    rows[i] = rows[i] with { IsBest = true };
        }

        return new RuneScoreSheet(rows, revision);
    }
}
