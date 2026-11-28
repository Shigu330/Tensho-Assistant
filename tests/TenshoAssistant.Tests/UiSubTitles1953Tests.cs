using System.Text.RegularExpressions;

// 0.1953 (X-607, user request of 18/11; the user's approval of 28/11 for the gestures that change the pictures). A sub-title of a page goes
// through UiComponents.SubTitle(text): the game's section font, gold, the same everywhere. The form written by hand (a gold text alone on
// its line, or the section font pushed by hand around a gold text) is refused from now on; four gold lines stay, notes and not titles.
internal static class UiSubTitles1953Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly string Root=Path.GetFullPath("src/TenshoAssistant");

    private static IEnumerable<(string Name,string Text)> Pages()
        =>Directory.EnumerateFiles(Root,"MainWindow*.cs",SearchOption.TopDirectoryOnly)
            .Select(path=>(Path.GetFileName(path),File.ReadAllText(path)));

    // Gold notes, not titles: a line under an action, a remark of the source, the guide's offer, the label of the form's preview.
    private static readonly string[] Notes=["CraftProjectionUi.FollowBlocked","DutyUi.Unparsed","Guide.Offer","Help.FormPreview"];

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("UI sub-titles 1953: no sub-title written by hand as a gold text alone on its line: UiComponents.SubTitle(text)",()=>
        {
            var hand=new Regex(@"^[ \t]*ImGui\.TextColored\(Accent, ?localization\.Get\(""([^""]+)""\)\);[ \t]*\r?$",RegexOptions.Multiline);
            var found=Pages().SelectMany(page=>hand.Matches(page.Text).Where(match=>!Notes.Contains(match.Groups[1].Value)).Select(match=>page.Name+" "+match.Groups[1].Value)).ToArray();
            Check(found.Length==0,"written by hand: "+string.Join(", ",found.Take(8)));
        }),
        ("UI sub-titles 1953: no section font pushed by hand around a gold text",()=>
        {
            var found=Pages().Where(page=>Regex.IsMatch(page.Text,@"PushSectionFont\(\)\)\s*ImGui\.TextColored\(Accent,")).Select(page=>page.Name).ToArray();
            Check(found.Length==0,"PushSectionFont())ImGui.TextColored(Accent,…) by hand: UiComponents.SubTitle(text): "+string.Join(", ",found));
        }),
        ("UI sub-titles 1953: SubTitle stays the section font in gold, and the pages use it",()=>
        {
            var component=File.ReadAllText(Path.Combine(Root,"UI/UiComponents.cs"));
            Check(component.Contains("using (PushSectionFont()) ImGui.TextColored(FfxivTheme.Gold,text);",StringComparison.Ordinal),"section font, gold");
            var calls=Pages().Sum(page=>Regex.Matches(page.Text,@"UiComponents\.SubTitle\(").Count);
            Check(calls>=60,"the sub-titles of the pages: "+calls);
            var contributing=File.ReadAllText(Path.GetFullPath("CONTRIBUTING.md"));
            Check(contributing.Split("UiComponents.SubTitle(").Length>=3,"named in both halves of CONTRIBUTING");
        }),
    ];
}
