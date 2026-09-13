using System.Globalization;

namespace RuneshapePriceChecker.App.Dashboard;

/// <summary>
/// Named priority levels for a rune, and the weight each one means.
///
/// The library exposed weight only as a number box, which asks the wrong question: the user
/// knows "Opulent matters most, Power second, the rest are worth having" and has no way to know
/// whether that means 3 and 2 or 30 and 20. Only the ordering matters — the scorer sums weights
/// per row and badges the highest — so naming a few levels says the same thing without inventing
/// a scale. Typed weights still work and are shown as Custom.
/// </summary>
public enum RunePriority
{
    /// <summary>Never worth taking; contributes nothing to a row's score.</summary>
    Ignore,

    /// <summary>Worth less than an ordinary rune. The shipped level for the blue tier.</summary>
    Low,

    /// <summary>The shipped level for most runes.</summary>
    Normal,

    /// <summary>Take over an ordinary rune. The shipped level for Power.</summary>
    Wanted,

    /// <summary>Take over anything else. The shipped level for Opulent.</summary>
    MustHave,

    /// <summary>A weight typed by hand that is not one of the levels above.</summary>
    Custom
}

public static class RunePriorities
{
    /// <summary>Order the picker lists the levels in: most wanted first, as the library reads top-down.</summary>
    public static IReadOnlyList<RunePriority> All { get; } =
        [RunePriority.MustHave, RunePriority.Wanted, RunePriority.Normal, RunePriority.Low, RunePriority.Ignore];

    public static double WeightOf(RunePriority priority) => priority switch
    {
        RunePriority.MustHave => 3.0,
        RunePriority.Wanted => 2.0,
        RunePriority.Normal => 1.0,
        RunePriority.Low => 0.5,
        RunePriority.Ignore => 0.0,
        _ => 1.0
    };

    /// <summary>The level a weight represents, or <see cref="RunePriority.Custom"/> when it is between levels.</summary>
    public static RunePriority FromWeight(double weight)
    {
        foreach (var priority in All)
            if (Math.Abs(WeightOf(priority) - weight) < 1e-9)
                return priority;
        return RunePriority.Custom;
    }

    public static string Label(RunePriority priority) => priority switch
    {
        RunePriority.MustHave => "Must have",
        RunePriority.Wanted => "Wanted",
        RunePriority.Normal => "Normal",
        RunePriority.Low => "Low",
        RunePriority.Ignore => "Ignore",
        _ => "Custom"
    };

    /// <summary>
    /// Label for a weight, including the number for a hand-typed one so it is not hidden.
    ///
    /// The shipped shortlist — Opulent 15 down to Life 10 — is deliberately ranked rather than
    /// levelled: those six are ordered against each other, which no named level can express. They
    /// read "Top (15)" rather than "Custom (15)", since there is nothing irregular about them.
    /// The level they map to is still <see cref="RunePriority.Custom"/>, so picking a named level
    /// over one of them stays an explicit change rather than a silent rounding.
    /// </summary>
    public static string LabelForWeight(double weight)
    {
        var priority = FromWeight(weight);
        if (priority != RunePriority.Custom) return Label(priority);
        var prefix = weight > WeightOf(RunePriority.MustHave) ? "Top" : "Custom";
        return string.Create(CultureInfo.InvariantCulture, $"{prefix} ({weight:0.##})");
    }

    /// <summary>
    /// What the picker offers for a given current weight: the five levels, plus the hand-typed
    /// one when that is what the rune is on, so selecting into the list is never a silent change.
    /// </summary>
    public static IReadOnlyList<string> ChoicesFor(double weight)
    {
        var labels = All.Select(Label).ToList();
        if (FromWeight(weight) == RunePriority.Custom)
            labels.Insert(0, LabelForWeight(weight));
        return labels;
    }

    /// <summary>The weight a picked label means, or null for the Custom entry, which changes nothing.</summary>
    public static double? WeightForLabel(string? label)
    {
        foreach (var priority in All)
            if (string.Equals(Label(priority), label, StringComparison.Ordinal))
                return WeightOf(priority);
        return null;
    }
}
