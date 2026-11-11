using TenshoAssistant.Diagnostics;
using TenshoAssistant.Gathering;

// 0.1094 (user report, 11/11 15 h: the DLL was reloaded seven times during one ocean voyage): the voyage record kept across a reload, and the
// play state the developer's scripts read before replacing the DLL.
internal static class VoyageReload1094Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset Start=new(2026,11,11,14,10,0,TimeSpan.Zero);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Voyage reload 1094: the record of the same voyage is kept, never below what was saved",() =>
        {
            var first=OceanVoyageRecord.Merge(null,20,0,[new(0,40536,1,20),new(0,40538,1,22)],[2,1,5],Start);
            Check(first.Catches.Count==2 && first.Missions.SequenceEqual([2,1,5]) && !first.ScheduleChecked,"first reading kept");
            // After a reload the game's line for the stop can be empty: what was saved stays.
            var after=OceanVoyageRecord.Merge(first with{ScheduleChecked=true},20,0,[],[0,0,0],Start.AddMinutes(3));
            Check(after.Catches.SequenceEqual(first.Catches),"catches kept across a reload");
            Check(after.Missions.SequenceEqual([2,1,5]),"missions kept");
            Check(after.ScheduleChecked,"schedule check not done again");
            // The game counts more: the larger value wins, each stop apart.
            var more=OceanVoyageRecord.Merge(after,20,1,[new(0,40536,2,40),new(1,51231,1,30)],[4,7,7],Start.AddMinutes(9));
            Check(more.Catches.Single(fish=>fish is {Zone:0,Item:40536}).Points==40 && more.At(1).Single().Item==51231,"per stop, larger wins");
            Check(more.Catches.Sum(fish=>fish.Points)==92 && more.Catches.Sum(fish=>fish.Count)==4,"totals of the voyage");
        }),
        ("Voyage reload 1094: another voyage starts empty",() =>
        {
            var saved=OceanVoyageRecord.Merge(null,20,2,[new(2,51230,3,90)],[11,15,14],Start) with{ResultLogged=true,ScheduleChecked=true};
            Check(!saved.SameVoyage(21,2,Start.AddMinutes(1)),"other route");
            Check(!saved.SameVoyage(20,0,Start.AddMinutes(1)),"stop behind: a new voyage of the same route");
            Check(!saved.SameVoyage(20,2,Start+OceanVoyageRecord.Keep+TimeSpan.FromMinutes(1)),"too old");
            Check(saved.SameVoyage(20,2,Start.AddMinutes(10)),"same voyage");
            var next=OceanVoyageRecord.Merge(saved,20,0,[],[0,0,0],Start.AddHours(2));
            Check(next.Catches.Count==0 && next.Missions.All(value=>value==0) && !next.ResultLogged && !next.ScheduleChecked,"nothing carried over");
        }),
        ("Voyage reload 1094: the saved file reads back, a broken one is ignored",() =>
        {
            var saved=OceanVoyageRecord.Merge(null,20,1,[new(0,40536,1,20),new(1,51231,2,60)],[7,15,12],Start) with{ScheduleChecked=true};
            var back=OceanVoyageRecord.FromJson(saved.ToJson());
            Check(back is not null && back.SameContent(saved) && back.Updated==saved.Updated,"round trip");
            Check(OceanVoyageRecord.FromJson(null) is null && OceanVoyageRecord.FromJson("{") is null && OceanVoyageRecord.FromJson("[]") is null,"unreadable");
            Check(OceanVoyageRecord.FromJson(saved.ToJson().Replace("\"Format\":1","\"Format\":9")) is null,"other format");
            Check(OceanVoyageRecord.FromJson(saved.ToJson().Replace("[1,51231,2,60]","[7,51231,2,60]")) is null,"stop out of range");
        }),
        ("Voyage reload 1094: play state for the developer's scripts",() =>
        {
            Check(PlayState.BusyReason(true,false,true)=="ocean voyage" && PlayState.BusyReason(false,true,false)=="competitive content"
                  && PlayState.BusyReason(false,false,true)=="duty" && PlayState.BusyReason(false,false,false) is null,"reasons");
            Check(PlayState.ShouldWrite(null,false,null,Start,Start),"first write");
            Check(!PlayState.ShouldWrite(null,true,null,Start.AddHours(1),Start),"free: written once");
            Check(PlayState.ShouldWrite("duty",true,null,Start.AddSeconds(2),Start),"change written");
            Check(!PlayState.ShouldWrite("duty",true,"duty",Start.AddSeconds(10),Start) && PlayState.ShouldWrite("duty",true,"duty",Start.AddSeconds(30),Start),"heartbeat while busy");
            var json=PlayState.ToJson("ocean voyage",Start);
            Check(json.Contains("\"busy\":true") && json.Contains("\"reason\":\"ocean voyage\"") && json.Contains("\"updated\":\"2026-11-11T14:10:00"),"file shape read by scripts/agent/playstate.py: "+json);
        }),
        ("Voyage reload 1094: wiring",() =>
        {
            string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("\"OceanVoyage.json\"") && plugin.Contains("new Diagnostics.PlayStateWriter(") && plugin.Contains("pluginInterface.IsDev || configuration.DeveloperMode"),"plugin wiring");
            var reader=Read("Gathering/OceanVoyageReader.Dalamud.cs");
            Check(reader.Contains("OceanVoyageRecord.Merge(record,") && reader.Contains("AtomicFile.Write(path,json)") && reader.Contains("OceanVoyageRecord.FromJson("),"reader keeps and reads the record");
            Check(Read("MainWindow.OceanVoyagePanel.cs").Contains("if(frame.ScheduleChecked)return;"),"schedule check once per voyage");
            var writer=Read("Diagnostics/PlayStateWriter.Dalamud.cs");
            Check(writer.Contains("CompetitiveGate.Add(framework,Update)") && writer.Contains("CompetitiveGate.Changed+=OnCut") && !writer.Contains("AddAlways"),"through the gate (DEC-038), one write when competitive content cuts");
        }),
    ];
}
