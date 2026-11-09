using TenshoAssistant.Discoveries;

// X-76 P6 (0.933): fishing facts: bite time by bait, Fisher's Intuition start and the catches under it; new kinds known to the merge tool.
internal static class FishingFacts933Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Fishing facts 933: new kinds, placed where they should, written by the watcher, merge tool aware",() =>
        {
            Check((int)DiscoveryKind.FishBiteBait==29 && (int)DiscoveryKind.IntuitionStart==30 && (int)DiscoveryKind.IntuitionCatch==31,"numbers");
            Check(DiscoveryStore.HasPlace(DiscoveryKind.FishBiteBait) && DiscoveryStore.HasPlace(DiscoveryKind.IntuitionStart) && !DiscoveryStore.HasPlace(DiscoveryKind.IntuitionCatch),"places");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("store.SeeSource(DiscoveryKind.FishBiteBait,item,bait,") && watcher.Contains("store.SeeSource(DiscoveryKind.IntuitionStart,trigger,0,intuition.Seconds,")
                && watcher.Contains("store.SeeSource(DiscoveryKind.IntuitionCatch,item,running.Trigger,1,at,territory);"),"written");
            Check(File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py")).Contains("PLACED={1,2,3,8,10,19,22,23,28,29,30}"),"merge tool");
        }),
    ];
}
