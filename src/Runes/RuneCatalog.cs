using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RuneshapePriceChecker.Configuration;
using RuneshapePriceChecker.Contracts;

namespace RuneshapePriceChecker.Runes;

/// <summary>
/// The rune library: the shipped list of 34 runeshapes (names, effects, tiers, default
/// weights, reference glyphs) plus the user layer beside the exe —
/// <c>config/rune-catalog.json</c> — holding weight overrides, sprite bindings and the
/// carried-this-run set. Thread-safe; every mutation bumps <see cref="Revision"/>, raises
/// <see cref="Changed"/> and schedules a debounced save.
///
/// Sprites are <b>bound</b>, not discovered: a gilded key the detector reports is matched
/// to an existing binding on shape alone (see <see cref="RuneKeyMatcher"/>); an unmatched
/// key is held in memory on its first sighting and persisted as an <i>unbound</i> binding
/// on its second (a one-frame OCR artefact never reaches disk), capped by
/// <see cref="RunesOptions.MaxUnboundBindings"/>. The user then picks which rune it is.
/// </summary>
public sealed class RuneCatalog : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly TimeSpan SaveDebounce = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Stored-sprite generation. Bump whenever the crop or hash behind
    /// <see cref="RuneBinding.ShapeHash"/> changes, or when the stored sprite itself changes
    /// enough that old ones would look wrong beside new ones — bindings from an older generation
    /// are dropped on load, since they can never match a freshly computed key again.
    /// v1: glyph crop from the row lattice.
    /// v2 (RUNE-9): crop anchored to the cell's own plate.
    /// v3 (RUNE-10): display sprite is the plate at 64px, no frame or parchment.
    /// </summary>
    internal const int CurrentHashVersion = 3;

    private readonly object _sync = new();
    private readonly IOptionsMonitor<RunesOptions> _options;
    private readonly ILogger _logger;
    private readonly string _userFilePath;
    private readonly List<RuneDefinition> _runes;
    private readonly Dictionary<string, RuneDefinition> _runesById;
    private readonly Dictionary<string, double> _weightOverrides = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<RuneBinding> _bindings = [];
    private readonly Dictionary<ulong, RuneBinding> _pending = [];
    private readonly HashSet<string> _carried = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _warnedConflicts = new(StringComparer.OrdinalIgnoreCase);
    private long _revision;
    private int _seedVersion;
    private bool _dirty;
    private bool _warnedCap;
    private System.Threading.Timer? _saveTimer;

    public RuneCatalog(IOptionsMonitor<RunesOptions> options, ILogger<RuneCatalog> logger)
        : this(options, logger, DefaultUserFilePath, LoadShippedJson(), LoadSeedJson())
    {
    }

    /// <param name="seedJson">
    /// Shipped named sprites to merge in, or null for none. Tests pass null so they start from the
    /// library they build themselves rather than from whatever the seed happens to hold.
    /// </param>
    internal RuneCatalog(IOptionsMonitor<RunesOptions> options, ILogger logger, string userFilePath, string shippedJson, string? seedJson = null)
    {
        _options = options;
        _logger = logger;
        _userFilePath = userFilePath;

        var shipped = JsonSerializer.Deserialize<RuneCatalogShipped>(shippedJson, JsonOptions) ?? new RuneCatalogShipped();
        _runes = shipped.Runes.Where(r => !string.IsNullOrWhiteSpace(r.Id)).ToList();
        _runesById = _runes.ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
        LoadUserLayer();
        MergeSeed(seedJson);
    }

    public static string DefaultUserFilePath => Path.Combine(AppContext.BaseDirectory, "config", "rune-catalog.json");

    /// <summary>Raised (outside the lock) after any change that affects scoring or the library view.</summary>
    public event Action? Changed;

    /// <summary>Monotonic counter bumped on every mutation; re-render hashes include it.</summary>
    public long Revision { get { lock (_sync) return _revision; } }

    public IReadOnlyList<RuneDefinition> Runes => _runes;

    /// <summary>Persisted bindings (bound and unbound). First-sighting pending keys are not included.</summary>
    public IReadOnlyList<RuneBinding> Bindings { get { lock (_sync) return _bindings.Select(Clone).ToList(); } }

    public IReadOnlyCollection<string> Carried { get { lock (_sync) return _carried.ToList(); } }

    public RuneDefinition? GetRune(string? id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return _runesById.TryGetValue(id, out var rune) ? rune : null;
    }

    public double GetWeight(string runeId)
    {
        lock (_sync)
        {
            if (_weightOverrides.TryGetValue(runeId, out var w)) return w;
        }
        return GetRune(runeId)?.Weight ?? _options.CurrentValue.UnknownRuneWeight;
    }

    public void SetWeight(string runeId, double weight)
    {
        if (GetRune(runeId) is null) return;
        lock (_sync)
        {
            var shipped = _runesById[runeId].Weight;
            if (Math.Abs(shipped - weight) < 1e-9) _weightOverrides.Remove(runeId);
            else _weightOverrides[runeId] = weight;
            TouchLocked();
        }
        Changed?.Invoke();
    }

    /// <summary>
    /// Weight for a sprite the user has not named, from its glyph colour alone.
    ///
    /// Gold deliberately does NOT map to the gold tier here yet, even though the measurement can
    /// now see it. Two things have to hold first and only one does: the colour must mean exactly
    /// one rune (it does — Opulent is the catalog's only gold tier), and it must read the same
    /// for the same rune every time (it does not). On the real fixture one rune counted 162 gold
    /// pixels in one row and 72 in another, landing either side of the threshold. Scoring off
    /// that would make the same rune worth 3 in one row and 1 in the next, which is precisely the
    /// inconsistency this was meant to remove.
    /// </summary>
    public double UnboundWeight(int hueBucket)
    {
        var o = _options.CurrentValue;
        return hueBucket switch
        {
            6 or 7 => o.BlueTierWeight,
            8 or 9 => o.PurpleTierWeight,
            _ => o.UnknownRuneWeight
        };
    }

    public RuneBinding? FindBinding(ulong shapeHash)
    {
        lock (_sync) return FindBindingLocked(shapeHash) is { } b ? Clone(b) : null;
    }

    private RuneBinding? FindBindingLocked(ulong shapeHash)
    {
        var threshold = _options.CurrentValue.MatchHammingThreshold;
        return RuneKeyMatcher.FindNearest(shapeHash, _bindings, b => b.ShapeHash, threshold)
            ?? RuneKeyMatcher.FindNearest(shapeHash, _pending.Values, b => b.ShapeHash, threshold);
    }

    /// <summary>Records a sighting. Returns true when the persisted state changed.</summary>
    public bool Observe(RuneKey key, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        var at = now ?? DateTimeOffset.UtcNow;
        var changed = false;
        lock (_sync)
        {
            var existing = FindBindingLocked(key.ShapeHash);
            if (existing is null)
            {
                _pending[key.ShapeHash] = new RuneBinding
                {
                    Id = RuneBinding.IdFor(key.ShapeHash),
                    ShapeHash = key.ShapeHash,
                    HueBucket = key.HueBucket,
                    SpritePngBase64 = SpriteCodec.ToPngBase64(key.SpriteRgb),
                    SeenCount = 1,
                    FirstSeenUtc = at,
                    LastSeenUtc = at
                };
            }
            else if (_pending.ContainsKey(existing.ShapeHash) && !_bindings.Contains(existing))
            {
                // Second sighting of a pending key: promote to the persisted layer (bounded).
                var unbound = _bindings.Count(b => !b.IsBound);
                if (unbound >= _options.CurrentValue.MaxUnboundBindings)
                {
                    if (!_warnedCap)
                    {
                        _warnedCap = true;
                        _logger.LogWarning("RuneCatalog: {Cap} unbound sprites already stored; new sprites are ignored until some are bound", unbound);
                    }
                    existing.SeenCount++;
                    existing.LastSeenUtc = at;
                }
                else
                {
                    _pending.Remove(existing.ShapeHash);
                    existing.SeenCount++;
                    existing.LastSeenUtc = at;
                    existing.HueBucket = key.HueBucket;
                    _bindings.Add(existing);
                    TouchLocked();
                    changed = true;
                }
            }
            else
            {
                existing.SeenCount++;
                existing.LastSeenUtc = at;
                existing.HueBucket = key.HueBucket;
                _dirty = true;
                ScheduleSaveLocked();
            }
        }
        if (changed) Changed?.Invoke();
        return changed;
    }

    /// <summary>Binds a stored sprite to a rune (<paramref name="runeId"/> null unbinds). The carried set follows the binding.</summary>
    public bool Bind(string bindingId, string? runeId)
    {
        if (runeId is not null && GetRune(runeId) is null) return false;
        lock (_sync)
        {
            var binding = _bindings.FirstOrDefault(b => string.Equals(b.Id, bindingId, StringComparison.OrdinalIgnoreCase));
            if (binding is null) return false;

            var previousCarriedId = binding.IsBound ? binding.RuneId! : binding.Id;
            binding.RuneId = runeId;
            var newCarriedId = runeId ?? binding.Id;
            if (_carried.Remove(previousCarriedId)) _carried.Add(newCarriedId);
            TouchLocked();
        }
        Changed?.Invoke();
        return true;
    }

    /// <summary>Forgets a stored sprite entirely.</summary>
    public bool RemoveBinding(string bindingId)
    {
        lock (_sync)
        {
            var removed = _bindings.RemoveAll(b => string.Equals(b.Id, bindingId, StringComparison.OrdinalIgnoreCase));
            if (removed == 0) return false;
            _carried.Remove(bindingId);
            TouchLocked();
        }
        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// Forgets every sprite the user has not bound to a rune, and returns how many went.
    /// A run of misread cells can fill the store up to <see cref="RunesOptions.MaxUnboundBindings"/>,
    /// at which point new sprites are dropped; clearing them one at a time is the only other way out.
    /// Bound sprites are untouched, as is the carried set for the runes they name.
    /// </summary>
    public int ForgetAllUnbound()
    {
        int removed;
        lock (_sync)
        {
            var doomed = _bindings.Where(b => !b.IsBound).ToList();
            if (doomed.Count == 0) return 0;
            foreach (var binding in doomed)
            {
                _ = _bindings.Remove(binding);
                _ = _carried.Remove(binding.Id);
            }
            _pending.Clear();
            _warnedCap = false;
            removed = doomed.Count;
            TouchLocked();
        }
        Changed?.Invoke();
        return removed;
    }

    /// <summary>The id a key is carried under: its rune id when bound, else its binding id.</summary>
    public static string CarriedIdFor(RuneBinding binding) => binding.IsBound ? binding.RuneId! : binding.Id;

    public bool IsCarried(string id) { lock (_sync) return _carried.Contains(id); }

    public void SetCarried(string id, bool carried)
    {
        lock (_sync)
        {
            var changed = carried ? _carried.Add(id) : _carried.Remove(id);
            if (!changed) return;
            TouchLocked();
        }
        Changed?.Invoke();
    }

    public void ResetCarried()
    {
        lock (_sync)
        {
            if (_carried.Count == 0) return;
            _carried.Clear();
            TouchLocked();
        }
        Changed?.Invoke();
    }

    /// <summary>
    /// Resolves a detected key to what the scorer needs: its binding (persisted or pending),
    /// the rune it is bound to (if any) and the weight — the rune's weight when bound, else the
    /// hue-rule weight for an unbound sprite.
    /// </summary>
    public RuneResolution Resolve(RuneKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        RuneBinding? binding;
        lock (_sync) binding = FindBindingLocked(key.ShapeHash) is { } b ? Clone(b) : null;

        var bindingId = binding?.Id ?? RuneBinding.IdFor(key.ShapeHash);
        var rune = GetRune(binding?.RuneId);
        var carriedId = rune?.Id ?? bindingId;
        var weight = rune is not null ? GetWeight(rune.Id) : UnboundWeight(key.HueBucket);
        return new RuneResolution(bindingId, rune, weight, IsCarried(carriedId), carriedId);
    }

    /// <summary>
    /// Resolves a key whose rune is already known from the Combinations table
    /// (<see cref="RuneCombinationTable.Lookup"/>), or falls back to <see cref="Resolve(RuneKey)"/>
    /// when <paramref name="knownRuneId"/> is null or names nothing.
    ///
    /// The table decides the resolution: the panel prints the row's name and draws its runes in
    /// a fixed order, which is better evidence than a shape hash that can drift a bit or two
    /// between rows. It also does the naming the user used to do by hand — a stored sprite that
    /// matches the key and has no rune yet is bound to this one, so the library fills itself in
    /// as panels are read. A sprite the user bound to a <i>different</i> rune is left as they set
    /// it (the table still wins for scoring) and the disagreement is logged once, since silently
    /// overruling a deliberate choice would hide whichever of the two is wrong.
    /// </summary>
    public RuneResolution Resolve(RuneKey key, string? knownRuneId)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (GetRune(knownRuneId) is not { } known) return Resolve(key);

        RuneBinding? binding;
        var named = false;
        string? conflict = null;
        lock (_sync)
        {
            var stored = FindBindingLocked(key.ShapeHash);
            if (stored is not null && _bindings.Contains(stored))
            {
                if (!stored.IsBound)
                {
                    stored.RuneId = known.Id;
                    if (_carried.Remove(stored.Id)) _carried.Add(known.Id);
                    TouchLocked();
                    named = true;
                }
                else if (!string.Equals(stored.RuneId, known.Id, StringComparison.OrdinalIgnoreCase) && _warnedConflicts.Add(stored.Id))
                {
                    conflict = stored.RuneId;
                }
            }
            binding = stored is null ? null : Clone(stored);
        }

        if (named)
        {
            _logger.LogInformation("RuneCatalog: sprite {Binding} named {Rune} from the Combinations table", binding!.Id, known.DisplayName);
            Changed?.Invoke();
        }
        if (conflict is not null)
        {
            _logger.LogWarning(
                "RuneCatalog: sprite {Binding} is bound to {Bound} but the Combinations table says the cell holds {Known}; scoring follows the table, the binding is left as set",
                binding!.Id, conflict, known.Id);
        }

        var bindingId = binding?.Id ?? RuneBinding.IdFor(key.ShapeHash);
        return new RuneResolution(bindingId, known, GetWeight(known.Id), IsCarried(known.Id), known.Id);
    }

    /// <summary>Writes pending changes now (also runs on dispose).</summary>
    public void Flush()
    {
        lock (_sync)
        {
            _saveTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            if (!_dirty) return;
            SaveLocked();
        }
    }

    public void Dispose()
    {
        Flush();
        lock (_sync)
        {
            _saveTimer?.Dispose();
            _saveTimer = null;
        }
    }

    private void TouchLocked()
    {
        _revision++;
        _dirty = true;
        ScheduleSaveLocked();
    }

    private void ScheduleSaveLocked()
    {
        _saveTimer ??= new System.Threading.Timer(_ => Flush(), null, Timeout.Infinite, Timeout.Infinite);
        _ = _saveTimer.Change(SaveDebounce, Timeout.InfiniteTimeSpan);
    }

    private void SaveLocked()
    {
        try
        {
            var layer = new RuneCatalogUserLayer
            {
                Revision = _revision,
                HashVersion = CurrentHashVersion,
                SeedVersion = _seedVersion,
                Weights = new Dictionary<string, double>(_weightOverrides, StringComparer.OrdinalIgnoreCase),
                Bindings = _bindings.Select(Clone).ToList(),
                Carried = _carried.OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList()
            };
            var dir = Path.GetDirectoryName(_userFilePath);
            if (!string.IsNullOrEmpty(dir)) _ = Directory.CreateDirectory(dir);
            var tmp = _userFilePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(layer, JsonOptions) + Environment.NewLine);
            File.Move(tmp, _userFilePath, overwrite: true);
            _dirty = false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RuneCatalog: failed to save {Path}", _userFilePath);
        }
    }

    private void LoadUserLayer()
    {
        if (!File.Exists(_userFilePath)) return;
        try
        {
            var layer = JsonSerializer.Deserialize<RuneCatalogUserLayer>(File.ReadAllText(_userFilePath), JsonOptions);
            if (layer is null) return;
            _revision = layer.Revision;
            _seedVersion = layer.SeedVersion;
            foreach (var (id, weight) in layer.Weights)
                if (_runesById.ContainsKey(id)) _weightOverrides[id] = weight;
            if (layer.HashVersion != CurrentHashVersion)
            {
                // The identity crop changed, so every stored hash names a crop that is no longer
                // produced. Keeping them would be worse than useless: they can never match again,
                // and they would hold the unbound store at its cap so no new sprite could be
                // saved. Weights and rune-level carried flags are unaffected and survive.
                _logger.LogInformation(
                    "RuneCatalog: dropping {Count} sprite binding(s) — identity hash v{Old} -> v{New}. Sprites will be re-learned; names must be set again.",
                    layer.Bindings.Count, layer.HashVersion, CurrentHashVersion);
                foreach (var id in layer.Carried)
                    if (_runesById.ContainsKey(id)) _carried.Add(id);
                // Seeded sprites were dropped along with the rest, so forget that they were ever
                // applied. A seed re-exported on the new generation then merges in cleanly.
                _seedVersion = 0;
                _dirty = true;
                ScheduleSaveLocked();
                return;
            }

            foreach (var binding in layer.Bindings)
            {
                if (string.IsNullOrEmpty(binding.Id)) binding.Id = RuneBinding.IdFor(binding.ShapeHash);
                if (binding.RuneId is not null && !_runesById.ContainsKey(binding.RuneId)) binding.RuneId = null;
                _bindings.Add(binding);
            }
            foreach (var id in layer.Carried) _carried.Add(id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RuneCatalog: could not read {Path}; starting with shipped defaults", _userFilePath);
        }
    }

    /// <summary>
    /// Merges the shipped named sprites into the library, once per seed version.
    ///
    /// Three things keep this from fighting the user. It only adds shape hashes the library has
    /// never seen, so nothing already bound is overwritten. It runs once — the applied version is
    /// persisted — so a sprite the user forgets on purpose does not reappear on the next launch.
    /// And it refuses a seed from a different hash generation, whose hashes name crops the
    /// detector no longer produces and so could never match anything anyway.
    /// </summary>
    private void MergeSeed(string? seedJson)
    {
        if (string.IsNullOrWhiteSpace(seedJson)) return;
        try
        {
            var seed = JsonSerializer.Deserialize<RuneSeedFile>(seedJson, JsonOptions);
            if (seed is null || seed.Bindings.Count == 0 || seed.SeedVersion <= _seedVersion) return;
            if (seed.HashVersion != CurrentHashVersion)
            {
                _logger.LogInformation(
                    "RuneCatalog: shipped sprite seed v{Seed} was captured on identity hash v{Old}, not v{New}; skipping it.",
                    seed.SeedVersion, seed.HashVersion, CurrentHashVersion);
                return;
            }

            var known = _bindings.Select(b => b.ShapeHash).ToHashSet();
            var now = DateTimeOffset.UtcNow;
            var added = 0;
            foreach (var seeded in seed.Bindings)
            {
                if (string.IsNullOrEmpty(seeded.RuneId) || !_runesById.ContainsKey(seeded.RuneId)) continue;
                if (!known.Add(seeded.ShapeHash)) continue;
                _bindings.Add(new RuneBinding
                {
                    Id = RuneBinding.IdFor(seeded.ShapeHash),
                    ShapeHash = seeded.ShapeHash,
                    HueBucket = seeded.HueBucket,
                    SpritePngBase64 = seeded.SpritePngBase64,
                    RuneId = seeded.RuneId,
                    SeenCount = 0,
                    FirstSeenUtc = now,
                    LastSeenUtc = now
                });
                added++;
            }

            _seedVersion = seed.SeedVersion;
            _revision++;
            _dirty = true;
            ScheduleSaveLocked();
            if (added > 0)
                _logger.LogInformation("RuneCatalog: seeded {Count} named sprite(s) from the shipped library (seed v{Seed}).", added, seed.SeedVersion);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RuneCatalog: could not read the shipped sprite seed; the library is unchanged");
        }
    }

    private static RuneBinding Clone(RuneBinding b) => new()
    {
        Id = b.Id,
        ShapeHash = b.ShapeHash,
        HueBucket = b.HueBucket,
        SpritePngBase64 = b.SpritePngBase64,
        RuneId = b.RuneId,
        SeenCount = b.SeenCount,
        FirstSeenUtc = b.FirstSeenUtc,
        LastSeenUtc = b.LastSeenUtc
    };

    /// <summary>Loads the embedded <c>ocr/rune-catalog.json</c>, same lookup as the unique-category map.</summary>
    internal static string LoadShippedJson()
        => LoadEmbedded("rune-catalog.json") ?? """{"schema":1,"runes":[]}""";

    /// <summary>Loads the embedded <c>ocr/rune-seed.json</c>, or null when the build ships no seed.</summary>
    internal static string? LoadSeedJson() => LoadEmbedded("rune-seed.json");

    private static string? LoadEmbedded(string fileName)
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            var name = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
            if (name is not null)
            {
                using var stream = asm.GetManifestResourceStream(name);
                using var reader = new StreamReader(stream!);
                return reader.ReadToEnd();
            }

            var projectDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..");
            var filePath = Path.Combine(projectDir, "ocr", fileName);
            if (File.Exists(filePath)) return File.ReadAllText(filePath);
        }
        catch
        {
        }
        return null;
    }

    /// <summary>Reads an embedded reference glyph (<c>ocr/rune-icons/&lt;id&gt;.png</c>) as PNG bytes, or null.</summary>
    public static byte[]? LoadReferenceIcon(string? iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName)) return null;
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            // MSBuild sanitises folder names in manifest resource names (rune-icons → rune_icons),
            // so match on the file name and the folder in either spelling.
            var name = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("." + iconName, StringComparison.OrdinalIgnoreCase)
                                  && (n.Contains("rune_icons", StringComparison.OrdinalIgnoreCase)
                                      || n.Contains("rune-icons", StringComparison.OrdinalIgnoreCase)));
            if (name is null) return null;
            using var stream = asm.GetManifestResourceStream(name);
            if (stream is null) return null;
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>What the catalog knows about one detected key.</summary>
public sealed record RuneResolution(string BindingId, RuneDefinition? Rune, double Weight, bool IsCarried, string CarriedId)
{
    public bool IsUnbound => Rune is null;
    public string DisplayName => Rune?.DisplayName ?? "Unbound rune";
}

/// <summary>PNG encoding for the detector's 32×32 RGB24 sprites.</summary>
public static class SpriteCodec
{
    public const int SpriteSize = RuneKey.SpriteSize;

    public static string? ToPngBase64(byte[]? rgb)
    {
        if (rgb is null || rgb.Length < SpriteSize * SpriteSize * 3) return null;
        try
        {
            using var bmp = new Bitmap(SpriteSize, SpriteSize, PixelFormat.Format24bppRgb);
            var rect = new Rectangle(0, 0, SpriteSize, SpriteSize);
            var data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            try
            {
                var row = new byte[SpriteSize * 3];
                for (var y = 0; y < SpriteSize; y++)
                {
                    for (var x = 0; x < SpriteSize; x++)
                    {
                        var si = ((y * SpriteSize) + x) * 3;
                        row[(x * 3)] = rgb[si + 2];     // B
                        row[(x * 3) + 1] = rgb[si + 1]; // G
                        row[(x * 3) + 2] = rgb[si];     // R
                    }
                    Marshal.Copy(row, 0, data.Scan0 + (y * data.Stride), row.Length);
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return Convert.ToBase64String(ms.ToArray());
        }
        catch
        {
            return null;
        }
    }
}
