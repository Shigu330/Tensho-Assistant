using System.Text.RegularExpressions;

// 0.1452 (X-433): the garden chat's rules (regular expressions of .NET's linear engine) are built off the game's thread when the watcher
// is made with the garden on, and waited for only if a line comes first; the measures of 0.1447 to 0.1452 are kept in docs/evidence.
internal static class GardenRules1452Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Garden rules 1452: built in the background when the garden is on; a line coming first waits for them, none is lost", () =>
        {
            var watcher = Read("src/TenshoAssistant/Reminders/GardenChatWatcher.Dalamud.cs");
            var made = watcher.IndexOf("chat.ChatMessageUnhandled+=OnMessage;", StringComparison.Ordinal);
            // 0.1457 (X-453 M4): the same through BackgroundBuild, whose behaviour MemoryCost1457Tests checks (waiting, failure said once, stop).
            Check(made > 0 && watcher.IndexOf("if(enabled())rulesBuild.Start();", made, StringComparison.Ordinal) > made, "started in the constructor, the garden on");
            Check(watcher.Contains("private GardenChat? Rules()=>rulesBuild.Get();", StringComparison.Ordinal), "waited for, or built at the first line as before");
            Check(watcher.Contains("rulesBuild=new(BuildRules,error=>log.Warning(error,\"Garden detection unavailable", StringComparison.Ordinal), "a failed build is said once, as before");
        }),
        ("Garden rules 1452: the evidence keeps the five loads measured, by thread, and the leads left", () =>
        {
            var evidence = Read("docs/evidence/0.1452-load-allocations.md");
            Check(Regex.Matches(evidence, @"^\| 0\.14(4[789]|5[01]) \| \d\d:\d\d:\d\d \|", RegexOptions.Multiline).Count == 10, "five loads in each half");
            Check(evidence.Contains("Pistes restantes", StringComparison.Ordinal) && evidence.Contains("Leads left", StringComparison.Ordinal), "the leads, both halves");
        }),
    ];
}
