// 0.1937 (X-153, GPT review of 11/11, accepted by the user): the events' family out of Plugin.cs. The festivals' reader has exactly one install
// path and one release path across every Plugin*.cs file, both in Plugin.Events.cs, inside the plugin's registrations, the install given with
// its own release; Plugin.cs calls it once where its block stood and creates none; the reader handed back is what the Events page reads.
using System.Text.RegularExpressions;

internal static class EventsFamily1937Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private const string Install = @"festivals=new Events\.FestivalReader\(", Release = @"festivals\?\.Dispose\(\)";

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Events family 1937: one install and one release path for the festivals' reader, in Plugin.Events.cs only; handed back", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Events.cs").Text;
            Check(files.Sum(file => Count(file.Text, Install)) == 1 && files.Sum(file => Count(file.Text, Release)) == 1, "one install and one release path");
            Check(Count(home, Install) == 1 && Count(home, Release) == 1 && Count(home, @"registrations\.Register\(") == 1, "in its family's file, through the plugin's registrations");
            Check(home.Contains("return festivals;", StringComparison.Ordinal), "the reader handed back");
            Check(!files.Single(file => file.Name == "Plugin.cs").Text.Contains("new Events.FestivalReader(", StringComparison.Ordinal), "Plugin.cs creates none");
        }),
        ("Events family 1937: Plugin.cs calls it once, where its block stood, before the Events page reads it", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var call = At("var festivals=RegisterFestivals(framework,client,data);");
            Check(Count(plugin, @"RegisterFestivals\(") == 1, "called once");
            Check(At("RegisterChocobo(data,framework,client,player,unlocks,condition);") < call && call < At("mainWindow.Festivals=()=>festivals;"), "after the chocobo family, before the Events page reads it");
            Check(call < At("RegisterGearsets(framework,player);"), "before the gear sets, as before");
        }),
        ("Events family 1937: the install and its release are given to one same registration of Plugin.Events.cs", () =>
            FamilyRegistrations.Paired("Events family 1937", PluginFiles().Single(file => file.Name == "Plugin.Events.cs").Text, [("festivals' reader", Install, Release)])),
    ];
}
