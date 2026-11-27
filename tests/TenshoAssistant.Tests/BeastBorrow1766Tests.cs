using System.Reflection;
using TenshoAssistant.Beastmaster;

// X-746 (0.1766, the user's idea I-029 of 18/11): Borrow (« Assimilation ») gives an action that depends on the familiar's family. The game
// links them in the Action sheet: the eight actions' condition is the familiar's Kinship (PrimaryCostType 125), its value the family
// (XBMPet column 1: 1 Beastkin « Thériens » … 8 Ashkin « Nécroïdes »); three copies of Cloud Skim come after it with another icon.
// docs/evidence/0.1766-beast-borrow.md.
internal static class BeastBorrow1766Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static Dictionary<string, IReadOnlyList<XbmRow>> Sheets()
        => (Dictionary<string, IReadOnlyList<XbmRow>>)typeof(BeastCatalogTests).GetMethod("Sheets", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, null)!;

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    // The rows read in the game files on 22/11 (game 2026.11.03): row, PrimaryCostType, PrimaryCostValue, French name, icon.
    private static readonly (uint Row, int CostType, int CostValue, string Name, uint Icon, string Description)[] Game =
    [
        (44895, 10, 4601, "Assimilation", 3914, "Emprunte temporairement certaines facultés de la bête…"),
        (44896, 125, 1, "Cuir de thérien", 3927, "Réduit de 25% les dégâts physiques subis et augmente de 5% vos chances de critiques.\nDurée : 30s\nCondition : Assimilation de thérien.\n\n* Cette action ne peut pas être ajoutée aux barres de raccourcis.\nRemplace Technique de bête lorsque les conditions sont réunies."),
        (44897, 125, 2, "Carapace d'insectoïde", 3926, "Vous immunise contre la plupart des projections et des attractions.\r\nDurée : 15s\r\n\r\n* Note."),
        (44898, 125, 3, "Rase-nuages", 3928, "Vous déplace rapidement dans la direction de votre choix."),
        (44899, 125, 4, "Plante-graines", 3929, "Poison."),
        (44900, 125, 5, "Vague répressive", 3930, "Eau."),
        (44901, 125, 6, "Peau d'écailles", 3931, "Barrière."),
        (44902, 125, 7, "Sape psychique", 3932, "Interruption."),
        (44903, 125, 8, "Cendres purifiantes", 3933, "Soin."),
        (45038, 125, 3, "Rase-nuages", 61273, ""), (45039, 125, 3, "Rase-nuages", 61273, ""), (45040, 125, 3, "Rase-nuages", 61273, ""), (45041, 125, 3, "Rase-nuages", 61273, ""),
        (9013, 0, 125, "Transe-Bahamut", 2714, ""),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast borrow 1766: behaviour: one action per family, the first row of each (not the copies of Cloud Skim), as the user's slide of the 93rd Letter LIVE", () =>
        {
            var borrows = BeastCatalog.BorrowsOf(Game, Enumerable.Range(1, 8));
            Check(borrows.Count == 8, "eight families: " + borrows.Count);
            var expected = new[] { "Cuir de thérien", "Carapace d'insectoïde", "Rase-nuages", "Plante-graines", "Vague répressive", "Peau d'écailles", "Sape psychique", "Cendres purifiantes" };
            var icons = new uint[] { 3927, 3926, 3928, 3929, 3930, 3931, 3932, 3933 };
            for (var family = 1; family <= 8; family++)
                Check(borrows[family].Name == expected[family - 1] && borrows[family].Action == 44895 + (uint)family && borrows[family].Icon == icons[family - 1] && borrows[family].Family == family,
                    $"family {family}: {borrows[family].Name} ({borrows[family].Action}, icon {borrows[family].Icon})");
            Check(borrows[3].Action == 44898 && borrows[3].Icon == 3928, "Cloud Skim: the first row, its own icon");
            Check(!borrows.Values.Any(borrow => borrow.Action == 44895 || borrow.Action == 9013), "Borrow itself and another job's condition 125 as a value: not taken");
        }),
        ("Beast borrow 1766: behaviour: only the families of the beasts; none without such rows; the effect up to the first empty line", () =>
        {
            var some = BeastCatalog.BorrowsOf(Game, [1, 2, 0]);
            Check(some.Keys.Order().SequenceEqual([1, 2]), "only the families asked: " + string.Join(",", some.Keys));
            Check(BeastCatalog.BorrowsOf(Game.Where(action => action.CostType != 125), Enumerable.Range(1, 8)).Count == 0, "no Kinship condition (a game update): none, never a guess");
            Check(some[1].Effect == "Réduit de 25% les dégâts physiques subis et augmente de 5% vos chances de critiques.\nDurée : 30s\nCondition : Assimilation de thérien.", "the effect, duration and condition: " + some[1].Effect);
            Check(some[2].Effect == "Vous immunise contre la plupart des projections et des attractions.\nDurée : 15s", "Windows line ends: " + some[2].Effect);
            Check(BeastCatalog.EffectOf("One line.") == "One line." && BeastCatalog.EffectOf("") == "" && BeastCatalog.EffectOf("\n\nAfter blank lines.\n\nNote.") == "After blank lines.", "no empty line: the whole; leading empty lines skipped");
        }),
        ("Beast borrow 1766: behaviour: the catalog gives each beast its family's number (XBMPet column 1) and its action; nothing without the reader", () =>
        {
            var catalog = BeastCatalog.Build(Sheets(), id => id == 47 ? "cu sith" : "squirrel", family => family == 1 ? "Therians" : "Insects", action => action == 45187 ? 2 : 5,
                id => "enemy " + id, id => "action " + id, id => ("Board one", 30, 25), borrowActions: Game, borrowLabel: "Assimilation (Nv 22)");
            var cuSith = catalog.Beasts.Single(beast => beast.Id == 1);
            var squirrel = catalog.Beasts.Single(beast => beast.Id == 2);
            Check(cuSith.FamilyId == 1 && squirrel.FamilyId == 2, "the families' numbers: " + cuSith.FamilyId + ", " + squirrel.FamilyId);
            Check(catalog.BorrowOf(cuSith)?.Name == "Cuir de thérien" && catalog.BorrowOf(squirrel)?.Name == "Carapace d'insectoïde", "each beast's action");
            Check(catalog.Borrows.Keys.Order().SequenceEqual([1, 2]) && catalog.BorrowLabel == "Assimilation (Nv 22)", "only the families of the beasts read; the game's label");
            var plain = BeastCatalog.Build(Sheets(), id => "x", family => "f", action => 2, id => "e", id => "a", id => ("b", 1, 1));
            Check(plain.Borrows.Count == 0 && plain.BorrowOf(plain.Beasts[0]) is null && plain.BorrowLabel.Length == 0, "without the reader: nothing shown");
        }),
        ("Beast borrow 1766: wiring: the loader reads the Kinship condition, ActionTransient and Addon 17749; the bestiary shows it after the family and under the filter; both languages; the evidence", () =>
        {
            var loader = Read("src/TenshoAssistant/Beastmaster/BeastCatalogLoader.Dalamud.cs");
            Check(loader.Contains("actions.Where(action => action.PrimaryCostType == BeastCatalog.KinshipCost)", StringComparison.Ordinal) && BeastCatalog.KinshipCost == 125, "the condition read");
            Check(loader.Contains("transient.TryGetRow(action.RowId, out var text) ? text.Description.ExtractText() : \"\"", StringComparison.Ordinal), "the effect: the game's description");
            Check(loader.Contains("private const uint BorrowLabelAddon = 17749;", StringComparison.Ordinal) && loader.Contains("borrowActions: borrowActions,", StringComparison.Ordinal), "the label, given to the catalog");
            var page = Read("src/TenshoAssistant/MainWindow.Beastmaster.cs");
            Check(page.Contains("ImGui.TableNextColumn(); ImGui.TextUnformatted(beast.Family);\n                DrawBeastBorrowIcon(catalog, beast);", StringComparison.Ordinal), "after each beast's family");
            Check(page.Contains("DrawBeastBorrowLine(catalog, borrow);", StringComparison.Ordinal) && page.Contains("$\"{family} · {borrowed.Name}##{family}\"", StringComparison.Ordinal), "under the filter and in its list");
            Check(page.Contains("if(TipHovered())TipText(localization.Format(\"BeastUi.BorrowTip\", BorrowLabel(catalog), borrow.Name, borrow.Effect));", StringComparison.Ordinal), "named on hover with its effect");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                Check(new[] { "Borrow", "BorrowLine", "BorrowTip" }.All(key => text.Contains($"    \"{key}\": ", StringComparison.Ordinal)), "the words, " + language);
            }
            var evidence = Read("docs/evidence/0.1766-beast-borrow.md");
            Check(evidence.Contains("PrimaryCostType", StringComparison.Ordinal) && evidence.Contains("Beastkin Familiar Effect: Grants Beast Kinship", StringComparison.Ordinal) && evidence.Contains("\n---\n", StringComparison.Ordinal), "the evidence, both halves");
        }),
    ];
}
