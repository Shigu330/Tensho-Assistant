// 0.1926 (X-153, GPT review of 11/11, accepted by the user): the gathering readers out of Plugin.cs. The gathering window's reader, the
// spearfishing reader, the collectables' reader, the action bar locator and the gather knowledge reader have exactly one install path and one
// release path across every Plugin*.cs file, all in Plugin.GatheringReaders.cs, inside the plugin's registrations, each install given with
// its own release; Plugin.cs calls the two blocks where they stood and creates none of them.
using System.Text.RegularExpressions;

internal static class GatheringReadersFamily1926Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release)[] Paths =
    [
        ("gathering window's reader", @"gatheringReader=new GatheringObservationReader\(", @"gatheringReader\?\.Dispose\(\)"),
        ("spearfishing reader", @"spearfishingReader=new SpearfishingReader\(", @"spearfishingReader\?\.Dispose\(\)"),
        ("collectables' reader", @"collectableReader=new CollectableReader\(", @"collectableReader\?\.Dispose\(\)"),
        ("action bar locator", @"barLocator=new Gathering\.ActionBarLocator\(", @"\bbarLocator\?\.Dispose\(\)"),
        ("gather knowledge reader", @"gatherKnowledge=new Gathering\.GatherKnowledgeReader\(", @"gatherKnowledge\?\.Dispose\(\)"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Gathering readers family 1926: one install and one release path for each gathering reader, in Plugin.GatheringReaders.cs only, in the order of before", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.GatheringReaders.cs").Text;
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
            Check(home.Contains("()=>{mainWindow.BarLocator=null;barLocator?.Dispose();}", StringComparison.Ordinal)
                && home.Contains("()=>{mainWindow.GatherAccess=null;mainWindow.GatherKind=null;gatherKnowledge?.Dispose();}", StringComparison.Ordinal), "what the window holds is let go with the reader");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!Regex.IsMatch(plugin, @"new (GatheringObservationReader|SpearfishingReader|CollectableReader|Gathering\.ActionBarLocator|Gathering\.GatherKnowledgeReader)\("), "Plugin.cs creates none of them");
        }),
        ("Gathering readers family 1926: Plugin.cs calls each block once, where it stood", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var readers = At("RegisterGatheringReaders(data,framework,player,client,gameGui,objects,targets);");
            var knowledge = At("RegisterGatherKnowledge(data,framework,client,player);");
            Check(Count(plugin, @"RegisterGatheringReaders\(") == 1 && Count(plugin, @"RegisterGatherKnowledge\(") == 1, "each called once");
            Check(At("RegisterQuests(configuration,") < readers && readers < At("RegisterGameWindows(framework,client,gameGui);") && At("RegisterGameWindows(framework,client,gameGui);") < knowledge
                && knowledge < At("mainWindow.ItemUnlockState="), "around the game windows' readers, as before");
        }),
        ("Gathering readers family 1926: each service's install and its release are given to one same registration of Plugin.GatheringReaders.cs (its own release, never another's)", () =>
            FamilyRegistrations.Paired("Gathering readers family 1926", PluginFiles().Single(file => file.Name == "Plugin.GatheringReaders.cs").Text, Paths.Select(path => (path.Service, path.Install, path.Release)))),
    ];
}
