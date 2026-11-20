using System.Reflection;
using TenshoAssistant.Beastmaster;

// 0.1559 (X-486, the user's request of 16/11): an enemy's action says the effect it inflicts: XBMBattleDetailAction column 3 is a Status row
// (docs/evidence/0.1559-beast-action-effect.md: 4880 Brûlure du vent, 5140 Poison, 5421 Glas…), 0 for none.
internal static class BeastActionEffect1559Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    // The sheets of BeastCatalogTests (action 2: column 3 = 4880), read through reflection so that test stays as it is.
    private static Dictionary<string, IReadOnlyList<XbmRow>> Sheets()
        => (Dictionary<string, IReadOnlyList<XbmRow>>)typeof(BeastCatalogTests).GetMethod("Sheets", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, null)!;

    private static BeastCatalog Build(Func<uint, string>? status) => BeastCatalog.Build(Sheets(),
        id => id == 47 ? "cu sith" : "squirrel", family => family == 1 ? "Therians" : "Insects", action => action == 45187 ? 2 : 5,
        id => "enemy " + id, id => "action " + id, id => ("Board one", 30, 25), statusName: status);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast action effect 1559: column 3 gives the status's name; 0 and no reader give none", () =>
        {
            var asked = new List<uint>();
            var catalog = Build(id => { asked.Add(id); return id == 4880 ? "Brûlure du vent" : "?"; });
            var actions = catalog.Boards.SelectMany(board => board.Battles).SelectMany(battle => battle.Enemies).SelectMany(enemy => enemy.Actions).ToArray();
            Check(actions.Any(action => action.Name == "action 46870" && action.Effect == "Brûlure du vent"), "action 46870 inflicts 4880");
            Check(actions.Any(action => action.Name == "action 46866" && action.Effect == ""), "action 46866: column 3 is 0, no effect");
            Check(!asked.Contains(0u), "row 0 never asked");
            var without = Build(null).Boards.SelectMany(board => board.Battles).SelectMany(battle => battle.Enemies).SelectMany(enemy => enemy.Actions);
            Check(without.All(action => action.Effect == ""), "no reader: no effect (the tests and tools of before)");
        }),
        ("Beast action effect 1559: the panel and the Boards tab show it after the action, both languages; the loader reads the Status sheet", () =>
        {
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("localization.Format(\"BeastUi.ActionEffect\", action.Effect)") && page.Split("BeastActionText(action)").Length == 3, "one text, used by the fight shown and the Boards tab");
            var loader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Beastmaster/BeastCatalogLoader.Dalamud.cs"));
            Check(loader.Contains("statusName: id => data.GetExcelSheet<Lumina.Excel.Sheets.Status>()"), "the loader");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json")).Contains("\"ActionEffect\": \"inflige : {0}\""), "fr");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json")).Contains("\"ActionEffect\": \"inflicts: {0}\""), "en");
            Check(File.Exists(Path.GetFullPath("docs/evidence/0.1559-beast-action-effect.md")), "the evidence");
        }),
    ];
}
