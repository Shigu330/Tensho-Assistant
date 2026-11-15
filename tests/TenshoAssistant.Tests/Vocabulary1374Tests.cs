using System.Text.Json;

// X-405 (0.1374, D-170 and D-171, choice A while the user has not answered, R14): « duty » is « mission » in French, the client's official name
// (Addon#2225.0, #3770.0, #11170.0, MainCommand#43.5); « contenu » is kept for the CONTENUS group, ranked content and the contents of something.
// An NPC who sells is a « marchand » (Addon#3408.0, #7610.0, MainCommand#71.6); « vendeur » stays the market-board player (Addon#6996.0).
internal static class Vocabulary1374Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static IEnumerable<(string Key, string Text)> Texts(string language)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")));
        var stack = new Stack<(string Path, JsonElement Value)>();
        stack.Push(("", document.RootElement));
        var found = new List<(string, string)>();
        while (stack.Count > 0)
        {
            var (path, value) = stack.Pop();
            if (value.ValueKind == JsonValueKind.Object) foreach (var property in value.EnumerateObject()) stack.Push((path.Length == 0 ? property.Name : path + "." + property.Name, property.Value));
            else if (value.ValueKind == JsonValueKind.String) found.Add((path, value.GetString()!));
        }
        return found;
    }

    // Where « contenu » keeps its other senses: the CONTENUS group, ranked or competitive content, the contents of something.
    private static readonly string[] ContenuKept =
    [
        "Help.AnswerSafe", "Help.AnswerPause", "Guide.InterestContent", "Guide.WindowArmoireWhere", "Navigation.CategoryContent", "Advanced.DesktopLinkTip",
        "Advanced.UndercutTip", "Competitive.Off", "HoverUi.Supplement3", "BankUi.UnreadBags", "Desktop.LinkPaused",
    ];
    // Where « vendeur » is the market-board player, not an NPC.
    private static readonly string[] VendeurKept =
    [
        "RetainerSales.SeenAt", "RetainerSales.Compare", "RetainerSales.Alone", "RetainerSales.CopyTip", "DiscoveriesUi.MarketPricesTip", "CraftStock.ValueHelp",
        "Advanced.RetainerSalesTip", "MarketBoard.SeenHelp",
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Vocabulary 1374: « contenu » is no longer a duty in French; its other senses are kept where listed", () =>
        {
            var left = Texts("fr").Where(text => text.Text.Contains("contenu", StringComparison.OrdinalIgnoreCase) && !ContenuKept.Contains(text.Key)).Select(text => text.Key).ToArray();
            Check(left.Length == 0, "« contenu » left for a duty: " + string.Join(", ", left));
            var fr = Texts("fr").ToDictionary(text => text.Key, text => text.Text);
            Check(fr["Category.DutyLoot"] == "Butin des missions" && fr["MapView.HideInDuty"] == "Masquer en mission" && fr["DutyUi.Duty"] == "Mission", "the page, the map's setting and the column");
        }),
        ("Vocabulary 1374: an NPC who sells is a « marchand »; « vendeur » is only the market-board player; one name for the tool", () =>
        {
            var left = Texts("fr").Where(text => text.Text.Contains("vendeu", StringComparison.OrdinalIgnoreCase) && !VendeurKept.Contains(text.Key)).Select(text => text.Key).ToArray();
            Check(left.Length == 0, "« vendeur » left for an NPC: " + string.Join(", ", left));
            var fr = Texts("fr").ToDictionary(text => text.Key, text => text.Text);
            var tool = new[] { "PageHelp.NpcVendorsTitle", "Category.NpcVendors", "NpcUi.Title", "ToolDirectory.NpcVendors.Name", "MapView.ShowVendors", "ListPanels.Vendor" };
            Check(tool.All(key => fr[key] == "PNJ marchands"), "one name for the tool: " + string.Join(" | ", tool.Select(key => $"{key}={fr.GetValueOrDefault(key)}")));
            var en = Texts("en").ToDictionary(text => text.Key, text => text.Text);
            Check(!en.Values.Any(text => text.Contains("merchant directory", StringComparison.OrdinalIgnoreCase) || text.Contains("Shopkeeper", StringComparison.Ordinal)), "en aligned on « vendor »");
        }),
    ];
}
