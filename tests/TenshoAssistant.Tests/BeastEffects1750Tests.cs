using System.Reflection;
using TenshoAssistant.Beastmaster;

// X-755 (0.1750, the user's reading R-X-553 of 20/11): the effects a beast inflicts, XBMPet columns 11 to 21, put in the game's order (the
// game's team window: columns 11 to 15, 17 to 21, then 16, Poison, last), with the game's icons 72461 to 72471 and names (Addon 12472 to
// 12481, then 17755); docs/evidence/0.1750-beast-effects.md.
internal static class BeastEffects1750Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static Dictionary<string, IReadOnlyList<XbmRow>> Sheets()
        => (Dictionary<string, IReadOnlyList<XbmRow>>)typeof(BeastCatalogTests).GetMethod("Sheets", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, null)!;

    // The eleven flags of a beast's XBMPet row, columns 11 to 21 as the sheet holds them ('1' set).
    private static Func<int, bool> Sheet(string flags) => column => flags[column - 11] == '1';

    // The same beast as the game's team window shows it: the eleven flags in the window's order ('1' set).
    private static string Window(IReadOnlyList<int> effects) => new(Enumerable.Range(0, 11).Select(at => effects.Contains(at) ? '1' : '.').ToArray());

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast effects 1750: behaviour: the game's order is columns 11 to 15, 17 to 21, then 16 (Poison last), with the game's names and icons", () =>
        {
            Check(BeastEffects.Order.Select(effect => effect.Column).SequenceEqual([11, 12, 13, 14, 15, 17, 18, 19, 20, 21, 16]), "the columns in the window's order");
            Check(BeastEffects.Order.Take(10).Select(effect => effect.Name).SequenceEqual(Enumerable.Range(12472, 10).Select(row => (uint)row)) && BeastEffects.Order[10].Name == 17755, "Addon 12472 to 12481, then 17755 « Poison »");
            Check(BeastEffects.Order.Select(effect => effect.Icon).SequenceEqual(Enumerable.Range(72461, 11).Select(icon => (uint)icon)), "icons 72461 to 72471");
        }),
        ("Beast effects 1750: behaviour: beasts of the sheet give the flags the game's team window showed (20/11)", () =>
        {
            foreach (var (name, sheet, window) in new[]
            {
                ("Mouton", ".......1...", "......1...."), ("Mandragore", ".....1...1.", "........1.1"), ("Opo-opo", "..1........", "..1........"),
                ("Coblyn", "...1.......", "...1......."), ("Dodo", "....1......", "....1......"), ("Chimère", ".1.........", ".1........."),
                ("Pampa", ".....1.....", "..........1"), ("Piège-volant", ".....1.....", "..........1"), ("Cu sith", "...........", "..........."),
                ("Apkallu", "1..........", "1.........."), ("row 39", "1.1.11.....", "1.1.1.....1"),
            })
                Check(Window(BeastEffects.Of(Sheet(sheet))) == window, $"{name}: sheet {sheet} gives {Window(BeastEffects.Of(Sheet(sheet)))}, the window showed {window}");
        }),
        ("Beast effects 1750: behaviour: the catalog reads the eleven columns of each beast and the names; none without them", () =>
        {
            var sheets = Sheets();
            var squirrel = sheets["XBMPet"][2].Cells;
            squirrel[16] = true; squirrel[20] = true;
            var asked = new List<uint>();
            var catalog = BeastCatalog.Build(sheets, id => id == 47 ? "cu sith" : "squirrel", family => family == 1 ? "Therians" : "Insects", action => action == 45187 ? 2 : 5,
                id => "enemy " + id, id => "action " + id, id => ("Board one", 30, 25), effectName: row => { asked.Add(row); return "name " + row; });
            Check(catalog.Beasts.Single(beast => beast.Id == 2).Effects.SequenceEqual([8, 10]), "Pesanteur then Poison: " + string.Join(",", catalog.Beasts.Single(beast => beast.Id == 2).Effects));
            Check(catalog.Beasts.Single(beast => beast.Id == 1).Effects.Count == 0, "a beast with no flag: none");
            Check(catalog.EffectNames.Count == 11 && catalog.EffectNames[0] == "name 12472" && catalog.EffectNames[10] == "name 17755" && asked.Count == 11, "the names in the game's order");
            var plain = BeastCatalog.Build(Sheets(), id => "x", family => "f", action => 2, id => "e", id => "a", id => ("b", 1, 1));
            Check(plain.EffectNames.Count == 0, "no names without the reader (the tests and tools of before)");
        }),
        ("Beast effects 1750: wiring: the loader reads the names in Addon; the bestiary, the team's rows and the beasts called show the icons, named on hover", () =>
        {
            var loader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Beastmaster/BeastCatalogLoader.Dalamud.cs"));
            Check(loader.Contains("effectName: row => addon.TryGetRow(row, out var text) ? text.Text.ExtractText() : \"\",", StringComparison.Ordinal), "the loader");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Split("DrawBeastEffects(catalog, beast);").Length == 4, "three places: the bestiary, the team's rows, the beasts called");
            Check(page.Contains("DrawGameIcon(BeastEffects.Order[at].Icon, side);", StringComparison.Ordinal) && page.Contains("TipText(catalog.EffectNames[at]);", StringComparison.Ordinal), "the game's icon, its name on hover");
            Check(File.Exists(Path.GetFullPath("docs/evidence/0.1750-beast-effects.md")), "the evidence");
        }),
    ];
}
