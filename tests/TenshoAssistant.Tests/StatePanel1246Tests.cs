using System.Text;
using TenshoAssistant.Desktop;
using TenshoAssistant.Link;

// 0.1246 (X-165, slice 3): Tensho's state in a floating panel (off by default, hidden while all is fine with « only when something is
// wrong ») and in the desktop application, through the local link (an optional part of protocol 3: an older application ignores it).
// Here: the state is bounded before it is sent; the worst line still fits (MaxLine 32 KB); a state out of bounds is dropped, never the
// snapshot; the window's dashboard receives the game's state through the typed contract; the wiring of the panel, settings and texts.
internal static class StatePanel1246Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    private static string Line(LinkState? state)
        => LocalLinkProtocol.Serialize(new LinkSnapshot(LocalLinkProtocol.Version,"0.1246.0",true,false,1,"Zone",1,State:state));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("State panel 1246: the state is bounded before it is sent (level known, heap finite, at most nine lines of 160 characters)",() =>
        {
            Check(LocalLinkProtocol.State(null,10,[]) is null && LocalLinkProtocol.State("Fine",10,[]) is null,"an unknown level is not sent");
            var state=LocalLinkProtocol.State("Watch",210.04,[.. Enumerable.Range(1,12).Select(n=>n==1 ? new string('x',400) : "line "+n)])!;
            Check(state.Overall=="Watch" && state.HeapMegabytes==210.0,"level and heap kept: "+state.HeapMegabytes);
            Check(state.Issues.Count==LocalLinkProtocol.MaxStateLines && state.Issues[0].Length==LocalLinkProtocol.MaxStateLine && state.Issues[1]=="line 2","nine lines, cut: "+state.Issues.Count);
            foreach(var heap in new double?[]{double.NaN,double.PositiveInfinity,-1,2_000_000,null})
                Check(LocalLinkProtocol.State("Ok",heap,[])!.HeapMegabytes is null,"heap not known: "+heap);
            Check(LocalLinkProtocol.ValidState(state),"what is built is valid");
        }),
        ("State panel 1246: the worst line fits in MaxLine and is read whole; a state out of bounds is dropped, the snapshot kept",() =>
        {
            Check(LocalLinkProtocol.Version==3 && LocalLinkProtocol.MaxLine==32768,"same protocol, 32 KB lines");
            var worst=new LinkSnapshot(LocalLinkProtocol.Version,new string('9',LocalLinkProtocol.MaxPlugin),true,false,uint.MaxValue,new string('漢',LocalLinkProtocol.MaxZone),long.MaxValue,
                new LinkLive(new string('漢',LocalLinkProtocol.MaxJob),1000,100_000,100_000,1000,1000),
                [.. LocalLinkProtocol.Categories.Values.Append(LocalLinkProtocol.SettingsCategory).Take(LocalLinkProtocol.MaxChanges).Select(c=>new LinkChange(c,long.MaxValue))],
                [.. Enumerable.Range(1,LocalLinkProtocol.MaxNotices).Select(n=>new LinkNotice(long.MaxValue-n,long.MaxValue,new string('«',LocalLinkProtocol.MaxNoticeText)))],
                [.. Enumerable.Range(1,LocalLinkProtocol.MaxHeld).Select(n=>new LinkHeld(uint.MaxValue-(uint)n,LocalLinkProtocol.MaxHeldCount))],
                new LinkState("Problem",1_048_576,[.. Enumerable.Range(1,LocalLinkProtocol.MaxStateLines).Select(_=>new string('«',LocalLinkProtocol.MaxStateLine))]));
            var line=LocalLinkProtocol.Serialize(worst);
            Check(Encoding.UTF8.GetByteCount(line)<=LocalLinkProtocol.MaxLine,"worst line "+Encoding.UTF8.GetByteCount(line)+" bytes");
            Check(LocalLinkProtocol.TryParse(line,out var read,out var why) && read!.State is { Issues.Count: LocalLinkProtocol.MaxStateLines },"the worst line is read with its state: "+why);
            foreach(var bad in new[]{new LinkState("Fine",1,[]),new LinkState("Ok",-5,[]),new LinkState("Ok",1,[.. Enumerable.Range(0,10).Select(n=>"x")]),
                new LinkState("Ok",1,[new string('x',LocalLinkProtocol.MaxStateLine+1)]),new LinkState("Ok",1,[null!]),new LinkState(null!,1,[]),new LinkState("Ok",1,null!)})
            {
                Check(LocalLinkProtocol.TryParse(Line(bad),out var kept,out var refused),"the snapshot is kept: "+refused);
                Check(kept!.State is null && kept.LoggedIn && kept.Zone=="Zone","the bad state alone is dropped");
            }
            Check(LocalLinkProtocol.TryParse("{\"Protocol\":3,\"Plugin\":\"0.1245.0\",\"LoggedIn\":true,\"Paused\":false,\"Territory\":1,\"Zone\":\"\",\"Sequence\":1}",out var older,out _) && older!.State is null,"a plugin of before sends none");
            Check(LocalLinkProtocol.TryParse(Line(new LinkState("Ok",210,[])),out var fine,out _) && fine!.State is { Overall: "Ok", HeapMegabytes: 210, Issues.Count: 0 },"a fine state crosses");
        }),
        ("State panel 1246: the window's dashboard receives the game's state through the typed contract; a plugin without it is said and harmless",() =>
        {
            var received=new List<string>();
            var map=new Dictionary<string,Delegate>{[nameof(DesktopSurface.GameState)]=new Action<string?,double?,string[]>((overall,heap,issues)=>received.Add($"{overall} {heap} {issues.Length}"))};
            var surface=DesktopSurface.FromMap(map,out var missing);
            surface.GameState("Watch",210,["a","b"]);surface.GameState(null,null,[]);
            Check(received.SequenceEqual(["Watch 210 2","  0"]),"called through: "+string.Join("|",received));
            Check(!missing.Contains(nameof(DesktopSurface.GameState)),"not said missing when given");
            var none=DesktopSurface.FromMap(new Dictionary<string,Delegate>(),out var absent);
            none.GameState("Ok",1,[]);
            Check(absent.Contains(nameof(DesktopSurface.GameState)),"said missing when not given");
            var host=Read("src/TenshoAssistant.Desktop/Host.cs");
            Check(host.Contains("public void GameState(TenshoAssistant.Link.LinkState? state)=>Surface.GameState(state?.Overall,state?.HeapMegabytes,state is null ? [] : [.. state.Issues]);"),"the host passes the state on");
            var bar=Read("src/TenshoAssistant.Desktop/StatusBar.cs");
            Check(bar.Contains("host.GameState(current is { Paused: false } ? current.State : null);"),"the status bar gives it each frame, none when paused or not linked");
            Check(bar.Contains("+Live(linked)+State(linked),linked is { Paused: false, State.Overall: not \"Ok\" }));"),"and says it after the live states, as a warning when not fine");
            var window=Read("src/TenshoAssistant/MainWindow.DesktopData.cs");
            Check(window.Contains("        GameState=DesktopGameState,\n"),"the window fills the entry");
            var dashboard=Read("src/TenshoAssistant/MainWindow.Dashboard.cs");
            Check(dashboard.Contains("if(inDesktopApp){DrawDesktopGameState();return;}"),"in the application the dashboard shows the game's state, not the application's own measures");
        }),
        ("State panel 1246: panel off by default, owned with the other panels, hidden while all is fine with the option; measures read only when wanted; link sends the state",() =>
        {
            var defaults=new TenshoAssistant.Configuration();
            Check(!defaults.StatePanel,"off by default");
            Check(Read("src/TenshoAssistant/Configuration.cs").Contains("\"StateServerBar\", \"StateServerBarOnlyProblems\", \"StatePanel\", "),"reset with the other settings");
            Check(Read("src/TenshoAssistant/MainWindow.TrackerWindow.cs").Contains("new(this,OccultPanelDefinition),new(this,StatePanelDefinition)];"),"a panel window like the others (owned, cut, removed)");
            var dashboard=Read("src/TenshoAssistant/MainWindow.Dashboard.cs");
            Check(dashboard.Contains("()=>configuration.StatePanel && !inDesktopApp,StatePanelVisible,DrawStatePanelContents,Alpha:0.7f);"),"its setting");
            Check(dashboard.Contains("return StateBar.Shown(DashboardState?.Overall,configuration.StateServerBarOnlyProblems);"),"the same rule as the bar entry");
            Check(dashboard.Contains("private bool DashboardWanted=>configuration.StateServerBar || configuration.StatePanel || configuration.DesktopLink;"),"read only while one of its readers is on");
            Check(dashboard.Contains("if(ImGui.SmallButton(localization.Get(\"Dashboard.Details\")+\"##TenshoStateDetails\"))OpenSettings();\n        Tip(\"Dashboard.DetailsTip\");"),"Details opens the settings");
            Check(Read("src/TenshoAssistant/Link/LocalLinkFeed.Dalamud.cs").Contains("loggedIn ? ReadHeld():null,ReadState()));"),"the link sends it");
            Check(Read("src/TenshoAssistant/Plugin.Desktop.cs").Contains("{Held=mainWindow.AlarmHeldItems,State=mainWindow.DashboardLinkState};"),"from the window's snapshot");
            var advanced=Read("src/TenshoAssistant/MainWindow.Advanced.cs");
            Check(advanced.Contains("BoolSetting(\"Advanced.StatePanel\",") && advanced.Contains("if(configuration.StateServerBar || configuration.StatePanel || inDesktopApp)"),"its setting, the option and the dashboard under either");
            var en=Read("src/TenshoAssistant/Localization/Languages/en.json");var fr=Read("src/TenshoAssistant/Localization/Languages/fr.json");
            foreach(var key in new[]{"StatePanel","StatePanelTip","Details","DetailsTip","DesktopGame","DesktopNoState","DesktopAllFine"})
                Check(en.Contains($"    \"{key}\": ") && fr.Contains($"    \"{key}\": "),"text: "+key);
            Check(en.Contains("these settings.\\nDefault: off.\"") && fr.Contains("ces paramètres.\\nPar défaut : désactivé.\""),"default said");
        }),
    ];
}
