using System.Numerics;
using RuneshapePriceChecker.Contracts;

namespace RuneshapePriceChecker.Runes;

/// <summary>
/// Decides whether two rune keys are the same rune. Matching is on shape alone: RUNE-1
/// measured the same rune 1–3 bits apart and distinct runes 22+ bits apart, while the hue
/// bucket of a dark glyph is decided by a handful of anti-aliased edge pixels and can flip
/// between frames — using it in the predicate would split one rune into several bindings.
/// </summary>
public static class RuneKeyMatcher
{
    public static int Hamming(ulong a, ulong b) => BitOperations.PopCount(a ^ b);

    public static bool IsSame(ulong a, ulong b, int threshold) => Hamming(a, b) <= threshold;

    public static bool IsSame(RuneKey a, RuneKey b, int threshold)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return IsSame(a.ShapeHash, b.ShapeHash, threshold);
    }

    /// <summary>
    /// Returns the candidate whose hash is closest to <paramref name="shapeHash"/> and within
    /// <paramref name="threshold"/>, or null. Lowest distance wins so a near-tie between two
    /// bindings resolves deterministically instead of by list order.
    /// </summary>
    public static T? FindNearest<T>(ulong shapeHash, IEnumerable<T> candidates, Func<T, ulong> hashOf, int threshold)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(hashOf);

        T? best = null;
        var bestDistance = int.MaxValue;
        foreach (var candidate in candidates)
        {
            var distance = Hamming(shapeHash, hashOf(candidate));
            if (distance <= threshold && distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }
        return best;
    }
}
