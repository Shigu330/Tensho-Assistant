using System.Text.RegularExpressions;
using TenshoAssistant.Guide;

// System plan 4.13 and 4.14 (ratchet): every page is a stop of the guide's visit or is left out of it with a reason, and every page is in the
// Home tool directory or is left out of it with a reason. A new page that is in neither list does not pass. Help in English and French for
// every page: HelpTests.
internal static class PageCoverageTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path));
    private static string[] Pages()=>Regex.Match(Read("src/TenshoAssistant/MainWindow.cs"),@"enum Page \{([^}]*)\}").Groups[1].Value
        .Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Pages: each one is a stop of the guide's visit or is left out of it with its reason, never both",() =>
        {
            var pages=Pages();
            Check(pages.Length>=50 && pages.Contains("Help"),"the list of pages is read");
            var stops=GuideFlow.EveryStop.Select(stop=>stop.Page).ToHashSet();
            var leftOut=GuideFlow.LeftOutOfVisit;
            foreach(var page in pages)
            {
                Check(stops.Contains(page) || leftOut.ContainsKey(page),$"page {page}: add it to the guide's visit (GuideFlow.AllStops) or to GuideFlow.LeftOut with its reason");
                Check(!(stops.Contains(page) && leftOut.ContainsKey(page)),$"page {page}: both a stop and left out");
            }
            foreach(var (page,reason) in leftOut)
            {
                Check(pages.Contains(page),$"left out of the visit but not a page: {page}");
                Check(reason.Trim().Length>=10,$"left out of the visit without a reason: {page}");
            }
            foreach(var page in stops)Check(pages.Contains(page),$"a stop that is not a page: {page}");
        }),
        ("Pages: each one is in the Home tool directory, written or produced from the list of pages, or left out with its reason",() =>
        {
            var pages=Pages();
            var tools=Read("src/TenshoAssistant/MainWindow.Tools.cs");
            // 0.1019 (test audit #60): each page once in MainWindow.Pages.cs is checked by Pages470Tests (same list, set equality with the enum).
            Check(tools.Contains("PlannedTools.Concat(PageTools())") && tools.Contains("if (!opened.Contains(entry.Page) && !DirectoryLeftOut.ContainsKey(entry.Page))")
                && tools.Contains("foreach (var entry in PageEntries)"),"the directory is completed from the list of pages");
            var opened=Regex.Matches(tools,@"FontAwesomeIcon\.\w+, Page\.(\w+)\),").Select(match=>match.Groups[1].Value).ToHashSet();
            Check(opened.Count>=20,"the written rows say which page they open");
            var leftOut=Regex.Matches(tools,@"\[Page\.(\w+)\] = ""([^""]*)""").ToDictionary(match=>match.Groups[1].Value,match=>match.Groups[2].Value);
            Check(leftOut.ContainsKey("Home") && leftOut.ContainsKey("Settings"),"the pages left out are read");
            foreach(var (page,reason) in leftOut)
            {
                Check(pages.Contains(page),$"left out of the directory but not a page: {page}");
                Check(reason.Trim().Length>=10,$"left out of the directory without a reason: {page}");
                Check(!opened.Contains(page),$"left out of the directory but a written row opens it: {page}");
            }
            foreach(var page in opened)Check(pages.Contains(page),$"a written row opens a page that does not exist: {page}");
            Check(tools.Contains("ToolText(string key)=>localization.Get(key.StartsWith(\"PageHelp.\",StringComparison.Ordinal) ? key : \"ToolDirectory.\"+key)"),
                "a produced row reads its page's help (in every language)");
            Check(!tools.Contains("OpenDirectoryTool(tool.Reference)") && tools.Contains("page = tool.Page;"),"« Open » goes to the row's page");
        }),
    ];
}
