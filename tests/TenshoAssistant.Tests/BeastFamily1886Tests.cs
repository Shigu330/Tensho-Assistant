// 0.1886 (X-153, GPT review of 11/11, accepted by the user): the Beastmaster family out of Plugin.cs, the first family of the game's readers.
// Its six services (catalogue loader, board reader, board panel, familiar reader, hotbar locator, developer probe) have exactly one install
// path and one release path across every Plugin*.cs file, both in Plugin.Beastmaster.cs, inside the plugin's registrations; Plugin.cs calls
// the family at the two places these services were made (the order of the registrations is the one of before) and creates none of them.
// Behaviour: released in reverse order, a failure while one installs releases the ones made before it and never the failed one.
using System.Text.RegularExpressions;
using TenshoAssistant;

internal static class BeastFamily1886Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release)[] Paths =
    [
        ("catalogue loader", @"beastLoader=new Beastmaster\.BeastCatalogLoader\(", @"beastLoader\?\.Dispose\(\)"),
        ("board reader", @"boardReader=new Beastmaster\.BeastBoardReader\(", @"boardReader\?\.Dispose\(\)"),
        ("board panel", @"boardWindow=new BeastBoardWindow\(mainWindow\)", @"boardWindows\.RemoveWindow\(boardWindow\)"),
        ("familiar reader", @"familiarReader=new Beastmaster\.BeastFamiliarReader\(", @"familiarReader\?\.Dispose\(\)"),
        ("hotbar locator", @"beastBarLocator=new Gathering\.ActionBarLocator\(", @"beastBarLocator\?\.Dispose\(\)"),
        ("developer probe", @"beastProbe=new Beastmaster\.BeastWindowProbe\(", @"beastProbe\?\.Dispose\(\)"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast family 1886: one install and one release path for each service, in Plugin.Beastmaster.cs only", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Beastmaster.cs").Text;
            foreach (var (service, install, release) in Paths)
            {
                Check(files.Sum(file => Count(file.Text, install)) == 1 && files.Sum(file => Count(file.Text, release)) == 1, service + ": one install and one release path");
                Check(Count(home, install) == 1 && Count(home, release) == 1, service + ": in its family's file");
            }
            Check(Count(home, @"registrations\.Register\(") == Paths.Length, "every one through the plugin's registrations");
            Check(home.Contains("public sealed partial class Plugin", StringComparison.Ordinal), "one class over its files");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!Regex.IsMatch(plugin, @"new Beastmaster\.Beast\w+\(") && !plugin.Contains("new BeastBoardWindow(", StringComparison.Ordinal), "Plugin.cs creates none of them");
            Check(Count(plugin, @"RegisterBeastCatalog\(") == 1 && Count(plugin, @"RegisterBeastBoard\(") == 1, "Plugin.cs calls the family once at each of its two places");
        }),
        ("Beast family 1886: the order of before: the catalogue where it was made, the board's services where they were, in their order", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var catalog = At("var beasts=RegisterBeastCatalog(data,framework,client,player);");
            Check(At("registrations.Register(()=>triadNpcLoader=new TriadNpcLoader(") < catalog && catalog < At("itemWatcher.Beasts=()=>beasts.State.Obtained;"),
                "the catalogue after the Triple Triad NPCs, before the discoveries read the beasts tamed");
            var board = At("RegisterBeastBoard(configuration,data,framework,gameGui,objects);");
            Check(At("registrations.Register(()=>housingProbe=new Maps.HousingFurnitureProbe(") < board && board < At("RegisterGameChat(configuration,framework,gameGui);"),
                "the board's services after the housing probe, before the chat layout probe");
            Check(catalog < board, "the catalogue first (the board reads it)");
            var home = PluginFiles().Single(file => file.Name == "Plugin.Beastmaster.cs").Text;
            var order = Paths.Skip(1).Select(path => Regex.Match(home, path.Install).Index).ToArray();
            Check(order.All(index => index > 0) && order.SequenceEqual(order.Order()), "board reader, panel, familiar, hotbars, probe: in their order of before");
        }),
        ("Beast family 1886: behaviour: released in reverse order; a service that fails to install releases the ones made before it, never itself", () =>
        {
            var said = new List<string>();
            var scope = new RegistrationScope();
            scope.Register(() => said.Add("+board"), () => said.Add("-board"));
            scope.Register(() => said.Add("+panel"), () => said.Add("-panel"));
            var failed = false;
            try { scope.Register(() => throw new InvalidOperationException("familiar"), () => said.Add("-familiar")); }
            catch (InvalidOperationException) { failed = true; }
            Check(failed, "the failure reaches the caller (the plugin's start)");
            scope.Dispose();
            Check(said.SequenceEqual(["+board", "+panel", "-panel", "-board"]), "the panel released before the reader it reads, the failed one never: " + string.Join(",", said));
            var second = new RegistrationScope();
            second.Register(() => { }, () => said.Add("-loader"));
            second.Register(() => { }, () => throw new InvalidOperationException("panel"));
            second.Register(() => { }, () => said.Add("-probe"));
            var thrown = false;
            try { second.Dispose(); } catch (AggregateException error) { thrown = error.InnerExceptions.Single().Message == "panel"; }
            Check(thrown && said.TakeLast(2).SequenceEqual(["-probe", "-loader"]), "a failing release does not stop the others, said at the end");
        }),
    ];
}
