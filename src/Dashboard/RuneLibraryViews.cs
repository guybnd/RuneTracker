using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RuneshapePriceChecker.App.Dashboard;

/// <summary>
/// One rune in the dashboard's Rune Library. Defined in the Dashboard assembly because it is
/// referenced <i>by</i> the app and cannot see the app's contracts; the app maps its catalog
/// into these views and pushes them over.
/// </summary>
public sealed class RuneLibraryEntryView : INotifyPropertyChanged
{
    private double _weight;
    private bool _isCarried;
    private int _seenCount;
    private byte[]? _spritePng;
    private ImageSource? _sprite;
    private ImageSource? _referenceIcon;

    public string Id { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string Effect { get; init; } = "";
    public bool Rare { get; init; }
    /// <summary><c>gold</c>, <c>purple</c>, <c>blue</c> or empty when unknown.</summary>
    public string Tier { get; init; } = "";
    public byte[]? ReferenceIconPng { get; init; }
    /// <summary>Binding id of the sprite bound to this rune, or null when none has been seen yet.</summary>
    public string? BindingId { get; init; }

    /// <summary>True once a seen sprite has been bound to this rune — the setup-progress unit.</summary>
    public bool IsBound => !string.IsNullOrEmpty(BindingId);

    public string TierLabel => string.IsNullOrEmpty(Tier) ? "tier unknown" : Tier;
    public string RareLabel => Rare ? "rare" : "";

    public Brush TierBrush => Tier switch
    {
        "gold" => new SolidColorBrush(Color.FromRgb(0xF0, 0xC0, 0x40)),
        "purple" => new SolidColorBrush(Color.FromRgb(0xB0, 0x6C, 0xFF)),
        "blue" => new SolidColorBrush(Color.FromRgb(0x5A, 0xB0, 0xFF)),
        _ => new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66))
    };

    public double Weight
    {
        get => _weight;
        set
        {
            if (Math.Abs(_weight - value) <= 1e-9) return;
            _weight = value;
            OnChanged();
            OnChanged(nameof(WeightText));
            OnChanged(nameof(PriorityLabel));
            OnChanged(nameof(PriorityChoices));
        }
    }

    /// <summary>Named level for the current weight — what the picker shows as selected.</summary>
    public string PriorityLabel => RunePriorities.LabelForWeight(_weight);

    /// <summary>Levels offered for this rune, including its own hand-typed weight when it has one.</summary>
    public IReadOnlyList<string> PriorityChoices => RunePriorities.ChoicesFor(_weight);

    public string WeightText
    {
        get => _weight.ToString("0.##", CultureInfo.InvariantCulture);
        set { if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var w)) Weight = Math.Clamp(w, 0, 100); }
    }

    public bool IsCarried
    {
        get => _isCarried;
        set { if (_isCarried != value) { _isCarried = value; OnChanged(); } }
    }

    public int SeenCount
    {
        get => _seenCount;
        set { if (_seenCount != value) { _seenCount = value; OnChanged(); OnChanged(nameof(SeenText)); OnChanged(nameof(SubtitleText)); } }
    }

    public string SeenText => _seenCount > 0 ? $"seen {_seenCount}×" : "not seen yet";

    /// <summary>Tier, rarity and sighting count on one line, so a row fits the narrowest window.</summary>
    public string SubtitleText
    {
        get
        {
            var tier = string.IsNullOrEmpty(Tier) ? "tier unknown" : Tier;
            var rare = Rare ? " · rare" : "";
            return $"{tier}{rare} · {SeenText}";
        }
    }

    public byte[]? SpritePng
    {
        get => _spritePng;
        set { _spritePng = value; _sprite = null; OnChanged(); OnChanged(nameof(Sprite)); OnChanged(nameof(HasSprite)); }
    }

    public bool HasSprite => _spritePng is { Length: > 0 };

    /// <summary>Created lazily on the UI thread from the PNG bytes; frozen so it can be shared.</summary>
    public ImageSource? Sprite => _sprite ??= RuneImageFactory.FromPng(_spritePng);

    public ImageSource? ReferenceIcon => _referenceIcon ??= RuneImageFactory.FromPng(ReferenceIconPng);

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>A gilded sprite the detector has stored but the user has not yet bound to a rune.</summary>
public sealed class UnboundSpriteView : INotifyPropertyChanged
{
    private string? _selectedRuneId;
    private ImageSource? _sprite;
    private bool _isCarried;

    public string BindingId { get; init; } = "";
    public byte[]? SpritePng { get; init; }
    public int SeenCount { get; init; }
    /// <summary>Glyph colour guess from the hue bucket: <c>blue</c>, <c>purple</c> or empty.</summary>
    public string ColourHint { get; init; } = "";
    public double CurrentWeight { get; init; }
    public IReadOnlyList<RuneChoice> Choices { get; init; } = [];

    public string Summary =>
        $"seen {SeenCount}× · {(string.IsNullOrEmpty(ColourHint) ? "dark glyph" : ColourHint + " glyph")} · scoring {CurrentWeight.ToString("0.##", CultureInfo.InvariantCulture)} until bound";

    public ImageSource? Sprite => _sprite ??= RuneImageFactory.FromPng(SpritePng);

    /// <summary>
    /// Carried state for a sprite that has not been named yet. The mark-carried hotkey can put one
    /// in the magazine before the user knows which rune it is, and without this the only row that
    /// could show or clear it would be a named rune's — so it would be stuck there invisibly.
    /// </summary>
    public bool IsCarried
    {
        get => _isCarried;
        set { if (_isCarried != value) { _isCarried = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCarried))); } }
    }

    public string? SelectedRuneId
    {
        get => _selectedRuneId;
        set { if (_selectedRuneId != value) { _selectedRuneId = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedRuneId))); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// One option in the "which rune is this?" picker. Carries the rune's reference glyph so the
/// dropdown can be matched against the seen sprite by eye — the names alone are hard to tell
/// apart when you are looking at an unlabelled glyph.
/// </summary>
public sealed record RuneChoice(string Id, string DisplayName, byte[]? IconPng = null)
{
    private ImageSource? _icon;

    public ImageSource? Icon => _icon ??= RuneImageFactory.FromPng(IconPng);

    public override string ToString() => DisplayName;
}

/// <summary>Edits flowing back from the library UI to the app's catalog.</summary>
public sealed class RuneLibraryCallbacks
{
    public Action<string, double>? SetWeight { get; init; }
    public Action<string, string?>? Bind { get; init; }
    public Action<string, bool>? SetCarried { get; init; }
    public Action? ResetCarried { get; init; }
    public Action<string>? Forget { get; init; }

    /// <summary>Drops every sprite the user has not bound to a rune. Bound sprites are kept.</summary>
    public Action? ForgetAllUnbound { get; init; }
}

public static class RuneImageFactory
{
    public static ImageSource? FromPng(byte[]? png)
    {
        if (png is null || png.Length == 0) return null;
        try
        {
            var image = new BitmapImage();
            using var ms = new MemoryStream(png);
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = ms;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }
}
