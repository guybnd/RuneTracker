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
/// Layout for the magazine strip, kept pure so the sizing can be tested without a window.
/// </summary>
public static class RuneMagazinePainter
{
    public const int IconSize = 40;
    public const int RowGap = 6;
    public const int PadX = 10;
    public const int PadY = 10;
    public const int HeaderHeight = 20;
    public const int LabelWidth = 150;

    public static int WidthFor(bool showNames) => PadX + IconSize + (showNames ? 8 + LabelWidth : 0) + PadX;

    public static int HeightFor(int count) => PadY + HeaderHeight + (count * (IconSize + RowGap)) - (count > 0 ? RowGap : 0) + PadY;

    /// <summary>Top-left of row <paramref name="index"/>'s icon, relative to the strip.</summary>
    public static Point IconOrigin(int index) => new(PadX, PadY + HeaderHeight + (index * (IconSize + RowGap)));

    /// <summary>
    /// How many rows fit in <paramref name="availableHeight"/>. The strip sits against the game's
    /// left edge, so it must never grow past the window and off the screen.
    /// </summary>
    public static int MaxRows(int availableHeight)
    {
        var usable = availableHeight - PadY - HeaderHeight - PadY;
        if (usable < IconSize) return 0;
        return Math.Max(0, ((usable + RowGap) / (IconSize + RowGap)));
    }
}

/// <summary>
/// Draws the player's magazine — the succession runes taken this run — as a column against the
/// left edge of the game window, so what has already been picked is visible without opening the
/// dashboard. Click-through and never activated, like the marker overlay.
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
            form.SafeShow(context, entries);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to render the rune magazine: {Context}", ErrorContext.FromException(ex));
        }
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
                cp.ExStyle |= 0x00080000; // WS_EX_LAYERED
                cp.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }

        public void SafeShow(WindowCaptureContext context, List<MagazineEntry> entries)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                _ = BeginInvoke(new Action<WindowCaptureContext, List<MagazineEntry>>(SafeShow), context, entries);
                return;
            }

            var fit = RuneMagazinePainter.MaxRows(context.ClientHeight);
            if (fit > 0 && entries.Count > fit)
                entries = entries.Take(fit).ToList();

            lock (_stateSync)
            {
                _entries = entries;
                foreach (var image in _images.Values) image?.Dispose();
                _images.Clear();
                foreach (var entry in entries)
                    _images[entry.Id] = Decode(entry.IconPng);
            }

            var width = RuneMagazinePainter.WidthFor(showNames: true);
            var height = RuneMagazinePainter.HeightFor(entries.Count);
            var top = context.ClientY + Math.Max(0, (context.ClientHeight - height) / 3);
            Bounds = new Rectangle(context.ClientX, top, width, Math.Max(1, height));

            Invalidate();
            PinTopMost();
            if (!Visible)
            {
                Show();
                PinTopMost();
            }
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

            using var panel = new SolidBrush(Color.FromArgb(190, 12, 14, 18));
            using var edge = new Pen(Color.FromArgb(200, 90, 80, 40), 1f);
            var body = new Rectangle(0, 0, Width - 1, Height - 1);
            g.FillRectangle(panel, body);
            g.DrawRectangle(edge, body);

            using var headerFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            using var nameFont = new Font("Segoe UI", 10f);
            using var headerBrush = new SolidBrush(Color.FromArgb(230, 255, 224, 102));
            using var nameBrush = new SolidBrush(Color.FromArgb(235, 225, 225, 225));

            g.DrawString($"Magazine · {entries.Count}", headerFont, headerBrush, RuneMagazinePainter.PadX, 5);

            for (var i = 0; i < entries.Count; i++)
            {
                var origin = RuneMagazinePainter.IconOrigin(i);
                var iconRect = new Rectangle(origin.X, origin.Y, RuneMagazinePainter.IconSize, RuneMagazinePainter.IconSize);

                Image? image;
                lock (_stateSync) _ = _images.TryGetValue(entries[i].Id, out image);
                if (image is not null)
                    g.DrawImage(image, iconRect);
                else
                    g.DrawRectangle(edge, iconRect);

                var textX = iconRect.Right + 8;
                var textRect = new Rectangle(textX, origin.Y, RuneMagazinePainter.LabelWidth, RuneMagazinePainter.IconSize);
                using var format = new StringFormat
                {
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                g.DrawString(entries[i].DisplayName, nameFont, nameBrush, textRect, format);
            }
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
