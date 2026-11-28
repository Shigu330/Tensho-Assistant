using System.Text.RegularExpressions;

// 0.1954 (X-607, user request of 18/11; the user's approval of 28/11 for the gestures that change the pictures). The « + » and « - » of a
// counter go through ToolButton(FontAwesomeIcon.Plus or Minus, tip, enabled): Tensho's square icon button, glyph centred, its tooltip
// with it. A « + » or « - » button written as text by hand (ImGui.Button or ImGui.SmallButton) is refused from now on in the pages.
internal static class UiCounterButtons1954Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly string Root=Path.GetFullPath("src/TenshoAssistant");

    private static IEnumerable<(string Name,string Text)> Pages()
        =>Directory.EnumerateFiles(Root,"*.cs",SearchOption.AllDirectories)
            .Where(path=>!path.Split(Path.DirectorySeparatorChar).Any(part=>part is "obj" or "bin"))
            .Select(path=>(Path.GetRelativePath(Root,path).Replace('\\','/'),File.ReadAllText(path)));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("UI counter buttons 1954: no « + » or « - » button written as text: ToolButton(FontAwesomeIcon.Plus or Minus, tip, enabled)",()=>
        {
            var hand=new Regex(@"ImGui\.(?:Small)?Button\(""(?:\+|-|−)(?:##[^""]*)?""");
            var found=Pages().Where(page=>hand.IsMatch(page.Text)).Select(page=>page.Name).ToArray();
            Check(found.Length==0,"« + » or « - » as text by hand: "+string.Join(", ",found));
        }),
        ("UI counter buttons 1954: the counters of My tools and Personal tasks use the icon buttons, with their tooltips in both languages",()=>
        {
            var tools=File.ReadAllText(Path.Combine(Root,"MainWindow.MyTools.cs"));
            Check(tools.Contains("ToolButton(Dalamud.Interface.FontAwesomeIcon.Minus,\"MyToolsUi.CountDownTip\",tool is not null)",StringComparison.Ordinal)
                &&tools.Contains("ToolButton(Dalamud.Interface.FontAwesomeIcon.Plus,\"MyToolsUi.CountUpTip\",tool is not null)",StringComparison.Ordinal),"My tools' counter, greyed in a preview");
            var tasks=File.ReadAllText(Path.Combine(Root,"MainWindow.PersonalTasks.cs"));
            Check(Regex.Matches(tasks,@"ToolButton\(Dalamud\.Interface\.FontAwesomeIcon\.Plus,""PersonalTasks\.CountUpTip"",true\)").Count==2
                &&tasks.Contains("ToolButton(Dalamud.Interface.FontAwesomeIcon.Minus,\"PersonalTasks.CountDownTip\",true)",StringComparison.Ordinal),"Personal tasks' counters");
            foreach(var language in new[]{"en","fr"})
            {
                var text=File.ReadAllText(Path.Combine(Root,"Localization/Languages",language+".json"));
                Check(Regex.Matches(text,@"""CountDownTip"": ").Count>=2&&Regex.Matches(text,@"""CountUpTip"": ").Count>=2,"the tooltips in "+language);
            }
        }),
    ];
}
