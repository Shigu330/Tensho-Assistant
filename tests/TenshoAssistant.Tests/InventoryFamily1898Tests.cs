// 0.1898 (X-153, GPT review of 11/11, accepted by the user): the inventory family out of Plugin.cs, second slice. The currency watcher and
// the gil journal's watcher have exactly one install path and one release path across every Plugin*.cs file, both in Plugin.Inventory.cs,
// inside the plugin's registrations; Plugin.cs calls each where its block stood, after the Discord notifier and before the server bar,
// and hands the services back to the server bar. Behaviour: what a family hands back is the service its install made.
using System.Text.RegularExpressions;
using TenshoAssistant;

internal static class InventoryFamily1898Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release)[] Paths =
    [
        ("currency watcher", @"currencies=new Inventory\.CurrencyWatcher\(", @"\(\)=>currencies\?\.Dispose\(\)"),
        ("gil journal's watcher", @"gilWatcher=new Inventory\.GilWatcher\(", @"gilWatcher\?\.Dispose\(\)"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Inventory family 1898: one install and one release path for the currencies and the gil journal, in Plugin.Inventory.cs only", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Inventory.cs").Text;
            foreach (var (service, install, release) in Paths)
            {
                Check(files.Sum(file => Count(file.Text, install)) == 1 && files.Sum(file => Count(file.Text, release)) == 1, service + ": one install and one release path");
                Check(Count(home, install) == 1 && Count(home, release) == 1, service + ": in its family's file");
            }
            Check(Count(home, @"registrations\.Register\(") == 3 + Paths.Length, "every one through the plugin's registrations (with the first slice's three)");
            Check(home.Contains("return currencies;", StringComparison.Ordinal) && home.Contains("return gilWatcher;", StringComparison.Ordinal), "both handed back");
            Check(home.Contains("discord.Enqueue(", StringComparison.Ordinal) && home.Contains("linkNotices.Add(message);", StringComparison.Ordinal), "the alerts keep Discord and the link notices");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!Regex.IsMatch(plugin, @"new Inventory\.(CurrencyWatcher|GilWatcher|GilJournalStore)\(") && !plugin.Contains("void CurrencyNotice(", StringComparison.Ordinal), "Plugin.cs creates none of them");
        }),
        ("Inventory family 1898: Plugin.cs calls each once, after the Discord notifier, before the server bar that reads them", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var currencies = At("var currencies=RegisterCurrencies(configuration,framework,client,player,data,condition,chat,objects,discord);");
            var gil = At("var gilWatcher=RegisterGilJournal(pluginInterface,framework,player,gameGui,condition,objects,tenshoData);");
            var bar = At("RegisterServerBar(dtrBar,framework,gameGui,player,client,inventory,configuration,localization,()=>currencies?.State,()=>gilWatcher?.State?.Entries,");
            Check(Count(plugin, @"RegisterCurrencies\(") == 1 && Count(plugin, @"RegisterGilJournal\(") == 1, "each called once");
            Check(At("var discord=new Alarms.DiscordNotifier();") < currencies && currencies < gil && gil < bar, "after the Discord notifier, before the server bar, in the order of before");
            Check(currencies < At("// DEC-038: one chat line on entry, nothing on leaving;") && At("mainWindow.AchievementNameOf=id=>") < gil, "each where its block stood");
        }),
        ("Inventory family 1898: behaviour: what a family hands back is the service its install made, released once", () =>
        {
            var released = 0;
            var scope = new RegistrationScope();
            object? watcher = null;
            object? Family() { scope.Register(() => watcher = new object(), () => released++); return watcher; }
            var handed = Family();
            Check(handed is not null && ReferenceEquals(handed, watcher), "the install ran, its service handed back");
            scope.Dispose();
            scope.Dispose();
            Check(released == 1, "released once");
        }),
    ];
}
