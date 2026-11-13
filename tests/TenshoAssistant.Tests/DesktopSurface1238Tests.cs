using System.Reflection;
using TenshoAssistant.Desktop;

// 0.1238 (X-151, GPT review of 11/11): the typed contract between the desktop application and the plugin's window. One source file
// (DesktopSurface) compiled into both; the plugin fills every entry from its own members, the application reads them back typed. Here: the
// round trip keeps every entry and calls through; an entry absent or of another type is said and stands in harmlessly; only .NET's own
// types cross (a type of the plugin would not be the same type in the application); the host no longer reaches the window's members by
// their names (one name left, DesktopSurfaceMap), and Verify's headless run fails on a missing entry.
internal static class DesktopSurface1238Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static readonly PropertyInfo[] Entries=typeof(DesktopSurface).GetProperties(BindingFlags.Instance|BindingFlags.Public).Where(p=>typeof(Delegate).IsAssignableFrom(p.PropertyType)).ToArray();

    private static DesktopSurface Recording(List<string> calls)=>new()
    {
        Pages=()=>["Home","Weather"],
        CurrentPage=()=>"Home",
        SetPage=name=>calls.Add("page "+name),
        HidePages=rule=>calls.Add("hide "+rule("Maps")),
        DesktopMode=()=>calls.Add("desktop"),
        UseData=(folder,category,_)=>calls.Add($"use {folder} {category}"),
        CopyData=(source,target)=>(1,[source+">"+target]),
        CopyDiscoveries=(source,target)=>(2,[]),
        DueAlarms=(now,front)=>(["ring "+now.Year],!front),
        AlarmNotification=()=>true,
        LinkNotices=(texts,front)=>(texts,front),
        AlarmHeld=(items,counts)=>calls.Add($"held {items.Length} {counts.Length}"),
        OpenWeather=(territory,zone)=>calls.Add($"weather {territory} {zone}"),
    };

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Desktop surface 1238: every entry crosses and is called through, typed, with nothing said missing",() =>
        {
            var calls=new List<string>();
            var map=Recording(calls).ToMap();
            Check(Entries.Length==13 && map.Count==Entries.Length && Entries.All(entry=>map.ContainsKey(entry.Name)),"every entry is in the map: "+string.Join(",",map.Keys));
            var surface=DesktopSurface.FromMap(map,out var missing);
            Check(missing.Count==0,"nothing missing: "+string.Join("; ",missing));
            Check(surface.Pages().SequenceEqual(["Home","Weather"]) && surface.CurrentPage()=="Home","pages");
            surface.SetPage("Weather");surface.HidePages(page=>page=="Maps");surface.DesktopMode();surface.UseData("m","bank",(_,_)=>{});
            surface.AlarmHeld([1,2],[3,4]);surface.OpenWeather(155,"Coerthas");
            Check(calls.SequenceEqual(["page Weather","hide True","desktop","use m bank","held 2 2","weather 155 Coerthas"]),"calls: "+string.Join(" | ",calls));
            Check(surface.CopyData("a","b") is (1,[ "a>b" ]) && surface.CopyDiscoveries("a","b").Copied==2,"copies");
            var (lines,sound)=surface.DueAlarms(new DateTimeOffset(2026,11,13,0,0,0,TimeSpan.Zero),false);
            Check(lines.SequenceEqual(["ring 2026"]) && sound && surface.AlarmNotification() && surface.LinkNotices(["x"],true) is ([ "x" ],true),"alarms and notices");
        }),
        ("Desktop surface 1238: an entry absent or of another type is said and stands in harmlessly; no map at all: every entry said",() =>
        {
            var map=new Dictionary<string,Delegate>(Recording([]).ToMap());
            map.Remove(nameof(DesktopSurface.DueAlarms));
            map[nameof(DesktopSurface.Pages)]=(Func<int>)(()=>3);
            var surface=DesktopSurface.FromMap(map,out var missing);
            Check(missing.Count==2 && missing.Contains("DueAlarms") && missing.Any(item=>item.StartsWith("Pages (another type: ",StringComparison.Ordinal)),"said: "+string.Join("; ",missing));
            Check(surface.Pages().Length==0 && surface.DueAlarms(DateTimeOffset.UtcNow,false) is ([],false),"harmless stand-ins");
            Check(surface.CurrentPage()=="Home","the others are kept");
            var none=DesktopSurface.FromMap(null,out var all);
            Check(all.Count==Entries.Length,"no map: every entry said ("+all.Count+")");
            Check(none.CopyData("a","b") is (0,[var problem]) && problem.Contains("another version",StringComparison.Ordinal),"no copy, said why");
            Check(!none.AlarmNotification() && none.LinkNotices(["x"],false) is ([],false) && none.CurrentPage()=="","nothing rung, no page");
            var threw=false;try{none.UseData("m","bank",(_,_)=>{});}catch(NotSupportedException){threw=true;}
            Check(threw,"no store built from nothing: the host says « not read »");
        }),
        ("Desktop surface 1238: only .NET's own types cross between the plugin and the application",() =>
        {
            var core=typeof(object).Assembly;
            IEnumerable<Type> All(Type type)=>type.IsGenericType ? new[]{type.GetGenericTypeDefinition()}.Concat(type.GetGenericArguments().SelectMany(All))
                : type.IsArray ? All(type.GetElementType()!) : [type];
            foreach(var entry in Entries)
                foreach(var type in All(entry.PropertyType))
                    Check(type.Assembly==core,$"{entry.Name}: {type} is not one of .NET's own types (it would not be the same type in the application)");
            Check(typeof(DesktopSurface).GetMethod(nameof(DesktopSurface.ToMap))!.ReturnType==typeof(IReadOnlyDictionary<string,Delegate>),"the map is a dictionary of .NET's own types");
            Check(Entries.All(entry=>entry.GetCustomAttributes().Any(attribute=>attribute.GetType().Name=="RequiredMemberAttribute")),"every entry is required: the plugin cannot leave one out");
        }),
        ("Desktop surface 1238: the plugin fills every entry from its members; the host reaches one name only; Verify and the start say a missing entry",() =>
        {
            var window=Read("src/TenshoAssistant/MainWindow.DesktopData.cs");
            Check(window.Contains("internal IReadOnlyDictionary<string,Delegate> DesktopSurfaceMap()=>new Desktop.DesktopSurface"),"the plugin's one entry");
            foreach(var entry in Entries)Check(window.Contains($"        {entry.Name}="),"filled by the plugin: "+entry.Name);
            Check(window.Contains("pageEntries=[.. PageEntries.Select(entry=>hidden(entry.Page.ToString()) ? entry with { Visible=()=>false } : entry)];") && window.Contains("pageGroups=null;"),"the window hides its own entries");
            var host=Read("src/TenshoAssistant.Desktop/Host.cs");
            foreach(var gone in new[]{"\"UseDesktopData\"","\"DesktopDueAlarms\"","\"DesktopAlarmNotification\"","\"DesktopLinkNotices\"","\"DesktopAlarmHeld\"","\"DesktopMode\"","\"CopyForDesktop\"","\"CopyDiscoveriesForDesktop\"","<Clone>$","k__BackingField","\"pageEntries\"","\"page\""})
                Check(!host.Contains(gone,StringComparison.Ordinal),"the host no longer reaches "+gone+" by name");
            Check(Read("src/TenshoAssistant.Desktop/Headless.cs").Contains("host.Surface.OpenWeather(territory,name);"),"the zone diagnostic is typed too");
            Check(host.Contains("GetMethod(\"DesktopSurfaceMap\",BindingFlags.Instance|BindingFlags.NonPublic,Type.EmptyTypes)") && host.Contains("Surface=DesktopSurface.FromMap(map,out SurfaceMissing);"),"one name, then typed");
            Check(DesktopContract.Members.Any(member=>member.Name=="DesktopSurfaceMap" && member.Parameters is { Length:0 }),"that name is in the written contract");
            var headless=Read("src/TenshoAssistant.Desktop/Headless.cs");
            var fail=headless.IndexOf("if(host.SurfaceMissing.Count>0){report.Fail(\"typed contract: the plugin does not give \"+string.Join(\"; \",host.SurfaceMissing));return 1;}",StringComparison.Ordinal);
            Check(fail>0 && fail<headless.IndexOf("var worker=new Thread(",StringComparison.Ordinal),"Verify's headless run fails on a missing entry, before any page is drawn");
            Check(Read("src/TenshoAssistant.Desktop/Program.cs").Contains("foreach(var missing in host.SurfaceMissing)Console.WriteLine(\"Typed contract: the plugin does not give \"+missing+\"; that part is off\");"),"said at start");
            Check(Read("src/TenshoAssistant/TenshoAssistant.csproj").Contains("..\\TenshoAssistant.Desktop\\DesktopSurface.cs") && Read("tests/TenshoAssistant.Tests/TenshoAssistant.Tests.csproj").Contains("../../src/TenshoAssistant.Desktop/DesktopSurface.cs"),"one source, compiled into the plugin, the application and the tests");
        }),
    ];
}
