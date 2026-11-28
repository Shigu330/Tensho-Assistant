// 0.1933 (X-153, GPT review of 11/11, accepted by the user): the macros' family out of Plugin.cs. The macro writer, the Macro Chain bridge and
// the macros' observation reader have exactly one install path and one release path across every Plugin*.cs file, all in Plugin.Macros.cs,
// inside the plugin's registrations, each install given with its own release; Plugin.cs calls the two blocks where they stood and creates
// none of them.
using System.Text.RegularExpressions;

internal static class MacrosFamily1933Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release)[] Paths =
    [
        ("macro writer", @"macroWriter=new Macros\.MacroWriter\(", @"macroWriter\?\.Dispose\(\)"),
        ("Macro Chain bridge", @"macroBridge = new MacroChainBridge\(", @"macroBridge\?\.Dispose\(\)"),
        ("macros' observation reader", @"macroObserver = new MacroObservationReader\(", @"macroObserver\?\.Dispose\(\)"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Macros family 1933: one install and one release path for the writer, the Macro Chain bridge and the observation reader, in Plugin.Macros.cs only, in the order of before", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Macros.cs").Text;
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
            Check(home.Contains("mainWindow.ConfigureMacroWriter(macroWriter!);", StringComparison.Ordinal), "the writer given to the window");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!Regex.IsMatch(plugin, @"new (Macros\.MacroWriter|MacroChainBridge|MacroObservationReader)\("), "Plugin.cs creates none of them");
        }),
        ("Macros family 1933: Plugin.cs calls each block once, where it stood", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var writer = At("RegisterMacroWriter(framework,chat);");
            var readers = At("RegisterMacroReaders(framework,player,client,commands,interop);");
            Check(Count(plugin, @"RegisterMacroWriter\(") == 1 && Count(plugin, @"RegisterMacroReaders\(") == 1, "each called once");
            Check(At("RegisterGameMap(") < writer && writer < At("var discord=RegisterAlarms("), "the writer: after the game map, before the alarms, as before");
            Check(At("sortBridge=new InventoryVisualSortBridge(") < readers && readers < At("hoverReader = new ItemHoverReader("), "the readers: after the visual sort, before the item hover, as before");
        }),
        ("Macros family 1933: each service's install and its release are given to one same registration of Plugin.Macros.cs (its own release, never another's)", () =>
            FamilyRegistrations.Paired("Macros family 1933", PluginFiles().Single(file => file.Name == "Plugin.Macros.cs").Text, Paths.Select(path => (path.Service, path.Install, path.Release)))),
    ];
}
