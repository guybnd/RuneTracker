using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RuneshapePriceChecker.Configuration;
using RuneshapePriceChecker.Rumours;

namespace RuneshapePriceChecker.Overlay;

/// <summary>One mark to draw beside a rumour line.</summary>
/// <param name="Line">The rumour's text box, in client coordinates.</param>
/// <param name="Rating">Tier letter, or <c>?</c> when the line matched nothing.</param>
/// <param name="Detail">Map and mods, or the raw OCR text when the line matched nothing.</param>
/// <param name="Colour">Tier colour.</param>
/// <param name="IsBest">Whether this is the best-rated rumour on the panel.</param>
public sealed record RumourBadge(Rectangle Line, string Rating, string Detail, Color Colour, bool IsBest);

/// <summary>
/// Turning a read panel into marks, and drawing them. Kept free of WinForms so the placement and
/// the colours are pinned by tests on an offscreen bitmap.
/// </summary>
public static class RumourBadgePainter
{
    /// <summary>Exceptional — the one rumour worth rearranging an evening around.</summary>
    public static readonly Color SPlusColour = Color.FromArgb(244, 128, 246);

    /// <summary>Very good — rare yellow.</summary>
    public static readonly Color APlusColour = Color.FromArgb(255, 236, 110);

    /// <summary>Good.</summary>
    public static readonly Color AColour = Color.FromArgb(120, 216, 120);

    /// <summary>Fine — magic blue, which a player already reads as "nothing special".</summary>
    public static readonly Color BColour = Color.FromArgb(96, 176, 232);

    /// <summary>Weak.</summary>
    public static readonly Color CColour = Color.FromArgb(176, 176, 176);

    /// <summary>Skip it.</summary>
    public static readonly Color DColour = Color.FromArgb(214, 96, 96);

    /// <summary>Nothing in the table came close — the wording is one the tool has not been taught.</summary>
    public static readonly Color UnknownColour = Color.FromArgb(255, 150, 60);

    private static readonly Color Backdrop = Color.FromArgb(232, 18, 18, 20);

    /// <summary>Gap between a rumour's text and its mark, as a fraction of the line's height.</summary>
    private const double GapFraction = 0.4;

    public static Color ColourFor(string? rating) => (rating ?? "").Trim().ToUpperInvariant() switch
    {
        "S+" or "S" => SPlusColour,
        "A+" => APlusColour,
        "A" => AColour,
        "B+" or "B" => BColour,
        "C+" or "C" => CColour,
        "D" => DColour,
        _ => UnknownColour
    };

    /// <summary>The marks a panel deserves, in the order its lines are drawn.</summary>
    public static IReadOnlyList<RumourBadge> FromPanel(RumourPanel panel)
    {
        ArgumentNullException.ThrowIfNull(panel);

        var best = panel.BestIndex;
        var badges = new List<RumourBadge>(panel.Rumours.Count);
        for (var i = 0; i < panel.Rumours.Count; i++)
        {
            var reading = panel.Rumours[i];
            badges.Add(reading.Rumour is { } rumour
                ? new RumourBadge(reading.Bounds, rumour.Rating, $"{rumour.Map} · {rumour.Mods}", ColourFor(rumour.Rating), i == best)
                : new RumourBadge(reading.Bounds, "?", $"not in the table — read as \"{reading.Text}\"", UnknownColour, false));
        }
        return badges;
    }

    /// <summary>
    /// The mark a charted node's tooltip deserves — one badge beside its title. Never starred: the
    /// star means "the best of these", and one map is not a list to be best of.
    /// </summary>
    public static IReadOnlyList<RumourBadge> FromMap(RumourMapTooltip map)
    {
        ArgumentNullException.ThrowIfNull(map);

        return map.Rumour is { } rumour
            ? [new RumourBadge(map.Bounds, rumour.Rating, $"{rumour.Name} · {rumour.Mods}", ColourFor(rumour.Rating), false)]
            : [new RumourBadge(map.Bounds, "?", $"not in the table — read as \"{map.Title}\"", UnknownColour, false)];
    }

    /// <summary>
    /// Where the marks go: one column beside the panel, every mark on the same side and sharing an
    /// edge, each centred on its own line.
    ///
    /// The side is chosen once for the whole panel rather than per line. Deciding per line looks
    /// broken — the panel is only as wide as its longest rumour, so a panel near the right edge of
    /// the screen leaves room beside the short lines and not the long ones, and the marks end up
    /// alternating sides down the list. One side for all of them reads as a column instead.
    ///
    /// Every coordinate here is relative to the captured client area — the panel reader works on a
    /// bitmap of it, so its boxes start at (0,0) whatever the game window's position on screen.
    /// </summary>
    public static IReadOnlyList<Rectangle> LayoutColumn(IReadOnlyList<Rectangle> lines, IReadOnlyList<Size> sizes, Size surface)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(sizes);
        if (lines.Count == 0 || lines.Count != sizes.Count) return [];

        var gap = (int)(lines.Max(l => l.Height) * GapFraction);
        var rightOfLines = lines.Max(l => l.Right);
        var leftOfLines = lines.Min(l => l.Left);
        var widest = sizes.Max(s => s.Width);

        var onTheRight = rightOfLines + gap + widest <= surface.Width;

        var boxes = new List<Rectangle>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var size = sizes[i];
            var left = onTheRight ? rightOfLines + gap : leftOfLines - gap - size.Width;
            left = Math.Clamp(left, 0, Math.Max(0, surface.Width - size.Width));

            var top = lines[i].Top + ((lines[i].Height - size.Height) / 2);
            top = Math.Clamp(top, 0, Math.Max(0, surface.Height - size.Height));

            boxes.Add(new Rectangle(left, top, size.Width, size.Height));
        }
        return boxes;
    }

    /// <summary>Font height for a line of <paramref name="lineHeight"/> pixels, never unreadably small.</summary>
    public static float FontHeight(int lineHeight) => Math.Max(11f, lineHeight * 0.58f);

    /// <summary>Draws every mark onto a surface the size of the captured client area.</summary>
    public static void Paint(Graphics g, IReadOnlyList<RumourBadge> badges, Size surface)
    {
        ArgumentNullException.ThrowIfNull(g);
        ArgumentNullException.ThrowIfNull(badges);

        var fonts = new List<(Font Rating, Font Detail, string Text, SizeF RatingSize, SizeF DetailSize, int PadX, int PadY, int Spacing)>(badges.Count);
        var sizes = new List<Size>(badges.Count);
        try
        {
            foreach (var badge in badges)
            {
                var fontHeight = FontHeight(badge.Line.Height);
                var ratingFont = new Font(FontFamily.GenericSansSerif, fontHeight, FontStyle.Bold, GraphicsUnit.Pixel);
                var detailFont = new Font(FontFamily.GenericSansSerif, fontHeight * 0.82f, FontStyle.Regular, GraphicsUnit.Pixel);

                var ratingText = badge.IsBest ? $"★ {badge.Rating}" : badge.Rating;
                var ratingSize = g.MeasureString(ratingText, ratingFont);
                var detailSize = g.MeasureString(badge.Detail, detailFont);

                var padX = (int)(fontHeight * 0.55f);
                var padY = (int)(fontHeight * 0.30f);
                var spacing = (int)(fontHeight * 0.5f);

                fonts.Add((ratingFont, detailFont, ratingText, ratingSize, detailSize, padX, padY, spacing));
                sizes.Add(new Size(
                    (int)(ratingSize.Width + spacing + detailSize.Width) + (padX * 2),
                    (int)Math.Max(ratingSize.Height, detailSize.Height) + (padY * 2)));
            }

            var boxes = LayoutColumn([.. badges.Select(b => b.Line)], sizes, surface);

            using var backdrop = new SolidBrush(Backdrop);
            using var detailBrush = new SolidBrush(Color.FromArgb(226, 226, 230));

            for (var i = 0; i < badges.Count && i < boxes.Count; i++)
            {
                var badge = badges[i];
                var box = boxes[i];
                var f = fonts[i];

                using var accent = new SolidBrush(badge.Colour);
                using var edge = new Pen(badge.Colour, badge.IsBest ? 2f : 1f);

                g.FillRectangle(backdrop, box);
                g.DrawRectangle(edge, box.X, box.Y, box.Width - 1, box.Height - 1);
                g.DrawString(f.Text, f.Rating, accent, box.X + f.PadX, box.Y + f.PadY);
                g.DrawString(badge.Detail, f.Detail, detailBrush,
                    box.X + f.PadX + f.RatingSize.Width + f.Spacing,
                    box.Y + f.PadY + ((f.RatingSize.Height - f.DetailSize.Height) / 2));
            }
        }
        finally
        {
            foreach (var f in fonts) { f.Rating.Dispose(); f.Detail.Dispose(); }
        }
    }
}

/// <summary>
/// Draws the tier marks over the Uncharted Waters panel after a read, then takes them down again.
///
/// Nothing tells the tool when the panel closes — the player charts the area, or clicks away, and
/// the marks would otherwise sit over a map that has moved on. So they are shown for
/// <see cref="RumoursOptions.HoldSeconds"/> and removed, which is also what makes a second press
/// of the hotkey a refresh rather than a toggle.
/// </summary>
public sealed class RumourOverlay(
    IOptionsMonitor<RumoursOptions> rumoursOptions,
    IOptionsMonitor<AppOptions> appOptions,
    ILogger<RumourOverlay> logger) : IDisposable
{
    private readonly object _sync = new();
    private Thread? _thread;
    private RumourForm? _form;

    /// <summary>Shows the marks for a rumour list over the game client at <paramref name="client"/>.</summary>
    public void Show(RumourPanel panel, Rectangle client)
    {
        ArgumentNullException.ThrowIfNull(panel);
        Show(RumourBadgePainter.FromPanel(panel), client);
    }

    /// <summary>Shows the mark for a charted node's tooltip.</summary>
    public void Show(RumourMapTooltip map, Rectangle client)
    {
        ArgumentNullException.ThrowIfNull(map);
        Show(RumourBadgePainter.FromMap(map), client);
    }

    private void Show(IReadOnlyList<RumourBadge> badges, Rectangle client)
    {
        var options = rumoursOptions.CurrentValue;
        if (!options.Overlay || appOptions.CurrentValue.AllOverlaysDisabled) return;
        if (client.Width <= 0 || client.Height <= 0) return;

        try
        {
            var form = GetForm();
            if (form is null) return;

            var hold = TimeSpan.FromSeconds(Math.Clamp(options.HoldSeconds, 2, 120));
            logger.LogDebug("RumourOverlay: drawing {Count} marks for {Hold}", badges.Count, hold);
            form.SafeShow(client, badges, hold);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to draw the rumour marks: {Context}", ErrorContext.FromException(ex));
        }
    }

    /// <summary>Takes the marks down now.</summary>
    public void Hide()
    {
        RumourForm? form;
        lock (_sync) form = _form;
        form?.SafeHide();
    }

    private RumourForm? GetForm()
    {
        lock (_sync)
        {
            if (_form is { IsDisposed: false }) return _form;
            if (_thread is { IsAlive: true } && _form is { IsDisposed: false }) return _form;
            _thread = OverlayFormRunner.Start<RumourForm>(
                "RuneshapePriceChecker-Rumours",
                _sync,
                f => _form = f,
                logger,
                "Rumour overlay form creation timed out; the rumour marks will be unavailable.");
            return _form;
        }
    }

    public void Dispose()
    {
        RumourForm? form;
        lock (_sync) form = _form;
        form?.SafeClose();
    }

    private sealed class RumourForm : OverlayFormBase
    {
        private readonly System.Windows.Forms.Timer _timer = new();
        private IReadOnlyList<RumourBadge> _badges = [];

        protected override bool ClickThrough => true;

        public RumourForm()
        {
            Bounds = new Rectangle(-32000, -32000, 1, 1);
            _timer.Tick += (_, _) => { _timer.Stop(); SafeHide(); };
        }

        public void SafeShow(Rectangle client, IReadOnlyList<RumourBadge> badges, TimeSpan hold)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                _ = BeginInvoke(new Action<Rectangle, IReadOnlyList<RumourBadge>, TimeSpan>(SafeShow), client, badges, hold);
                return;
            }

            _badges = badges;
            _timer.Stop();
            _timer.Interval = (int)hold.TotalMilliseconds;
            _timer.Start();
            SafeShow(client);
        }

        public override void SafeHide()
        {
            if (!IsDisposed && !InvokeRequired) _timer.Stop();
            base.SafeHide();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_badges.Count == 0) return;

            ConfigureGraphics(e.Graphics);
            RumourBadgePainter.Paint(e.Graphics, _badges, ClientSize);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer.Dispose();
            base.Dispose(disposing);
        }
    }
}
