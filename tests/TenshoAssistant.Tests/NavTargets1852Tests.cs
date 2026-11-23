using System.Text.RegularExpressions;
using TenshoAssistant.Help;

// X-813 (review 0.1812, I1): X-809 asked for every link to a tab or a section in one table, with a test that fails on a stale target. The links
// that chose a tab by a flag (focusStocksTab, focusCraftTab, focusCurrencyTab…) and the Settings categories written in the code stayed outside.
// They are rows of Help/NavTargets.cs now, followed by MainWindow.OpenLink; NavTargets1809Tests reads every row against the pages, bars and
// categories as they are; here, no flag or category is left in the code and every key the code passes is in the table.
internal static class NavTargets1852Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    private static IEnumerable<(string File, string Code)> Sources() =>
        Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n")));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Nav targets 1852: no link chooses a tab by a flag or names a Settings category in the code any more", () =>
        {
            var flags = new Regex(@"\bfocus(?:Stocks|Craft|Uses|Gather|Bank|Currency|CurrencyValue|Orchestrion|Achievement|Title)Tab\b");
            var raised = new Regex(@"\bfocus\w*Tab\s*=\s*true");
            var category = new Regex(@"OpenSettingsCategory\(""");
            foreach (var (file, code) in Sources())
            {
                Check(!flags.IsMatch(code), file + ": a tab flag is left: " + flags.Match(code).Value);
                Check(!raised.IsMatch(code), file + ": a tab chosen by a flag: " + raised.Match(code).Value);
                Check(!category.IsMatch(code), file + ": a Settings category written in the code instead of a row of the table");
            }
            var items = Read("src/TenshoAssistant/MainWindow.Items.cs");
            Check(!items.Contains("itemSheetFocus", StringComparison.Ordinal) && !Read("src/TenshoAssistant/MainWindow.ItemSheet.cs").Contains("itemSheetFocus", StringComparison.Ordinal), "the item sheet has no focus of its own");
        }),
        ("Nav targets 1852: every key the code passes to OpenLink, and every tool's « Settings » category, is a row of the table", () =>
        {
            var keys = new List<string>();
            var categories = new List<string>();
            foreach (var (_, code) in Sources())
            {
                keys.AddRange(Regex.Matches(code, @"\bOpenLink\(""([\w.]+)""\)").Select(match => match.Groups[1].Value));
                categories.AddRange(Regex.Matches(code, @"DrawToolSettingsLink\(""\w+"",""(\w+)""\)").Select(match => match.Groups[1].Value));
            }
            Check(keys.Distinct().Count() >= 12, "the code's links are found: " + keys.Distinct().Count());
            foreach (var key in keys.Distinct()) Check(NavTargets.Table.ContainsKey(key), key + " is passed to OpenLink and missing from the table");
            Check(categories.Count >= 8, "the tools' links are found");
            foreach (var name in categories.Distinct()) Check(NavTargets.Of("Panels." + name) == new NavTarget("Settings", "Panels", name), "Panels." + name + " is not a row of the table");
            foreach (var key in new[] { "Item.Stocks", "Item.Craft", "Item.Uses", "Item.Gathering", "Storage.Bank", "Storage.Currencies", "Storage.CurrencyValue", "Collections.Music", "Collections.Titles", "Collections.Achievements", "Dashboard.ServerBar" })
                Check(keys.Contains(key), key + ": a row no link uses");
            var settings = Read("src/TenshoAssistant/MainWindow.Settings.cs");
            Check(settings.Contains("{OpenLink(\"Panels.\"+category);IsOpen=true;}", StringComparison.Ordinal), "the tools' « Settings » follow the table");
            Check(Read("src/TenshoAssistant/MainWindow.ItemSheet.cs").Contains("private void OpenItemSheet()=>OpenLink(\"Item.Overview\");", StringComparison.Ordinal), "the item sheet opens through the table");
        }),
        ("Nav targets 1852: a tab asked by a link is kept until it is open, and dropped when its bar has no such tab", () =>
        {
            var open = Read("src/TenshoAssistant/MainWindow.Navigation.cs");
            Check(open.Contains("private void OpenLink(string key)=>Open(NavTargets.Of(key));", StringComparison.Ordinal), "one way to follow a key");
            Check(open.Contains("private string? TakeNavTab(string bar)=>navTabRequest is { } request && request.Bar==bar ? request.Tab:null;", StringComparison.Ordinal), "asked until reached");
            var bar = Read("src/TenshoAssistant/UI/Components/DetailPage.cs");
            var take = bar.IndexOf("var linked=TakeNavTab(id);", StringComparison.Ordinal);
            var stale = bar.IndexOf("if(linked is not null && !arranged.Any(tab=>tab.Key==linked)){NavTabReached(id);linked=null;}", StringComparison.Ordinal);
            var reached = bar.IndexOf("if(key==linked)NavTabReached(id);", StringComparison.Ordinal);
            Check(take > 0 && stale > take && reached > stale, "the bar ends the request when the tab is open or missing");
            Check(bar.IndexOf("if(!open)continue;", StringComparison.Ordinal) < reached, "ended only once the tab is open");
            Check(NavTargets.Bars["Items"] == "ItemSheet" && NavTargets.Bars["Storage"] == "StorageTabs" && NavTargets.Bars["Collections"] == "CollectionKinds", "the three bars of the links");
        }),
    ];
}
