// Test audit of 2026-11-09, batch 2 (0.1021): DutyProgress355Tests, DutySaved990Tests merged into DutyProgressTests; every case is kept.
using TenshoAssistant.Duties;
using TenshoAssistant.Maps;
internal static class DutyProgressTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From DutyProgress355Tests: 0.355 (g, C-01): unlocked / completed duties; unknown stays in the « not completed » lists, only confirmed completions count.
        ("Duty progress: filters keep unknown statuses out of « completed » and inside « not completed »",() =>
        {
            Check(DutyProgress.Matches(DutyProgressFilter.NotCompleted,null) && DutyProgress.Matches(DutyProgressFilter.NotCompleted,DutyStatus.Locked)
                && !DutyProgress.Matches(DutyProgressFilter.NotCompleted,DutyStatus.Completed),"not completed");
            Check(DutyProgress.Matches(DutyProgressFilter.UnlockedNotCompleted,DutyStatus.Unlocked) && DutyProgress.Matches(DutyProgressFilter.UnlockedNotCompleted,null)
                && !DutyProgress.Matches(DutyProgressFilter.UnlockedNotCompleted,DutyStatus.Locked),"unlocked, not completed");
            Check(!DutyProgress.Matches(DutyProgressFilter.Completed,null) && DutyProgress.Matches(DutyProgressFilter.All,null),"completed only when confirmed");
            var duties = new[] { new DutyDefinition(1,10,100,"A","Dungeon",15), new DutyDefinition(2,20,200,"B","Dungeon",16), new DutyDefinition(3,30,300,"C","Trial",20) };
            var progress = new DutyProgressState(new Dictionary<uint,DutyStatus>{[10]=DutyStatus.Completed,[20]=DutyStatus.Unlocked},DateTimeOffset.UnixEpoch);
            Check(progress.Of(30) is null && DutyProgress.Count(duties,progress) == (1,2),"counts only known statuses");
            var reader = Read("Duties/DutyProgressReader.Dalamud.cs");
            Check(reader.Contains("(nint)UIState.MemberFunctionPointers.IsInstanceContentCompleted==0",StringComparison.Ordinal)
                && reader.Contains("framework.IsInFrameworkUpdateThread",StringComparison.Ordinal)
                && reader.Contains("TimeSpan.FromMilliseconds(2)",StringComparison.Ordinal),"pointer checked, framework thread, 2 ms budget");
            Check(Read("Plugin.cs").Contains("() => dutyProgress?.Dispose()",StringComparison.Ordinal),"reader disposed");
        }),
        // From DutySaved990Tests: X-106 (0.990): the duties' completion kept per character on disk, shown logged out as « saved ».
        ("Duty saved 990: round trip per character with the last one, broken file read as none",() =>
        {
            var at=DateTimeOffset.UtcNow;
            var all=new Dictionary<ulong,DutyProgress.Saved>{[7]=new(new(){[1]=DutyStatus.Completed,[2]=DutyStatus.Unlocked},at)};
            Check(DutyProgress.Read(DutyProgress.Write(all,7)) is { } back && back.Last==7 && back.All[7].ByInstance[1]==DutyStatus.Completed,"round trip");
            Check(DutyProgress.Read("{x")==null,"broken");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Duties/DutyProgressReader.Dalamud.cs"));
            Check(reader.Contains("writer.Enqueue(\"Duty progress could not be saved.\"") && reader.Contains("new DutyProgressState(last.ByInstance,last.ReadAt,true)"),"saved, shown");
        }),
        ("Duty unlocks (T13): the finder row first, then the quest's own column; unknown stays 0",() =>
        {
            Check(TenshoAssistant.Duties.DutyUnlocks.FromFinder(1,67092,0,0)==67092,"le Palais des morts");
            Check(TenshoAssistant.Duties.DutyUnlocks.FromFinder(0,0,1,69408)==69408,"second pair");
            Check(TenshoAssistant.Duties.DutyUnlocks.FromFinder(2,20045,1,69501)==69501,"another duty first, then the quest");
            Check(TenshoAssistant.Duties.DutyUnlocks.FromFinder(4,594,0,0)==0,"not a quest");
            var duties=TenshoAssistant.Duties.DutyUnlocks.Apply([new TenshoAssistant.Duties.DutyDefinition(1,10,100,"A","Dungeon",15),new TenshoAssistant.Duties.DutyDefinition(2,20,200,"B","Dungeon",16,0,77),new TenshoAssistant.Duties.DutyDefinition(3,30,300,"C","Dungeon",17)],new Dictionary<uint,uint>{[10]=65579,[20]=99});
            Check(duties[0].UnlockQuest==65579 && duties[1].UnlockQuest==77 && duties[2].UnlockQuest==0,"filled only where unknown");
            var catalog=new TenshoAssistant.Duties.DutyCatalog(duties);
            Check(catalog.UnlockedBy(65579).Single().DutyId==1 && !catalog.UnlockedBy(0).Any(),"and the other way round");
        }),
    ];
}
