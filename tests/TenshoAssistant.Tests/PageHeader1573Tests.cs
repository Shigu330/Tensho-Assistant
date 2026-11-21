using System.Text.RegularExpressions;
using TenshoAssistant.UI;

// 0.1573 (X-600, user request of 18/11): one header for a page with sub-pages (title, description, tab bar), drawn from the page list as
// the Storage page draws its own; the row of small buttons written by hand (crafting, gathering, collections) is gone and must not come back.
internal static class PageHeader1573Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private static IEnumerable<string> Sources() => Directory.EnumerateFiles(Path.GetFullPath("src/TenshoAssistant"), "*.cs", SearchOption.AllDirectories)
        .Where(file => !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && !file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar));

    private enum Tab { Simulation, Presets, Live }

    /// <summary>The page list as the test reads it: page, has a navigation label, parent, draw method, and whether the line gives a tab and a description.</summary>
    private static List<(string Page, string? Parent, string Draw, bool Tab, bool About)> Entries()
    {
        var rows = new List<(string, string?, string, bool, bool)>();
        foreach (Match row in Regex.Matches(Read("src/TenshoAssistant/MainWindow.Pages.cs"), @"^\s*new\(Page\.(\w+),.*$", RegexOptions.Multiline))
        {
            var line = row.Value;
            var parent = Regex.Match(line, @""",Page\.(\w+)[,)]");
            var draw = Regex.Match(line, @",(?:\(\)=>)?(Draw\w+)");
            rows.Add((row.Groups[1].Value, parent.Success ? parent.Groups[1].Value : null, draw.Groups[1].Value, line.Contains(",Tab:"), line.Contains(",About:")));
        }
        return rows;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Page header 1573: a new bar selects the page's tab, and what the bar reports during that frame is not a click", () =>
        {
            var sync = new PageTabSync<Tab>();
            Check(sync.Pending(Tab.Live) && sync.Force(Tab.Live, Tab.Live) && !sync.Force(Tab.Live, Tab.Simulation), "the bar is new: the tab of the page is forced, and no other");
            Check(sync.Chosen(Tab.Live, Tab.Simulation) is null, "ImGui opens its first tab on a new bar: the page does not follow it");
            sync.Shown(Tab.Live);
            Check(!sync.Pending(Tab.Live) && !sync.Force(Tab.Live, Tab.Live), "nothing is forced once the bar shows the page (a forced tab every frame would refuse every click)");
            Check(sync.Chosen(Tab.Live, Tab.Live) is null, "the open tab is the page: nothing changes");
        }),
        ("Page header 1573: a click on another tab changes the page; a page changed from elsewhere is not sent back by the former tab", () =>
        {
            var sync = new PageTabSync<Tab>();
            sync.Shown(Tab.Simulation);
            Check(sync.Chosen(Tab.Simulation, Tab.Presets) == Tab.Presets, "the bar reports another tab as open: the player clicked it");
            sync.Shown(Tab.Presets);
            // The side bar, a link or a command opens the current synthesis: for one frame ImGui still reports « Presets » as the open tab.
            Check(sync.Force(Tab.Live, Tab.Live) && sync.Chosen(Tab.Live, Tab.Presets) is null, "the former tab still open during the frame of the change is not a click");
            sync.Shown(Tab.Live);
            Check(sync.Chosen(Tab.Live, Tab.Live) is null && !sync.Pending(Tab.Live), "the next frame is quiet");
            sync.Reset();
            Check(sync.Pending(Tab.Live), "after a page without tabs, the bar starts again from the page");
        }),
        ("Page header 1573 (ratchet): a family of pages gives its tabs and descriptions in the page list, and its pages draw no heading of their own", () =>
        {
            var entries = Entries();
            Check(entries.Count >= 50, "page list read: " + entries.Count);
            var parents = entries.Where(entry => entry.Parent is not null).Select(entry => entry.Parent!).Distinct().ToArray();
            Check(parents.Length >= 3 && parents.Contains("CraftSimulation") && parents.Contains("Gathering") && parents.Contains("Collections"), "families: " + string.Join(",", parents));
            var family = entries.Where(entry => entry.Parent is not null || parents.Contains(entry.Page)).ToArray();
            foreach (var entry in family) Check(entry.Tab && entry.About, "a page of a family names its tab and its description in the list (Tab:, About:): " + entry.Page);
            foreach (var entry in entries.Except(family)) Check(!entry.Tab, "a tab without a family: " + entry.Page);
            var code = string.Join("\n", Sources().Where(file => Path.GetFileName(file).StartsWith("MainWindow.", StringComparison.Ordinal)).Select(file => File.ReadAllText(file).Replace("\r\n", "\n")));
            foreach (var draw in family.Select(entry => entry.Draw).Distinct())
            {
                var start = code.IndexOf("    private void " + draw + "(", StringComparison.Ordinal);
                Check(start >= 0, "draw method found: " + draw);
                var body = code[start..code.IndexOf("\n    }\n", start, StringComparison.Ordinal)];
                Check(!body.Contains("Heading(", StringComparison.Ordinal), "the header of a family is drawn by DrawPageHeader, not by the page: " + draw);
            }
        }),
        ("Page header 1573 (ratchet): one component draws the tabs of the sub-pages; no row of sub-page buttons written by hand", () =>
        {
            var header = Read("src/TenshoAssistant/UI/Navigation/MainWindow.PageHeader.cs");
            Check(header.Contains("foreach(var entry in PageEntries)") && header.Contains("Heading(title(),shown.About?.Invoke() ?? \"\");") && header.Contains("ImGui.BeginTabBar(\"PageTabs\"+root+\"###PageTabs\"+root+\"/\"+drag.Generation,drag.Settle(arranged,entry=>entry.Page.ToString()) ? ImGuiTabBarFlags.None:ImGuiTabBarFlags.Reorderable)")
                && header.Contains("pageTabs.Force(current,entry.Page)") && header.Contains("pageTabs.Chosen(current,entry.Page)") && header.Contains("pageTabs.Shown(page);"), "the header: title, description and tabs from the page list");
            var window = Read("src/TenshoAssistant/MainWindow.cs");
            Check(window.IndexOf("DrawPageHeader();", StringComparison.Ordinal) is var at and > 0 && at < window.IndexOf("PageEntryFor(page)?.Draw();", StringComparison.Ordinal), "drawn above every page");
            var entries = Entries();
            string Root(string page) => entries.FirstOrDefault(entry => entry.Page == page).Parent ?? page;
            var roots = entries.Where(entry => entry.Parent is not null).Select(entry => entry.Parent!).ToHashSet();
            var link = new Regex(@"Button\(.*?page ?= ?Page\.(\w+)");
            var rows = new List<string>();
            foreach (var file in Sources())
            {
                var text = File.ReadAllText(file);
                var name = Path.GetFileName(file);
                foreach (var gone in new[] { "CategoryItem(", "CategoryLink(", "DrawCategoryNavigation" }) Check(!text.Contains(gone, StringComparison.Ordinal), gone + " is back in " + name);
                Check(name == "MainWindow.PageHeader.cs" || !text.Contains("###PageTab", StringComparison.Ordinal), "a second tab bar of pages in " + name);
                // A row written by hand: buttons to two pages of the same family, on the same line or within three lines.
                var lines = text.Replace("\r\n", "\n").Split('\n');
                var seen = new List<(int Line, string Page)>();
                for (var index = 0; index < lines.Length; index++)
                    foreach (Match match in link.Matches(lines[index]))
                    {
                        var target = match.Groups[1].Value;
                        if (!roots.Contains(Root(target))) continue;
                        if (seen.Any(former => index - former.Line <= 3 && former.Page != target && Root(former.Page) == Root(target))) rows.Add(name + ":" + (index + 1));
                        seen.Add((index, target));
                    }
            }
            // One known pair, not a page header: the two links of the crafting tool's card in the tool list (a popup).
            Check(rows.SequenceEqual(["MainWindow.Tools.cs:269"]) || rows.Count == 1 && rows[0].StartsWith("MainWindow.Tools.cs:", StringComparison.Ordinal),
                "buttons to sub-pages of one family side by side (they are tabs of the page header): " + string.Join(", ", rows));
        }),
    ];
}
