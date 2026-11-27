// 0.1887 (X-153, GPT review of 11/11, accepted by the user): the retainer market family out of Plugin.cs, after the Beastmaster family
// (0.1886). Its four services (the retainer sale list reader, the panel's and the sale list's Universalis comparisons, the market tax
// reader) have exactly one install path and one release path across every Plugin*.cs file, both in Plugin.Market.cs, inside the plugin's
// registrations; Plugin.cs calls the family where these services were made and creates none of them; the sale list's reader is handed
// back, the character status reads it. Behaviour: the reader handed back is the one the install made, and released once at unload.
using System.Text.RegularExpressions;
using TenshoAssistant;

internal static class MarketFamily1887Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release)[] Paths =
    [
        ("retainer sale list reader", @"retainerSales=new Market\.RetainerSalesReader\(", @"retainerSales\?\.Dispose\(\)"),
        ("the panel's comparison", @"retainerCompare=new Market\.RetainerCompareService\{", @"retainerCompare\?\.Dispose\(\)"),
        ("the sale list's comparison", @"retainerSaleListCompare=new Market\.RetainerCompareService\{", @"retainerSaleListCompare\?\.Dispose\(\)"),
        ("market tax reader", @"marketTaxes=new Market\.MarketTaxReader\(", @"marketTaxes\?\.Dispose\(\)"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Market family 1887: one install and one release path for each service, in Plugin.Market.cs only", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Market.cs").Text;
            foreach (var (service, install, release) in Paths)
            {
                Check(files.Sum(file => Count(file.Text, install)) == 1 && files.Sum(file => Count(file.Text, release)) == 1, service + ": one install and one release path");
                Check(Count(home, install) == 1 && Count(home, release) == 1, service + ": in its family's file");
            }
            // 0.1893: the rest of the family lives beside it (MarketFamily1893Tests); these four are the retainer market's own.
            var retainer = home[home.IndexOf("RegisterRetainerMarket(", StringComparison.Ordinal)..];
            Check(Count(retainer, @"registrations\.Register\(") == Paths.Length, "every one through the plugin's registrations");
            var order = Paths.Select(path => Regex.Match(home, path.Install).Index).ToArray();
            Check(order.SequenceEqual(order.Order()), "in their order of before");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!plugin.Contains("new Market.RetainerSalesReader(", StringComparison.Ordinal) && !plugin.Contains("new Market.RetainerCompareService", StringComparison.Ordinal)
                && !plugin.Contains("new Market.MarketTaxReader(", StringComparison.Ordinal), "Plugin.cs creates none of them");
            var call = plugin.IndexOf("var retainerSales=RegisterRetainerMarket(configuration,framework,gameGui,client,player,marketBoardEvents);", StringComparison.Ordinal);
            Check(call > 0 && Count(plugin, @"RegisterRetainerMarket\(") == 1, "Plugin.cs calls the family once");
            Check(call > plugin.IndexOf("SettingsNotice(\"Plugin.SettingsBackupKeptAside\",backupAside);", StringComparison.Ordinal)
                && call < plugin.IndexOf("registrations.Register(()=>mainWindow.CommunityHelp=communityHelp=new Help.CommunityHelpIndex()", StringComparison.Ordinal), "where the services were made");
            Check(call < plugin.IndexOf("RegisterCharacterStatus(pluginInterface,data,framework,player,dailyRoulettes,retainerSales);", StringComparison.Ordinal), "the character status reads the reader handed back (X-153, 0.1930: given to its family)");
            Check(home.Contains("return retainerSales;", StringComparison.Ordinal), "the sale list's reader handed back");
        }),
        ("Market family 1887: behaviour: what a family hands back is the service its install made, released once at unload", () =>
        {
            var made = 0; var released = 0;
            var scope = new RegistrationScope();
            object? reader = null;
            object? Family() { scope.Register(() => { reader = new object(); made++; }, () => released++); return reader; }
            var handed = Family();
            Check(handed is not null && ReferenceEquals(handed, reader) && made == 1, "the install ran once, its service handed back");
            scope.Dispose();
            scope.Dispose();
            Check(released == 1, "released once, a second unload does nothing");
        }),
    ];
}
