using System.Diagnostics;
using System.Text.RegularExpressions;
using TenshoAssistant.Reminders;

// X-428 (0.1414, CI of 0.1405, run 43692342607): a garden regular expression ran past its timeout on a busy processor; on a slow player's PC
// a planting would not have been read. Every garden expression now runs on the linear engine (NonBacktracking) with no timeout: long and
// hostile inputs read in a time bounded by their length, the same answers as before, and no RegexMatchTimeoutException is possible.
internal static class GardenLinear1414Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + path)).Replace("\r\n", "\n");
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 14, 0, 0, TimeSpan.Zero);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Garden linear 1414: every garden expression is built on the linear engine with no timeout", () =>
        {
            Check((GardenJournal.Linear & RegexOptions.NonBacktracking) != 0 && (GardenJournal.Linear & RegexOptions.CultureInvariant) != 0, "linear and culture-invariant");
            Check(GardenJournal.MatchTimeout == Regex.InfiniteMatchTimeout, "no timeout");
            foreach (var file in new[] { "Reminders/GardenJournal.cs", "Reminders/GardenChat.cs" })
            {
                var text = Read(file);
                var built = Regex.Matches(text, @"new\((?:new Regex\()?[^;]*?,(?:GardenJournal\.)?Linear,(?:GardenJournal\.)?MatchTimeout\)").Count
                    + Regex.Matches(text, @"Regex\.Match(?:es)?\([^;]*?,GardenJournal\.Linear,GardenJournal\.MatchTimeout\)").Count;
                var all = Regex.Matches(text, @"\bnew\(@?""|new Regex\(|Regex\.Match(?:es)?\(").Count;
                Check(built == all && all > 0, $"{file}: {all} expression(s), {built} on the linear engine");
                Check(!text.Contains("RegexOptions.CultureInvariant,", StringComparison.Ordinal), file + ": no expression on the backtracking engine");
            }
        }),
        ("Garden linear 1414: a long place full of numbers and separators is read fast, slot or no slot (a journal file edited by hand)", () =>
        {
            var hostile = string.Concat(Enumerable.Repeat(" · 1 a", 4000)) + " ·";   // 24 001 characters, no slot at the end
            var watch = Stopwatch.StartNew();
            Check(GardenJournal.Slot(new GardenBed { Place = hostile }) is null, "no slot");
            var good = string.Concat(Enumerable.Repeat("x · ", 3000)) + "patch 3, bed 8";
            var slot = GardenJournal.Slot(new GardenBed { Place = good });
            Check(slot is { Patch: 3, Bed: 8 } && slot.Zone.Length == good.Length - " · patch 3, bed 8".Length, "the last separator still splits the zone: " + slot?.Zone.Length);
            Check(watch.Elapsed < TimeSpan.FromSeconds(2), "two long places in " + watch.Elapsed.TotalMilliseconds + " ms");
            Check(GardenJournal.Slot(GardenJournal.Plant("Mist 5 · patch 2, bed 7", "Seed", 24, 0, Now)) is { Zone: "Mist 5", Patch: 2, Bed: 7 }, "a usual place");
        }),
        ("Garden linear 1414: the chat line and the menu title read as before, and their longest accepted inputs stay fast", () =>
        {
            var chat = new GardenChat(["You plant ", null, " in bed ", null, "."], ["You plant the seeds in the flowerpot."], true);
            Check(chat.Read("", "You plant 6 in bed 2.") == new GardenChatEvent(GardenChatKind.PlantedBed, 2, 6), "a planting");
            Check(chat.Read("", "You plant the seeds in the flowerpot.") == new GardenChatEvent(GardenChatKind.PlantedPot, null, null), "a pot");
            Check(chat.Read("Player", "You plant 6 in bed 2.") is null, "a player's line");
            var worst = "You plant " + string.Concat(Enumerable.Repeat(" in bed a1", 18)) + "!";   // under the 200 characters a line may have
            var title = GardenMenuTitle.From("<ordinal(lnum2)> Bed, <ordinal(lnum1)> Patch");
            Check(title?.Read("6th Bed, 2nd Patch") == (2, 6), "a menu title");
            Check(GardenMenuTitle.From("<num(lnum1)> <num(lnum1)>") is null, "a title with the patch twice is not used");
            var watch = Stopwatch.StartNew();
            for (var i = 0; i < 500; i++)
            {
                Check(chat.Read("", worst) is null, "no planting in a line of many numbers");
                Check(title!.Read(new string('1', 79)) is null, "no title in 79 digits");
            }
            Check(watch.Elapsed < TimeSpan.FromSeconds(2), "1000 worst reads in " + watch.Elapsed.TotalMilliseconds + " ms");
        }),
    ];
}
