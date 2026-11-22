using TenshoAssistant.Beastmaster;

// X-770 M2 (review of 0.1768, 22/11): with a family chosen in the « Ephemeral items » tab, the feeds whose families were not read in their
// description (a client whose bracket is not recognised: German, Japanese) left the list without a word, and the family showed « Feed (0) »
// as if it could eat nothing: unknown taken for no (R06). Now they stay in a family's list, marked, and the page says how many.
internal static class BeastItems1769Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static readonly Dictionary<int, string> French = new()
    { [1] = "Thériens", [2] = "Insectoïdes", [3] = "Ptériens", [4] = "Floréens", [5] = "Hydrides", [6] = "Cuirassiens", [7] = "Animides", [8] = "Nécroïdes" };

    private static XbmRow Row(uint row, int kind, string name, string description)
    {
        var cells = new object?[BeastItems.Columns];
        cells[0] = (byte)kind; cells[1] = 243000u + row; cells[2] = (ushort)0; cells[3] = name.ToLowerInvariant(); cells[11] = name; cells[12] = description; cells[13] = "";
        return new XbmRow(row, 0, cells);
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast items 1769: behaviour: the review's case: feeds whose families were not read stay in a family's list; those of other families leave it", () =>
        {
            var items = new[]
            {
                Row(146, 3, "Simili-viande", "Dégâts physiques infligés +20% [Utilisable sur: Thériens, Insectoïdes]"),
                Row(149, 3, "Simili-boulette de poisson", "Chances de critiques +35% [Utilisable sur: Animides]"),
                // A bracket the reading does not recognise (as in a German or Japanese client): its families unknown.
                Row(150, 3, "Kunstfleisch", "Verursachter physischer Schaden +20% [Geeignet für: Tierwesen, Ungeziefer]"),
                Row(151, 3, "Sans crochet", "Restaure 10% des PV."),
                Row(1, 1, "Ceinture de force", "PV maximums +20%"),
            }.Select(row => BeastItems.Read(row, French)).OfType<BeastItem>().ToArray();
            Check(items.Length == 5 && items.Count(BeastItems.FamiliesUnread) == 2 && !BeastItems.FamiliesUnread(items[4]), "two feeds with their families unread; the gear is not a feed");
            var beastkin = BeastItems.Of(items, BeastItems.Feed, 1, "").Select(item => item.Row).ToArray();
            Check(beastkin.SequenceEqual([146u, 150u, 151u]), "beastkin: its feed and the two unread, in the sheet's order: " + string.Join(",", beastkin));
            var soulkin = BeastItems.Of(items, BeastItems.Feed, 7, "").Select(item => item.Row).ToArray();
            Check(soulkin.SequenceEqual([149u, 150u, 151u]), "soulkin: its feed and the two unread, never the beastkin's: " + string.Join(",", soulkin));
            Check(!BeastItems.UsableBy(items[2], 1) && BeastItems.FamiliesUnread(items[2]), "unread: not said usable, said unknown");
            Check(BeastItems.Of(items, BeastItems.Feed, 1, "Kunst").Count == 1 && BeastItems.Of(items, BeastItems.Feed, 0, "").Count == 4, "the search still filters them; every family: all four");
        }),
        ("Beast items 1769: behaviour: a client whose brackets are never read: a family's list is never empty for that reason", () =>
        {
            var items = Enumerable.Range(140, 60).Select(row => BeastItems.Read(Row((uint)row, 3, "Futter " + row, "Effekt [Geeignet für: Tierwesen]"), French)).OfType<BeastItem>().ToArray();
            Check(Enumerable.Range(1, 8).All(family => BeastItems.Of(items, BeastItems.Feed, family, "").Count == 60), "each family: the 60 feeds kept (unknown), not « Feed (0) »");
        }),
        ("Beast items 1769: wiring: the page says how many unread feeds a family's list keeps; both languages", () =>
        {
            var page = Read("src/TenshoAssistant/MainWindow.Beastmaster.cs");
            Check(page.Contains("if (kind == BeastItems.Feed && beastItemFamily > 0 && shown.Count(BeastItems.FamiliesUnread) is var unread and > 0)", StringComparison.Ordinal)
                && page.Contains("localization.Format(\"BeastUi.ItemFamiliesUnreadKept\", unread)", StringComparison.Ordinal), "the line under the feeds' rule");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                Check(text.Contains("    \"ItemFamiliesUnreadKept\": \"{0} ", StringComparison.Ordinal), "the line, " + language);
            }
            Check(Read("src/TenshoAssistant/Localization/Languages/fr.json").Contains("Les aliments dont les familles n'ont pas pu être lues restent dans la liste, marqués.", StringComparison.Ordinal), "the filter's tip says it");
        }),
    ];
}
