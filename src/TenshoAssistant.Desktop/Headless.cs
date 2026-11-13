using System.Text;
using Lumina.Excel.Sheets;
using PluginUiTest.Host;
using TenshoAssistant.Desktop;

/// <summary>
/// --headless (R33: Verify.ps1 runs it, a failure fails Verify, publish.sh and the release): every page of the plugin's main window drawn
/// for 20 frames without a window (no renderer: the draw lists are built and dropped), after the 12 catalogue loaders. Every page must be
/// classified in DesktopPages (an unclassified page fails the run); the pages that stay in the game are drawn too (they must not throw). Per
/// page: its kind, the exception it throws (none, or its type, message and first plugin frame), the Dalamud members it reached through the
/// "absent" stand-ins, the icons it asked, whether the draw produced vertices, and the status bar's texts (a missing text fails). A watchdog
/// fails the run in bounded time when a loader tick or a frame blocks. Exit code: 0 all drawn, 1 an exception, an unclassified page, a
/// missing text or a loader error, 3 a block. Writes a markdown report (--out=, default %LOCALAPPDATA%\TenshoAssistant\Desktop\desktop-headless.md).
/// </summary>
internal static class Headless
{
    public static void OpenZone(Host host,uint territory)
    {
        var name=host.Data.Excel.GetSheet<TerritoryType>().TryGetRow(territory,out var row) ? row.PlaceName.ValueNullable?.Name.ExtractText() ?? "" : "";
        host.Surface.OpenWeather(territory,name);
    }

    /// <summary>--step=A,B: calls these MainWindow methods one by one inside a frame, each on a worker thread with a 5 s limit (finds a blocking call).</summary>
    public static int Steps(Host host,string steps)
    {
        var io=Dalamud.Bindings.ImGui.ImGui.GetIO();io.DisplaySize=new(1280,800);
        Dalamud.Bindings.ImGui.ImGui.NewFrame();
        Dalamud.Bindings.ImGui.ImGui.Begin("probe");
        if(steps=="entries")
        {
            // Each page entry's label and visibility delegate, on a worker thread with a limit.
            var entries=(System.Collections.IEnumerable)host.Window.GetType().GetProperty("PageEntries",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(host.Window)!;
            foreach(var entry in entries)
                foreach(var part in new[]{"Label","Visible"})
                    if(entry.GetType().GetProperty(part)!.GetValue(entry) is Delegate call)
                    {
                        var thread=new Thread(()=>{try{call.DynamicInvoke();}catch{}}){IsBackground=true};thread.Start();
                        if(!thread.Join(3000)){Console.WriteLine($"  entry {entry.GetType().GetProperty("Page")!.GetValue(entry)} {part}: BLOCKED ({call.Method.Name})");return 1;}
                    }
            Console.WriteLine("  entries: all labels and visibility delegates returned");
            return 0;
        }
        foreach(var name in steps.Split(','))
        {
            var method=host.Window.GetType().GetMethod(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public,[]);
            if(method is null){Console.WriteLine($"  step {name}: no such method");continue;}
            Exception? error=null;
            var thread=new Thread(()=>{try{method.Invoke(host.Window,null);}catch(Exception e){error=e.InnerException ?? e;}}){IsBackground=true};
            thread.Start();
            Console.WriteLine(thread.Join(5000) ? $"  step {name}: returned{(error is null ? "" : " with "+error.GetType().Name+": "+error.Message+" "+error.StackTrace?.Split((char)10).FirstOrDefault())}" : $"  step {name}: BLOCKED (over 5 s)");
            if(thread.IsAlive)return 1;
        }
        return 0;
    }

    // Watchdog: what the worker is doing and since when; a step over its limit fails the run (the blocked worker cannot be stopped).
    private static readonly object Gate=new();
    private static string doing="start";
    private static DateTime since=DateTime.UtcNow;
    private static TimeSpan limit=TimeSpan.FromSeconds(60);
    private static void Beat(string what,TimeSpan allowed){lock(Gate){doing=what;since=DateTime.UtcNow;limit=allowed;}}

    public static int Run(Host host,StatusBar status,string cache,string[] args)
    {
        if(Program.Option(args,"--step=") is { } steps)return Steps(host,steps);
        var output=Program.Option(args,"--out=") ?? Path.Combine(Program.Own,"desktop-headless.md");
        var report=new Report(host,status,output);
        // R33: a page added to the plugin and not classified stops the run at once.
        var unclassified=DesktopPages.Unclassified(host.Pages);
        if(unclassified.Count>0)
        {
            report.Fail(DesktopPages.UnclassifiedMessage(unclassified));
            return 1;
        }
        // X-151 (0.1157): every member of the plugin the application reaches by name exists, with its parameters (DesktopContract).
        var broken=DesktopContract.Missing(host.Plugin);
        if(broken.Count>0){report.Fail("contract: the plugin lacks "+string.Join("; ",broken));return 1;}
        Console.WriteLine($"PASS contract: the {DesktopContract.Members.Length} members of the plugin the app reaches by name exist");
        // X-151 (0.1238): every entry of the typed contract is given by the plugin, with its type (DesktopSurface).
        if(host.SurfaceMissing.Count>0){report.Fail("typed contract: the plugin does not give "+string.Join("; ",host.SurfaceMissing));return 1;}
        Console.WriteLine("PASS typed contract: every entry of DesktopSurface given by the plugin, with its type");
        var code=-1;
        var worker=new Thread(()=>{try{code=Body(host,status,cache,args,report);}catch(Exception error){report.Fail($"{error.GetType().Name}: {error.Message}");code=1;}}){IsBackground=true,Name="headless"};
        worker.Start();
        while(!worker.Join(1000))
        {
            string what;DateTime start;TimeSpan allowed;
            lock(Gate){what=doing;start=since;allowed=limit;}
            if(DateTime.UtcNow-start>allowed)
            {
                report.Fail($"BLOCKED: {what} did not return within {allowed.TotalSeconds:0} s.");
                Console.Out.Flush();
                Environment.Exit(3);
            }
        }
        return code;
    }

    private static int Body(Host host,StatusBar status,string cache,string[] args,Report report)
    {
        foreach(var name in Host.Loaders)
        {
            Beat($"loader {name} (constructor)",TimeSpan.FromSeconds(120));
            var run=host.StartLoader(name,cache);
            while(true)
            {
                Beat($"loader {name}, tick {run.Ticks}",TimeSpan.FromSeconds(120));
                if(run.Tick())break;
            }
            report.Loaders.Add(run.Result!);
            Console.WriteLine("  loader "+run.Result);
            if(run.Result!.Contains("(error:",StringComparison.Ordinal)){report.Fail("loader error: "+run.Result);return 1;}
        }
        foreach(var service in new[]{"TenshoAssistant.Maps.MapCatalogService","TenshoAssistant.Gathering.WeatherCatalogService"})
        {
            var state=host.Service(service)?.GetType().GetProperty("State")?.GetValue(host.Service(service));
            var published=state?.GetType().GetProperty("Catalog")?.GetValue(state) is not null;
            Console.WriteLine($"  {service.Split('.')[^1]}: {(published ? "catalogue published" : "NO CATALOGUE (error: "+state?.GetType().GetProperty("Error")?.GetValue(state)+")")}");
            if(!published){report.Fail($"{service}: no catalogue");return 1;}
        }
        var size=new System.Numerics.Vector2(1280,820);
        // X-01 (0.1050): the window's JIT warm-up (Diagnostics/JitWarmup) finished before the pages are timed, as in the game where a page is
        // opened seconds after loading; --cold stops it first, to time the pages as they were before it (the same build, before and after).
        if(host.Window.GetType().GetField("jitWarmup",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.GetValue(host.Window) is IDisposable warmup)
        {
            if(args.Contains("--cold")){warmup.Dispose();report.Warmup="stopped (--cold)";}
            else
            {
                Beat("JIT warm-up",TimeSpan.FromSeconds(120));
                var waited=warmup.GetType().GetMethod("Wait")?.Invoke(warmup,[TimeSpan.FromSeconds(110)]) is true;
                string Read(string name)=>Convert.ToString(warmup.GetType().GetProperty(name)?.GetValue(warmup),System.Globalization.CultureInfo.InvariantCulture) ?? "?";
                report.Warmup=waited ? $"{Read("Prepared")} methods compiled ahead in {Read("Elapsed")}, {Read("Skipped")} skipped" : "not finished in 110 s";
            }
            // Its memory cost: the process working set at this point (compare with --cold).
            GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
            report.Warmup+=$"; working set {Environment.WorkingSet/1048576} MB after a full collection";
            Console.WriteLine("  JIT warm-up: "+report.Warmup);
        }
        // Frames before measuring, so the constructor-time and first-frame calls (the window, the navigation) are not charged to the first page.
        Beat("first frames",TimeSpan.FromSeconds(60));
        for(var i=0;i<3;i++)Ui.Frame(host,status,size,1/60f);
        AbsentProxy.Reached.Clear();
        var common=new HashSet<string>();
        host.SetPage("Home");for(var i=0;i<3;i++)Ui.Frame(host,status,size,1/60f);
        lock(AbsentProxy.Reached)common.UnionWith(AbsentProxy.Reached);
        report.Common=common.Order().ToArray();
        var failed=0;
        // --jit-names: the methods compiled during each page's first frame, counted by declaring type (runtime JIT events, this thread):
        // what the warm-up does not cover yet. Diagnostic only, off by default.
        using var jitNames=args.Contains("--jit-names") ? new JitNames() : null;
        foreach(var page in host.Pages)
        {
            lock(AbsentProxy.Reached)AbsentProxy.Reached.Clear();
            var iconsBefore=TextureProviderProxy.Served+TextureProviderProxy.Missing;
            host.SetPage(page);
            if(page=="Weather" && uint.TryParse(Program.Option(args,"--zone=") ?? "155",out var zone))OpenZone(host,zone);
            var errors=new List<string>();var vertices=0;
            // X-01 (0.1050): the first frame of a page and its settled frames timed, as the plugin's "Selftest draw" lines do in the game
            // (first, settled): what a page costs on opening is measured here without the game, before and after a change.
            var times=new double[20];
            double jitMs=0;long jitMethods=0;
            var jitBefore=System.Runtime.JitInfo.GetCompilationTime(true);var methodsBefore=System.Runtime.JitInfo.GetCompiledMethodCount(true);
            for(var frame=0;frame<20;frame++)
            {
                Beat($"page {page}, frame {frame}",TimeSpan.FromSeconds(30));
                var frameStart=System.Diagnostics.Stopwatch.GetTimestamp();
                if(frame==0)jitNames?.Begin();
                var failed1=Ui.Frame(host,status,size,1/60f);
                if(frame==0 && jitNames is not null && jitNames.End() is { Count:>0 } compiled)
                    Console.WriteLine($"    jit {page}: "+string.Join(", ",compiled.GroupBy(name=>name).OrderByDescending(group=>group.Count()).Take(12).Select(group=>$"{group.Key} {group.Count()}")));
                times[frame]=System.Diagnostics.Stopwatch.GetElapsedTime(frameStart).TotalMilliseconds;
                if(frame==0)
                {
                    // The part of the first frame spent compiling the page's code (JIT, once per process): what is left is the page's own work.
                    jitMs=(System.Runtime.JitInfo.GetCompilationTime(true)-jitBefore).TotalMilliseconds;
                    jitMethods=System.Runtime.JitInfo.GetCompiledMethodCount(true)-methodsBefore;
                }
                if(failed1 is { } failure)
                {
                    var at=new System.Diagnostics.StackTrace(failure,false).GetFrames().Select(f=>f.GetMethod()).FirstOrDefault(m=>m?.DeclaringType?.Assembly==host.Plugin);
                    var text=$"{failure.GetType().Name}: {Short(failure.Message)}{(at is null ? "" : $" (in {at.DeclaringType?.Name}.{at.Name})")}";
                    if(!errors.Contains(text))errors.Add(text);
                }
                vertices=Math.Max(vertices,Dalamud.Bindings.ImGui.ImGui.GetDrawData().TotalVtxCount);
                if(frame%5==4)Thread.Sleep(50);// let the pages' off-frame loads (lexicon, searches) finish
            }
            // The status bar's texts for this page: a key missing from the language files shows « [Desktop.…] ».
            var lines=status.Lines();
            foreach(var (line,_) in lines)
                if(line.Contains("[Desktop.",StringComparison.Ordinal))errors.Add("status bar text missing: "+Short(line));
            string[] reached;lock(AbsentProxy.Reached)reached=AbsentProxy.Reached.Except(common).Order().ToArray();
            var icons=TextureProviderProxy.Served+TextureProviderProxy.Missing-iconsBefore;
            if(errors.Count>0)failed++;
            var kind=DesktopPages.Kind(page)!.Value;
            var settled=Settled(times);
            report.Rows.Add(new(page,kind,errors.Count==0,vertices,icons,reached,errors,DesktopPages.LiveKey(page) is { } live ? host.Text(live) : "-",times[0],settled,jitMs,jitMethods));
            Console.WriteLine($"  {page,-22} {kind,-9} {(errors.Count==0 ? "drawn":"THROWS"),-7} vtx {vertices,6} icons {icons,4} {string.Create(System.Globalization.CultureInfo.InvariantCulture,$"first {times[0],7:0.0} ms (jit {jitMs,6:0.0} ms, {jitMethods,4} methods) settled {settled,5:0.0} ms")}  absent: {(reached.Length==0 ? "-" : string.Join(", ",reached))}{(errors.Count==0 ? "" : "  | "+string.Join("; ",errors))}");
        }
        // X-119 step 2 (0.1081): the status bar's texts of the local link (linked, paused, no character, a zone), on a page that needs it.
        host.SetPage("Home");
        const int protocol=TenshoAssistant.Link.LocalLinkProtocol.Version;
        foreach(var snapshot in new TenshoAssistant.Link.LinkSnapshot[]{new(protocol,"0.0.0",true,false,132,"Test",1),new(protocol,"0.0.0",false,false,0,"",2),new(protocol,"0.0.0",false,true,0,"",3),
                    new(protocol,"0.0.0",true,false,132,"Test",4,new("MIN",100,650,700,20,140))})
        {
            status.Link=()=>snapshot;
            foreach(var (line,_) in status.Lines())
                if(line.Contains("[Desktop.",StringComparison.Ordinal)){report.Fail("status bar link text missing: "+Short(line));failed++;}
        }
        status.Link=null;
        failed+=FoldedNavigation(host,status,report);
        failed+=GardenViews(host,status,report);
        failed+=DashboardView(host,status,report);
        failed+=ReadOnlyCheck(host,report);
        report.Write(failed==0);
        return failed==0 ? 0 : 1;
    }

    /// <summary>
    /// X-154 (0.1252): the shell in a narrow window (500 px): the navigation folds behind its button, the page is drawn in one column, and the
    /// same navigation is drawn in the popup once the button is clicked. Then « always shown » in the narrow window and « always folded » in
    /// the wide one. The number of failures (an exception, or a popup that does not open or close).
    /// </summary>
    private static int FoldedNavigation(Host host,StatusBar status,Report report)
    {
        var failed=0;
        void Fail(string why){report.Fail("folded navigation: "+why);failed++;}
        var narrow=new System.Numerics.Vector2(500,820);var wide=new System.Numerics.Vector2(1280,820);
        var io=Dalamud.Bindings.ImGui.ImGui.GetIO();
        host.SetPage("Home");
        Exception? thrown=null;
        void Frames(System.Numerics.Vector2 size,int count,string what)
        {
            for(var i=0;i<count;i++){Beat("folded navigation, "+what,TimeSpan.FromSeconds(30));thrown??=Ui.Frame(host,status,size,1/60f);}
        }
        try
        {
            if(!host.SetSetting("NavigationMode",0)){Fail("the setting NavigationMode could not be reached");return failed;}
            Frames(narrow,3,"narrow window");
            // A click on the button (first item of the window, under the title bar): through ImGui's input queue, as the window's own mouse.
            var before=PopupOpen();
            io.AddMousePosEvent(20,Dalamud.Bindings.ImGui.ImGui.GetFrameHeight()+14);Frames(narrow,2,"pointer on the button");
            io.AddMouseButtonEvent(0,true);Frames(narrow,1,"button pressed");
            io.AddMouseButtonEvent(0,false);Frames(narrow,3,"popup open");
            var opened=PopupOpen();
            // A click beside the popup (the right edge of the narrow window) closes it.
            io.AddMousePosEvent(narrow.X-12,narrow.Y/2);Frames(narrow,2,"pointer beside the popup");
            io.AddMouseButtonEvent(0,true);Frames(narrow,1,"click beside the popup");
            io.AddMouseButtonEvent(0,false);Frames(narrow,2,"popup closed");
            if(before || !opened)Fail($"the popup did not open on a click of the button (open before: {before}, after: {opened})");
            if(PopupOpen())Fail("the popup did not close on a click beside it");
            host.SetSetting("NavigationMode",1);Frames(narrow,3,"always shown, narrow window");
            host.SetSetting("NavigationMode",2);Frames(wide,3,"always folded, wide window");
            if(thrown is not null)Fail($"{thrown.GetType().Name}: {Short(thrown.Message)}");
        }
        finally{host.SetSetting("NavigationMode",0);io.AddMousePosEvent(-1,-1);Frames(wide,2,"back to the wide window");}
        Console.WriteLine(failed==0 ? "  folded navigation: drawn in a 500 px window, popup opened by its button and closed by a click beside it, both forced modes drawn" : $"  folded navigation: {failed} failure(s)");
        return failed;
    }

    /// <summary>
    /// X-178 (0.1257): the garden journal drawn in its two views (icons, then the detailed text) with a trial patch: eight beds given only to
    /// this run's copy of the settings in memory (never written, never in the application's window): growing, tending later, tending due,
    /// ripe, a seed whose name matches no item, and an empty bed. The icon view must ask for icons. The number of failures.
    /// </summary>
    private static int GardenViews(Host host,StatusBar status,Report report)
    {
        var failed=0;
        void Fail(string why){report.Fail("garden views: "+why);failed++;}
        var before=host.GetSetting("GardenBeds");var detailedBefore=host.GetSetting("GardenDetailedView");
        var size=new System.Numerics.Vector2(1280,820);
        try
        {
            if(!GardenTrial(host)){Fail("the trial patch could not be given (GardenBed, or the setting GardenBeds)");return failed;}
            host.SetPage("Garden");
            foreach(var detailed in new[]{false,true})
            {
                if(!host.SetSetting("GardenDetailedView",detailed)){Fail("the setting GardenDetailedView could not be reached");break;}
                var icons=TextureProviderProxy.Served+TextureProviderProxy.Missing;
                Exception? thrown=null;var vertices=0;
                // 70 frames in icons: past the wait after which a missing icon gives way to the name (UI/IconWait).
                for(var frame=0;frame<(detailed ? 6 : 70);frame++)
                {
                    Beat($"garden {(detailed ? "detailed" : "icon")} view, frame {frame}",TimeSpan.FromSeconds(30));
                    thrown??=Ui.Frame(host,status,size,1/60f);
                    vertices=Math.Max(vertices,Dalamud.Bindings.ImGui.ImGui.GetDrawData().TotalVtxCount);
                }
                icons=TextureProviderProxy.Served+TextureProviderProxy.Missing-icons;
                if(thrown is not null)Fail($"{(detailed ? "detailed" : "icon")} view: {thrown.GetType().Name}: {Short(thrown.Message)}");
                if(!detailed && icons<=0)Fail("the icon view asked for no icon");
                Console.WriteLine($"  garden {(detailed ? "detailed" : "icon"),-8} view: {(thrown is null ? "drawn" : "THROWS")}, vtx {vertices}, icons {icons}");
            }
        }
        finally{host.SetSetting("GardenBeds",before);host.SetSetting("GardenDetailedView",detailedBefore ?? false);host.SetPage("Home");}
        return failed;
    }

    /// <summary>
    /// X-264 (0.1271): the dashboard's gauges and curves drawn from a trial hour of measures (given as the link would give them, never
    /// written), on the page that holds the dashboard: it must draw, and its cost by frame is said beside the same page without measures.
    /// The number of failures.
    /// </summary>
    private static int DashboardView(Host host,StatusBar status,Report report)
    {
        var failed=0;
        var size=new System.Numerics.Vector2(1280,820);
        var linkBefore=status.Link;
        try
        {
            host.SetPage("Diagnostics");
            double Time(string what,out Exception? thrown,out int vertices)
            {
                thrown=null;vertices=0;var times=new List<double>();
                for(var frame=0;frame<40;frame++)
                {
                    Beat($"dashboard {what}, frame {frame}",TimeSpan.FromSeconds(30));
                    var start=System.Diagnostics.Stopwatch.GetTimestamp();
                    thrown??=Ui.Frame(host,status,size,1/60f);
                    if(frame>=10)times.Add(System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds);
                    vertices=Math.Max(vertices,Dalamud.Bindings.ImGui.ImGui.GetDrawData().TotalVtxCount);
                }
                times.Sort();
                return times[times.Count/2];
            }
            var without=Time("without measures",out var first,out var plain);
            DashboardTrial(host,status);
            var with=Time("with a trial hour",out var second,out var drawn);
            foreach(var thrown in new[]{first,second})
                if(thrown is not null){report.Fail($"dashboard view: {thrown.GetType().Name}: {Short(thrown.Message)}");failed++;}
            if(drawn<=plain){report.Fail($"dashboard view: the trial hour drew nothing more ({drawn} vertices against {plain})");failed++;}
            Console.WriteLine(string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"  dashboard gauges and curves: {(failed==0 ? "drawn" : "FAILED")}, vtx {drawn} (page without measures {plain}), frame {with:0.00} ms (without {without:0.00} ms)"));
        }
        finally{status.Link=linkBefore;host.GameMeasures(null,Environment.TickCount64);host.GameState(null);host.SetPage("Home");}
        return failed;
    }

    /// <summary>
    /// X-264 (0.1271): a trial hour of the game's measures, given as the link gives them (the headless run, or a capture with
    /// --dashboard-trial): 360 points ten seconds apart ending now, with a slow page now and then, a few points without the page's time
    /// (the curve breaks there), requests in bursts, a panel off near the end; then a linked game whose state is « to look at ».
    /// </summary>
    // The window keeps 360 points ten seconds apart (the plugin's HealthHistory, not a type of the application).
    private const int TrialPoints=360;
    private const long TrialStepMs=10_000;
    public static void DashboardTrial(Host host,StatusBar status)
    {
        var now=Environment.TickCount64;
        static TenshoAssistant.Link.LinkMeasures At(int point)
            =>TenshoAssistant.Link.LocalLinkProtocol.Measures(300+120*Math.Sin(point/40.0)+point*0.25,point is >=100 and <110 ? null : 0.6+0.3*Math.Sin(point/7.0)+(point%90==45 ? 5.5 : 0),
                0.5+0.25*Math.Sin(point/11.0),1,point/120,point/3+(point>200 ? (point-200)*2 : 0),0,point>300 ? 1 : 0,0);
        for(var point=0;point<TrialPoints;point++)
            host.GameMeasures(At(point),now-(TrialPoints-1-point)*TrialStepMs);
        var state=new TenshoAssistant.Link.LinkState("Watch",512,["Trial: a panel was turned off after an error."],At(TrialPoints-1));
        var snapshot=new TenshoAssistant.Link.LinkSnapshot(TenshoAssistant.Link.LocalLinkProtocol.Version,"trial",true,false,0,"",1,State:state);
        status.Link=()=>snapshot;
        // The headless run draws without polling the status bar: the state is given here too (a window run gets it again each frame).
        host.GameState(state);
    }

    /// <summary>
    /// X-178 (0.1257): a trial patch given to the copy of the settings in memory (the headless run, or a capture with --garden-trial; never
    /// written: the application saves nothing): three patches at one place; in the first, seven beds: growing, tending later, tending due,
    /// ripe, two more, and a seed whose name matches no item (bed 7 stays empty); a few beds in the second and the third. False when the plugin's types cannot be reached.
    /// </summary>
    public static bool GardenTrial(Host host)
    {
        var bedType=host.Plugin.GetType("TenshoAssistant.Reminders.GardenBed");
        if(bedType is null)return false;
        var items=host.Data.Excel.GetSheet<Item>().Where(row=>row.RowId>=7715 && row.Icon!=0 && !row.Name.IsEmpty).Take(6).ToArray();
        var beds=(System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(bedType))!;
        var now=DateTimeOffset.UtcNow;
        // (patch, bed number, hours since planting, growing hours, tending hours, a seed the game knows). X-178 (0.1263): three patches at
        // the same place, as at a large house: the first with every state, the second and the third with a few beds.
        var trial=new (int Patch,int Number,double Planted,int Grow,int Tend,bool Known)[]{(1,1,1,48,0,true),(1,2,2,72,24,true),(1,3,30,72,24,true),(1,4,60,48,24,true),(1,5,10,120,48,true),(1,6,0.2,24,0,true),(1,8,5,24,0,false),
            (2,1,3,48,24,true),(2,2,40,72,24,true),(2,7,80,48,0,true),(3,5,12,72,24,true),(3,6,50,48,24,true)};
        for(var i=0;i<trial.Length;i++)
        {
            var (patch,number,planted,grow,tend,real)=trial[i];
            var bed=Activator.CreateInstance(bedType)!;
            void Set(string name,object value)=>bedType.GetProperty(name)!.SetValue(bed,value);
            Set("Place",$"Trial 1-1 · patch {patch}, bed {number}");
            // The last one: a name no item bears and no noted item (the icon view writes the name).
            var known=real && items.Length>0 ? items[i%items.Length] : (Item?)null;
            Set("Seed",known?.Name.ExtractText() ?? "No such seed");
            Set("SeedId",known?.RowId ?? 0u);
            Set("PlantedAt",now.AddHours(-planted));Set("GrowHours",grow);Set("TendHours",tend);
            beds.Add(bed);
        }
        return host.SetSetting("GardenBeds",beds);
    }

    private static bool PopupOpen()=>Dalamud.Bindings.ImGui.ImGui.IsPopupOpen("",Dalamud.Bindings.ImGui.ImGuiPopupFlags.AnyPopupId|Dalamud.Bindings.ImGui.ImGuiPopupFlags.AnyPopupLevel);

    /// <summary>
    /// X-180 (0.1168, night review N1; R28 behaviour test): the window built as the application builds it saves nothing: the stores built on a
    /// copy are read-only and leave its files as they are, the window's save refuses (DesktopReadOnlyException), and an alarm added there is
    /// taken back with « not saved » instead of « added ». The number of failures.
    /// </summary>
    private static int ReadOnlyCheck(Host host,Report report)
    {
        const System.Reflection.BindingFlags Flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;
        var failed=0;
        void Fail(string why){report.Fail("read-only in the app: "+why);failed++;}
        var folder=Path.Combine(Path.GetTempPath(),"Tensho-Desktop-ReadOnly-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var tools=Path.Combine(folder,"MyTools.json");
            File.WriteAllText(tools,"{\"Schema\":1,\"Tools\":[]}");
            var before=File.ReadAllBytes(tools);
            var window=host.Window;var type=window.GetType();
            var note=host.UseData(folder,"my-tools");
            var store=type.GetProperty("MyToolsStore",Flags)?.GetValue(window);
            if(store?.GetType().GetProperty("ReadOnly")?.GetValue(store) is not true)Fail("my tools are not read-only on the copy ("+note+")");
            if(type.GetField("save",Flags)?.GetValue(window) is not System.Action save)Fail("the window has no save");
            else
            {
                var refused=false;
                try{save();}catch(Exception error) when(error.GetType().Name=="DesktopReadOnlyException"){refused=true;}
                if(!refused)Fail("the window's save did not refuse");
            }
            var configuration=type.GetField("configuration",Flags)?.GetValue(window);
            var alarms=configuration?.GetType().GetProperty("Alarms");
            var alarmType=host.Plugin.GetType("TenshoAssistant.Alarms.TenshoAlarm");
            if(configuration is null || alarms is null || alarmType is null || type.GetMethod("AddAlarms",Flags) is not { } add)Fail("the alarms could not be reached");
            else
            {
                var count=(alarms.GetValue(configuration) as System.Collections.IList)?.Count ?? 0;
                var added=(System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(alarmType))!;
                var alarm=Activator.CreateInstance(alarmType)!;alarmType.GetProperty("Label")?.SetValue(alarm,"Read-only check");added.Add(alarm);
                add.Invoke(window,[added]);
                var after=(alarms.GetValue(configuration) as System.Collections.IList)?.Count ?? 0;
                if(after!=count)Fail($"an alarm added in the app stayed ({count} before, {after} after)");
                if(type.GetField("alarmStatus",Flags)?.GetValue(window) is not string said || said!=host.Text("Desktop.NotSaved"))Fail("the alarms do not say « not saved »");
            }
            if(!File.ReadAllBytes(tools).AsSpan().SequenceEqual(before))Fail("MyTools.json of the copy changed");
            Console.WriteLine(failed==0 ? "  read-only in the app: nothing saved (my tools, the window's save, an alarm)" : $"  read-only in the app: {failed} failure(s)");
        }
        finally
        {
            try{Directory.Delete(folder,true);}catch(Exception error) when(error is IOException or UnauthorizedAccessException){}
        }
        return failed;
    }

    internal static string Short(string text)=>(text.Length>140 ? text[..140]+"…" : text).Replace("|","/").Replace("\n"," ");

    /// <summary>Runtime JIT events (MethodJittingStarted) between Begin and End (the frame; other threads' few compilations included), as "namespace.type" of each compiled method.</summary>
    private sealed class JitNames : System.Diagnostics.Tracing.EventListener
    {
        private readonly List<string> names=[];
        private int thread=-1;
        public void Begin(){lock(names){names.Clear();thread=Environment.CurrentManagedThreadId;}}
        // The runtime's events reach the listener a little later, on its own thread: the window is closed after a short wait.
        public List<string> End(){Thread.Sleep(300);lock(names){thread=-1;return [..names];}}
        protected override void OnEventSourceCreated(System.Diagnostics.Tracing.EventSource source)
        {
            if(source.Name=="Microsoft-Windows-DotNETRuntime")EnableEvents(source,System.Diagnostics.Tracing.EventLevel.Verbose,(System.Diagnostics.Tracing.EventKeywords)0x10);
        }
        protected override void OnEventWritten(System.Diagnostics.Tracing.EventWrittenEventArgs data)
        {
            if(data.EventName!="MethodJittingStarted_V1" || data.Payload is null || data.PayloadNames is null)return;
            var space=data.Payload[data.PayloadNames.IndexOf("MethodNamespace")] as string ?? "?";
            lock(names)if(thread!=-1)names.Add(space.Length>70 ? space[..70] : space);
        }
    }

    /// <summary>Median of the last ten frames of a page (the frame after the off-frame loads had time to finish).</summary>
    internal static double Settled(double[] times)
    {
        var last=times.Skip(Math.Max(0,times.Length-10)).Order().ToArray();
        return last.Length==0 ? 0 : last[last.Length/2];
    }

    private sealed record Row(string Page,DesktopPageKind Kind,bool Drawn,int Vertices,int Icons,string[] Reached,List<string> Errors,string Live,double FirstMs,double SettledMs,double JitMs,long JitMethods);

    /// <summary>The markdown report of the run (the evidence of X-119 quotes it) and the summary line Verify.ps1 reads.</summary>
    private sealed class Report(Host host,StatusBar status,string output)
    {
        public readonly List<string> Loaders=[];
        public readonly List<Row> Rows=[];
        public string[] Common=[];
        public string Warmup="-";
        private readonly object gate=new();
        private bool written;

        public void Fail(string why)
        {
            Console.WriteLine("FAIL headless: "+why);
            Write(false,why);
        }

        public void Write(bool passed,string? why=null)
        {
            lock(gate)
            {
                if(written)return;
                written=true;
                var drawn=Rows.Count(r=>r.Drawn);
                var summary=passed
                    ? $"PASS headless: {drawn} pages drawn without exception ({Rows.Count(r=>r.Kind==DesktopPageKind.AsIs)} as is, {Rows.Count(r=>r.Kind==DesktopPageKind.NeedsLink)} need the link, {Rows.Count(r=>r.Kind==DesktopPageKind.InGame)} in game), 0 threw, 0 blocked, every page classified"
                    : $"FAIL headless: {why ?? $"{Rows.Count-drawn} page(s) threw"} ({drawn} of {Rows.Count} drawn so far)";
                if(passed)Console.WriteLine(summary);
                var text=new StringBuilder();
                text.AppendLine("# Desktop application, headless run");
                text.AppendLine();
                text.AppendLine($"- App {DesktopVersionCheck.Three(Program.AppVersion)}, plugin copy `{Path.GetFileName(Path.GetDirectoryName(host.Plugin.Location))}`, run {DateTime.Now:yyyy-MM-dd HH:mm}");
                text.AppendLine($"- {status.VersionEnglish}");
                text.AppendLine($"- Text language `{host.TextCode}`, game data `{host.GameLanguage}`");
                text.AppendLine($"- Result: **{summary}**");
                text.AppendLine($"- JIT warm-up of the window: {Warmup}");
                text.AppendLine();
                text.AppendLine("## Catalogue loaders");
                text.AppendLine();
                foreach(var loader in Loaders)text.AppendLine("- "+loader);
                text.AppendLine();
                text.AppendLine("## Pages (20 frames each)");
                text.AppendLine();
                text.AppendLine("| Page | Kind | Drawn without exception | Vertices (max) | Icons asked | First frame (ms) | Of which compiling (ms, methods) | Settled (ms) | Live parts named by the status bar | Dalamud members reached (absent stand-ins) | Exception |");
                text.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
                foreach(var row in Rows)
                    text.AppendLine($"| {row.Page} | {row.Kind} | {(row.Drawn ? "yes" : "NO")} | {row.Vertices} | {row.Icons} | {row.FirstMs.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)} | {row.JitMs.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)}, {row.JitMethods} | {row.SettledMs.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)} | {row.Live} | {(row.Reached.Length==0 ? "-" : string.Join(", ",row.Reached))} | {(row.Errors.Count==0 ? "-" : string.Join("; ",row.Errors))} |");
                text.AppendLine();
                text.AppendLine($"Reached by the window on every page (header, navigation, Home): {(Common.Length==0 ? "-" : string.Join(", ",Common))}");
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
                    File.WriteAllText(output,text.ToString());
                    Console.WriteLine("  report: "+Path.GetFullPath(output));
                }
                catch(Exception error){Console.WriteLine($"  report not written ({error.Message})");}
            }
        }
    }
}
