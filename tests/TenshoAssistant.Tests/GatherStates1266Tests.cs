// 0.1266 (X-156, Gathering and fishing): the node read in game is « measured », the Bountiful bonus and the expected yield of a GP plan
// are flagged « estimate » (community rule), reads in progress are « waiting », as everywhere.
internal static class GatherStates1266Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gather states 1266: the node read is measured; without a recent read it waits, or failed on a read error",() =>
        {
            var observation=Read("MainWindow.GatheringObservation.cs");
            Check(observation.Contains("OriginLine(UI.UiOrigin.Measured,localization.Format(\"GatheringObservation.Summary\","),"the read node: measured, with its summary");
            Check(observation.Contains("StateLine(state.Status==GatheringObservationStatus.Error ? UI.UiState.Failed:UI.UiState.Waiting,"),"no recent read: waiting, or failed");
            Check(observation.Contains("UI.UiComponents.BeginDataTable(\"ObservedGatheringRows\",4)"),"common table");
        }),
        ("Gather states 1266: the Bountiful bonus and a plan's expected yield are flagged as estimates, with the reason in both languages",() =>
        {
            var gp=Read("MainWindow.GatheringGp.cs");
            Check(gp.Contains("OriginMark(UI.UiOrigin.Estimated,localization.Get(\"GpUi.BountifulRule\"));"),"GP tool: the estimated bonus carries the mark");
            Check(gp.IndexOf("GpUi.BountifulEstimateHelp",StringComparison.Ordinal)<gp.IndexOf("OriginMark(UI.UiOrigin.Estimated",StringComparison.Ordinal),"the bonus keeps its own tooltip, the mark comes after");
            Check(gp.Contains("StateLine(UI.UiState.Waiting,localization.Get(\"GpUi.Loading\"))") && gp.Contains("UI.UiComponents.BeginDataTable(\"GpFrontier\",3)"),"GP tool: waiting, common table");
            var panel=Read("MainWindow.GatheringPanel.cs");
            var at=panel.IndexOf("localization.Format(\"GatheringPanel.Expected\"",StringComparison.Ordinal);
            Check(at>0 && panel[at..].IndexOf("OriginMark(UI.UiOrigin.Estimated,",StringComparison.Ordinal) is >0 and <260,"panel: the expected yield carries the mark");
            foreach(var file in new[]{"en","fr"})
                Check(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{file}.json")).Contains("\n    \"BountifulRule\": \""),file+": GpUi.BountifulRule");
        }),
        ("Gather states 1266: fishing and gathering pages say waiting as everywhere and use the common tables and sub-titles",() =>
        {
            Check(Read("MainWindow.GatheringFish.cs").Contains("StateLine(UI.UiState.Waiting,localization.Get(\"FishingPanel.GatherBuddyLoading\"))")
                && Read("MainWindow.FishingPanel.cs").Contains("StateLine(UI.UiState.Waiting,localization.Get(\"FishingPanel.GatherBuddyLoading\"))"),"fish data: waiting");
            var sources=Read("MainWindow.FishingSources.cs");
            Check(sources.Contains("StateLine(UI.UiState.Waiting,localization.Get(\"FishingSources.Loading\"))") && !sources.Contains("ImGui.TextColored(Accent"),"fishing sources: waiting, sub-titles");
            Check(Read("MainWindow.Gathering.cs").Contains("UI.UiComponents.BeginDataTable(\"GatheringSources\", 6)") && Read("MainWindow.GatheringBrowser.cs").Contains("UI.UiComponents.BeginDataTable(\"GatheringNowRows\",4)"),"common tables");
            Check(Read("MainWindow.GatheringRoute.cs").Contains("UI.UiComponents.SubTitle(localization.Format(\"ChainUi.RouteZone\"") && Read("MainWindow.FishWindows.cs").Contains("UI.UiComponents.SubTitle(localization.Get(\"FishWindows.Title\"))"),"zone and windows titles are sub-titles");
        }),
    ];
}
