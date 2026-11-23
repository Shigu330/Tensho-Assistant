using System.Text.RegularExpressions;
using TenshoAssistant.Help;

// X-853 (review 0.1858, I1): X-850 said one jump to a tab was left outside the table of links (the collections' kind); four more were, through
// fields the earlier guards did not know: mapsTabRequest (Settings › Bookmarks, the map's « Use in tool », a monster's place, a chosen bookmark;
// dropped after the bar was drawn once), freeCompanyTabRequest (« Plan routes », a route's « Build ») and requestedSettingsTab = "Map" in plain
// text (the map's cog). They are rows of Help/NavTargets.cs now. Here: no field of that kind but the two of MainWindow.Open, set only where a
// link is followed; no tab chosen by a request number in a bar's list; every new row is followed by OpenLink where the old jump was.
internal static class NavTargets1859Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    private static IEnumerable<(string File, string Code)> Sources() =>
        Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n")));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Nav targets 1859: no tab request of a page's own (…TabRequest, requested…Tab) but MainWindow.Open's, set only where a link is followed", () =>
        {
            var names = new Regex(@"\b(?:\w*TabRequest|requested\w*Tab)\b");
            var set = new Regex(@"\b(\w*TabRequest|requested\w*Tab)\s*=(?!=)\s*([^;]*);");
            // Where each of the two may be set, and to what: Open (Navigation), OpenSettingsCategory (SettingsCategories), a Settings tab once open.
            var allowed = new HashSet<string>
            {
                "MainWindow.Navigation.cs|navTabRequest=null", "MainWindow.ItemContextMenu.cs|navTabRequest=null", "MainWindow.Navigation.cs|navTabRequest=(bar,key)",
                "MainWindow.Navigation.cs|requestedSettingsTab=tab", "MainWindow.SettingsCategories.cs|requestedSettingsTab=tab",
                "MainWindow.Settings.cs|requestedSettingsTab=null",
            };
            var seen = new HashSet<string>();
            foreach (var (file, code) in Sources())
            {
                foreach (Match name in names.Matches(code))
                    Check(name.Value is "navTabRequest" or "requestedSettingsTab", file + ": a tab asked outside the table of links: " + name.Value);
                foreach (Match assignment in set.Matches(code))
                {
                    var key = file + "|" + assignment.Groups[1].Value + "=" + assignment.Groups[2].Value.Trim();
                    Check(allowed.Contains(key), "a tab asked outside MainWindow.Open: " + key);
                    seen.Add(key);
                }
            }
            Check(seen.Count == allowed.Count, "the guard reads the code: " + string.Join(", ", allowed.Except(seen)));
            var open = Read("src/TenshoAssistant/MainWindow.Navigation.cs");
            Check(open.Contains("if(target.Category is { } category)OpenSettingsCategory(tab,category,target.SettingKey);\n                else requestedSettingsTab=tab;", StringComparison.Ordinal), "a Settings tab named alone opens on the category chosen last");
        }),
        ("Nav targets 1859: no bar chooses a tab by a request number; the Maps page's and the Free Company page's tabs are rows of the table", () =>
        {
            var number = new Regex(@",\s*request\s*==\s*\d+\s*\)");
            foreach (var (file, code) in Sources())
                Check(!number.IsMatch(code), file + ": a tab chosen by a request number: " + number.Match(code).Value);
            Check(NavTargets.Bars["Maps"] == "MapsTabs" && NavTargets.Bars["FreeCompany"] == "FreeCompanyTabs", "the two bars");
            Check(NavTargets.Of("Maps.Maps") == new NavTarget("Maps", "Maps") && NavTargets.Of("Maps.Markers") == new NavTarget("Maps", "Markers"), "the Maps page's rows");
            Check(NavTargets.Of("FreeCompany.Routes") == new NavTarget("FreeCompany", "Routes") && NavTargets.Of("FreeCompany.Builds") == new NavTarget("FreeCompany", "Builds"), "the Free Company page's rows");
            Check(NavTargets.Of("MapView.Settings") == new NavTarget("Settings", "Map"), "the map's cog: the Map tab, on the category chosen last");
        }),
        ("Nav targets 1859: each jump the review found follows its row through OpenLink", () =>
        {
            var sites = new (string File, string Code)[]
            {
                ("MainWindow.Settings.cs", "OpenLink(\"Maps.Markers\");"),
                ("MainWindow.MapView.cs", "bookmarkSaved=false;OpenLink(\"Maps.Maps\");IsOpen=true;"),
                ("MainWindow.MapView.cs", "{OpenLink(\"MapView.Settings\");IsOpen=true;BringToFront();}"),
                ("MainWindow.Monsters.cs", "mapOpenError=null; OpenLink(\"Maps.Maps\");"),
                ("MainWindow.MapBookmarks.cs", "selectedMapId = point.MapId; OpenLink(\"Maps.Maps\");"),
                ("MainWindow.AirshipSectors.cs", "{plannerKind=VesselKind.Airship;OpenLink(\"FreeCompany.Routes\");}"),
                ("MainWindow.RoutePlanner.cs", "buildSearchKey=null;OpenLink(\"FreeCompany.Builds\");"),
            };
            foreach (var (file, code) in sites)
                Check(Read("src/TenshoAssistant/" + file).Contains(code, StringComparison.Ordinal), file + ": " + code);
            var desktop = Read("src/TenshoAssistant/MainWindow.DesktopData.cs");
            Check(desktop.Contains("Open(new TenshoAssistant.Help.NavTarget(\"Settings\",tab,category.Length>0 ? category:null));", StringComparison.Ordinal), "the desktop pictures open Settings through Open");
            var table = Read("src/TenshoAssistant/Help/NavTargets.cs");
            Check(table.Contains("X-853 (review 0.1858, I1): X-850 said one jump was left outside; four more were", StringComparison.Ordinal), "the table's comment says what was left");
        }),
    ];
}
