// 0.1102 (relais 21): two parsers missing after relais 20: the gil of the ocean fishing missions get their own category, and an airship sector
// unlocked (log message 4163) becomes a discovery fact, as the submarines' VoyageUnlock.
using TenshoAssistant.Discoveries;
using TenshoAssistant.Inventory;

internal static class GainParsers1102Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gil journal: the ocean fishing missions' gil, from their windows (dalamud.log of 11/11)",() =>
        {
            var now = DateTimeOffset.UtcNow;
            Check(GilJournal.Classify(new Dictionary<string,DateTimeOffset> { ["IKDMission"] = now.AddSeconds(-3),["IKDFishingLog"] = now.AddSeconds(-1) },now) == GilCategory.OceanFishing,"mission");
            Check(GilJournal.Classify(new Dictionary<string,DateTimeOffset> { ["IKDFishingLog"] = now.AddSeconds(-30) },now) == GilCategory.Other,"too old: Other");
            Check((int)GilCategory.OceanFishing == 8 && (int)GilCategory.Mail == 7 && !GilJournal.IsTransfer(GilCategory.OceanFishing),"added last, not a transfer");
            foreach (var language in new[] { "en","fr" })
                Check(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")).Contains("\"CategoryOceanFishing\": "),language + ": label");
        }),
        ("Discoveries: an airship sector unlocked (log message 4163) is a fact, checked against the sheet",() =>
        {
            Check(GainLogMessages.UnlockedAirshipPoint(4163,12) == 12 && GainLogMessages.UnlockedAirshipPoint(4161,12) == 0
                  && GainLogMessages.UnlockedAirshipPoint(4163,null) == 0 && GainLogMessages.UnlockedAirshipPoint(4163,0) == 0,"the first number of 4163 only");
            Check((int)DiscoveryKind.AirshipUnlock == 38 && !DiscoveryStore.HasPlace(DiscoveryKind.AirshipUnlock),"kind 38, no place");
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("GetRowOrDefault(point) is { } row && !row.Name.IsEmpty") && plugin.Contains("DiscoveryKind.AirshipUnlock,point,0,1,"),"kept only when the sheet names it, detail 0");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Discoveries.cs"));
            Check(page.Contains("DiscoveryKind.AirshipUnlock=>localization.Format(\"DiscoveriesUi.AirshipSectorUnlock\"") && page.Contains("DiscoveryKind.FishPoints,DiscoveryKind.AirshipUnlock]"),"label, searched");
            foreach (var language in new[] { "en","fr" })
            {
                var text = File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                Check(text.Contains("\"SummaryAirshipUnlock\": ") && text.Contains("\"AirshipSectorUnlock\": "),language + ": summary and label");
            }
        }),
    ];
}
