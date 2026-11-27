// 0.1902 (X-153, GPT review of 11/11, accepted by the user): the leves family out of Plugin.cs. The leve progress reader has exactly one
// install path and one release path across every Plugin*.cs file, both in Plugin.Leves.cs, inside the plugin's registrations; Plugin.cs
// calls it where its block stood, with the leves' list, and creates none; the reader handed back is what the Leves page reads.
using System.Text.RegularExpressions;
using TenshoAssistant;

internal static class LevesFamily1902Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Leves family 1902: one install and one release path for the leve progress reader, in Plugin.Leves.cs only; handed back", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Leves.cs").Text;
            const string install = @"leveReader=new Leves\.LeveProgressReader\(", release = @"leveReader\?\.Dispose\(\)";
            Check(files.Sum(file => Count(file.Text, install)) == 1 && files.Sum(file => Count(file.Text, release)) == 1, "one install and one release path");
            Check(Count(home, install) == 1 && Count(home, release) == 1 && Count(home, @"registrations\.Register\(") == 1, "in its family's file, through the plugin's registrations");
            Check(home.Contains("return leveReader;", StringComparison.Ordinal) && home.Contains("log,leveList)", StringComparison.Ordinal), "the reader handed back, the list passed in");
            Check(!files.Single(file => file.Name == "Plugin.cs").Text.Contains("new Leves.LeveProgressReader(", StringComparison.Ordinal), "Plugin.cs creates none");
        }),
        ("Leves family 1902: Plugin.cs calls it once, where its block stood, before the Leves page reads it", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var call = At("var leveReader=RegisterLeveProgress(framework,client,player,unlocks,data,LeveList);");
            Check(Count(plugin, @"RegisterLeveProgress\(") == 1, "called once");
            Check(At("IReadOnlyList<Leves.LeveDefinition> LeveList()=>") < call && call < At("mainWindow.LeveDefinitions=LeveList;"), "after the leves' list, where the block stood");
            Check(call < At("mainWindow.LeveProgress=()=>{leveReader?.Want();return leveReader?.State;};"), "before the Leves page reads the reader handed back");
        }),
        // X-885 M2 (0.1906): this case replaced « behaviour », the same RegistrationScope test copied in each family test, which touched nothing
        // of the family; each service's install and its release are now checked given to one same registration of Plugin.Leves.cs.
        ("Leves family 1902: each service's install and its release are given to one same registration of Plugin.Leves.cs (its own release, never another's)", () =>
            FamilyRegistrations.Paired("Leves family 1902", PluginFiles().Single(file => file.Name == "Plugin.Leves.cs").Text, [("leve progress reader", @"leveReader=new Leves\.LeveProgressReader\(", @"leveReader\?\.Dispose\(\)")])),
    ];
}
