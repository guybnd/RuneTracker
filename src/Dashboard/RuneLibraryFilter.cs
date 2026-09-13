using System.Globalization;
using System.Windows.Media;

namespace RuneshapePriceChecker.App.Dashboard;

/// <summary>
/// Which subset of the 34 runes the library shows. The list lives inside an already-scrolling
/// settings pane in a window that can be as narrow as 500px, so showing all 34 rows at all times
/// buries the handful the user is actually working on.
/// </summary>
public enum RuneLibraryFilter
{
    /// <summary>All 34 runes.</summary>
    All,

    /// <summary>Runes the detector has never produced a sprite for — the remaining setup work.</summary>
    Unseen,

    /// <summary>Runes with at least one sighting, bound or not.</summary>
    Seen,

    /// <summary>Runes marked as taken during the current run.</summary>
    Carried
}

/// <summary>
/// Pure filtering and summarising for the Rune Library, kept out of the window so it can be
/// tested without a WPF dispatcher (same reasoning as <c>RuneLibraryEntryView</c>'s bindings).
/// </summary>
public static class RuneLibraryFilters
{
    private static readonly RuneLibraryFilter[] Order =
        [RuneLibraryFilter.All, RuneLibraryFilter.Unseen, RuneLibraryFilter.Seen, RuneLibraryFilter.Carried];

    /// <summary>Order the filter picker lists the options in.</summary>
    public static IReadOnlyList<RuneLibraryFilter> All => Order;

    /// <summary>Position in the picker, for syncing the combo's selection.</summary>
    public static int IndexOf(RuneLibraryFilter filter) => Array.IndexOf(Order, filter);

    public static string Label(RuneLibraryFilter filter) => filter switch
    {
        RuneLibraryFilter.All => "All runes",
        RuneLibraryFilter.Unseen => "Not seen yet",
        RuneLibraryFilter.Seen => "Seen",
        RuneLibraryFilter.Carried => "Carried",
        _ => "All runes"
    };

    public static bool IsMatch(RuneLibraryEntryView entry, RuneLibraryFilter filter)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return filter switch
        {
            RuneLibraryFilter.All => true,
            RuneLibraryFilter.Unseen => entry.SeenCount == 0,
            RuneLibraryFilter.Seen => entry.SeenCount > 0,
            RuneLibraryFilter.Carried => entry.IsCarried,
            _ => true
        };
    }

    public static IReadOnlyList<RuneLibraryEntryView> Apply(IEnumerable<RuneLibraryEntryView> entries, RuneLibraryFilter filter)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return filter == RuneLibraryFilter.All
            ? [.. entries]
            : [.. entries.Where(e => IsMatch(e, filter))];
    }

    /// <summary>
    /// Message shown when a filter hides everything, so an empty list never reads as a bug.
    /// Null for <see cref="RuneLibraryFilter.All"/>, which can only be empty if the catalog is.
    /// </summary>
    public static string EmptyMessage(RuneLibraryFilter filter) => filter switch
    {
        RuneLibraryFilter.All => "The rune catalog is empty.",
        RuneLibraryFilter.Unseen => "Every rune has been seen at least once.",
        RuneLibraryFilter.Seen => "No runes seen yet — open a Combinations panel in game.",
        RuneLibraryFilter.Carried => "Nothing marked as carried this run.",
        _ => "The rune catalog is empty."
    };
}

/// <summary>
/// The one-line progress readout above the rows. Binding sprites to names is a one-off setup
/// chore, and without a count there is nothing to tell the user how much of it is left.
/// </summary>
public static class RuneLibrarySummary
{
    public static string Describe(IEnumerable<RuneLibraryEntryView> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var total = 0;
        var bound = 0;
        var carried = 0;
        foreach (var entry in entries)
        {
            total++;
            if (entry.IsBound) bound++;
            if (entry.IsCarried) carried++;
        }

        var carriedText = carried == 0
            ? "none carried yet"
            : string.Create(CultureInfo.InvariantCulture, $"{carried} carried this run");
        return string.Create(CultureInfo.InvariantCulture, $"{bound} of {total} bound · {carriedText}");
    }
}

/// <summary>
/// One rune in the "carried this run" strip. The carried set is the tracker's actual state, and
/// as a column of 34 checkboxes it could only be read by scanning every row.
/// </summary>
public sealed record CarriedRuneChip(string Id, string DisplayName, ImageSource? Icon);
