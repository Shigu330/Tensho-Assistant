using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using PluginUiTest.Host;
using TenshoAssistant.Desktop;

/// <summary>
/// X-271 (R37): --references=&lt;folder&gt; [--compare=&lt;folder&gt;]: the reference images of the pages, drawn by the real plugin in a hidden
/// window (never shown: the game may be in front) with new settings (no saved data of the player: the same picture on every machine and
/// every day, the empty states of the pages that hold the player's data), in the order of <see cref="ReferencePlan"/>. Each picture is taken once the page is
/// still (two frames alike, at most <see cref="MaxFrames"/>). With --compare, each picture is compared with the one of the same name there
/// (ImageCompare: SSIM and changed area, a diff picture when blocks changed). report.json lists them; the tolerance is decided by the
/// caller (the agent's publication). Exit 0 when every picture was taken, 1 when a page threw (its picture is still written).
/// </summary>
internal static class References
{
    public const int MaxFrames=150;
    /// <summary>X-285: a page that still shows a waiting text is drawn on (its load ends between frames), this long at most.</summary>
    public static readonly TimeSpan WaitLimit=TimeSpan.FromSeconds(20);
    private const int MinFrames=10;
    /// <summary>Three captures alike in a row: the page is still (its off-frame work, such as the garden's crossings, done).</summary>
    private const int StillCaptures=3;
    /// <summary>The version the plugin copy and the status bar show in the pictures.</summary>
    public static readonly Version Stamp=new(0,0,0,0);
    /// <summary>X-282: the instant the plugin copy reads instead of the computer's clock (Rewrite.Plugin's clock, HostClock): the countdowns,
    /// Eorzea's hour, the forecasts and the trial garden's dates are the same on every day.</summary>
    public static readonly DateTimeOffset Clock=new(2026,11,11,10,0,0,TimeSpan.Zero);

    public static int Run(Host host,StatusBar status,string cache,Win32Gl window,string folder,string? compare)
    {
        Directory.CreateDirectory(folder);
        host.HideInGamePages();
        status.Pictures=true;
        // The header's clocks (the computer's and Eorzea's) change each second: off in the pictures (in memory, never saved).
        foreach(var clock in new[]{"ShowHeaderClock","ShowHeaderEorzeaTime"})
            if(!host.SetSetting(clock,false))Console.WriteLine($"  references: the setting {clock} could not be reached");
        var plan=ReferencePlan.Plan(host.Pages);
        var rows=new List<Dictionary<string,object?>>();
        var thrown=0;
        var loaders=Host.Loaders.Select(name=>host.StartLoader(name,cache)).ToList();
        var watch=System.Diagnostics.Stopwatch.StartNew();
        foreach(var shot in plan.Where(shot=>shot.Setup=="loading"))
        {
            // The catalogues have not ticked yet: the page as the player sees it in the first second.
            status.Loading=(0,loaders.Count);
            thrown+=Take(host,status,window,shot,folder,compare,rows);
        }
        status.Loading=null;
        foreach(var loader in loaders)
            while(!loader.Tick()){}
        foreach(var loader in loaders)Console.WriteLine("  loader "+loader.Result);
        if(loaders.Any(loader=>loader.Result!.Contains("(error:",StringComparison.Ordinal))){Console.WriteLine("FAIL references: a catalogue did not load");return 1;}
        foreach(var shot in plan.Where(shot=>shot.Setup!="loading"))
        {
            var beds=host.GetSetting("GardenBeds");var detailed=host.GetSetting("GardenDetailedView");var link=status.Link;
            try
            {
                if(shot.Setup.StartsWith("garden-",StringComparison.Ordinal))
                {
                    if(!Headless.GardenTrial(host) || !host.SetSetting("GardenDetailedView",shot.Setup=="garden-detailed"))
                        Console.WriteLine($"  {shot.Id}: the trial patch could not be given");
                }
                if(shot.Setup=="dashboard")Headless.DashboardTrial(host,status);
                // X-284 M2: the page's sheets cannot be read for this picture (its error state), then are given back.
                if(shot.Setup=="error" && !host.SheetsFail(true))Console.WriteLine($"  {shot.Id}: the sheets could not be made to fail");
                if(shot.Page=="Weather")Headless.OpenZone(host,155);
                thrown+=Take(host,status,window,shot,folder,compare,rows);
            }
            finally
            {
                if(shot.Setup.StartsWith("garden-",StringComparison.Ordinal)){host.SetSetting("GardenBeds",beds);host.SetSetting("GardenDetailedView",detailed ?? false);}
                if(shot.Setup=="error")host.SheetsFail(false);
                if(shot.Setup=="dashboard"){status.Link=link;host.GameMeasures(null,Environment.TickCount64);host.GameState(null);}
            }
        }
        var report=Path.Combine(folder,"report.json");
        File.WriteAllText(report,JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,$"{(thrown==0 ? "PASS" : "FAIL")} references: {rows.Count} pictures in {folder} ({watch.Elapsed.TotalSeconds:0.0} s), {thrown} page(s) threw; report {report}"));
        return thrown==0 ? 0 : 1;
    }

    /// <summary>One picture: the page drawn until still, written, compared; 1 when the page threw.</summary>
    private static int Take(Host host,StatusBar status,Win32Gl window,ReferencePlan.Shot shot,string folder,string? compare,List<Dictionary<string,object?>> rows)
    {
        if(!host.SetSetting("ThemeMode",shot.Theme))Console.WriteLine($"  {shot.Id}: the theme could not be set");
        window.Resize(shot.Width,shot.Height);
        // X-285: the keys of the texts asked by the last frame drawn, the one pictured: an error key means the picture shows an error
        // state, a waiting key (outside the loading pictures) a wait that never ends.
        var asked=new HashSet<string>(StringComparer.Ordinal);
        host.ListenKeys(key=>asked.Add(key));
        host.SetPage(shot.Page);
        byte[]? last=null;byte[]? picture=null;var frames=0;var still=false;var alike=0;Exception? error=null;
        // The garden's icons wait for their texture before giving way to the name (UI/IconWait): as the headless run, 70 frames.
        var least=shot.Setup.StartsWith("garden-",StringComparison.Ordinal) ? 70 : MinFrames;
        var display=new Vector2(shot.Width,shot.Height);
        var waited=System.Diagnostics.Stopwatch.StartNew();var waiting=false;
        for(;(frames<MaxFrames || (waiting && waited.Elapsed<WaitLimit)) && !still;frames++)
        {
            window.Pump();
            asked.Clear();
            error??=Ui.Frame(host,status,display,1/60f);
            window.Render(Dalamud.Bindings.ImGui.ImGui.GetDrawData(),new Vector4(0.08f,0.08f,0.1f,1));
            if(frames>=least-StillCaptures)
            {
                picture=window.Capture();
                alike=last is not null && last.AsSpan().SequenceEqual(picture) ? alike+1 : 0;
                waiting=shot.Setup!="loading" && asked.Any(ReferenceErrors.IsWaiting);
                still=frames>=least-1 && alike>=StillCaptures-1 && !waiting;
                last=picture;
            }
            window.Swap();
            // The pages' off-frame loads (icons, the lexicon, searches, the garden's crossings) finish between frames.
            Thread.Sleep(frames<5 ? 0 : 15);
        }
        host.ListenKeys(null);
        var shown=ReferenceErrors.ShownFor(asked,shot.Setup);
        host.SetSetting("ThemeMode",0);
        var width=(int)window.Size.X;var height=(int)window.Size.Y;
        var file=Path.Combine(folder,shot.Id+".png");
        Png.Write(file,width,height,picture!);
        var row=new Dictionary<string,object?>{["id"]=shot.Id,["page"]=shot.Page,["theme"]=shot.Theme,["width"]=width,["height"]=height,["setup"]=shot.Setup,
            ["frames"]=frames,["still"]=still,["error"]=error is null ? null : $"{error.GetType().Name}: {Headless.Short(error.Message)}",["error_keys"]=shown};
        var line=new StringBuilder($"  {shot.Id,-26} {width}x{height} {(still ? "still" : "moving")} after {frames,3} frames");
        if(compare is not null && File.Exists(Path.Combine(compare,shot.Id+".png")))
        {
            var (w,h,before)=Png.Read(Path.Combine(compare,shot.Id+".png"));
            var result=ImageCompare.Compare(w,h,before,width,height,picture!);
            row["reference"]=true;row["ssim"]=Math.Round(result.Ssim,5);row["changed"]=Math.Round(result.Changed,5);row["same_size"]=result.SameSize;
            row["box"]=new[]{result.Left,result.Top,result.Right,result.Bottom};
            if(result.SameSize && result.ChangedBlocks>0)Png.Write(Path.Combine(folder,shot.Id+".diff.png"),width,height,ImageCompare.DiffPicture(width,height,before,picture!));
            line.Append(string.Create(CultureInfo.InvariantCulture,$"  ssim {result.Ssim:0.0000} changed {result.Changed*100:0.00} %{(result.SameSize ? "" : " (size differs)")}"));
        }
        else row["reference"]=false;
        if(error is not null)line.Append("  THROWS "+row["error"]);
        if(shown.Length>0)line.Append("  SHOWS AN ERROR "+string.Join(", ",shown));
        Console.WriteLine(line.ToString());
        rows.Add(row);
        return error is null ? 0 : 1;
    }
}
