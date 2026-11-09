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
            // 0.1019 (test audit #11, #51, #52): the merge tool knows exactly the kinds of the plugin, read from its KINDS line.
            var kinds=System.Text.RegularExpressions.Regex.Match(File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py")),@"(?m)^KINDS=set\(range\(1,(\d+)\)\)-\{([\d,]*)\}");
            Check(kinds.Success,"merge tool: KINDS line");
            var excluded=kinds.Groups[2].Value.Split(',',StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToHashSet();
            var tool=Enumerable.Range(1,int.Parse(kinds.Groups[1].Value)-1).Where(kind=>!excluded.Contains(kind)).ToHashSet();
            var plugin=Enum.GetValues<TenshoAssistant.Discoveries.DiscoveryKind>().Select(kind=>(int)kind).Where(kind=>kind>0).ToHashSet();
            Check(tool.SetEquals(plugin),"merge tool kinds = DiscoveryKind: only tool "+string.Join(",",tool.Except(plugin))+", only plugin "+string.Join(",",plugin.Except(tool)));
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs")).Contains("Item gain not attributed:"),"diagnosis");
        }),
    ];
}
