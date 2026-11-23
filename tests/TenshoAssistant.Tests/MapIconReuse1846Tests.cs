// X-685 (0.1846): the largest allocation of the game's thread in the 120 s after a load was the map canvas's icon list (MapIconDraw[]
// 29 MB, a list made anew and grown each frame): the lists are kept and emptied. And the server bar's number is the memory of Dalamud and
// of all its plugins (Tensho is not counted apart): the bar says so (« Tensho: ok · Dalamud 521 MB »), it no longer reads as Tensho's.
internal static class MapIconReuse1846Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Map icon reuse 1846: wiring: the canvas's icon list and hovered icons are fields emptied each frame, never made anew", () =>
        {
            var view = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(view.Contains("var icons=mapIconList;icons.Clear();", StringComparison.Ordinal), "the icon list kept");
            Check(view.Contains("var hits=mapIconHits;hits.Clear();", StringComparison.Ordinal), "the hovered icons kept");
            Check(!view.Contains("new List<MapIconDraw>()", StringComparison.Ordinal), "no list of icons made per frame");
        }),
        ("Map icon reuse 1846: languages: the server bar names Dalamud beside the number, in English and French", () =>
        {
            var en = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json"));
            var fr = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json"));
            Check(en.Contains("\"Bar\": \"Tensho: {0} · Dalamud {1} MB\"", StringComparison.Ordinal), "en");
            Check(fr.Contains("\"Bar\": \"Tensho : {0} · Dalamud {1} Mo\"", StringComparison.Ordinal), "fr");
        }),
    ];
}
