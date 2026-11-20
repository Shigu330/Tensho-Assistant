using System.Reflection;
using TenshoAssistant.Beastmaster;

// 0.1562 (X-622, review of 0.1560, minor findings). M1: an enemy's action effect is « inflicted » only when its status harms (Status column 6 = 2,
// docs/evidence/0.1562-beast-effect-category.md); a helpful one (a bonus, a barrier, Couvert) is « granted », one not known an « effect ».
// M2: the 10 count a fight's strongest enemy that has a weakness, so a strongest enemy without one no longer lets its second strongest's
// weakness go for that of two weak enemies of the same fight.
internal static class BeastReview1562Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private const uint Fire = 1, Lightning = 4;

    private static Dictionary<string, IReadOnlyList<XbmRow>> Sheets()
        => (Dictionary<string, IReadOnlyList<XbmRow>>)typeof(BeastCatalogTests).GetMethod("Sheets", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, null)!;

    private static BoardEnemy Enemy(string name, uint weakness, int power)
        => new(name, 0, weakness == 0 ? "" : "w" + weakness, [], new BeastStats(power, power, power, power, power), weakness);
    private static uint AutoOf(uint id) => BeastPlan1543Tests.Held.First(row => row.Id == id).Auto;

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast review 1562 M1: the status's category goes with the action's effect; none without a reader", () =>
        {
            BeastCatalog Build(Func<uint, int>? category) => BeastCatalog.Build(Sheets(),
                id => id == 47 ? "cu sith" : "squirrel", family => family == 1 ? "Therians" : "Insects", action => action == 45187 ? 2 : 5,
                id => "enemy " + id, id => "action " + id, id => ("Board one", 30, 25), statusName: id => id == 4880 ? "Brûlure du vent" : "?", statusCategory: category);
            BoardAction[] Actions(BeastCatalog catalog) => catalog.Boards.SelectMany(board => board.Battles).SelectMany(battle => battle.Enemies).SelectMany(enemy => enemy.Actions).ToArray();
            var asked = new List<uint>();
            var harmful = Actions(Build(id => { asked.Add(id); return id == 4880 ? BoardAction.Harmful : 0; }));
            Check(harmful.Any(action => action.Name == "action 46870" && action.Effect == "Brûlure du vent" && action.EffectCategory == BoardAction.Harmful), "4880 harms");
            Check(harmful.Where(action => action.Effect.Length == 0).All(action => action.EffectCategory == 0) && !asked.Contains(0u), "no effect: no category, row 0 never asked");
            Check(Actions(Build(_ => BoardAction.Helpful)).First(action => action.Name == "action 46870").EffectCategory == BoardAction.Helpful, "a helpful status");
            Check(Actions(Build(null)).All(action => action.EffectCategory == 0), "no reader: not known");
        }),
        ("Beast review 1562 M1: « inflicts » only for a harmful status, « grants » for a helpful one, « effect » otherwise; the loader reads column 6", () =>
        {
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("action.EffectCategory == BoardAction.Harmful ? localization.Format(\"BeastUi.ActionEffect\", action.Effect)")
                && page.Contains("action.EffectCategory == BoardAction.Helpful ? \"BeastUi.ActionEffectGrants\" : \"BeastUi.ActionEffectOther\""), "the three texts");
            var loader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Beastmaster/BeastCatalogLoader.Dalamud.cs"));
            Check(loader.Contains("statusCategory: id => data.GetExcelSheet<Lumina.Excel.Sheets.Status>().GetRowOrDefault(id)?.StatusCategory ?? 0"), "the loader");
            foreach (var (language, grants, other) in new[] { ("en", "grants: {0}", "effect: {0}"), ("fr", "confère : {0}", "effet : {0}") })
            {
                var text = File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                Check(text.Contains($"\"ActionEffectGrants\": \"{grants}\"") && text.Contains($"\"ActionEffectOther\": \"{other}\""), language);
            }
            var evidence = File.ReadAllText(Path.GetFullPath("docs/evidence/0.1562-beast-effect-category.md"));
            Check(evidence.Contains("| 2413 | Couvert | 1 |") && evidence.Contains("| 5421 | Glas | 2 |"), "the evidence");
        }),
        ("Beast review 1562 M2: a strongest enemy without a weakness: its fight's strongest row is that of the strongest enemy that has one", () =>
        {
            var fight = new[] { Enemy("Boss", 0, 5), Enemy("Second", Lightning, 3), Enemy("Weak 1", Fire, 1), Enemy("Weak 2", Fire, 1) };
            var (rows, top, power) = BeastTeams.Ranked(fight);
            Check(top.SetEquals([Lightning]) && power == BeastTeams.Power(fight[1]), $"the second strongest's weakness is the fight's strongest row: {string.Join(",", top)} {power}");
            Check(rows[0].Row == Lightning && rows[1] is { Row: Fire, Count: 2 }, "Lightning before Fire, as Best (Weights) orders them");
            Check(BeastTeams.Ranked([Enemy("Alone", 0, 4)]) is { Rows.Count: 0, Top.Count: 0, Power: 0 }, "no weakness at all: nothing");
            Check(BeastTeams.PlanVersion >= 6, "plans kept on disk made again");
        }),
        ("Beast review 1562 M2: one place, the second strongest's weakness over two weak enemies of the same fight (the case of the review)", () =>
        {
            var board = new Board(9, "b", 1, 1, 1, 5, [new BoardBattle(true, [Enemy("Boss", 0, 5), Enemy("Second", Lightning, 3), Enemy("Weak 1", Fire, 1), Enemy("Weak 2", Fire, 1)])], []);
            var plan = BeastTeams.Plan(board, BeastPlan1543Tests.Held.Select(BeastPlan1543Tests.Make).ToArray(), BeastPlan1543Tests.Held.Select(row => row.Id).ToHashSet(), null);
            Check(plan.Team.Count == 1 && AutoOf(plan.Team[0].Beast) == Lightning, $"the lightning beast (the second strongest), not fire (two weak): {AutoOf(plan.Team[0].Beast)}");
        }),
    ];
}
