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
    private readonly Type pageType;
    private readonly FieldInfo pageField;
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
        pageType=mainType.GetNestedType("Page",BindingFlags.NonPublic)!;
        pageField=mainType.GetField("page",BindingFlags.Instance|BindingFlags.NonPublic)!;
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
        // X-119 step 4 (0.1158): the window knows it is drawn by the desktop application (its navigation follows the player's page choices).
        mainType.GetMethod("DesktopMode",BindingFlags.Instance|BindingFlags.NonPublic,Type.EmptyTypes)?.Invoke(Window,null);
        // Data the plugin fills after building the window (Plugin.cs): the lexicon, read off the frame from the sheets.
        if(Plugin.GetType("TenshoAssistant.Lexicon.LexiconLoader")?.GetMethod("Read",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic) is { } read
           && mainType.GetProperty("LexiconLoad",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic) is { } lexicon)
            lexicon.SetValue(Window,Delegate.CreateDelegate(lexicon.PropertyType,DataStub,read));
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
    /// X-119 step 2, part 2 and X-151 (0.1131): the plugin's stores of a category built on the application's copy of the plugin's folder,
    /// by the plugin's own MainWindow.UseDesktopData (the one entry, called by its name; DesktopData1131Tests checks it exists). On the UI
    /// thread, between frames. A note in English for the log.
    /// </summary>
    public string UseData(string mirror,string category)
    {
        var use=Window.GetType().GetMethod("UseDesktopData",BindingFlags.Instance|BindingFlags.NonPublic,[typeof(string),typeof(string),typeof(Action<Exception,string>)]);
        if(use is null)return $"{category}: the plugin has no UseDesktopData (another version?)";
        try{use.Invoke(Window,[mirror,category,(Action<Exception,string>)((error,message)=>Console.WriteLine($"  {category}: {message} ({error.GetType().Name})"))]);return $"{category}: read from the copy";}
        catch(TargetInvocationException error){return $"{category}: not read ({error.InnerException?.GetType().Name}: {error.InnerException?.Message})";}
    }

    /// <summary>
    /// X-119 with X-173 (0.1147): the player's data file (TenshoData.db) copied into the mirror by the plugin's own
    /// TenshoDataDatabase.CopyForDesktop (SQLite's backup, the plugin's file read-only; called by its name, DesktopData1147Tests). Any thread.
    /// </summary>
    public (int Copied,IReadOnlyList<string> Problems) CopyData(string source,string mirror)
    {
        copyData??=Plugin.GetType("TenshoAssistant.TenshoDataDatabase")?.GetMethod("CopyForDesktop",BindingFlags.Static|BindingFlags.Public,[typeof(string),typeof(string)]);
        if(copyData is null)return (0,["TenshoData.db: the plugin has no CopyForDesktop (another version?)"]);
        try
        {
            if(copyData.Invoke(null,[Path.Combine(source,"TenshoData.db"),Path.Combine(mirror,"TenshoData.db")]) is System.Runtime.CompilerServices.ITuple { Length:2 } result)
                return ((int)result[0]!,(IReadOnlyList<string>)result[1]!);
            return (0,["TenshoData.db: no answer from CopyForDesktop"]);
        }
        catch(TargetInvocationException error){return (0,[$"TenshoData.db: not copied ({error.InnerException?.GetType().Name}: {error.InnerException?.Message})"]);}
    }
    private MethodInfo? copyData;

    /// <summary>
    /// X-119 (0.1152): the discoveries' database (Discoveries/Discoveries.db, that file only) copied into the mirror by the plugin's own
    /// TenshoDataDatabase.CopyDiscoveriesForDesktop (SQLite's backup, the plugin's file read-only; called by its name). Any thread.
    /// </summary>
    public (int Copied,IReadOnlyList<string> Problems) CopyDiscoveries(string source,string mirror)
    {
        copyDiscoveries??=Plugin.GetType("TenshoAssistant.TenshoDataDatabase")?.GetMethod("CopyDiscoveriesForDesktop",BindingFlags.Static|BindingFlags.Public,[typeof(string),typeof(string)]);
        if(copyDiscoveries is null)return (0,["Discoveries.db: the plugin has no CopyDiscoveriesForDesktop (another version?)"]);
        try
        {
            if(copyDiscoveries.Invoke(null,[Path.Combine(source,"Discoveries","Discoveries.db"),Path.Combine(mirror,"Discoveries","Discoveries.db")]) is System.Runtime.CompilerServices.ITuple { Length:2 } result)
                return ((int)result[0]!,(IReadOnlyList<string>)result[1]!);
            return (0,["Discoveries.db: no answer from CopyDiscoveriesForDesktop"]);
        }
        catch(TargetInvocationException error){return (0,[$"Discoveries.db: not copied ({error.InnerException?.GetType().Name}: {error.InnerException?.Message})"]);}
    }
    private MethodInfo? copyDiscoveries;

    /// <summary>X-163 (0.1132): the alarms to ring now, from the plugin's MainWindow.DesktopDueAlarms (called by its name); none when it is missing.</summary>
    public (string[] Lines,bool Sound) DueAlarms(DateTimeOffset now,bool gameInFront)
    {
        due??=Window.GetType().GetMethod("DesktopDueAlarms",BindingFlags.Instance|BindingFlags.NonPublic,[typeof(DateTimeOffset),typeof(bool)]);
        if(due?.Invoke(Window,[now,gameInFront]) is not System.Runtime.CompilerServices.ITuple { Length:2 } result)return ([],false);
        return ((string[])result[0]!,(bool)result[1]!);
    }
    private MethodInfo? due;

    /// <summary>X-163 (0.1154): whether the alarms rung also show a Windows notification, from MainWindow.DesktopAlarmNotification (by its name); false when it is missing.</summary>
    public bool AlarmNotification()
    {
        notification??=Window.GetType().GetMethod("DesktopAlarmNotification",BindingFlags.Instance|BindingFlags.NonPublic,Type.EmptyTypes);
        return notification?.Invoke(Window,null) is true;
    }
    private MethodInfo? notification;

    /// <summary>X-163 (0.1155): the plugin's alerts received by the link, as MainWindow.DesktopLinkNotices rings them (by its name); none when it is missing.</summary>
    public (string[] Lines,bool Sound) LinkNotices(string[] texts,bool gameInFront)
    {
        notices??=Window.GetType().GetMethod("DesktopLinkNotices",BindingFlags.Instance|BindingFlags.NonPublic,[typeof(string[]),typeof(bool)]);
        if(notices?.Invoke(Window,[texts,gameInFront]) is not System.Runtime.CompilerServices.ITuple { Length:2 } result)return ([],false);
        return ((string[])result[0]!,(bool)result[1]!);
    }
    private MethodInfo? notices;

    /// <summary>X-163 (0.1156): the held counts of the alarms' items brought by the link, to MainWindow.DesktopAlarmHeld (by its name); nothing when it is missing.</summary>
    public void AlarmHeld(uint[] items,long[] counts)
    {
        held??=Window.GetType().GetMethod("DesktopAlarmHeld",BindingFlags.Instance|BindingFlags.NonPublic,[typeof(uint[]),typeof(long[])]);
        held?.Invoke(Window,[items,counts]);
    }
    private MethodInfo? held;

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

    public IEnumerable<string> Pages=>Enum.GetNames(pageType);
    public void SetPage(string name)=>pageField.SetValue(Window,Enum.Parse(pageType,name));
    public string CurrentPage=>pageField.GetValue(Window)!.ToString()!;
    public object? Service(string fullName)=>made.FirstOrDefault(pair=>pair.Key.FullName==fullName).Value;

    /// <summary>A text of the plugin's language files, in the plugin's language (LocalizationManager.Format).</summary>
    public string Text(string key,params object[] args)=>(string)format.Invoke(localization,[key,args])!;
    public bool HasText(string key)=>(bool)has.Invoke(localization,[key])!;

    /// <summary>The pages that stay in the game leave the navigation (their entry's Visible answers false), before the first frame.</summary>
    public void HideInGamePages()
    {
        var mainType=Window.GetType();
        var entries=(Array)mainType.GetProperty("PageEntries",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(Window)!;
        var copy=Array.CreateInstance(entries.GetType().GetElementType()!,entries.Length);
        for(var i=0;i<entries.Length;i++)
        {
            var entry=entries.GetValue(i)!;
            var page=entry.GetType().GetProperty("Page")!.GetValue(entry)!.ToString()!;
            if(DesktopPages.Kind(page)==DesktopPageKind.InGame)
            {
                entry=entry.GetType().GetMethod("<Clone>$")!.Invoke(entry,null)!;
                entry.GetType().GetField("<Visible>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(entry,(Func<bool>)(()=>false));
            }
            copy.SetValue(entry,i);
        }
        mainType.GetField("pageEntries",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(Window,copy);
        mainType.GetField("pageGroups",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(Window,null);
    }

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
