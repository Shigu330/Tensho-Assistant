// 0.877 (X-75.3): mooch put down to the fish caught before, lures and the previous weather kept, seen lines in the fishing panel.
using TenshoAssistant.Discoveries;
internal static class Fishing877Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Fishing: mooch flag 16 uses the last fish, lures 1 and 2 kept, previous weather from the forecast; the panel shows what was seen",() =>
        {
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("var bait=(flags & 16)!=0 && lastFish!=0 ? lastFish:") && watcher.Contains("lastFish=item;"),"mooch");
            Check(watcher.Contains("if((flags & 3) is 1 or 2)store.SeeSource(DiscoveryKind.FishLure,"),"lure");
            Check(watcher.Contains("store.SeeSource(DiscoveryKind.FishTransition,item,before,EorzeaMinute(at),at,territory);"),"previous weather");
            Check(!DiscoveryStore.HasPlace(DiscoveryKind.FishLure) && !DiscoveryStore.HasPlace(DiscoveryKind.FishTransition),"no place");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.FishingPanel.cs")).Contains("if(SeenFishLine(fishId) is { } seenLine)"),"panel");
        }),
    ];
}
