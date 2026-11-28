// 0.1941 (X-153, GPT review of 11/11, accepted by the user): the discoveries' item watcher out of Plugin.cs. The watcher has exactly one
// install path and one release path across every Plugin*.cs file, both in Plugin.ItemWatcher.cs, inside the plugin's registrations, the
// install given with its own release; Plugin.cs calls it once where its block stood, creates none, and gives the watcher handed back its
// sources afterwards, as before.
using System.Text.RegularExpressions;

internal static class ItemWatcherFamily1941Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private const string Install = @"itemWatcher=new Discoveries\.ItemDiscoveryWatcher\(", Release = @"itemWatcher\?\.Dispose\(\)";
    private const string Call = "var itemWatcher=RegisterItemWatcher(configuration,data,framework,client,gameGui,objects,targets,condition,inventory,discoveries,chat);";

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Item watcher family 1941: one install and one release path for the discoveries' item watcher, in Plugin.ItemWatcher.cs only; handed back", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.ItemWatcher.cs").Text;
            Check(files.Sum(file => Count(file.Text, Install)) == 1 && files.Sum(file => Count(file.Text, Release)) == 1, "one install and one release path");
            Check(Count(home, Install) == 1 && Count(home, Release) == 1 && Count(home, @"registrations\.Register\(") == 1, "in its family's file, through the plugin's registrations");
            Check(home.Contains("return itemWatcher;", StringComparison.Ordinal), "the watcher handed back");
            Check(home.Contains("()=>configuration.Discovery,()=>configuration.DeveloperMode && configuration.Discovery.DebugLog,", StringComparison.Ordinal), "its settings and debug switch, as before");
            Check(!files.Single(file => file.Name == "Plugin.cs").Text.Contains("new Discoveries.ItemDiscoveryWatcher(", StringComparison.Ordinal), "Plugin.cs creates none");
        }),
        ("Item watcher family 1941: Plugin.cs calls it once, where its block stood, before it gives the watcher its sources", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var call = At(Call);
            Check(Count(plugin, @"RegisterItemWatcher\(") == 1, "called once");
            Check(At("mainWindow.VentureRowOf=") < call && call < At("var seenPrices=new Market.SeenPriceStore();"), "after the ventures' rows, before the prices seen, as before");
            Check(call < At("if(itemWatcher is not null){itemWatcher.Beasts=") && call < At("RegisterGardenWatcher(configuration,data,framework,client,targets,gameGui,()=>itemWatcher);"), "before its sources and the garden read it");
        }),
        ("Item watcher family 1941: the install and its release are given to one same registration of Plugin.ItemWatcher.cs", () =>
            FamilyRegistrations.Paired("Item watcher family 1941", PluginFiles().Single(file => file.Name == "Plugin.ItemWatcher.cs").Text, [("discoveries' item watcher", Install, Release)])),
    ];
}
