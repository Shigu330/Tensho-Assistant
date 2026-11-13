using System.Numerics;
using TenshoAssistant.UI;

// 0.1252 (X-154, version 1 of the visual base): the navigation column follows the scale and folds behind a button (automatic under a width,
// or by the setting), a selected row or button carries a mark beside its background and never the accent as text colour, the keyboard and
// gamepad focus frame takes the theme's accent. Ratchets: tables and accent-coloured titles written by hand can only go down.
internal static class VisualBase1252Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static int Count(string text,string part)
    {
        var count=0;
        for(var at=text.IndexOf(part,StringComparison.Ordinal);at>=0;at=text.IndexOf(part,at+part.Length,StringComparison.Ordinal))count++;
        return count;
    }
    private static int CountInSources(string part)
        =>Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"*.cs",SearchOption.AllDirectories)
            .Where(path=>!path.Contains(Path.DirectorySeparatorChar+"obj"+Path.DirectorySeparatorChar) && !path.Contains(Path.DirectorySeparatorChar+"bin"+Path.DirectorySeparatorChar))
            .Sum(path=>Count(File.ReadAllText(path),part));
    private static int Baseline(string name)
    {
        var line=Read("tests/TenshoAssistant.Tests/visual-baseline.txt").Split('\n').FirstOrDefault(l=>l.StartsWith(name+"=",StringComparison.Ordinal))
            ?? throw new Exception("visual-baseline.txt has no line "+name);
        return int.Parse(line[(name.Length+1)..].Trim(),System.Globalization.CultureInfo.InvariantCulture);
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Visual base 1252: the navigation column follows the scale and folds under a width or by the setting",() =>
        {
            Check(NavigationLayout.Width(1)==200 && NavigationLayout.Width(1.5f)==300,"width times the scale");
            Check(NavigationLayout.Width(0)==200 && NavigationLayout.Width(float.NaN)==200,"a scale that is not a positive number counts as 1");
            Check(NavigationLayout.Folded(0,619,1) && !NavigationLayout.Folded(0,620,1),"automatic: folded under 620 at scale 1");
            Check(NavigationLayout.Folded(0,900,1.5f) && !NavigationLayout.Folded(0,930,1.5f),"automatic: the threshold follows the scale (930 at 1.5)");
            Check(!NavigationLayout.Folded(1,100,1),"always shown");
            Check(NavigationLayout.Folded(2,5000,1),"always folded");
            Check(NavigationLayout.Folded(7,100,1) && !NavigationLayout.Folded(-3,2000,1),"a mode out of range counts as automatic");
        }),
        ("Visual base 1252: the selection mark stays inside the element, on its left or bottom edge",() =>
        {
            var min=new Vector2(10,20);var max=new Vector2(210,46);
            foreach(var (scale,width) in new[]{(1f,3f),(0.5f,2f),(1.5f,5f),(2f,6f),(0f,3f)})
            {
                var side=SelectionMark.SideMark(min,max,scale);
                Check(side.Min==min && side.Max.Y==max.Y && side.Max.X-side.Min.X==width,$"left bar, scale {scale}: {width} px wide, the whole height");
                var under=SelectionMark.UnderMark(min,max,scale);
                Check(under.Max==max && under.Min.X==min.X && under.Max.Y-under.Min.Y==width,$"bottom bar, scale {scale}: {width} px high, the whole width");
            }
            var tiny=SelectionMark.SideMark(new(0,0),new(1,1),4);
            Check(tiny.Max.X<=1 && tiny.Max.Y<=1,"never outside a tiny element");
            var flat=SelectionMark.UnderMark(new(0,0),new(1,1),4);
            Check(flat.Min.Y>=0,"never above a tiny element");
        }),
        ("Visual base 1252: every palette keeps the mark and the focus frame visible (3:1, non-text)",() =>
        {
            foreach(var (theme,palette) in ThemePalettes.All)
            {
                Check(ThemePalettes.Contrast(palette.Accent,palette.Panel)>=3,$"{theme}: accent on panel");
                // The light theme is the lowest (3.85): the accent is a mark there, never a text colour on the selection.
                Check(ThemePalettes.Contrast(palette.Accent,palette.Selected)>=3,$"{theme}: accent on selection ({ThemePalettes.Contrast(palette.Accent,palette.Selected):0.00})");
            }
        }),
        ("Visual base 1252: wiring of the shell, the components and the theme",() =>
        {
            var shell=Read("src/TenshoAssistant/MainWindow.cs");
            Check(!shell.Contains("ImGuiTableColumnFlags.WidthFixed, 200)"),"no fixed 200 px navigation column");
            Check(shell.Contains("NavigationLayout.Folded(configuration.NavigationMode, ImGui.GetWindowWidth(), scale)") && shell.Contains("NavigationLayout.Width(scale)"),"the layout decides");
            Check(shell.Contains("ToolButton(FontAwesomeIcon.Bars, \"Navigation.Open\", true)") && shell.Contains("ImGui.OpenPopup(\"NavigationPopup\")"),"the folded navigation opens from a button with its tooltip");
            var popup=shell[shell.IndexOf("private void DrawNavigationPopup(",StringComparison.Ordinal)..shell.IndexOf("private void DrawNavigation()",StringComparison.Ordinal)];
            Check(popup.Contains("DrawNavigation();") && popup.Contains("if (page != before) ImGui.CloseCurrentPopup();"),"the popup draws the same navigation and closes on a page change");
            Check(Count(shell,"DrawNavigation();")==2 && shell.Contains("if (!folded)"),"the navigation is drawn in one place per frame");
            Check(shell.Contains("if(inDesktopApp)ApplyTheme();FfxivTheme.Push();"),"the desktop application applies the chosen theme (the plugin does it once per frame for every window)");
            var components=Read("src/TenshoAssistant/UI/UiComponents.cs");
            var row=components[components.IndexOf("public static bool SelectableRow(",StringComparison.Ordinal)..components.IndexOf("public static bool NavigationItem(",StringComparison.Ordinal)];
            Check(row.Contains("SelectionMark.SideMark(") && !row.Contains("ImGuiCol.Text"),"a selected row: the mark, and the text colour untouched");
            Check(components.Contains("=> SelectableRow(label, selected, 26*Scale);"),"the navigation item is a selectable row, at the scale");
            Check(components.Contains("SelectionMark.UnderMark("),"a selected category button carries the mark too");
            Check(components.Contains("public static void SubTitle(string text)"),"sub-title component");
            var theme=Read("src/TenshoAssistant/UI/FfxivTheme.cs");
            Check(theme.Contains("ImGui.PushStyleColor(ImGuiCol.NavHighlight, c.Accent);"),"the focus frame takes the accent");
            var push=theme[theme.IndexOf("public static void Push()",StringComparison.Ordinal)..theme.IndexOf("public static void Pop()",StringComparison.Ordinal)];
            Check(theme.Contains($"ImGui.PopStyleColor({Count(push,"ImGui.PushStyleColor(")});"),"as many colours popped as pushed");
            Check(theme.Contains($"ImGui.PopStyleVar({Count(push,"ImGui.PushStyleVar(")});"),"as many style values popped as pushed");
        }),
        ("Visual base 1252: the navigation setting is saved, reset and translated",() =>
        {
            var configuration=Read("src/TenshoAssistant/Configuration.cs");
            Check(configuration.Contains("public int NavigationMode { get; set; }") && configuration.Contains("\"ThemeMode\", \"NavigationMode\","),"saved, and in « restore every setting »");
            Check(new TenshoAssistant.Configuration().NavigationMode==NavigationLayout.Auto,"automatic by default");
            Check(Read("src/TenshoAssistant/MainWindow.Settings.cs").Contains("ComboSetting(\"Settings.NavigationMode\",\"Settings.NavigationModeTip\","),"shared setting row (tooltip and reset)");
            Check(Read("src/TenshoAssistant/SettingsIndex.cs").Contains("(\"Settings.NavigationMode\",\"General\")"),"found by the settings search");
            foreach(var file in new[]{"en","fr"})
            {
                var text=Read($"src/TenshoAssistant/Localization/Languages/{file}.json");
                foreach(var key in new[]{"\"NavigationMode\": ","\"NavigationModeTip\": ","\"NavigationModeAuto\": ","\"NavigationModeShown\": ","\"NavigationModeFolded\": ","\"Open\": "})
                    Check(text.Contains("    "+key),$"{file}: {key}");
            }
        }),
        ("Visual base 1252: tables and accent titles written by hand only go down (visual-baseline.txt)",() =>
        {
            foreach(var (name,part) in new[]{("BeginTable","ImGui.BeginTable("),("TextColoredAccent","ImGui.TextColored(Accent")})
            {
                var now=CountInSources(part);var allowed=Baseline(name);
                Check(now<=allowed,$"{part}: {now} written by hand, {allowed} allowed: use the shared component (BeginDataTable, SubTitle, StateLine) instead of adding one");
            }
        }),
    ];
}
