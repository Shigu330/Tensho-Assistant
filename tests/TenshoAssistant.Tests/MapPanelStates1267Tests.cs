// 0.1267 (X-156, Map and panels): the map's lists and search, the list panels and the Triple Triad panels say waiting, failed and
// estimate as everywhere.
internal static class MapPanelStates1267Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map and panel states 1267: the map's lists and search wait or fail as everywhere",() =>
        {
            var lists=Read("MainWindow.MapLocalLists.cs");
            Check(lists.Contains("StateLine(UI.UiState.Failed,localization.Format(\"MapLocal.IndexError\",error))") && lists.Contains("StateLine(UI.UiState.Waiting,localization.Format(\"MapLocal.IndexLoading\","),"positions: failed or waiting");
            Check(!lists.Contains("ImGui.TextColored(UI.FfxivTheme.Error,localization.Format(\"MapLocal.IndexError\""),"no error colour written by hand");
            foreach(var key in new[]{"GatheringLoading","VendorsLoading"})Check(lists.Contains("StateLine(UI.UiState.Waiting,localization.Get(\"MapLocal."+key+"\"))"),key);
            Check(Read("MainWindow.MapSearch.cs").Contains("StateLine(UI.UiState.Waiting,localization.Get(\"MapsUi.FindLoading\"))"),"search: waiting");
            Check(Read("MainWindow.Fates.cs").Contains("StateLine(UI.UiState.Waiting,localization.Get(\"Fates.Loading\"))"),"FATEs: waiting");
            Check(Read("MainWindow.Maps.cs").Contains("UI.UiComponents.SubTitle(selected.Zone);"),"the chosen zone is a sub-title");
        }),
        ("Map and panel states 1267: panels wait as everywhere; simulated odds and limited advice are estimates",() =>
        {
            Check(Read("MainWindow.ListPanels.cs").Contains("StateLine(UI.UiState.Waiting,localization.Get(\"ListPanels.CurrenciesLoading\"))"),"currencies panel: waiting");
            var advice=Read("MainWindow.TriadAdvice.cs");
            Check(advice.Contains("StateLine(UI.UiState.Waiting,localization.Get(\"TriadAdvice.Waiting\"))"),"Triple Triad advice: waiting");
            Check(advice.Contains("if(!advice.Exact)OriginMark(UI.UiOrigin.Estimated,localization.Get(\"TriadAdvice.Approximate\"));"),"an advice cut by the time limit: estimate; an exact one: no mark");
            Check(Read("MainWindow.TriadRequest.cs").Contains("OriginLine(UI.UiOrigin.Estimated,localization.Get(\"TriadRequest.Estimate\"))"),"challenge panel: the odds are an estimate");
            var simulation=Read("MainWindow.TriadSimulation.cs");
            Check(simulation.Contains("if(result.Exact)ImGui.TextColored(Success,localization.Get(\"TriadSim.Exact\"));") && simulation.Contains("else OriginLine(UI.UiOrigin.Estimated,localization.Get(\"TriadSim.Estimate\"));"),"simulation: exact, or an estimate");
            Check(Read("MainWindow.Squadron.cs").Contains("OriginLine(UI.UiOrigin.Estimated,localization.Get(\"Squadron.Estimate\"))"),"squadron: the success percent is an estimate");
        }),
    ];
}
