// 0.1895 (X-153, GPT review of 11/11, accepted by the user): the field content family out of Plugin.cs. The shared FATE ranks and the
// Occult Crescent's state have exactly one install path and one release path across every Plugin*.cs file, both in Plugin.Fields.cs,
// inside the plugin's registrations; Plugin.cs calls each where its block stood and creates none; the main window still reads them.
// Behaviour: the save handed to a family is the caller's own, called by the service, not at install.
using System.Text.RegularExpressions;
using TenshoAssistant;

internal static class FieldsFamily1895Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release)[] Paths =
    [
        ("shared FATE ranks", @"sharedFates=new Fields\.SharedFateReader\(", @"sharedFates\?\.Dispose\(\)"),
        ("Occult Crescent", @"occultReader=new Fields\.OccultCrescentReader\(", @"occultReader\?\.Dispose\(\)"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Fields family 1895: one install and one release path for the shared FATEs and the Occult Crescent, in Plugin.Fields.cs only", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Fields.cs").Text;
            foreach (var (service, install, release) in Paths)
            {
                Check(files.Sum(file => Count(file.Text, install)) == 1 && files.Sum(file => Count(file.Text, release)) == 1, service + ": one install and one release path");
                Check(Count(home, install) == 1 && Count(home, release) == 1, service + ": in its family's file");
            }
            Check(Count(home, @"registrations\.Register\(") == Paths.Length, "every one through the plugin's registrations");
            Check(home.Contains("mainWindow.SharedFatesNow=()=>sharedFates?.Shown;", StringComparison.Ordinal)
                && home.Contains("mainWindow.OccultStateRead=()=>occultReader?.State;", StringComparison.Ordinal), "the main window still reads them");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!Regex.IsMatch(plugin, @"new Fields\.(SharedFateReader|OccultCrescentReader)\("), "Plugin.cs creates none of them");
        }),
        ("Fields family 1895: Plugin.cs calls each once, where its block stood", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var fates = At("RegisterSharedFates(configuration,framework,client,player,gameGui,()=>SaveTimed(\"shared FATEs\"));");
            var occult = At("RegisterOccultCrescent(framework);");
            Check(Count(plugin, @"RegisterSharedFates\(") == 1 && Count(plugin, @"RegisterOccultCrescent\(") == 1, "each called once");
            Check(At("var (gear,gameGearsets,graceWatcher)=RegisterGear(") < fates && fates < At("mainWindow.GraceSource=()=>graceWatcher?.Shown;"), "the shared FATEs after the gear, as before");
            Check(At("mainWindow.GameDataCheck=()=>gameDataCheck;") < occult && occult < At("mainWindow.GcFeminine=()=>framework.RunOnFrameworkThread("), "the Occult Crescent after the game data check, before the Grand Companies, as before");
        }),
        ("Fields family 1895: behaviour: the save handed to a family is the caller's, called by the service only", () =>
        {
            var saved = new List<string>();
            void SaveTimed(string what) => saved.Add(what);
            Action? kept = null;
            var scope = new RegistrationScope();
            void Family(Action save) => scope.Register(() => kept = save, () => kept = null);
            Family(() => SaveTimed("shared FATEs"));
            Check(saved.Count == 0 && kept is not null, "nothing saved at install");
            kept!();
            Check(saved.SequenceEqual(["shared FATEs"]), "the caller's save, with its name");
            scope.Dispose();
            Check(kept is null, "released at unload");
        }),
    ];
}
