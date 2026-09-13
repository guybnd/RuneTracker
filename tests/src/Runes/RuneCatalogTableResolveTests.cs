using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using RuneshapePriceChecker.Configuration;
using RuneshapePriceChecker.Contracts;
using RuneshapePriceChecker.Runes;
using Xunit;

namespace RuneshapePriceChecker.Tests.Runes;

/// <summary>
/// <see cref="RuneCatalog.Resolve(RuneKey, string?)"/>: the Combinations table names the rune,
/// and the library learns the sprite from it (RUNE-24).
/// </summary>
public sealed class RuneCatalogTableResolveTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "rpc-table-" + Guid.NewGuid().ToString("N"));
    private readonly RuneCatalog _catalog;

    public RuneCatalogTableResolveTests()
    {
        Directory.CreateDirectory(_dir);
        _catalog = new RuneCatalog(new StaticOptionsMonitor<RunesOptions>(new RunesOptions()), NullLogger.Instance, Path.Combine(_dir, "c.json"), RuneCatalog.LoadShippedJson());
    }

    public void Dispose()
    {
        _catalog.Dispose();
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); } catch { }
    }

    private static RuneKey Key(ulong hash) => new(hash, -1, new byte[RuneKey.SpriteSize * RuneKey.SpriteSize * 3], new Rectangle(57, 1, 52, 45), 1, 6);

    [Fact]
    public void KnownRune_ResolvesToItEvenWithNoStoredSprite()
    {
        var key = Key(0xDAC8B878FA5A5831);

        var res = _catalog.Resolve(key, "celestial");

        Assert.Equal("celestial", res.Rune?.Id);
        Assert.False(res.IsUnbound);
        Assert.Equal(_catalog.GetWeight("celestial"), res.Weight);
        Assert.Equal("celestial", res.CarriedId);
        Assert.Equal(RuneBinding.IdFor(key.ShapeHash), res.BindingId);
        Assert.Empty(_catalog.Bindings); // nothing to name yet — a first sighting is not persisted
    }

    [Fact]
    public void KnownRune_NamesAStoredUnboundSprite_AndRaisesChanged()
    {
        var key = Key(0xDAC8B878FA5A5831);
        _catalog.Observe(key);
        _catalog.Observe(key);
        var binding = Assert.Single(_catalog.Bindings);
        Assert.False(binding.IsBound);
        var revision = _catalog.Revision;
        var changed = 0;
        _catalog.Changed += () => changed++;

        var res = _catalog.Resolve(key, "celestial");

        Assert.Equal("celestial", res.Rune?.Id);
        Assert.Equal("celestial", Assert.Single(_catalog.Bindings).RuneId);
        Assert.True(_catalog.Revision > revision);
        Assert.Equal(1, changed);

        // Naming is a one-off: resolving again changes nothing.
        _ = _catalog.Resolve(key, "celestial");
        Assert.Equal(1, changed);
    }

    [Fact]
    public void KnownRune_CarriesTheSpritesCarriedFlagOverToTheRune()
    {
        var key = Key(0xDAC8B878FA5A5831);
        _catalog.Observe(key);
        _catalog.Observe(key);
        var binding = Assert.Single(_catalog.Bindings);
        _catalog.SetCarried(binding.Id, true);

        var res = _catalog.Resolve(key, "celestial");

        Assert.True(res.IsCarried);
        Assert.True(_catalog.IsCarried("celestial"));
        Assert.False(_catalog.IsCarried(binding.Id));
    }

    [Fact]
    public void KnownRune_LeavesAConflictingUserBindingAlone_ButScoresTheTablesRune()
    {
        var key = Key(0xDAC8B878FA5A5831);
        _catalog.Observe(key);
        _catalog.Observe(key);
        var binding = Assert.Single(_catalog.Bindings);
        Assert.True(_catalog.Bind(binding.Id, "fire"));

        var res = _catalog.Resolve(key, "celestial");

        Assert.Equal("celestial", res.Rune?.Id);
        Assert.Equal("fire", Assert.Single(_catalog.Bindings).RuneId);
    }

    [Fact]
    public void UnknownOrMissingRuneId_FallsBackToTheHash()
    {
        var key = Key(0xDAC8B878FA5A5831);

        Assert.True(_catalog.Resolve(key, null).IsUnbound);
        Assert.True(_catalog.Resolve(key, "not-a-rune").IsUnbound);

        _catalog.Observe(key);
        _catalog.Observe(key);
        _catalog.Bind(Assert.Single(_catalog.Bindings).Id, "fire");
        Assert.Equal("fire", _catalog.Resolve(key, null).Rune?.Id);
    }
}
