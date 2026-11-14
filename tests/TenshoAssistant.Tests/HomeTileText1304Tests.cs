using TenshoAssistant.UI;

// 0.1304 (X-286, review of block 0.1278 M5, the Home picture seen on 13/11): a home tile's name or line wider than the tile ended in the
// middle of a word, cut by the clip (« simulateur, carnet, r », « plan d'équipeme »); it now ends with « … » and reads whole in the
// tile's tooltip, at every width.
internal static class HomeTileText1304Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");
    // Seven pixels a character: a measure like a font's, growing with the length.
    private static float Measure(string text) => text.Length * 7f;

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Home tile text 1304: a text that fits is left whole; one that does not ends with « … » and fits, at every width", () =>
        {
            const string line = "Simulateur, carnet, recettes et listes de courses";
            Check(TextFit.Fit(line, line.Length * 7f, Measure) == line && !TextFit.Shortened(line, line), "fits: whole");
            for (var width = 7f; width < line.Length * 7f; width += 7f)
            {
                var shown = TextFit.Fit(line, width, Measure);
                Check(shown.EndsWith(TextFit.Ellipsis, StringComparison.Ordinal) && Measure(shown) <= width && TextFit.Shortened(line, shown),
                    $"width {width}: « {shown} »");
                Check(line.StartsWith(shown[..^1].TrimEnd(' ', ','), StringComparison.Ordinal), $"width {width}: the start of the text");
            }
            Check(TextFit.Fit(line, 3f, Measure) == TextFit.Ellipsis && TextFit.Fit("", 0f, Measure) == "", "nothing fits; empty");
        }),
        ("Home tile text 1304: no word cut when the last whole word keeps most of the line", () =>
        {
            Check(TextFit.Fit("Simulateur, carnet, recettes", 20 * 7f, Measure) == "Simulateur, carnet…", "cut after « carnet », the comma dropped");
            Check(TextFit.Fit("Créneaux, météo, PR et plus", 18 * 7f, Measure) == "Créneaux, météo…", "after « météo »");
            Check(TextFit.Fit("Abcdefghijklmnopqrstuvwxyz", 10 * 7f, Measure) == "Abcdefghi…", "one long word: cut, with « … »");
            Check(TextFit.Fit("Ab cdefghijklmnopqrstuvwxyz", 12 * 7f, Measure) == "Ab cdefghij…", "the last space too far back: the word is cut");
        }),
        ("Home tile text 1304: the tiles draw the fitted texts and the whole ones in the tooltip", () =>
        {
            var home = Read("src/TenshoAssistant/MainWindow.Home.cs");
            var tiles = home[home.IndexOf("private void DrawHomeTiles()", StringComparison.Ordinal)..];
            Check(tiles.Contains("var shownName=UI.TextFit.Fit(name,room,Measure);var shownLine=UI.TextFit.Fit(line,room,Measure);", StringComparison.Ordinal), "fitted to the room left of the icon");
            Check(tiles.Contains("TipText(name+\"\\n\"+line);", StringComparison.Ordinal), "the whole text in the tooltip");
            Check(tiles.Contains("ImGui.GetColorU32(ImGuiCol.Text),shownName);", StringComparison.Ordinal) && tiles.Contains("ImGui.GetColorU32(ImGuiCol.TextDisabled),shownLine);", StringComparison.Ordinal), "the fitted texts drawn");
        }),
    ];
}
