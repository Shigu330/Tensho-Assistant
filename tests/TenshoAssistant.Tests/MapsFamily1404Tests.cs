// 0.1404 (X-153, seventh slice, GPT review of 11/11): the map family out of Plugin.cs. The live map reader and the map positions' loader each
// have exactly one install path and one release path across every Plugin*.cs file, both in Plugin.Maps.cs, inside the plugin's registrations,
// in the order of before; the markers source and the positions' index are created there once, the map view configured there; Plugin.cs calls
// the family once, at the place the block stood (after the English place names, before the item sources), and creates none of them.
using System.Text.RegularExpressions;

internal static class MapsFamily1404Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static (string Name,string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"Plugin*.cs")
        .Select(path => (Path.GetFileName(path),File.ReadAllText(path).Replace("\r\n","\n"))).ToArray();

    private static int Count(string text,string pattern) => Regex.Matches(text,pattern).Count;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Maps family 1404: one install and one release path for each reader, in Plugin.Maps.cs only, in the order of before; called once at the same place",() =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Maps.cs").Text;
            var parts = new (string Part,string Install,string Release)[]
            {
                ("live map reader",@"mapLive=new Maps\.MapLiveReader\(",@"\(\)=>mapLive\?\.Dispose\(\)\);"),
                ("map positions' loader",@"mapLocalLoader=new Maps\.MapLocalIndexLoader\(",@"\(\)=>mapLocalLoader\?\.Dispose\(\)\);"),
            };
            var last = -1;
            foreach (var (part,install,release) in parts)
            {
                Check(files.Sum(file => Count(file.Text,install)) == 1 && files.Sum(file => Count(file.Text,release)) == 1,part + ": one install and one release path");
                Check(Count(home,install) == 1 && Count(home,release) == 1,part + ": in its family's file");
                var at = Regex.Match(home,install).Index;
                Check(at > last,part + ": created in the order of before");
                last = at;
            }
            Check(Count(home,@"registrations\.Register\(") == 2,"through the plugin's registrations");
            foreach (var made in new[] { @"new Maps\.MapMarkerSource\(", @"new Maps\.MapLocalIndexService\(", @"mainWindow\.ConfigureMapView\(" })
                Check(files.Sum(file => Count(file.Text,made)) == 1 && Count(home,made) == 1,made + ": once, in the family");
            Check(files.Sum(file => Count(file.Text,@"private Maps\.MapLiveReader\? mapLive;")) == 1 && home.Contains("private Maps.MapLiveReader? mapLive;"),"the live reader is the family's field");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            var names = plugin.IndexOf("mainWindow.EnglishPlaceIds=",StringComparison.Ordinal);
            var call = plugin.IndexOf("RegisterMaps(",StringComparison.Ordinal);
            var sources = plugin.IndexOf("var itemSources=new Community.GarlandItemSourceService(",StringComparison.Ordinal);
            Check(names > 0 && call > names && sources > call && Count(plugin,@"RegisterMaps\(") == 1,"Plugin.cs calls the family once, where the block stood");
            Check(!Regex.IsMatch(plugin,@"new Maps\.(MapLiveReader|MapLocalIndexLoader|MapLocalIndexService|MapMarkerSource)\(") && !plugin.Contains("Maps.MapLiveReader? mapLive=null;"),
                "Plugin.cs creates none of them");
            Check(plugin.Contains("mapLive?.State"),"the later readers still ask the live reader");
        }),
    ];
}
