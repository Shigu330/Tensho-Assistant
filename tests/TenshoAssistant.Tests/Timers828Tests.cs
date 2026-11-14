// 0.828 (X-53, X-45): the once-a-second timers that never ran, and the vendors a quest layer hid.
internal static class Timers828Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Timers: no clock starts at long.MinValue (now minus it overflows: the refresh never ran)",() =>
        {
            var source=Path.GetFullPath("src/TenshoAssistant");
            var bad=Directory.GetFiles(source,"*.cs",SearchOption.AllDirectories).Where(file=>!file.Contains("\\obj\\") && !file.Contains("/obj/"))
                .Where(file=>System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(file),@"long\s+\w+\s*=\s*long\.MinValue")).Select(Path.GetFileName).ToArray();
            Check(bad.Length==0,"started at long.MinValue: "+string.Join(", ",bad));
            long at=-100_000;Check(Environment.TickCount64-at>=1000,"the first call runs");
        }),
        // 0.1307 (X-266, user report of 13/11 at Mor Dhona): a quest layer alone no longer leaves a Level position always there; a quest layer
        // beside an always-there layer still does (MorePresent keeps « always »), which is what 0.828 protected.
        ("Vendors: a Level position is left out for a festival not running, or when only quest layers place it in its territory (X-266)",() =>
        {
            var loader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Npcs/NpcVendorLoader.Dalamud.cs"));
            Check(loader.Contains("switch(NpcShopLinks.LevelCondition(placed,placedAnywhere.Contains(npcId)))") && TenshoAssistant.Npcs.NpcShopLinks.LevelCondition((0,0),true)==0
                && TenshoAssistant.Npcs.NpcShopLinks.LevelCondition(TenshoAssistant.Npcs.NpcShopLinks.MorePresent((2,0),(0,0)),true)==0,"festival, quest only, never a quest layer beside an always-there one");
        }),
    ];
}
