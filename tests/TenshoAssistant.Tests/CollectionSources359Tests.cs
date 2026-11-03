// 0.359 (g, C-08): Collections filter by source, from the right-click menu's sources, one cached mask per entry.
internal static class CollectionSources359Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Collection sources: masks from SourceLines, kept until a catalog changes, filter only on kinds with unlock items",() =>
        {
            var sources = Read("MainWindow.CollectionSources.cs");
            Check(sources.Contains("foreach(var line in SourceLines(items,request:false))mask|=1<<line.Group;",StringComparison.Ordinal)
                && !Read("MainWindow.SourceLinks.cs").Contains("itemSources?.Get(item.Id)",StringComparison.Ordinal)
                && sources.Contains("collectionSourceMasks.Clear();collectionSourceRevision++;",StringComparison.Ordinal),"same sources as the menu without any request, cache dropped with the catalogs");
            Check(sources.Contains("kind is CollectionKind.Mount or CollectionKind.Companion or CollectionKind.Orchestrion",StringComparison.Ordinal),"kinds with unlock items");
            var page = Read("MainWindow.Collections.cs");
            Check(page.Contains("StateMatches(entry) && CollectionSourceMatches(entry,sourceFilter)",StringComparison.Ordinal)
                && page.Contains("collectionViewSourceRevision!=collectionSourceRevision",StringComparison.Ordinal),"list rebuilt on filter or revision change only");
            var group = Read("MainWindow.SourceLinks.cs");
            Check(group.Contains("private enum SourceGroup { Achievements, Duties, FromItems, Trades, Vendors, Quests, Craft, Gathering }",StringComparison.Ordinal),"bits fit below the market bit");
        }),
    ];
}
