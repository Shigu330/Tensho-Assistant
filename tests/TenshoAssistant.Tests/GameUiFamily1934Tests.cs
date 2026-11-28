// 0.1934 (X-153, GPT review of 11/11, accepted by the user): the game interface's readers out of Plugin.cs. The game's theme reader, the game's
// windows' reader and the game window follower have exactly one install path and one release path across every Plugin*.cs file, all in
// Plugin.GameUi.cs, inside the plugin's registrations, each install given with its own release (what the window and the bank overlay read
// from them let go with them); Plugin.cs calls the two blocks where they stood and creates none of them.
using System.Text.RegularExpressions;

internal static class GameUiFamily1934Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release)[] Paths =
    [
        ("game's theme reader", @"themeReader=new UI\.GameThemeReader\(", @"themeReader\?\.Dispose\(\)"),
        ("game's windows' reader", @"gameWindows=new UI\.GameWindowsReader\(", @"gameWindows\?\.Dispose\(\)"),
        ("game window follower", @"gameWindowFollower=new UI\.GameWindowFollower\(", @"gameWindowFollower\?\.Dispose\(\)"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Game UI family 1934: one install and one release path for the theme, windows and follower readers, in Plugin.GameUi.cs only, in the order of before", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.GameUi.cs").Text;
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
            Check(home.Contains("()=>{mainWindow.GameWindowSource=null;gameWindows?.Dispose();}", StringComparison.Ordinal)
                && home.Contains("()=>{mainWindow.GameWindowFollow=null;bankOverlay.Follow=null;gameWindowFollower?.Dispose();}", StringComparison.Ordinal), "what the window and the bank overlay hold is let go with the readers");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!Regex.IsMatch(plugin, @"new UI\.(GameThemeReader|GameWindowsReader|GameWindowFollower)\("), "Plugin.cs creates none of them");
        }),
        ("Game UI family 1934: Plugin.cs calls each block once, where it stood", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var theme = At("RegisterGameTheme(gameConfig);");
            var windows = At("RegisterGameWindows(framework,client,gameGui);");
            Check(Count(plugin, @"RegisterGameTheme\(") == 1 && Count(plugin, @"RegisterGameWindows\(") == 1, "each called once");
            Check(At("mainWindow.ConfigureDataCache(dataCache);") < theme && theme < At("wikiSources.Dispose"), "the theme: after the data cache, before the wiki's sources, as before");
            Check(At("RegisterGatheringReaders(") < windows && windows < At("RegisterGatherKnowledge("), "the windows: between the gathering readers and the gather knowledge, as before");
        }),
        ("Game UI family 1934: each service's install and its release are given to one same registration of Plugin.GameUi.cs (its own release, never another's)", () =>
            FamilyRegistrations.Paired("Game UI family 1934", PluginFiles().Single(file => file.Name == "Plugin.GameUi.cs").Text, Paths.Select(path => (path.Service, path.Install, path.Release)))),
    ];
}
