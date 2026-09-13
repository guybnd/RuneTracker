namespace RuneshapePriceChecker.Configuration;

/// <summary>
/// Settings for succession-rune scoring and the per-rune marker overlay (RUNE-2).
/// Bound from the <c>"Runes"</c> section of <c>config/appsettings.json</c>.
/// </summary>
public sealed class RunesOptions
{
    /// <summary>Global hotkey that empties the carried set, e.g. <c>Ctrl+Alt+R</c>. Empty disables it.</summary>
    public string ResetHotkey { get; set; } = "Ctrl+Alt+R";

    /// <summary>
    /// Global hotkey that adds the marked rune under the cursor to the magazine, or takes it back
    /// out. Empty disables it. Pressed in game with the Combinations panel open, so it must not
    /// collide with a game binding — Alt+V is unused by Path of Exile 2's defaults.
    /// </summary>
    public string MarkCarriedHotkey { get; set; } = "Alt+V";

    /// <summary>Draw the magazine column (runes taken this run) against the game window's left edge.</summary>
    public bool MagazineOverlay { get; set; } = true;

    /// <summary>Weight used for an unbound sprite whose glyph hue says nothing about its tier.</summary>
    public double UnknownRuneWeight { get; set; } = 1.0;

    /// <summary>Weight for an unbound sprite with a blue glyph (hue buckets 6–7).</summary>
    public double BlueTierWeight { get; set; } = 0.5;

    /// <summary>Weight for an unbound sprite with a purple glyph (hue buckets 8–9).</summary>
    public double PurpleTierWeight { get; set; } = 1.0;

    /// <summary>Unbound bindings persisted at most; past this, new unbound keys are dropped with one warning.</summary>
    public int MaxUnboundBindings { get; set; } = 64;

    /// <summary>Two keys are the same rune when their shape hashes differ in at most this many bits.</summary>
    public int MatchHammingThreshold { get; set; } = 8;

    /// <summary>A new rune at or above this weight is marked "more valuable" (orange) instead of "valuable" (green).</summary>
    public double HighValueWeight { get; set; } = 2.0;

    /// <summary>How an already-carried rune is marked: <c>slash</c> (grey frame + diagonal), <c>cross</c> (red frame + X) or <c>dim</c> (darken only).</summary>
    public string CarriedMarkerStyle { get; set; } = "slash";

    /// <summary>Master switch for the per-rune marker overlay.</summary>
    public bool MarkerOverlay { get; set; } = true;
}
