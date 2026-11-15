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
            // X-435 M2 (0.1421): an invented order (not a reading): a loading screen newer than the result; the gain stays « Other ».
            var loadingNewer = new Dictionary<string, DateTimeOffset> { ["XBMResult"] = Now.AddSeconds(-10), ["NowLoading"] = Now.AddSeconds(-2) };
            Check(GilJournal.ForDelta(GilJournal.Classify(loadingNewer, Now), 2000) == GilCategory.Other, "a loading screen newer than the result: a teleport gain stays « Other »");
            // The 11/13 case (docs/evidence/0.1319-gil-other.md): the log lists the windows newest first (GilWatcher, OrderByDescending):
            // « XBMResult, NeedGreed, NowLoading, XBMContentsMainHUD »: the result was the newest: Beastmaster.
            var logged = new[] { "XBMResult", "NeedGreed", "NowLoading", "XBMContentsMainHUD" };
            var november13 = logged.Select((window, rank) => (window, rank)).ToDictionary(pair => pair.window, pair => Now.AddSeconds(-2 - 3 * pair.rank));
            Check(GilJournal.Classify(november13, Now) == GilCategory.Beastmaster, "the 11/13 case, in the logged order: Beastmaster");
            Check(Read("src/TenshoAssistant/Inventory/GilWatcher.Dalamud.cs").Contains(".OrderByDescending(pair=>pair.Value).Select(pair=>pair.Key)", StringComparison.Ordinal), "the log lists the newest window first");
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
