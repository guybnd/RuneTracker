using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RuneshapePriceChecker.Runes;

namespace RuneshapePriceChecker.Rumours;

/// <summary>One Island Rumour as shipped in <c>ocr/rumour-tiers.json</c>.</summary>
public sealed class RumourDefinition
{
    /// <summary>Stable id, e.g. <c>nothing-to-drink</c>.</summary>
    public string Id { get; set; } = "";

    /// <summary>Canonical name as the tier list writes it, e.g. <c>Nothing to drink</c>.</summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Strings the game actually prints for this rumour when they differ from the canonical name.
    /// Mostly cosmetic ("Nothin' to drink"), but <c>It's Warm</c> prints as "Warm but risky",
    /// which no amount of fuzzy matching would connect to its name.
    /// </summary>
    public List<string> Aliases { get; set; } = [];

    /// <summary>Map the rumour charts, e.g. <c>Stagnant Basin</c>.</summary>
    public string Map { get; set; } = "";

    /// <summary>What the map is worth running for, e.g. <c>Oil</c>.</summary>
    public string Mods { get; set; } = "";

    /// <summary>Tier letter: <c>S+</c>, <c>A+</c>, <c>A</c>, <c>B</c>, <c>C</c> or <c>D</c>.</summary>
    public string Rating { get; set; } = "";

    /// <summary><c>standard</c>, <c>unique</c> or <c>boss</c>.</summary>
    public string Category { get; set; } = "standard";
}

/// <summary>On-disk shape of <c>ocr/rumour-tiers.json</c>.</summary>
public sealed class RumourFile
{
    public string? Source { get; set; }
    public List<RumourDefinition> Rumours { get; set; } = [];
}

/// <summary>
/// The rumour tier list, and the lookup that turns an OCR'd rumour line into one of its rows.
///
/// The lines are in the game's handwritten parchment font and Windows OCR mangles them badly —
/// "Nothin' to drink" came back as "Lodoiw' to c(riwk" from a real capture. That does not matter
/// as much as it looks like it should: the candidate set is closed and only twenty rows wide, so
/// the job is not to read the text but to pick which of twenty known strings it is, and the
/// mangled text is still far closer to the right one than to any other. Across every line the
/// engine returned for the first four captures the nearest row was the right row, twenty times
/// out of twenty, by a margin of at least three edits.
///
/// The guards exist for the rows never yet seen on screen. <see cref="MaxDistance"/> is one more
/// than the worst correct match measured (8), and <see cref="MinMargin"/> rejects a match the
/// runner-up is nearly as close to — which is what a rumour whose printed string is an alias
/// nobody has recorded yet would look like. Both failures are reported rather than guessed at,
/// so an unknown string surfaces as "unrecognised" and can be added to the table.
/// </summary>
public sealed class RumourTable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Edits allowed between an OCR'd line and a row's name or alias.</summary>
    public const int MaxDistance = 9;

    /// <summary>How much closer the winner must be than the runner-up for the match to be trusted.</summary>
    public const int MinMargin = 2;

    /// <summary>Edits allowed between a charted node's title and a map name.</summary>
    public const int MaxMapDistance = 3;

    /// <summary>Tier order, best first, for ranking a panel's rumours against each other.</summary>
    private static readonly string[] RatingOrder = ["S+", "S", "A+", "A", "B+", "B", "C+", "C", "D"];

    private readonly IReadOnlyList<RumourDefinition> _rumours;
    private readonly List<(string Normalized, RumourDefinition Rumour)> _candidates = [];
    private readonly List<(string Normalized, RumourDefinition Rumour)> _maps = [];

    public RumourTable(ILogger<RumourTable>? logger = null) : this(LoadShippedJson(), logger)
    {
    }

    internal RumourTable(string json, ILogger? logger = null)
    {
        var file = JsonSerializer.Deserialize<RumourFile>(json, JsonOptions) ?? new RumourFile();
        _rumours = file.Rumours.Where(r => !string.IsNullOrWhiteSpace(r.Name)).ToList();

        foreach (var rumour in _rumours)
        {
            Add(rumour.Name, rumour);
            foreach (var alias in rumour.Aliases) Add(alias, rumour);

            var map = RuneTooltipReader.Normalize(rumour.Map);
            if (map.Length > 0) _maps.Add((map, rumour));
        }

        logger?.LogInformation("RumourTable: {Count} rumours loaded ({Candidates} match strings)", _rumours.Count, _candidates.Count);

        void Add(string text, RumourDefinition rumour)
        {
            var normalized = RuneTooltipReader.Normalize(text);
            if (normalized.Length > 0) _candidates.Add((normalized, rumour));
        }
    }

    public IReadOnlyList<RumourDefinition> Rumours => _rumours;

    /// <summary>
    /// The rumour <paramref name="ocrLine"/> names, or null when nothing is close enough or two
    /// rows are too close together to call. Ties and near-ties are refusals, not guesses.
    /// </summary>
    public RumourDefinition? Match(string? ocrLine) => Match(ocrLine, out _);

    /// <inheritdoc cref="Match(string?)"/>
    /// <param name="ocrLine">The line as OCR read it.</param>
    /// <param name="distance">Edits between the line and the row it matched; -1 when none did.</param>
    public RumourDefinition? Match(string? ocrLine, out int distance) =>
        Nearest(_candidates, ocrLine, MaxDistance, out distance);

    /// <summary>
    /// The rumour whose map <paramref name="ocrTitle"/> names — the title line of a charted node's
    /// tooltip, e.g. "Sloughed Gully". Held to a tighter bar than a rumour line: map names are
    /// drawn in the game's block small caps and come back all but perfect, so a loose match here
    /// would be a wrong answer rather than a rescued one.
    /// </summary>
    public RumourDefinition? MatchMap(string? ocrTitle, out int distance) =>
        Nearest(_maps, ocrTitle, MaxMapDistance, out distance);

    /// <inheritdoc cref="MatchMap(string?, out int)"/>
    public RumourDefinition? MatchMap(string? ocrTitle) => MatchMap(ocrTitle, out _);

    private static RumourDefinition? Nearest(
        List<(string Normalized, RumourDefinition Rumour)> candidates,
        string? text,
        int maxDistance,
        out int distance)
    {
        distance = -1;
        if (string.IsNullOrWhiteSpace(text)) return null;

        var normalized = RuneTooltipReader.Normalize(text);
        if (normalized.Length == 0) return null;

        RumourDefinition? best = null;
        var bestDistance = int.MaxValue;
        var runnerUp = int.MaxValue;

        foreach (var (candidate, rumour) in candidates)
        {
            var d = StrComp.GetEditDistance(normalized, candidate, maxDistance);
            if (d < 0) continue;

            if (d < bestDistance)
            {
                // The previous winner becomes the runner-up only if it is a different rumour:
                // a row's own alias sitting one edit further away is not a competing answer.
                if (best is not null && !ReferenceEquals(best, rumour)) runnerUp = bestDistance;
                best = rumour;
                bestDistance = d;
            }
            else if (!ReferenceEquals(best, rumour) && d < runnerUp)
            {
                runnerUp = d;
            }
        }

        if (best is null) return null;
        if (runnerUp != int.MaxValue && runnerUp - bestDistance < MinMargin) return null;

        distance = bestDistance;
        return best;
    }

    /// <summary>Where a rating sits in the tier order; unknown ratings sort last.</summary>
    public static int RatingRank(string? rating)
    {
        if (string.IsNullOrWhiteSpace(rating)) return RatingOrder.Length;
        var index = Array.FindIndex(RatingOrder, r => string.Equals(r, rating.Trim(), StringComparison.OrdinalIgnoreCase));
        return index < 0 ? RatingOrder.Length : index;
    }

    internal static string LoadShippedJson()
    {
        var assembly = typeof(RumourTable).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("rumour-tiers.json", StringComparison.OrdinalIgnoreCase));
        if (resourceName is not null)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is not null)
            {
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
        }

        var onDisk = Path.Combine(AppContext.BaseDirectory, "ocr", "rumour-tiers.json");
        return File.Exists(onDisk) ? File.ReadAllText(onDisk) : "{}";
    }
}
