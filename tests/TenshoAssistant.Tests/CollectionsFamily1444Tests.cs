// 0.1444 (X-153, twelfth slice, GPT review of 11/11): the collections family out of Plugin.cs. The music reader, the title changer, the
// achievement progress watcher and the extra collection reader each have exactly one install path and one release path across every Plugin*.cs
// file, all in Plugin.Collections.cs, inside the plugin's registrations, in the order of before; Plugin.cs calls the family once, at the place
// the block stood (after the automatic alarms, before the hunting log), and creates none of them.
using System.Text.RegularExpressions;

internal static class CollectionsFamily1444Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static (string Name,string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"Plugin*.cs")
        .Select(path => (Path.GetFileName(path),File.ReadAllText(path).Replace("\r\n","\n"))).ToArray();

    private static int Count(string text,string pattern) => Regex.Matches(text,pattern).Count;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Collections family 1444: one install and one release path for each, in Plugin.Collections.cs only, in the order of before; called once at the same place",() =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Collections.cs").Text;
            var parts = new (string Part,string Install,string Release)[]
            {
                ("music",@"bgm=new Collections\.BgmReader\(",@"\(\)=>bgm\?\.Dispose\(\)\);"),
                ("title changer",@"titleChanger=new Collections\.TitleChanger\(",@"\(\)=>titleChanger\?\.Dispose\(\)\);"),
                ("achievement progress",@"achievementProgress=new Collections\.AchievementProgressWatcher\(",@"\(\)=>achievementProgress\?\.Dispose\(\)\);"),
                ("extra collections",@"extraCollections=new Collections\.ExtraCollectionReader\(",@"\(\)=>extraCollections\?\.Dispose\(\)\);"),
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
            Check(Count(home,@"registrations\.Register\(") == 4,"through the plugin's registrations");
            Check(home.Contains("mainWindow.PlayingRoll=()=>") && home.Contains("mainWindow.MusicRead=()=>bgm?.Playing is not null;")
                && home.Contains("registrations.Register(()=>mainWindow.TitleChangerSource=titleChanger=") && home.Contains("registrations.Register(()=>mainWindow.AchievementProgressSource=achievementProgress=")
                && home.Contains("mainWindow.ExtraCollections=()=>{extraCollections?.Want();return extraCollections?.State;};"),"the main window still reads them");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            var alarms = plugin.IndexOf("RegisterAutoAlarms(framework,",StringComparison.Ordinal);
            var call = plugin.IndexOf("RegisterCollections(",StringComparison.Ordinal);
            var hunting = plugin.IndexOf("// g, C-12 (0.364): hunting log",StringComparison.Ordinal);
            Check(alarms > 0 && call > alarms && hunting > call && Count(plugin,@"RegisterCollections\(") == 1,"Plugin.cs calls the family once, where the block stood");
            Check(!Regex.IsMatch(plugin,@"(BgmReader|TitleChanger|AchievementProgressWatcher|ExtraCollectionReader|BuildRollByBgm)\b"),"Plugin.cs creates none of them");
        }),
    ];
}
