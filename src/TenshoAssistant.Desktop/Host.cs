using System.Globalization;
using System.Numerics;
using System.Reflection;
using Dalamud.Bindings.ImGui;
using Lumina;
using Lumina.Data;
using Lumina.Data.Files;
using TenshoAssistant.Desktop;

/// <summary>
/// The plugin's real main window (TenshoAssistant.MainWindow from the built DLL), built with its real constructor: services the plugin
/// creates are created the same way, Dalamud services are stand-ins. IDataManager, IFramework and IPluginLog as in tools/ClientSnapshot;
/// ITextureProvider reads the game icon (.tex) through Lumina and uploads it to the desktop renderer; any other Dalamud interface is an
/// "absent" stand-in that answers default values and records each member the page code reached (X-118's inventory evidence).
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
    public static Func<int,int,PixelPointer,ulong>? Upload;
    public static readonly Dictionary<uint,(ulong Id,int W,int H)?> Icons=[];
    public static GameData? IconData;

    /// <summary>The plugin's catalogue loaders that need only (service, IDataManager, IFramework, IPluginLog[, DataCache]) (Plugin.cs).</summary>
    public static readonly string[] Loaders=["MapCatalogLoader","WeatherCatalogLoader","DutyCatalogLoader","GpCatalogLoader","CraftActionCatalogLoader","FashionHintLoader",
        "ItemUseCatalogLoader","ItemCatalogLoader","RecipeCatalogLoader","GatheringCatalogLoader","NpcVendorLoader","MapLocalIndexLoader"];

    public Host(string pluginPath,string sqpack,string? gameLanguage,string? textLanguage,string? settingsPath)
    {
        Plugin=System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(pluginPath);
        var pluginDirectory=Path.GetDirectoryName(pluginPath)!;
        var configurationType=Plugin.GetType("TenshoAssistant.Configuration",true)!;
        var saved=ReadSettings(settingsPath,configurationType,out SettingsNote);
        if(saved is not null)made[configurationType]=saved;
        var savedCode=saved is null ? null : configurationType.GetProperty("LanguageCode")?.GetValue(saved) as string;
        TextCode=DesktopPaths.TextLanguage(textLanguage ?? savedCode,CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        GameLanguage=Enum.Parse<Language>(gameLanguage ?? DesktopPaths.GameLanguage(TextCode),true);
        Data=new GameData(sqpack,new LuminaOptions{DefaultExcelLanguage=GameLanguage,CacheFileResources=false,PanicOnSheetChecksumMismatch=false});
        IconData=Data;
        DataStub=new DataManagerStub(Data,GameLanguage);
        // Dalamud's scale is set by its interface manager; 1 here (no DPI scaling yet).
        typeof(Dalamud.Interface.Utility.ImGuiHelpers).GetProperty("GlobalScale",BindingFlags.Static|BindingFlags.Public)!.GetSetMethod(true)?.Invoke(null,[1f]);
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
            var settings=new Newtonsoft.Json.JsonSerializerSettings
            {
                TypeNameHandling=Newtonsoft.Json.TypeNameHandling.Objects,
                TypeNameAssemblyFormatHandling=Newtonsoft.Json.TypeNameAssemblyFormatHandling.Simple,
                SerializationBinder=new PluginBinder(Plugin),
            };
            var value=Newtonsoft.Json.JsonConvert.DeserializeObject(File.ReadAllText(path),configurationType,settings);
            note=value is null ? "new (saved settings empty)" : "read from "+path+" (read-only)";
            return value;
        }
        catch(Exception error){note=$"new (saved settings unreadable: {error.GetType().Name}: {error.Message})";return null;}
    }

    /// <summary>Type names of the saved settings: the plugin's own from the loaded plugin (its copy), the others as Newtonsoft does.</summary>
    private sealed class PluginBinder(Assembly plugin):Newtonsoft.Json.Serialization.DefaultSerializationBinder
    {
        public override Type BindToType(string? assemblyName,string typeName)
            => assemblyName=="TenshoAssistant" && plugin.GetType(typeName) is { } type ? type : base.BindToType(assemblyName,typeName);
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

    /// <summary>A game icon as Dalamud would give it: ui/icon/XXX000/XXXXXX_hr1.tex (high resolution, else the normal one), read by Lumina.</summary>
    public static unsafe (ulong Id,int W,int H)? Icon(uint icon)
    {
        if(Icons.TryGetValue(icon,out var known))return known;
        (ulong,int,int)? result=null;
        var folder=$"ui/icon/{icon/1000*1000:000000}/{icon:000000}";
        var tex=IconData?.GetFile<TexFile>(folder+"_hr1.tex") ?? IconData?.GetFile<TexFile>(folder+".tex");
        if(tex is not null)
        {
            var bgra=tex.ImageData;
            fixed(byte* pixels=bgra)
                result=Upload is { } upload ? (upload(tex.Header.Width,tex.Header.Height,new PixelPointer(pixels)),tex.Header.Width,tex.Header.Height) : (icon,tex.Header.Width,tex.Header.Height);
        }
        Icons[icon]=result;
        return result;
    }
}

internal readonly unsafe struct PixelPointer(byte* pointer){public readonly byte* Pointer=pointer;}

/// <summary>IDataManager stand-in, as tools/ClientSnapshot (a real class: DispatchProxy cannot emit GetExcelSheet's self-referencing constraint).</summary>
public sealed class DataManagerStub(GameData data,Language language):Dalamud.Plugin.Services.IDataManager
{
    private static Language? Lumina(Dalamud.Game.ClientLanguage? given)=>given is { } value ? Enum.Parse<Language>(value.ToString()) : null;
    public Dalamud.Game.ClientLanguage Language=>Enum.Parse<Dalamud.Game.ClientLanguage>(language.ToString());
    public GameData GameData=>data;
    public Lumina.Excel.ExcelModule Excel=>data.Excel;
    public bool HasModifiedGameDataFiles=>false;
    public Lumina.Excel.ExcelSheet<T> GetExcelSheet<T>(Dalamud.Game.ClientLanguage? language=null,string? name=null) where T:struct,Lumina.Excel.IExcelRow<T>
        =>data.Excel.GetSheet<T>(Lumina(language) ?? Current,name);
    public Lumina.Excel.SubrowExcelSheet<T> GetSubrowExcelSheet<T>(Dalamud.Game.ClientLanguage? language=null,string? name=null) where T:struct,Lumina.Excel.IExcelSubrow<T>
        =>data.Excel.GetSubrowSheet<T>(Lumina(language) ?? Current,name);
    private Language Current=>language;
    public FileResource? GetFile(string path)=>data.GetFile(path);
    public T? GetFile<T>(string path) where T:FileResource=>data.GetFile<T>(path);
    public Task<T> GetFileAsync<T>(string path,CancellationToken cancellationToken) where T:FileResource=>Task.Run(()=>data.GetFile<T>(path)!,cancellationToken);
    public bool FileExists(string path)=>data.FileExists(path);
}

/// <summary>IFramework stand-in: always on the framework thread; keeps the Update handlers so the host can tick them.</summary>
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
        AbsentProxy.Note("IFramework."+method.Name);
        return method.ReturnType.IsValueType && method.ReturnType!=typeof(void) ? Activator.CreateInstance(method.ReturnType):null;
    }
}

/// <summary>IPluginLog stand-in: warnings and errors printed and counted.</summary>
public class LogProxy:DispatchProxy
{
    public static int Warnings;public static string? LastError;
    public static Dalamud.Plugin.Services.IPluginLog Create()=>Create<Dalamud.Plugin.Services.IPluginLog,LogProxy>();
    protected override object? Invoke(MethodInfo? method,object?[]? args)
    {
        var name=method!.Name;
        if(name.StartsWith("get_",StringComparison.Ordinal))return method.ReturnType.IsValueType ? Activator.CreateInstance(method.ReturnType):null;
        var text=string.Join(" | ",(args ?? []).Select(a=>a switch{Exception e=>e.GetType().Name+": "+e.Message,object[] list=>string.Join(",",list),_=>a?.ToString()}));
        if(name is "Warning"){Warnings++;if(Environment.GetEnvironmentVariable("PROBE_LOG") is not null)Console.WriteLine("  log warning: "+text);}
        else if(name is "Error" or "Fatal"){LastError=text;Console.WriteLine("  log error: "+text);}
        return null;
    }
}

/// <summary>Any other Dalamud interface: default answers (false, 0, null, empty), each member reached recorded for the inventory.</summary>
public class AbsentProxy:DispatchProxy
{
    public static readonly HashSet<string> Reached=[];
    public static void Note(string member){lock(Reached)Reached.Add(member);}
    public string Name="";
    public static object? Create(Type type)
    {
        try{var proxy=DispatchProxy.Create(type,typeof(AbsentProxy));((AbsentProxy)proxy).Name=type.Name;return proxy;}
        catch(Exception e){Note($"({type.Name}: no stand-in, {e.GetType().Name})");return null;}
    }
    protected override object? Invoke(MethodInfo? method,object?[]? args)
    {
        var name=method!.Name;
        if(!name.StartsWith("add_",StringComparison.Ordinal) && !name.StartsWith("remove_",StringComparison.Ordinal))Note(Name+"."+name);
        var parameters=method.GetParameters();
        for(var i=0;i<parameters.Length;i++)
            if(parameters[i].IsOut)args![i]=parameters[i].ParameterType.GetElementType() is { IsValueType:true } element ? Activator.CreateInstance(element):null;
        var type=method.ReturnType;
        if(type==typeof(void))return null;
        if(type==typeof(string))return "";
        if(type.IsValueType)return Activator.CreateInstance(type);
        if(type.IsArray)return Array.CreateInstance(type.GetElementType()!,0);
        if(type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() is var g && (g==typeof(IEnumerable<>) || g==typeof(IReadOnlyList<>) || g==typeof(IReadOnlyCollection<>)))
            return Array.CreateInstance(type.GetGenericArguments()[0],0);
        return null;
    }
}

/// <summary>ITextureProvider stand-in: game icons from Lumina (.tex), uploaded to the desktop renderer; anything else recorded as absent.</summary>
public class TextureProviderProxy:DispatchProxy
{
    public static Dalamud.Plugin.Services.ITextureProvider Create()=>Create<Dalamud.Plugin.Services.ITextureProvider,TextureProviderProxy>();
    public static int Served,Missing;
    protected override object? Invoke(MethodInfo? method,object?[]? args)
    {
        switch(method!.Name)
        {
            case "TryGetFromGameIcon":
            case "GetFromGameIcon":
                var lookup=(Dalamud.Interface.Textures.GameIconLookup)args![0]!;
                var shared=SharedTextureProxy.Create(lookup.IconId);
                if(method.Name=="GetFromGameIcon")return shared;
                args[1]=shared;return true;
        }
        AbsentProxy.Note("ITextureProvider."+method.Name);
        return method.ReturnType==typeof(bool) ? false : method.ReturnType.IsValueType && method.ReturnType!=typeof(void) ? Activator.CreateInstance(method.ReturnType):null;
    }
}

public class SharedTextureProxy:DispatchProxy
{
    private uint icon;
    public static Dalamud.Interface.Textures.ISharedImmediateTexture Create(uint icon)
    {var proxy=Create<Dalamud.Interface.Textures.ISharedImmediateTexture,SharedTextureProxy>();((SharedTextureProxy)(object)proxy).icon=icon;return proxy;}
    protected override object? Invoke(MethodInfo? method,object?[]? args)
    {
        var loaded=Host.Icon(icon);
        if(method!.Name is "TryGetWrap")
        {
            args![1]=null;
            if(loaded is not { } found){args[0]=null;TextureProviderProxy.Missing++;return false;}
            TextureProviderProxy.Served++;
            args[0]=WrapProxy.Create(found.Id,found.W,found.H);return true;
        }
        if(method.Name is "GetWrapOrEmpty" or "GetWrapOrDefault")return loaded is { } f ? WrapProxy.Create(f.Id,f.W,f.H) : null;
        AbsentProxy.Note("ISharedImmediateTexture."+method.Name);
        return method.ReturnType.IsValueType && method.ReturnType!=typeof(void) ? Activator.CreateInstance(method.ReturnType):null;
    }
}

public class WrapProxy:DispatchProxy
{
    private ulong id;private int w,h;
    public static Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap Create(ulong id,int w,int h)
    {var proxy=Create<Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap,WrapProxy>();var self=(WrapProxy)(object)proxy;self.id=id;self.w=w;self.h=h;return proxy;}
    protected override object? Invoke(MethodInfo? method,object?[]? args)=>method!.Name switch
    {
        "get_Handle"=>(ImTextureID)id,"get_Width"=>w,"get_Height"=>h,"get_Size"=>new Vector2(w,h),"Dispose"=>null,
        _=>method.ReturnType.IsValueType && method.ReturnType!=typeof(void) ? Activator.CreateInstance(method.ReturnType):null,
    };
}
