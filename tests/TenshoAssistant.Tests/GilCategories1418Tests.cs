using TenshoAssistant.Inventory;

// 0.1418, X-18: fewer gains « Other » in the gil journal: the journal's reward window (quests and leves), the custom deliveries' window and
// the Beastmaster trial's result give their category to a gain; a spending next to them stays « Other »; every category has an icon and
// its two texts (docs/evidence/0.1418-gil-windows.md).
internal static class GilCategories1418Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 15, 0, 0, TimeSpan.Zero);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Gil categories 1418: each reward window gives its category to a gain, the newest window winning; a spending stays « Other »", () =>
        {
            foreach (var (window, category) in new[] { ("JournalResult", GilCategory.Quest), ("SatisfactionSupply", GilCategory.CustomDeliveries), ("XBMResult", GilCategory.Beastmaster) })
            {
                var seen = new Dictionary<string, DateTimeOffset> { [window] = Now.AddSeconds(-3) };
                var read = GilJournal.Classify(seen, Now);
                Check(read == category, window + ": " + read);
                Check(GilJournal.ForDelta(read, 2000) == category && GilJournal.ForDelta(read, -500) == GilCategory.Other, window + ": gains only");
                Check(GilJournal.Classify(seen, Now.AddSeconds(20)) == GilCategory.Other, window + ": 15 seconds at most");
            }
            var both = new Dictionary<string, DateTimeOffset> { ["XBMResult"] = Now.AddSeconds(-10), ["NowLoading"] = Now.AddSeconds(-2) };
            Check(GilJournal.ForDelta(GilJournal.Classify(both, Now), 2000) == GilCategory.Other, "the 11/13 case: a loading screen after the result is newer: a teleport gain stays « Other »");
            var after = new Dictionary<string, DateTimeOffset> { ["NowLoading"] = Now.AddSeconds(-10), ["XBMResult"] = Now.AddSeconds(-2) };
            Check(GilJournal.Classify(after, Now) == GilCategory.Beastmaster, "the result newest: Beastmaster");
        }),
        ("Gil categories 1418: the values written before keep their number; every category has an icon and an English and French text", () =>
        {
            Check((int)GilCategory.ChallengeLog == 11 && (int)GilCategory.Quest == 12 && (int)GilCategory.CustomDeliveries == 13 && (int)GilCategory.Beastmaster == 14, "appended");
            var page = Read("src/TenshoAssistant/MainWindow.GilJournal.cs");
            var en = Read("src/TenshoAssistant/Localization/Languages/en.json");
            var fr = Read("src/TenshoAssistant/Localization/Languages/fr.json");
            foreach (var category in Enum.GetValues<GilCategory>())
            {
                if (category != GilCategory.Other) Check(page.Contains("GilCategory." + category + "=>Dalamud.Interface.FontAwesomeIcon.", StringComparison.Ordinal), category + ": an icon");
                Check(en.Contains("\"Category" + category + "\": ", StringComparison.Ordinal) && fr.Contains("\"Category" + category + "\": ", StringComparison.Ordinal), category + ": two texts");
            }
            Check(GilJournal.Windows.Count(window => window.Category is GilCategory.Quest or GilCategory.CustomDeliveries or GilCategory.Beastmaster) == 3, "one window each");
            var evidence = Read("docs/evidence/0.1418-gil-windows.md");
            Check(evidence.Contains("`JournalResult`") && evidence.Contains("`SatisfactionSupply`") && evidence.Contains("`XBMResult`") && evidence.Contains("present"), "the probe");
        }),
    ];
}
