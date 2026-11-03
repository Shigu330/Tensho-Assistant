// 0.397 (g, R-03): « Go there » on a gathering source — flag, Tensho map, nearest aetheryte's question (never an automatic teleport),
// disabled without a known position.
internal static class GoThere397Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Go there: flag then the nearest aetheryte's question, position from the local index, disabled when unknown",() =>
        {
            var page = Read("MainWindow.Gathering.cs");
            Check(page.Contains("AskNearestAetheryteAt(target.Map,target.Pixel,",StringComparison.Ordinal) && page.Contains("ImGui.BeginDisabled(goTo is null);",StringComparison.Ordinal),"button");
            var ask = Read("MainWindow.ChatMapLinks.cs");
            Check(ask.Contains("mapNearestRequest=(map.MapId,pixel,place,",StringComparison.Ordinal) && ask.Contains("request.Direct && !failed",StringComparison.Ordinal),"question unless the setting asks for a direct teleport");
            Check(!new TenshoAssistant.Configuration().GoThereTeleport && TenshoAssistant.ConfigurationReset.Reset.Contains("GoThereTeleport"),"off by default (0.399)");
            var goTo = Read("MainWindow.GatheringGoTo.cs");
            Check(goTo.Contains("index.Fishing(source.PointId)",StringComparison.Ordinal) && goTo.Contains("index.Gathering(source.BaseId)",StringComparison.Ordinal),"positions");
        }),
    ];
}
