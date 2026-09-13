using System.Globalization;

namespace RuneshapePriceChecker.App.Dashboard;

/// <summary>
/// The rune priority ladder: an ordered list of the runes you actually want, most wanted first,
/// with everything else sharing one baseline weight underneath it.
///
/// This replaces the named levels the library used to offer (Ignore / Low / Normal / Wanted /
/// Must have). Those asked the wrong question. The user does not think "Death is Wanted and Bond
/// is Wanted"; they think "Death before Bond". A ladder says that directly, and the levels could
/// not say it at all — two runes on the same level are indistinguishable, so the scorer had no
/// basis to recommend one row over another.
///
/// Weight is derived from position rather than typed: each rung is <see cref="Step"/> heavier
/// than the rung below, and the bottom rung sits <see cref="Step"/> above <see cref="BaseWeight"/>.
/// So the ladder alone decides every weight, and reordering in the UI is the only thing that
/// changes them. Runs of equal weights cannot appear by accident, which is what makes a single
/// recommendation possible (see <c>RuneRowScorer</c>).
/// </summary>
public static class RuneRanking
{
    /// <summary>What an unranked rune is worth. Also the floor the ladder is measured from.</summary>
    public const double BaseWeight = 2.0;

    /// <summary>How much heavier each rung is than the one below it.</summary>
    public const double Step = 2.0;

    /// <summary>True when a weight puts a rune on the ladder rather than in the baseline crowd.</summary>
    public static bool IsRanked(double weight) => weight > BaseWeight + (Step / 2);

    /// <summary>
    /// Weight for the rung at <paramref name="index"/> of a ladder <paramref name="count"/> deep.
    /// Index 0 is the top. The bottom rung is <see cref="BaseWeight"/> + <see cref="Step"/>.
    /// </summary>
    public static double WeightAt(int index, int count) => BaseWeight + (Step * (count - index));

    /// <summary>
    /// The ladder implied by a set of current weights: every ranked rune, heaviest first.
    /// Ties break on display name so that a library saved with duplicate weights — an older
    /// version's, or a hand-edited file — still produces one stable order rather than flickering.
    /// </summary>
    public static IReadOnlyList<string> LadderOf(IEnumerable<RuneLibraryEntryView> runes)
    {
        ArgumentNullException.ThrowIfNull(runes);
        return runes.Where(r => IsRanked(r.Weight))
            .OrderByDescending(r => r.Weight)
            .ThenBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(r => r.Id)
            .ToList();
    }

    /// <summary>
    /// Display order for the library: the ladder from the top, then everyone else alphabetically.
    /// Sorting by weight alone would leave the baseline crowd in an arbitrary order, and the crowd
    /// is 25 of the 34 rows.
    /// </summary>
    public static IReadOnlyList<RuneLibraryEntryView> Sort(IEnumerable<RuneLibraryEntryView> runes)
    {
        ArgumentNullException.ThrowIfNull(runes);
        return runes
            .OrderByDescending(r => IsRanked(r.Weight) ? r.Weight : double.MinValue)
            .ThenBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Moves a rune one rung towards the top. A rune that is not on the ladder joins it at the
    /// bottom, so promoting something from the crowd takes one click rather than none and then
    /// nothing visible happening.
    /// </summary>
    public static IReadOnlyList<string> MoveUp(IReadOnlyList<string> ladder, string runeId)
    {
        ArgumentNullException.ThrowIfNull(ladder);
        var next = ladder.ToList();
        var i = IndexOf(next, runeId);
        if (i < 0) next.Add(runeId);
        else if (i > 0) (next[i - 1], next[i]) = (next[i], next[i - 1]);
        return next;
    }

    /// <summary>
    /// Moves a rune one rung towards the bottom. The bottom rung falls off the ladder entirely and
    /// rejoins the baseline crowd — otherwise there would be no way back off it.
    /// </summary>
    public static IReadOnlyList<string> MoveDown(IReadOnlyList<string> ladder, string runeId)
    {
        ArgumentNullException.ThrowIfNull(ladder);
        var next = ladder.ToList();
        var i = IndexOf(next, runeId);
        if (i < 0) return next;
        if (i == next.Count - 1) next.RemoveAt(i);
        else (next[i], next[i + 1]) = (next[i + 1], next[i]);
        return next;
    }

    /// <summary>The weight every rune on <paramref name="ladder"/> is worth at its position.</summary>
    public static IReadOnlyDictionary<string, double> WeightsFor(IReadOnlyList<string> ladder)
    {
        ArgumentNullException.ThrowIfNull(ladder);
        var weights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < ladder.Count; i++) weights[ladder[i]] = WeightAt(i, ladder.Count);
        return weights;
    }

    /// <summary>
    /// Which runes change weight going from one ladder to another, and to what. Only these need
    /// writing back to the catalog, rather than all 34 rows on every click.
    ///
    /// A swap within the ladder moves exactly two. Joining or leaving the ladder moves every rung,
    /// because the bottom rung is anchored at <see cref="BaseWeight"/> + <see cref="Step"/> and a
    /// deeper ladder is measured from there — which is the property that keeps the lowest ranked
    /// rune clear of the unranked crowd however long the ladder grows.
    /// </summary>
    public static IReadOnlyList<(string Id, double Weight)> Diff(IReadOnlyList<string> before, IReadOnlyList<string> after)
    {
        var from = WeightsFor(before);
        var to = WeightsFor(after);
        var changed = new List<(string, double)>();
        foreach (var id in from.Keys.Concat(to.Keys).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var was = from.TryGetValue(id, out var w) ? w : BaseWeight;
            var now = to.TryGetValue(id, out var n) ? n : BaseWeight;
            if (Math.Abs(was - now) > 1e-9) changed.Add((id, now));
        }
        return changed;
    }

    /// <summary>
    /// Rank shown beside a rune: its position on the ladder, or an em dash for the crowd. The
    /// weight is not shown — it is an implementation detail of the ordering, and showing it
    /// invites the user to reason about arithmetic instead of about order.
    /// </summary>
    public static string RankLabel(IReadOnlyList<string> ladder, string runeId)
    {
        ArgumentNullException.ThrowIfNull(ladder);
        var i = IndexOf(ladder, runeId);
        return i < 0 ? "—" : (i + 1).ToString(CultureInfo.InvariantCulture);
    }

    private static int IndexOf(IReadOnlyList<string> ladder, string runeId)
    {
        for (var i = 0; i < ladder.Count; i++)
            if (string.Equals(ladder[i], runeId, StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }
}
