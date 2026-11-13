using TenshoAssistant.Desktop;
using TenshoAssistant.Link;

// 0.1271 (X-264, slice 3): the desktop application draws the game's gauges and every curve. The link carries the dashboard's measures
// inside Tensho's state (an optional part of protocol 3, bounded); the application keeps its own hour of them. Behaviour: what crosses the
// pipe, what an end of another version does with it, what is dropped when out of bounds.
internal static class DashboardLink1271Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static string Line(LinkState? state)=>LocalLinkProtocol.Serialize(new LinkSnapshot(LocalLinkProtocol.Version,"0.1271.0",true,false,129,"Limsa",7,State:state));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Dashboard link 1271: the measures cross the pipe as they are, an unknown stays unknown",() =>
        {
            var measures=LocalLinkProtocol.Measures(412.3456,0.71,null,null,2,37,0,1,0);
            Check(measures is { MemoryMb:412.346,PageMs:0.71,FrameAvgMs:null,FrameP95Ms:null,SlowFrames:2,Requests:37,WritesFailed:0,PanelsOff:1,Unavailable:0 },"built, rounded, unknown kept: "+measures);
            var state=LocalLinkProtocol.State("Watch",512,["a panel is off"],measures);
            Check(LocalLinkProtocol.TryParse(Line(state),out var read,out var error) && read!.State is { Overall:"Watch",Measures: { } back } && back==measures,"round trip: "+error);
            Check(Line(state).Contains("\"FrameAvgMs\":null"),"an unknown is sent as null, never 0");
        }),
        ("Dashboard link 1271: values out of bounds are not sent, and dropped when received, the state kept",() =>
        {
            var built=LocalLinkProtocol.Measures(double.NaN,-1,double.PositiveInfinity,2_000_000,-5,int.MaxValue,3,4,5);
            Check(built is { MemoryMb:null,PageMs:null,FrameAvgMs:null,FrameP95Ms:null,SlowFrames:0,Requests:LocalLinkProtocol.MaxCounter,WritesFailed:3 },"not a number, negative, endless, too large: unknown; counters kept in range: "+built);
            Check(LocalLinkProtocol.ValidMeasures(built),"what the builder gives is always within bounds");
            foreach(var bad in new[]{new LinkMeasures(-1,0,0,0,0,0,0,0,0),new LinkMeasures(0,LocalLinkProtocol.MaxMeasure+1,0,0,0,0,0,0,0),new LinkMeasures(0,0,0,0,-1,0,0,0,0),new LinkMeasures(0,0,0,0,0,0,0,0,LocalLinkProtocol.MaxCounter+1)})
            {
                Check(!LocalLinkProtocol.ValidMeasures(bad),"out of bounds: "+bad);
                // Written by another program on the pipe (never by the plugin's builder): the measures are dropped, the state and the snapshot kept.
                var line=Line(new LinkState("Ok",210,[],bad));
                Check(LocalLinkProtocol.TryParse(line,out var read,out var error) && read!.State is { Overall:"Ok",HeapMegabytes:210,Measures:null },"measures dropped, state kept: "+error);
                Check(LocalLinkProtocol.State("Ok",210,[],bad) is { Measures:null },"and never sent by the plugin");
            }
        }),
        ("Dashboard link 1271: ends of another version: a state without measures is read, measures are ignored by an older application",() =>
        {
            // A plugin of before 0.1271: no « Measures » in the state.
            var old=Line(new LinkState("Ok",210,[])).Replace(",\"Measures\":null","");
            Check(!old.Contains("Measures"),"the line of an older plugin has no measures");
            var parsed=LocalLinkProtocol.TryParse(old,out var read,out var error);
            Check(parsed && read!.State is { Overall:"Ok",Measures:null },"older plugin: state read, no measures: "+error);
            // An application of before 0.1271 reads the line with its own record, which has no such field: the field is ignored (unknown
            // members are skipped by the reader), the rest is read.
            var line=Line(LocalLinkProtocol.State("Ok",210,[],LocalLinkProtocol.Measures(1,2,3,4,5,6,7,8,9)));
            var before=System.Text.Json.JsonSerializer.Deserialize<OldSnapshot>(line);
            Check(before is { Protocol:LocalLinkProtocol.Version,State: { Overall:"Ok",HeapMegabytes:210 } },"older application: the state it knows is read");
            Check(System.Text.Encoding.UTF8.GetByteCount(Line(new LinkState("Problem",1_048_576,[.. Enumerable.Range(1,LocalLinkProtocol.MaxStateLines).Select(_=>new string('«',LocalLinkProtocol.MaxStateLine))],
                LocalLinkProtocol.Measures(LocalLinkProtocol.MaxMeasure,LocalLinkProtocol.MaxMeasure,LocalLinkProtocol.MaxMeasure,LocalLinkProtocol.MaxMeasure,LocalLinkProtocol.MaxCounter,LocalLinkProtocol.MaxCounter,LocalLinkProtocol.MaxCounter,LocalLinkProtocol.MaxCounter,LocalLinkProtocol.MaxCounter))))<LocalLinkProtocol.MaxLine,
                "the worst state with the worst measures stays under the longest line");
        }),
        ("Dashboard link 1271: the application's contract carries the measures with its clock, and stands in harmlessly without them",() =>
        {
            var received=new List<string>();
            var map=new Dictionary<string,Delegate>{[nameof(DesktopSurface.GameMeasures)]=new Action<double?[]?,int[]?,long>((numbers,counters,at)=>received.Add($"{numbers?.Length} {counters?.Length} {at}"))};
            var surface=DesktopSurface.FromMap(map,out var missing);
            surface.GameMeasures([1,null,3,4],[1,2,3,4,5],42);surface.GameMeasures(null,null,43);
            Check(received.SequenceEqual(["4 5 42","  43"]) && !missing.Contains(nameof(DesktopSurface.GameMeasures)),"called through: "+string.Join(" | ",received));
            var none=DesktopSurface.FromMap(new Dictionary<string,Delegate>(),out var absent);
            none.GameMeasures([1],[1],1);
            Check(absent.Contains(nameof(DesktopSurface.GameMeasures)),"a plugin of before: said missing, nothing drawn, no crash");
        }),
        ("Dashboard link 1271: wiring: the last point of the game's history goes out; the application keeps its hour and draws every curve",() =>
        {
            var page=Read("src/TenshoAssistant/MainWindow.Dashboard.cs");
            Check(page.Contains("Volatile.Write(ref dashboardMeasures,Link.LocalLinkProtocol.Measures(") && page.Contains("state.Issues,Volatile.Read(ref dashboardMeasures)) : null;"),"written at each point on the draw thread, read by the link's feed");
            Check(page.Contains("desktopHistory.Add(new HealthSample(at,level,") && page.Contains("if(numbers is not { Length: >=4 } || counters is not { Length: >=5 }){desktopMeasures=null;return;}"),"the application keeps a point by its own clock; arrays too short: no measures");
            Check(page.Contains("desktopHistory,allCurves:true);") && page.Contains("dashboardHistory,allCurves:false);"),"every curve in the application, one on a click in game");
            Check(page.Contains("if(desktopMeasures is not var (numbers,counters)){ImGui.TextDisabled(localization.Get(\"Dashboard.DesktopNoMeasures\"));return;}"),"no measures: said");
            Check(Read("src/TenshoAssistant/MainWindow.DesktopData.cs").Contains("        GameMeasures=DesktopGameMeasures,\n"),"the window fills the entry");
            Check(Read("src/TenshoAssistant.Desktop/StatusBar.cs").Contains("host.GameMeasures(current is { Paused: false } ? current.State?.Measures : null,Environment.TickCount64);"),"given each frame, none when paused or not linked");
            var headless=Read("src/TenshoAssistant.Desktop/Headless.cs");
            Check(headless.Contains("failed+=DashboardView(host,status,report);") && headless.Contains("if(drawn<=plain)"),"the headless run draws the trial hour and fails when nothing more is drawn");
            foreach(var file in new[]{"en","fr"})
            {
                var text=Read($"src/TenshoAssistant/Localization/Languages/{file}.json");
                foreach(var key in new[]{"GaugePageTipDesktop","DesktopNoMeasures"})Check(text.Contains("\n    \""+key+"\": \""),$"{file}: Dashboard.{key}");
            }
        }),
    ];

    // The snapshot as an application of before 0.1271 declares it (its state has no measures).
    private sealed record OldState(string Overall,double? HeapMegabytes,IReadOnlyList<string> Issues);
    private sealed record OldSnapshot(int Protocol,string Plugin,bool LoggedIn,bool Paused,uint Territory,string Zone,long Sequence,OldState? State=null);
}
