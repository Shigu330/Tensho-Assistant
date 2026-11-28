// 0.1936 (X-153, GPT review of 11/11, accepted by the user): the reminders' family out of Plugin.cs. The preparation reminder's watcher and the
// garden's chat watcher have exactly one install path and one release path across every Plugin*.cs file, both in Plugin.Reminders.cs, inside
// the plugin's registrations, each install given with its own release; Plugin.cs calls the two blocks where they stood and creates neither;
// the discoveries' item watcher is read when a harvest is seen, not copied when the garden's watcher is made.
using System.Text.RegularExpressions;

internal static class RemindersFamily1936Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release)[] Paths =
    [
        ("preparation reminder", @"prepReminders=new Reminders\.PrepReminderWatcher\(", @"prepReminders\?\.Dispose\(\)"),
        ("garden's chat watcher", @"gardenWatcher=new Reminders\.GardenChatWatcher\(", @"gardenWatcher\?\.Dispose\(\)"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Reminders family 1936: one install and one release path for the preparation reminder and the garden's watcher, in Plugin.Reminders.cs only, in the order of before", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Reminders.cs").Text;
            var last = -1;
            foreach (var (service, install, release) in Paths)
            {
                Check(files.Sum(file => Count(file.Text, install)) == 1 && files.Sum(file => Count(file.Text, release)) == 1, service + ": one install and one release path");
                Check(Count(home, install) == 1 && Count(home, release) == 1, service + ": in its family's file");
                var at = Regex.Match(home, install).Index;
                Check(at > last, service + ": in the order of before");
                last = at;
            }
            Check(Count(home, @"registrations\.Register\(") == Paths.Length, "every one through the plugin's registrations, nothing else registered here");
            Check(home.Contains("Func<Discoveries.ItemDiscoveryWatcher?> itemWatcher", StringComparison.Ordinal) && home.Contains("itemWatcher()?.SourceSeen(Discoveries.GainSource.Garden,", StringComparison.Ordinal),
                "the item watcher read when the harvest is seen");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!Regex.IsMatch(plugin, @"new Reminders\.(PrepReminderWatcher|GardenChatWatcher)\("), "Plugin.cs creates neither");
        }),
        ("Reminders family 1936: Plugin.cs calls each block once, where it stood", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var prep = At("RegisterPrepReminders(configuration,data,framework,client,objects,condition,chat);");
            var garden = At("RegisterGardenWatcher(configuration,data,framework,client,targets,gameGui,()=>itemWatcher);");
            Check(Count(plugin, @"RegisterPrepReminders\(") == 1 && Count(plugin, @"RegisterGardenWatcher\(") == 1, "each called once");
            Check(At("()=>gearWear?.Dispose());") < prep && prep < At("mainWindow.GearCatalog=()=>"), "the preparation reminder: after the gear wear, before the gear catalogue, as before");
            Check(At("Discoveries.ItemDiscoveryWatcher? itemWatcher=null;") < garden && At("mainWindow.SquadronMemberName=") < garden && garden < At("mainWindow.SquadronTrainingName="), "the garden: between the squadron's names, after the item watcher, as before");
        }),
        ("Reminders family 1936: each service's install and its release are given to one same registration of Plugin.Reminders.cs (its own release, never another's)", () =>
            FamilyRegistrations.Paired("Reminders family 1936", PluginFiles().Single(file => file.Name == "Plugin.Reminders.cs").Text, Paths.Select(path => (path.Service, path.Install, path.Release)))),
    ];
}
