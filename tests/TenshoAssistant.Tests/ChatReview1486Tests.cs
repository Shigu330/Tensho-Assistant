using System.Text.RegularExpressions;

// 0.1486 (X-503 M1, M2: review of 0.1482): the chat window's narrow margins stay its own (its popups, menus and tooltips get the usual ones);
// the settings of X-484 go through shared helpers, so the settings' search and SETTINGS.md find them.
internal static class ChatReview1486Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    private static readonly string[] Keys = ["ChatWindow.HiddenPlayers", "ChatWindow.HighlightedPlayers", "ChatWindow.KeepWords", "ChatWindow.HideWords",
                                             "ChatWindow.WordsMatchCase", "ChatWindow.Unlisted", "ChatWindow.FiltersUse"];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat review 1486: the narrow margins are given back to what the chat window opens (X-503 M1)", () =>
        {
            var panel = Read("src/TenshoAssistant/PanelWindow.cs");
            var pre = panel[panel.IndexOf("public override void PreDraw()", StringComparison.Ordinal)..panel.IndexOf("public override void PostDraw()", StringComparison.Ordinal)];
            Check(pre.IndexOf("outerPadding = ImGui.GetStyle().WindowPadding;", StringComparison.Ordinal) is var kept and > 0
                  && kept < pre.IndexOf("ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, padding);", StringComparison.Ordinal), "the usual margins kept before the narrow ones");
            var draw = panel[panel.IndexOf("public override void Draw()", StringComparison.Ordinal)..];
            draw = draw[..draw.IndexOf("\n    }", StringComparison.Ordinal)];
            var back = draw.IndexOf("if (inner) ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, outerPadding);", StringComparison.Ordinal);
            var contents = draw.IndexOf("panel.Contents();", StringComparison.Ordinal);
            Check(back > 0 && contents > back, "given back before the contents (after Begin: the window keeps its own)");
            Check(draw.Contains("finally { if (inner) ImGui.PopStyleVar(); }", StringComparison.Ordinal), "and taken off after, even on an error");
        }),
        ("Chat review 1486: the settings of X-484 are in the search and in SETTINGS.md (X-503 M2)", () =>
        {
            var index = Read("src/TenshoAssistant/SettingsIndex.cs");
            foreach (var key in Keys)
                Check(index.Contains("(\"" + key + "\",\"Panels\")", StringComparison.Ordinal), key + ": found by the settings' search");
            var settings = Read("docs/project/SETTINGS.md");
            Check(settings.Contains("| `Chat.HiddenPlayers` |", StringComparison.Ordinal) && settings.Contains("| `Chat.HighlightedPlayers` |", StringComparison.Ordinal), "the players' lists, by their place in the configuration");
            foreach (var key in Keys.Skip(2))
                Check(settings.Contains("| `(" + key + ")` |", StringComparison.Ordinal), key + ": a tab's setting, listed by its label");
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            foreach (var key in Keys)
                Check(!Regex.IsMatch(window, @"ImGui\.(InputText|Checkbox|SmallButton)\(localization\.Get\(""" + Regex.Escape(key) + @""""), key + ": through a shared helper, never by hand again");
        }),
    ];
}
