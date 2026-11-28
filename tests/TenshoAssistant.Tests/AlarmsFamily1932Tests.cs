// 0.1932 (X-153, GPT review of 11/11, accepted by the user): the alarms' family out of Plugin.cs. The Discord notifier, the alarm service and
// the automatic alarms' watcher have exactly one install path and one release path across every Plugin*.cs file, all in Plugin.Alarms.cs,
// inside the plugin's registrations, each install given with its own release; the notifier is registered before the alarm service (so
// released after it) and handed back; Plugin.cs calls the two blocks where they stood and creates none of them.
using System.Text.RegularExpressions;

internal static class AlarmsFamily1932Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release)[] Paths =
    [
        ("Discord notifier", @"var discord=new Alarms\.DiscordNotifier\(\);", @"registrations\.Register\(\(\)=>\{\},discord\.Dispose\);"),
        ("alarm service", @"alarms=new Alarms\.AlarmService\(", @"\(\)=>alarms\?\.Dispose\(\)"),
        ("automatic alarms' watcher", @"autoAlarms=new Alarms\.AutoAlarmWatcher\(", @"autoAlarms\?\.Dispose\(\)"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Alarms family 1932: one install and one release path for the notifier, the alarm service and the automatic alarms, in Plugin.Alarms.cs only, in the order of before", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Alarms.cs").Text;
            var last = -1;
            foreach (var (service, install, release) in Paths)
            {
                Check(files.Sum(file => Count(file.Text, install)) == 1 && files.Sum(file => Count(file.Text, release)) == 1, service + ": one install and one release path");
                Check(Count(home, install) == 1 && Count(home, release) == 1, service + ": in its family's file");
                var at = Regex.Match(home, install).Index;
                Check(at > last, service + ": in the order of before");
                last = at;
            }
            // The notifier, the chat link handler's release, the alarm service, the automatic alarms.
            Check(Count(home, @"registrations\.Register\(") == 4 && home.Contains("registrations.Register(()=>{},()=>{chat.RemoveChatLinkHandler();alarmLinks.Clear();});", StringComparison.Ordinal),
                "through the plugin's registrations, the chat link handler released with them");
            Check(home.Contains("configuration,save,", StringComparison.Ordinal) && !home.Contains("SaveTimed", StringComparison.Ordinal) && home.Contains("return discord;", StringComparison.Ordinal),
                "the saves given in; the notifier handed back");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!Regex.IsMatch(plugin, @"new Alarms\.(DiscordNotifier|AlarmService|AutoAlarmWatcher)\("), "Plugin.cs creates none of them");
        }),
        ("Alarms family 1932: Plugin.cs calls each block once, where it stood, before the services that post to Discord", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var alarms = At("var discord=RegisterAlarms(configuration,framework,condition,chat,objects,()=>SaveTimed(\"alarms\"));");
            var auto = At("RegisterAutoAlarms(framework,client,configuration,()=>SaveTimed(\"automatic alarms\"));");
            Check(Count(plugin, @"RegisterAlarms\(") == 1 && Count(plugin, @"RegisterAutoAlarms\(") == 1, "each called once");
            Check(At("mainWindow.ConfigureMacroWriter(") < alarms && alarms < At("RegisterGathering(") && alarms < At("RegisterCurrencies("), "the alarms: after the macro writer, before the gathering family and the currencies that post through the notifier");
            Check(At("RegisterAetherialWheels(") < auto && auto < At("RegisterCollections("), "the automatic alarms: after the wheels they follow, before the collections, as before");
        }),
        ("Alarms family 1932: each service's install and its release are given to one same registration of Plugin.Alarms.cs (its own release, never another's)", () =>
        {
            var home = PluginFiles().Single(file => file.Name == "Plugin.Alarms.cs").Text;
            // The notifier is made on its own line, its release given to the very next registration (an empty install), as before.
            Check(home.Contains("var discord=new Alarms.DiscordNotifier();\n        registrations.Register(()=>{},discord.Dispose);", StringComparison.Ordinal), "the notifier's release right after it is made");
            FamilyRegistrations.Paired("Alarms family 1932", home, Paths.Skip(1).Select(path => (path.Service, path.Install, path.Release)));
        }),
    ];
}
