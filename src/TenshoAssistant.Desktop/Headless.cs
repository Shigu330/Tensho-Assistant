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
        host.Window.GetType().GetMethod("OpenWeather",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.Invoke(host.Window,[territory,name]);
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
        report.Write(failed==0);
        return failed==0 ? 0 : 1;
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
