// 0.333 (DEC-023 point 6, user request: mission AND training): the squadron's mission and training ends are read from PlayerState,
// guarded on their own, and shown in the Reminders with an alarm. Native code is outside the test project: pinned in the source.
internal static class Squadron333Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly string Source = Path.GetFullPath("src/TenshoAssistant");
    private static string Read(string file) => File.ReadAllText(Path.Combine(Source,file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
    ];
}
