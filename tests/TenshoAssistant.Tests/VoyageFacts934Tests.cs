using TenshoAssistant.Discoveries;

// X-76 P7 (0.934): submarine finds keep their HQ flag; one visit per explored sector (second find, surveillance).
internal static class VoyageFacts934Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Voyage facts 934: HQ finds, one visit per sector, merge tool aware",() =>
        {
            Check((int)DiscoveryKind.VoyageVisit==32 && !DiscoveryStore.HasPlace(DiscoveryKind.VoyageVisit),"kind");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/FreeCompany/VoyageWatcher.Dalamud.cs"));
            Check(watcher.Contains("found.ItemHQPrimary || found.ItemIdPrimary>=1_000_000") && watcher.Contains("SectorVisit?.Invoke(found.Point,found.DoubleDip,sub.SurveillanceBase+sub.SurveillanceBonus);"),"read");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs")).Contains("SeeSource(Discoveries.DiscoveryKind.VoyageVisit,sector,twice ? 1u:0u,surveillance,"),"written");
            Check(File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py")).Contains("KINDS=set(range(1,"),"merge tool");
        }),
    ];
}
