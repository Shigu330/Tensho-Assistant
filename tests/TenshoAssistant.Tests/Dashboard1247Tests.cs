using TenshoAssistant.Diagnostics;

// 0.1247 (X-165, slice 4): measures Tensho took without showing them, now on the dashboard: the panels turned off after an error (worth a
// look), the saves in the background that failed (a problem), the load's time, and the requests of the market prices (Universalis) and of
// Discord counted with the others (the host only: a webhook's path is a secret).
internal static class Dashboard1247Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    private static HealthInputs Fine()=>new(200L*1048576,150L*1048576,"Home",0.4,2.5,[],null,null,false,false,null,
        new Dictionary<string,int>(),36,0,0,true,[],0,null,1890);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Dashboard 1247: panels turned off are worth a look, failed background saves a problem, the load's time said; unknown stays unknown",() =>
        {
            var (overall,lines)=HealthBoard.Evaluate(Fine());
            Check(overall==HealthLevel.Ok && lines.Count==11,"fine: "+overall+" "+lines.Count);
            Check(lines.Any(line=>line.Key=="Dashboard.Load" && (long)line.Values[0]==1890) && lines.Any(line=>line.Key=="Dashboard.PanelsOk"),"load and panels said");
            var off=HealthBoard.Evaluate(Fine() with { PanelsOff=["GpTickBar","WearBar"] });
            Check(off.Overall==HealthLevel.Watch && off.Lines.Single(line=>line.Key=="Dashboard.PanelsOff").Values is [2,"GpTickBar, WearBar"],"panels off: worth a look, named");
            var writes=HealthBoard.Evaluate(Fine() with { WritesFailed=3,LastWriteFailure="voyages not saved" });
            Check(writes.Overall==HealthLevel.Problem && writes.Lines.Single(line=>line.Key=="Dashboard.WritesFailed").Values is [3,"voyages not saved"],"background saves: a problem, counted, last said");
            Check(HealthBoard.Evaluate(Fine() with { WritesFailed=1,SaveError="disk full" }).Lines.Any(line=>line.Key=="Dashboard.SaveError"),"the settings save error is said first");
            var unknown=HealthBoard.Evaluate(Fine() with { LoadMilliseconds=null,PanelsOff=null }).Lines;
            Check(unknown.Any(line=>line.Key=="Dashboard.LoadUnknown") && unknown.Any(line=>line.Key=="Dashboard.PanelsOk"),"load unknown said, never 0");
            Check(lines.Count(line=>line.Level==HealthLevel.Ok)>=3 && 11-3<=TenshoAssistant.Link.LocalLinkProtocol.MaxStateLines,"the lines that can be not fine fit in the link's state");
        }),
        ("Dashboard 1247: the market prices and Discord are counted by host, never by path",() =>
        {
            var before=TenshoAssistant.Community.BoundedHttp.Sent.GetValueOrDefault("count-1247.test");
            TenshoAssistant.Community.BoundedHttp.Count("count-1247.test");TenshoAssistant.Community.BoundedHttp.Count("count-1247.test");
            Check(TenshoAssistant.Community.BoundedHttp.Sent["count-1247.test"]==before+2,"counted");
            Parallel.For(0,1000,_=>TenshoAssistant.Community.BoundedHttp.Count("parallel-1247.test"));
            Check(TenshoAssistant.Community.BoundedHttp.Sent["parallel-1247.test"]==1000,"counted from many threads at once");
            var market=Read("src/TenshoAssistant/Market/MarketService.cs");
            Check(market.Split("Community.BoundedHttp.Count(\"universalis.app\");").Length-1==3,"the three Universalis requests of the market service");
            var board=Read("src/TenshoAssistant/Market/MarketBoardService.cs");
            Check(board.Contains("Community.BoundedHttp.Count(\"universalis.app\");") && board.Contains("Community.BoundedHttp.Count(new Uri(url).Host);"),"the two of the market board");
            Check(Read("src/TenshoAssistant/Alarms/DiscordNotifier.cs").Contains("TenshoAssistant.Community.BoundedHttp.Count(request.RequestUri?.Host ?? \"?\");"),"Discord: the host only");
            var en=Read("src/TenshoAssistant/Localization/Languages/en.json");
            Check(en.Contains("\"Requests\": \"Requests sent this session: {0} ({1}).\""),"no more « not counted yet »");
        }),
        ("Dashboard 1247: wiring: panels and overlays noted where they turn off, background writers report, the load's time set once",() =>
        {
            Check(Read("src/TenshoAssistant/MainWindow.TrackerWindow.cs").Contains("internal void PanelFailed(string name,Exception error){log.Error(error,") && Read("src/TenshoAssistant/MainWindow.TrackerWindow.cs").Contains("NotePanelOff(name);}"),"panel windows and attached panels");
            var plugin=Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Contains("(perhaps after a game update).\",name);mainWindow.NotePanelOff(name);}"),"the plugin's overlays");
            Check(plugin.Split("new BackgroundWriter((error,message)=>{log.Warning(error,message);mainWindow.NoteWriteFailed(message);});").Length-1==2,"both background writers");
            Check(plugin.Contains("SayLoad(\"created\");\n            mainWindow.LoadMilliseconds="),"the load's time");
            var dashboard=Read("src/TenshoAssistant/MainWindow.Dashboard.cs");
            Check(dashboard.Contains("internal void NoteWriteFailed(string message){Interlocked.Increment(ref writesFailed);Volatile.Write(ref lastWriteFailure,message);}"),"a writer's thread: counted atomically");
            Check(dashboard.Contains("[.. panelsOff],Volatile.Read(ref writesFailed),Volatile.Read(ref lastWriteFailure),LoadMilliseconds);"),"read by the dashboard");
            var fr=Read("src/TenshoAssistant/Localization/Languages/fr.json");
            foreach(var key in new[]{"Load","LoadUnknown","PanelsOk","PanelsOff","WritesFailed"})
                Check(Read("src/TenshoAssistant/Localization/Languages/en.json").Contains($"    \"{key}\": ") && fr.Contains($"    \"{key}\": "),"text: "+key);
        }),
    ];
}
