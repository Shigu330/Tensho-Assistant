using System.Text.RegularExpressions;

// 0.1943 (X-607, user request of 18/11: « code as easy as possible to maintain, above all for a human »; survey of relay 226, gesture 2).
// The item's tooltip of the last drawn item goes through ItemTip(id, glamourOnly, hq) (UI/Components/Tooltip.cs): the twenty-eight
// « if(ImGui.IsItemHovered())DrawItemInfoTooltip(…) » written by hand moved to it, with the same guard; the form is refused from now on.
internal static class UiItemTips1943Tests
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
        ("UI item tips 1943: no « if(ImGui.IsItemHovered())DrawItemInfoTooltip(…) » written by hand outside ItemTip: use ItemTip(id, glamourOnly, hq)",()=>
        {
            var hand=new Regex(@"if\s*\(\s*ImGui\.IsItemHovered\(\)\s*\)\s*DrawItemInfoTooltip\(");
            var found=Sources().Where(source=>source.Name!="UI/Components/Tooltip.cs").SelectMany(source=>hand.Matches(source.Text).Select(_=>source.Name)).ToArray();
            Check(found.Length==0,"written by hand: "+string.Join(", ",found));
            var tooltip=File.ReadAllText(Path.Combine(Root,"UI/Components/Tooltip.cs"));
            Check(hand.Matches(tooltip).Count==1,"only ItemTip itself");
        }),
        ("UI item tips 1943: ItemTip keeps the guard and the tooltip of before (plain hover, the same three arguments)",()=>
        {
            var tooltip=File.ReadAllText(Path.Combine(Root,"UI/Components/Tooltip.cs")).Replace("\r\n","\n");
            Check(tooltip.Contains("    private void ItemTip(uint id,bool glamourOnly=false,bool? hq=null)\n    {\n        if(ImGui.IsItemHovered())DrawItemInfoTooltip(id,glamourOnly,hq);\n    }"),"same guard, same call");
            Check(File.ReadAllText(Path.Combine(Root,"MainWindow.MarketHover.cs")).Contains("private void DrawItemInfoTooltip(uint id,bool glamourOnly=false,bool? hq=null,IReadOnlyList<string>? lines=null)"),"the tooltip's order of arguments");
        }),
        ("UI item tips 1943: the pages use it, named arguments kept (glamour only, high quality)",()=>
        {
            var calls=Sources().Sum(source=>Regex.Matches(source.Text,@"\bItemTip\(").Count);
            Check(calls>=29,"twenty-eight sites and the helper: "+calls);
            var all=string.Concat(Sources().Select(source=>source.Text));
            Check(all.Contains("ItemTip(shown.Id,glamourOnly:look is not null,hq:hq);") && all.Contains("ItemTip(listing.ItemId, hq: listing.Hq);"),"named arguments");
        }),
    ];
}
