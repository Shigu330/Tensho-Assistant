// 0.1930 (X-153, GPT review of 11/11, accepted by the user): the character's family out of Plugin.cs. The character's status watcher and the
// character's attributes' reader have exactly one install path and one release path across every Plugin*.cs file, both in Plugin.Character.cs,
// inside the plugin's registrations, each install given with its own release; Plugin.cs calls the two blocks where they stood and creates
// neither.
using System.Text.RegularExpressions;

internal static class CharacterFamily1930Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release)[] Paths =
    [
        ("character's status watcher", @"characterStatus=new Character\.CharacterStatusWatcher\(", @"characterStatus\?\.Dispose\(\)"),
        ("character's attributes' reader", @"statsReader = new CharacterStatsReader\(", @"statsReader\?\.Dispose\(\)"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Character family 1930: one install and one release path for the status watcher and the attributes' reader, in Plugin.Character.cs only, in the order of before", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Character.cs").Text;
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
            Check(home.Contains("mainWindow.CharacterStatusSource=characterStatus;", StringComparison.Ordinal)
                && home.IndexOf("mainWindow.CharacterStatusSource=", StringComparison.Ordinal) > home.IndexOf("characterStatus=new Character.CharacterStatusWatcher(", StringComparison.Ordinal), "the window given the watcher once it is made");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!Regex.IsMatch(plugin, @"new (Character\.CharacterStatusWatcher|CharacterStatsReader)\("), "Plugin.cs creates neither");
        }),
        ("Character family 1930: Plugin.cs calls each block once, where it stood", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var status = At("RegisterCharacterStatus(pluginInterface,data,framework,player,dailyRoulettes,retainerSales);");
            var stats = At("RegisterCharacterStats(data,framework,player,client,objects,gameGui);");
            Check(Count(plugin, @"RegisterCharacterStatus\(") == 1 && Count(plugin, @"RegisterCharacterStats\(") == 1, "each called once");
            Check(At("var retainerSales=RegisterRetainerMarket(") < status && At("mainWindow.ChallengeRows=") < status && status < At("IReadOnlySet<uint>? cabinetItems=null;"),
                "the status: after the retainers' sales and the challenge log, before the armoire, as before");
            Check(At("RegisterDutyCatalog(") < stats && stats < At("RegisterWeatherAndGpCatalogs("), "the attributes: between the duty catalogue and the weather, as before");
        }),
        ("Character family 1930: the registrations' reader skips a line comment, so an apostrophe in it opens no character (« the retainers' job »)", () =>
        {
            var text = "registrations.Register(()=>a=new A(x,\n    // the retainers' job, named\n    y),()=>a?.Dispose());\nregistrations.Register(()=>b=new B(\"//\"),()=>b?.Dispose());\n";
            var calls = FamilyRegistrations.Calls(text);
            Check(calls.Count == 2 && calls[0].Text.EndsWith("()=>a?.Dispose())", StringComparison.Ordinal) && calls[1].Text == "registrations.Register(()=>b=new B(\"//\"),()=>b?.Dispose())",
                "two calls, each closed where it ends; « // » in a string is no comment");
        }),
        ("Character family 1930: each service's install and its release are given to one same registration of Plugin.Character.cs (its own release, never another's)", () =>
            FamilyRegistrations.Paired("Character family 1930", PluginFiles().Single(file => file.Name == "Plugin.Character.cs").Text, Paths.Select(path => (path.Service, path.Install, path.Release)))),
    ];
}
