// X-115 (client map database, step A): snapshot of the installed client, read with Lumina alone (no Dalamud, no game running).
// Usage: ClientSnapshot <sqpack> <out folder> [--lang=French] [--plugin=<TenshoAssistant.dll>] [--only=family,…] [--probe-lgb=all]
// 1. The five catalogues of the plugin cache (catalog-map-positions, -npc-vendors, -gathering, -items, -recipes) are built by the plugin's
//    OWN loaders (MapLocalIndexLoader, NpcVendorLoader, GatheringCatalogLoader, ItemCatalogLoader, RecipeCatalogLoader), taken from the
//    built TenshoAssistant.dll by reflection and fed a Lumina GameData through stand-ins of IDataManager, IFramework and IPluginLog.
//    They write through the plugin's DataCache into <out>/cache: the same envelope, family, scope and version string as the plugin's
//    own cache, so a snapshot compares with cache files copied from the plugin (import-cache in tools/client-snapshot.py).
// 2. client-maps.json: maps, map markers and aetherytes (not cached by the plugin), read here directly.
// 3. client-placements.json (X-116): what the layout files (.lgb) place: NPCs, objects, FATE and CE ranges, aetherytes, coffers, exits.
// It never writes to the game folder. The output is Square Enix data: keep it out of git (tools/client-snapshot.py stores it outside the repo).
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Json;
using Lumina;
using Lumina.Data;
using Lumina.Excel.Sheets;

internal static class Program
{
    internal static readonly string Hooks=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"XIVLauncher","addon","Hooks","dev");
    internal static readonly string[] Families=["catalog-map-positions","catalog-npc-vendors","catalog-gathering","catalog-items","catalog-recipes"];
    private static readonly Dictionary<string,string> Loaders=new()
    {
        ["catalog-map-positions"]="TenshoAssistant.Maps.MapLocalIndexLoader",
        ["catalog-npc-vendors"]="TenshoAssistant.Npcs.NpcVendorLoader",
        ["catalog-gathering"]="TenshoAssistant.Gathering.GatheringCatalogLoader",
        ["catalog-items"]="TenshoAssistant.Items.ItemCatalogLoader",
        ["catalog-recipes"]="TenshoAssistant.Craft.RecipeCatalogLoader",
    };

    private static int Main(string[] args)
    {
        if(args.Length<2){Console.Error.WriteLine("usage: ClientSnapshot <sqpack> <out folder> [--lang=French] [--plugin=<dll>] [--only=family,…]");return 2;}
        var plugin=Option(args,"--plugin=") ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..","..","src","TenshoAssistant","bin","x64","Release","TenshoAssistant.dll"));
        if(!File.Exists(plugin)){Console.Error.WriteLine($"FAIL plugin not built: {plugin} (run .\\scripts\\Verify.ps1 once)");return 1;}
        var pluginDir=Path.GetDirectoryName(plugin)!;
        AssemblyLoadContext.Default.Resolving+=(context,name)=>
        {
            foreach(var dir in new[]{Hooks,pluginDir})
            {
                var path=Path.Combine(dir,name.Name+".dll");
                if(File.Exists(path))return context.LoadFromAssemblyPath(path);
            }
            return null;
        };
        return Run(args,plugin);
    }

    internal static string? Option(string[] args,string prefix)=>args.FirstOrDefault(a=>a.StartsWith(prefix,StringComparison.Ordinal))?[prefix.Length..];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Run(string[] args,string plugin)
    {
        var sqpack=args[0];var outDir=Path.GetFullPath(args[1]);
        var language=Enum.Parse<Language>(Option(args,"--lang=") ?? "French",true);
        var only=Option(args,"--only=")?.Split(',',StringSplitOptions.RemoveEmptyEntries).ToHashSet() ?? [..Families,"maps","placements"];
        using var data=new GameData(sqpack,new LuminaOptions{DefaultExcelLanguage=language,CacheFileResources=false,PanicOnSheetChecksumMismatch=false});
        Directory.CreateDirectory(outDir);
        var failures=0;
        var assembly=AssemblyLoadContext.Default.LoadFromAssemblyPath(plugin);
        Console.WriteLine($"plugin {assembly.GetName().Version} ({plugin}), Lumina {typeof(GameData).Assembly.GetName().Version}, Lumina.Excel {typeof(Item).Assembly.GetName().Version}, language {language}");
        foreach(var family in Families.Where(only.Contains))
        {
            var started=System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var count=Catalogs.Build(assembly,family,Loaders[family],data,language,Path.Combine(outDir,"cache"));
                Console.WriteLine($"PASS {family} written in {started.Elapsed.TotalSeconds:0.0} s ({count} log warnings)");
            }
            catch(Exception failure)
            {
                failures++;
                Console.WriteLine($"FAIL {family}: {(failure is TargetInvocationException {InnerException:{ } inner} ? inner : failure).GetType().Name}: {(failure.InnerException ?? failure).Message}");
            }
        }
        if(only.Contains("maps"))
        {
            try{Maps.Write(data,Path.Combine(outDir,"client-maps.json"));}
            catch(Exception failure){failures++;Console.WriteLine($"FAIL maps: {failure.GetType().Name}: {failure.Message}");}
        }
        if(only.Contains("placements"))
        {
            try{Placements.Write(data,Path.Combine(outDir,"client-placements.json"));}
            catch(Exception failure){failures++;Console.WriteLine($"FAIL placements: {failure.GetType().Name}: {failure.Message}");}
        }
        if(Option(args,"--probe-lgb=") is { } probe)Placements.Probe(data,probe);
        return failures==0 ? 0:1;
    }
}

/// <summary>Runs one plugin catalogue loader to completion on a Lumina GameData, through the plugin's own DataCache.</summary>
internal static class Catalogs
{
    public static int Build(Assembly plugin,string family,string loaderName,GameData data,Language language,string cacheDir)
    {
        var loaderType=plugin.GetType(loaderName,true)!;
        var ctor=loaderType.GetConstructors(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Single();
        var parameters=ctor.GetParameters();
        var service=Activator.CreateInstance(parameters[0].ParameterType,true)!;
        var cacheType=plugin.GetType("TenshoAssistant.Caching.DataCache",true)!;
        using var cache=(IDisposable)Activator.CreateInstance(cacheType,cacheDir)!;
        var dataProxy=new DataManagerStub(data,language);
        var framework=FrameworkProxy.Create();
        var log=LogProxy.Create();
        var loader=ctor.Invoke([service,dataProxy,framework,log,cache]);
        var host=(FrameworkProxy)(object)framework;
        var deadline=DateTime.UtcNow.AddMinutes(10);
        while(host.Handlers is { } handlers)
        {
            handlers(framework);
            if(DateTime.UtcNow>deadline)throw new TimeoutException("loader did not finish within 10 minutes");
        }
        ((Task)cacheType.GetMethod("FlushAsync")!.Invoke(cache,null)!).GetAwaiter().GetResult();
        if(cacheType.GetProperty("Error")!.GetValue(cache) is string error)throw new InvalidOperationException(error);
        if(LogProxy.LastError is { } logged)throw new InvalidOperationException("loader logged an error: "+logged);
        (loader as IDisposable)?.Dispose();
        // The loaders log a warning and stop when the build fails: the family file must be there, or the family failed.
        var marker=$"\"Family\":\"{family}\"";
        var written=Directory.Exists(cacheDir) && Directory.EnumerateFiles(cacheDir,"*.json").Any(file=>
        {
            using var stream=File.OpenRead(file);var head=new byte[512];var read=stream.Read(head);
            return System.Text.Encoding.UTF8.GetString(head,0,read).Contains(marker,StringComparison.Ordinal);
        });
        if(!written)throw new InvalidOperationException("the loader finished without writing its cache file (see the warning above)");
        return LogProxy.Warnings;
    }
}

/// <summary>IDataManager stand-in: GameData, Language and the Excel sheet getters, forwarded to Lumina; file access too. A real class, not a
/// DispatchProxy: the proxy cannot emit GetExcelSheet's self-referencing generic constraint (TypeLoadException, tried 2026-11-10).</summary>
public sealed class DataManagerStub(GameData data,Language language):Dalamud.Plugin.Services.IDataManager
{
    private static Language? Lumina(Dalamud.Game.ClientLanguage? given)=>given is { } value ? Enum.Parse<Language>(value.ToString()) : null;
    public Dalamud.Game.ClientLanguage Language=>Enum.Parse<Dalamud.Game.ClientLanguage>(language.ToString());
    public GameData GameData=>data;
    public Lumina.Excel.ExcelModule Excel=>data.Excel;
    public bool HasModifiedGameDataFiles=>false;
    public Lumina.Excel.ExcelSheet<T> GetExcelSheet<T>(Dalamud.Game.ClientLanguage? language=null,string? name=null) where T:struct,Lumina.Excel.IExcelRow<T>
        =>data.Excel.GetSheet<T>(Lumina(language) ?? language_,name);
    public Lumina.Excel.SubrowExcelSheet<T> GetSubrowExcelSheet<T>(Dalamud.Game.ClientLanguage? language=null,string? name=null) where T:struct,Lumina.Excel.IExcelSubrow<T>
        =>data.Excel.GetSubrowSheet<T>(Lumina(language) ?? language_,name);
    private Language language_=>language;
    public FileResource? GetFile(string path)=>data.GetFile(path);
    public T? GetFile<T>(string path) where T:FileResource=>data.GetFile<T>(path);
    public Task<T> GetFileAsync<T>(string path,CancellationToken cancellationToken) where T:FileResource=>Task.Run(()=>data.GetFile<T>(path)!,cancellationToken);
    public bool FileExists(string path)=>data.FileExists(path);
}

/// <summary>IFramework stand-in: always "on the framework thread", never unloading; keeps the Update handlers so the host can tick them.</summary>
public class FrameworkProxy:DispatchProxy
{
    public Dalamud.Plugin.Services.IFramework.OnUpdateDelegate? Handlers;
    public static Dalamud.Plugin.Services.IFramework Create()=>Create<Dalamud.Plugin.Services.IFramework,FrameworkProxy>();
    protected override object? Invoke(MethodInfo? method,object?[]? args)
    {
        switch(method!.Name)
        {
            case "get_IsInFrameworkUpdateThread":return true;
            case "get_IsFrameworkUnloading":return false;
            case "add_Update":Handlers+=(Dalamud.Plugin.Services.IFramework.OnUpdateDelegate)args![0]!;return null;
            case "remove_Update":Handlers-=(Dalamud.Plugin.Services.IFramework.OnUpdateDelegate)args![0]!;return null;
        }
        throw new NotSupportedException($"IFramework.{method.Name} is not available outside Dalamud");
    }
}

/// <summary>IPluginLog stand-in: warnings and errors printed; an error fails the family.</summary>
public class LogProxy:DispatchProxy
{
    public static int Warnings;public static string? LastError;
    public static Dalamud.Plugin.Services.IPluginLog Create(){Warnings=0;LastError=null;return Create<Dalamud.Plugin.Services.IPluginLog,LogProxy>();}
    protected override object? Invoke(MethodInfo? method,object?[]? args)
    {
        var name=method!.Name;
        if(name.StartsWith("get_",StringComparison.Ordinal))return method.ReturnType.IsValueType ? Activator.CreateInstance(method.ReturnType):null;
        var text=string.Join(" | ",(args ?? []).Select(a=>a switch{Exception e=>(Environment.GetEnvironmentVariable("SNAPSHOT_TRACE") is null ? e.GetType().Name+": "+e.Message : e.ToString()),object[] list=>string.Join(",",list),_=>a?.ToString()}));
        if(name is "Warning"){Warnings++;Console.WriteLine("  warning: "+text);}
        else if(name is "Error" or "Fatal"){LastError=text;Console.WriteLine("  error: "+text);}
        return null;
    }
}

/// <summary>Maps, their markers and the aetherytes, in a small JSON of our own (the plugin does not cache them).</summary>
internal static class Maps
{
    public static void Write(GameData data,string path)
    {
        var markers=data.Excel.GetSubrowSheet<MapMarker>();
        var maps=data.Excel.GetSheet<Map>().Where(m=>m.TerritoryType.RowId!=0).Select(m=>new
        {
            Id=m.RowId,Key=m.Id.ExtractText(),Territory=m.TerritoryType.RowId,Name=m.PlaceName.ValueNullable?.Name.ExtractText() ?? "",
            Region=m.PlaceNameRegion.ValueNullable?.Name.ExtractText() ?? "",Sub=m.PlaceNameSub.ValueNullable?.Name.ExtractText() ?? "",
            m.SizeFactor,m.OffsetX,m.OffsetY,MarkerRange=(uint)m.MapMarkerRange,
        }).ToList();
        var markerRows=new List<object>();
        foreach(var range in maps.Select(m=>m.MarkerRange).Where(r=>r!=0).Distinct().Order())
        {
            if(!markers.TryGetRow(range,out var rows))continue;
            foreach(var marker in rows)
                markerRows.Add(new{Range=range,Index=marker.SubrowId,marker.X,marker.Y,Icon=marker.Icon,
                    Label=marker.PlaceNameSubtext.ValueNullable?.Name.ExtractText() ?? "",marker.DataType,DataKey=marker.DataKey.RowId});
        }
        var aetherytes=data.Excel.GetSheet<Aetheryte>().Where(a=>a.PlaceName.RowId!=0 || a.AethernetName.RowId!=0).Select(a=>new
        {
            Id=a.RowId,Name=a.PlaceName.ValueNullable?.Name.ExtractText() ?? "",Aethernet=a.AethernetName.ValueNullable?.Name.ExtractText() ?? "",
            Territory=a.Territory.RowId,Map=a.Map.RowId,a.IsAetheryte,
        }).ToList();
        var json=JsonSerializer.SerializeToUtf8Bytes(new{Schema=1,Source="Lumina.Excel/Map+MapMarker+Aetheryte",Maps=maps,Markers=markerRows,Aetherytes=aetherytes},
            new JsonSerializerOptions{Encoder=System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping});
        File.WriteAllBytes(path,json);
        Console.WriteLine($"PASS maps: {maps.Count} maps, {markerRows.Count} markers, {aetherytes.Count} aetherytes");
    }
}

/// <summary>X-116 (step B): what the client's layout files (.lgb) place in each territory, in a small JSON of our own (client-placements.json).
/// Families, from the probe of 2026-11-10 (--probe-lgb=all, docs/evidence/x116-client-placements.md): enpc (EventNPC, ENpcBase row), eobj (EventObject,
/// EObj row), fate (the EventRange whose instance id is Fate.Location, Fate row), ce (the MapRange of DynamicEvent.LGBMapRange), aetheryte, treasure
/// (Treasure row), exit (ExitRange, destination TerritoryType). The layout files place no monster (0 BattleNPC entry), so monsters stay out.
/// Positions are world X/Y/Z; one row per placement, keyed by its layout (Bg index), instance id and base row.</summary>
internal static class Placements
{
    private static readonly string[] Files=["bg","planevent","planlive","planmap","planner"];

    private static IEnumerable<(string Bg,string File,Lumina.Data.Parsing.Layer.LayerCommon.Layer Layer)> Layers(GameData data,IEnumerable<string> bgs)
    {
        foreach(var bg in bgs)
            foreach(var kind in Files)
            {
                var path=$"bg/{bg}/level/{kind}.lgb";
                if(!data.FileExists(path))continue;
                var raw=data.GetFile(path)!.Data;
                if(raw.Length<16 || raw[0]!='L' || raw[12]!='L')continue;  // a few planner.lgb files have another header (probe: 17)
                foreach(var layer in data.GetFile<Lumina.Data.Files.LgbFile>(path)!.Layers)yield return (bg,kind,layer);
            }
    }

    private static string Level(string bg){var cut=bg.IndexOf("/level/",StringComparison.Ordinal);return cut<0 ? "":bg[..cut];}

    public static void Write(GameData data,string path)
    {
        var started=System.Diagnostics.Stopwatch.StartNew();
        var territories=data.Excel.GetSheet<TerritoryType>().Where(t=>Level(t.Bg.ExtractText()).Length>0).Select(t=>new
        {
            Id=t.RowId,Bg=Level(t.Bg.ExtractText()),Name=t.PlaceName.ValueNullable?.Name.ExtractText() ?? "",Map=t.Map.RowId,Use=(uint)t.TerritoryIntendedUse.RowId,
        }).ToList();
        var bgs=territories.Select(t=>t.Bg).Distinct().Order(StringComparer.Ordinal).ToList();
        var bgIndex=bgs.Select((bg,i)=>(bg,i)).ToDictionary(p=>p.bg,p=>p.i);
        var fateByLocation=data.Excel.GetSheet<Fate>().Where(f=>f.Location!=0).GroupBy(f=>f.Location).ToDictionary(g=>g.Key,g=>g.Select(f=>f.RowId).ToArray());
        var ceByRange=data.Excel.GetSheet<DynamicEvent>().Where(e=>e.LGBMapRange!=0).GroupBy(e=>e.LGBMapRange).ToDictionary(g=>g.Key,g=>g.Select(e=>e.RowId).ToArray());
        var rows=new List<Row>();
        foreach(var (bg,file,layer) in Layers(data,bgs))
            foreach(var o in layer.InstanceObjects)
            {
                var p=o.Transform.Translation;
                void Add(string family,uint baseId)=>rows.Add(new(family,bgIndex[bg],file,o.InstanceId,baseId,R(p.X),R(p.Y),R(p.Z),layer.FestivalID));
                switch(o.AssetType)
                {
                    case Lumina.Data.Parsing.Layer.LayerEntryType.EventNPC:Add("enpc",((Lumina.Data.Parsing.Layer.LayerCommon.ENPCInstanceObject)o.Object).ParentData.ParentData.BaseId);break;
                    case Lumina.Data.Parsing.Layer.LayerEntryType.EventObject:Add("eobj",((Lumina.Data.Parsing.Layer.LayerCommon.EventInstanceObject)o.Object).ParentData.BaseId);break;
                    case Lumina.Data.Parsing.Layer.LayerEntryType.Aetheryte:Add("aetheryte",((Lumina.Data.Parsing.Layer.LayerCommon.AetheryteInstanceObject)o.Object).ParentData.BaseId);break;
                    case Lumina.Data.Parsing.Layer.LayerEntryType.Treasure:Add("treasure",((Lumina.Data.Parsing.Layer.LayerCommon.TreasureInstanceObject)o.Object).ParentData.BaseId);break;
                    case Lumina.Data.Parsing.Layer.LayerEntryType.ExitRange:Add("exit",((Lumina.Data.Parsing.Layer.LayerCommon.ExitRangeInstanceObject)o.Object).TerritoryType);break;
                }
                if(fateByLocation.TryGetValue(o.InstanceId,out var fates))foreach(var fate in fates)Add("fate",fate);
                if(ceByRange.TryGetValue(o.InstanceId,out var events))foreach(var ce in events)Add("ce",ce);
            }
        string Name<T>(uint id,Func<T,string> name) where T:struct,Lumina.Excel.IExcelRow<T> => data.Excel.GetSheet<T>().GetRowOrDefault(id) is { } row ? name(row):"";
        var names=new Dictionary<string,Dictionary<uint,string>>();
        foreach(var family in rows.Select(r=>r.Family).Distinct())
            names[family]=rows.Where(r=>r.Family==family).Select(r=>r.Base).Distinct().Order().ToDictionary(id=>id,id=>family switch
            {
                "enpc"=>Name<ENpcResident>(id,r=>r.Singular.ExtractText()),
                "eobj"=>Name<EObjName>(id,r=>r.Singular.ExtractText()),
                "fate"=>Name<Fate>(id,r=>r.Name.ExtractText()),
                "ce"=>Name<DynamicEvent>(id,r=>r.Name.ExtractText()),
                "aetheryte"=>Name<Aetheryte>(id,r=>r.PlaceName.ValueNullable?.Name.ExtractText() ?? ""),
                "exit"=>Name<TerritoryType>(id,r=>r.PlaceName.ValueNullable?.Name.ExtractText() ?? ""),
                _=>"",
            });
        var options=new JsonSerializerOptions{Encoder=System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault};
        var json=JsonSerializer.SerializeToUtf8Bytes(new{Schema=1,Source="Lumina LgbFile bg/planevent/planlive/planmap/planner + Fate.Location + DynamicEvent.LGBMapRange",
            Bgs=bgs,Territories=territories,Names=names,Placements=rows},options);
        File.WriteAllBytes(path,json);
        Console.WriteLine($"PASS placements: {rows.Count} in {bgs.Count} layouts ({string.Join(", ",rows.GroupBy(r=>r.Family).OrderBy(g=>g.Key).Select(g=>$"{g.Key} {g.Count()}"))}), "
            +$"{json.Length/1024} KB, {started.Elapsed.TotalSeconds:0.0} s");
    }

    private static float R(float value)=>MathF.Round(value,2);

    /// <summary>--probe-lgb=all: every asset type of the layout files, counted per file kind (R06: which placements the client holds).</summary>
    public static void Probe(GameData data,string _)
    {
        var bgs=data.Excel.GetSheet<TerritoryType>().Select(t=>Level(t.Bg.ExtractText())).Where(bg=>bg.Length>0).Distinct().ToList();
        var counts=new SortedDictionary<string,int>(StringComparer.Ordinal);
        foreach(var (_,file,layer) in Layers(data,bgs))
            foreach(var o in layer.InstanceObjects)
            {
                var key=$"{file} {o.AssetType}";
                counts[key]=counts.GetValueOrDefault(key)+1;
            }
        Console.WriteLine($"PROBE lgb: {bgs.Count} layouts");
        foreach(var (key,count) in counts)Console.WriteLine($"  {key}: {count}");
    }

    private sealed record Row(string Family,int Bg,string File,uint Instance,uint Base,float X,float Y,float Z,ushort Festival);
}
