using System.Globalization;
using System.Numerics;
using System.Reflection;
using Dalamud.Bindings.ImGui;
using Lumina;
using Lumina.Data;
using PluginUiTest.Host;
using TenshoAssistant.Desktop;

/// <summary>
/// The plugin's real main window (TenshoAssistant.MainWindow from the built DLL), built with its real constructor: services the plugin
/// creates are created the same way, Dalamud services are the stand-ins of PluginUiTest.Host (StandIns.cs, moved there in X-120):
/// IDataManager, IFramework and IPluginLog as in tools/ClientSnapshot; ITextureProvider reads the game icon (.tex) through Lumina and
/// uploads it to the desktop renderer; any other Dalamud interface is an "absent" stand-in that answers default values and records each
/// member the page code reached (X-118's inventory evidence).
/// The plugin's saved settings (pluginConfigs\TenshoAssistant.json) are read as Dalamud reads them, read-only: the app never saves them.
/// </summary>
internal sealed class Host
{
    public readonly Assembly Plugin;
    public readonly GameData Data;
    public readonly DataManagerStub DataStub;
    public readonly object Window;
    public readonly string SettingsNote;
    public readonly string TextCode;
    public readonly Language GameLanguage;
    private readonly object localization;
    private readonly MethodInfo format,has;
    private readonly Dictionary<Type,object?> made=[];
    private readonly List<object> loaders=[];
    /// <summary>X-151 (0.1238): the window's typed contract (DesktopSurface), read once after the window is built.</summary>
    public readonly DesktopSurface Surface;
    /// <summary>X-151: the entries the plugin did not give (another version): said at start, a failure of the headless run.</summary>
    public readonly IReadOnlyList<string> SurfaceMissing;
    private readonly string? settingsPath;
    private readonly Type configurationType;

    /// <summary>The plugin's catalogue loaders that need only (service, IDataManager, IFramework, IPluginLog[, DataCache]) (Plugin.cs).</summary>
    public static readonly string[] Loaders=["MapCatalogLoader","WeatherCatalogLoader","DutyCatalogLoader","GpCatalogLoader","CraftActionCatalogLoader","FashionHintLoader",
        "ItemUseCatalogLoader","ItemCatalogLoader","RecipeCatalogLoader","GatheringCatalogLoader","NpcVendorLoader","MapLocalIndexLoader"];

    public Host(string pluginPath,string sqpack,string? gameLanguage,string? textLanguage,string? settingsPath)
    {
        Plugin=System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(pluginPath);
        var pluginDirectory=Path.GetDirectoryName(pluginPath)!;
        configurationType=Plugin.GetType("TenshoAssistant.Configuration",true)!;
        this.settingsPath=settingsPath;
        var saved=ReadSettings(settingsPath,configurationType,out SettingsNote);
        if(saved is not null)made[configurationType]=saved;
        var savedCode=saved is null ? null : configurationType.GetProperty("LanguageCode")?.GetValue(saved) as string;
        TextCode=DesktopPaths.TextLanguage(textLanguage ?? savedCode,CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        GameLanguage=Enum.Parse<Language>(gameLanguage ?? DesktopPaths.GameLanguage(TextCode),true);
        Data=new GameData(sqpack,new LuminaOptions{DefaultExcelLanguage=GameLanguage,CacheFileResources=false,PanicOnSheetChecksumMismatch=false});
        GameIcons.Data=Data;
        DataStub=new DataManagerStub(Data,GameLanguage);
        // Dalamud's scale is set by its interface manager; 1 here (no DPI scaling yet).
        ImGuiHeadless.GlobalScale(1f);
        var mainType=Plugin.GetType("TenshoAssistant.MainWindow",true)!;
        var localizationType=Plugin.GetType("TenshoAssistant.Localization.LocalizationManager",true)!;
        localization=Activator.CreateInstance(localizationType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,
            [(Action<Exception,string>)((e,m)=>Console.WriteLine($"  localization warning: {m}: {e.Message}")),pluginDirectory],null)!;
        localizationType.GetMethod("SelectInitialLanguage")!.Invoke(localization,[TextCode,GameLanguage.ToString()]);
        format=localizationType.GetMethod("Format")!;has=localizationType.GetMethod("Has")!;
        made[localizationType]=localization;
        var ctor=mainType.GetConstructors(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Single();
        // The save action does nothing: the app reads the plugin's settings and never writes them.
        var args=ctor.GetParameters().Select(p=>p.ParameterType==typeof(Action) ? (Action)(()=>{}) : Make(p.ParameterType,0)).ToArray();
        Window=ctor.Invoke(args);
        // X-151 (0.1238): the one name left for the window's own entries; everything after it is typed (DesktopSurface, shared source).
        var map=mainType.GetMethod("DesktopSurfaceMap",BindingFlags.Instance|BindingFlags.NonPublic,Type.EmptyTypes)?.Invoke(Window,null) as IReadOnlyDictionary<string,Delegate>;
        Surface=DesktopSurface.FromMap(map,out SurfaceMissing);
        if(map is null)SurfaceMissing=["MainWindow.DesktopSurfaceMap (another version?)"];
        // X-119 step 4 (0.1158): the window knows it is drawn by the desktop application (its navigation follows the player's page choices).
        Surface.DesktopMode();
        // X-178 (0.1258): the garden's seed list and crossbreeds, built by the window from the sheets as in the game.
        mainType.GetMethod("ConfigureGarden",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)?.Invoke(Window,[DataStub]);
        // X-285 (0.1300): the page loaders that read only the sheets, given by the plugin's own method, as Plugin.cs does (the lexicon,
        // the allied societies, the Grand Companies, the island, the deliveries, the carnivale, the relics, ocean fishing, FATEs, field
        // content, deep dungeons). Before, only the lexicon was given here: the others drew an error or a load that never ended.
        mainType.GetMethod("UseSheetLoaders",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)?.Invoke(Window,[DataStub,LogProxy.Create()]);
    }

    /// <summary>
    /// X-284 M2 (review of block 0.1278: the reference pictures lacked the error state): the sheet loaders given a data manager whose every
    /// read fails, as after a game update the plugin cannot read (R08), or the real one back; the pages that read their sheets once forget
    /// what they read (MainWindow.ForgetSheetPages), so their next draw reads again. False when the plugin has no such method.
    /// </summary>
    public bool SheetsFail(bool fail)
    {
        var type=Window.GetType();
        var use=type.GetMethod("UseSheetLoaders",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        var forget=type.GetMethod("ForgetSheetPages",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        if(use is null || forget is null)return false;
        use.Invoke(Window,[fail ? FailingData.Create() : DataStub,LogProxy.Create()]);
        forget.Invoke(Window,null);
        return true;
    }

    /// <summary>X-284 M2: a data manager whose every member throws (the sheets unreadable), for the error pictures.</summary>
    public class FailingData:DispatchProxy
    {
        public static Dalamud.Plugin.Services.IDataManager Create()=>DispatchProxy.Create<Dalamud.Plugin.Services.IDataManager,FailingData>();
        protected override object? Invoke(MethodInfo? method,object?[]? args)=>
            throw new InvalidDataException($"The game's sheets cannot be read (reference picture of the error state: {method?.Name}).");
    }

    /// <summary>The plugin's saved settings read as Dalamud reads them (Newtonsoft, type names), or null (no file, or unreadable).</summary>
    private object? ReadSettings(string? path,Type configurationType,out string note)
    {
        if(path is null){note="new (asked)";return null;}
        if(!File.Exists(path)){note="new (no saved settings at "+path+")";return null;}
        try
        {
            var value=Parse(path);
            note=value is null ? "new (saved settings empty)" : "read from "+path+" (read-only, without the Discord addresses)";
            return value;
        }
        catch(Exception error){note=$"new (saved settings unreadable: {error.GetType().Name}: {error.Message})";return null;}
    }

    /// <summary>
    /// X-150 (0.1120): one reading of the plugin's settings into a new Configuration, never into the live one: the file read whole and
    /// bounded (SettingsGuard.ReadBounded), its secrets taken out (SettingsGuard.Project), then Newtonsoft with type names whose binder
    /// accepts the closed list of SettingsGuard.AllowedType only. Any thread (the app reads it off the frame). Throws when it is not valid.
    /// </summary>
    public object? Parse(string path)
    {
        var settings=new Newtonsoft.Json.JsonSerializerSettings
        {
            TypeNameHandling=Newtonsoft.Json.TypeNameHandling.Objects,
            TypeNameAssemblyFormatHandling=Newtonsoft.Json.TypeNameAssemblyFormatHandling.Simple,
            SerializationBinder=new PluginBinder(Plugin),
            MaxDepth=64,
        };
        return Newtonsoft.Json.JsonConvert.DeserializeObject(SettingsGuard.Project(SettingsGuard.ReadBounded(path)),configurationType,settings);
    }

    /// <summary>
    /// X-119 step 2, part 2 and X-151 (0.1131, typed in 0.1238): the plugin's stores of a category built on the application's copy of the
    /// plugin's folder, by the plugin's own MainWindow.UseDesktopData (DesktopSurface.UseData). On the UI thread, between frames. A note in
    /// English for the log.
    /// </summary>
    public string UseData(string mirror,string category)
    {
        try{Surface.UseData(mirror,category,(error,message)=>Console.WriteLine($"  {category}: {message} ({error.GetType().Name})"));return $"{category}: read from the copy";}
        catch(Exception error){return $"{category}: not read ({error.GetType().Name}: {error.Message})";}
    }

    /// <summary>
    /// X-119 with X-173 (0.1147, typed in 0.1238): the player's data file (TenshoData.db) copied into the mirror by the plugin's own
    /// TenshoDataDatabase.CopyForDesktop (DesktopSurface.CopyData: SQLite's backup, the plugin's file read-only). Any thread.
    /// </summary>
    public (int Copied,IReadOnlyList<string> Problems) CopyData(string source,string mirror)
    {
        try{return Surface.CopyData(Path.Combine(source,"TenshoData.db"),Path.Combine(mirror,"TenshoData.db"));}
        catch(Exception error){return (0,[$"TenshoData.db: not copied ({error.GetType().Name}: {error.Message})"]);}
    }

    /// <summary>
    /// X-119 (0.1152, typed in 0.1238): the discoveries' database (Discoveries/Discoveries.db, that file only) copied into the mirror by the
    /// plugin's own TenshoDataDatabase.CopyDiscoveriesForDesktop (DesktopSurface.CopyDiscoveries: SQLite's backup, read-only). Any thread.
    /// </summary>
    public (int Copied,IReadOnlyList<string> Problems) CopyDiscoveries(string source,string mirror)
    {
        try{return Surface.CopyDiscoveries(Path.Combine(source,"Discoveries","Discoveries.db"),Path.Combine(mirror,"Discoveries","Discoveries.db"));}
        catch(Exception error){return (0,[$"Discoveries.db: not copied ({error.GetType().Name}: {error.Message})"]);}
    }

    /// <summary>X-163 (0.1132, typed in 0.1238): the alarms to ring now, from the plugin's MainWindow.DesktopDueAlarms; none when the plugin gives none.</summary>
    public (string[] Lines,bool Sound) DueAlarms(DateTimeOffset now,bool gameInFront)=>Surface.DueAlarms(now,gameInFront);

    /// <summary>X-163 (0.1154, typed in 0.1238): whether the alarms rung also show a Windows notification (MainWindow.DesktopAlarmNotification).</summary>
    public bool AlarmNotification()=>Surface.AlarmNotification();

    /// <summary>X-163 (0.1155, typed in 0.1238): the plugin's alerts received by the link, as MainWindow.DesktopLinkNotices rings them.</summary>
    public (string[] Lines,bool Sound) LinkNotices(string[] texts,bool gameInFront)=>Surface.LinkNotices(texts,gameInFront);

    /// <summary>X-163 (0.1156, typed in 0.1238): the held counts of the alarms' items brought by the link, to MainWindow.DesktopAlarmHeld.</summary>
    public void AlarmHeld(uint[] items,long[] counts)=>Surface.AlarmHeld(items,counts);
    /// <summary>X-165 (0.1246): Tensho's state in the game brought by the link (null: none), to MainWindow.DesktopGameState.</summary>
    public void GameState(TenshoAssistant.Link.LinkState? state)=>Surface.GameState(state?.Overall,state?.HeapMegabytes,state is null ? [] : [.. state.Issues]);
    /// <summary>X-264 (0.1271): the game's measures brought by the link (null: none) at the application's clock, to MainWindow.DesktopGameMeasures.</summary>
    public void GameMeasures(TenshoAssistant.Link.LinkMeasures? measures,long at)
        =>Surface.GameMeasures(measures is null ? null : [measures.MemoryMb,measures.PageMs,measures.FrameAvgMs,measures.FrameP95Ms],
            measures is null ? null : [measures.SlowFrames,measures.Requests,measures.WritesFailed,measures.PanelsOff,measures.Unavailable],at);

    /// <summary>X-150: a new reading of the settings, off the frame (StatusBar groups the link's notices); null without a settings file read at start.</summary>
    public object? ReadSettingsAgain()=>settingsPath is null || !made.TryGetValue(configurationType,out var target) || target is null ? null : Parse(settingsPath);

    /// <summary>
    /// X-119 step 2 (0.1114), X-150 (0.1120): a settings object read and checked off the frame (ReadSettingsAgain) applied at once to the
    /// window's own Configuration, on the UI thread between frames: every public property and field copied from it, so a reading that
    /// failed never leaves the live settings half changed (the last valid ones stay). A note in English of what happened.
    /// </summary>
    public string ApplySettings(object fresh)
    {
        if(!made.TryGetValue(configurationType,out var target) || target is null)return "not read again: no saved settings were read at start";
        if(fresh.GetType()!=configurationType)return "not read again: not the plugin's settings";
        foreach(var property in configurationType.GetProperties(BindingFlags.Instance|BindingFlags.Public).Where(p=>p.CanRead && p.CanWrite && p.GetIndexParameters().Length==0))
            property.SetValue(target,property.GetValue(fresh));
        foreach(var field in configurationType.GetFields(BindingFlags.Instance|BindingFlags.Public).Where(f=>!f.IsInitOnly))
            field.SetValue(target,field.GetValue(fresh));
        return "read again from "+settingsPath;
    }

    /// <summary>Type names of the saved settings: the plugin's own from the loaded plugin (its copy), the others as Newtonsoft does; X-150
    /// (audit A15): any type outside SettingsGuard.AllowedType refused (the reading fails, nothing applied).</summary>
    private sealed class PluginBinder(Assembly plugin):Newtonsoft.Json.Serialization.DefaultSerializationBinder
    {
        public override Type BindToType(string? assemblyName,string typeName)
        {
            var type=assemblyName=="TenshoAssistant" && plugin.GetType(typeName) is { } own ? own : base.BindToType(assemblyName,typeName);
            return SettingsGuard.AllowedType(type,plugin) ? type : throw new Newtonsoft.Json.JsonSerializationException("type not allowed in the settings: "+type.FullName);
        }
    }

    public IEnumerable<string> Pages=>Surface.Pages();
    public void SetPage(string name)=>Surface.SetPage(name);
    /// <summary>X-154 (0.1252): one setting of the copy in memory (--theme=, the folded navigation of the headless run); never written: the app saves nothing.</summary>
    /// <summary>X-178 (0.1257): one setting of the copy in memory, to put it back after a trial (null when it cannot be reached).</summary>
    public object? GetSetting(string name)
        =>made.TryGetValue(configurationType,out var target) && target is not null ? configurationType.GetProperty(name)?.GetValue(target) : null;

    public bool SetSetting(string name,object? value)
    {
        if(!made.TryGetValue(configurationType,out var target) || target is null || configurationType.GetProperty(name) is not { CanWrite:true } property)return false;
        property.SetValue(target,value);
        return true;
    }
    public string CurrentPage=>Surface.CurrentPage();
    public object? Service(string fullName)=>made.FirstOrDefault(pair=>pair.Key.FullName==fullName).Value;

    /// <summary>A text of the plugin's language files, in the plugin's language (LocalizationManager.Format).</summary>
    public string Text(string key,params object[] args)=>(string)format.Invoke(localization,[key,args])!;
    public bool HasText(string key)=>(bool)has.Invoke(localization,[key])!;

    /// <summary>The pages that stay in the game leave the navigation (their entry's Visible answers false), before the first frame
    /// (X-151, 0.1238: by the window itself, DesktopSurface.HidePages, no reflection on its record any more).</summary>
    /// <summary>X-285: the keys of the texts the page asks, given to <paramref name="listen"/> (null: stop listening).</summary>
    public void ListenKeys(Action<string>? listen)=>localization.GetType().GetProperty("KeyRead",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)!
        .SetValue(localization,listen);

    public void HideInGamePages()=>Surface.HidePages(page=>DesktopPages.Kind(page)==DesktopPageKind.InGame);

    /// <summary>One catalogue loader of the plugin, ticked as the game's framework would until it unsubscribes.</summary>
    public sealed class LoaderRun(string name,FrameworkProxy framework,System.Diagnostics.Stopwatch clock,Action dispose)
    {
        public string Name=>name;
        public int Ticks;
        public string? Result;
        public bool Done=>Result is not null;
        /// <summary>One framework tick; true when the loader is done (its catalogue published or its error logged).</summary>
        public bool Tick()
        {
            if(Done)return true;
            if(framework.Handlers is { } handlers){handlers((Dalamud.Plugin.Services.IFramework)(object)framework);Ticks++;return false;}
            dispose();
            Result=$"{name}: done in {clock.Elapsed.TotalSeconds:0.0} s, {Ticks} framework ticks{(LogProxy.LastError is { } e ? " (error: "+e+")":"")}";
            return true;
        }
    }

    /// <summary>Starts one catalogue loader of the plugin (service, IDataManager, IFramework, IPluginLog[, DataCache]).</summary>
    public LoaderRun StartLoader(string loaderName,string cacheDir)
    {
        var loaderType=Plugin.GetType(loaderName) ?? Plugin.GetTypes().Single(t=>t.Name==loaderName);
        var ctor=loaderType.GetConstructors(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).OrderBy(c=>c.GetParameters().Length).First();
        var framework=FrameworkProxy.Create();
        var args=ctor.GetParameters().Select(p=>p.ParameterType.FullName switch
        {
            "Dalamud.Plugin.Services.IDataManager"=>DataStub,
            "Dalamud.Plugin.Services.IFramework"=>framework,
            "Dalamud.Plugin.Services.IPluginLog"=>LogProxy.Create(),
            "TenshoAssistant.Caching.DataCache"=>Activator.CreateInstance(p.ParameterType,cacheDir),
            _=>Make(p.ParameterType,0),
        }).ToArray();
        var clock=System.Diagnostics.Stopwatch.StartNew();
        // Kept alive: a loader's Dispose clears its service (the plugin disposes it only when unloading).
        loaders.Add(ctor.Invoke(args));
        return new LoaderRun(loaderType.Name,(FrameworkProxy)(object)framework,clock,
            ()=>{foreach(var arg in args.OfType<IDisposable>().Where(a=>a.GetType().FullName=="TenshoAssistant.Caching.DataCache"))arg.Dispose();});
    }

    /// <summary>A value for a constructor parameter: Dalamud stand-ins for interfaces, the plugin's own classes built recursively (one per type).</summary>
    private object? Make(Type type,int depth)
    {
        if(made.TryGetValue(type,out var known))return known;
        object? value;
        if(type.FullName=="Dalamud.Plugin.Services.IDataManager")value=DataStub;
        else if(type.FullName=="Dalamud.Plugin.Services.IFramework")value=FrameworkProxy.Create();
        else if(type.FullName=="Dalamud.Plugin.Services.IPluginLog")value=LogProxy.Create();
        else if(type.FullName=="Dalamud.Plugin.Services.ITextureProvider")value=TextureProviderProxy.Create();
        else if(type.IsInterface)value=AbsentProxy.Create(type);
        else if(typeof(Delegate).IsAssignableFrom(type))value=null;
        else if(type==typeof(string))value="";
        else if(type.IsValueType)value=Activator.CreateInstance(type);
        else if(depth>6)value=null;
        else
        {
            var ctor=type.GetConstructors(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).OrderBy(c=>c.GetParameters().Length).FirstOrDefault();
            try{value=ctor is null ? null : ctor.Invoke(ctor.GetParameters().Select(p=>p.HasDefaultValue && p.ParameterType==typeof(string) ? p.DefaultValue : Make(p.ParameterType,depth+1)).ToArray());}
            catch(Exception e){value=null;AbsentProxy.Note($"(constructor of {type.Name} failed: {(e.InnerException ?? e).GetType().Name}: {(e.InnerException ?? e).Message})");}
        }
        made[type]=value;
        return value;
    }
}
