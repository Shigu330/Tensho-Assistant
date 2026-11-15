using System.Text.Json;

// X-397 (0.1368, review of 0.1362 to 0.1364, I1): one name for the craft lists in French, « liste d'artisanat », titles and labels
// included, whatever the case; the label a tip quotes is the page's own title.
internal static class CraftListName1368Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Key(JsonElement root, string section, string key) => root.GetProperty(section).GetProperty(key).GetString() ?? "";

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Craft list name 1368: « liste d'artisanat » only, case-insensitive, titles included", () =>
        {
            var text = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json"));
            foreach (var variant in new[] { "liste de fabrication", "listes de fabrication" })
                Check(!text.Contains(variant, StringComparison.OrdinalIgnoreCase), "French: « " + variant + " » in any case");
            using var json = JsonDocument.Parse(text);
            var root = json.RootElement;
            Check(Key(root, "CraftLists", "Title") == "Listes d'artisanat" && Key(root, "CraftLists", "PanelTitle") == "Liste d'artisanat", "page and panel titles");
            Check(Key(root, "CraftSimulation", "ListsTab") == "Liste d'artisanat", "the crafting tab");
            Check(Key(root, "GatheringPanel", "ListSetting").StartsWith("Liste d'artisanat", StringComparison.Ordinal)
                && Key(root, "GatheringPanel", "ListSettingTip").Contains("liste d'artisanat", StringComparison.Ordinal), "the label and its tip say the same");
            Check(Key(root, "CraftLists", "AddTip").Contains("« " + Key(root, "CraftLists", "Title") + " »", StringComparison.Ordinal), "the tip quotes the page's own title");
        }),
    ];
}
