// 0.1222 (X-256, X-257, first-player review of 12/11, G5, G6, G12, G13, D5, D6, D11): what a new player meets first. The side bar's
// secondary groups start folded until the player folds or unfolds one; the guide has a « Panels » step (every panel next to the game's
// windows, all off or on at once), PaissaDB among the sources, and with nothing ticked it visits the pages for everyone only; Home has
// one description; the FAQ starts with « Where do I start? », answers the newcomer's questions (safe, files and erasing, panels, update)
// and ends with the install and the desktop app; « Disconnect » and the webhook say what stays.
using TenshoAssistant.Guide;
using TenshoAssistant.Help;

internal static class FirstScreen1222Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("First screen 1222: secondary groups folded until the player chooses",() =>
        {
            var folded = FirstScreen.Folded(null);
            Check(folded.SetEquals(new[] { "Progression","Content","GoldSaucer","References" }),"the defaults");
            Check(!folded.Contains("Objects") && !folded.Contains("Preferences"),"the main groups stay open");
            var saved = new HashSet<string> { "Objects" };
            Check(ReferenceEquals(FirstScreen.Folded(saved),saved) && ReferenceEquals(FirstScreen.ToSave(saved),saved),"the player's choice wins");
            var first = FirstScreen.ToSave(null);
            Check(first.SetEquals(FirstScreen.FoldedGroups) && !ReferenceEquals(first,FirstScreen.FoldedGroups),"the first click starts from a copy of what was shown");
            first.Remove("Content");
            Check(FirstScreen.FoldedGroups.Contains("Content"),"the defaults never change");
            var navigation = Read("src/TenshoAssistant/UI/Navigation/MainWindow.CategoryNavigation.cs");
            Check(navigation.Contains("FirstScreen.Folded(configuration.NavigationCollapsed).Contains(key)",StringComparison.Ordinal)
                  && navigation.Contains("configuration.NavigationCollapsed=TenshoAssistant.Guide.FirstScreen.ToSave(configuration.NavigationCollapsed)",StringComparison.Ordinal),"side bar wired");
            var pages = Read("src/TenshoAssistant/MainWindow.Pages.cs");
            foreach (var group in FirstScreen.FoldedGroups) Check(pages.Contains($",\"{group}\",",StringComparison.Ordinal),"a real group: " + group);
        }),
        ("First screen 1222: the guide's Panels step, PaissaDB, and the pages for everyone when nothing is ticked",() =>
        {
            var steps = Enum.GetValues<GuideStep>();
            Check(Array.IndexOf(steps,GuideStep.Panels) == Array.IndexOf(steps,GuideStep.Settings) + 1,"Panels right after Settings");
            Check(GuideFlow.Stops(null).All(stop => stop.Interest is null) && GuideFlow.Windows(null).All(task => task.Interest is null),"nothing ticked: for everyone only");
            Check(GuideFlow.Stops(null).Count < GuideFlow.EveryStop.Count,"a shorter visit");
            var guide = Read("src/TenshoAssistant/MainWindow.Guide.cs");
            Check(guide.Contains("case GuideStep.Panels:",StringComparison.Ordinal) && guide.Contains("Guide.PanelsAllOff",StringComparison.Ordinal) && guide.Contains("Guide.PanelsAllOn",StringComparison.Ordinal),"panels step drawn");
            var set = guide[guide.IndexOf("private void SetGuidePanels",StringComparison.Ordinal)..guide.IndexOf("private void DrawGuidePanels",StringComparison.Ordinal)];
            foreach (var panel in new[] { "ShowCharacterPanel","ShowDutyFinderLoot","RetainerSalesPanel","FishingPanel","InventorySortButton","TriadMatchOverlay","TriadChallengePanel","AlarmServerBar","ReminderServerBar","OccultPanel","ShowBeastBoardPanel","CraftLivePanel" })
                Check(set.Contains($"configuration.{panel} = shown",StringComparison.Ordinal),"all at once: " + panel);
            Check(set.Contains("craftLive.SetEnabled(CraftLiveWanted(configuration));",StringComparison.Ordinal) && set.Contains("SaveGatheringChoice(shown ? true : null, shown);",StringComparison.Ordinal),"with the side effects of Settings");
            Check(guide.Contains("BoolSetting(\"Advanced.Paissa\"",StringComparison.Ordinal),"PaissaDB among the sources");
            foreach (var language in new[] { "en","fr" })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                foreach (var key in new[] { "StepPanelsTitle","StepPanelsText","PanelsAllOff","PanelsAllOffTip","PanelsAllOn","PanelsAllOnTip" })
                    Check(text.Contains($"\"{key}\": ",StringComparison.Ordinal),language + ": " + key);
            }
        }),
        ("First screen 1222: Home has one description, and the FAQ speaks to a newcomer",() =>
        {
            Check(Read("src/TenshoAssistant/MainWindow.Home.cs").Contains("HelpBannerShows(Page.Home) ? \"\" : localization.Get(\"Home.Description\")",StringComparison.Ordinal),"one description on Home");
            Check(Read("src/TenshoAssistant/MainWindow.Help.cs").Contains("page == Page.Home ? \"PageHelp.HomeStart\"",StringComparison.Ordinal),"the banner says where to start");
            var keys = Faq.Entries.Select(entry => entry.Key).ToList();
            Check(keys[0] == "WhereStart","first: where to start");
            Check(keys.TakeLast(5).SequenceEqual(new[] { "Install","Desktop","DesktopUpdate","DesktopLink","DesktopInstall" }),"last: the install and the desktop app");
            foreach (var key in new[] { "Safe","Files","Panels","Update" }) Check(keys.Contains(key),"new question " + key);
            var english = Read("src/TenshoAssistant/Localization/Languages/en.json");
            var french = Read("src/TenshoAssistant/Localization/Languages/fr.json");
            Check(english.Contains("%APPDATA%\\\\XIVLauncher\\\\pluginConfigs\\\\TenshoAssistant.json",StringComparison.Ordinal) && english.Contains("%LOCALAPPDATA%\\\\TenshoAssistant\\\\",StringComparison.Ordinal),"where the files are");
            Check(english.Contains("Your gist and your ticket stay on GitHub",StringComparison.Ordinal) && french.Contains("Votre gist et votre ticket restent sur GitHub",StringComparison.Ordinal),"Disconnect says what stays");
            Check(english.Contains("kept in clear in the plugin's configuration file",StringComparison.Ordinal) && french.Contains("gardée en clair dans le fichier de configuration",StringComparison.Ordinal),"the webhook address is in clear");
            var readme = Read("README.md");
            Check(readme.Split("%LOCALAPPDATA%\\TenshoAssistant\\").Length == 3,"README: the desktop app's folder, in both halves");
        }),
    ];
}
