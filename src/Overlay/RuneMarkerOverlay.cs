using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RuneshapePriceChecker.Configuration;
using RuneshapePriceChecker.Contracts;
using RuneshapePriceChecker.OCR;
using RuneshapePriceChecker.Runes;

namespace RuneshapePriceChecker.Overlay;

/// <summary>One frame to draw: a gilded cell in capture-region coordinates and how to mark it.</summary>
public sealed record RuneMarker(Rectangle Cell, RuneMarkerKind Kind, bool IsTopPick, bool IsUnbound);

public enum CarriedMarkerStyle
{
    /// <summary>Grey frame plus a diagonal slash (default).</summary>
    Slash,
    /// <summary>Red frame plus an X.</summary>
    Cross,
    /// <summary>Darken the icon only, no frame.</summary>
    Dim
}

/// <summary>
/// Pure drawing of rune markers, kept separate from the window so it can be exercised on an
/// offscreen bitmap in tests. Frames sit just outside the cell's drawn rectangle (gold frame
/// included) so they never cover the glyph; thickness scales with the cell so the same look
/// holds at every capture profile.
/// </summary>
public static class RuneMarkerPainter
{
    /// <summary>
    /// "Already in your magazine". Was a mid grey, which is the one colour a parchment panel full
    /// of brown ink and gold frames gives you no contrast against — it read as a shadow rather
    /// than a mark. Red carries the meaning on its own (don't take this one) and is the furthest
    /// hue from both the green and the orange used for runes still worth taking.
    /// </summary>
    public static readonly Color CarriedColor = Color.FromArgb(255, 72, 72);
    public static readonly Color CarriedCrossColor = Color.FromArgb(255, 72, 72);
    public static readonly Color ValuableColor = Color.FromArgb(88, 255, 122);
    public static readonly Color HighValueColor = Color.FromArgb(255, 160, 64);
    public static readonly Color BadgeGold = Color.FromArgb(255, 224, 102);
    public static readonly Color BadgeAmber = Color.FromArgb(245, 197, 66);
    private static readonly Color BadgeBack = Color.FromArgb(230, 27, 27, 27);

    public static Color ColorFor(RuneMarkerKind kind, CarriedMarkerStyle style) => kind switch
    {
        RuneMarkerKind.Carried => style == CarriedMarkerStyle.Cross ? CarriedCrossColor : CarriedColor,
        RuneMarkerKind.HighValue => HighValueColor,
        _ => ValuableColor
    };

    /// <summary>3 px at the 45 px cells of 2560x1440, never thinner than 2 px.</summary>
    public static int FrameThickness(Rectangle cell) => Math.Max(2, (int)Math.Round(3.0 * cell.Height / 45.0));

    public static CarriedMarkerStyle ParseStyle(string? text) => text?.Trim().ToLowerInvariant() switch
    {
        "cross" or "x" => CarriedMarkerStyle.Cross,
        "dim" => CarriedMarkerStyle.Dim,
        _ => CarriedMarkerStyle.Slash
    };

    public static IReadOnlyList<RuneMarker> FromSheet(RuneScoreSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        var markers = new List<RuneMarker>();
        foreach (var key in sheet.AllKeys)
        {
            var cell = key.Key.CellBounds;
            if (cell.Width <= 0 || cell.Height <= 0) continue;
            markers.Add(new RuneMarker(cell, key.Marker, key.IsTopPick, key.IsUnbound));
        }
        return markers;
    }

    public static void Paint(Graphics g, IReadOnlyList<RuneMarker> markers, CarriedMarkerStyle style)
    {
        ArgumentNullException.ThrowIfNull(g);
        ArgumentNullException.ThrowIfNull(markers);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        foreach (var marker in markers)
        {
            var cell = marker.Cell;
            var t = FrameThickness(cell);
            var color = ColorFor(marker.Kind, style);

            if (marker.Kind == RuneMarkerKind.Carried && style == CarriedMarkerStyle.Dim)
            {
                using var dim = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
                g.FillRectangle(dim, cell);
            }
            else
            {
                // The frame band occupies [cell.X - t, cell.X) on the left and [cell.Right, cell.Right + t)
                // on the right (same vertically). The pen is centred on the path, so the path runs
                // through the middle of that band: half a thickness outside the cell edge.
                var half = t / 2f;
                using var pen = new Pen(color, t) { LineJoin = LineJoin.Miter, Alignment = PenAlignment.Center };
                g.DrawRectangle(pen, cell.X - half, cell.Y - half, cell.Width + t, cell.Height + t);

                if (marker.Kind == RuneMarkerKind.Carried)
                {
                    using var strike = new Pen(color, Math.Max(2, t)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                    var inset = t;
                    g.DrawLine(strike, cell.Left + inset, cell.Bottom - inset, cell.Right - inset, cell.Top + inset);
                    if (style == CarriedMarkerStyle.Cross)
                        g.DrawLine(strike, cell.Left + inset, cell.Top + inset, cell.Right - inset, cell.Bottom - inset);
                }
            }

            var r = Math.Max(7, (int)Math.Round(cell.Height * 0.2));
            if (marker.IsUnbound)
                DrawBadge(g, new Point(cell.Left - (r / 2), cell.Top - (r / 2)), r, "?", BadgeAmber);
            if (marker.IsTopPick)
                DrawBadge(g, new Point(cell.Right - (r * 3 / 2), cell.Top - (r / 2)), r, "★", BadgeGold);
        }
    }

    private static void DrawBadge(Graphics g, Point topLeft, int radius, string glyph, Color color)
    {
        var d = radius * 2;
        var bounds = new Rectangle(topLeft.X, topLeft.Y, d, d);
        using var back = new SolidBrush(BadgeBack);
        using var ring = new Pen(color, Math.Max(1f, radius / 6f));
        g.FillEllipse(back, bounds);
        g.DrawEllipse(ring, bounds);

        using var font = new Font("Segoe UI Symbol", Math.Max(6f, radius * 1.25f), FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(color);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(glyph, font, brush, new RectangleF(bounds.X, bounds.Y - (radius * 0.08f), bounds.Width, bounds.Height), format);
    }
}

/// <summary>
/// Click-through, always-on-top layer covering the capture region that frames every gilded
/// rune in place: grey for carried, green for valuable, orange for more valuable, a star
/// badge on the top pick. Same window recipe as the debug bounds overlay and the price
/// overlay (layered + transparent + no-activate, chroma-keyed background, own STA thread).
/// </summary>
public sealed class RuneMarkerOverlay(
    IPoe2WindowResolutionProvider windowResolutionProvider,
    IOptionsMonitor<RunesOptions> runesOptions,
    IOptionsMonitor<OcrOptions> ocrOptions,
    IOptionsMonitor<AppOptions> appOptions,
    ILogger<RuneMarkerOverlay> logger) : IDisposable
{
    private readonly object _sync = new();
    private Thread? _overlayThread;
    private RuneMarkerForm? _overlayForm;
    private string _lastSignature = string.Empty;

    public void Render(LeagueWindowSnapshot snapshot, RuneScoreSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(sheet);

        try
        {
            var app = appOptions.CurrentValue;
            var runes = runesOptions.CurrentValue;
            if (!runes.MarkerOverlay || app.AllOverlaysDisabled || ocrOptions.CurrentValue.DebugOverlay)
            {
                Hide();
                return;
            }

            var captureRegion = windowResolutionProvider.CurrentCaptureRegion;
            if (captureRegion is null || !sheet.HasKeys || !snapshot.InterfaceDetected)
            {
                Hide();
                return;
            }

            var style = RuneMarkerPainter.ParseStyle(runes.CarriedMarkerStyle);
            var signature = $"{captureRegion.X},{captureRegion.Y},{captureRegion.Width},{captureRegion.Height}|{style}|{sheet.Signature()}";
            if (signature == _lastSignature)
                return;

            EnsureOverlayThreadStarted();
            var form = GetOverlayForm();
            if (form is null)
            {
                logger.LogDebug("RuneMarkerOverlay: form not available, skipping render");
                return;
            }

            _lastSignature = signature;
            var markers = RuneMarkerPainter.FromSheet(sheet);
            logger.LogDebug("RuneMarkerOverlay: drawing {Count} markers", markers.Count);
            form.SafeShow(captureRegion, markers, style);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to render rune markers: {Context}", ErrorContext.FromException(ex));
        }
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

        _overlayThread = OverlayFormRunner.Start<RuneMarkerForm>(
            "RuneMarkerOverlay",
            _sync,
            f => _overlayForm = f,
            logger);
    }

    private RuneMarkerForm? GetOverlayForm()
    {
        lock (_sync) return _overlayForm is { IsDisposed: false } f ? f : null;
    }

    public void Dispose()
    {
        GetOverlayForm()?.SafeClose();
    }

    private sealed class RuneMarkerForm : Form
    {
        private static readonly Color TransparencyChroma = Color.FromArgb(1, 2, 3);
        private readonly object _stateSync = new();
        private IReadOnlyList<RuneMarker> _markers = [];
        private CarriedMarkerStyle _style = CarriedMarkerStyle.Slash;
        private volatile bool _isHidden = true;

        public RuneMarkerForm()
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

        public void SafeShow(OcrCaptureRegion captureRegion, IReadOnlyList<RuneMarker> markers, CarriedMarkerStyle style)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                _isHidden = false;
                _ = BeginInvoke(new Action<OcrCaptureRegion, IReadOnlyList<RuneMarker>, CarriedMarkerStyle>(SafeShow), captureRegion, markers, style);
                return;
            }

            _isHidden = false;
            lock (_stateSync)
            {
                _markers = markers;
                _style = style;
            }

            // Badges poke a little past the cells, so give the window a small margin around the region.
            const int margin = 12;
            Bounds = new Rectangle(captureRegion.X - margin, captureRegion.Y - margin, captureRegion.Width + (2 * margin), Math.Max(1, captureRegion.Height + (2 * margin)));
            _offset = new Point(margin, margin);
            Invalidate();
            PinTopMost();
            if (!Visible)
            {
                Show();
                PinTopMost();
            }
        }

        private Point _offset;

        public void SafeHide()
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                if (_isHidden) return;
                _isHidden = true;
                _ = BeginInvoke(new Action(SafeHide));
                return;
            }

            lock (_stateSync) _markers = [];
            Hide();
            Bounds = new Rectangle(-32000, -32000, 1, 1);
        }

        public void SafeClose()
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                _ = BeginInvoke(new Action(SafeClose));
                return;
            }
            Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (IsDisposed) return;

            IReadOnlyList<RuneMarker> markers;
            CarriedMarkerStyle style;
            lock (_stateSync)
            {
                markers = _markers;
                style = _style;
            }

            e.Graphics.TranslateTransform(_offset.X, _offset.Y);
            RuneMarkerPainter.Paint(e.Graphics, markers, style);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            PinTopMost();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            PinTopMost();
        }

        private void PinTopMost()
        {
            if (!IsHandleCreated) return;
            _ = NativeMethods.SetWindowPos(Handle, NativeMethods.HWND_TOPMOST, Left, Top, Width, Height,
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOOWNERZORDER | NativeMethods.SWP_NOSENDCHANGING);
        }

        private static class NativeMethods
        {
            public static readonly IntPtr HWND_TOPMOST = new(-1);
            public const uint SWP_NOACTIVATE = 0x0010;
            public const uint SWP_NOOWNERZORDER = 0x0200;
            public const uint SWP_NOSENDCHANGING = 0x0400;

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
        }
    }
}
