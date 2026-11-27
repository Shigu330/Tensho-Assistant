// 0.1901 (X-153, GPT review of 11/11, accepted by the user): the diagnostics family out of Plugin.cs. The self-check runner and the reading
// capture have exactly one install path and one release path across every Plugin*.cs file, both in Plugin.Diagnostics.cs, inside the
// plugin's registrations; Plugin.cs calls each where its block stood and creates none; the self-check stays a field, which the command
// and the page sweep read. Pairing (X-885 M2, 0.1906): each service's install and its release given to one same registration.
using System.Text.RegularExpressions;
using TenshoAssistant;

internal static class DiagnosticsFamily1901Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release)[] Paths =
    [
        ("self-check", @"selfCheck=new Diagnostics\.SelfCheckRunner\(", @"\(\)=>selfCheck\?\.Dispose\(\)"),
        ("reading capture", @"readingCapture=new Diagnostics\.ReadingCapture\(", @"readingCapture\?\.Dispose\(\)"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Diagnostics family 1901: one install and one release path for the self-check and the reading capture, in Plugin.Diagnostics.cs only", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Diagnostics.cs").Text;
            foreach (var (service, install, release) in Paths)
            {
                Check(files.Sum(file => Count(file.Text, install)) == 1 && files.Sum(file => Count(file.Text, release)) == 1, service + ": one install and one release path");
                Check(Count(home, install) == 1 && Count(home, release) == 1, service + ": in its family's file");
            }
            Check(Count(home, @"registrations\.Register\(") == Paths.Length, "every one through the plugin's registrations");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!Regex.IsMatch(plugin, @"new Diagnostics\.(SelfCheckRunner|ReadingCapture)\("), "Plugin.cs creates none of them");
            Check(plugin.Contains("private Diagnostics.SelfCheckRunner? selfCheck;", StringComparison.Ordinal) && plugin.Contains("selfCheck?.Start(\"command\");", StringComparison.Ordinal), "the self-check stays the plugin's field, the command reads it");
        }),
        ("Diagnostics family 1901: Plugin.cs calls each once, where its block stood", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var check = At("RegisterSelfCheck(pluginInterface,configuration,data,framework,client,player);");
            var capture = At("RegisterReadingCapture(configuration,framework,objects,condition,gameGui,chat);");
            Check(Count(plugin, @"RegisterSelfCheck\(") == 1 && Count(plugin, @"RegisterReadingCapture\(") == 1, "each called once");
            Check(At("RegisterGameChat(configuration,framework,gameGui);") < check && check < At("mainWindow.ConfigureMarketBoard(marketBoard);"), "the self-check after the game chat, before the market board, as before");
            Check(At("var currencies=RegisterCurrencies(") < capture && capture < At("Gathering.RegenTickReader? regenTick=null;"), "the reading capture after the currencies, before the GP tick, as before");
        }),
        // X-885 M2 (0.1906): this case replaced « behaviour », the same RegistrationScope test copied in each family test, which touched nothing
        // of the family; each service's install and its release are now checked given to one same registration of Plugin.Diagnostics.cs.
        ("Diagnostics family 1901: each service's install and its release are given to one same registration of Plugin.Diagnostics.cs (its own release, never another's)", () =>
            FamilyRegistrations.Paired("Diagnostics family 1901", PluginFiles().Single(file => file.Name == "Plugin.Diagnostics.cs").Text, Paths.Select(path => (path.Service, path.Install, path.Release)))),
    ];
}
