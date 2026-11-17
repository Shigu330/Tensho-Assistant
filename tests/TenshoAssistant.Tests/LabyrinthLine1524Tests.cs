using TenshoAssistant.Competitive;

// 0.1524 (X-552, seen by the master in dalamud.log of 17/11: « Labyrinth reading » every 1.5 s, 188 lines in one session): the line names only the
// labyrinth's windows and its loading screens, so a tooltip or a cast bar coming and going no longer writes it again.
internal static class LabyrinthLine1524Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    // Two consecutive readings of 17/11 10:20:01 and 10:20:01.993 (the game's windows as the log wrote them): only « Tooltip » differs.
    private static readonly string[] Before = ["AreaMap", "CastBarEnemy", "ChatLog", "ChatLogPanel_0", "CursorAddon", "JobHudXBM0", "JobHudXBM1", "NamePlate", "ScenarioTree", "ScreenLog", "XBMContentsMainHUD", "_ActionBar", "_ActionBar01"];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Labyrinth line 1524: windows that come and go do not change the line; the labyrinth's windows and loadings do", () =>
        {
            var first = string.Join(", ", CompetitiveZones.LabyrinthWindows(Before));
            var tooltip = string.Join(", ", CompetitiveZones.LabyrinthWindows([.. Before, "Tooltip"]));
            var castBarGone = string.Join(", ", CompetitiveZones.LabyrinthWindows(Before.Where(name => name != "CastBarEnemy")));
            Check(first == tooltip && first == castBarGone, $"same line: {first} / {tooltip} / {castBarGone}");
            Check(first == "JobHudXBM0, JobHudXBM1, XBMContentsMainHUD", "the labyrinth's own windows, sorted: " + first);
            Check(string.Join(", ", CompetitiveZones.LabyrinthWindows([.. Before, "XBMStageDetailList"])) != first, "a board window opening changes it");
            Check(CompetitiveZones.LabyrinthWindows(["NowLoading", "FadeMiddle", "NowLoading"]).SequenceEqual(["FadeMiddle", "NowLoading"]), "loadings kept, each once");
        }),
        ("Labyrinth line 1524: the reading writes its line from these windows, once per change", () =>
        {
            var gate = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Competitive/CompetitiveGate.Dalamud.cs"));
            Check(gate.Contains("windows: {string.Join(\", \",CompetitiveZones.LabyrinthWindows(names))}") && gate.Contains("if(seen==labyrinthSeen)return;"), "filtered, written on change");
        }),
    ];
}
