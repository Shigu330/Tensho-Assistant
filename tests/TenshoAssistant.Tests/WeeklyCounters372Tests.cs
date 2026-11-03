// 0.372 (g, M-02): weekly counters read from the game — each guarded on its own, function addresses checked, treasure map time from the game.
internal static class WeeklyCounters372Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Weekly counters: Wondrous Tails, hunt bills, treasure map allowance and roulette state, guarded and address-checked",() =>
        {
            var timers = Read("Reminders/GameTimers.Dalamud.cs");
            Check(timers.Contains("(nint)UIState.MemberFunctionPointers.GetNextMapAllowanceTimestamp!=0",StringComparison.Ordinal)
                && timers.Contains("catch(Exception){bingo=null;}",StringComparison.Ordinal) && timers.Contains("catch(Exception){bills=null;huntBills=null;}",StringComparison.Ordinal)
                && timers.Contains("catch(Exception){nextMap=null;}",StringComparison.Ordinal),"guarded each on its own");
            var page = Read("MainWindow.Reminders.cs");
            Check(page.Contains("var mapReady=timers?.NextMapAllowance ?? mapAt?.AddHours(18);",StringComparison.Ordinal),"game time first, manual time as fallback");
            Check(Read("MainWindow.DutyFinder.cs").Contains("Reminders.GameTimers.RouletteDone((byte)selection.Id)",StringComparison.Ordinal),"roulette state in the Duty Finder panel");
        }),
    ];
}
