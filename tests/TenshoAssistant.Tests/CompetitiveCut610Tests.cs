using System.Text.RegularExpressions;
using TenshoAssistant.Competitive;

// 0.610 (DEC-038): Tensho stops in PvP and ranked content, with no way around it.
internal static class CompetitiveCut610Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Root=>Path.GetFullPath("src/TenshoAssistant");
    private static string Source(string file)=>File.ReadAllText(Path.Combine(Root,file)).Replace("\r\n","\n");
    private static IEnumerable<string> Code()=>Directory.EnumerateFiles(Root,"*.cs",SearchOption.AllDirectories).Where(file=>!file.Contains("\\obj\\") && !file.Contains("/obj/") && !file.Contains("\\bin\\") && !file.Contains("/bin/"));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Competitive: the zones come from the flags, deep dungeons and the list",() =>
        {
            var zones=CompetitiveZones.Build([(250,true),(100,false)],[(579,true,9),(561,false,CompetitiveZones.DeepDungeonContentType),(132,false,2),(0,true,5)]);
            foreach(var territory in new uint[]{250,579,561,940,941,831,1339,1343})Check(zones.Contains(territory),$"{territory} cut");
            foreach(var territory in new uint[]{100,132,0})Check(!zones.Contains(territory),$"{territory} free");
        }),
        ("Competitive: the decision; in doubt, stopped",() =>
        {
            var zones=new HashSet<uint>{579};
            Check(CompetitiveZones.IsCut(true,100,true,zones,false),"IsPvP alone");
            Check(CompetitiveZones.IsCut(true,579,false,zones,false),"a listed zone");
            Check(!CompetitiveZones.IsCut(true,100,false,zones,true),"it comes back outside");
            Check(CompetitiveZones.IsCut(true,100,false,null,false),"data unread: stopped");
            Check(CompetitiveZones.IsCut(true,0,false,zones,true) && !CompetitiveZones.IsCut(true,0,false,zones,false),"loading screen: unchanged");
            Check(!CompetitiveZones.IsCut(false,579,true,zones,true),"logged out: nothing to protect");
        }),
        ("Competitive: every framework subscription goes through the gate",() =>
        {
            var around=Code().Where(file=>!file.EndsWith("CompetitiveGate.Dalamud.cs") && Regex.IsMatch(File.ReadAllText(file),@"\.Update\s*[+-]=")).Select(Path.GetFileName).ToArray();
            Check(around.Length==0,"going around the gate: "+string.Join(", ",around));
            var always=Code().Where(file=>File.ReadAllText(file).Contains("CompetitiveGate.AddAlways(")).Select(file=>Path.GetFileName(file)!).Order().ToArray();
            Check(always.SequenceEqual(["AlarmDtrEntry.Dalamud.cs","CurrencyDtrEntry.Dalamud.cs","NativeMapReplacer.Dalamud.cs","ReminderDtrEntry.Dalamud.cs"]),"only what must undo something: "+string.Join(", ",always));
            foreach(var file in always)Check(File.ReadAllText(Code().First(path=>Path.GetFileName(path)==file)).Contains("CompetitiveGate.Cut)"),$"{file} acts on the cut");
        }),
        ("Competitive: nothing shown, no command, no menu, no hook work, no way to turn it off",() =>
        {
            var plugin=Source("Plugin.cs");
            Check(plugin.Contains("private void Draw()\n    {\n        if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;"),"no drawing");
            Check(plugin.Contains("private void OnCommand(string command, string arguments)\n    {\n        if(TenshoAssistant.Competitive.CompetitiveGate.Cut)"),"commands");
            Check(plugin.Contains("private void Open(){if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;") && plugin.Contains("private void OpenConfig(){if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;"),"opening");
            Check(plugin.IndexOf("new TenshoAssistant.Competitive.CompetitiveWatcher(")<plugin.IndexOf("mainWindow = new MainWindow("),"decided before anything starts");
            Check(Source("Items/ItemContextMenuBridge.Dalamud.cs").Contains("if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;"),"context menu");
            Check(Source("Craft/CraftRecorder.Dalamud.cs").Contains("TenshoAssistant.Competitive.CompetitiveGate.Cut"),"recorder");
            Check(Source("Macros/MacroChainBridge.Dalamud.cs").Contains("!TenshoAssistant.Competitive.CompetitiveGate.Cut"),"macro chain");
            Check(plugin.Contains("discord.Clear()"),"what waits for Discord does not leave during the cut");
            Check(!Source("Configuration.cs").Contains("Competitive"),"no setting");
        }),
    ];
}
