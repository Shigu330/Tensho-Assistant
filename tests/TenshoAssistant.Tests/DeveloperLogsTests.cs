using TenshoAssistant.Maps;
// Test audit of 2026-11-09, batch 2 (0.1021): LinkTest836Tests, MenuLog781Tests, ListPanelLog840Tests, NearbyLog818Tests, MessageCapture809Tests, MapMarkers842Tests, MapMarks791Tests merged into DeveloperLogsTests; every case is kept.
internal static class DeveloperLogsTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From LinkTest836Tests: 0.836 (X-03): the developer command that prints the link in four forms.
        ("Link test: developer mode only, four forms, each click logged",() =>
        {
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("is \"linktest\" && mainWindow.DeveloperMode"),"developer mode only");
            Check(plugin.Contains("Link test clicked: Tensho link") && plugin.Contains("Type=Dalamud.Game.Text.XivChatType.Echo"),"logged, Echo forms");
        }),
        // From MenuLog781Tests: 0.781 (X-03): what the game opens on a right click, in developer mode.
        ("Menu log: developer mode only, before any early return",() =>
        {
            var bridge=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Items/ItemContextMenuBridge.Dalamud.cs"));
            Check(bridge.Contains("if(configuration.DeveloperMode)") && bridge.IndexOf("Context menu opened:")<bridge.IndexOf("if(disposed || !configuration.ItemContextMenuEnabled"),"logged first");
        }),
        // From ListPanelLog840Tests: 0.840 (X-65): the list panel's lines written to the log in developer mode, on change.
        ("List panel log: developer mode, on change only",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ListPanels.cs"));
            Check(page.Contains("if(listPanelLogged.GetValueOrDefault(key)!=line){listPanelLogged[key]=line;log.Information(\"List panel {0}: {1}.\",key,line);}"),"on change");
        }),
        // From NearbyLog818Tests: 0.818 (X-53): the nearby monsters' chain written to the log in developer mode, to find why a list's monster is not marked.
        ("Nearby monsters log: developer mode only, written when it changes",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.NearbyMonsters.cs"));
            Check(page.Contains("if(!developerMode || line==nearbyMonstersLogged)return;"),"developer mode, changes only");
            Check(page.Contains("list needs {needed.Count} items") && page.Contains("drop data {(lumina?.State.Data is null"),"what each step sees");
        }),
        // From MessageCapture809Tests: 0.809 (X-43, step 3): the game's messages captured, bounded, without player names, before reading drops, spells and catches.
        ("Message capture: the game's own messages, five per kind, eighty kinds, player names masked, cut in PvP",() =>
        {
            var capture=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs"));
            Check(capture.Contains("if(message.Sender.TextValue.Trim().Length!=0)return;"),"the game only");
            Check(capture.Contains("if(seen>=5 || messageKinds.Count>=80 && seen==0)return;") && capture.Contains("Line(\"messages\",400,"),"bounded");
            Check(capture.Contains("case Dalamud.Game.Text.SeStringHandling.Payloads.PlayerPayload:text.Append(\"<player>\");break;"),"names masked");
            Check(capture.IndexOf("if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;")<capture.IndexOf("var kind=(int)message.LogKind;"),"cut first");
        }),
        // From MapMarkers842Tests: 0.842 (X-67): the game map's own markers with flags and second icon, in the developer capture.
        ("Map markers capture: flags and second icon, bounded, on change",() =>
        {
            var capture=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs"));
            Check(capture.Contains("var count=Math.Min((int)agent->MapMarkerCount,agent->MapMarkers.Length);") && capture.Contains("shown<60"),"bounded");
            Check(capture.Contains("if(now==mapMarkers)return;"),"on change");
        }),
        // From MapMarks791Tests: 0.791 (X-01): where the map's 110-160 ms frames go, measured in parts (developer mode).
        ("Map marks: developer mode only, started with the canvas, in order",() =>
        {
            var map=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(map.Contains("private void MapMarkStart(){if(DeveloperMode)") && map.Contains("if(!DeveloperMode)return;"),"developer mode");
            var parts=new[]{"MapMarkStart();","MapMark(\"canvas texture, names, zones\");","MapMark(\"canvas gathering points\");","MapMark(\"canvas areas\");","MapMark(\"canvas icons\");","MapMark(\"canvas bookmarks, treasure, player\");","finally{MapMark(\"canvas edges and tooltips\");draw.PopClipRect();}"};
            for(var index=1;index<parts.Length;index++)Check(map.IndexOf(parts[index-1])<map.IndexOf(parts[index]) && map.IndexOf(parts[index-1])>0,"order "+index);
            foreach(var mark in new[]{"canvas vendors","canvas icon list","canvas icons drawn","canvas nearby objects","canvas monsters","canvas menus","canvas nearest aetheryte","canvas end"})
                Check(map.Contains("MapMark(\""+mark+"\");"),"0.805: "+mark);
            Check(map.Contains("MeasureMap(\"map overlay controls\","),"layout split (the canvas: SlowFrameTests)");
        }),
        ("Reading capture: developer mode only, stopped by the competitive cut, limited",() =>
        {
            var code=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs"));
            Check(code.Contains("CompetitiveGate.Add(framework,OnUpdate)") && code.Contains("CompetitiveGate.Remove(framework,OnUpdate)"),"the frame goes through the gate");
            Check(code.Contains("CompetitiveGate.Cut") && code.Contains("ChatMessageUnhandled-=OnMessage"),"the chat too, with its cleanup");
            Check(code.Contains("if(disposed || !enabled())return;"),"nothing outside developer mode");
            Check(code.Contains("if(count>=limit)return;"),"a limit on the lines");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs")).Contains("()=>configuration.DeveloperMode,log)"),"wired to the setting");
        }),
    ];
}
