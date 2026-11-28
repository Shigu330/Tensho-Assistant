// 0.1939 (X-153, GPT review of 11/11, accepted by the user): the gear wear's family out of Plugin.cs. The gear wear's watcher has exactly one
// install path and one release path across every Plugin*.cs file, both in Plugin.GearWear.cs, inside the plugin's registrations, the install
// given with its own release; Plugin.cs calls it once where its block stood and creates none; the worn gear is read when it is checked.
using System.Text.RegularExpressions;

internal static class GearWearFamily1939Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private const string Install = @"gearWear=new Gear\.GearWearWatcher\(", Release = @"gearWear\?\.Dispose\(\)";
    private const string Call = "RegisterGearWear(configuration,framework,client,condition,chat,()=>gear?.Equipped ?? []);";

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Gear wear family 1939: one install and one release path for the gear wear's watcher, in Plugin.GearWear.cs only; the worn gear read when checked", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.GearWear.cs").Text;
            Check(files.Sum(file => Count(file.Text, Install)) == 1 && files.Sum(file => Count(file.Text, Release)) == 1, "one install and one release path");
            Check(Count(home, Install) == 1 && Count(home, Release) == 1 && Count(home, @"registrations\.Register\(") == 1, "in its family's file, through the plugin's registrations");
            Check(home.Contains("new Gear.GearWearWatcher(framework,client,equipped,()=>configuration.GearWear ?? new(),log)", StringComparison.Ordinal), "the worn gear given as a function, read when checked");
            Check(home.Contains("localization.Format(\"GearWear.AlertRepair\",", StringComparison.Ordinal) && home.Contains("options.ChatAllowed(inDuty)", StringComparison.Ordinal), "the alert with the alarm options, as before");
            Check(!files.Single(file => file.Name == "Plugin.cs").Text.Contains("new Gear.GearWearWatcher(", StringComparison.Ordinal), "Plugin.cs creates none");
        }),
        ("Gear wear family 1939: Plugin.cs calls it once, where its block stood", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var call = At(Call);
            Check(Count(plugin, @"RegisterGearWear\(") == 1, "called once");
            Check(At("RegisterInventoryWindow(configuration,framework,gameGui);") < call && call < At("RegisterPrepReminders(configuration,data,framework,client,objects,condition,chat);"), "after the inventory window, before the preparation reminder, as before");
            Check(At("var (gear,gameGearsets,graceWatcher)=RegisterGear(") < call, "after the gear loader it reads");
        }),
        ("Gear wear family 1939: the install and its release are given to one same registration of Plugin.GearWear.cs", () =>
            FamilyRegistrations.Paired("Gear wear family 1939", PluginFiles().Single(file => file.Name == "Plugin.GearWear.cs").Text, [("gear wear's watcher", Install, Release)])),
    ];
}
