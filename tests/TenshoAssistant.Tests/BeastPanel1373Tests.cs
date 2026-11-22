using TenshoAssistant.Beastmaster;

// X-366 (0.1373, essai T0-13 of 14/11, user's capture): the team panel beside the Beastmaster board cut its long lines at the window's edge
// (« Rangs pas encore lus : le rang 5 (la limite du plateau) est supposé pour c… », « Succube épéis… ») and wrote « 0 ennemis sur 1 ».
internal static class BeastPanel1373Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast panel 1373: the count of enemies agrees with its noun, in English and French", () =>
        {
            Check(BeastTeams.HitKey(1) == "BeastUi.TeamHitOne" && BeastTeams.HitKey(0) == "BeastUi.TeamHitOne" && BeastTeams.HitKey(2) == "BeastUi.TeamHit" && BeastTeams.HitKey(5) == "BeastUi.TeamHit", "keys");
            var en = Read("src/TenshoAssistant/Localization/Languages/en.json");
            var fr = Read("src/TenshoAssistant/Localization/Languages/fr.json");
            Check(en.Contains("\"TeamHit\": \"weakness hit: {0} of {1} enemies\"") && en.Contains("\"TeamHitOne\": \"weakness hit: {0} of {1} enemy\""), "en");
            Check(fr.Contains("\"TeamHit\": \"faiblesse touchée : {0} sur {1} ennemis\"") && fr.Contains("\"TeamHitOne\": \"faiblesse touchée : {0} sur {1} ennemi\""), "fr: « 0 sur 1 ennemi », « 2 sur 3 ennemis »");
            Check(!fr.Contains("{0} ennemis sur {1}"), "the old form, which made « 0 ennemis sur 1 », is gone");
        }),
        ("Beast panel 1373: every line of the panel wraps inside its width; the fights' lines indented as a block, not by spaces", () =>
        {
            var page = Read("src/TenshoAssistant/MainWindow.Beastmaster.cs");
            var panel = page[page.IndexOf("internal void DrawBeastBoardPanel()", StringComparison.Ordinal)..page.IndexOf("private void DrawBeastTeam(", StringComparison.Ordinal)];
            Check(panel.Contains("var wrapAt = ImGui.GetCursorPosX() + BeastPanelWrap;") && panel.Contains("DrawBeastPlan(plan, board, catalog, state.Obtained is null, wrapAt, fights: false);")
                && panel.Contains("ImGui.PushTextWrapPos(wrapAt);"), "the panel wraps at its width, its note too");
            var plan = page[page.IndexOf("internal void DrawBeastPlan(", StringComparison.Ordinal)..page.IndexOf("private (int Wiki, object? Seen, int Revision)? beastPlacesFor;", StringComparison.Ordinal)];
            Check(plan.Contains("ImGui.PushTextWrapPos(wrapAt);") && plan.Contains("finally { ImGui.PopTextWrapPos(); }"), "the plan's lines wrap, the wrap always popped");
            Check(!plan.Contains("\"    \"") && !plan.Contains("\"    → \"") && plan.Contains("ImGui.Indent();") && plan.Contains("finally { ImGui.Unindent(); }"), "indented as a block, so that a wrapped line keeps its indent");
            Check(plan.Contains("localization.Format(BeastTeams.HitKey(battle.Enemies), battle.WeaknessesHit, battle.Enemies)"), "the agreed text");
            var window = Read("src/TenshoAssistant/BeastBoardWindow.cs");
            Check(window.Contains("public const float MaxWidth = 460, WrapWidth = MaxWidth - 24;") && window.Contains("owner.BeastPanelWrap = Beastmaster.BeastPanelSize.Wrap(width);")
                && window.Contains("new Vector2(width, Math.Max(300, at.Height))"), "one width for the window and its wrap (X-551: the game's window beside it)");
        }),
    ];
}
