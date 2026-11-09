using TenshoAssistant.FreeCompany;

// X-73 (0.1003): airship finds paired with the voyage log's sectors, only when stops and sectors match; a new discovery kind known everywhere.
internal static class AirshipLoot1003Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Airship loot 1003: stops with finds paired in order with the route, nothing when the counts differ",() =>
        {
            // The user's voyage of 2026-11-08 18:50: stop 1 gave wind crystals (10) and 7588, stop 2 gave 12224.
            var stops=new[]{(10u,19,7588u,15),(12224u,6,0u,0),(0u,0,0u,0),(0u,0,0u,0),(0u,0,0u,0)};
            var paired=AirshipLogRoute.Pair(stops,[1u,2u]);
            Check(paired is { Count:3 } && paired[0]==(10u,1u,19) && paired[1]==(7588u,1u,15) && paired[2]==(12224u,2u,6),"paired");
            Check(AirshipLogRoute.Pair(stops,[1u])==null && AirshipLogRoute.Pair(stops,[1u,2u,3u])==null,"counts differ");
        }),
        ("Airship loot 1003: the kind is labelled, an item source, known by the merge tool; unattributed gains logged in developer mode",() =>
        {
            Check(Enum.IsDefined(TenshoAssistant.Discoveries.DiscoveryKind.AirshipDrop),"kind");
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Discoveries.cs"));
            Check(page.Contains("DiscoveryKind.AirshipDrop=>localization.Format(\"DiscoveriesUi.FromAirshipSector\"") && page.Contains("DiscoveryKind.VoyageDrop,DiscoveryKind.AirshipDrop,"),"label, source");
            Check(File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py")).Contains("KINDS=set(range(1,38))-{26,27}"),"merge tool");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs")).Contains("Item gain not attributed:"),"diagnosis");
        }),
    ];
}
