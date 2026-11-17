using System.Text.RegularExpressions;
using TenshoAssistant.Desktop;

// 0.1536 (X-520, review of 0.1497 I2): every tab of the Settings page has its reference picture (General in « Settings », the others in their
// first category, Debug in developer mode), and the chat's tab settings (X-560) are pictured wide and narrow; the application opens them
// through the typed contract.
internal static class SettingsPictures1536Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Settings pictures 1536: every tab the Settings page draws has its picture, in a category of that tab", () =>
        {
            var tabs = Regex.Matches(Read("src/TenshoAssistant/MainWindow.Settings.cs"), @"SettingsTab\(""(\w+)""").Select(match => match.Groups[1].Value).Distinct().ToArray();
            Check(tabs.Length == 7 && tabs[0] == "General", "seven tabs, General first: " + string.Join(",", tabs));
            var categories = Regex.Matches(Read("src/TenshoAssistant/MainWindow.SettingsCategories.cs"), @"new\(""(\w+)"",""(\w+)"",").Select(match => (Tab: match.Groups[1].Value, Id: match.Groups[2].Value)).ToArray();
            var plan = ReferencePlan.Plan(DesktopPages.AsIs.Concat(DesktopPages.NeedsLink));
            Check(plan.Any(shot => shot.Id == "Settings" && shot.Setup.Length == 0) && plan.Any(shot => shot.Id == "Settings-small"), "General: the page's own pictures");
            foreach (var tab in tabs.Skip(1))
            {
                var shot = plan.SingleOrDefault(shot => shot.Id == "Settings-" + tab);
                Check(shot is not null && shot.Page == "Settings", "pictured: " + tab);
                var setup = ReferencePlan.SettingsSetup(shot!.Setup);
                Check(setup is { } s && s.Tab == tab && categories.Contains((tab, s.Category)) && categories.First(c => c.Tab == tab).Id == s.Category, $"{tab}: its first category ({shot.Setup})");
                Check(setup!.Value.Developer == (tab == "Debug"), tab + ": developer mode only for Debug");
            }
            var chat = plan.Where(shot => ReferencePlan.SettingsSetup(shot.Setup) is { Category: "ChatTabs" }).ToArray();
            Check(chat.Length == 2 && chat.Any(shot => shot.Width < 800) && chat.Any(shot => shot.Width == 1280), "the chat's tab settings, wide and narrow");
            Check(plan.Select(shot => shot.Id).Distinct().Count() == plan.Count, "one name per picture");
            var last = plan.Select((shot, index) => (shot, index)).Where(row => row.shot.Setup.StartsWith(ReferencePlan.SettingsPrefix, StringComparison.Ordinal)).Select(row => row.index).ToArray();
            Check(last.Length == ReferencePlan.SettingsShots.Length && last[0] == plan.Count - last.Length, "taken last: the tab and category they open stay chosen");
        }),
        ("Settings pictures 1536: the setup is read back; anything else is not a settings picture", () =>
        {
            Check(ReferencePlan.SettingsSetup("settings:Map:MapGame") is ("Map", "MapGame", false), "a tab and its category");
            Check(ReferencePlan.SettingsSetup("settings:Debug:DebugTools") is ("Debug", "DebugTools", true), "Debug in developer mode");
            Check(ReferencePlan.SettingsSetup("") is null && ReferencePlan.SettingsSetup("garden-icons") is null && ReferencePlan.SettingsSetup("settings:") is null && ReferencePlan.SettingsSetup("settings::X") is null, "other setups");
        }),
        ("Settings pictures 1536: the application opens the tab through the typed contract, and gives the window back", () =>
        {
            var calls = new List<string>();
            var map = new Dictionary<string, Delegate> { [nameof(DesktopSurface.OpenSettings)] = (Action<string, string, bool>)((tab, category, developer) => calls.Add($"{tab} {category} {developer}")) };
            var surface = DesktopSurface.FromMap(map, out var missing);
            surface.OpenSettings("Map", "MapGame", false);
            Check(calls.SequenceEqual(["Map MapGame False"]) && !missing.Contains(nameof(DesktopSurface.OpenSettings)), "typed, called through");
            var references = Read("src/TenshoAssistant.Desktop/References.cs");
            Check(references.Contains("if(ReferencePlan.SettingsSetup(shot.Setup) is { } settings)host.Surface.OpenSettings(settings.Tab,settings.Category,settings.Developer);")
                  && references.Contains("if(ReferencePlan.SettingsSetup(shot.Setup) is not null)host.Surface.OpenSettings(\"General\",\"\",false);"), "opened before the picture, given back after");
            var window = Read("src/TenshoAssistant/MainWindow.DesktopData.cs");
            Check(window.Contains("OpenSettings=DesktopOpenSettings,") && window.Contains("if(category.Length>0)OpenSettingsCategory(tab,category);else requestedSettingsTab=tab;"), "the window opens the tab and the category");
        }),
    ];
}
