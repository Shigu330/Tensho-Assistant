using System.Numerics;
using TenshoAssistant;
using TenshoAssistant.Discoveries;

// X-76 step 5 (0.949): PaissaDB switch in « game data only », shared sightings rounded to the day, warning before connecting, manifest action listed.
internal static class NetworkRules949Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Network rules 949: PaissaDB switch, day only in shared sightings, warning, manifest",() =>
        {
            var c=new Configuration();
            Check(c.CommunityPaissa && (CommunitySources.Bits(c)&128)!=0,"PaissaDB on by default, in the bits");
            CommunitySources.ApplyGameDataOnly(c,true);Check(!c.CommunityPaissa,"cut by game data only");
            CommunitySources.ApplyGameDataOnly(c,false);Check(c.CommunityPaissa,"given back");
            var store=new DiscoveryStore();var at=new DateTimeOffset(2026,11,8,21,37,12,TimeSpan.Zero);
            store.SeeSource(DiscoveryKind.FishCatch,5,1,1,at,1,1,new Vector3(1,0,1));
            var mine=CommunityDiscoveries.Mine(store.Snapshot(at,false)).Facts.Single();
            Check(mine.First==new DateTimeOffset(2026,11,8,0,0,0,TimeSpan.Zero) && mine.Last==mine.First,"day only");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Discoveries.cs")).Contains("localization.Get(\"DiscoveriesUi.PublicWarning\")"),"warning");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/TenshoAssistant.json")).Contains("equipping a gear set"),"manifest");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Plots.cs")).Contains("!configuration.CommunityPaissa"),"plots gated");
        }),
    ];
}
