// 0.1412 (X-153, eighth slice, GPT review of 11/11): the gathering and fishing family out of Plugin.cs. The two community fish services, the
// notebooks' reader, the bait reader and the fishing copy each have exactly one install path and one release path across every Plugin*.cs file,
// both in Plugin.Gathering.cs, inside the plugin's registrations, in the order of before; Plugin.cs calls the family once, at the place the block
// stood (after the alarms, before the job names), and creates none of them.
using System.Text.RegularExpressions;

internal static class GatheringFamily1412Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static (string Name,string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"Plugin*.cs")
        .Select(path => (Path.GetFileName(path),File.ReadAllText(path).Replace("\r\n","\n"))).ToArray();

    private static int Count(string text,string pattern) => Regex.Matches(text,pattern).Count;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gathering family 1412: one install and one release path for each, in Plugin.Gathering.cs only, in the order of before; called once at the same place",() =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Gathering.cs").Text;
            var parts = new (string Part,string Install,string Release)[]
            {
                ("GatherBuddy fish",@"var gatherBuddyFish=new Community\.GatherBuddyFishService\(",@"registrations\.Register\(\(\)=>\{\},gatherBuddyFish\.Dispose\);"),
                ("bite times",@"var biteTimes=new Community\.FishBiteTimeService\(",@"registrations\.Register\(\(\)=>\{\},biteTimes\.Dispose\);"),
                ("notebooks",@"notebooks=new Gathering\.NotebookReader\(",@"\(\)=>notebooks\?\.Dispose\(\)\);"),
                ("bait reader",@"baitReader=new Gathering\.FishingBaitReader\(",@"\(\)=>baitReader\?\.Dispose\(\)\);"),
                ("fishing copy",@"CompetitiveGate\.Add\(framework,copyFishingNow\)",@"CompetitiveGate\.Remove\(framework,copyFishingNow\)"),
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
            Check(Count(home,@"registrations\.Register\(") == 5,"through the plugin's registrations");
            Check(home.Contains("mainWindow.ConfigureGatherBuddyFish(gatherBuddyFish);") && home.Contains("mainWindow.NotebookSource=notebooks;")
                && home.Contains("mainWindow.FishingBait=()=>baitReader?.Bait;") && home.Contains("mainWindow.ConfigureNodeEffects(node=>")
                && home.Contains("mainWindow.ConfigureBountifulBase(item=>") && home.Contains("mainWindow.ConfigureFishingHelp("),"the main window still reads them");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            var alarms = plugin.IndexOf("if(alarms is not null)alarms.Gathered=mainWindow.AlarmGathered;",StringComparison.Ordinal);
            var call = plugin.IndexOf("RegisterGathering(",StringComparison.Ordinal);
            var jobs = plugin.IndexOf("mainWindow.ConfigureJobNames(job=>",StringComparison.Ordinal);
            Check(alarms > 0 && call > alarms && jobs > call && Count(plugin,@"RegisterGathering\(") == 1,"Plugin.cs calls the family once, where the block stood");
            Check(!Regex.IsMatch(plugin,@"(GatherBuddyFishService|FishBiteTimeService|NotebookReader|FishingBaitReader|copyFishingNow|GatheringPointBonus)\b"),"Plugin.cs creates none of them");
        }),
    ];
}
