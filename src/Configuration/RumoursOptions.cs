namespace RuneshapePriceChecker.Configuration;

/// <summary>
/// Settings for the Island Rumours checker (RUNE-27). Bound from the <c>"Rumours"</c> section of
/// <c>config/appsettings.json</c>.
/// </summary>
public sealed class RumoursOptions
{
    /// <summary>
    /// Global hotkey that reads the Uncharted Waters panel on screen and marks each rumour with
    /// its tier, e.g. <c>Alt+C</c>. Empty disables it. Pressed with the world map open, so it must
    /// not collide with a game binding.
    /// </summary>
    public string ReadHotkey { get; set; } = "Alt+C";

    /// <summary>Master switch for the rumour overlay.</summary>
    public bool Overlay { get; set; } = true;

    /// <summary>
    /// Mark rumours and charted maps without being asked, when the cursor comes to rest on one.
    /// The hotkey stays live either way, as the manual re-read.
    /// </summary>
    public bool AutoScan { get; set; } = true;

    /// <summary>How often the cursor is looked at. Costs nothing on its own — no capture, no OCR.</summary>
    public int PollMs { get; set; } = 100;

    /// <summary>
    /// How long the cursor must be still before the screen is read. Long enough that sweeping
    /// across the map reads nothing, short enough that resting on a node feels immediate.
    /// </summary>
    public int SettleMs { get; set; } = 200;

    /// <summary>
    /// How long between re-reads of the same resting place. Before anything is found these catch a
    /// panel that appears after the cursor stops — on a click, or just slowly. Once something is
    /// marked they become the check that it is still there.
    /// </summary>
    public int RescanMs { get; set; } = 500;

    /// <summary>
    /// How many times an empty resting place is read before it is given up on. A place already
    /// showing marks is exempt: those re-reads are how the marks come down when the panel closes.
    /// </summary>
    public int MaxReadsPerRest { get; set; } = 6;

    /// <summary>
    /// How long the marks stay up after a read, as a backstop. With auto-scan on, the marks
    /// normally come down the moment a re-read finds the panel gone or the cursor wanders off, so
    /// this only has to catch the case where neither happens — the map closed by a key, with the
    /// mouse left where it was.
    /// </summary>
    public int HoldSeconds { get; set; } = 6;
}
