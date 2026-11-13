using System.Text.RegularExpressions;
using TenshoAssistant.Competitive;

// 0.610 (DEC-038): Tensho stops in PvP and ranked content, with no way around it.
internal static class CompetitiveCut610Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static bool HasCode(string text,string code)=>System.Text.RegularExpressions.Regex.Replace(text,@"\s+","").Contains(System.Text.RegularExpressions.Regex.Replace(code,@"\s+",""),StringComparison.Ordinal);
    private static string Root=>Path.GetFullPath("src/TenshoAssistant");
    private static string Source(string file)=>File.ReadAllText(Path.Combine(Root,file)).Replace("\r\n","\n");
    private static IEnumerable<string> Code()=>Directory.EnumerateFiles(Root,"*.cs",SearchOption.AllDirectories).Where(file=>!file.Contains("\\obj\\") && !file.Contains("/obj/") && !file.Contains("\\bin\\") && !file.Contains("/bin/"));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Competitive: the zones come from the flags, deep dungeons and the list",() =>
        {
            var zones=CompetitiveZones.Build([(250,true),(100,false)],[(579,true,9),(561,false,CompetitiveZones.DeepDungeonContentType),(132,false,2),(0,true,5)]);
            foreach(var territory in new uint[]{250,579,561,940,941,831})Check(zones.Contains(territory),$"{territory} cut");
            // X-137 (0.1115): the Beastmaster labyrinth (1339 to 1343) is no longer cut.
            foreach(var territory in new uint[]{100,132,0,1339,1343})Check(!zones.Contains(territory),$"{territory} free");
        }),
        ("Competitive: the decision; in doubt, stopped",() =>
        {
            var zones=new HashSet<uint>{430};
            Check(CompetitiveZones.IsCut(true,100,true,zones,false),"IsPvP alone");
            Check(CompetitiveZones.IsCut(true,430,false,zones,false),"a listed zone");
            Check(!CompetitiveZones.IsCut(true,100,false,zones,true),"it comes back outside");
            Check(CompetitiveZones.IsCut(true,100,false,null,false),"data unread: stopped");
            Check(CompetitiveZones.IsCut(true,0,false,zones,true) && !CompetitiveZones.IsCut(true,0,false,zones,false),"loading screen: unchanged");
            Check(!CompetitiveZones.IsCut(false,579,true,zones,true),"logged out: nothing to protect");
        }),
        ("Competitive (0.612): in the Battlehall, stopped in the arena's instance only",() =>
        {
            var zones=CompetitiveZones.Build([],[(579,true,9)]);
            Check(zones.Contains(579),"still known as a place");
            Check(!CompetitiveZones.IsCut(true,579,false,zones,false),"the public hall, PvE matches included: not stopped");
            Check(CompetitiveZones.IsCut(true,579,false,zones,false,inInstance:true),"in the instance: stopped");
            Check(!CompetitiveZones.IsCut(true,579,false,zones,true,inInstance:false),"out of it: back");
            Check(CompetitiveZones.IsCut(true,579,false,null,false),"data unread: still stopped");
            Check(CompetitiveZones.IsCut(true,940,false,zones,false) && !CompetitiveZones.InstanceOnly.Contains(831),"the other places stay stopped from the door");
        }),
        ("Competitive: every framework subscription goes through the gate",() =>
        {
            var around=Code().Where(file=>!file.EndsWith("CompetitiveGate.Dalamud.cs") && Regex.IsMatch(File.ReadAllText(file),@"\.Update\s*[+-]=")).Select(Path.GetFileName).ToArray();
            Check(around.Length==0,"going around the gate: "+string.Join(", ",around));
            var always=Code().Where(file=>File.ReadAllText(file).Contains("CompetitiveGate.AddAlways(")).Select(file=>Path.GetFileName(file)!).Order().ToArray();
            Check(always.SequenceEqual(["AlarmDtrEntry.Dalamud.cs","CurrencyDtrEntry.Dalamud.cs","GilDtrEntry.Dalamud.cs","InfoDtrEntries.Dalamud.cs","NativeMapReplacer.Dalamud.cs","ReminderDtrEntry.Dalamud.cs"]),"only what must undo something: "+string.Join(", ",always));
            foreach(var file in always)Check(File.ReadAllText(Code().First(path=>Path.GetFileName(path)==file)).Contains("CompetitiveGate.Cut)"),$"{file} acts on the cut");
        }),
        ("Competitive (0.615): the chat is not read during a cut either",() =>
        {
            var listeners=Code().Where(file=>Regex.IsMatch(File.ReadAllText(file),@"\.ChatMessage\w*\s*\+=")).ToArray();
            Check(listeners.Length>=2,"the chat listeners found");
            foreach(var file in listeners)Check(File.ReadAllText(file).Contains("if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;"),Path.GetFileName(file)+" reads the chat during a cut");
        }),
        ("Competitive: nothing shown, no command, no menu, no hook work, no way to turn it off",() =>
        {
            var plugin=Source("Plugin.cs");
            Check(HasCode(plugin,"private void Draw(){ if(TenshoAssistant.Competitive.CompetitiveGate.Cut){DrawBeastBoard(TenshoAssistant.Competitive.CompetitiveGate.LabyrinthBoard);return;}"),"no drawing but the labyrinth board's panel (DEC-047)");
            Check(plugin.Contains("boardWindow.IsOpen=allowed && !boardPanelBroken && showBoardPanel() && boardReader?.State.Board==true;"),"that panel only while the board is open");
            Check(HasCode(plugin,"private void OnCommand(string command, string arguments){ if(TenshoAssistant.Competitive.CompetitiveGate.Cut)"),"commands");
            Check(plugin.Contains("private void Open(){if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;") && plugin.Contains("private void OpenConfig(){if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;"),"opening");
            Check(plugin.IndexOf("new TenshoAssistant.Competitive.CompetitiveWatcher(")<plugin.IndexOf("mainWindow = new MainWindow("),"decided before anything starts");
            Check(Source("Items/ItemContextMenuBridge.Dalamud.cs").Contains("if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;"),"context menu");
            Check(Source("Craft/CraftRecorder.Dalamud.cs").Contains("TenshoAssistant.Competitive.CompetitiveGate.Cut"),"recorder");
            Check(Source("Macros/MacroChainBridge.Dalamud.cs").Contains("!TenshoAssistant.Competitive.CompetitiveGate.Cut"),"macro chain");
            Check(plugin.Contains("discord.Clear()"),"what waits for Discord does not leave during the cut");
            Check(!Source("Configuration.cs").Contains("Competitive"),"no setting");
            Check(Source("Competitive/CompetitiveGate.Dalamud.cs").Contains("Competitive cut {0}: territory"),"each change in the log, for the in-game checks");
        }),
    ];
}
