using TenshoAssistant.Discoveries;

// X-85 V4 (0.962): the catch just before a spectral current is noted (seen with, never « triggers »), shown at the stop, known to the merge tool.
internal static class SpectralBefore962Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Spectral before 962: kind, noted at the current's start within 90 s, panel line, merge tool",() =>
        {
            Check((int)DiscoveryKind.SpectralBefore==35 && !DiscoveryStore.HasPlace(DiscoveryKind.SpectralBefore),"kind");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("if(spectral && !spectralBefore && lastFish!=0 && now-lastFishAt is >= 0 and <= 90_000)") && watcher.Contains("store.SeeSource(DiscoveryKind.SpectralBefore,lastFish,lastFishBait,1,at,territory);"),"noted");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.OceanVoyagePanel.cs")).Contains("Find(Discoveries.DiscoveryKind.SpectralBefore,"),"panel");
            Check(File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py")).Contains("KINDS=set(range(1,"),"merge tool");
        }),
    ];
}
