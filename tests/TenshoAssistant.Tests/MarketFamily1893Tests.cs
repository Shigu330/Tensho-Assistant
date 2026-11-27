// 0.1893 (X-153, GPT review of 11/11, accepted by the user): the rest of the market family out of Plugin.cs, after its retainer market
// (0.1887). The world reader, the prices seen reader and the undercut watcher have exactly one install path and one release path across
// every Plugin*.cs file, all in Plugin.Market.cs, inside the plugin's registrations; Plugin.cs calls each where it stood and creates none.
// Behaviour: a failing install releases those made before it, never itself, and the unload releases the others in reverse order.
using System.Text.RegularExpressions;
using TenshoAssistant;

internal static class MarketFamily1893Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release, string Call)[] Paths =
    [
        ("world reader", @"marketReader = new MarketWorldReader\(", @"marketReader\?\.Dispose\(\)", "RegisterMarketWorld(framework,player,client,data);"),
        ("prices seen reader", @"seenReader=new Market\.SeenPriceReader\(", @"seenReader\?\.Dispose\(\)", "RegisterSeenPrices(configuration,marketBoardEvents,seenPrices,bankBridge);"),
        ("undercut watcher", @"undercut=new Market\.UndercutWatcher\(", @"undercut\?\.Dispose\(\)", "RegisterUndercut(configuration,marketBoardEvents,framework,chat,data);"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Market family 1893: one install and one release path for the world reader, the prices seen and the undercut watcher, in Plugin.Market.cs", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Market.cs").Text;
            foreach (var (service, install, release, _) in Paths)
            {
                Check(files.Sum(file => Count(file.Text, install)) == 1 && files.Sum(file => Count(file.Text, release)) == 1, service + ": one install and one release path");
                Check(Count(home, install) == 1 && Count(home, release) == 1, service + ": in its family's file");
            }
            Check(Count(home, @"registrations\.Register\(") == 4 + Paths.Length, "every one through the plugin's registrations (with the retainer market's four)");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!plugin.Contains("new MarketWorldReader(", StringComparison.Ordinal) && !plugin.Contains("new Market.SeenPriceReader(", StringComparison.Ordinal)
                && !plugin.Contains("new Market.UndercutWatcher(", StringComparison.Ordinal), "Plugin.cs creates none of them");
        }),
        ("Market family 1893: Plugin.cs calls each once, where the service was made, in the order of before", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            var calls = Paths.Select(path => plugin.IndexOf(path.Call, StringComparison.Ordinal)).ToArray();
            Check(calls.All(at => at > 0) && Paths.All(path => plugin.Split(path.Call).Length == 2), "each called once");
            Check(calls.SequenceEqual(calls.Order()), "in the order of before");
            Check(calls[0] < plugin.IndexOf("RegisterPersistence(pluginInterface,configuration,data,inventory,gameGui,player,client,framework);", StringComparison.Ordinal), "the world reader before the persistence, as before");
            Check(calls[1] > plugin.IndexOf("var seenPrices=new Market.SeenPriceStore();", StringComparison.Ordinal)
                && calls[1] < plugin.IndexOf("LoadDiscoveries(discoveryFolder,discoveries,seenPrices,framework,chat);", StringComparison.Ordinal), "the prices seen: after their store, before the discoveries load");
            Check(calls[2] > plugin.IndexOf("()=>shopReminder?.Dispose());", StringComparison.Ordinal)
                && calls[2] < plugin.IndexOf("RegisterDesktop(pluginInterface,configuration,framework,client,player,inventory,objects);", StringComparison.Ordinal), "the undercut watcher: after the shop reminder, before the desktop");
            Check(plugin.Contains("(\"UndercutClipboard\",()=>undercut?.FlushClipboard())", StringComparison.Ordinal), "the undercut watcher's field still read by the clipboard flush");
        }),
        ("Market family 1893: behaviour: a failing install releases those made before it, never itself; the unload releases in reverse order", () =>
        {
            var released = new List<string>();
            var scope = new RegistrationScope();
            scope.Register(() => { }, () => released.Add("world"));
            scope.Register(() => { }, () => released.Add("seen"));
            var failed = false;
            try { scope.Register(() => throw new InvalidOperationException("install"), () => released.Add("undercut")); }
            catch (InvalidOperationException) { failed = true; }
            Check(failed, "the failing install is said");
            Check(!released.Contains("undercut"), "a service whose install failed is not released");
            scope.Dispose();
            Check(released.SequenceEqual(["seen", "world"]), "the others released once, in reverse order: " + string.Join(",", released));
        }),
    ];
}
