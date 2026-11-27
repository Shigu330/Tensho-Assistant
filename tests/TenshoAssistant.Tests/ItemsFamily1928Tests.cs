// 0.1928 (X-153, GPT review of 11/11, accepted by the user): the items' family out of Plugin.cs. The desynthesis levels, the shop reminder, the
// items' uses loader, the Grand Company supply reader and the items' catalogue loader have exactly one install path and one release path
// across every Plugin*.cs file, all in Plugin.Items.cs, inside the plugin's registrations, each install given with its own release; Plugin.cs
// calls the four blocks where they stood and creates none of them.
using System.Text.RegularExpressions;

internal static class ItemsFamily1928Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release)[] Paths =
    [
        ("desynthesis levels", @"desynthLevels=new Items\.DesynthesisLevels\(", @"desynthLevels\?\.Dispose\(\)"),
        ("shop reminder", @"shopReminder=new Items\.ItemNoteShopReminder\(", @"shopReminder\?\.Dispose\(\)"),
        ("items' uses loader", @"usageLoader=new ItemUseCatalogLoader\(", @"usageLoader\?\.Dispose\(\)"),
        ("Grand Company supply reader", @"gcSupplyReader=new Items\.GcSupplyReader\(", @"gcSupplyReader\?\.Dispose\(\)"),
        ("items' catalogue loader", @"catalogLoader = new ItemCatalogLoader\(", @"catalogLoader\?\.Dispose\(\)"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Items family 1928: one install and one release path for each items' service, in Plugin.Items.cs only, in the order of before", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Items.cs").Text;
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
            Check(home.Contains("mainWindow.GcSupply.Restore(configuration.GcSupplySeen);", StringComparison.Ordinal) && home.IndexOf("mainWindow.GcSupply.Restore(", StringComparison.Ordinal) < home.IndexOf("gcSupplyReader=new Items.GcSupplyReader(", StringComparison.Ordinal),
                "today's request restored before its reader starts");
            Check(home.Contains("configuration.GcSupplySeen=seen;\n            try{saver.Save();}", StringComparison.Ordinal), "what the reader sees is saved");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!Regex.IsMatch(plugin, @"new (Items\.DesynthesisLevels|Items\.ItemNoteShopReminder|ItemUseCatalogLoader|Items\.GcSupplyReader|ItemCatalogLoader)\("), "Plugin.cs creates none of them");
        }),
        ("Items family 1928: Plugin.cs calls each block once, where it stood", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var desynth = At("RegisterDesynthesis(data,framework,client);");
            var shop = At("RegisterShopReminder(configuration,data,framework,gameGui,player,chat);");
            var uses = At("RegisterItemUses(configuration,data,framework);");
            var catalog = At("RegisterItemCatalog(configuration,data,framework,gameGui,client,player,saver);");
            foreach (var name in new[] { "RegisterDesynthesis", "RegisterShopReminder", "RegisterItemUses", "RegisterItemCatalog" })
                Check(Count(plugin, name + @"\(") == 1, name + ": called once");
            Check(At("mainWindow.GraceSource=") < desynth && desynth < At("mainWindow.HomeRegion=()=>"), "the desynthesis: after the grace, before the home region");
            Check(At("mainWindow.StackSizeOf=") < shop && shop < At("RegisterUndercut("), "the shop reminder: after the stack sizes, before the undercut");
            Check(At("RegisterMiniGameReaders(") < uses && uses < At("workshopDeliveryReader=new FreeCompany.WorkshopDeliveryReader(") && At("workshopDeliveryReader?.Dispose()") < catalog
                && catalog < At("RegisterRecipeAndGatheringCatalogs(data,framework);"), "the uses and the catalogue: around the workshop's deliveries, before the recipes' catalogue, as before");
        }),
        ("Items family 1928: each service's install and its release are given to one same registration of Plugin.Items.cs (its own release, never another's)", () =>
            FamilyRegistrations.Paired("Items family 1928", PluginFiles().Single(file => file.Name == "Plugin.Items.cs").Text, Paths.Select(path => (path.Service, path.Install, path.Release)))),
    ];
}
