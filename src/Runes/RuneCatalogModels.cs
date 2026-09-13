using System.Text.Json.Serialization;

namespace RuneshapePriceChecker.Runes;

/// <summary>One of the game's 34 runeshapes, as shipped in <c>ocr/rune-catalog.json</c>.</summary>
public sealed class RuneDefinition
{
    /// <summary>Stable slug, e.g. <c>opulent</c>. Rune ids and binding ids share one namespace.</summary>
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Effect { get; set; } = "";
    /// <summary>The game's own rarity flag (<c>RemnantRareRune</c> icon set).</summary>
    public bool Rare { get; set; }
    /// <summary>Glyph colour tier where confirmed: <c>gold</c>, <c>purple</c>, <c>blue</c>; null when unknown.</summary>
    public string? Tier { get; set; }
    /// <summary>Shipped default weight; the user layer may override it.</summary>
    public double Weight { get; set; } = 1.0;
    /// <summary>Embedded reference-glyph resource name (file under <c>ocr/rune-icons/</c>), or null.</summary>
    public string? Icon { get; set; }
}

/// <summary>
/// A sprite the detector has seen, keyed by its shape hash. Unbound until the user picks
/// which rune it is; <see cref="RuneId"/> is null while unbound.
/// </summary>
public sealed class RuneBinding
{
    /// <summary>Binding id: <c>k-&lt;shape hash hex&gt;</c>. Survives binding so the carried set and history keep pointing at it.</summary>
    public string Id { get; set; } = "";
    public ulong ShapeHash { get; set; }
    /// <summary>Advisory only (latest sighting wins) — never part of the match predicate.</summary>
    public int HueBucket { get; set; }
    public string? SpritePngBase64 { get; set; }
    public string? RuneId { get; set; }
    public int SeenCount { get; set; }
    public DateTimeOffset FirstSeenUtc { get; set; }
    public DateTimeOffset LastSeenUtc { get; set; }

    [JsonIgnore]
    public bool IsBound => !string.IsNullOrEmpty(RuneId);

    public static string IdFor(ulong shapeHash) => "k-" + shapeHash.ToString("x16", System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>On-disk shape of the user layer, <c>config/rune-catalog.json</c>.</summary>
public sealed class RuneCatalogUserLayer
{
    public long Revision { get; set; }
    public Dictionary<string, double> Weights { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<RuneBinding> Bindings { get; set; } = [];
    public List<string> Carried { get; set; } = [];
}

/// <summary>Shipped file shape: the rune list plus the schema version.</summary>
public sealed class RuneCatalogShipped
{
    public int Schema { get; set; } = 1;
    public List<RuneDefinition> Runes { get; set; } = [];
}
