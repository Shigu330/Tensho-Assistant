using TenshoAssistant.Npcs;

// 0.352 (g, I-11): unlock conditions of shop articles read from the game data, kept in the vendor cache (new version), shown after the price.
internal static class ShopRequirements352Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Shop requirements: quest and achievement per article, none by default, cache version changed, drawn after every price",() =>
        {
            var plain = new NpcShopSale(1,NpcShopKind.Gil,"Shop",5057,1,false,[]);
            Check(plain.QuestRequired == 0 && plain.AchievementRequired == 0,"nothing required by default (older cached rows too)");
            var loader = Read("Npcs/NpcVendorLoader.Dalamud.cs");
            Check(loader.Contains("entry.AchievementRequired.RowId",StringComparison.Ordinal) && loader.Contains("entry.Quest.RowId,entry.AchievementUnlock.RowId",StringComparison.Ordinal),"gil and special shops");
            Check(loader.Contains("fcc-295;req-352",StringComparison.Ordinal),"vendor cache rebuilt once with the new fields");
            Check(Read("MainWindow.NpcVendors.cs").Contains("if(sale.QuestRequired!=0 || sale.AchievementRequired!=0)",StringComparison.Ordinal),"shown wherever a price is");
            Check(Read("MainWindow.ShopRequirements.cs").Contains("done ? true:null",StringComparison.Ordinal),"an achievement not confirmed is « not read », never « to do »");
        }),
    ];
}
