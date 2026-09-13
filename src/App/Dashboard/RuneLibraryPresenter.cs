using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RuneshapePriceChecker.Runes;

namespace RuneshapePriceChecker.App.Dashboard;

/// <summary>
/// Maps the <see cref="RuneCatalog"/> into the dashboard's Rune Library views and pushes them
/// whenever the catalog changes (debounced), and wires the library's edits back into the
/// catalog. Lives in the app because the Dashboard assembly cannot reference the catalog.
/// </summary>
public sealed class RuneLibraryPresenter(
    RuneCatalog catalog,
    DashboardService dashboard,
    ILogger<RuneLibraryPresenter> logger) : IHostedService, IDisposable
{
    private static readonly TimeSpan PushDebounce = TimeSpan.FromMilliseconds(250);
    private readonly Dictionary<string, byte[]?> _iconCache = new(StringComparer.OrdinalIgnoreCase);
    private System.Threading.Timer? _pushTimer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        dashboard.SetRuneLibraryCallbacks(new RuneLibraryCallbacks
        {
            SetWeight = (runeId, weight) => catalog.SetWeight(runeId, weight),
            Bind = (bindingId, runeId) => catalog.Bind(bindingId, runeId),
            SetCarried = (id, carried) => catalog.SetCarried(id, carried),
            ResetCarried = catalog.ResetCarried,
            Forget = bindingId => catalog.RemoveBinding(bindingId)
        });
        catalog.Changed += SchedulePush;
        Push();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        catalog.Changed -= SchedulePush;
        return Task.CompletedTask;
    }

    private void SchedulePush()
    {
        _pushTimer ??= new System.Threading.Timer(_ => Push(), null, Timeout.Infinite, Timeout.Infinite);
        _ = _pushTimer.Change(PushDebounce, Timeout.InfiniteTimeSpan);
    }

    private void Push()
    {
        try
        {
            var bindings = catalog.Bindings;
            var boundByRune = bindings.Where(b => b.IsBound)
                .GroupBy(b => b.RuneId!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.SeenCount).First(), StringComparer.OrdinalIgnoreCase);
            var choices = catalog.Runes.Select(r => new RuneChoice(r.Id, r.DisplayName)).ToList();

            var runes = catalog.Runes.Select(r =>
            {
                boundByRune.TryGetValue(r.Id, out var bound);
                return new RuneLibraryEntryView
                {
                    Id = r.Id,
                    DisplayName = r.DisplayName,
                    Effect = r.Effect,
                    Rare = r.Rare,
                    Tier = r.Tier ?? "",
                    ReferenceIconPng = LoadIcon(r.Icon),
                    BindingId = bound?.Id,
                    Weight = catalog.GetWeight(r.Id),
                    IsCarried = catalog.IsCarried(r.Id),
                    SeenCount = bound?.SeenCount ?? 0,
                    SpritePng = bound?.SpritePngBase64 is { } b64 ? Convert.FromBase64String(b64) : null
                };
            }).ToList();

            var unbound = bindings.Where(b => !b.IsBound)
                .OrderByDescending(b => b.LastSeenUtc)
                .Select(b => new UnboundSpriteView
                {
                    BindingId = b.Id,
                    SpritePng = b.SpritePngBase64 is { } b64 ? Convert.FromBase64String(b64) : null,
                    SeenCount = b.SeenCount,
                    ColourHint = b.HueBucket is 6 or 7 ? "blue" : b.HueBucket is 8 or 9 ? "purple" : "",
                    CurrentWeight = catalog.UnboundWeight(b.HueBucket),
                    Choices = choices
                }).ToList();

            dashboard.SetRuneLibrary(runes, unbound);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Rune library push failed: {Context}", ErrorContext.FromException(ex));
        }
    }

    private byte[]? LoadIcon(string? icon)
    {
        if (string.IsNullOrEmpty(icon)) return null;
        if (!_iconCache.TryGetValue(icon, out var bytes))
        {
            bytes = RuneCatalog.LoadReferenceIcon(icon);
            _iconCache[icon] = bytes;
        }
        return bytes;
    }

    public void Dispose()
    {
        _pushTimer?.Dispose();
        _pushTimer = null;
    }
}
