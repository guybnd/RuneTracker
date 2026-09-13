using System.Drawing.Drawing2D;
using System.Drawing.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RuneshapePriceChecker.Configuration;
using RuneshapePriceChecker.OCR;
using RuneshapePriceChecker.Runes;

namespace RuneshapePriceChecker.Overlay;

/// <summary>One rune in the magazine strip: what to draw for it.</summary>
public sealed record MagazineEntry(string Id, string DisplayName, byte[]? IconPng);

/// <summary>
/// Geometry of the magazine strip, kept pure so the arithmetic that keeps it inside the game
/// window and maps a click to a row can be tested without opening anything.
/// </summary>
public static class RuneMagazinePainter
{
    public const int IconSize = 40;
    public const int RowGap = 6;
    public const int SidePad = 8;
    public const int TopPad = 6;
    public const int ResetHeight = 26;

    /// <summary>Painted width of the strip. Icons only — the name appears on hover.</summary>
    public const int StripWidth = IconSize + (SidePad * 2);

    /// <summary>Transparent gutter to the right of the strip, where the hover label is drawn.</summary>
    public const int LabelWidth = 190;

    public static int WindowWidth => StripWidth + LabelWidth;

    /// <summary>Y of the icon area's first row, below the reset button.</summary>
    public static int ContentTop => TopPad + ResetHeight + RowGap;

    public static Rectangle ResetButton => new(SidePad, TopPad, IconSize, ResetHeight);

    /// <summary>Full height every row needs, ignoring the viewport.</summary>
    public static int ContentHeight(int count) => count <= 0 ? 0 : (count * (IconSize + RowGap)) - RowGap;

    /// <summary>Height of the scrolling area inside a strip of <paramref name="stripHeight"/>.</summary>
    public static int ViewportHeight(int stripHeight) => Math.Max(0, stripHeight - ContentTop - TopPad);

    /// <summary>
    /// The icon rectangle for a row, already scrolled. May fall outside the viewport; callers clip.
    /// </summary>
    public static Rectangle IconAt(int index, int scroll) =>
        new(SidePad, ContentTop + (index * (IconSize + RowGap)) - scroll, IconSize, IconSize);

    /// <summary>Largest scroll offset that still shows content, so the list cannot be dragged past its end.</summary>
    public static int MaxScroll(int count, int stripHeight) =>
        Math.Max(0, ContentHeight(count) - ViewportHeight(stripHeight));

    /// <summary>Row index at a point in strip coordinates, or -1. Only rows inside the viewport count.</summary>
    public static int RowAt(Point point, int count, int scroll, int stripHeight)
    {
        var viewportBottom = ContentTop + ViewportHeight(stripHeight);
        if (point.Y < ContentTop || point.Y >= viewportBottom) return -1;

        for (var i = 0; i < count; i++)
        {
            var rect = IconAt(i, scroll);
            if (rect.Contains(point)) return i;
        }
        return -1;
    }
}

/// <summary>
/// Draws the player's magazine — the succession runes taken this run — as a narrow column of
/// icons against the game window's left edge. Hovering an icon names it, right-clicking removes
/// it, and a button at the top empties the magazine.
///
/// Unlike the marker overlay this one is <b>not</b> click-through: it has to receive hover and
/// right-click. It stays <c>WS_EX_NOACTIVATE</c> so it never takes focus from the game, and the
/// window is <i>shaped</i> to just the reset button, the visible icons and the hover label — a
/// chroma-keyed pixel is still part of a window for hit-testing, so painting no background was
/// not enough and the strip swallowed clicks across a tall column of the screen.
/// </summary>
public sealed class RuneMagazineOverlay(
    RuneCatalog catalog,
    IPoe2WindowResolutionProvider windowResolutionProvider,
    IOptionsMonitor<RunesOptions> runesOptions,
    IOptionsMonitor<AppOptions> appOptions,
    ILogger<RuneMagazineOverlay> logger) : IDisposable
{
    private readonly object _sync = new();
    private readonly Dictionary<string, byte[]?> _iconCache = new(StringComparer.OrdinalIgnoreCase);
    private Thread? _overlayThread;
    private MagazineForm? _overlayForm;
    private string _lastSignature = string.Empty;

    public void Render(bool interfaceDetected)
    {
        try
        {
            var runes = runesOptions.CurrentValue;
            if (!runes.MagazineOverlay || appOptions.CurrentValue.AllOverlaysDisabled)
            {
                Hide();
                return;
            }

            var context = windowResolutionProvider.CurrentWindowCaptureContext;
            if (context is null || !windowResolutionProvider.IsPoe2WindowForeground)
            {
                Hide();
                return;
            }

            var entries = BuildEntries();
            if (entries.Count == 0)
            {
                Hide();
                return;
            }

            var signature = $"{context.ClientX},{context.ClientY},{context.ClientHeight}|{string.Join(",", entries.Select(e => e.Id))}";
            if (signature == _lastSignature) return;

            EnsureOverlayThreadStarted();
            var form = GetOverlayForm();
            if (form is null) return;

            _lastSignature = signature;
            form.SafeShow(context, entries, Dismiss, ResetAll);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to render the rune magazine: {Context}", ErrorContext.FromException(ex));
        }
    }

    private void Dismiss(string id)
    {
        logger.LogInformation("Magazine: {Id} removed from the strip", id);
        catalog.SetCarried(id, false);
        _lastSignature = string.Empty; // force a redraw on the next cycle
    }

    private void ResetAll()
    {
        logger.LogInformation("Magazine: emptied from the strip");
        catalog.ResetCarried();
        _lastSignature = string.Empty;
    }

    /// <summary>
    /// The carried set as drawable rows. A rune that has been named shows its reference glyph; one
    /// still unnamed shows the sprite as read from the panel, so a magazine filled by hotkey
    /// before any naming is still readable.
    /// </summary>
    private List<MagazineEntry> BuildEntries()
    {
        var carried = catalog.Carried.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (carried.Count == 0) return [];

        var entries = new List<MagazineEntry>();
        foreach (var rune in catalog.Runes)
        {
            if (!carried.Remove(rune.Id)) continue;
            entries.Add(new MagazineEntry(rune.Id, rune.DisplayName, LoadIcon(rune.Icon)));
        }

        // Whatever is left is carried under a binding id — an unnamed sprite.
        foreach (var binding in catalog.Bindings)
        {
            if (!carried.Remove(binding.Id)) continue;
            var sprite = binding.SpritePngBase64 is { } b64 ? Convert.FromBase64String(b64) : null;
            entries.Add(new MagazineEntry(binding.Id, "Unnamed rune", sprite));
        }

        return entries;
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

    public void Hide()
    {
        _lastSignature = string.Empty;
        GetOverlayForm()?.SafeHide();
    }

    private void EnsureOverlayThreadStarted()
    {
        lock (_sync)
        {
            if (_overlayThread is { IsAlive: true } && _overlayForm is { IsDisposed: false })
                return;
        }

        _overlayThread = OverlayFormRunner.Start<MagazineForm>(
            "RuneMagazineOverlay",
            _sync,
            f => _overlayForm = f,
            logger);
    }

    private MagazineForm? GetOverlayForm()
    {
        lock (_sync) return _overlayForm is { IsDisposed: false } f ? f : null;
    }

    public void Dispose() => GetOverlayForm()?.SafeClose();

    private sealed class MagazineForm : Form
    {
        private static readonly Color TransparencyChroma = Color.FromArgb(1, 2, 3);
        private readonly object _stateSync = new();
        private List<MagazineEntry> _entries = [];
        private readonly Dictionary<string, Image?> _images = new(StringComparer.OrdinalIgnoreCase);
        private Action<string>? _onDismiss;
        private Action? _onResetAll;
        private int _scroll;
        private int _hoverRow = -1;
        private bool _hoverReset;

        public MagazineForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = TransparencyChroma;
            TransparencyKey = TransparencyChroma;
            DoubleBuffered = true;
            Bounds = new Rectangle(-32000, -32000, 1, 1);
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE — never steals focus from the game
                return cp;                // deliberately NOT WS_EX_TRANSPARENT: this strip is interactive
            }
        }

        public void SafeShow(WindowCaptureContext context, List<MagazineEntry> entries, Action<string> onDismiss, Action onResetAll)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                _ = BeginInvoke(new Action<WindowCaptureContext, List<MagazineEntry>, Action<string>, Action>(SafeShow), context, entries, onDismiss, onResetAll);
                return;
            }

            lock (_stateSync)
            {
                _entries = entries;
                _onDismiss = onDismiss;
                _onResetAll = onResetAll;
                foreach (var image in _images.Values) image?.Dispose();
                _images.Clear();
                foreach (var entry in entries)
                    _images[entry.Id] = Decode(entry.IconPng);
            }

            Bounds = new Rectangle(context.ClientX, context.ClientY, RuneMagazinePainter.WindowWidth, Math.Max(1, context.ClientHeight));
            ClampScroll();
            UpdateRegion();
            Invalidate();
            PinTopMost();
            if (!Visible)
            {
                Show();
                PinTopMost();
            }
        }

        private void ClampScroll()
        {
            int count;
            lock (_stateSync) count = _entries.Count;
            _scroll = Math.Clamp(_scroll, 0, RuneMagazinePainter.MaxScroll(count, Height));
        }

        /// <summary>
        /// Shapes the window to exactly the reset button, the visible icons, and the hover label
        /// when one is showing.
        ///
        /// A chroma-keyed pixel is still part of the window as far as hit-testing is concerned, so
        /// painting no background was not enough — the strip still claimed a tall column of the
        /// screen and swallowed clicks meant for the game. A region removes those pixels from the
        /// window entirely: outside it there is nothing to click on.
        /// </summary>
        private void UpdateRegion()
        {
            List<MagazineEntry> entries;
            lock (_stateSync) entries = _entries;

            using var path = new GraphicsPath();
            path.AddRectangle(RuneMagazinePainter.ResetButton);

            var viewportTop = RuneMagazinePainter.ContentTop;
            var viewportBottom = viewportTop + RuneMagazinePainter.ViewportHeight(Height);
            for (var i = 0; i < entries.Count; i++)
            {
                var rect = RuneMagazinePainter.IconAt(i, _scroll);
                if (rect.Bottom < viewportTop || rect.Top > viewportBottom) continue;
                path.AddRectangle(Rectangle.Intersect(rect, new Rectangle(0, viewportTop, Width, viewportBottom - viewportTop)));
            }

            if (_hoverRow >= 0 && _hoverRow < entries.Count)
                path.AddRectangle(HoverLabelBounds(RuneMagazinePainter.IconAt(_hoverRow, _scroll)));
            else if (_hoverReset)
                path.AddRectangle(HoverLabelBounds(RuneMagazinePainter.ResetButton));

            Region = new Region(path);
        }

        /// <summary>
        /// Where the hover label sits for a given anchor. Shared by painting and by the region, so
        /// the label is never drawn into pixels the window does not own.
        /// </summary>
        private Rectangle HoverLabelBounds(Rectangle anchor)
        {
            const int height = 26;
            var top = Math.Clamp(anchor.Top + ((anchor.Height - height) / 2), 0, Math.Max(0, Height - height));
            return new Rectangle(RuneMagazinePainter.StripWidth + 6, top, RuneMagazinePainter.LabelWidth - 8, height);
        }

        private static Image? Decode(byte[]? png)
        {
            if (png is null || png.Length == 0) return null;
            try
            {
                using var ms = new MemoryStream(png);
                return Image.FromStream(ms);
            }
            catch
            {
                return null;
            }
        }

        public void SafeHide()
        {
            if (IsDisposed) return;
            if (InvokeRequired) { _ = BeginInvoke(SafeHide); return; }
            if (Visible) Hide();
        }

        public void SafeClose()
        {
            if (IsDisposed) return;
            if (InvokeRequired) { _ = BeginInvoke(SafeClose); return; }
            Close();
        }

        private void PinTopMost()
        {
            try { TopMost = false; TopMost = true; } catch { }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int count;
            lock (_stateSync) count = _entries.Count;

            var row = RuneMagazinePainter.RowAt(e.Location, count, _scroll, Height);
            var overReset = RuneMagazinePainter.ResetButton.Contains(e.Location);
            if (row != _hoverRow || overReset != _hoverReset)
            {
                _hoverRow = row;
                _hoverReset = overReset;
                UpdateRegion(); // the hover label needs pixels the window owns
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_hoverRow != -1 || _hoverReset)
            {
                _hoverRow = -1;
                _hoverReset = false;
                UpdateRegion();
                Invalidate();
            }
            base.OnMouseLeave(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            int count;
            lock (_stateSync) count = _entries.Count;

            var max = RuneMagazinePainter.MaxScroll(count, Height);
            if (max > 0)
            {
                _scroll = Math.Clamp(_scroll - (e.Delta / 2), 0, max);
                UpdateRegion();
                Invalidate();
            }
            base.OnMouseWheel(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            List<MagazineEntry> entries;
            Action<string>? dismiss;
            Action? reset;
            lock (_stateSync)
            {
                entries = _entries;
                dismiss = _onDismiss;
                reset = _onResetAll;
            }

            if (e.Button == MouseButtons.Left && RuneMagazinePainter.ResetButton.Contains(e.Location))
            {
                reset?.Invoke();
                base.OnMouseDown(e);
                return;
            }

            if (e.Button == MouseButtons.Right)
            {
                var row = RuneMagazinePainter.RowAt(e.Location, entries.Count, _scroll, Height);
                if (row >= 0 && row < entries.Count)
                {
                    var id = entries[row].Id;
                    lock (_stateSync)
                    {
                        _entries = entries.Where((_, i) => i != row).ToList();
                        if (_images.Remove(id, out var image)) image?.Dispose();
                    }
                    _hoverRow = -1;
                    ClampScroll();
                    UpdateRegion();
                    Invalidate();
                    dismiss?.Invoke(id);
                }
            }

            base.OnMouseDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            List<MagazineEntry> entries;
            lock (_stateSync) entries = _entries;

            var g = e.Graphics;
            g.Clear(TransparencyChroma);
            if (entries.Count == 0) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            // No panel behind the strip: everything left unpainted stays chroma-keyed, which is
            // both transparent and click-through, so the column only occupies the pixels it
            // actually uses and the game keeps the rest of its left edge.
            using var edge = new Pen(Color.FromArgb(200, 90, 80, 40), 1f);

            DrawResetButton(g);

            var viewport = new Rectangle(0, RuneMagazinePainter.ContentTop, RuneMagazinePainter.StripWidth, RuneMagazinePainter.ViewportHeight(Height));
            var clip = g.Clip;
            g.SetClip(viewport);

            using var slotPen = new Pen(Color.FromArgb(150, 140, 128, 80), 1f);
            using var hoverPen = new Pen(Color.FromArgb(255, 255, 224, 102), 2f);
            using var slotFill = new SolidBrush(Color.FromArgb(140, 10, 12, 16));

            for (var i = 0; i < entries.Count; i++)
            {
                var rect = RuneMagazinePainter.IconAt(i, _scroll);
                if (rect.Bottom < viewport.Top || rect.Top > viewport.Bottom) continue;

                // Each icon carries its own small backdrop so it stays readable against whatever
                // is behind it, without the strip claiming a column of the screen.
                g.FillRectangle(slotFill, rect);
                Image? image;
                lock (_stateSync) _ = _images.TryGetValue(entries[i].Id, out image);
                if (image is not null) g.DrawImage(image, rect);
                g.DrawRectangle(i == _hoverRow ? hoverPen : slotPen, rect);
            }

            g.Clip = clip;

            // More below than fits: a small chevron, so a clipped list does not look like the end.
            if (_scroll < RuneMagazinePainter.MaxScroll(entries.Count, Height))
            {
                using var more = new SolidBrush(Color.FromArgb(200, 255, 224, 102));
                var cx = RuneMagazinePainter.StripWidth / 2;
                var cy = viewport.Bottom + 4;
                g.FillPolygon(more, [new Point(cx - 5, cy), new Point(cx + 5, cy), new Point(cx, cy + 5)]);
            }

            if (_hoverRow >= 0 && _hoverRow < entries.Count)
                DrawHoverLabel(g, entries[_hoverRow].DisplayName, RuneMagazinePainter.IconAt(_hoverRow, _scroll));
        }

        private void DrawResetButton(Graphics g)
        {
            var rect = RuneMagazinePainter.ResetButton;
            using var fill = new SolidBrush(_hoverReset ? Color.FromArgb(220, 70, 40, 40) : Color.FromArgb(200, 40, 44, 52));
            using var pen = new Pen(_hoverReset ? Color.FromArgb(240, 255, 130, 130) : Color.FromArgb(180, 110, 100, 70), 1f);
            using var font = new Font("Segoe UI", 11f, FontStyle.Bold);
            using var text = new SolidBrush(Color.FromArgb(235, 235, 230, 220));
            using var centre = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            g.FillRectangle(fill, rect);
            g.DrawRectangle(pen, rect);
            g.DrawString("⟲", font, text, rect, centre);

            if (_hoverReset)
                DrawHoverLabel(g, "Empty the magazine", rect);
        }

        /// <summary>
        /// Draws the name in the transparent gutter beside the strip. The gutter is chroma-keyed,
        /// so everywhere this is not drawn still passes clicks through to the game.
        /// </summary>
        private void DrawHoverLabel(Graphics g, string text, Rectangle anchor)
        {
            using var font = new Font("Segoe UI", 10f);
            var size = g.MeasureString(text, font);
            var width = (int)Math.Ceiling(size.Width) + 16;
            var height = (int)Math.Ceiling(size.Height) + 10;
            var top = Math.Clamp(anchor.Top + ((anchor.Height - height) / 2), 0, Math.Max(0, Height - height));
            var box = new Rectangle(RuneMagazinePainter.StripWidth + 6, top, Math.Min(width, RuneMagazinePainter.LabelWidth - 8), height);

            using var fill = new SolidBrush(Color.FromArgb(235, 18, 20, 26));
            using var pen = new Pen(Color.FromArgb(220, 120, 108, 60), 1f);
            using var brush = new SolidBrush(Color.FromArgb(240, 235, 232, 225));
            using var format = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };

            g.FillRectangle(fill, box);
            g.DrawRectangle(pen, box);
            g.DrawString(text, font, brush, new Rectangle(box.X + 8, box.Y, box.Width - 12, box.Height), format);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_stateSync)
                {
                    foreach (var image in _images.Values) image?.Dispose();
                    _images.Clear();
                }
            }
            base.Dispose(disposing);
        }
    }
}
