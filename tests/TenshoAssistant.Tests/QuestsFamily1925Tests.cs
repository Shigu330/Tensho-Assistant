// 0.1925 (X-153, GPT review of 11/11, accepted by the user): the quests' family out of Plugin.cs. The quest reader and the collection reader
// have exactly one install path and one release path across every Plugin*.cs file, both in Plugin.Quests.cs, inside the plugin's
// registrations, each install given with its own release; Plugin.cs calls the block where it stood and creates neither.
using System.Text.RegularExpressions;

internal static class QuestsFamily1925Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release)[] Paths =
    [
        ("quest reader", @"questReader = new QuestReader\(", @"questReader\?\.Dispose\(\)"),
        ("collection reader", @"collectionReader = new CollectionReader\(", @"collectionReader\?\.Dispose\(\)"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Quests family 1925: one install and one release path for the quest and collection readers, in Plugin.Quests.cs only, in the order of before", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Quests.cs").Text;
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
            Check(home.Contains("new CompletionListsData(tenshoData)", StringComparison.Ordinal), "the completion lists kept in Tensho's data file");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!Regex.IsMatch(plugin, @"new (QuestReader|CollectionReader)\("), "Plugin.cs creates neither");
        }),
        ("Quests family 1925: Plugin.cs calls it once, where its block stood", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var call = At("RegisterQuests(configuration,data,framework,player,client,unlocks,tenshoData);");
            Check(Count(plugin, @"RegisterQuests\(") == 1, "called once");
            Check(At("RegisterFashion(configuration,data,framework,gameGui);") < call && call < At("registrations.Register(() => windows.AddWindow(mainWindow)"), "after the fashion family, before the windows, as before");
        }),
        ("Quests family 1925: each service's install and its release are given to one same registration of Plugin.Quests.cs (its own release, never another's)", () =>
            FamilyRegistrations.Paired("Quests family 1925", PluginFiles().Single(file => file.Name == "Plugin.Quests.cs").Text, Paths.Select(path => (path.Service, path.Install, path.Release)))),
    ];
}
