using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RuneshapePriceChecker.App;
using RuneshapePriceChecker.Configuration;
using RuneshapePriceChecker.Contracts;
using RuneshapePriceChecker.Runes;
using Xunit;

namespace RuneshapePriceChecker.Tests.Runes;

public class RuneCatalogTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "rpc-runes-" + Guid.NewGuid().ToString("N"));

    private const string ShippedJson = """
    {"schema":1,"runes":[
      {"id":"opulent","displayName":"Opulent Rune","effect":"Increased Monster Rarity","tier":"gold","weight":3.0,"icon":"opulent.png"},
      {"id":"power","displayName":"Power Rune","effect":"Empowered","rare":true,"weight":2.0},
      {"id":"ward","displayName":"Ward Rune","effect":"Protected by Runic Ward","rare":true,"tier":"blue","weight":0.5}
    ]}
    """;

    private static IOptionsMonitor<RunesOptions> Options(Action<RunesOptions>? configure = null)
    {
        var o = new RunesOptions();
        configure?.Invoke(o);
        return new StaticOptions(o);
    }

    private RuneCatalog NewCatalog(IOptionsMonitor<RunesOptions>? options = null)
        => new(options ?? Options(), NullLogger.Instance, Path.Combine(_dir, "rune-catalog.json"), ShippedJson);

    private static RuneKey Key(ulong hash, int hue = 9) => new(hash, hue, new byte[32 * 32 * 3], new Rectangle(10, 20, 45, 45));

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); } catch { }
    }

    [Fact]
    public void LoadsShippedRunesWithWeights()
    {
        using var catalog = NewCatalog();

        Assert.Equal(3, catalog.Runes.Count);
        Assert.Equal(3.0, catalog.GetWeight("opulent"));
        Assert.Equal(2.0, catalog.GetWeight("power"));
        Assert.Equal(0.5, catalog.GetWeight("ward"));
    }

    [Fact]
    public void FirstSightingIsNotPersisted_SecondSightingIs()
    {
        using var catalog = NewCatalog();
        var key = Key(0xABCDEF0123456789);

        Assert.False(catalog.Observe(key));
        Assert.Empty(catalog.Bindings);

        Assert.True(catalog.Observe(key));
        var binding = Assert.Single(catalog.Bindings);
        Assert.Equal(2, binding.SeenCount);
        Assert.False(binding.IsBound);
        Assert.Equal(RuneBinding.IdFor(key.ShapeHash), binding.Id);
    }

    [Fact]
    public void NearHashMatchesExistingBinding_HueDifferenceDoesNotSplit()
    {
        using var catalog = NewCatalog();
        var key = Key(0xF000000000000000, hue: 9);
        catalog.Observe(key);
        catalog.Observe(key);

        var jittered = Key(0xF000000000000007, hue: 1); // 3 bits apart, different hue
        catalog.Observe(jittered);

        var binding = Assert.Single(catalog.Bindings);
        Assert.Equal(3, binding.SeenCount);
        Assert.Equal(1, binding.HueBucket); // latest sighting wins, advisory only
    }

    [Fact]
    public void UnboundCapIsEnforced()
    {
        using var catalog = NewCatalog(Options(o => o.MaxUnboundBindings = 2));
        // Hashes far apart (32+ bits) so the matcher treats them as four distinct runes.
        foreach (var hash in new ulong[] { 0x0000FFFF0000FFFF, 0xFFFF0000FFFF0000, 0x00FF00FF00FF00FF, 0xFF00FF00FF00FF00 })
        {
            var key = Key(hash);
            catalog.Observe(key);
            catalog.Observe(key);
        }

        Assert.Equal(2, catalog.Bindings.Count(b => !b.IsBound));
    }

    [Fact]
    public void BindMigratesCarriedIdAndPersistsAcrossReload()
    {
        var key = Key(0x1234567890ABCDEF);
        using (var catalog = NewCatalog())
        {
            catalog.Observe(key);
            catalog.Observe(key);
            var bindingId = catalog.Bindings.Single().Id;
            catalog.SetCarried(bindingId, true);
            Assert.True(catalog.IsCarried(bindingId));

            Assert.True(catalog.Bind(bindingId, "power"));
            Assert.False(catalog.IsCarried(bindingId));
            Assert.True(catalog.IsCarried("power"));
            catalog.SetWeight("opulent", 4.5);
            catalog.Flush();
        }

        using (var reloaded = NewCatalog())
        {
            var binding = Assert.Single(reloaded.Bindings);
            Assert.Equal("power", binding.RuneId);
            Assert.True(reloaded.IsCarried("power"));
            Assert.Equal(4.5, reloaded.GetWeight("opulent"));
            Assert.Equal(2.0, reloaded.GetWeight("power")); // untouched shipped weight
        }
    }

    [Fact]
    public void ResolveUsesHueRuleForUnboundAndNeverGold()
    {
        using var catalog = NewCatalog();
        var dark = Key(0x1111, hue: 1);
        var blue = Key(0x2222, hue: 6);
        var purple = Key(0x3333, hue: 9);

        Assert.Equal(1.0, catalog.Resolve(dark).Weight);
        Assert.Equal(0.5, catalog.Resolve(blue).Weight);
        Assert.Equal(1.0, catalog.Resolve(purple).Weight);
        Assert.True(catalog.Resolve(dark).IsUnbound);
        Assert.All(new[] { dark, blue, purple }, k => Assert.NotEqual(3.0, catalog.Resolve(k).Weight));
    }

    [Fact]
    public void RevisionBumpsOnMutationsButNotOnPlainSightings()
    {
        using var catalog = NewCatalog();
        var key = Key(0x5555);
        catalog.Observe(key);
        catalog.Observe(key); // promotion → bump
        var afterPromotion = catalog.Revision;

        catalog.Observe(key); // plain sighting → no bump
        Assert.Equal(afterPromotion, catalog.Revision);

        catalog.SetCarried("power", true);
        Assert.True(catalog.Revision > afterPromotion);

        var beforeReset = catalog.Revision;
        catalog.ResetCarried();
        Assert.True(catalog.Revision > beforeReset);
        Assert.Empty(catalog.Carried);
    }

    [Fact]
    public void RemoveBindingForgetsSpriteAndCarriedFlag()
    {
        using var catalog = NewCatalog();
        var key = Key(0x7777);
        catalog.Observe(key);
        catalog.Observe(key);
        var id = catalog.Bindings.Single().Id;
        catalog.SetCarried(id, true);

        Assert.True(catalog.RemoveBinding(id));
        Assert.Empty(catalog.Bindings);
        Assert.False(catalog.IsCarried(id));
    }

    // Distinct sprites must be further apart than RunesOptions.MatchHammingThreshold (8 bits) or
    // the matcher folds them into one binding. These three are 32+ bits apart from each other.
    private const ulong HashA = 0x0000000000000000;
    private const ulong HashB = 0x00000000FFFFFFFF;
    private const ulong HashC = 0xFFFFFFFF00000000;

    [Fact]
    public void ForgetAllUnboundKeepsBoundSpritesAndTheirCarriedFlags()
    {
        using var catalog = NewCatalog();
        foreach (var hash in new[] { HashA, HashB, HashC })
        {
            catalog.Observe(Key(hash));
            catalog.Observe(Key(hash));
        }

        Assert.True(catalog.Bind(RuneBinding.IdFor(HashA), "opulent"));
        catalog.SetCarried("opulent", true);
        catalog.SetCarried(RuneBinding.IdFor(HashB), true);
        Assert.Equal(3, catalog.Bindings.Count);

        Assert.Equal(2, catalog.ForgetAllUnbound());

        var survivor = Assert.Single(catalog.Bindings);
        Assert.Equal("opulent", survivor.RuneId);
        Assert.True(catalog.IsCarried("opulent"));
        Assert.False(catalog.IsCarried(RuneBinding.IdFor(HashB)));
    }

    [Fact]
    public void ForgetAllUnboundOnACleanCatalogChangesNothing()
    {
        using var catalog = NewCatalog();
        var before = catalog.Revision;

        Assert.Equal(0, catalog.ForgetAllUnbound());
        Assert.Equal(before, catalog.Revision);
    }

    [Fact]
    public void ForgetAllUnboundAlsoDropsFirstSightingsStillPending()
    {
        // A sprite seen once lives in the pending map, not the binding list. Leaving it there
        // would resurrect the junk on its very next sighting, so "Forget all" would not stick.
        using var catalog = NewCatalog();
        var junk = Key(HashA);
        catalog.Observe(junk);
        catalog.Observe(junk);
        var pendingOnly = Key(HashC);
        catalog.Observe(pendingOnly);

        Assert.Equal(1, catalog.ForgetAllUnbound());

        catalog.Observe(pendingOnly);
        Assert.Empty(catalog.Bindings);
    }

    [Fact]
    public void ShippedCatalogFileHasAll34RunesWithWeights()
    {
        var json = RuneCatalog.LoadShippedJson();
        using var catalog = new RuneCatalog(Options(), NullLogger.Instance, Path.Combine(_dir, "x.json"), json);

        Assert.Equal(34, catalog.Runes.Count);
        Assert.Equal(3.0, catalog.GetWeight("opulent"));
        Assert.Equal(2.0, catalog.GetWeight("power"));
        Assert.Equal(0.5, catalog.GetWeight("ward"));
        Assert.Equal(0.5, catalog.GetWeight("volcanic"));
        Assert.Equal(1.0, catalog.GetWeight("bond"));
        Assert.Equal(34, catalog.Runes.Select(r => r.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains(catalog.Runes, r => r.Id == "bait");
        Assert.NotNull(RuneCatalog.LoadReferenceIcon("opulent.png"));
    }

    [Theory]
    [InlineData("Ctrl+Alt+R", 0x0002u | 0x0001u, (uint)'R')]
    [InlineData("shift+F9", 0x0004u, (uint)Keys.F9)]
    [InlineData("ctrl + numpad0", 0x0002u, (uint)Keys.NumPad0)]
    public void HotkeyParse_Accepts(string text, uint modifiers, uint vk)
    {
        Assert.True(GlobalHotkeyService.TryParse(text, out var m, out var k));
        Assert.Equal(modifiers, m);
        Assert.Equal(vk, k);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Ctrl+Alt")]
    [InlineData("Ctrl+R+S")]
    [InlineData("Bogus+Q")]
    public void HotkeyParse_Rejects(string text)
    {
        Assert.False(GlobalHotkeyService.TryParse(text, out _, out _));
    }

    private sealed class StaticOptions(RunesOptions value) : IOptionsMonitor<RunesOptions>
    {
        public RunesOptions CurrentValue => value;
        public RunesOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<RunesOptions, string?> listener) => null;
    }
}
