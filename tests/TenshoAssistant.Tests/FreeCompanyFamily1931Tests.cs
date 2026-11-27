// 0.1931 (X-153, GPT review of 11/11, accepted by the user): the free company's family out of Plugin.cs. The aetherial wheels' reader and the
// workshop deliveries' reader have exactly one install path and one release path across every Plugin*.cs file, both in Plugin.FreeCompany.cs,
// inside the plugin's registrations, each install given with its own release; Plugin.cs calls the two blocks where they stood and creates
// neither.
using System.Text.RegularExpressions;

internal static class FreeCompanyFamily1931Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release)[] Paths =
    [
        ("aetherial wheels' reader", @"wheelReader=new FreeCompany\.AetherialWheelReader\(", @"wheelReader\?\.Dispose\(\)"),
        ("workshop deliveries' reader", @"workshopDeliveryReader=new FreeCompany\.WorkshopDeliveryReader\(", @"workshopDeliveryReader\?\.Dispose\(\)"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Free company family 1931: one install and one release path for the wheels' and the workshop deliveries' readers, in Plugin.FreeCompany.cs only, in the order of before", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.FreeCompany.cs").Text;
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
            Check(home.Contains("configuration,save,()=>configuration.DeveloperMode)", StringComparison.Ordinal) && !home.Contains("SaveTimed", StringComparison.Ordinal), "the wheels saved by the save given in");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!Regex.IsMatch(plugin, @"new FreeCompany\.(AetherialWheelReader|WorkshopDeliveryReader)\("), "Plugin.cs creates neither");
        }),
        ("Free company family 1931: Plugin.cs calls each block once, where it stood", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var wheels = At("RegisterAetherialWheels(configuration,data,framework,gameGui,()=>SaveTimed(\"aetherial wheels\"));");
            var deliveries = At("RegisterWorkshopDeliveries(framework,gameGui);");
            Check(Count(plugin, @"RegisterAetherialWheels\(") == 1 && Count(plugin, @"RegisterWorkshopDeliveries\(") == 1, "each called once");
            Check(At("RegisterChatPlayers(") < wheels && wheels < At("autoAlarms=new Alarms.AutoAlarmWatcher("), "the wheels: after the chat's players, before the automatic alarms that follow them, as before");
            Check(At("RegisterItemUses(") < deliveries && deliveries < At("RegisterItemCatalog("), "the deliveries: between the items' uses and the items' catalogue, as before");
        }),
        ("Free company family 1931: each service's install and its release are given to one same registration of Plugin.FreeCompany.cs (its own release, never another's)", () =>
            FamilyRegistrations.Paired("Free company family 1931", PluginFiles().Single(file => file.Name == "Plugin.FreeCompany.cs").Text, Paths.Select(path => (path.Service, path.Install, path.Release)))),
    ];
}
