// 0.1927 (X-153, GPT review of 11/11, accepted by the user): the mini-games' family out of Plugin.cs. The Triple Triad NPCs' loader, the Mini
// Cactpot reader and the Triple Triad reader have exactly one install path and one release path across every Plugin*.cs file, all in
// Plugin.MiniGames.cs, inside the plugin's registrations, each install given with its own release; Plugin.cs calls the two blocks where they
// stood and creates none of them.
using System.Text.RegularExpressions;

internal static class MiniGamesFamily1927Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release)[] Paths =
    [
        ("Triple Triad NPCs' loader", @"triadNpcLoader=new TriadNpcLoader\(", @"triadNpcLoader\?\.Dispose\(\)"),
        ("Mini Cactpot reader", @"cactpotReader=new MiniCactpotObservationReader\(", @"cactpotReader\?\.Dispose\(\)"),
        ("Triple Triad reader", @"triadReader = new TriadObservationReader\(", @"triadReader\?\.Dispose\(\)"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Mini-games family 1927: one install and one release path for each mini-game reader, in Plugin.MiniGames.cs only, in the order of before", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.MiniGames.cs").Text;
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
            Check(home.Contains("mainWindow.ConfigureTriadNpcs(triadNpcs);", StringComparison.Ordinal), "the NPCs given to the window");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!Regex.IsMatch(plugin, @"new (TriadNpcService|TriadNpcLoader|MiniCactpotObservationReader|TriadObservationReader)\("), "Plugin.cs creates none of them");
        }),
        ("Mini-games family 1927: Plugin.cs calls each block once, where it stood", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var npcs = At("RegisterTriadNpcs(data,framework,client,player,unlocks);");
            var readers = At("RegisterMiniGameReaders(framework,player,client,gameGui,targets,data);");
            Check(Count(plugin, @"RegisterTriadNpcs\(") == 1 && Count(plugin, @"RegisterMiniGameReaders\(") == 1, "each called once");
            Check(At("RegisterDutyFinder(configuration,framework,gameGui);") < npcs && npcs < At("RegisterBeastCatalog("), "the NPCs: after the Duty Finder, before the Beastmaster's catalogue, as before");
            Check(At("mainWindow.EurekaChanceFor=") < readers && readers < At("ItemUseCatalogLoader? usageLoader=null;"), "the readers: after the gathering helpers, before the item uses' loader, as before");
        }),
        ("Mini-games family 1927: each service's install and its release are given to one same registration of Plugin.MiniGames.cs (its own release, never another's)", () =>
            FamilyRegistrations.Paired("Mini-games family 1927", PluginFiles().Single(file => file.Name == "Plugin.MiniGames.cs").Text, Paths.Select(path => (path.Service, path.Install, path.Release)))),
    ];
}
