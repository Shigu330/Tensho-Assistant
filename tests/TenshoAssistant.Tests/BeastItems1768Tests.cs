using TenshoAssistant.Beastmaster;

// X-744 (0.1768, the user's idea I-026, first part): the Crucible's ephemeral items (XBMItem: 1 beast gear, 2 Crucible item, 3 feed), the
// families that can eat each feed (named only in the bracket that ends its description, in the client's language; no column holds them),
// and the game's own guide of the board's spaces (DescriptionString). Real texts of the game files (2026.11.03), French and English.
// docs/evidence/0.1768-crucible-items.md.
internal static class BeastItems1768Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static readonly Dictionary<int, string> French = new()
    { [1] = "Thériens", [2] = "Insectoïdes", [3] = "Ptériens", [4] = "Floréens", [5] = "Hydrides", [6] = "Cuirassiens", [7] = "Animides", [8] = "Nécroïdes" };

    private static readonly Dictionary<int, string> English = new()
    { [1] = "Beastkin", [2] = "Vilekin", [3] = "Cloudkin", [4] = "Seedkin", [5] = "Wavekin", [6] = "Scalekin", [7] = "Soulkin", [8] = "Ashkin" };

    private static XbmRow Row(uint row, int kind, uint icon, string name, string description)
    {
        var cells = new object?[BeastItems.Columns];
        cells[0] = (byte)kind; cells[1] = icon; cells[2] = (ushort)0; cells[3] = name.ToLowerInvariant(); cells[11] = name; cells[12] = description; cells[13] = "";
        return new XbmRow(row, 0, cells);
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast items 1768: behaviour: a feed's families read from the bracket of its description, French and English, the effect before it", () =>
        {
            var (effect, families) = BeastItems.Split("Dégâts physiques infligés +20%\n[Utilisable sur: Thériens, Insectoïdes, Ptériens, Floréens, Hydrides, Cuirassiens, Nécroïdes]", French);
            Check(effect == "Dégâts physiques infligés +20%" && families.SequenceEqual([1, 2, 3, 4, 5, 6, 8]), $"Meat simular, French: {effect} / {string.Join(",", families)}");
            (effect, families) = BeastItems.Split("Physical damage dealt +20%\n[Suitable for beastkin, vilekin, cloudkin, seedkin, wavekin, scalekin, and ashkin.]", English);
            Check(effect == "Physical damage dealt +20%" && families.SequenceEqual([1, 2, 3, 4, 5, 6, 8]), "English, any case, « and » before the last: " + string.Join(",", families));
            (_, families) = BeastItems.Split("Critical hit rate +35% [Utilisable sur: Animides]", French);
            Check(families.SequenceEqual([7]), "one family");
            (_, families) = BeastItems.Split("Effect [Suitable for soulkin.]", English);
            Check(families.SequenceEqual([7]), "« soulkin » alone");
            (_, families) = BeastItems.Split("x [Suitable for ashkins and beastkinly]", English);
            Check(families.Count == 0, "whole words only: " + string.Join(",", families));
        }),
        ("Beast items 1768: behaviour: no bracket, or no family recognised in it: the whole text, no family (unknown, never every family)", () =>
        {
            var (effect, families) = BeastItems.Split("Restaure 10% des PV du dresseur ou d'une bête.", French);
            Check(effect == "Restaure 10% des PV du dresseur ou d'une bête." && families.Count == 0, "no bracket");
            (effect, families) = BeastItems.Split("Effet [Utilisable sur: Inconnus]", French);
            Check(effect == "Effet [Utilisable sur: Inconnus]" && families.Count == 0, "a bracket without a family");
        }),
        ("Beast items 1768: behaviour: rows read by kind, the empty row and unknown kinds left out; the feeds of one family", () =>
        {
            var rows = new[]
            {
                Row(0, 0, 0, "", ""), Row(1, 1, 243001, "Ceinture de force", "PV maximums +20%"), Row(76, 2, 243136, "Potion de bête I", "Restaure 10% des PV du dresseur ou d'une bête."),
                Row(146, 3, 243078, "Simili-viande", "Dégâts physiques infligés +20% [Utilisable sur: Thériens, Insectoïdes, Ptériens, Floréens, Hydrides, Cuirassiens, Nécroïdes]"),
                Row(149, 3, 243081, "Simili-boulette de poisson", "Chances de critiques +35% [Utilisable sur: Animides]"), Row(300, 9, 1, "Odd", "x"),
            };
            var items = rows.Select(row => BeastItems.Read(row, French)).OfType<BeastItem>().ToArray();
            Check(items.Select(item => item.Row).SequenceEqual([1u, 76u, 146u, 149u]), "four items: " + string.Join(",", items.Select(item => item.Row)));
            Check(items[0].Kind == BeastItems.Gear && items[0].Icon == 243001 && items[0].Effect == "PV maximums +20%" && items[0].Families.Count == 0, "the gear: icon, effect, no family");
            Check(BeastItems.Of(items, BeastItems.Feed, 7, "").Select(item => item.Row).SequenceEqual([149u]) && BeastItems.Of(items, BeastItems.Feed, 1, "").Select(item => item.Row).SequenceEqual([146u]), "each family its feeds");
            Check(BeastItems.Of(items, BeastItems.Feed, 0, "").Count == 2 && BeastItems.Of(items, BeastItems.Gear, 7, "").Count == 1, "every family; the family does not filter the gear");
            Check(BeastItems.Of(items, BeastItems.CrucibleItem, 0, "potion").Count == 1 && BeastItems.Of(items, BeastItems.Feed, 0, "critiques").Count == 1, "the search in names and effects");
            Check(BeastItems.UsableBy(items[3], 7) && !BeastItems.UsableBy(items[3], 1) && !BeastItems.UsableBy(items[0], 1), "usable by");
            Check(BeastItems.Read(new XbmRow(5, 0, new object?[13]), French) is null, "another column count: no item (a game update)");
        }),
        ("Beast items 1768: behaviour: the guide's rows: a title and a text for each space and each kind", () =>
        {
            Check(BeastItems.Guide.Select(rule => rule.Key).SequenceEqual(["Campsite", "Shop", "Treasure", "Random", "Gear", "CrucibleItem", "Feed"]), "seven rules");
            Check(BeastItems.Guide.All(rule => rule.Text == rule.Title + 27), "each text 27 rows after its title (1708 → 1735, 1715 → 1742), as the game's guide");
        }),
        ("Beast items 1768: wiring: the loader reads XBMItem raw (14 columns), XBMItemType and the guide, apart from the rest; the tab; both languages; the evidence", () =>
        {
            var loader = Read("src/TenshoAssistant/Beastmaster/BeastCatalogLoader.Dalamud.cs");
            Check(loader.Contains("if (itemSheet.Columns.Count == BeastItems.Columns)", StringComparison.Ordinal) && loader.Contains("return catalog with { Items = items, ItemKinds = kinds, Rules = rules };", StringComparison.Ordinal)
                && loader.Contains("catch (Exception error) when (error is not OperationCanceledException and not OutOfMemoryException) { return catalog; }", StringComparison.Ordinal), "read apart: a failure keeps the rest");
            var page = Read("src/TenshoAssistant/MainWindow.Beastmaster.cs");
            Check(page.Contains("(\"Items\", localization.Get(\"BeastUi.Items\"), () => DrawBeastItems(catalog)),", StringComparison.Ordinal) && page.Contains("BeastItems.Of(catalog.Items, kind, beastItemFamily, beastItemQuery)", StringComparison.Ordinal), "the tab, its lists worked out when something changes");
            Check(page.Contains("foreach (var rule in catalog.Rules.Where(rule => rule.Key is \"Campsite\" or \"Shop\" or \"Treasure\" or \"Random\")) DrawBeastRule(rule);", StringComparison.Ordinal), "the spaces' rules");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                Check(new[] { "ItemAllFamilies", "ItemEveryFamily", "ItemFamilies", "ItemFamiliesUnknown", "ItemFamilyTip", "ItemKind1", "ItemKind2", "ItemKind3", "ItemSearchTip", "ItemSpaces", "Items", "ItemsNone" }
                    .All(key => text.Contains($"    \"{key}\": ", StringComparison.Ordinal)), "the words, " + language);
            }
            var evidence = Read("docs/evidence/0.1768-crucible-items.md");
            Check(evidence.Contains("[Suitable for beastkin", StringComparison.Ordinal) && evidence.Contains("DescriptionString", StringComparison.Ordinal) && evidence.Contains("\n---\n", StringComparison.Ordinal), "the evidence, both halves");
        }),
    ];
}
