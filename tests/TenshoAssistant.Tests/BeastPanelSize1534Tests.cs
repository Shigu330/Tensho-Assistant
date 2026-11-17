using TenshoAssistant.Beastmaster;

// 0.1534 (X-551, the user's request of 17/11): the team panel is never wider than the game's window beside it (nor than 460, nor under 300), its
// lines wrap inside it; the team is one row per beast as in the game's « Formation de l'équipe ».
internal static class BeastPanelSize1534Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast panel size 1534: the game's window beside it bounds the width; the wrap is inside it", () =>
        {
            Check(BeastPanelSize.Width(380) == 380 && BeastPanelSize.Wrap(380) == 356, "as wide as a 380-pixel window, lines at 356");
            Check(BeastPanelSize.Width(900) == 460 && BeastPanelSize.Width(120) == 300, "between 300 and 460");
            Check(BeastPanelSize.Width(0) == 460 && BeastPanelSize.Width(float.NaN) == 460, "not read: as before");
            Check(BeastPanelSize.Wrap(BeastPanelSize.Most) == 436, "the old wrap at the widest");
        }),
        ("Beast panel size 1534: the window sets the width and the wrap; the team is a table of rows", () =>
        {
            var window = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/BeastBoardWindow.cs"));
            Check(window.Contains("var width = Beastmaster.BeastPanelSize.Width(at.Party ? at.PartyWidth : at.Width);"), "the window beside it");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("BeginDataTable(\"###BeastTeamRows\", 3)") && page.Contains("localization.Format(\"BeastUi.TeamRank\", pick.Rank)"), "one row per beast");
        }),
    ];
}
