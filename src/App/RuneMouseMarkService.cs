using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RuneshapePriceChecker.Configuration;
using RuneshapePriceChecker.OCR;
using RuneshapePriceChecker.Overlay;
using RuneshapePriceChecker.Runes;

namespace RuneshapePriceChecker.App;

/// <summary>
/// Marks the rune under the cursor with a mouse button — right-click by default — using a
/// low-level mouse hook.
///
/// <see cref="GlobalHotkeyService"/> cannot do this: <c>RegisterHotKey</c> takes virtual keys,
/// not mouse buttons. A hook is the only way to see a click that is going to the game.
///
/// Two rules keep it from interfering with play:
/// <list type="bullet">
/// <item>It acts only when the click is inside a rune the overlay is currently marking. Anywhere
/// else the click is passed straight through untouched, so right-click still does whatever it
/// does in game everywhere except on top of a marked rune.</item>
/// <item>It swallows only the clicks it acts on, so the game does not also receive them.</item>
/// </list>
///
/// The hook callback must return fast — Windows drops a hook that is slow — so it does no more
/// than a rectangle test against the last drawn sheet, and it never blocks.
/// </summary>
public sealed class RuneMouseMarkService(
    IOptionsMonitor<RunesOptions> options,
    IPoe2WindowResolutionProvider windowResolution,
    RuneMagazine magazine,
    RuneToastOverlay toast,
    ILogger<RuneMouseMarkService> logger) : IHostedService, IDisposable
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;

    private Thread? _thread;
    private IntPtr _hook;
    private uint _threadId;
    private LowLevelMouseProc? _proc; // held so the delegate is not collected while installed

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _thread = new Thread(RunMessageLoop) { IsBackground = true, Name = "RuneMouseMark" };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
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
            _threadId = GetCurrentThreadId();
            _proc = HookCallback;
            _hook = SetWindowsHookEx(WH_MOUSE_LL, _proc, IntPtr.Zero, 0);
            if (_hook == IntPtr.Zero)
            {
                logger.LogWarning("Mouse-mark hook could not be installed (Win32 {Error}); use the keyboard hotkey instead", Marshal.GetLastWin32Error());
                return;
            }

            logger.LogInformation("Mouse-mark hook installed; {Button} marks the rune under the cursor", options.CurrentValue.MarkCarriedMouseButton);
            Application.Run();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Mouse-mark hook thread failed: {Context}", ErrorContext.FromException(ex));
        }
        finally
        {
            if (_hook != IntPtr.Zero) _ = UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0) return CallNextHookEx(_hook, nCode, wParam, lParam);

        try
        {
            if (ShouldMark((int)wParam))
            {
                var result = magazine.ToggleAtCursor(out var runeName);
                if (result is RuneMarkResult.Marked or RuneMarkResult.Unmarked)
                {
                    toast.Show(RuneToast.For(result, runeName)); // posts to the toast thread; does not block the hook
                    return 1; // handled here — do not pass this click to the game
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Mouse-mark hook callback failed");
        }

        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    /// <summary>
    /// Whether this message is the configured marking click, and the game is actually in front.
    /// The foreground check keeps a right-click in another application from being swallowed.
    /// </summary>
    private bool ShouldMark(int message)
    {
        var configured = options.CurrentValue.MarkCarriedMouseButton;
        if (string.IsNullOrWhiteSpace(configured) || configured.Equals("none", StringComparison.OrdinalIgnoreCase))
            return false;

        var wanted = configured.Trim().ToLowerInvariant() switch
        {
            "right" => WM_RBUTTONDOWN,
            "middle" => WM_MBUTTONDOWN,
            _ => 0
        };
        return wanted != 0 && message == wanted && windowResolution.IsPoe2WindowForeground;
    }

    public void Dispose()
    {
        if (_threadId != 0) _ = PostThreadMessage(_threadId, 0x0012 /* WM_QUIT */, IntPtr.Zero, IntPtr.Zero);
        _threadId = 0;
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostThreadMessage(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);
}
