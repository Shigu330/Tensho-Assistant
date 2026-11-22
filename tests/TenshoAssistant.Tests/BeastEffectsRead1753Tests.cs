using System.Reflection;
using TenshoAssistant.Beastmaster;

// X-764 M3 (review of 0.1751, R08): XBMPet columns 11 to 21 were read by « is true » with no check of their type; the build only checked the
// column count. A game update that changed their type (or moved them at the same count) made every beast show « no effect » unsaid. Now the
// flags are read only when every beast's eleven columns hold flags; otherwise no beast shows an effect and the bestiary says they are
// unavailable.
internal static class BeastEffectsRead1753Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static Dictionary<string, IReadOnlyList<XbmRow>> Sheets()
        => (Dictionary<string, IReadOnlyList<XbmRow>>)typeof(BeastCatalogTests).GetMethod("Sheets", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, null)!;

    private static BeastCatalog Build(Dictionary<string, IReadOnlyList<XbmRow>> sheets) =>
        BeastCatalog.Build(sheets, id => "beast " + id, family => "family", action => 2, id => "enemy", id => "action", id => ("Board", 30, 25));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast effects read 1753: behaviour: flags in the eleven columns are read; a column that holds something else (a game update) makes every beast's effects unavailable, never « none »", () =>
        {
            var sheets = Sheets();
            sheets["XBMPet"][2].Cells[16] = true;
            var read = Build(sheets);
            Check(!read.EffectsUnavailable && read.Beasts.Single(beast => beast.Id == 2).Effects.SequenceEqual([10]), "Poison read: " + string.Join(",", read.Beasts.Single(beast => beast.Id == 2).Effects));
            var changed = Sheets();
            changed["XBMPet"][2].Cells[16] = true;
            changed["XBMPet"][1].Cells[12] = 1u;
            var moved = Build(changed);
            Check(moved.EffectsUnavailable, "a number where a flag was: unavailable");
            Check(moved.Beasts.All(beast => beast.Effects.Count == 0), "no beast shows an effect then, the one whose flags were fine included");
            Check(moved.Beasts.Count == read.Beasts.Count, "the rest of the catalog still read");
        }),
        ("Beast effects read 1753: wiring: the bestiary says the effects are unavailable, in English and French", () =>
        {
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("if (catalog.EffectsUnavailable) ImGui.TextDisabled(localization.Get(\"BeastUi.EffectsUnavailable\"));", StringComparison.Ordinal), "the line");
            foreach (var language in new[] { "en", "fr" })
                Check(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")).Contains("\"EffectsUnavailable\": \"", StringComparison.Ordinal), language);
        }),
    ];
}
