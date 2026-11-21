using System.Text.Json;

// 0.1687 (X-706, review of 0.1682 to 0.1686): M1, a text shown to players never speaks of « the user » (for the player who reads it, the user
// is the player): the rule under Chum is « Tensho's rule »; M2, the evidence says where that rule is shown (each cell and the « Chum » badge, no
// column header); M3, the JIT test checks that the probes compiled before the window stay out of it (ChatFrameJit1633Tests).
internal static class Review1686Fixes1687Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    private static Dictionary<string, string> Texts(string code)
    {
        var values = new Dictionary<string, string>();
        using var document = JsonDocument.Parse(Read($"src/TenshoAssistant/Localization/Languages/{code}.json").TrimStart('﻿'));
        foreach (var section in document.RootElement.EnumerateObject())
            if (section.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var key in section.Value.EnumerateObject())
                    if (key.Value.ValueKind == JsonValueKind.String) values[section.Name + "." + key.Name] = key.Value.GetString()!;
            }
            else if (section.Value.ValueKind == JsonValueKind.String) values[section.Name] = section.Value.GetString()!;
        return values;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review fixes 1687: M1, the rule under Chum is Tensho's, said in both languages", () =>
        {
            var en = Texts("en");
            var fr = Texts("fr");
            Check(en["FishingPanel.ChumTip"].Contains("a rule of Tensho's (neither the game nor the source gives a figure)", StringComparison.Ordinal), en["FishingPanel.ChumTip"]);
            Check(en["FishingPanel.BiteTimeChum"] == "Under Chum: half, Tensho's rule (neither the game nor the source gives a figure).", en["FishingPanel.BiteTimeChum"]);
            Check(fr["FishingPanel.ChumTip"].Contains("règle de Tensho (ni le jeu ni la source ne donnent de chiffre)", StringComparison.Ordinal), fr["FishingPanel.ChumTip"]);
            Check(fr["FishingPanel.BiteTimeChum"] == "Sous Amorçage : moitié, règle de Tensho (ni le jeu ni la source ne donnent de chiffre).", fr["FishingPanel.BiteTimeChum"]);
        }),
        ("Review fixes 1687: M1, no text shown to players speaks of « the user »", () =>
        {
            foreach (var (code, words) in new[] { ("en", new[] { "the user", "The user" }), ("fr", new[] { "l'utilisateur", "L'utilisateur", "l’utilisateur" }) })
            {
                var found = Texts(code).Where(text => words.Any(word => text.Value.Contains(word, StringComparison.Ordinal))).Select(text => text.Key).ToList();
                Check(found.Count == 0, code + ": " + string.Join(", ", found));
            }
        }),
        ("Review fixes 1687: M2, the evidence says where the rule is shown: each cell and the « Chum » badge, no column header", () =>
        {
            var evidence = Read("docs/evidence/0.1685-fishing-bite-stopwatch.md");
            Check(!evidence.Contains("tooltip of the column\n  « Bite time » and of each cell", StringComparison.Ordinal) && !evidence.Contains("l'info-bulle de la colonne « Temps de touche » et de chaque case", StringComparison.Ordinal), "the header tooltip that does not exist is no longer claimed");
            Check(evidence.Contains("in the one of the « Chum » badge (no column header has a tooltip)", StringComparison.Ordinal) && evidence.Contains("dans celle du badge « Amorçage » (aucun en-tête de colonne n'a\n  d'info-bulle)", StringComparison.Ordinal), "both halves say where");
            var clock = Read("src/TenshoAssistant/MainWindow.FishingClock.cs");
            Check(clock.Contains("Tip(\"FishingPanel.ChumTip\");", StringComparison.Ordinal) && clock.Contains("localization.Get(\"FishingPanel.BiteTimeChum\")", StringComparison.Ordinal), "the badge and each cell, as said");
        }),
        ("Review fixes 1687: M3, the JIT test keeps the probes before the window out of its names", () =>
        {
            var test = Read("tests/TenshoAssistant.Tests/ChatFrameJit1633Tests.cs");
            Check(test.Contains("&& !heard.Any(name=>name.Contains(\"Probe1633\",StringComparison.Ordinal))", StringComparison.Ordinal), "the probes checked out of the window");
            Check(test.Contains("new System.Reflection.Emit.DynamicMethod(\"Probe1633\"", StringComparison.Ordinal), "the probes are named so");
        }),
    ];
}
