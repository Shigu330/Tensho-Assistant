// 0.1250 (X-153, fifth slice, GPT review of 11/11): the discoveries family out of Plugin.cs (Plugin.Discoveries.cs): the store and its
// folder, the GitHub exchange (one install and one release path across every Plugin*.cs) and the loading off the frame, moved as they were
// (prices seen first, then the discoveries; nothing written before it is read; a database that cannot be opened never replaced). Plugin.cs
// calls the family at the same places as before: the store before the item watcher that fills it, the loading after the seen prices' reader.
using System.Text.RegularExpressions;

internal static class DiscoveriesFamily1250Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static (string Name,string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"Plugin*.cs")
        .Select(path => (Path.GetFileName(path),File.ReadAllText(path).Replace("\r\n","\n"))).ToArray();

    private static int Count(string text,string pattern) => Regex.Matches(text,pattern).Count;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discoveries family 1250: store, exchange and loading in Plugin.Discoveries.cs only; one install and one release path for the exchange",() =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Discoveries.cs").Text;
            foreach (var (what,pattern) in new[]
            {
                ("the store",@"new Discoveries\.DiscoveryStore\(\)"),
                ("the exchange's install",@"discoveryExchange=new Discoveries\.GitHubExchange\(discoveryFolder\)"),
                ("the exchange's release",@"\(\)=>discoveryExchange\?\.Dispose\(\)"),
                ("the database opened",@"new Discoveries\.DiscoveryDatabase\(databasePath\)"),
                // X-574 (0.1793): the files of before no longer converted: only named as left aside.
                ("the files of before named",@"Discoveries\.DiscoveryFiles\.OldFiles\(saved\)"),
                ("the base read safely",@"Discoveries\.DiscoveryFiles\.ReadBase\(saved,"),
                ("the prices seen read",@"Market\.SeenPriceStore\.ReadAtStart\("),
            })
                Check(files.Sum(file => Count(file.Text,pattern)) == 1 && Count(home,pattern) == 1,what + ": once, in its family's file");
            Check(Count(home,@"registrations\.Register\(") == 1,"the exchange through the plugin's registrations");
            Check(Count(home,@"Task\.Run\(") == 1,"one loading worker, as before");
            Check(home.IndexOf("Market.SeenPriceStore.ReadAtStart(",StringComparison.Ordinal) < home.IndexOf("new Discoveries.DiscoveryDatabase(databasePath)",StringComparison.Ordinal),"the prices seen first, then the discoveries, as before");
            Check(home.Contains("mainWindow.DiscoveriesWriteBlocked=true;") && home.Contains("_=framework.RunOnFrameworkThread(()=>chat.PrintError(\"[Tensho] \"+said));"),"blocked saves and the chat line kept");
        }),
        ("Discoveries family 1250: Plugin.cs calls it at the places of before: the store before its item watcher, the loading after the seen prices' reader",() =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            var store = plugin.IndexOf("var (discoveries,discoveryFolder)=RegisterDiscoveries(pluginInterface);",StringComparison.Ordinal);
            // X-153 (0.1941): the item watcher is made by its own family (Plugin.ItemWatcher.cs), called at the same place.
            var watcher = plugin.IndexOf("var itemWatcher=RegisterItemWatcher(",StringComparison.Ordinal);
            // X-153 (0.1893): the seen prices' reader is made by its market family, called at the same place.
            var reader = plugin.IndexOf("RegisterSeenPrices(configuration,marketBoardEvents,seenPrices,bankBridge);",StringComparison.Ordinal);
            var load = plugin.IndexOf("LoadDiscoveries(discoveryFolder,discoveries,seenPrices,framework,chat);",StringComparison.Ordinal);
            Check(store > 0 && store < watcher && watcher < reader && reader < load,"order of before");
            Check(Count(plugin,@"RegisterDiscoveries\(") == 1 && Count(plugin,@"LoadDiscoveries\(") == 1,"called once each");
        }),
    ];
}
