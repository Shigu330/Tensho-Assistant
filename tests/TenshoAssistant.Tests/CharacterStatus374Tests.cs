using TenshoAssistant.Character;

// 0.374 (g, M-04): one status file per character — fresh reads merged over what was kept, unknown stays unknown, roulettes only for
// today, files refused when they hold another character, « done » boxes per character with the shared ones moved once.
internal static class CharacterStatus374Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));
    private static readonly DateTimeOffset Now = new(2026,11,3,12,0,0,TimeSpan.Zero);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
    ];
}
