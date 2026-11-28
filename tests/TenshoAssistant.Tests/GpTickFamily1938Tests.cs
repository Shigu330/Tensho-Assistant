// 0.1938 (X-153, GPT review of 11/11, accepted by the user): the GP tick's family out of Plugin.cs. The GP regeneration tick's reader has
// exactly one install path and one release path across every Plugin*.cs file, both in Plugin.GpTick.cs, inside the plugin's registrations, the
// install given with its own release; Plugin.cs calls it once where its block stood and creates none; the GP alert keeps the alarm options.
using System.Text.RegularExpressions;

internal static class GpTickFamily1938Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private const string Install = @"regenTick=new Gathering\.RegenTickReader\(", Release = @"regenTick\?\.Dispose\(\)";

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("GP tick family 1938: one install and one release path for the GP tick's reader, in Plugin.GpTick.cs only; handed back with its alert", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.GpTick.cs").Text;
            Check(files.Sum(file => Count(file.Text, Install)) == 1 && files.Sum(file => Count(file.Text, Release)) == 1, "one install and one release path");
            Check(Count(home, Install) == 1 && Count(home, Release) == 1 && Count(home, @"registrations\.Register\(") == 1, "in its family's file, through the plugin's registrations");
            Check(home.Contains("return regenTick;", StringComparison.Ordinal), "the reader handed back");
            Check(home.Contains("localization.Format(\"GpTick.Alert\",gp,max)", StringComparison.Ordinal) && home.Contains("options.ChatAllowed(inDuty)", StringComparison.Ordinal), "the GP alert with the alarm options, as before");
            Check(!files.Single(file => file.Name == "Plugin.cs").Text.Contains("new Gathering.RegenTickReader(", StringComparison.Ordinal), "Plugin.cs creates none");
        }),
        ("GP tick family 1938: Plugin.cs calls it once, where its block stood, before the gathering pages read it", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var call = At("var regenTick=RegisterGpTick(configuration,framework,objects,condition,chat);");
            Check(Count(plugin, @"RegisterGpTick\(") == 1, "called once");
            Check(At("RegisterReadingCapture(configuration,framework,objects,condition,gameGui,chat);") < call && call < At("mainWindow.RegenTick=()=>regenTick?.State;"), "after the reading capture, before the pages read it, as before");
            Check(call < At("mainWindow.UseSheetLoaders(data,log);"), "before the sheet loaders, as before");
        }),
        ("GP tick family 1938: the install and its release are given to one same registration of Plugin.GpTick.cs", () =>
            FamilyRegistrations.Paired("GP tick family 1938", PluginFiles().Single(file => file.Name == "Plugin.GpTick.cs").Text, [("GP tick's reader", Install, Release)])),
    ];
}
