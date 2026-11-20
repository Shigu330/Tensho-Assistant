using System.Diagnostics;
using TenshoAssistant.Reminders;

// X-427 (0.1407): the CI of 0.1405 failed once on a 50 ms regular expression timeout of the garden (tests in parallel on a busy processor).
// One shared one-second timeout; a timeout reads as « nothing read »; the longest inputs the code accepts stay far below it.
internal static class GardenRegex1407Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Garden regex 1407: one shared timeout, no 50 ms one left (X-428, 0.1414: infinite, on the linear engine)", () =>
        {
            Check(GardenJournal.MatchTimeout == System.Text.RegularExpressions.Regex.InfiniteMatchTimeout, "infinite since X-428");
            foreach (var file in new[] { "Reminders/GardenJournal.cs", "Reminders/GardenChat.cs" })
            {
                var text = Read(file);
                Check(!text.Contains("TimeSpan.FromMilliseconds(50)", StringComparison.Ordinal), "no 50 ms timeout in " + file);
            }
            Check(System.Text.RegularExpressions.Regex.Matches(Read("Reminders/GardenChat.cs"), "GardenJournal\\.MatchTimeout").Count == 5, "the five expressions of the chat, the menu and (X-581, 0.1572) the bed's state lines use it");
        }),
        ("Garden regex 1407: no timeout can happen in the journal, the chat line and the menu title (X-428, 0.1414: linear engine, no catch left)", () =>
        {
            var journal = Read("Reminders/GardenJournal.cs");
            Check(journal.Contains("new(@\"^(.+) · \\D*(\\d+)\\D+(\\d+)$\",Linear,MatchTimeout)", StringComparison.Ordinal), "the bed's slot");
            var chat = Read("Reminders/GardenChat.cs");
            Check(System.Text.RegularExpressions.Regex.Matches(chat, "GardenJournal\\.Linear,GardenJournal\\.MatchTimeout").Count == 5, "the chat line, the menu title and (X-581, 0.1572) the bed's state lines");
            foreach (var text in new[] { journal, chat })
                Check(!text.Contains("RegexMatchTimeoutException", StringComparison.Ordinal), "no timeout to catch");
        }),
        ("Garden regex 1407: behaviour: the slots read as before, and the worst accepted place takes far less than the timeout", () =>
        {
            var now = new DateTimeOffset(2026, 11, 15, 9, 0, 0, TimeSpan.Zero);
            var slot = GardenJournal.Slot(GardenJournal.Plant("Plot · patch 2, bed 7", "Seed", 24, 0, now));
            Check(slot is { Zone: "Plot", Patch: 2, Bed: 7 }, "read: " + slot);
            Check(GardenJournal.Slot(GardenJournal.Plant("Pot 3", "Seed", 24, 0, now)) is null, "a pot has no slot");
            var worst = new string('a', 10) + " · " + string.Concat(Enumerable.Repeat("a1", 13));
            var watch = Stopwatch.StartNew();
            for (var i = 0; i < 200; i++) Check(GardenJournal.Slot(GardenJournal.Plant(worst[..Math.Min(worst.Length, 40)], "Seed", 24, 0, now)) is null, "no slot in a place of many numbers");
            Check(watch.Elapsed < TimeSpan.FromSeconds(1), "200 worst reads in " + watch.Elapsed.TotalMilliseconds + " ms");
        }),
    ];
}
