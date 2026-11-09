// Test audit of 2026-11-09, batch 2 (0.1021): Squadron333Tests, Squadron451Tests merged into SquadronTests; every case is kept.
using TenshoAssistant.Reminders;
internal static class SquadronTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly string Source = Path.GetFullPath("src/TenshoAssistant");
    private static string Read(string file) => File.ReadAllText(Path.Combine(Source,file));
    private static string ReadLf(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");
    private static readonly SquadronRequirement Set=new(new(100,100,100),20,20,20,20);
    private static SquadronMission Mission(int percentBase,SquadronRequirement set)=>new(1,"Mission",20,2,percentBase,[set]);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From Squadron333Tests: 0.333 (DEC-023 point 6, user request: mission AND training): the squadron's mission and training ends are read from PlayerState,
        // guarded on their own, and shown in the Reminders with an alarm. Native code is outside the test project: pinned in the source.
        ("Squadron: mission and training ends read from PlayerState, guarded, both shown in the Reminders",() =>
        {
            var timers = Read("Reminders/GameTimers.Dalamud.cs");
            foreach (var member in new[] { "ActiveGcArmyExpedition","SquadronMissionCompletionTimestamp","ActiveGcArmyTraining","SquadronTrainingCompletionTimestamp" })
                Check(timers.Contains("player->" + member,StringComparison.Ordinal),"read: " + member);
            Check(timers.Contains("catch(Exception){mission=null;training=null;}",StringComparison.Ordinal) && timers.Contains("player->IsLoaded",StringComparison.Ordinal),"guarded on its own, only once the player is loaded");
            var page = Read("MainWindow.Reminders.cs");
            Check(page.Contains("SquadronRow(\"SquadronMission\"",StringComparison.Ordinal) && page.Contains("SquadronRow(\"SquadronTraining\"",StringComparison.Ordinal),"both rows");
            Check(File.ReadAllText(Path.GetFullPath("docs/evidence/0.332-squadron-probe.md")).Contains("SquadronTrainingCompletionTimestamp",StringComparison.Ordinal),"fields proved by the probe");
        }),
        // From Squadron451Tests: N-06 (0.451, DEC-027, point 1): the squadron planner from the game sheets; the squadron read on click on the framework thread.
        ("Squadron: success percent = base + each attribute reached + all three (sheet columns), bounded",() =>
        {
            var routine=Mission(20,Set);
            Check(SquadronPlanner.Percent(routine,Set,new(99,99,99))==20 && SquadronPlanner.Percent(routine,Set,new(100,0,0))==40,"base, one");
            Check(SquadronPlanner.Percent(routine,Set,new(100,100,0))==60 && SquadronPlanner.Percent(routine,Set,new(100,100,100))==100,"two, three with the bonus");
            var flagged=new SquadronRequirement(new(100,100,100),0,0,0,100);
            Check(SquadronPlanner.Percent(Mission(0,flagged),flagged,new(500,500,99))==0 && SquadronPlanner.Percent(Mission(0,flagged),flagged,new(100,100,100))==100,"flagged: all or nothing");
            Check(SquadronPlanner.Percent(Mission(100,Set),Set,new(0,0,0))==100 && SquadronPlanner.Percent(Mission(90,Set),Set,new(100,100,100))==100,"bounded at 100");
            Check(SquadronPlanner.Met(new(100,99,100),Set.Required)==2,"met count");
        }),
        ("Squadron: parties of four free members with known attributes, training bonus added, best first",() =>
        {
            SquadronMember Member(int slot,int physical,int mental,int tactical,bool busy=false)=>new(slot,1,30,busy,new(physical,mental,tactical));
            var members=new[]{Member(0,40,10,10),Member(1,40,10,10),Member(2,10,40,10),Member(3,10,40,10),Member(4,10,10,40),Member(5,90,90,90,busy:true),
                new SquadronMember(6,1,30,false,null)};
            var parties=SquadronPlanner.Best(members,new(20,20,20),Mission(20,Set),Set,count:10);
            Check(parties.Count==5 && parties.All(party=>party.Members.Count==4 && party.Members.All(member=>member.Slot is >= 0 and <= 4)),"5 parties of 4 among 5 free known members");
            Check(parties[0].Total==new SquadronStats(20+100,20+100,20+40) && parties[0].Percent==60 && parties[0].Met==2,"bonus added, best first: "+parties[0].Total);
            Check(parties.Zip(parties.Skip(1)).All(pair=>pair.First.Percent>=pair.Second.Percent),"sorted");
            Check(SquadronPlanner.Best(members.Take(3).ToArray(),default,Mission(20,Set),Set).Count==0,"fewer than four");
        }),
        ("Squadron: attributes by class and level from GcArmyMemberGrow (index = level, level 60 after the collection); busy members",() =>
        {
            var byIndex=Enumerable.Range(0,60).Select(level=>level==0 ? default:new SquadronStats(level,level,level)).ToArray();
            Check(SquadronPlanner.GrowStats(byIndex,new(130,31,53),1)==new SquadronStats(1,1,1) && SquadronPlanner.GrowStats(byIndex,new(130,31,53),59)==new SquadronStats(59,59,59),"levels 1 and 59");
            Check(SquadronPlanner.GrowStats(byIndex,new(130,31,53),60)==new SquadronStats(130,31,53),"level 60");
            Check(SquadronPlanner.GrowStats(byIndex,new(130,31,53),0) is null && SquadronPlanner.GrowStats(byIndex,new(130,31,53),61) is null,"outside: unknown");
            var sheets=new SquadronSheets([],new Dictionary<uint,(IReadOnlyList<SquadronStats>,SquadronStats?)>{[3]=(byIndex,new(130,31,53))},new Dictionary<uint,string>());
            var raw=new SquadronRaw(1,0,new(0,0,0),[new(0,1,1,3,60,0,0,0,0,0,0),new(1,2,1,3,10,0,1,0,0,0,0),new(2,3,1,3,10,0,2,0,0,0,0),new(3,4,1,3,10,0,4,0,0,0,0),new(4,5,1,9,10,0,0,0,0,0,0)]);
            var members=SquadronPlanner.Members(raw,sheets);
            Check(members[0].Stats==new SquadronStats(130,31,53) && members[1].Busy && members[2].Busy && !members[3].Busy && members[4].Stats is null,"flags 1 and 2 busy, unknown class");
        }),
        ("Squadron: read on click on the framework thread, fields only, sheets read once, probe log in English, no network",() =>
        {
            var reader=ReadLf("Reminders/SquadronReader.Dalamud.cs");
            Check(reader.Contains("manager->Data==null)return null;") && reader.Contains("Math.Min((int)data->MemberCount,MaximumMembers)")
                && !reader.Contains("GetMemberCount(") && !reader.Contains("GetMember("),"fields only, bounded");
            var plugin=ReadLf("Plugin.cs");
            Check(plugin.Contains("mainWindow.ReadSquadron=()=>framework.RunOnFrameworkThread(()=>") && plugin.Contains("catch(Exception error){log.Warning(error,\"Squadron unreadable (game update?).\");throw;}"),"framework thread, guarded");
            Check(plugin.Contains("new Reminders.SquadronStats(row.Unknown1,row.Unknown2,row.Unknown3)") && plugin.Contains("if(squadronSheets is not null || squadronSheetsFailed)return squadronSheets;"),"level 60 columns, read once");
            var ui=ReadLf("MainWindow.Squadron.cs");
            Check(ui.Split("ReadSquadron!()").Length==2 && ui.Contains("if(ImGui.Button(localization.Get(\"Squadron.Read\")))squadronRead=ReadSquadron!();"),"read on the button only");
            Check(ui.Contains("log.Information(\"Squadron probe:") && !ui.Contains("http") && ReadLf("MainWindow.Reminders.cs").Contains("DrawSquadronPlanner()"),"probe log, offline, wired");
        }),
    ];
}
