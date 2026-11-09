// 0.882 (X-77), 0.885 (only the automatic GitHub exchange kept): own sightings only, the common file, read from main.
using TenshoAssistant.Discoveries;
internal static class CommunityDiscoveries882Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Community discoveries: only what you saw is shared, imports reset; the common file parses; no other way to send than the GitHub exchange",() =>
        {
            var at=new DateTimeOffset(2026,11,8,12,0,0,TimeSpan.Zero);
            var store=new DiscoveryStore();
            store.SeeSource(DiscoveryKind.Venture,5,7,2,at);
            store.Merge([new DiscoveryFact{Kind=DiscoveryKind.Venture,Subject=6,Detail=7,LevelMin=1,LevelMax=1,Imported=1,First=at,Last=at}]);
            var mine=CommunityDiscoveries.Mine(store.Snapshot(at,false));
            Check(mine.Facts.Count==1 && mine.Facts[0].Subject==5 && mine.Facts[0].Imported==0,"own sightings only");
            Check(CommunityDiscoveries.Parse(File.ReadAllText(Path.GetFullPath("community-discoveries/discoveries.json"))) is { Facts.Count:>0 } common
                && common.Facts.All(fact=>fact.Seen==0 && fact.Imported>0 && DiscoveryStore.Valid(fact)),"common file");
            Check(CommunityDiscoveries.Address.StartsWith("https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/",StringComparison.Ordinal),"read from main");
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Discoveries.cs"));
            Check(!page.Contains("DiscoveriesUi.TakeCommon") && !page.Contains("DiscoveriesUi.Share\"") && !File.Exists(Path.GetFullPath(".github/ISSUE_TEMPLATE/discoveries.yml")),"no other way");
        }),
    ];
}
