using System.Diagnostics;
using System.Drawing;
using RuneshapePriceChecker.OCR;
using RuneshapePriceChecker.Overlay;
using RuneshapePriceChecker.Rumours;

// Reads Uncharted Waters panels out of screenshots and prints what the tool would say about them
// (RUNE-27). Point it at a PNG or a folder of them:
//
//   dotnet run --project tests/RumourSimulator -- "E:\Git\RuneshapeCaptures\incoming"
//
// With no arguments it reads the fixtures the tests use. The whole path runs here — Windows OCR,
// the panel search, the tier table — so what it prints is what the overlay will draw.

// --render <dir> also writes each capture with the marks drawn on it, which is the only way to
// see what the overlay looks like without the game running.
string? renderDir = null;
var rest = new List<string>();
for (var a = 0; a < args.Length; a++)
{
    if (string.Equals(args[a], "--render", StringComparison.OrdinalIgnoreCase) && a + 1 < args.Length) renderDir = args[++a];
    else rest.Add(args[a]);
}
if (renderDir is not null) _ = Directory.CreateDirectory(renderDir);

var paths = rest.Count > 0 ? rest.ToArray() : [Path.Combine(RepoRoot(), "tests", "fixtures", "rumours")];

var files = new List<string>();
foreach (var path in paths)
{
    if (Directory.Exists(path)) files.AddRange(Directory.GetFiles(path, "*.png", SearchOption.AllDirectories));
    else if (File.Exists(path)) files.Add(path);
    else Console.Error.WriteLine($"not found: {path}");
}

if (files.Count == 0)
{
    Console.Error.WriteLine("No .png files to read.");
    return 1;
}

var table = new RumourTable();
var engine = new WindowsOcrEngine("eng");
var found = 0;
var unrecognised = new List<string>();

foreach (var file in files.Order())
{
    using var bitmap = new Bitmap(file);
    var sw = Stopwatch.StartNew();
    var lines = engine.RecognizeLines(bitmap);
    var panel = RumourPanelReader.Read(lines, table);
    var map = panel is null ? RumourMapTooltipReader.Read(lines, table) : null;
    sw.Stop();

    Console.WriteLine();
    Console.WriteLine($"{Path.GetFileName(file)}  ({bitmap.Width}x{bitmap.Height}, {sw.ElapsedMilliseconds} ms)");

    var badges = panel is not null ? RumourBadgePainter.FromPanel(panel)
               : map is not null ? RumourBadgePainter.FromMap(map)
               : [];
    if (renderDir is not null && badges.Count > 0) Render(file, bitmap, badges, renderDir);

    if (map is not null)
    {
        found++;
        if (map.Rumour is { } charted)
        {
            Write(ColourFor(charted.Rating), $"  [{charted.Rating,-2}] {charted.Map,-20} {charted.Name,-20} {charted.Mods}   (charted map)");
            Console.WriteLine($"        read as \"{map.Title}\" (d={map.Distance}) at {map.Bounds.X},{map.Bounds.Y} {map.Bounds.Width}x{map.Bounds.Height}");
        }
        else
        {
            Write(ConsoleColor.Red, $"  [ ? ] map tooltip not in the table — OCR read \"{map.Title}\"");
            unrecognised.Add(map.Title);
        }
        continue;
    }

    if (panel is null)
    {
        Console.WriteLine("  nothing to mark in this capture");
        continue;
    }

    found++;
    var best = panel.BestIndex;
    for (var i = 0; i < panel.Rumours.Count; i++)
    {
        var reading = panel.Rumours[i];
        if (reading.Rumour is not { } rumour)
        {
            Write(ConsoleColor.Red, $"  [ ? ] unrecognised — OCR read \"{reading.Text}\"");
            unrecognised.Add(reading.Text);
            continue;
        }

        var star = i == best ? "*" : " ";
        var line = $" {star}[{rumour.Rating,-2}] {rumour.Name,-20} {rumour.Map,-20} {rumour.Mods}";
        Write(ColourFor(rumour.Rating), line);
        Console.WriteLine($"        read as \"{reading.Text}\" (d={reading.Distance}) at {reading.Bounds.X},{reading.Bounds.Y} {reading.Bounds.Width}x{reading.Bounds.Height}");
    }
}

Console.WriteLine();
Console.WriteLine($"{found}/{files.Count} captures held something to mark.");

if (unrecognised.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Unrecognised lines — these are rumours whose printed wording the table does not know yet.");
    Console.WriteLine("Send them along with the screenshot and they can be added as aliases:");
    foreach (var text in unrecognised) Console.WriteLine($"  \"{text}\"");
}

return 0;

static void Render(string file, Bitmap source, IReadOnlyList<RumourBadge> badges, string directory)
{
    using var canvas = new Bitmap(source);
    using var g = Graphics.FromImage(canvas);
    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
    RumourBadgePainter.Paint(g, badges, canvas.Size);
    var target = Path.Combine(directory, Path.GetFileNameWithoutExtension(file) + "-marked.png");
    canvas.Save(target, System.Drawing.Imaging.ImageFormat.Png);
    Console.WriteLine($"        rendered {target}");
}

static void Write(ConsoleColor colour, string text)
{
    var previous = Console.ForegroundColor;
    Console.ForegroundColor = colour;
    Console.WriteLine(text);
    Console.ForegroundColor = previous;
}

static ConsoleColor ColourFor(string rating)
{
    return rating switch
    {
        "S+" or "S" => ConsoleColor.Magenta,
        "A+" or "A" => ConsoleColor.Green,
        "B+" or "B" => ConsoleColor.Yellow,
        "C+" or "C" => ConsoleColor.DarkYellow,
        _ => ConsoleColor.DarkGray
    };
}

static string RepoRoot()
{
    var directory = AppContext.BaseDirectory;
    while (directory is not null && !File.Exists(Path.Combine(directory, "RuneshapePriceChecker.slnx")))
        directory = Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar));
    return directory ?? Directory.GetCurrentDirectory();
}
