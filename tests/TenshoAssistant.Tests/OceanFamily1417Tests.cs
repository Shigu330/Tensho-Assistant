// 0.1417 (X-153, ninth slice, GPT review of 11/11): the ocean fishing family out of Plugin.cs. The voyage reader and the play state each have
// exactly one install path and one release path across every Plugin*.cs file, both in Plugin.Ocean.cs, inside the plugin's registrations, in
// the order of before; the voyage data and the timetable are read off the frame there; Plugin.cs calls the family once, at the place the block
// stood (after the sheet loaders, before the inventory window's reader), and creates none of them.
using System.Text.RegularExpressions;

internal static class OceanFamily1417Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static (string Name,string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"Plugin*.cs")
        .Select(path => (Path.GetFileName(path),File.ReadAllText(path).Replace("\r\n","\n"))).ToArray();

    private static int Count(string text,string pattern) => Regex.Matches(text,pattern).Count;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Ocean family 1417: one install and one release path for each, in Plugin.Ocean.cs only, in the order of before; called once at the same place",() =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Ocean.cs").Text;
            var parts = new (string Part,string Install,string Release)[]
            {
                ("voyage reader",@"oceanVoyageReader=new OceanVoyageReader\(",@"\(\)=>oceanVoyageReader\?\.Dispose\(\)\);"),
                ("play state",@"playState=new Diagnostics\.PlayStateWriter\(",@"\(\)=>playState\?\.Dispose\(\)\);"),
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
            Check(home.Contains("mainWindow.OceanVoyageData=InBackground(oceanVoyageData);") && home.Contains("mainWindow.OceanTimetableNow=InBackground(ocean);")
                && home.Contains("var voyageWriter=new BackgroundWriter("),"the sheets off the frame, the record's writer");
            Check(home.IndexOf("InBackground(oceanVoyageData)",StringComparison.Ordinal) < home.IndexOf("oceanVoyageReader=new OceanVoyageReader(",StringComparison.Ordinal),"data before the reader, as before");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            var loaders = plugin.IndexOf("mainWindow.UseSheetLoaders(data,log);",StringComparison.Ordinal);
            var call = plugin.IndexOf("RegisterOcean(",StringComparison.Ordinal);
            // X-153 (0.1897): the inventory window is made by its family, called at the same place.
            var inventory = plugin.IndexOf("RegisterInventoryWindow(configuration,framework,gameGui);",StringComparison.Ordinal);
            Check(loaders > 0 && call > loaders && inventory > call && Count(plugin,@"RegisterOcean\(") == 1,"Plugin.cs calls the family once, where the block stood");
            Check(!Regex.IsMatch(plugin,@"(OceanVoyageReader|PlayStateWriter|IKDSpot|IKDFishParam|OceanVoyageData|voyageWriter)\b"),"Plugin.cs creates none of them");
        }),
    ];
}
