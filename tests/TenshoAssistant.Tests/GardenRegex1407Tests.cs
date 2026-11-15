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
        ("Garden regex 1407: one shared timeout of a second, no 50 ms one left", () =>
        {
            Check(GardenJournal.MatchTimeout == TimeSpan.FromSeconds(1), "one second");
            foreach (var file in new[] { "Reminders/GardenJournal.cs", "Reminders/GardenChat.cs" })
            {
                var text = Read(file);
                Check(!text.Contains("TimeSpan.FromMilliseconds(50)", StringComparison.Ordinal), "no 50 ms timeout in " + file);
            }
            Check(System.Text.RegularExpressions.Regex.Matches(Read("Reminders/GardenChat.cs"), "GardenJournal\\.MatchTimeout").Count == 4, "the four expressions of the chat and the menu use it");
        }),
        ("Garden regex 1407: a timeout is « nothing read » in the journal, the chat line and the menu title", () =>
        {
            var journal = Read("Reminders/GardenJournal.cs");
            Check(journal.Contains("try{match=SlotText.Match(bed.Place);}\n        catch(System.Text.RegularExpressions.RegexMatchTimeoutException){return null;}", StringComparison.Ordinal), "the bed's slot");
            var chat = Read("Reminders/GardenChat.cs");
            Check(chat.Contains("try{return ReadLine(text);}\n        catch(RegexMatchTimeoutException){return null;}", StringComparison.Ordinal), "the chat line");
            Check(chat.Contains("try{return Build(macro);}\n        catch(RegexMatchTimeoutException){return null;}", StringComparison.Ordinal), "the menu title's pattern");
            Check(chat.Contains("try{match=pattern.Match(text.Trim());}\n        catch(RegexMatchTimeoutException){return null;}", StringComparison.Ordinal), "the menu title");
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
