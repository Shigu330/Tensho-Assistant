namespace TenshoAssistant.Desktop;

/// <summary>
/// X-151 (0.1238, GPT review of 11/11, accepted by the user): the typed contract between the desktop application and the plugin's window.
/// One source file compiled into both (and into the tests): the plugin fills every entry from its own members (MainWindow.DesktopSurfaceMap,
/// « required »: a missing entry or a member whose parameters changed breaks the plugin's build), the application reads them back typed
/// (FromMap). Only delegates made of .NET's own types cross between the two (the plugin is loaded at run time from a rewritten copy, so a
/// type of the plugin or of this file is not the same type on the other side; CoreLib's types are): DesktopSurface1238Tests forbids any
/// other type in an entry. The one name still reached by reflection is MainWindow.DesktopSurfaceMap (DesktopContract); an entry the plugin
/// does not give is said at start and fails Verify's headless run.
/// </summary>
internal sealed class DesktopSurface
{
    /// <summary>The window's pages (MainWindow.Page names), in order.</summary>
    public required Func<string[]> Pages { get; init; }
    /// <summary>The page shown.</summary>
    public required Func<string> CurrentPage { get; init; }
    /// <summary>Shows a page by its name (an unknown name throws).</summary>
    public required Action<string> SetPage { get; init; }
    /// <summary>The pages whose name answers true leave the navigation (their entry is hidden), before the first frame.</summary>
    public required Action<Func<string,bool>> HidePages { get; init; }
    /// <summary>X-119 step 4: the window is drawn by the desktop application (its navigation follows the player's page choices).</summary>
    public required Action DesktopMode { get; init; }
    /// <summary>X-119 step 2: the stores of a category built on the application's copy (folder, category, warning).</summary>
    public required Action<string,string,Action<Exception,string>> UseData { get; init; }
    /// <summary>X-173: the player's data file copied by SQLite's backup (source file, target file) → copied, problems. Any thread.</summary>
    public required Func<string,string,(int Copied,IReadOnlyList<string> Problems)> CopyData { get; init; }
    /// <summary>X-119: the discoveries' database copied the same way. Any thread.</summary>
    public required Func<string,string,(int Copied,IReadOnlyList<string> Problems)> CopyDiscoveries { get; init; }
    /// <summary>X-163: the alarms to ring now (now, game in front) → lines, sound.</summary>
    public required Func<DateTimeOffset,bool,(string[] Lines,bool Sound)> DueAlarms { get; init; }
    /// <summary>X-163: whether the alarms rung also show a Windows notification.</summary>
    public required Func<bool> AlarmNotification { get; init; }
    /// <summary>X-163: the plugin's alerts received by the link (texts, game in front) → lines, sound.</summary>
    public required Func<string[],bool,(string[] Lines,bool Sound)> LinkNotices { get; init; }
    /// <summary>X-163: the held counts of the alarms' items brought by the link (items, counts).</summary>
    public required Action<uint[],long[]> AlarmHeld { get; init; }
    /// <summary>--zone (diagnostic): the weather page of a territory (territory, zone name).</summary>
    public required Action<uint,string> OpenWeather { get; init; }

    /// <summary>The entries by name, as they cross to the application (a dictionary of CoreLib's types only).</summary>
    public IReadOnlyDictionary<string,Delegate> ToMap()=>new Dictionary<string,Delegate>(StringComparer.Ordinal)
    {
        [nameof(Pages)]=Pages,[nameof(CurrentPage)]=CurrentPage,[nameof(SetPage)]=SetPage,[nameof(HidePages)]=HidePages,
        [nameof(DesktopMode)]=DesktopMode,[nameof(UseData)]=UseData,[nameof(CopyData)]=CopyData,[nameof(CopyDiscoveries)]=CopyDiscoveries,
        [nameof(DueAlarms)]=DueAlarms,[nameof(AlarmNotification)]=AlarmNotification,[nameof(LinkNotices)]=LinkNotices,
        [nameof(AlarmHeld)]=AlarmHeld,[nameof(OpenWeather)]=OpenWeather,
    };

    /// <summary>
    /// The entries read back typed. An entry absent, or of another delegate type (a plugin of another version), is listed in « missing »
    /// and stands in as a harmless default (no page, nothing rung, nothing copied), so that part is off and said, never a crash.
    /// </summary>
    public static DesktopSurface FromMap(IReadOnlyDictionary<string,Delegate>? map,out IReadOnlyList<string> missing)
    {
        var absent=new List<string>();
        T Take<T>(string name,T fallback) where T:Delegate
        {
            if(map is not null && map.TryGetValue(name,out var entry) && entry is T typed)return typed;
            absent.Add(map is not null && map.TryGetValue(name,out var other) && other is not null ? $"{name} (another type: {other.GetType()})" : name);
            return fallback;
        }
        var surface=new DesktopSurface
        {
            Pages=Take<Func<string[]>>(nameof(Pages),()=>[]),
            CurrentPage=Take<Func<string>>(nameof(CurrentPage),()=>""),
            SetPage=Take<Action<string>>(nameof(SetPage),_=>{}),
            HidePages=Take<Action<Func<string,bool>>>(nameof(HidePages),_=>{}),
            DesktopMode=Take<Action>(nameof(DesktopMode),()=>{}),
            UseData=Take<Action<string,string,Action<Exception,string>>>(nameof(UseData),(_,category,_)=>throw new NotSupportedException("the plugin gives no "+nameof(UseData)+" for "+category)),
            CopyData=Take<Func<string,string,(int,IReadOnlyList<string>)>>(nameof(CopyData),(_,_)=>(0,["TenshoData.db: the plugin gives no CopyData (another version?)"])),
            CopyDiscoveries=Take<Func<string,string,(int,IReadOnlyList<string>)>>(nameof(CopyDiscoveries),(_,_)=>(0,["Discoveries.db: the plugin gives no CopyDiscoveries (another version?)"])),
            DueAlarms=Take<Func<DateTimeOffset,bool,(string[],bool)>>(nameof(DueAlarms),(_,_)=>([],false)),
            AlarmNotification=Take<Func<bool>>(nameof(AlarmNotification),()=>false),
            LinkNotices=Take<Func<string[],bool,(string[],bool)>>(nameof(LinkNotices),(_,_)=>([],false)),
            AlarmHeld=Take<Action<uint[],long[]>>(nameof(AlarmHeld),(_,_)=>{}),
            OpenWeather=Take<Action<uint,string>>(nameof(OpenWeather),(_,_)=>{}),
        };
        missing=absent;
        return surface;
    }
}
