// 0.1940 (X-153, GPT review of 11/11, accepted by the user): the inventory family's third slice out of Plugin.cs. The inventory's visual sort,
// the item under the mouse and the inventory's reader have exactly one install path and one release path across every Plugin*.cs file, all in
// Plugin.InventoryReaders.cs, inside the plugin's registrations, each install given with its own release; Plugin.cs calls each once where its
// block stood and creates none.
using System.Text.RegularExpressions;

internal static class InventoryReadersFamily1940Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release, string Call)[] Paths =
    [
        ("visual sort", @"sortBridge=new InventoryVisualSortBridge\(", @"sortBridge\?\.Dispose\(\)", "RegisterVisualSort(inventory,player,client,framework,bankBridge);"),
        ("item hover", @"hoverReader = new ItemHoverReader\(", @"hoverReader\?\.Dispose\(\)", "RegisterItemHover(configuration,gameGui,player,client,framework,keyState);"),
        ("inventory reader", @"inventoryReader = new DalamudInventoryReader\(", @"inventoryReader\?\.Dispose\(\)", "RegisterInventoryReader(inventory,player,client,framework);"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Inventory readers family 1940: one install and one release path for the visual sort, the item hover and the inventory reader, in Plugin.InventoryReaders.cs only", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.InventoryReaders.cs").Text;
            var last = -1;
            foreach (var (service, install, release, _) in Paths)
            {
                Check(files.Sum(file => Count(file.Text, install)) == 1 && files.Sum(file => Count(file.Text, release)) == 1, service + ": one install and one release path");
                Check(Count(home, install) == 1 && Count(home, release) == 1, service + ": in its family's file");
                var at = Regex.Match(home, install).Index;
                Check(at > last, service + ": in the order of before");
                last = at;
            }
            Check(Count(home, @"registrations\.Register\(") == Paths.Length, "every one through the plugin's registrations");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!Regex.IsMatch(plugin, @"new (InventoryVisualSortBridge|ItemHoverReader|DalamudInventoryReader)\("), "Plugin.cs creates none of them");
        }),
        ("Inventory readers family 1940: Plugin.cs calls each once, where its block stood, in the order of before", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var calls = Paths.Select(path => At(path.Call)).ToArray();
            Check(Paths.All(path => plugin.Split(path.Call).Length == 2), "each called once");
            Check(calls.SequenceEqual(calls.Order()), "in the order of before");
            Check(At("registrations.Register(()=>{ },itemContextMenu.Dispose);") < calls[0] && calls[0] < At("RegisterMacroReaders(framework,player,client,commands,interop);"), "the visual sort after the context menu, before the macros' readers");
            Check(At("RegisterMacroReaders(framework,player,client,commands,interop);") < calls[1] && calls[1] < At("RegisterMapCatalog(data,framework);"), "the item hover after the macros' readers, before the maps' catalogue");
            Check(At("RegisterRecipeAndGatheringCatalogs(data,framework);") < calls[2] && calls[2] < At("craftLive.SetEnabled(MainWindow.CraftLiveWanted(configuration));"), "the inventory reader after the catalogues, before the craft's live reading");
        }),
        ("Inventory readers family 1940: each service's install and its release are given to one same registration of Plugin.InventoryReaders.cs", () =>
            FamilyRegistrations.Paired("Inventory readers family 1940", PluginFiles().Single(file => file.Name == "Plugin.InventoryReaders.cs").Text, Paths.Select(path => (path.Service, path.Install, path.Release)))),
    ];
}
