// X-118 (plan 4 of the desktop application, step 0): the plugin's own pages in a desktop window, without Dalamud and without the game running.
// Result and measures: docs/evidence/x118-desktop-probe.md.
// Usage: DesktopProbe [--page=Weather] [--zone=<territory id>] [--field=name=value] [--click=x,y] [--capture=<png>] [--frames=N]
//                     [--loaders=all] [--lang=French] [--ui=fr] [--sqpack=<folder>] [--plugin=<dll>]
//        DesktopProbe --headless [--loaders=all] [--out=<md>]   (every page drawn for 20 frames without a window: exceptions, icons, Dalamud members reached)
//        DesktopProbe --headless --step=A,B | --step=entries   (diagnostic: which MainWindow call blocks, each on a worker with a 5 s limit)
//        DesktopProbe --inventory [--inventory-out=<md>]   (per page: what of Dalamud its draw code can reach, by IL call graph of the built plugin)
//        DesktopProbe --api=Type[:filter] | --il=Type::Method   (Dalamud's metadata, or the IL of one of its methods)
// PROBE_TRACE=1 prints each step of a frame; PROBE_LOG=1 prints the plugin's log warnings.
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Lumina.Data;

internal static class Program
{
    internal static readonly string Hooks=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"XIVLauncher","addon","Hooks","dev");
    internal static string? Option(string[] args,string prefix)=>args.FirstOrDefault(a=>a.StartsWith(prefix,StringComparison.Ordinal))?[prefix.Length..];
    internal static string PluginPath(string[] args)=>Option(args,"--plugin=") ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..","..","src","TenshoAssistant","bin","x64","Release","TenshoAssistant.dll"));
    internal const string DefaultSqpack=@"C:\Program Files (x86)\Steam\steamapps\common\FINAL FANTASY XIV Online\game\sqpack";

    [STAThread]
    private static int Main(string[] args)
    {
        var pluginDir=Path.GetDirectoryName(PluginPath(args))!;
        AssemblyLoadContext.Default.Resolving+=(context,name)=>
        {
            foreach(var dir in new[]{Hooks,pluginDir})
            {
                var path=Path.Combine(dir,name.Name+".dll");
                if(File.Exists(path))return context.LoadFromAssemblyPath(path);
            }
            return null;
        };
        // cimgui.dll (Dalamud's native ImGui build) is loaded from the installed Dalamud, never copied into the repository.
        AssemblyLoadContext.Default.ResolvingUnmanagedDll+=(_,name)=>
        {
            var path=Path.Combine(Hooks,name.EndsWith(".dll",StringComparison.OrdinalIgnoreCase) ? name : name+".dll");
            return File.Exists(path) ? NativeLibrary.Load(path) : IntPtr.Zero;
        };
        return Run(args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Run(string[] args)
    {
        if(Option(args,"--api=") is { } api)return Api.Dump(api);
        if(Option(args,"--il=") is { } il)return Api.Il(il);
        var plugin=PluginPath(args);
        if(!File.Exists(plugin)){Console.Error.WriteLine($"FAIL plugin not built: {plugin} (run .\\scripts\\Verify.ps1 once)");return 1;}
        if(args.Contains("--inventory"))return Inventory.Run(plugin,Option(args,"--inventory-out="));
        var sqpack=Option(args,"--sqpack=") ?? DefaultSqpack;
        var language=Enum.Parse<Language>(Option(args,"--lang=") ?? "French",true);
        var cache=Path.Combine(Path.GetTempPath(),"TenshoDesktopProbe","cache");
        var watch=Stopwatch.StartNew();
        var (w,h,pixels)=StartUi(args.Contains("--headless"),out var window);
        Console.WriteLine($"PASS imgui: Dalamud.Bindings.ImGui {typeof(Dalamud.Bindings.ImGui.ImGui).Assembly.GetName().Version}, Dear ImGui {Dalamud.Bindings.ImGui.ImGui.GetVersion()}, font atlas {w}x{h}{(window is null ? "" : ", "+window.GlInfo)}");
        var rewritten=Rewrite.Plugin(plugin);
        Console.WriteLine($"PASS fonts: Dalamud font statics pointed at the desktop atlas in a temporary copy of the plugin: {string.Join(", ",Rewrite.Sites.Select(p=>$"{p.Key} {p.Value} sites"))}");
        var host=new Host(rewritten,sqpack,language,Option(args,"--ui=") ?? "fr",Path.GetDirectoryName(plugin)!);
        Console.WriteLine($"PASS window: {host.Window.GetType().FullName} built with its real constructor ({watch.Elapsed.TotalSeconds:0.0} s)");
        // The plugin's catalogue loaders that need only (service, IDataManager, IFramework, IPluginLog[, DataCache]) (Plugin.cs); --loaders=all runs
        // them all (items, recipes and gathering build their cache the first time: minutes), else the two the probe's pages need.
        string[] all=["MapCatalogLoader","WeatherCatalogLoader","DutyCatalogLoader","GpCatalogLoader","CraftActionCatalogLoader","FashionHintLoader",
            "ItemUseCatalogLoader","ItemCatalogLoader","RecipeCatalogLoader","GatheringCatalogLoader","NpcVendorLoader","MapLocalIndexLoader"];
        foreach(var loader in Option(args,"--loaders=")=="all" ? all : all[..2])
            Console.WriteLine("  loader "+host.RunLoader(loader,cache));
        foreach(var service in new[]{"TenshoAssistant.Maps.MapCatalogService","TenshoAssistant.Gathering.WeatherCatalogService"})
        {
            var state=host.Service(service)?.GetType().GetProperty("State")?.GetValue(host.Service(service));
            Console.WriteLine($"  {service.Split('.')[^1]}: {(state?.GetType().GetProperty("Catalog")?.GetValue(state) is null ? "NO CATALOG (error: "+state?.GetType().GetProperty("Error")?.GetValue(state)+")" : "catalogue published")}");
        }
        if(args.Contains("--headless"))return Headless.Run(host,args);
        return Interactive(host,window!,args);
    }

    private static unsafe (int,int,IntPtr) StartUi(bool headless,out Win32Gl? window)
    {
        window=headless ? null : new Win32Gl("Tensho Assistant (desktop probe, X-118)",1280,820,true);
        var (w,h,pixels)=Ui.Start(17f);
        if(window is null){Ui.SetFontTexture(1);Host.Upload=null;}
        else
        {
            Ui.SetFontTexture(Win32Gl.Texture(w,h,(byte*)pixels,false));
            Host.Upload=(width,height,bytes)=>Win32Gl.Texture(width,height,bytes.Pointer,true);
        }
        return (w,h,pixels);
    }

    private static int Interactive(Host host,Win32Gl window,string[] args)
    {
        if(Option(args,"--page=") is { } page)host.SetPage(page);
        if(Option(args,"--zone=") is { } zone && uint.TryParse(zone,out var territory))Headless.OpenZone(host,territory);
        // --field=name=value: a text field of the window (a search box), as if typed.
        if(Option(args,"--field=") is { } field && field.Split('=',2) is [var fieldName,var fieldValue])
            host.Window.GetType().GetField(fieldName,BindingFlags.Instance|BindingFlags.NonPublic)?.SetValue(host.Window,fieldValue);
        var capture=Option(args,"--capture=");
        var frames=int.TryParse(Option(args,"--frames="),out var n) ? n : capture is null ? int.MaxValue : 90;
        var clock=Stopwatch.StartNew();var last=0.0;string? lastError=null;
        for(var frame=0;frame<frames && !window.Closed;frame++)
        {
            window.Pump();
            // --click=x,y: a left click at that point of the window, on frames 30 to 32 (through ImGui's input queue, as the window's own mouse).
            if(Option(args,"--click=") is { } click && click.Split(',') is [var cx,var cy] && frame is >=30 and <=32)
            {
                var io=Dalamud.Bindings.ImGui.ImGui.GetIO();
                io.AddMousePosEvent(float.Parse(cx),float.Parse(cy));
                if(frame==31)io.AddMouseButtonEvent(0,true);
                if(frame==32)io.AddMouseButtonEvent(0,false);
            }
            var now=clock.Elapsed.TotalSeconds;
            var failure=Ui.Frame(host,window.Size,(float)(now-last));last=now;
            if(failure is not null && failure.Message!=lastError){lastError=failure.Message;Console.WriteLine($"  page {host.CurrentPage}: {failure.GetType().Name}: {failure.Message}");}
            window.Render(Dalamud.Bindings.ImGui.ImGui.GetDrawData(),new Vector4(0.08f,0.08f,0.1f,1));
            if(capture is not null && frame==frames-1)
            {
                Png.Write(capture,(int)window.Size.X,(int)window.Size.Y,window.Capture());
                Console.WriteLine($"PASS capture: {capture} ({window.Size.X}x{window.Size.Y}), page {host.CurrentPage}, icons served {TextureProviderProxy.Served}, missing {TextureProviderProxy.Missing}");
            }
            window.Swap();
            if(capture is not null)Thread.Sleep(16);
        }
        window.Dispose();
        return 0;
    }
}
