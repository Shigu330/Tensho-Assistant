// 0.1899 (X-153, GPT review of 11/11, accepted by the user): the duties family out of Plugin.cs. The Duty Finder's reader and loot
// service, the duty catalogue's loader and the duty progress reader have exactly one install path and one release path across every
// Plugin*.cs file, all in Plugin.Duties.cs, inside the plugin's registrations; Plugin.cs calls each group where its block stood and creates
// none. Pairing (X-885 M2, 0.1906): each service's install and its release given to one same registration.
using System.Text.RegularExpressions;
using TenshoAssistant;

internal static class DutiesFamily1899Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release)[] Paths =
    [
        ("Duty Finder reader", @"dutyFinderReader=new Duties\.DutyFinderReader\(", @"dutyFinderReader\?\.Dispose\(\)"),
        ("Duty Finder loot", @"var dutyFinderLoot=new DutyLootService\(", @"dutyFinderLoot\.Dispose\)"),
        ("duty catalogue loader", @"dutyLoader = new DutyCatalogLoader\(", @"dutyLoader\?\.Dispose\(\)"),
        ("duty progress reader", @"dutyProgress = new DutyProgressReader\(", @"dutyProgress\?\.Dispose\(\)"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Duties family 1899: one install and one release path for the Duty Finder, its loot, the duty catalogue and the duty progress, in Plugin.Duties.cs", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Duties.cs").Text;
            var last = -1;
            foreach (var (service, install, release) in Paths)
            {
                Check(files.Sum(file => Count(file.Text, install)) == 1 && files.Sum(file => Count(file.Text, release)) == 1, service + ": one install and one release path");
                Check(Count(home, install) == 1 && Count(home, release) == 1, service + ": in its family's file");
                var at = Regex.Match(home, install).Index;
                Check(at > last, service + ": in the order of before");
                last = at;
            }
            Check(Count(home, @"registrations\.Register\(") == Paths.Length, "every one through the plugin's registrations");
            Check(home.Contains("mainWindow.ConfigureDutyFinder(dutyFinder,dutyFinderLoot);", StringComparison.Ordinal)
                && home.Contains("mainWindow.DutyProgressUnavailable = () => dutyProgress?.Unavailable == true;", StringComparison.Ordinal), "the main window still reads them");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            // The Duty loot page's own service (the dutyLoot field, made with the plugin) is not this family's; the Duty Finder's loot is.
            Check(!Regex.IsMatch(plugin, @"new (Duties\.DutyFinderService|Duties\.DutyFinderReader|DutyCatalogLoader|DutyProgressReader)\(")
                && !plugin.Contains("dutyFinderLoot", StringComparison.Ordinal), "Plugin.cs creates none of them");
        }),
        ("Duties family 1899: Plugin.cs calls each group once, where its block stood", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var finder = At("RegisterDutyFinder(configuration,framework,gameGui);");
            var catalog = At("RegisterDutyCatalog(pluginInterface,data,framework,client,unlocks,player);");
            Check(Count(plugin, @"RegisterDutyFinder\(") == 1 && Count(plugin, @"RegisterDutyCatalog\(") == 1, "each called once");
            Check(At("mainWindow.ConfigureLumina(lumina);") < finder && finder < At("var triadNpcs=new TriadNpcService();"), "the Duty Finder after Lumina, before the Triple Triad NPCs, as before");
            Check(At("mapLoader = new MapCatalogLoader(") < catalog && catalog < At("CharacterStatsReader? statsReader = null;"), "the duty catalogue after the map catalogue, before the character stats, as before");
        }),
        // X-885 M2 (0.1906): this case replaced « behaviour », the same RegistrationScope test copied in each family test, which touched nothing
        // of the family; each service's install and its release are now checked given to one same registration of Plugin.Duties.cs.
        ("Duties family 1899: each service's install and its release are given to one same registration of Plugin.Duties.cs (its own release, never another's)", () =>
            FamilyRegistrations.Paired("Duties family 1899", PluginFiles().Single(file => file.Name == "Plugin.Duties.cs").Text, Paths.Select(path => (path.Service, path.Install, path.Release)))),
    ];
}
