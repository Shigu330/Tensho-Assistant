// 0.1903 (X-153, GPT review of 11/11, accepted by the user): the fashion family out of Plugin.cs. The fashion hints' loader and the Fashion
// Report reader have exactly one install path and one release path across every Plugin*.cs file, both in Plugin.Fashion.cs, inside the
// plugin's registrations, with the main window's stains and weekly themes; Plugin.cs calls the family where its block stood and creates none.
// Pairing (X-885 M2, 0.1906): each service's install and its release given to one same registration.
using System.Text.RegularExpressions;
using TenshoAssistant;

internal static class FashionFamily1903Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release)[] Paths =
    [
        ("fashion hints' loader", @"fashionLoader=new FashionHintLoader\(", @"fashionLoader\?\.Dispose\(\)"),
        ("Fashion Report reader", @"fashionCheckReader=new Fashion\.FashionCheckReader\(", @"fashionCheckReader\?\.Dispose\(\)"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Fashion family 1903: one install and one release path for the hints' loader and the Fashion Report reader, in Plugin.Fashion.cs only", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Fashion.cs").Text;
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
            Check(home.Contains("mainWindow.ConfigureFashionStains(", StringComparison.Ordinal) && home.Contains("mainWindow.ConfigureFashionCheck(fashionCheck,", StringComparison.Ordinal), "the main window still configured");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!Regex.IsMatch(plugin, @"new (FashionHintLoader|Fashion\.FashionCheckService|Fashion\.FashionCheckReader)\("), "Plugin.cs creates none of them");
        }),
        ("Fashion family 1903: Plugin.cs calls it once, where its block stood", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var call = At("RegisterFashion(configuration,data,framework,gameGui);");
            Check(Count(plugin, @"RegisterFashion\(") == 1, "called once");
            Check(At("craftLogReader=new CraftLogReader(") < call && call < At("QuestReader? questReader = null;"), "after the craft log reader, before the quests, as before");
        }),
        // X-885 M2 (0.1906): this case replaced « behaviour », the same RegistrationScope test copied in each family test, which touched nothing
        // of the family; each service's install and its release are now checked given to one same registration of Plugin.Fashion.cs.
        ("Fashion family 1903: each service's install and its release are given to one same registration of Plugin.Fashion.cs (its own release, never another's)", () =>
            FamilyRegistrations.Paired("Fashion family 1903", PluginFiles().Single(file => file.Name == "Plugin.Fashion.cs").Text, Paths.Select(path => (path.Service, path.Install, path.Release)))),
    ];
}
