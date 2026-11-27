// 0.1888 (X-153, GPT review of 11/11, accepted by the user): the gear family out of Plugin.cs, after the Beastmaster (0.1886) and retainer
// market (0.1887) families. Its three services (gear loader, the game's gear sets and job levels, the manuals' Grace) have exactly one install
// path and one release path across every Plugin*.cs file, both in Plugin.Gear.cs, inside the plugin's registrations; Plugin.cs calls the family
// where these services were made, creates none of them, and reads the three handed back. Behaviour: a family's save goes through the action
// it is given (the plugin's timed save), never its own.
using System.Text.RegularExpressions;
using TenshoAssistant;

internal static class GearFamily1888Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release)[] Paths =
    [
        ("gear loader", @"gear=new Gear\.GearLoader\(", @"gear\?\.Dispose\(\)"),
        ("game gear sets", @"gameGearsets=new Gear\.GameGearsetReader\(", @"gameGearsets\?\.Dispose\(\)"),
        ("manuals' Grace", @"graceWatcher=new Gear\.GraceWatcher\(", @"graceWatcher\?\.Dispose\(\)"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Gear family 1888: one install and one release path for each service, in Plugin.Gear.cs only", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Gear.cs").Text;
            foreach (var (service, install, release) in Paths)
            {
                Check(files.Sum(file => Count(file.Text, install)) == 1 && files.Sum(file => Count(file.Text, release)) == 1, service + ": one install and one release path");
                Check(Count(home, install) == 1 && Count(home, release) == 1, service + ": in its family's file");
            }
            Check(Count(home, @"registrations\.Register\(") == Paths.Length, "every one through the plugin's registrations");
            var order = Paths.Select(path => Regex.Match(home, path.Install).Index).ToArray();
            Check(order.SequenceEqual(order.Order()), "in their order of before");
            Check(home.Contains("return (gear,gameGearsets,graceWatcher);", StringComparison.Ordinal), "the three handed back");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!Regex.IsMatch(plugin, @"new Gear\.(GearLoader|GameGearsetReader|GraceWatcher)\("), "Plugin.cs creates none of them");
            var call = plugin.IndexOf("var (gear,gameGearsets,graceWatcher)=RegisterGear(configuration,data,framework,client,player,objects,chat,()=>SaveTimed(\"grace\"));", StringComparison.Ordinal);
            Check(call > 0 && Count(plugin, @"RegisterGear\(") == 1, "Plugin.cs calls the family once");
            Check(call > plugin.IndexOf("RegisterVoyages(", StringComparison.Ordinal) && call < plugin.IndexOf("RegisterSharedFates(configuration,framework,client,player,gameGui,", StringComparison.Ordinal),
                "where the services were made: after the voyages, before the shared FATEs");
            Check(plugin.Contains("mainWindow.GraceSource=()=>graceWatcher?.Shown;", StringComparison.Ordinal) && plugin.Contains("mainWindow.EquippedGear=()=>gear?.Equipped ?? [];", StringComparison.Ordinal)
                && plugin.Contains("if(gameGearsets is not null)gameGearsets.Copied=read=>", StringComparison.Ordinal), "the plugin's later wiring reads the three handed back");
            Check(home.Contains("()=>configuration.GraceCounts??=[],saveGrace,", StringComparison.Ordinal) && !home.Contains("SaveTimed(", StringComparison.Ordinal), "the Grace saved through the action given");
        }),
        ("Gear family 1888: behaviour: a family registers in order, released in reverse, and saves through the action it is given", () =>
        {
            var said = new List<string>();
            var saves = 0;
            var scope = new RegistrationScope();
            void Family(Action save)
            {
                scope.Register(() => said.Add("+gear"), () => said.Add("-gear"));
                scope.Register(() => said.Add("+sets"), () => said.Add("-sets"));
                scope.Register(() => { said.Add("+grace"); save(); }, () => said.Add("-grace"));
            }
            Family(() => saves++);
            scope.Dispose();
            Check(saves == 1, "the save given was used");
            Check(said.SequenceEqual(["+gear", "+sets", "+grace", "-grace", "-sets", "-gear"]), "in order, released in reverse: " + string.Join(",", said));
        }),
    ];
}
