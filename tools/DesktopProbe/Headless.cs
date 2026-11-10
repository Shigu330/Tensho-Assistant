using System.Text;
using Lumina.Excel.Sheets;

/// <summary>
/// --headless: every page of the plugin's main window drawn for a few frames, without a window (no renderer: the draw lists are built and
/// dropped). Per page: the exception it throws (none, or its type, message and first plugin frame), the Dalamud members it reached through
/// the "absent" stand-ins, the icons it asked, and whether the draw produced vertices. Writes a markdown table (--out=).
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

    public static int Run(Host host,string[] args)
    {
        if(Program.Option(args,"--step=") is { } steps)return Steps(host,steps);
        var rows=new List<string>();
        var size=new System.Numerics.Vector2(1280,800);
        var clean=0;var failed=0;
        // Frames before measuring, so the constructor-time and first-frame calls (the window, the navigation) are not charged to the first page.
        for(var i=0;i<3;i++)Ui.Frame(host,size,1/60f);
        AbsentProxy.Reached.Clear();
        var common=new HashSet<string>();
        host.SetPage("Home");for(var i=0;i<3;i++)Ui.Frame(host,size,1/60f);
        lock(AbsentProxy.Reached)common.UnionWith(AbsentProxy.Reached);
        foreach(var page in host.Pages)
        {
            lock(AbsentProxy.Reached)AbsentProxy.Reached.Clear();
            var iconsBefore=TextureProviderProxy.Served+TextureProviderProxy.Missing;
            host.SetPage(page);
            if(page=="Weather" && uint.TryParse(Program.Option(args,"--zone=") ?? "155",out var zone))OpenZone(host,zone);
            var errors=new List<string>();var vertices=0;
            for(var frame=0;frame<20;frame++)
            {
                if(Ui.Frame(host,size,1/60f) is { } failure)
                {
                    var at=new System.Diagnostics.StackTrace(failure,false).GetFrames().Select(f=>f.GetMethod()).FirstOrDefault(m=>m?.DeclaringType?.Assembly==host.Plugin);
                    var text=$"{failure.GetType().Name}: {Short(failure.Message)}{(at is null ? "" : $" (in {at.DeclaringType?.Name}.{at.Name})")}";
                    if(!errors.Contains(text))errors.Add(text);
                }
                vertices=Math.Max(vertices,Dalamud.Bindings.ImGui.ImGui.GetDrawData().TotalVtxCount);
                if(frame%5==4)Thread.Sleep(50);// let the pages' off-frame loads (lexicon, searches) finish
            }
            string[] reached;lock(AbsentProxy.Reached)reached=AbsentProxy.Reached.Except(common).Order().ToArray();
            var icons=TextureProviderProxy.Served+TextureProviderProxy.Missing-iconsBefore;
            if(errors.Count==0)clean++;else failed++;
            rows.Add($"| {page} | {(errors.Count==0 ? "yes" : "NO")} | {vertices} | {icons} | {(reached.Length==0 ? "-" : string.Join(", ",reached))} | {(errors.Count==0 ? "-" : string.Join("; ",errors))} |");
            Console.WriteLine($"  {page,-22} {(errors.Count==0 ? "drawn":"THROWS"),-7} vtx {vertices,6} icons {icons,4}  absent: {(reached.Length==0 ? "-" : string.Join(", ",reached))}{(errors.Count==0 ? "" : "  | "+string.Join("; ",errors))}");
        }
        Console.WriteLine($"PASS headless: {clean} pages drawn without exception, {failed} threw (of {clean+failed}); members reached by the window itself on Home: {string.Join(", ",common.Order())}");
        if(Program.Option(args,"--out=") is { } output)
        {
            var text=new StringBuilder();
            text.AppendLine("| Page | Drawn without exception | Vertices (max of 20 frames) | Icons asked | Dalamud members reached (absent stand-ins) | Exception |");
            text.AppendLine("|---|---|---|---|---|---|");
            foreach(var row in rows)text.AppendLine(row);
            text.AppendLine();
            text.AppendLine($"Reached by the window on every page (header, navigation, Home): {string.Join(", ",common.Order())}");
            File.WriteAllText(output,text.ToString());
        }
        return 0;
    }

    private static string Short(string text)=>(text.Length>140 ? text[..140]+"…" : text).Replace("|","/").Replace("\n"," ");
}
