// Test audit of 2026-11-09, batch 2 (0.1021): CharacterStatus374Tests, CharacterTimers936Tests merged into CharacterStatusTests; every case is kept.
using TenshoAssistant.Character;
using TenshoAssistant.Maps;
internal static class CharacterStatusTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));
    private static readonly DateTimeOffset Now = new(2026,11,3,12,0,0,TimeSpan.Zero);
    private static readonly DateTimeOffset TimersNow=new(2026,11,8,20,0,0,TimeSpan.Zero);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From CharacterStatus374Tests: 0.374 (g, M-04): one status file per character — fresh reads merged over what was kept, unknown stays unknown, roulettes only for
        // today, files refused when they hold another character, « done » boxes per character with the shared ones moved once.
        ("Character status: a fresh read wins where it knows, the rest stays as last read",() =>
        {
            var old = new CharacterStatus{ContentId=7,Name="A (W)",SeenAt=Now.AddDays(-1),Ventures=[new("R",Now.AddHours(1))],HuntBills=2,BingoHeld=true,BingoStickers=5,
                RoulettesDone=1,RoulettesTotal=8,RoulettesAt=Now.AddDays(-1),Done=new(){["Supply"]=Now.AddDays(-1)}};
            var fresh = new CharacterStatus{ContentId=7,Name="",SeenAt=Now,HuntBills=3};
            var merged = CharacterStatuses.Merge(old,fresh);
            Check(merged.Name=="A (W)" && merged.Ventures!.Length==1 && merged.HuntBills==3 && merged.BingoStickers==5 && merged.Done!.Count==1 && merged.SeenAt==Now,"merged");
            Check(CharacterStatuses.Merge(old,fresh with{ContentId=8}).Ventures is null,"another character is never merged");
            Check(CharacterStatuses.Changed(old,merged) && !CharacterStatuses.Changed(merged,merged with{SeenAt=Now.AddSeconds(30)}),"only content changes ask for a save");
            Check(CharacterStatuses.Changed(merged,merged with{Ventures=[new("R",Now.AddHours(2))]}),"venture change seen");
        }),
        ("Character status: ventures and roulettes as the overview shows them",() =>
        {
            var status = new CharacterStatus{ContentId=1,Ventures=[new("A",Now.AddMinutes(-5)),new("B",Now.AddHours(2)),new("C",Now.AddHours(1))]};
            Check(CharacterStatuses.Ventures(status,Now)==(1,2,Now.AddHours(1)),"ready, running, next end");
            Check(CharacterStatuses.Ventures(status with{Ventures=null},Now) is null,"unknown stays unknown");
            var roulettes = status with{RoulettesDone=3,RoulettesTotal=8,RoulettesAt=Now.AddHours(-1)};
            Check(CharacterStatuses.RoulettesToday(roulettes,Now.AddHours(-2))==(3,8),"read after the reset");
            Check(CharacterStatuses.RoulettesToday(roulettes,Now.AddMinutes(-30)) is null,"read before the reset means nothing");
        }),
        ("Character status: one file per character, another character's file refused, the overview's erase deletes the file",() =>
        {
            var folder = Path.Combine(Path.GetTempPath(),"tensho-characters-"+Guid.NewGuid().ToString("N"));
            try
            {
                CharacterStatuses.Write(folder,new CharacterStatus{ContentId=11,Name="A",SeenAt=Now,HuntBills=1});
                CharacterStatuses.Write(folder,new CharacterStatus{ContentId=22,Name="B",SeenAt=Now});
                File.Copy(Path.Combine(folder,"22.json"),Path.Combine(folder,"33.json"));
                var failed = new List<string>();
                var all = CharacterStatuses.ReadAll(folder,(path,_)=>failed.Add(Path.GetFileName(path)));
                Check(all.Count==2 && all.Any(status=>status.ContentId==11 && status.HuntBills==1) && failed.SequenceEqual(["33.json"]),"read back, mismatch refused");
                var thrown = false;
                try{CharacterStatuses.Write(folder,new CharacterStatus());}catch(ArgumentException){thrown=true;}
                Check(thrown,"no character, no file");
            }
            finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
            var watcher = Read("Character/CharacterStatusWatcher.Dalamud.cs");
            Check(watcher.Contains("if(id==0 || id==current)return false;",StringComparison.Ordinal),"the logged-in character is never erased");
            Check(watcher.Contains("if(migrated){if(WriteLocked(id)){configuration.ReminderDone=null;saveConfiguration();}}",StringComparison.Ordinal),"shared boxes moved once, only after the file is written");
            Check(Read("MainWindow.Reminders.cs").Contains("var done=ResetSchedule.DoneSince(ReminderDoneAt(key),reset);",StringComparison.Ordinal),"done boxes per character");
        }),
        // From CharacterTimers936Tests: X-76 P9 (0.936): leves, deliveries, allied quests and squadron kept per character; merged like the rest; daily and weekly counts only since their reset.
        ("Character timers 936: merge keeps the last known, counts shown only since their reset, change detected",() =>
        {
            var old=new CharacterStatus{ContentId=1,LeveAllowances=40,DeliveriesLeft=6,AlliedQuestsLeft=3,TimersAt=TimersNow.AddHours(-1),SquadronMissionDone=TimersNow.AddHours(2)};
            var fresh=new CharacterStatus{ContentId=1,SeenAt=TimersNow};
            var merged=CharacterStatuses.Merge(old,fresh);
            Check(merged.LeveAllowances==40 && merged.DeliveriesLeft==6 && merged.AlliedQuestsLeft==3 && merged.TimersAt==old.TimersAt && merged.SquadronMissionDone==old.SquadronMissionDone,"kept");
            var newer=CharacterStatuses.Merge(old,fresh with{LeveAllowances=41,AlliedQuestsLeft=2,TimersAt=TimersNow});
            Check(newer.LeveAllowances==41 && newer.AlliedQuestsLeft==2 && newer.DeliveriesLeft==6 && newer.TimersAt==TimersNow,"fresh wins");
            Check(CharacterStatuses.Changed(old,old with{LeveAllowances=41}) && !CharacterStatuses.Changed(old,old with{TimersAt=TimersNow}),"change");
            Check(CharacterStatuses.DailyWeekly(old,TimersNow.AddHours(-2),TimersNow.AddDays(-2))==(3,6),"since reset");
            Check(CharacterStatuses.DailyWeekly(old,TimersNow.AddMinutes(-10),TimersNow.AddDays(-2))==(null,6),"daily reset passed");
            Check(CharacterStatuses.DailyWeekly(new CharacterStatus{ContentId=1},TimersNow,TimersNow)==(null,null),"never read");
        }),
        ("Job progress (T21): one row per experience slot, the job standing for its class, in the game's order",() =>
        {
            // ClassJob rows: 1 gladiator and 19 paladin share slot 1; 8 carpenter alone in slot 8.
            var jobs=TenshoAssistant.Gear.JobProgressList.Jobs(new Dictionary<uint,int>{[1]=1,[19]=1,[8]=8},new Dictionary<uint,int>{[19]=1,[8]=30});
            Check(string.Join(",",jobs.Select(job=>job.Job))=="19,8","paladin first, then carpenter");
            Check(new TenshoAssistant.Gear.JobProgressRow(19,0,0,0) is { Unlocked:false },"level 0: not unlocked");
            Check(Math.Abs(new TenshoAssistant.Gear.JobProgressRow(19,90,50,200).Fraction-0.25f)<0.001f,"share of the level");
            Check(new TenshoAssistant.Gear.JobProgressRow(19,100,0,0).Fraction==1,"nothing to earn");
            Check(new TenshoAssistant.Gear.JobProgressRow(19,90,500,200).Fraction==1,"never past the bar");
        }),
        ("Company actions (T27): the statuses paired with the timers in order, or no end at all",() =>
        {
            var now=new DateTimeOffset(2026,11,6,10,0,0,TimeSpan.Zero);
            // The reading of 2026-11-06: 19131 and 19135 seconds at the last update, 76 seconds ago.
            var read=TenshoAssistant.Reminders.CompanyBenefits.Build([364,2932],[19131,19135,0],76,now);
            Check(read[0].Status==364 && read[0].Ends==now.AddSeconds(19055),"first status, first timer");
            Check(read[1].Status==2932 && read[1].Ends==now.AddSeconds(19059),"second status, second timer");
            Check(TenshoAssistant.Reminders.CompanyBenefits.Build([364,2932],[0,0,0],0,now).All(benefit=>benefit.Ends is null),"window not opened: unknown");
            Check(TenshoAssistant.Reminders.CompanyBenefits.Build([364],[19131,19135,0],0,now).All(benefit=>benefit.Ends is null),"counts differ: unknown");
            Check(TenshoAssistant.Reminders.CompanyBenefits.Build([364],[50],76,now).All(benefit=>benefit.Ends is null),"run out: unknown");
            Check(TenshoAssistant.Reminders.CompanyBenefits.Build([],[0,0,0],0,now).Count==0,"none active");
        }),
    ];
}
