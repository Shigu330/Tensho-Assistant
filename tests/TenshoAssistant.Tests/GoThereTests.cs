using TenshoAssistant.Maps;
// Test audit of 2026-11-09, batch 2 (0.1021): GoThere397Tests, GoThereMenu762Tests, HiddenGoThere834Tests merged into GoThereTests; every case is kept.
internal static class GoThereTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From GoThere397Tests: 0.397 (g, R-03): « Go there » on a gathering source — flag, Tensho map, nearest aetheryte's question (never an automatic teleport),
        // disabled without a known position.
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
        // From GoThereMenu762Tests: 0.762 (X-15, user request): « Go there » in an item's right-click menu.
        ("Go there from the menu: the same steps as the Gathering page, an open and near point first, only when placed",() =>
        {
            var links=Read("MainWindow.DutyFinder.cs");
            Check(links.Contains("var go=GoThereSource(item.Id);") && links.Contains("if(go is { } there && ImGui.Selectable(localization.Get(\"GatheringDetails.GoThere\")"),"in the menu");
            Check(links.Contains("AskNearestAetheryteAt(there.Target.Map,there.Target.Pixel,") && links.Contains("direct:configuration.GoThereTeleport"),"the same steps: a question unless the setting says otherwise");
            Check(links.Contains(".ThenBy(pair=>pair.Source.Zone==here ? 0:1).ThenBy(pair=>ListPanelZoneCost(pair.Source.Zone) ?? uint.MaxValue)"),"near first");
        }),
        // From HiddenGoThere834Tests: 0.834 (X-64, user report): « Go there » for an item only found as a hidden item of its point.
        ("Go there: hidden sources ranked last, not left out",() =>
        {
            var menu=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.DutyFinder.cs"));
            Check(!menu.Contains("sources.Where(source=>!source.IsHidden)"),"not left out");
            Check(menu.Contains(".OrderBy(pair=>pair.Source.IsHidden ? 1:0)"),"ranked last");
            Check(menu.Contains("GatherAccess?.Invoke(source.BaseId)?.Blocks(source.Level)!=true"),"0.835: unavailable left out");
            var access=new TenshoAssistant.Gathering.GatherPointAccess(null,17,60);
            Check(!access.Blocks(60) && access.Blocks(61),"level");
            Check(new TenshoAssistant.Gathering.GatherPointAccess("Tome",17,90).Blocks(10),"tome");
        }),
        ("Teleport list (0.662): the game's function is called only with the player there for a moment",() =>
        {
            var at=new DateTimeOffset(2026,11,6,12,0,0,TimeSpan.Zero);
            var gate=new TenshoAssistant.Maps.TeleportListGate();
            Check(!gate.Allowed(false,false,at),"no player");
            Check(!gate.Allowed(true,true,at.AddSeconds(1)),"between zones");
            Check(!gate.Allowed(true,false,at.AddSeconds(2)),"just back");
            Check(!gate.Allowed(true,false,at.AddSeconds(4)),"not yet");
            Check(gate.Allowed(true,false,at.AddSeconds(5)),"settled");
            Check(!gate.Allowed(false,false,at.AddSeconds(6)) && !gate.Allowed(true,false,at.AddSeconds(7)),"it starts again after a change of zone");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs")).Replace("\r\n","\n");
            Check(reader.Contains("if(listAllowed && Interlocked.Exchange(ref costsRequest,0)==1)"),"a request waits, it is not spent");
            Check(reader.IndexOf("listAllowed=teleportGate.Allowed")<reader.IndexOf("ReadTeleportCosts());"),"checked first");
        }),
    ];
}
