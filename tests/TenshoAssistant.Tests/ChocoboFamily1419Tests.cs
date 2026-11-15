// 0.1419 (X-153, tenth slice, GPT review of 11/11): the chocobo family out of Plugin.cs. The companion chocobo's reader and the racing
// chocobo's reader each have exactly one install path and one release path across every Plugin*.cs file, both in Plugin.Chocobo.cs, inside
// the plugin's registrations, in the order of before; Plugin.cs calls the family once, at the place the block stood (after the gear lines,
// before the dyes' names), and creates none of them.
using System.Text.RegularExpressions;

internal static class ChocoboFamily1419Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static (string Name,string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"Plugin*.cs")
        .Select(path => (Path.GetFileName(path),File.ReadAllText(path).Replace("\r\n","\n"))).ToArray();

    private static int Count(string text,string pattern) => Regex.Matches(text,pattern).Count;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chocobo family 1419: one install and one release path for each, in Plugin.Chocobo.cs only, in the order of before; called once at the same place",() =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Chocobo.cs").Text;
            var parts = new (string Part,string Install,string Release)[]
            {
                ("companion chocobo",@"chocobo=new Gear\.ChocoboReader\(",@"\(\)=>chocobo\?\.Dispose\(\)\);"),
                ("racing chocobo",@"raceChocobo=new MiniGames\.RaceChocoboReader\(",@"\(\)=>raceChocobo\?\.Dispose\(\)\);"),
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
            Check(home.Contains("mainWindow.Chocobo=()=>{chocobo?.Want();return chocobo?.State;};") && home.Contains("mainWindow.RaceChocobo=()=>{raceChocobo?.Want();return raceChocobo?.State;};")
                && home.Contains("mainWindow.RaceChocoboRanks=new Lazy<MiniGames.RaceChocoboRank[]>(()=>LoadSheet(\"ChocoboRaceRank\"") && home.Contains("mainWindow.RaceChocoboAbility=id=>"),
                "the main window still reads them");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            var gear = plugin.IndexOf("mainWindow.EquippedGear=()=>gear?.Equipped ?? [];",StringComparison.Ordinal);
            var call = plugin.IndexOf("RegisterChocobo(",StringComparison.Ordinal);
            var stain = plugin.IndexOf("mainWindow.StainName=id=>",StringComparison.Ordinal);
            Check(gear > 0 && call > gear && stain > call && Count(plugin,@"RegisterChocobo\(") == 1,"Plugin.cs calls the family once, where the block stood");
            Check(!Regex.IsMatch(plugin,@"(ChocoboReader|RaceChocoboReader|ChocoboRaceRank|ChocoboRaceAbility|raceChocobo)\b"),"Plugin.cs creates none of them");
        }),
    ];
}
