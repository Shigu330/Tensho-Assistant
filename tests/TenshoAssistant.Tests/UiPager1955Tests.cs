using System.Text.RegularExpressions;

// 0.1955 (X-607, user request of 18/11). The page bar of a long list (« < Page n / m > ») goes through Pager (UI/Components/Pager.cs); a
// previous or next page button written by hand (SmallButton("<###…") or (">###…")) is refused from now on in the pages.
internal static class UiPager1955Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly string Root=Path.GetFullPath("src/TenshoAssistant");

    private static IEnumerable<(string Name,string Text)> Pages()
        =>Directory.EnumerateFiles(Root,"*.cs",SearchOption.AllDirectories)
            .Select(path=>Path.GetRelativePath(Root,path).Replace('\\','/'))
            .Where(name=>name!="UI/Components/Pager.cs"&&!name.Split('/').Any(part=>part is "obj" or "bin"))
            .Select(name=>(name,File.ReadAllText(Path.Combine(Root,name))));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("UI pager 1955: no page button written by hand: Pager(id, ref page, pages, pageKey, previousTipKey, nextTipKey)",()=>
        {
            var found=Pages().Where(page=>Regex.IsMatch(page.Text,@"SmallButton\(""[<>]###")).Select(page=>page.Name).ToArray();
            Check(found.Length==0,"page button by hand: "+string.Join(", ",found));
        }),
        ("UI pager 1955: the pager keeps the ids, the greyed ends and the tooltips; the two long lists use it; CONTRIBUTING names it",()=>
        {
            var pager=File.ReadAllText(Path.Combine(Root,"UI/Components/Pager.cs")).Replace("\r\n","\n");
            Check(pager.Contains("ImGui.BeginDisabled(page<=0);")&&pager.Contains("ImGui.BeginDisabled(page>=pages-1);"),"greyed at both ends");
            Check(pager.Contains("\"<###\"+id+\"Previous\"")&&pager.Contains("\">###\"+id+\"Next\""),"the same ids as before");
            Check(pager.Contains("Tip(previousTipKey);")&&pager.Contains("Tip(nextTipKey);"),"the tooltips");
            Check(File.ReadAllText(Path.Combine(Root,"MainWindow.CraftFromStock.cs")).Contains("Pager(\"CraftStock\",ref fromStockPage,known.Pages,\"CraftStock.Page\",\"CraftStock.PreviousTip\",\"CraftStock.NextTip\");"),"Craft from stock");
            Check(File.ReadAllText(Path.Combine(Root,"MainWindow.Lexicon.cs")).Contains("Pager(\"Lexicon\",ref lexiconPage,pages,\"Lexicon.Page\",\"Lexicon.PreviousTip\",\"Lexicon.NextTip\");"),"Lexicon");
            Check(File.ReadAllText(Path.GetFullPath("CONTRIBUTING.md")).Split("`Pager`").Length>=3,"named in both halves of CONTRIBUTING");
        }),
    ];
}
