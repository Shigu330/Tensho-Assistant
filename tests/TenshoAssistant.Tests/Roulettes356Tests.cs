// 0.356 (g, C-02): daily duty roulettes, done today and role bonus; native calls guarded, rows from the game data.
internal static class Roulettes356Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Daily roulettes: completion asked only once resolved, role bonus indexed by its bonus row, rows with a completion index",() =>
        {
            var timers = Read("Reminders/GameTimers.Dalamud.cs");
            Check(timers.Contains("(nint)InstanceContent.MemberFunctionPointers.IsRouletteIncomplete!=0",StringComparison.Ordinal)
                && timers.Contains("canAsk ? !content->IsRouletteIncomplete(roulette.Id):null",StringComparison.Ordinal),"pointer checked, unknown when not");
            Check(timers.Contains("roulette.BonusRow<bonuses.Length && bonuses[roulette.BonusRow]",StringComparison.Ordinal)
                && timers.Contains("catch(Exception){daily=null;}",StringComparison.Ordinal),"bounded index, guarded on its own");
            var plugin = Read("Plugin.cs");
            Check(plugin.Contains("row.IsInDutyFinder && row.CompletionArrayIndex>=0",StringComparison.Ordinal),"daily = finder roulettes with a completion index");
            Check(Read("MainWindow.Reminders.cs").Contains("GameTimers.Read(DailyRoulettes?.Invoke(),ChallengeRows?.Invoke(),CompanyActionNames)",StringComparison.Ordinal),"read with the Calendar counters");
            Check(File.Exists(Path.GetFullPath("docs/evidence/0.356-roulettes.md")),"evidence");
        }),
    ];
}
