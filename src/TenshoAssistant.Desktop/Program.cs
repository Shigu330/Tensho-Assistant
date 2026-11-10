// X-119 (plan 4 of the desktop application, step 1; X-118's probe moved here): the plugin's own pages in a desktop window, game closed.
// Evidence: docs/evidence/x119-desktop-app.md (X-118's measures: docs/evidence/x118-desktop-probe.md).
// Usage: TenshoAssistant.Desktop [--page=Weather] [--zone=<territory id>] [--field=name=value] [--click=x,y] [--capture=<png>] [--frames=N]
//                                [--lang=French] [--ui=fr] [--sqpack=<folder>] [--plugin=<dll>] [--new-settings]
//        TenshoAssistant.Desktop --headless [--out=<md>]   (all 12 catalogues, every page classified and drawn for 20 frames without a window;
//                                                           exit 0 only if no page throws, blocks or is unclassified; 4 = SKIP, no game data or Dalamud)
//        TenshoAssistant.Desktop --headless --step=A,B | --step=entries   (diagnostic: which MainWindow call blocks, each on a worker with a 5 s limit)
//        TenshoAssistant.Desktop --inventory [--inventory-out=<md>]   (per page: what of Dalamud its draw code can reach, by IL call graph of the built plugin)
//        TenshoAssistant.Desktop --api=Type[:filter] | --il=Type::Method   (Dalamud's metadata, or the IL of one of its methods)
//        TenshoAssistant.Desktop --update-selftest=<release.json> [--update-own=<folder>]   (X-121: the update on a fake local release, Verify.ps1)
//        TenshoAssistant.Desktop --update-probe   (X-121: the real GitHub request for the latest Stable release, read and checked, nothing downloaded)
//        TenshoAssistant.Desktop --print-version   (prints the version and stops)
// X-121: --no-update turns the update off for this run (no request, no hand-over); --no-forward starts this copy even when a newer one is installed.
// PROBE_TRACE=1 prints each step of a frame; PROBE_LOG=1 prints the plugin's log warnings.
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PluginUiTest.Host;
using TenshoAssistant.Desktop;

internal static class Program
{
    private static readonly string AppData=Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    internal static readonly string Hooks=DalamudPaths.Hooks(Environment.GetEnvironmentVariable("DALAMUD_HOME"),AppData);
    internal static readonly string Own=DesktopPaths.Own(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
    internal static readonly string FontFolder=DalamudPaths.Fonts(AppData);
    /// <summary>The folder of the plugin copy the app runs (set once the copy is made; the resolvers read it).</summary>
    internal static string? PluginFolder;
    internal static readonly Version AppVersion=typeof(Program).Assembly.GetName().Version ?? new Version(0,0,0);
    internal static string? Option(string[] args,string prefix)=>args.FirstOrDefault(a=>a.StartsWith(prefix,StringComparison.Ordinal))?[prefix.Length..];
    /// <summary>The plugin's own files the copy carries beside the DLL (as in latest.zip).</summary>
    internal static readonly string[] PluginSiblings=["Raphael.Net.dll","raphael_bindings.dll","TenshoAssistant.json"];
    /// <summary>Exit code of a run that could not happen here (no Dalamud install or no game data): Verify.ps1 prints SKIP, -Release refuses it.</summary>
    internal const int Skip=4;

    /// <summary>The built plugin: --plugin=, else plugin\ next to the exe (the packaged app), else the repository's Release build (development).</summary>
    internal static string? PluginPath(string[] args)
    {
        if(Option(args,"--plugin=") is { } given)return Path.GetFullPath(given);
        var packaged=Path.Combine(AppContext.BaseDirectory,"plugin",DesktopVersionCheck.PluginFile);
        if(File.Exists(packaged))return packaged;
        for(var folder=new DirectoryInfo(AppContext.BaseDirectory);folder is not null;folder=folder.Parent)
        {
            var built=Path.Combine(folder.FullName,"src","TenshoAssistant","bin","x64","Release",DesktopVersionCheck.PluginFile);
            if(File.Exists(built))return built;
        }
        return null;
    }

    [STAThread]
    private static int Main(string[] args)
    {
        if(args.Contains("--print-version")){Console.WriteLine($"TenshoAssistant.Desktop {DesktopVersionCheck.Three(AppVersion)}");return 0;}
        if(Option(args,"--update-selftest=") is { } fakeRelease)return UpdateSelfTest.Run(fakeRelease,Option(args,"--update-own="));
        if(args.Contains("--update-probe"))return UpdateSelfTest.Probe();
        var diagnostic=args.Any(a=>a is "--headless" or "--inventory" || a.StartsWith("--api=",StringComparison.Ordinal) || a.StartsWith("--il=",StringComparison.Ordinal));
        // Started by a double-click: Windows made a console for this process alone; it is let go so only the window stays.
        if(!diagnostic && GetConsoleProcessList(new uint[2],2)==1)FreeConsole();
        // X-121: the copy the user started hands over to the newest installed copy (versions\<version> of the app's own folder), if any.
        if(Updates(args,diagnostic) && DesktopUpdater.Forward(Own,AppVersion,AppContext.BaseDirectory,args))return 0;
        if(!File.Exists(Path.Combine(Hooks,"Dalamud.Bindings.ImGui.dll")) || !File.Exists(Path.Combine(Hooks,"cimgui.dll")))
            return Stop(diagnostic,Skip,$"SKIP desktop: no Dalamud install in {Hooks} (XIVLauncher with Dalamud is needed: the app reads Dalamud's ImGui and Lumina there, nothing is shipped).");
        // Dalamud's assemblies and cimgui.dll (Dalamud's native ImGui build) from the installed Dalamud, never copied (R05); the plugin's
        // own files (raphael_bindings.dll) from the plugin copy (PluginUiTest.Host's DalamudRuntime, X-120).
        DalamudRuntime.Install(Hooks,()=>[PluginFolder]);
        try{return Run(args,diagnostic);}
        catch(Exception error) when(!diagnostic)
        {
            // X-121: an updated copy that fails goes back to the previous one at the next start (its version set aside).
            var back=DesktopUpdater.RollBackAfterFailure(Own,AppContext.BaseDirectory);
            return Stop(false,1,$"Tensho Assistant (desktop) stopped: {(error.InnerException ?? error).GetType().Name}: {(error.InnerException ?? error).Message}"+(back is null ? "" : "\n"+back));
        }
    }

    /// <summary>X-121: the update runs for a window opened by the player, not for a diagnostic, a capture or --no-update.</summary>
    private static bool Updates(string[] args,bool diagnostic)=>!diagnostic && !args.Contains("--no-update") && Option(args,"--capture=") is null;

    /// <summary>A stop before the window: printed for a diagnostic run, shown in a message box for a double-click.</summary>
    private static int Stop(bool console,int code,string text)
    {
        Console.Error.WriteLine(text);
        if(!console)MessageBoxW(IntPtr.Zero,text,"Tensho Assistant",0x10);
        return code;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Run(string[] args,bool diagnostic)
    {
        if(Option(args,"--api=") is { } api)return Api.Dump(api);
        if(Option(args,"--il=") is { } il)return Api.Il(il);
        var plugin=PluginPath(args);
        if(plugin is null || !File.Exists(plugin))return Stop(diagnostic,1,$"FAIL plugin not found: {plugin ?? "plugin\\TenshoAssistant.dll next to the app, or src/TenshoAssistant/bin/x64/Release"} (run .\\scripts\\Verify.ps1 once)");
        if(args.Contains("--inventory"))return Inventory.Run(plugin,Option(args,"--inventory-out="));
        var launcher=DalamudPaths.XivLauncher(AppData);
        var given=Option(args,"--sqpack=");
        var candidates=DalamudPaths.SqpackCandidates(given,ReadText(Path.Combine(launcher,"launcherConfigV3.json")));
        var sqpack=candidates.FirstOrDefault(Directory.Exists);
        if(given is not null && !Directory.Exists(given))return Stop(diagnostic,1,$"FAIL --sqpack={given}: no such folder");
        if(sqpack is null)return Stop(diagnostic,Skip,$"SKIP desktop: no game data found (tried {string.Join("; ",candidates)}); install the game or pass --sqpack=<game\\sqpack folder>.");
        var version=DesktopVersionCheck.Run(AppVersion,
            DesktopVersionCheck.Candidates(launcher,folder=>Directory.Exists(folder) ? Directory.GetDirectories(folder) : [],ReadText(Path.Combine(launcher,"dalamudConfig.json"))),
            File.Exists,path=>{try{return AssemblyName.GetAssemblyName(path).Version;}catch(Exception){return null;}});
        Console.WriteLine($"Tensho Assistant desktop {DesktopVersionCheck.Three(AppVersion)}");
        Console.WriteLine(DesktopVersionCheck.English(version));
        var watch=Stopwatch.StartNew();
        var headless=args.Contains("--headless");
        var (w,h,pixels)=StartUi(headless,out var window);
        Console.WriteLine($"PASS imgui: Dalamud.Bindings.ImGui {typeof(Dalamud.Bindings.ImGui.ImGui).Assembly.GetName().Version}, Dear ImGui {Dalamud.Bindings.ImGui.ImGui.GetVersion()}, font atlas {w}x{h}{(window is null ? "" : ", "+window.GlInfo)}");
        // TEMPORARY (X-119 step 1): Dalamud's font statics block outside the game; the plugin is copied to %LOCALAPPDATA%\TenshoAssistant\Desktop\plugin
        // (never the repository nor the installed plugin) and those calls are pointed at the desktop atlas (PluginUiTest.Host's Rewrite). For
        // Tensho this is replaced by a real seam in the plugin after the post-7.58 release (the fonts read from one property of the plugin's UI
        // components, set by Plugin.cs from UiBuilder and by this app from its atlas; a ratchet test forbids new direct uses). Until then the
        // plugin's source is not changed (frozen until Dalamud is ready for 7.58).
        var copy=Rewrite.Plugin(plugin,Path.Combine(Own,"plugin"),Hooks,AppVersion.ToString(),PluginSiblings);
        PluginFolder=Path.GetDirectoryName(copy);
        Console.WriteLine($"PASS fonts: Dalamud font statics pointed at the desktop atlas in a copy of the plugin ({PluginFolder}): {string.Join(", ",Rewrite.Sites.Select(p=>$"{p.Key} {p.Value} sites"))}");
        var settings=args.Contains("--new-settings") ? null : DesktopPaths.PluginConfig(AppData);
        var host=new Host(copy,sqpack,Option(args,"--lang="),Option(args,"--ui="),settings);
        Console.WriteLine($"PASS window: {host.Window.GetType().FullName} built with its real constructor ({watch.Elapsed.TotalSeconds:0.0} s); settings: {host.SettingsNote}; text {host.TextCode}, game data {host.GameLanguage}");
        var status=new StatusBar(host,version);
        var cache=Path.Combine(Own,"cache");
        if(headless)return Headless.Run(host,status,cache,args);
        if(Updates(args,diagnostic))
        {
            // X-121: at start, in the background (« Automatic update » of update.json turns it off; the button still works).
            status.Updater=new DesktopUpdater(Own,AppVersion,AppContext.BaseDirectory,args,version,new GitHubReleases());
            status.Updater.Check(manual:false);
        }
        return Interactive(host,status,cache,window!,args);
    }

    private static string? ReadText(string path)
    {
        try{return File.Exists(path) ? File.ReadAllText(path) : null;}
        catch(IOException){return null;}
        catch(UnauthorizedAccessException){return null;}
    }

    private static unsafe (int,int,IntPtr) StartUi(bool headless,out Win32Gl? window)
    {
        window=headless ? null : new Win32Gl($"Tensho Assistant {DesktopVersionCheck.Three(AppVersion)}",1280,860,true);
        var (w,h,pixels)=ImGuiHeadless.Start(17f,FontFolder);
        if(window is null){ImGuiHeadless.SetFontTexture(1);GameIcons.Upload=null;}
        else
        {
            ImGuiHeadless.SetFontTexture(Win32Gl.Texture(w,h,(byte*)pixels,false));
            GameIcons.Upload=(width,height,bytes)=>Win32Gl.Texture(width,height,bytes.Pointer,true);
        }
        return (w,h,pixels);
    }

    private static int Interactive(Host host,StatusBar status,string cache,Win32Gl window,string[] args)
    {
        host.HideInGamePages();
        // The catalogues load as in the game: their loaders tick once per frame (the first run builds the cache, later runs read it).
        var loaders=new Queue<Host.LoaderRun>(Host.Loaders.Select(name=>host.StartLoader(name,cache)));
        var total=loaders.Count;
        if(Option(args,"--page=") is { } page)host.SetPage(page);
        var zone=Option(args,"--zone=") is { } zoneText && uint.TryParse(zoneText,out var territory) ? territory : (uint?)null;
        // --field=name=value: a text field of the window (a search box), as if typed.
        if(Option(args,"--field=") is { } field && field.Split('=',2) is [var fieldName,var fieldValue])
            host.Window.GetType().GetField(fieldName,BindingFlags.Instance|BindingFlags.NonPublic)?.SetValue(host.Window,fieldValue);
        var capture=Option(args,"--capture=");
        var frames=int.TryParse(Option(args,"--frames="),out var n) ? n : capture is null ? int.MaxValue : 90;
        var clock=Stopwatch.StartNew();var last=0.0;string? lastError=null;var readyAt=-1;var used=false;
        for(var frame=0;!window.Closed;frame++)
        {
            window.Pump();
            var budget=Stopwatch.StartNew();
            while(loaders.TryPeek(out var loader) && budget.ElapsedMilliseconds<12)
                if(loader.Tick()){loaders.Dequeue();Console.WriteLine("  loader "+loader.Result);}
            status.Loading=loaders.Count==0 ? null : (total-loaders.Count,total);
            if(loaders.Count==0 && readyAt<0)readyAt=frame;
            // --zone: once the weather catalogue is there (the page's own « open a zone » call).
            if(zone is { } wanted && loaders.All(l=>l.Name!="WeatherCatalogLoader")){Headless.OpenZone(host,wanted);zone=null;}
            status.Redirect();
            // --click=x,y: a left click at that point of the window, on frames 30 to 32 (through ImGui's input queue, as the window's own mouse).
            if(Option(args,"--click=") is { } click && click.Split(',') is [var cx,var cy] && frame is >=30 and <=32)
            {
                var io=Dalamud.Bindings.ImGui.ImGui.GetIO();
                io.AddMousePosEvent(float.Parse(cx,CultureInfo.InvariantCulture),float.Parse(cy,CultureInfo.InvariantCulture));
                if(frame==31)io.AddMouseButtonEvent(0,true);
                if(frame==32)io.AddMouseButtonEvent(0,false);
            }
            var now=clock.Elapsed.TotalSeconds;
            var failure=Ui.Frame(host,status,window.Size,(float)(now-last));last=now;
            if(failure is not null && failure.Message!=lastError){lastError=failure.Message;Console.WriteLine($"  page {host.CurrentPage}: {failure.GetType().Name}: {failure.Message}");}
            // X-121: an installed update restarts the app only while the window is untouched and the catalogues are loaded.
            used|=Dalamud.Bindings.ImGui.ImGui.IsAnyItemActive() || Dalamud.Bindings.ImGui.ImGui.GetIO().WantTextInput
                || Dalamud.Bindings.ImGui.ImGui.IsMouseDown(Dalamud.Bindings.ImGui.ImGuiMouseButton.Left) || Dalamud.Bindings.ImGui.ImGui.IsMouseDown(Dalamud.Bindings.ImGui.ImGuiMouseButton.Right);
            status.Updater?.Tick(loaders.Count>0,used);
            if(status.Updater?.ExitRequested==true)break;
            window.Render(Dalamud.Bindings.ImGui.ImGui.GetDrawData(),new Vector4(0.08f,0.08f,0.1f,1));
            // A capture waits for the catalogues and 30 frames more, so the picture shows the page as the player sees it.
            var end=capture is not null ? readyAt>=0 && frame>=Math.Max(frames-1,readyAt+30) : frame>=frames-1;
            if(end && capture is not null)
            {
                Png.Write(capture,(int)window.Size.X,(int)window.Size.Y,window.Capture());
                Console.WriteLine($"PASS capture: {capture} ({window.Size.X}x{window.Size.Y}), page {host.CurrentPage}, icons served {TextureProviderProxy.Served}, missing {TextureProviderProxy.Missing}");
            }
            window.Swap();
            if(capture is not null)Thread.Sleep(16);
            if(end)break;
        }
        window.Dispose();
        return 0;
    }

    [DllImport("kernel32")] private static extern uint GetConsoleProcessList(uint[] list,uint count);
    [DllImport("kernel32")] private static extern bool FreeConsole();
    [DllImport("user32",CharSet=CharSet.Unicode)] private static extern int MessageBoxW(IntPtr owner,string text,string caption,uint type);
}
