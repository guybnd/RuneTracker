using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace RuneshapePriceChecker.Runes;

/// <summary>One Runeshape Combination as shipped in <c>ocr/rune-combinations.json</c>.</summary>
public sealed class RuneCombination
{
    /// <summary>Result item or gem name as the game prints it after the prefix, e.g. <c>Skyfall</c>.</summary>
    public string Name { get; set; } = "";

    /// <summary>Gem level from the "(Level N)" variant, or 0 when the recipe has none.</summary>
    public int Level { get; set; }

    /// <summary>Area-level tier the recipe appears at, e.g. <c>Lv70+</c>. Informational.</summary>
    public string Tier { get; set; } = "";

    /// <summary>Catalog rune ids in the order the Combinations panel draws them.</summary>
    public List<string> Runes { get; set; } = [];
}

/// <summary>On-disk shape of <c>ocr/rune-combinations.json</c>, written by <c>scripts/update-rune-combinations.ps1</c>.</summary>
public sealed class RuneCombinationFile
{
    public string? Source { get; set; }
    public string? FetchedUtc { get; set; }
    public List<RuneCombination> Combinations { get; set; } = [];
}

/// <summary>
/// The game's Runeshape Combinations, keyed by result name, and the lookup that turns a row's
/// OCR text plus a gilded cell's position into a rune id.
///
/// This is the identity source the sprite hash was standing in for. The panel draws a
/// combination's runes in a fixed order and the app already reads each row's name for pricing,
/// so a gilded rune is named by <c>(row name, icon count, cell index)</c> with no image matching
/// at all — and, unlike a hash, it cannot drift between rows or captures. Hashing stays for rows
/// whose name did not read, and as the thing the table names in the library (see
/// <see cref="RuneCatalog.Resolve(Contracts.RuneKey, string?)"/>).
/// </summary>
public sealed class RuneCombinationTable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>"Level 20" wherever it sits in the row text — the game's prefix ("Skill Level 20: X") or poe2db's suffix ("X (Level 20)").</summary>
    private static readonly Regex LevelToken = new(@"\blevel\s*(\d{1,2})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>A trailing quantity, "x3" / "×3", which the game prints for currency results.</summary>
    private static readonly Regex TrailingQuantity = new(@"\s*[x×]\s*\d+\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IReadOnlyList<RuneCombination> _combinations;
    private readonly Dictionary<string, List<RuneCombination>> _byKey = new(StringComparer.Ordinal);
    private readonly ILogger? _logger;

    public RuneCombinationTable(ILogger<RuneCombinationTable> logger) : this(LoadShippedJson(), logger)
    {
    }

    internal RuneCombinationTable(string json, ILogger? logger = null)
    {
        _logger = logger;
        var file = JsonSerializer.Deserialize<RuneCombinationFile>(json, JsonOptions) ?? new RuneCombinationFile();
        _combinations = file.Combinations
            .Where(c => !string.IsNullOrWhiteSpace(c.Name) && c.Runes.Count > 0)
            .ToList();
        foreach (var combination in _combinations)
        {
            var key = NormaliseName(combination.Name);
            if (key.Length == 0) continue;
            if (!_byKey.TryGetValue(key, out var list)) _byKey[key] = list = [];
            list.Add(combination);
        }
        _logger?.LogInformation("RuneCombinationTable: {Count} combinations loaded ({Names} names)", _combinations.Count, _byKey.Count);
    }

    public IReadOnlyList<RuneCombination> Combinations => _combinations;

    /// <summary>
    /// The rune at <paramref name="cellIndex"/> of the combination the row text names, or null
    /// when the text does not name exactly one recipe shape.
    ///
    /// Candidates are the recipes whose normalised name matches the text (exactly, else within
    /// one edit for short names and two for longer ones — OCR reads a letter wrong more often
    /// than it drops a word), narrowed to those with <paramref name="cellCount"/> runes, then to
    /// the level the text states. Several survivors are fine as long as they agree on the rune at
    /// that cell; a disagreement returns null rather than a guess, because a wrong name paints a
    /// wrong marker with full confidence.
    /// </summary>
    public string? Lookup(string? rowText, int cellCount, int cellIndex)
    {
        if (string.IsNullOrWhiteSpace(rowText) || cellIndex < 0) return null;

        var (key, level) = ParseRowText(rowText);
        if (key.Length == 0) return null;

        var candidates = FindByKey(key);
        if (candidates.Count == 0) return null;

        if (cellCount > 0)
        {
            var byCount = candidates.Where(c => c.Runes.Count == cellCount).ToList();
            if (byCount.Count > 0) candidates = byCount;
        }
        if (level > 0)
        {
            var byLevel = candidates.Where(c => c.Level == level).ToList();
            if (byLevel.Count > 0) candidates = byLevel;
        }

        string? rune = null;
        foreach (var candidate in candidates)
        {
            if (cellIndex >= candidate.Runes.Count) return null;
            var id = candidate.Runes[cellIndex];
            if (rune is null) rune = id;
            else if (!string.Equals(rune, id, StringComparison.OrdinalIgnoreCase)) return null;
        }
        return rune;
    }

    private List<RuneCombination> FindByKey(string key)
    {
        if (_byKey.TryGetValue(key, out var exact)) return exact;

        var maxDistance = key.Length >= 8 ? 2 : 1;
        var fuzzy = new List<RuneCombination>();
        foreach (var (candidateKey, list) in _byKey)
        {
            if (Math.Abs(candidateKey.Length - key.Length) > maxDistance) continue;
            if (StrComp.AreFewCharsAway(key, candidateKey, maxDistance)) fuzzy.AddRange(list);
        }
        return fuzzy;
    }

    /// <summary>
    /// Splits a row's OCR text into the normalised result name and the gem level it states, if
    /// any. The game prefixes results with their kind — "Skill Level 20: Skyfall", "Support:
    /// Healing Runes" — so everything up to the last colon goes; a "(Level N)" suffix and a
    /// trailing quantity go too, since poe2db writes the former and the game the latter.
    /// </summary>
    internal static (string Key, int Level) ParseRowText(string rowText)
    {
        var text = rowText.Trim();
        var level = 0;
        var levelMatch = LevelToken.Match(text);
        if (levelMatch.Success && int.TryParse(levelMatch.Groups[1].Value, out var parsed)) level = parsed;

        var colon = text.LastIndexOf(':');
        if (colon >= 0 && colon < text.Length - 1) text = text[(colon + 1)..];
        text = LevelToken.Replace(text, " ");
        text = TrailingQuantity.Replace(text, string.Empty);
        return (NormaliseName(text), level);
    }

    /// <summary>Lower-case letters and digits only, so punctuation, spacing and OCR case slips never matter.</summary>
    internal static string NormaliseName(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString();
    }

    internal static string LoadShippedJson()
    {
        var assembly = typeof(RuneCombinationTable).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("rune-combinations.json", StringComparison.OrdinalIgnoreCase));
        if (resourceName is not null)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is not null)
            {
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
        }

        var onDisk = Path.Combine(AppContext.BaseDirectory, "ocr", "rune-combinations.json");
        return File.Exists(onDisk) ? File.ReadAllText(onDisk, Encoding.UTF8) : "{}";
    }
}
