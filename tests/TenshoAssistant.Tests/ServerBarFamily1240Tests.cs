// 0.1240 (X-153, third slice, GPT review of 11/11): the server bar family out of Plugin.cs, after the desktop (0.1225) and persistence
// (0.1239) families. Each of the five entries of the game's server bar has exactly one install path and one release path across every
// Plugin*.cs file, both in Plugin.ServerBar.cs, inside the plugin's registrations, created in the order of before (alarms, reminders,
// currencies, gil, optional entries); Plugin.cs calls the family once, after the services they show (registered earlier, so released
// after them), and creates none of them; the tooltips still reach them.
using System.Text.RegularExpressions;

internal static class ServerBarFamily1240Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static (string Name,string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"Plugin*.cs")
        .Select(path => (Path.GetFileName(path),File.ReadAllText(path).Replace("\r\n","\n"))).ToArray();

    private static int Count(string text,string pattern) => Regex.Matches(text,pattern).Count;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Server bar family 1240: one install and one release path for each entry, in Plugin.ServerBar.cs only, in the order of before",() =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.ServerBar.cs").Text;
            var entries = new (string Entry,string Install,string Release)[]
            {
                ("alarms",@"alarms=new Alarms\.AlarmDtrEntry\(",@"alarms\?\.Dispose\(\);alarms=null;"),
                ("reminders",@"reminders=new Alarms\.ReminderDtrEntry\(",@"reminders\?\.Dispose\(\);reminders=null;"),
                ("currencies",@"currencies=new Alarms\.CurrencyDtrEntry\(",@"currencies\?\.Dispose\(\);currencies=null;"),
                ("gil",@"gil=new Inventory\.GilDtrEntry\(",@"gil\?\.Dispose\(\);gil=null;"),
                ("optional entries",@"info=new Alarms\.InfoDtrEntries\(",@"info\?\.Dispose\(\);info=null;"),
            };
            var last = -1;
            foreach (var (entry,install,release) in entries)
            {
                Check(files.Sum(file => Count(file.Text,install)) == 1 && files.Sum(file => Count(file.Text,release)) == 1,entry + ": one install and one release path");
                Check(Count(home,install) == 1 && Count(home,release) == 1,entry + ": in its family's file");
                var at = Regex.Match(home,install).Index;
                Check(at > last,entry + ": created in the order of before (the order the game's bar first shows them)");
                last = at;
            }
            // 0.1245 (X-165): a sixth, Tensho's state, last.
            Check(files.Sum(file => Count(file.Text,@"new (Alarms|Inventory)\.\w*DtrEntr(y|ies)\(")) == 6,"no other server bar entry anywhere");
            Check(Count(home,@"registrations\.Register\(") == 6,"all through the plugin's registrations");
            foreach (var reach in new[] { "alarmBar=()=>alarms;","reminderBar=()=>reminders;","currencyBar=()=>currencies;","gilBar=()=>gil;" })
                Check(home.Contains(reach,StringComparison.Ordinal),"the tooltip reaches it: " + reach);
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            var call = plugin.IndexOf("RegisterServerBar(dtrBar,framework,gameGui,player,client,inventory,configuration,localization,()=>currencies?.State,()=>gilWatcher?.State?.Entries,()=>gear?.Equipped ?? []);",StringComparison.Ordinal);
            Check(call > 0 && Count(plugin,@"RegisterServerBar\(") == 1,"Plugin.cs calls the family once");
            foreach (var service in new[] { "currencies=new Inventory.CurrencyWatcher(","gilWatcher=new Inventory.GilWatcher(","gear=new Gear.GearLoader(","var discord=new Alarms.DiscordNotifier();" })
                Check(plugin.IndexOf(service,StringComparison.Ordinal) is > 0 and var made && made < call,"after what it shows (released before it): " + service);
            Check(plugin.Contains("(\"AlarmBar\",()=>alarmBar?.Invoke()?.DrawTooltip()),",StringComparison.Ordinal) && plugin.Contains("(\"GilBar\",()=>gilBar?.Invoke()?.DrawTooltip()),",StringComparison.Ordinal),"tooltips drawn as before");
        }),
    ];
}
