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

    /// <summary>Stack size of the result ("Divine Orb x10" — the game prints "10x Divine Orb"), or 0 when the recipe has none.</summary>
    public int Quantity { get; set; }

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

    /// <summary>A stack size: the game prints it in front ("10x Divine Orb"), poe2db behind ("Divine Orb x10").</summary>
    private static readonly Regex LeadingQuantity = new(@"^\s*(\d+)\s*[x×]\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TrailingQuantity = new(@"\s+[x×]\s*(\d+)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
    /// than it drops a word), narrowed to those with <paramref name="cellCount"/> runes and the
    /// stack size the text states, then to the level it states. Several survivors are fine as
    /// long as they agree on the rune at that cell; a disagreement returns null rather than a
    /// guess, because a wrong name paints a wrong marker with full confidence.
    ///
    /// Cell count and stack size are strict: no recipe of that shape means null, not the nearest
    /// one. A row the detector miscounted — the clipped top row of a 1080p capture came back with
    /// 8 cells of 9 — would otherwise be named from the wrong index with full confidence.
    /// </summary>
    public string? Lookup(string? rowText, int cellCount, int cellIndex)
    {
        if (string.IsNullOrWhiteSpace(rowText) || cellIndex < 0) return null;

        var (key, level, quantity) = ParseRowText(rowText);
        if (key.Length == 0) return null;

        var candidates = FindByKey(key);
        if (candidates.Count == 0) return null;

        if (cellCount > 0)
        {
            candidates = candidates.Where(c => c.Runes.Count == cellCount).ToList();
            if (candidates.Count == 0) return null;
        }
        if (quantity > 0)
        {
            // "1x Divine Orb" is the single-orb recipe, which poe2db lists with no stack at all.
            candidates = candidates.Where(c => c.Quantity == quantity || (quantity == 1 && c.Quantity == 0)).ToList();
            if (candidates.Count == 0) return null;
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
    /// Splits a row's OCR text into the normalised result name, the gem level and the stack size
    /// it states, if any. The game prefixes results with their kind — "Skill Level 20: Skyfall",
    /// "Support: Healing Runes" — so everything up to the last colon goes; a "(Level N)" suffix
    /// goes too. The stack size is kept as a separate part of the identity, not dropped: "Divine
    /// Orb x10" and "Divine Orb x3" are different recipes, and the game prints it in front as
    /// "10x Divine Orb" while poe2db writes it behind.
    /// </summary>
    internal static (string Key, int Level, int Quantity) ParseRowText(string rowText)
    {
        var text = rowText.Trim();
        var level = 0;
        var levelMatch = LevelToken.Match(text);
        if (levelMatch.Success && int.TryParse(levelMatch.Groups[1].Value, out var parsed)) level = parsed;

        var colon = text.LastIndexOf(':');
        if (colon >= 0 && colon < text.Length - 1) text = text[(colon + 1)..];
        text = LevelToken.Replace(text, " ").Trim();

        var quantity = 0;
        var leading = LeadingQuantity.Match(text);
        var trailing = TrailingQuantity.Match(text);
        if (leading.Success && int.TryParse(leading.Groups[1].Value, out var q1))
        {
            quantity = q1;
            text = text[leading.Length..];
        }
        else if (trailing.Success && int.TryParse(trailing.Groups[1].Value, out var q2))
        {
            quantity = q2;
            text = text[..trailing.Index];
        }
        return (NormaliseName(text), level, quantity);
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
