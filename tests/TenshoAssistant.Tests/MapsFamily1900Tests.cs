// 0.1900 (X-153, GPT review of 11/11, accepted by the user): the rest of the map family out of Plugin.cs, after its live reader and
// positions (0.1404). The game map's replacement, the housing furniture probe and the treasure map reader have exactly one install path and
// one release path across every Plugin*.cs file, all in Plugin.Maps.cs, inside the plugin's registrations; Plugin.cs calls each where its
// block stood and creates none; the treasure map reader is handed back, the discoveries' item watcher reads it.
using System.Text.RegularExpressions;
using TenshoAssistant;

internal static class MapsFamily1900Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release)[] Paths =
    [
        ("game map replacement", @"mapReplacer=new Maps\.NativeMapReplacer\(", @"mapReplacer\?\.Dispose\(\)"),
        ("housing furniture probe", @"housingProbe=new Maps\.HousingFurnitureProbe\(", @"housingProbe\?\.Dispose\(\)"),
        ("treasure map reader", @"treasureMap=new Maps\.TreasureMapReader\(", @"treasureMap\?\.Dispose\(\)"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Maps family 1900: one install and one release path for the game map, the housing probe and the treasure map, in Plugin.Maps.cs", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Maps.cs").Text;
            var last = -1;
            foreach (var (service, install, release) in Paths)
            {
                Check(files.Sum(file => Count(file.Text, install)) == 1 && files.Sum(file => Count(file.Text, release)) == 1, service + ": one install and one release path");
                Check(Count(home, install) == 1 && Count(home, release) == 1, service + ": in its family's file");
                var at = Regex.Match(home, install).Index;
                Check(at > last, service + ": in the order of before");
                last = at;
            }
            Check(Count(home, @"registrations\.Register\(") == 2 + Paths.Length, "every one through the plugin's registrations (with the live reader's two)");
            Check(home.Contains("return treasureMap;", StringComparison.Ordinal) && home.Contains("mainWindow.ConfigureGameMapOpener(()=>mapReplacer?.OpenGameMap());", StringComparison.Ordinal), "the reader handed back, the main window still wired");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!Regex.IsMatch(plugin, @"new Maps\.(NativeMapReplacer|HousingFurnitureProbe|TreasureMapReader)\("), "Plugin.cs creates none of them");
        }),
        ("Maps family 1900: Plugin.cs calls each once, where its block stood; the item watcher reads the treasure map handed back", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var game = At("RegisterGameMap(configuration,framework,gameGui,client,condition);");
            var housing = At("RegisterHousingProbe(configuration,framework,client,objects);");
            var treasure = At("var treasureMap=RegisterTreasureMap(framework,client,data);");
            Check(new[] { "RegisterGameMap", "RegisterHousingProbe", "RegisterTreasureMap" }.All(name => Count(plugin, name + @"\(") == 1), "each called once");
            Check(At("mainWindow.IslandAnimalBookSource=islandAnimalBook;") < game && game < At("Macros.MacroWriter? macroWriter=null;"), "the game map after the island's animal book, before the macro writer, as before");
            Check(At("communityHelp=new Help.CommunityHelpIndex()") < housing && housing < At("RegisterBeastBoard(configuration,data,framework,gameGui,objects);"), "the housing probe after the community help, before the beast board, as before");
            Check(At("mainWindow.LeveDefinitions=LeveList;") < treasure && treasure < At("if(itemWatcher is not null)itemWatcher.TreasureMapItem=()=>treasureMap?.Spot"), "the treasure map after the leves' list, before the item watcher reads it");
        }),
        // X-885 M2 (0.1906): this case replaced « behaviour », the same RegistrationScope test copied in each family test, which touched nothing
        // of the family; each service's install and its release are now checked given to one same registration of Plugin.Maps.cs.
        ("Maps family 1900: each service's install and its release are given to one same registration of Plugin.Maps.cs (its own release, never another's)", () =>
            FamilyRegistrations.Paired("Maps family 1900", PluginFiles().Single(file => file.Name == "Plugin.Maps.cs").Text, Paths.Select(path => (path.Service, path.Install, path.Release)))),
    ];
}
