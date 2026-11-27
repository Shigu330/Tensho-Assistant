// 0.1896 (X-153, GPT review of 11/11, accepted by the user): the hunting family out of Plugin.cs. The S rank kills watcher and the hunting
// log reader have exactly one install path and one release path across every Plugin*.cs file, both in Plugin.Hunting.cs, inside the
// plugin's registrations; Plugin.cs calls each where its block stood and creates none; the main window still reads the hunting log.
// Pairing (X-885 M2, 0.1906): each service's install and its release given to one same registration.
using System.Text.RegularExpressions;
using TenshoAssistant;

internal static class HuntingFamily1896Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release)[] Paths =
    [
        ("S rank kills", @"srankKills=new Hunting\.SRankKillWatcher\(", @"srankKills\?\.Dispose\(\)"),
        ("hunting log", @"huntReader=new Hunting\.HuntingLogReader\(", @"huntReader\?\.Dispose\(\)"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Hunting family 1896: one install and one release path for the S rank kills and the hunting log, in Plugin.Hunting.cs only", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Hunting.cs").Text;
            foreach (var (service, install, release) in Paths)
            {
                Check(files.Sum(file => Count(file.Text, install)) == 1 && files.Sum(file => Count(file.Text, release)) == 1, service + ": one install and one release path");
                Check(Count(home, install) == 1 && Count(home, release) == 1, service + ": in its family's file");
            }
            Check(Count(home, @"registrations\.Register\(") == Paths.Length, "every one through the plugin's registrations");
            Check(home.Contains("mainWindow.HuntSlots=()=>{huntReader?.Want();return huntReader?.State;};", StringComparison.Ordinal), "the main window still reads the hunting log");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!Regex.IsMatch(plugin, @"new Hunting\.(SRankKillWatcher|HuntingLogReader)\("), "Plugin.cs creates none of them");
        }),
        ("Hunting family 1896: Plugin.cs calls each once, where its block stood", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var kills = At("RegisterSRankKills(configuration,framework,client,objects,chat,()=>SaveTimed(\"S rank kills\"));");
            var log = At("RegisterHuntingLog(framework,client);");
            Check(Count(plugin, @"RegisterSRankKills\(") == 1 && Count(plugin, @"RegisterHuntingLog\(") == 1, "each called once");
            Check(At("mainWindow.HomeRegion=()=>") < kills && kills < At("var characterJobs=new Character.CharacterJobsData(tenshoData);"), "the S rank kills after the home region, before the characters' jobs, as before");
            Check(At("catch(Exception error){log.Warning(error,\"Monster sheets not read ahead") < log && log < At("mainWindow.LeveProgress=()=>"), "the hunting log after the monster sheets, before the leves, as before");
        }),
        // X-885 M2 (0.1906): this case replaced « behaviour », the same RegistrationScope test copied in each family test, which touched nothing
        // of the family; each service's install and its release are now checked given to one same registration of Plugin.Hunting.cs.
        ("Hunting family 1896: each service's install and its release are given to one same registration of Plugin.Hunting.cs (its own release, never another's)", () =>
            FamilyRegistrations.Paired("Hunting family 1896", PluginFiles().Single(file => file.Name == "Plugin.Hunting.cs").Text, Paths.Select(path => (path.Service, path.Install, path.Release)))),
    ];
}
