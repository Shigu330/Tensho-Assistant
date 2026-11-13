using TenshoAssistant.Help;

// 0.1230 (X-220, user's request of 12/11, K-36): one search at the top of the navigation over pages, items, quests and maps. Pure matching tests
// (accents and case ignored, every word, names that start with the first word first, bounds) and the wiring (off the frame, opens only Tensho's
// pages: no teleport, no shortcut).
internal static class QuickSearch1230Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Source(string path)=>File.ReadAllText(Path.GetFullPath(path));

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Quick search 1230: words, accents and case ignored; too short or too long gives nothing",() =>
        {
            Check(QuickSearch.Tokens("é").Length==0 && QuickSearch.Tokens(" ").Length==0,"one letter: nothing");
            Check(QuickSearch.Tokens(new string('a',QuickSearch.MaxLength+1)).Length==0,"too long: nothing");
            var tokens=QuickSearch.Tokens("éther  CLAIR");
            Check(tokens.SequenceEqual(new[]{"ETHER","CLAIR"}),"normalized words: "+string.Join("|",tokens));
            Check(QuickSearch.Matches(QuickSearch.Normalize("Sable éthéréen clair"),tokens),"every word, any order");
            Check(!QuickSearch.Matches(QuickSearch.Normalize("Sable éthéréen"),tokens),"a missing word refuses");
        }),
        ("Quick search 1230: names starting with the first word first, then source order, at most the limit",() =>
        {
            var names=new[]{"Iron ore","Ingot of iron","Iron ingot","Cobalt iron","Silver"};
            var top=QuickSearch.Top(names,name=>name,QuickSearch.Tokens("iron"),3);
            Check(top.SequenceEqual(new[]{"Iron ore","Iron ingot","Ingot of iron"}),"order: "+string.Join("|",top));
            Check(QuickSearch.Top(names,name=>name,QuickSearch.Tokens("zz")).Count==0,"no match");
            Check(QuickSearch.Top(names,name=>name,[]).Count==0,"no word: nothing");
        }),
        ("Quick search 1230: wiring: above the navigation, items, quests and maps off the frame, opens only Tensho's pages",() =>
        {
            var window=Source("src/TenshoAssistant/MainWindow.cs");
            Check(window.Contains("DrawQuickSearch();\n        foreach(var group in PageGroups)".Replace("\n",Environment.NewLine)) || window.Contains("DrawQuickSearch();\r\n        foreach(var group in PageGroups)") || window.Contains("DrawQuickSearch();\n        foreach(var group in PageGroups)"),"search above the page list");
            var search=Source("src/TenshoAssistant/MainWindow.QuickSearch.cs");
            Check(search.Contains("quickTask=Task.Run(()=>") && search.Contains("items.Search(query,QuickSearch.PerKind)"),"off the frame, from the item index");
            Check(search.Contains("case QuickSearchKind.Place:ShowMapOnTenshoMap(hit.Id);") && !search.Contains("Teleport") && !search.Contains("AddCommand"),"opens the Tensho map, never teleports, no command or shortcut");
            foreach(var language in new[]{"en","fr"})
            {
                var text=Source($"src/TenshoAssistant/Localization/Languages/{language}.json");
                Check(text.Contains("  \"QuickSearch\": {") && text.Contains("\"KindPlace\": "),language+": QuickSearch texts");
            }
        }),
    ];
}
