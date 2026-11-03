using TenshoAssistant.Items;

// 0.349 (g, X-07): wiki and Lodestone pages of an item, opened on the player's click or command only; hosts credited.
internal static class ExternalLinks349Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("External links: wiki by English name, Lodestone search in the Tensho language, names escaped",() =>
        {
            Check(ExternalLinks.Wiki("Darksteel Ore") == "https://ffxiv.consolegameswiki.com/wiki/Darksteel_Ore","wiki page = English name with underscores");
            Check(ExternalLinks.Lodestone("fr","Minerai d'acier sombre") == "https://fr.finalfantasyxiv.com/lodestone/playguide/db/search/?q=Minerai%20d%27acier%20sombre","French Lodestone, escaped");
            Check(ExternalLinks.Lodestone("ja","x").StartsWith("https://jp.",StringComparison.Ordinal) && ExternalLinks.Lodestone("zz","x").StartsWith("https://na.",StringComparison.Ordinal),"jp for Japanese, na otherwise");
            var items = new[] { new ItemDefinition(1,"Minerai de fer",false),new ItemDefinition(2,"Fer",false),new ItemDefinition(3,"Lingot de fer",false) };
            Check(ExternalLinks.Find(items,"fer")?.Id == 2 && ExternalLinks.Find(items,"lingot")?.Id == 3 && ExternalLinks.Find(items,"or pur") is null && ExternalLinks.Find(items," ") is null,"exact name first, else the shortest containing");
            Check(Read("Items/ItemContextMenuBridge.Dalamud.cs").Contains("Links?.Invoke(token.Item.ItemId)",StringComparison.Ordinal),"right-click entries");
            Check(Read("Plugin.cs").Contains("parts[0].ToLowerInvariant() is \"wiki\" or \"lodestone\"",StringComparison.Ordinal),"commands");
            Check(Read("Credits/CreditCatalog.cs").Contains("\"fr.finalfantasyxiv.com\"",StringComparison.Ordinal),"Lodestone hosts credited");
        }),
    ];
}
