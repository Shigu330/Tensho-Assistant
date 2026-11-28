using System.Text.RegularExpressions;

// 0.1956 (X-607, user request of 18/11). A rich tooltip (item, action, place, quest, duty, FATE, NPC, recipe, gathering…) opens behind
// one guard, RichTipHovered() (UI/Components/Tooltip.cs): the last drawn item hovered, not when greyed. The hover test written by hand in
// front of a Draw…Tooltip is refused from now on.
internal static class UiRichTips1956Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly string Root=Path.GetFullPath("src/TenshoAssistant");

    private static IEnumerable<(string Name,string Text)> Sources()
        =>Directory.EnumerateFiles(Root,"*.cs",SearchOption.AllDirectories)
            .Select(path=>Path.GetRelativePath(Root,path).Replace('\\','/'))
            .Where(name=>!name.Split('/').Any(part=>part is "obj" or "bin"))
            .Select(name=>(name,File.ReadAllText(Path.Combine(Root,name))));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("UI rich tips 1956: no « if(ImGui.IsItemHovered())DrawXTooltip(…) » written by hand: if(RichTipHovered())DrawXTooltip(…)",()=>
        {
            var hand=new Regex(@"if ?\(ImGui\.IsItemHovered\(\)\) ?Draw[A-Za-z]*Tooltip\(");
            var found=Sources().Where(source=>hand.IsMatch(source.Text)).Select(source=>source.Name).ToArray();
            Check(found.Length==0,"hover test by hand in front of a rich tooltip: "+string.Join(", ",found));
        }),
        ("UI rich tips 1956: the guard keeps the old test (not when greyed), the pages and ItemTip use it, CONTRIBUTING names it",()=>
        {
            var tooltip=File.ReadAllText(Path.Combine(Root,"UI/Components/Tooltip.cs"));
            Check(tooltip.Contains("private static bool RichTipHovered()=>ImGui.IsItemHovered();",StringComparison.Ordinal),"the same test as before");
            Check(tooltip.Contains("if(RichTipHovered())DrawItemInfoTooltip(id,glamourOnly,hq);",StringComparison.Ordinal),"ItemTip");
            var calls=Sources().Sum(source=>Regex.Matches(source.Text,@"if\(RichTipHovered\(\)\)Draw[A-Za-z]*Tooltip\(").Count);
            Check(calls>=30,"the rich tooltips of the pages: "+calls);
            Check(File.ReadAllText(Path.GetFullPath("CONTRIBUTING.md")).Split("RichTipHovered()").Length>=3,"named in both halves of CONTRIBUTING");
        }),
    ];
}
