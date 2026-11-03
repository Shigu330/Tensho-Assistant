using TenshoAssistant.Duties;

// 0.355 (g, C-01): unlocked / completed duties; unknown stays in the « not completed » lists, only confirmed completions count.
internal static class DutyProgress355Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
    ];
}
