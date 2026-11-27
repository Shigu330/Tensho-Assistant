// 0.1443 (X-153, eleventh slice, GPT review of 11/11): the chat family out of Plugin.cs. The Tensho chat window's reader, the chat map link
// watcher and the chat link hover watcher each have exactly one install path and one release path across every Plugin*.cs file, all in
// Plugin.Chat.cs, inside the plugin's registrations, in the order of before; Plugin.cs calls the family once, at the place the block stood
// (after the treasure map, before the automatic alarms), and creates none of them.
using System.Text.RegularExpressions;

internal static class ChatFamily1443Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static (string Name,string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"Plugin*.cs")
        .Select(path => (Path.GetFileName(path),File.ReadAllText(path).Replace("\r\n","\n"))).ToArray();

    private static int Count(string text,string pattern) => Regex.Matches(text,pattern).Count;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat family 1443: one install and one release path for each, in Plugin.Chat.cs only, in the order of before; called once at the same place",() =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Chat.cs").Text;
            var parts = new (string Part,string Install,string Release)[]
            {
                ("chat window reader",@"chatReader=new Chat\.ChatReader\(",@"\(\)=>chatReader\?\.Dispose\(\)\);"),
                ("chat map links",@"chatLinks=new Maps\.ChatMapLinkWatcher\(",@"\(\)=>chatLinks\?\.Dispose\(\)\);"),
                ("chat link hover",@"chatLinkHover=new Maps\.ChatLinkHoverWatcher\(",@"\(\)=>chatLinkHover\?\.Dispose\(\)\);"),
            };
            var last = -1;
            foreach (var (part,install,release) in parts)
            {
                Check(files.Sum(file => Count(file.Text,install)) == 1 && files.Sum(file => Count(file.Text,release)) == 1,part + ": one install and one release path");
                Check(Count(home,install) == 1 && Count(home,release) == 1,part + ": in its family's file");
                var at = Regex.Match(home,install).Index;
                Check(at > last,part + ": created in the order of before");
                last = at;
            }
            // 0.1894: the rest of the family follows RegisterChat in the same file (ChatFamily1894Tests); these three are its own.
            var own = home[..home.IndexOf("private void RegisterGameChat(",StringComparison.Ordinal)];
            Check(Count(own,@"registrations\.Register\(") == 3,"through the plugin's registrations");
            Check(home.Contains("mainWindow.ChatLinkHistory=chatLinks?.History;") && home.Contains("mainWindow.ChatLinkHover=chatLinkHover?.Hover;"),"the main window still reads them");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            var treasure = plugin.IndexOf("if(itemWatcher is not null)itemWatcher.TreasureMapItem=",StringComparison.Ordinal);
            var call = plugin.IndexOf("RegisterChat(",StringComparison.Ordinal);
            var alarms = plugin.IndexOf("Alarms.AutoAlarmWatcher? autoAlarms=null;",StringComparison.Ordinal);
            Check(treasure > 0 && call > treasure && alarms > call && Count(plugin,@"RegisterChat\(") == 1,"Plugin.cs calls the family once, where the block stood");
            Check(!Regex.IsMatch(plugin,@"(ChatReader|ChatMapLinkWatcher|ChatLinkHoverWatcher)\b"),"Plugin.cs creates none of them");
        }),
    ];
}
