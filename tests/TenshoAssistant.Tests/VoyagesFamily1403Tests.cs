// 0.1403 (X-153, sixth slice, GPT review of 11/11): the company voyages family out of Plugin.cs. The voyage watcher, the airship voyage table
// and the submarine functions probe each have exactly one install path and one release path across every Plugin*.cs file, both in
// Plugin.Voyages.cs, inside the plugin's registrations, in the order of before; the voyage history is created there once; Plugin.cs calls the
// family once, at the place the block stood (after the duty pop alert, before the gear loader), and creates none of them.
using System.Text.RegularExpressions;

internal static class VoyagesFamily1403Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static (string Name,string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"Plugin*.cs")
        .Select(path => (Path.GetFileName(path),File.ReadAllText(path).Replace("\r\n","\n"))).ToArray();

    private static int Count(string text,string pattern) => Regex.Matches(text,pattern).Count;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Voyages family 1403: one install and one release path for each, in Plugin.Voyages.cs only, in the order of before; called once at the same place",() =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Voyages.cs").Text;
            var parts = new (string Part,string Install,string Release)[]
            {
                ("voyage watcher",@"voyages=new FreeCompany\.VoyageWatcher\(",@"\(\)=>voyages\?\.Dispose\(\)\);"),
                ("airship voyage table",@"airshipTable=new FreeCompany\.AirshipVoyageTable\(",@"\(\)=>airshipTable\?\.Dispose\(\)\);"),
                ("submarine functions probe",@"submarineProbe=new FreeCompany\.SubmarineVoyageProbe\(",@"\(\)=>submarineProbe\?\.Dispose\(\)\);"),
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
            Check(Count(home,@"registrations\.Register\(") == 3,"through the plugin's registrations");
            Check(files.Sum(file => Count(file.Text,@"new FreeCompany\.VoyageHistoryStore\(")) == 1 && Count(home,@"new FreeCompany\.VoyageHistoryStore\(") == 1,"the history created once, with them");
            Check(home.Contains("mainWindow.VoyageHistorySource=voyageHistory;") && home.Contains("mainWindow.VoyageReadUnavailable=()=>voyages?.ReadUnavailable==true;")
                && home.Contains("mainWindow.AirshipTableFor=speed=>airshipTable?.TableFor(speed);"),"the main window still reads them");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            var pop = plugin.IndexOf("registrations.Register(()=>client.CfPop+=OnDutyPop,()=>client.CfPop-=OnDutyPop);",StringComparison.Ordinal);
            var call = plugin.IndexOf("RegisterVoyages(",StringComparison.Ordinal);
            var gear = plugin.IndexOf("registrations.Register(()=>gear=new Gear.GearLoader(",StringComparison.Ordinal);
            Check(pop > 0 && call > pop && gear > call && Count(plugin,@"RegisterVoyages\(") == 1,"Plugin.cs calls the family once, where the block stood");
            Check(!Regex.IsMatch(plugin,@"FreeCompany\.(VoyageWatcher|AirshipVoyageTable|SubmarineVoyageProbe|VoyageHistoryStore)\b"),"Plugin.cs creates none of them");
        }),
    ];
}
