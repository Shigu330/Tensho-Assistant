// X-97 (0.982): the fishing helper shows a collectable fish's tiers and scrips, from the same rules as the gathering panel.
internal static class FishCollectable982Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Fish collectable 982: tiers and scrips under the fish, scrip exchanges only",() =>
        {
            var panel=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.FishingPanel.cs"));
            Check(panel.Contains("Collectables?.Catalog?.For(row.FishId) is { Count:>0 } collectable && collectable.FirstOrDefault(rule=>rule.Scrips) is { } scrip"),"rules");
            Check(panel.Contains("\"FishingPanel.CollectableTip\""),"scrips in the tooltip");
        }),
    ];
}
