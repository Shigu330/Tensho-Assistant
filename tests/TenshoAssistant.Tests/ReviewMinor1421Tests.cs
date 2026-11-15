// 0.1421, X-435 (review of block 0.1413-0.1419), minor findings M2, M3, M4: the 11/13 gil case in the logged order, the garden timings only a
// guard against a runaway engine, the retainer table fitting the page (Gil and Items fixed to their numbers, help lines wrapped).
using TenshoAssistant.Inventory;

internal static class ReviewMinor1421Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review minor 1421 (M2): the evidence of 11/13 lists XBMResult first, and the log writes the newest window first: a Beastmaster gain", () =>
        {
            var evidence = Read("docs/evidence/0.1418-gil-windows.md");
            Check(evidence.Contains("windows seen in the last 15 s: XBMResult, NeedGreed, NowLoading, XBMContentsMainHUD", StringComparison.Ordinal), "the logged line");
            var now = new DateTimeOffset(2026, 11, 13, 20, 0, 0, TimeSpan.Zero);
            var seen = new Dictionary<string, DateTimeOffset> { ["XBMResult"] = now.AddSeconds(-1), ["NeedGreed"] = now.AddSeconds(-4), ["NowLoading"] = now.AddSeconds(-7), ["XBMContentsMainHUD"] = now.AddSeconds(-9) };
            Check(GilJournal.ForDelta(GilJournal.Classify(seen, now), 2000) == GilCategory.Beastmaster, "the +2,000 of 11/13 is the Beastmaster's");
            var test = Read("tests/TenshoAssistant.Tests/GilCategories1418Tests.cs");
            Check(!test.Contains("the 11/13 case: a loading screen after the result is newer", StringComparison.Ordinal) && test.Contains("the 11/13 case, in the logged order: Beastmaster", StringComparison.Ordinal), "the 1418 test names the right case");
        }),
        ("Review minor 1421 (M3): the garden tests bound no speed, only a runaway engine", () =>
        {
            var test = Read("tests/TenshoAssistant.Tests/GardenLinear1414Tests.cs");
            Check(!test.Contains("TimeSpan.FromSeconds(2)", StringComparison.Ordinal) && test.Contains("Runaway = TimeSpan.FromSeconds(30)", StringComparison.Ordinal), "a 30 s guard, no 2 s bound");
        }),
        ("Review minor 1421 (M4): the retainer table fits the page: Gil and Items fixed to their numbers, the help lines wrapped", () =>
        {
            var page = Read("src/TenshoAssistant/MainWindow.CharacterStatus.cs");
            Check(page.Contains("key is \"Gil\" or \"Items\" ? ImGuiTableColumnFlags.WidthFixed:ImGuiTableColumnFlags.None", StringComparison.Ordinal), "the number columns fit");
            foreach (var key in new[] { "CharacterStatus.Help", "CharacterStatus.RetainersHelp" })
                Check(page.Contains("ImGui.PushTextWrapPos(0);\n        ImGui.TextDisabled(localization.Get(\"" + key + "\"));\n        ImGui.PopTextWrapPos();", StringComparison.Ordinal), key + " wraps");
        }),
    ];
}
