// 0.912 (X-76 step 2b, harmonisation H2): text that carries a meaning (text, error, warning, success, muted, selected) follows the theme,
// so light themes stay readable; one state mark (check, cross, question mark) drawn from the icon font, as « ✗ » is missing from the game font.
internal static class ThemeColours912Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Source(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static IEnumerable<(string Name,string Text)> Pages()
        =>Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"MainWindow*.cs").Select(path=>(Path.GetFileName(path),File.ReadAllText(path)));
    // Fixed text colours replaced by the theme's: white text, red error, orange warning, green/orange/red levels, grey for unknown rows.
    private static readonly string[] Removed=
    [
        "new Vector4(0.93f,0.93f,0.93f",
        "new Vector4(0.9f,0.45f,0.4f",
        "new Vector4(0.95f,0.55f,0.3f,",
        "new(0.95f,0.55f,0.3f,",
        "new Vector4(0.85f,0.45f,0.3f",
        "new Vector4(0.35f,0.8f,0.55f",
        "new Vector4(0.98f,0.62f,0.20f",
        "new Vector4(0.95f,0.80f,0.30f",
        "new Vector4(0.95f,0.65f,0.30f",
        "new Vector4(0.55f,0.55f,0.55f",
        "MeldWarning",
    ];
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Theme colours: no fixed text colour left in the main window's pages; aliases read the theme each time",() =>
        {
            foreach(var (name,text) in Pages())
                foreach(var literal in Removed)
                    Check(!text.Contains(literal,StringComparison.Ordinal),$"{name}: fixed colour {literal}");
            var window=Source("MainWindow.cs");
            foreach(var alias in new[]{"Text => FfxivTheme.Text;","Error => FfxivTheme.Error;","Warning => FfxivTheme.Warning;","Selected => FfxivTheme.Selected;"})
                Check(window.Contains("private static Vector4 "+alias,StringComparison.Ordinal),"alias "+alias);
        }),
        ("Theme colours: chosen toggle buttons use the theme's selection colour, not the accent",() =>
        {
            Check(Source("MainWindow.CollectablePanel.cs").Contains("if(chosen)ImGui.PushStyleColor(ImGuiCol.Button,Selected);",StringComparison.Ordinal),"collectable risk");
            Check(Source("MainWindow.Items.cs").Contains("if (selected) ImGui.PushStyleColor(ImGuiCol.Button, Selected);",StringComparison.Ordinal),"craft book job");
            foreach(var (name,text) in Pages())
                Check(!text.Contains("PushStyleColor(ImGuiCol.Button,Accent",StringComparison.Ordinal) && !text.Contains("PushStyleColor(ImGuiCol.Button, Accent",StringComparison.Ordinal),$"{name}: accent button");
        }),
        ("State mark: one helper from the icon font, used by the pages; no « ✗ » or « ✓ » drawn as text",() =>
        {
            var window=Source("MainWindow.cs");
            Check(window.Contains("private static void DrawStateMark(bool? state)",StringComparison.Ordinal) && window.Contains("ImGui.PushFont(UiBuilder.IconFont);",StringComparison.Ordinal)
                && window.Contains("FontAwesomeIcon.Check",StringComparison.Ordinal) && window.Contains("FontAwesomeIcon.Times",StringComparison.Ordinal)
                && window.Contains("FontAwesomeIcon.Question",StringComparison.Ordinal),"helper");
            Check(Source("MainWindow.Beastmaster.cs").Contains("DrawStateMark(known);",StringComparison.Ordinal),"beastmaster");
            foreach(var file in new[]{"MainWindow.Guide.cs","MainWindow.FashionWeek.cs","MainWindow.ChocoboBarding.cs","MainWindow.Gear.cs","MainWindow.Items.cs"})
                Check(Source(file).Contains("DrawStateMark(",StringComparison.Ordinal),file);
            foreach(var (name,text) in Pages())
                Check(!text.Contains("\"✗",StringComparison.Ordinal) && !text.Contains(" ✗\"",StringComparison.Ordinal) && !text.Contains("\"✓",StringComparison.Ordinal) && !text.Contains(" ✓\"",StringComparison.Ordinal),$"{name}: glyph drawn as text");
            foreach(var language in new[]{"en","fr","de","ja"})
            {
                var texts=Source($"Localization/Languages/{language}.json");
                Check(!texts.Contains('✓') && !texts.Contains('✗'),$"{language}: glyph in a text");
            }
        }),
    ];
}
