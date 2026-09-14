using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RuneshapePriceChecker.Configuration;
using RuneshapePriceChecker.OCR;
using RuneshapePriceChecker.Overlay;

namespace RuneshapePriceChecker.App;

/// <summary>
/// Marks the rumour list and charted maps without being asked, by watching the cursor rather than
/// the screen.
///
/// Scanning the screen on a timer is the obvious design and the wrong one: a full client read
/// costs about 230 ms at 2560x1440, so even once a second is a fifth of a core burned forever, on
/// top of the pricing loop already running at 100 ms. But neither panel appears on its own — both
/// are drawn against the node under the pointer — so the cursor says when there is anything worth
/// looking at, and asking it costs nothing.
///
/// So: poll the cursor position, and read only when it has come to rest. A read covers the region
/// around the cursor rather than the whole client, which is roughly 100 ms rather than 230.
///
/// Reading a resting place exactly once was the first attempt and it only half worked — charted
/// maps were marked and the rumour list was not. A map tooltip is already drawn by the time the
/// cursor has been still for <see cref="RumoursOptions.SettleMs"/>; the rumour list is not, whether
/// because it waits for a click or simply takes longer to appear. Either way the single read
/// happened before there was anything to see and the resting place was then considered done. So a
/// resting place is now read a few times before it is given up on.
///
/// Once something is marked the re-reads keep going, and become the answer to the opposite problem:
/// nothing tells the tool when the player closes the map. A read that finds the panel gone takes
/// the marks down within one interval, which is what <see cref="RumoursOptions.HoldSeconds"/> was
/// doing far too slowly on its own.
/// </summary>
public sealed class RumourWatchService(
    IOptionsMonitor<RumoursOptions> options,
    IPoe2WindowResolutionProvider windowResolution,
    RumourReadService reader,
    RumourOverlay overlay,
    ILogger<RumourWatchService> logger) : BackgroundService
{
    /// <summary>How far the cursor may drift and still count as the same resting place.</summary>
    private const int SettleSlopPixels = 6;

    /// <summary>How far it must move to count as a new resting place.</summary>
    private const int MovedPixels = 24;

    /// <summary>How far outside the marked panel the cursor may stray before the marks come down.</summary>
    private const int StrayMargin = 160;

    private Point _lastSeen;
    private DateTime _stillSince = DateTime.MinValue;
    private Point _restingAt = new(int.MinValue, int.MinValue);
    private int _readsAtRest;
    private DateTime _lastReadAt = DateTime.MinValue;
    private string _lastShown = "";
    private Rectangle _shownAt = Rectangle.Empty;
    private DateTime _shownUntil = DateTime.MinValue;

    /// <summary>Reads the cursor position. Replaced in tests.</summary>
    internal Func<Point>? CursorProvider { get; set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Rumour watch: started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = options.CurrentValue;

            try
            {
                if (settings.AutoScan) Tick(settings);
                else if (_lastShown.Length > 0) Clear();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Rumour watch: tick failed: {Context}", ErrorContext.FromException(ex));
            }

            try
            {
                await Task.Delay(Math.Clamp(settings.PollMs, 50, 2000), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void Tick(RumoursOptions settings)
    {
        if (!windowResolution.IsPoe2WindowForeground)
        {
            if (_lastShown.Length > 0) Clear();
            _stillSince = DateTime.MinValue;
            return;
        }

        ForgetExpiredMarks();

        var cursor = (CursorProvider ?? (() => Cursor.Position))();

        if (!IsWithin(cursor, _lastSeen, SettleSlopPixels))
        {
            _lastSeen = cursor;
            _stillSince = DateTime.UtcNow;

            // Moving away from what is marked is the cheapest possible sign the player is done
            // with it — no capture, no OCR, and it beats waiting out the hold timer.
            if (_lastShown.Length > 0 && HasStrayed(cursor)) Clear();
            return;
        }

        if ((DateTime.UtcNow - _stillSince).TotalMilliseconds < Math.Clamp(settings.SettleMs, 0, 5000)) return;

        if (!IsWithin(cursor, _restingAt, MovedPixels))
        {
            _restingAt = cursor;
            _readsAtRest = 0;
            _lastReadAt = DateTime.MinValue;
        }

        var rescan = Math.Clamp(settings.RescanMs, 100, 5000);
        if (_lastReadAt != DateTime.MinValue && (DateTime.UtcNow - _lastReadAt).TotalMilliseconds < rescan) return;

        // Give up on an empty resting place, but never on one that is showing something: those
        // re-reads are what notice the panel closing.
        if (_lastShown.Length == 0 && _readsAtRest >= Math.Clamp(settings.MaxReadsPerRest, 1, 20)) return;

        _lastReadAt = DateTime.UtcNow;
        _readsAtRest++;

        var result = reader.Read(aroundCursor: true);
        if (result.Outcome == RumourReadOutcome.Busy)
        {
            // The hotkey is mid-read. Try again next tick rather than spending one of the budget.
            _lastReadAt = DateTime.MinValue;
            _readsAtRest--;
            return;
        }

        if (!result.HasSomething)
        {
            if (_lastShown.Length > 0)
            {
                logger.LogDebug("Rumour watch: what was marked is gone; clearing");
                Clear();
            }
            return;
        }

        // The same thing in the same place is not news — redrawing on every re-read would be
        // wasted work. It is worth one redraw as the hold runs out, though: the panel is evidently
        // still open, so the marks should not blink off underneath it.
        var signature = Signature(result);
        var hold = TimeSpan.FromSeconds(Math.Clamp(options.CurrentValue.HoldSeconds, 2, 120));
        if (signature == _lastShown && DateTime.UtcNow < _shownUntil - RefreshLead) return;

        _lastShown = signature;
        _shownAt = ShownBounds(result);
        _shownUntil = DateTime.UtcNow + hold;

        if (result.Panel is { } panel) overlay.Show(panel, result.Client);
        else if (result.Map is { } map) overlay.Show(map, result.Client);
    }

    /// <summary>How long before the marks expire a confirming re-read redraws them.</summary>
    private static readonly TimeSpan RefreshLead = TimeSpan.FromSeconds(1);

    private void Clear()
    {
        _lastShown = "";
        _shownAt = Rectangle.Empty;
        _shownUntil = DateTime.MinValue;
        overlay.Hide();
    }

    /// <summary>
    /// Lets go of marks that have timed out on their own. Without this the watch would believe
    /// something was still on screen long after the overlay took it down, and would keep paying
    /// for re-reads to confirm it.
    /// </summary>
    private void ForgetExpiredMarks()
    {
        if (_lastShown.Length == 0 || DateTime.UtcNow < _shownUntil) return;
        _lastShown = "";
        _shownAt = Rectangle.Empty;
        _shownUntil = DateTime.MinValue;
    }

    /// <summary>Whether the cursor has left the neighbourhood of what is currently marked.</summary>
    private bool HasStrayed(Point cursor)
    {
        if (_shownAt.IsEmpty) return false;
        var box = _shownAt;
        box.Inflate(StrayMargin, StrayMargin);
        return !box.Contains(cursor);
    }

    /// <summary>What is marked, in screen coordinates, so a cursor position can be tested against it.</summary>
    internal static Rectangle ShownBounds(RumourReadResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var client = result.Client;
        if (result.Panel is { } panel)
            return new Rectangle(client.X + panel.Bounds.X, client.Y + panel.Bounds.Y, panel.Bounds.Width, panel.Bounds.Height);
        if (result.Map is { } map)
            return new Rectangle(client.X + map.Bounds.X, client.Y + map.Bounds.Y, map.Bounds.Width, map.Bounds.Height);
        return Rectangle.Empty;
    }

    /// <summary>What makes one sighting the same as the last: the same things named in the same places.</summary>
    internal static string Signature(RumourReadResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Signature();
    }

    private static bool IsWithin(Point a, Point b, int slop) =>
        Math.Abs(a.X - b.X) <= slop && Math.Abs(a.Y - b.Y) <= slop;
}
