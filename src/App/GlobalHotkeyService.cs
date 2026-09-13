using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RuneshapePriceChecker.Configuration;
using RuneshapePriceChecker.Runes;

namespace RuneshapePriceChecker.App;

/// <summary>
/// Registers the "reset carried runes" global hotkey (<see cref="RunesOptions.ResetHotkey"/>)
/// on a message-only window living on its own STA thread. No other hotkey machinery exists in
/// the app: the overlay forms are <c>WS_EX_NOACTIVATE</c> and never see keyboard input, and the
/// dashboard's WPF window is not guaranteed to exist. Re-registers when the setting changes; a
/// failed registration logs a warning and leaves the dashboard's reset button as the fallback.
/// </summary>
public sealed class GlobalHotkeyService(
    IOptionsMonitor<RunesOptions> options,
    RuneCatalog catalog,
    ILogger<GlobalHotkeyService> logger) : IHostedService, IDisposable
{
    private const int HotkeyId = 0x5255; // 'RU'
    private Thread? _thread;
    private HotkeyWindow? _window;
    private ApplicationContext? _context;
    private IDisposable? _optionsSubscription;
    private string _lastRequestedHotkey = "";
    private readonly ManualResetEventSlim _ready = new();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _thread = new Thread(RunMessageLoop) { IsBackground = true, Name = "GlobalHotkey" };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _ = _ready.Wait(TimeSpan.FromSeconds(5), cancellationToken);

        // IOptionsMonitor fires several times per settings write, so only act on a real change.
        _optionsSubscription = options.OnChange(o =>
        {
            var hotkey = o.ResetHotkey ?? "";
            if (Interlocked.Exchange(ref _lastRequestedHotkey, hotkey) == hotkey) return;
            _window?.RequestReregister();
        });
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    private void RunMessageLoop()
    {
        try
        {
            _lastRequestedHotkey = options.CurrentValue.ResetHotkey ?? "";
            _window = new HotkeyWindow(() => options.CurrentValue.ResetHotkey, OnHotkey, logger);
            _window.Register();
            _ready.Set();
            _context = new ApplicationContext();
            Application.Run(_context);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Global hotkey thread failed: {Context}", ErrorContext.FromException(ex));
            _ready.Set();
        }
        finally
        {
            _window?.DestroyHandle();
            _window = null;
        }
    }

    private void OnHotkey()
    {
        logger.LogInformation("Reset hotkey pressed — clearing carried succession runes");
        catalog.ResetCarried();
    }

    public void Dispose()
    {
        _optionsSubscription?.Dispose();
        _optionsSubscription = null;
        try { _context?.ExitThread(); } catch { }
        _ready.Dispose();
    }

    /// <summary>Parses <c>Ctrl+Alt+R</c>-style text into RegisterHotKey arguments. Empty/whitespace means disabled.</summary>
    public static bool TryParse(string? text, out uint modifiers, out uint virtualKey)
    {
        modifiers = 0;
        virtualKey = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        foreach (var raw in text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (raw.ToLowerInvariant())
            {
                case "ctrl" or "control": modifiers |= 0x0002; continue;
                case "alt": modifiers |= 0x0001; continue;
                case "shift": modifiers |= 0x0004; continue;
                case "win" or "windows": modifiers |= 0x0008; continue;
            }

            if (virtualKey != 0) return false; // two non-modifier keys
            if (raw.Length == 1 && char.IsLetterOrDigit(raw[0]))
            {
                virtualKey = char.ToUpperInvariant(raw[0]);
                continue;
            }
            if (Enum.TryParse<Keys>(raw, ignoreCase: true, out var key) && key != Keys.None)
            {
                virtualKey = (uint)key;
                continue;
            }
            return false;
        }

        return virtualKey != 0;
    }

    private sealed class HotkeyWindow : NativeWindow
    {
        private const int WM_HOTKEY = 0x0312;
        private const int WM_APP_REREGISTER = 0x8000 + 0x52;
        private static readonly IntPtr HWND_MESSAGE = new(-3);
        private readonly Func<string?> _hotkeyText;
        private readonly Action _onHotkey;
        private readonly ILogger _logger;
        private bool _registered;
        private string? _warnedFor;

        public HotkeyWindow(Func<string?> hotkeyText, Action onHotkey, ILogger logger)
        {
            _hotkeyText = hotkeyText;
            _onHotkey = onHotkey;
            _logger = logger;
            CreateHandle(new CreateParams { Parent = HWND_MESSAGE });
        }

        public void Register()
        {
            if (_registered)
            {
                _ = UnregisterHotKey(Handle, HotkeyId);
                _registered = false;
            }

            var text = _hotkeyText();
            if (!TryParse(text, out var modifiers, out var vk))
            {
                if (!string.IsNullOrWhiteSpace(text))
                    _logger.LogWarning("Reset hotkey '{Hotkey}' is not a valid key combination; hotkey disabled", text);
                return;
            }

            const uint MOD_NOREPEAT = 0x4000;
            if (RegisterHotKey(Handle, HotkeyId, modifiers | MOD_NOREPEAT, vk))
            {
                _registered = true;
                _warnedFor = null;
                _logger.LogInformation("Reset hotkey registered: {Hotkey}", text);
            }
            else if (_warnedFor != text)
            {
                // Another app already owning the combination is a normal outcome (Win32 1409), and
                // the dashboard button still works — say so once per hotkey, not on every retry.
                _warnedFor = text;
                var error = Marshal.GetLastWin32Error();
                var reason = error == 1409 ? "another application already uses it" : $"Win32 error {error}";
                _logger.LogWarning("Reset hotkey '{Hotkey}' is unavailable ({Reason}); pick another in Settings or use the Reset carried runes button", text, reason);
            }
        }

        public void RequestReregister()
        {
            if (Handle != IntPtr.Zero)
                _ = PostMessage(Handle, WM_APP_REREGISTER, IntPtr.Zero, IntPtr.Zero);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HotkeyId)
            {
                try { _onHotkey(); }
                catch (Exception ex) { _logger.LogWarning(ex, "Reset hotkey handler failed"); }
                return;
            }
            if (m.Msg == WM_APP_REREGISTER)
            {
                Register();
                return;
            }
            base.WndProc(ref m);
        }

        public override void DestroyHandle()
        {
            if (_registered && Handle != IntPtr.Zero)
            {
                _ = UnregisterHotKey(Handle, HotkeyId);
                _registered = false;
            }
            base.DestroyHandle();
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}
