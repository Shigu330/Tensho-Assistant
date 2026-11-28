// 0.1897 (X-153, GPT review of 11/11, accepted by the user): the inventory family out of Plugin.cs, first slice. The storage windows'
// highlight, the game's inventory window and the gear sets read on request have exactly one install path and one release path across every
// Plugin*.cs file, all in Plugin.Inventory.cs, inside the plugin's registrations; Plugin.cs calls each where its block stood and creates
// none; the main window still reads them. Pairing (X-885 M2, 0.1906): each service's install and its release given to one same registration.
using System.Text.RegularExpressions;
using TenshoAssistant;

internal static class InventoryFamily1897Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release, string Call)[] Paths =
    [
        ("storage highlight", @"storageHighlight=new Inventory\.StorageHighlightReader\(", @"storageHighlight\?\.Dispose\(\)", "RegisterStorageHighlight(configuration,framework,gameGui,inventory);"),
        ("inventory window", @"inventoryWindow=new Inventory\.InventoryWindowReader\(", @"inventoryWindow\?\.Dispose\(\)", "RegisterInventoryWindow(configuration,framework,gameGui);"),
        ("gear sets", @"gearsets=new Inventory\.GearsetReader\(", @"gearsets\?\.Dispose\(\)", "RegisterGearsets(framework,player);"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Inventory family 1897: one install and one release path for the storage highlight, the inventory window and the gear sets, in Plugin.Inventory.cs", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Inventory.cs").Text;
            var last = -1;
            foreach (var (service, install, release, _) in Paths)
            {
                Check(files.Sum(file => Count(file.Text, install)) == 1 && files.Sum(file => Count(file.Text, release)) == 1, service + ": one install and one release path");
                Check(Count(home, install) == 1 && Count(home, release) == 1, service + ": in its family's file");
                var at = Regex.Match(home, install).Index;
                Check(at > last, service + ": in the order of before");
                last = at;
            }
            // 0.1898: the second slice follows in the same file (InventoryFamily1898Tests); these three come first.
            var first = home[..home.IndexOf("RegisterCurrencies(", StringComparison.Ordinal)];
            Check(Count(first, @"registrations\.Register\(") == Paths.Length, "every one through the plugin's registrations");
            Check(home.Contains("mainWindow.InventoryWindow=inventoryWindow;", StringComparison.Ordinal) && home.Contains("mainWindow.Gearsets=()=>gearsets;", StringComparison.Ordinal), "the main window still reads them");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!Regex.IsMatch(plugin, @"new Inventory\.(StorageHighlightReader|InventoryWindowReader|GearsetReader)\("), "Plugin.cs creates none of them");
        }),
        ("Inventory family 1897: Plugin.cs calls each once, where its block stood, in the order of before", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var calls = Paths.Select(path => At(path.Call)).ToArray();
            Check(Paths.All(path => plugin.Split(path.Call).Length == 2), "each called once");
            Check(calls.SequenceEqual(calls.Order()), "in the order of before");
            Check(At("mainWindow.BeastWiki=beastWiki;") < calls[0] && calls[0] < At("var (discoveries,discoveryFolder)=RegisterDiscoveries(pluginInterface);"), "the highlight after the beast wiki, before the discoveries, as before");
            Check(At("RegisterOcean(pluginInterface,configuration,data,framework,client,condition);") < calls[1] && calls[1] < At("RegisterGearWear(configuration,framework,client,condition,chat,()=>gear?.Equipped ?? []);"), "the inventory window after the ocean fishing, before the gear wear, as before");
            Check(At("mainWindow.Festivals=()=>festivals;") < calls[2] && calls[2] < At("(uint Id,string Name,uint Icon)[]? keyItems=null;"), "the gear sets after the festivals, before the key items, as before");
        }),
        // X-885 M2 (0.1906): this case replaced « behaviour », the same RegistrationScope test copied in each family test, which touched nothing
        // of the family; each service's install and its release are now checked given to one same registration of Plugin.Inventory.cs.
        ("Inventory family 1897: each service's install and its release are given to one same registration of Plugin.Inventory.cs (its own release, never another's)", () =>
            FamilyRegistrations.Paired("Inventory family 1897", PluginFiles().Single(file => file.Name == "Plugin.Inventory.cs").Text, Paths.Select(path => (path.Service, path.Install, path.Release)))),
    ];
}
