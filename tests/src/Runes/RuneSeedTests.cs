using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RuneshapePriceChecker.Configuration;
using RuneshapePriceChecker.Contracts;
using RuneshapePriceChecker.Runes;
using Xunit;

namespace RuneshapePriceChecker.Tests.Runes;

/// <summary>
/// The shipped sprite seed (<c>ocr/rune-seed.json</c>): naming a sprite takes a human, so the
/// names somebody has already given ship with the build rather than being re-earned per install.
/// </summary>
public class RuneSeedTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "rpc-seed-" + Guid.NewGuid().ToString("N"));

    private const string ShippedJson = """
    {"schema":1,"runes":[
      {"id":"opulent","displayName":"Opulent Rune","effect":"","tier":"gold","weight":15.0},
      {"id":"power","displayName":"Power Rune","effect":"","weight":14.0},
      {"id":"bond","displayName":"Bond Rune","effect":"","tier":"purple","weight":1.0}
    ]}
    """;

    private static readonly IOptionsMonitor<RunesOptions> Options = new StaticOptions(new RunesOptions());

    private string UserFile => Path.Combine(_dir, "rune-catalog.json");

    private RuneCatalog Open(string? seed) => new(Options, NullLogger.Instance, UserFile, ShippedJson, seed);

    /// <summary>A seed of two named sprites at the current hash generation.</summary>
    private static string Seed(int version = 1, int hashVersion = RuneCatalog.CurrentHashVersion) => $$"""
    {"seedVersion":{{version}},"hashVersion":{{hashVersion}},"bindings":[
      {"shapeHash":1234567890123456789,"hueBucket":1,"runeId":"opulent","spritePngBase64":"AAAA"},
      {"shapeHash":17790598031390427477,"hueBucket":9,"runeId":"power","spritePngBase64":"BBBB"}
    ]}
    """;

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); } catch { }
    }

    [Fact]
    public void AFreshLibraryStartsWithTheShippedNamesAlreadyBound()
    {
        using var catalog = Open(Seed());

        Assert.Equal(2, catalog.Bindings.Count);
        Assert.All(catalog.Bindings, b => Assert.True(b.IsBound));
        Assert.Equal("opulent", catalog.Bindings.Single(b => b.ShapeHash == 1234567890123456789).RuneId);

        // A hash above long.MaxValue must survive the round trip, or half the seed would be
        // silently mangled: shape hashes are unsigned and use the full 64 bits.
        Assert.Equal("power", catalog.Bindings.Single(b => b.ShapeHash == 17790598031390427477).RuneId);
    }

    [Fact]
    public void SeedingRunsOncePerVersion_SoAForgottenSpriteStaysForgotten()
    {
        using (var first = Open(Seed()))
        {
            Assert.True(first.RemoveBinding(RuneBinding.IdFor(1234567890123456789)));
            first.Flush();
        }

        using var reopened = Open(Seed());

        // The user threw that sprite away on purpose. Handing it back every launch would make the
        // Forget button look broken.
        var binding = Assert.Single(reopened.Bindings);
        Assert.Equal("power", binding.RuneId);
    }

    [Fact]
    public void ANewerSeedAddsItsNewSpritesToAnExistingLibrary()
    {
        using (var first = Open(Seed())) { first.Flush(); }

        var grown = $$"""
        {"seedVersion":2,"hashVersion":{{RuneCatalog.CurrentHashVersion}},"bindings":[
          {"shapeHash":1234567890123456789,"hueBucket":1,"runeId":"opulent","spritePngBase64":"AAAA"},
          {"shapeHash":999,"hueBucket":9,"runeId":"bond","spritePngBase64":"CCCC"}
        ]}
        """;
        using var reopened = Open(grown);

        Assert.Equal(3, reopened.Bindings.Count);
        Assert.Contains(reopened.Bindings, b => b.RuneId == "bond");
        Assert.Single(reopened.Bindings.Where(b => b.ShapeHash == 1234567890123456789)); // not duplicated
    }

    [Fact]
    public void TheUsersOwnNamingIsNeverOverwritten()
    {
        using (var first = Open(seed: null))
        {
            var key = new RuneKey(1234567890123456789, 1, new byte[RuneKey.SpriteSize * RuneKey.SpriteSize * 3], new Rectangle(0, 0, 45, 45));
            first.Observe(key);
            first.Observe(key);
            Assert.True(first.Bind(RuneBinding.IdFor(key.ShapeHash), "bond"));
            first.Flush();
        }

        using var reopened = Open(Seed());

        // The seed calls that hash Opulent; the user called it Bond. The user wins.
        Assert.Equal("bond", reopened.Bindings.Single(b => b.ShapeHash == 1234567890123456789).RuneId);
    }

    [Fact]
    public void ASeedFromAnotherHashGenerationIsRefused()
    {
        // Its hashes name crops the detector no longer produces, so every entry would be dead
        // weight that could never match a key again.
        using var catalog = Open(Seed(hashVersion: RuneCatalog.CurrentHashVersion - 1));
        Assert.Empty(catalog.Bindings);
    }

    [Fact]
    public void AMissingOrBrokenSeedLeavesTheLibraryAlone()
    {
        using (var none = Open(seed: null)) Assert.Empty(none.Bindings);
        using var broken = Open("{ not json");
        Assert.Empty(broken.Bindings);
    }

    [Fact]
    public void SeededSpritesNameRunesTheShippedCatalogActuallyHas()
    {
        var bogus = $$"""
        {"seedVersion":1,"hashVersion":{{RuneCatalog.CurrentHashVersion}},"bindings":[
          {"shapeHash":111,"hueBucket":1,"runeId":"no-such-rune","spritePngBase64":"AAAA"},
          {"shapeHash":222,"hueBucket":1,"spritePngBase64":"AAAA"}
        ]}
        """;
        using var catalog = Open(bogus);
        Assert.Empty(catalog.Bindings);
    }

    [Fact]
    public void TheShippedSeedIsCurrentAndCoversTheRuneList()
    {
        var json = RuneCatalog.LoadSeedJson();
        Assert.NotNull(json);

        var seed = JsonSerializer.Deserialize<RuneSeedFile>(json!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(seed);

        // If CurrentHashVersion is bumped without re-exporting, the seed silently stops applying
        // and every new install is back to naming 33 sprites by hand. Fail loudly here instead.
        Assert.Equal(RuneCatalog.CurrentHashVersion, seed!.HashVersion);
        Assert.True(seed.SeedVersion > 0, "the shipped seed must carry a version so installs can tell it apart");

        var named = seed.Bindings.Select(b => b.RuneId).Where(id => !string.IsNullOrEmpty(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        Assert.True(named.Count >= 30, $"the shipped seed covers only {named.Count} rune(s)");
        Assert.All(seed.Bindings, b => Assert.False(string.IsNullOrEmpty(b.SpritePngBase64)));

        // Every name in the seed must exist in the catalog it ships beside.
        using var catalog = new RuneCatalog(Options, NullLogger.Instance, UserFile, RuneCatalog.LoadShippedJson(), json);
        Assert.Equal(seed.Bindings.Count, catalog.Bindings.Count);
        Assert.All(catalog.Bindings, b => Assert.True(b.IsBound));
    }

    private sealed class StaticOptions(RunesOptions value) : IOptionsMonitor<RunesOptions>
    {
        public RunesOptions CurrentValue => value;
        public RunesOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<RunesOptions, string?> listener) => null;
    }
}
