// 0.382 (g, M-11): challenge log and Doman Enclave read from the game — only once loaded, guarded each on its own, no game function called.
internal static class Counters382Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Counters: challenge log only once loaded, Doman Enclave only once read, each guarded, rows in Reminders",() =>
        {
            var timers = Read("Reminders/GameTimers.Dalamud.cs");
            Check(timers.Contains("ui->ContentsNote.State==ContentsNote.ContentsNoteState.Loaded",StringComparison.Ordinal)
                && timers.Contains("enclave->State.Allowance>0",StringComparison.Ordinal),"loaded only");
            Check(timers.Contains("catch(Exception){challenge=null;}",StringComparison.Ordinal) && timers.Contains("catch(Exception){doman=null;}",StringComparison.Ordinal),"guarded");
            var page = Read("MainWindow.Reminders.cs");
            Check(page.Contains("Row(\"ChallengeLog\"",StringComparison.Ordinal) && page.Contains("Row(\"DomanEnclave\"",StringComparison.Ordinal),"rows");
        }),
    ];
}
