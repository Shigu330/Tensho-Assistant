// 0.865 (X-74): the sources seen in game first in the item's « How to get it ».
internal static class SeenSources865Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Seen sources: first, counted as known (no fallback asked), cached by revision",() =>
        {
            var hover=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MarketHover.cs"));
            Check(hover.Contains("foreach(var seen in DiscoveredSourcesOf(id)){known++;"),"first, known");
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Discoveries.cs"));
            Check(page.Contains("if(discoveredSources.Item==item && discoveredSources.Revision==store.Revision)return discoveredSources.Lines;"),"cached");
        }),
    ];
}
