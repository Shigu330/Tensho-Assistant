using TenshoAssistant.Desktop;

// 0.1158 (X-119 step 4, plan 4): a choice per page of where it is listed in the navigation: in the game and the desktop app, or one of
// them. Home and Settings stay on both sides (a choice can always be undone), the pages that stay in the game are never listed in the app
// nor offered; a saved value that is not one of the names (a number, an unknown word, a hand-edited file) reads as « both ».
internal static class PagePlaces1158Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Page places 1158: the rules of where a page is listed",() =>
        {
            var places=new Dictionary<string,string>{["Items"]="App",["Market"]="game",["Lexicon"]="2",["Gear"]="Everywhere",["Home"]="App",["Settings"]="Game",["Maps"]="App",["Quests"]=" app "};
            Check(DesktopPages.Place(places,"Items")==PagePlace.App && !DesktopPages.InGameNavigation(places,"Items") && DesktopPages.InAppNavigation(places,"Items"),"app only");
            Check(DesktopPages.Place(places,"Market")==PagePlace.Game && DesktopPages.InGameNavigation(places,"Market") && !DesktopPages.InAppNavigation(places,"Market"),"game only, any case");
            Check(DesktopPages.Place(places,"Quests")==PagePlace.App,"spaces around a name");
            Check(DesktopPages.Place(places,"Lexicon")==PagePlace.Both && DesktopPages.Place(places,"Gear")==PagePlace.Both,"a number or an unknown word is not a choice");
            Check(DesktopPages.InGameNavigation(places,"Home") && DesktopPages.InAppNavigation(places,"Home")
                && DesktopPages.InGameNavigation(places,"Settings") && DesktopPages.InAppNavigation(places,"Settings"),"Home and Settings on both sides whatever is saved");
            Check(DesktopPages.InGameNavigation(places,"Maps") && !DesktopPages.InAppNavigation(places,"Maps") && !DesktopPages.Choosable("Maps"),"a page that stays in the game: in the game, never in the app, not offered");
            Check(DesktopPages.Place(null,"Items")==PagePlace.Both && DesktopPages.Place(new Dictionary<string,string>{["Items"]=null!},"Items")==PagePlace.Both,"no choice: both");
            Check(!DesktopPages.Choosable("NotAPage"),"an unknown page is not offered");
            // Every page is listed somewhere, whatever the choices.
            var all=DesktopPages.InGame.Concat(DesktopPages.NeedsLink).Concat(DesktopPages.AsIs).ToArray();
            foreach(var choice in new[]{"Game","App","Both"})
            {
                var every=all.ToDictionary(page=>page,_=>choice);
                Check(all.All(page=>DesktopPages.InGameNavigation(every,page) || DesktopPages.InAppNavigation(every,page)),"listed somewhere with all on "+choice);
                Check(DesktopPages.InGameNavigation(every,"Settings") && DesktopPages.InAppNavigation(every,"Settings"),"Settings reachable on both sides with all on "+choice);
            }
        }),
        ("Page places 1158: saved with the settings, offered in Settings › Advanced, followed by both navigations",() =>
        {
            var configuration=Read("src/TenshoAssistant/Configuration.cs");
            Check(configuration.Contains("public Dictionary<string,string> PagePlaces { get; set; } = [];") && configuration.Contains("\"DesktopInstallFolder\", \"PagePlaces\", "),"a setting, reset with the others");
            Check(Read("src/TenshoAssistant/MainWindow.cs").Contains("if(entry.Visible?.Invoke()!=false && ListedHere(entry.Page))Navigation(entry.Label!(),entry.Page);"),"the navigation follows it");
            var advanced=Read("src/TenshoAssistant/MainWindow.Advanced.cs");
            Check(advanced.Contains("if(!Desktop.DesktopPages.Choosable(name))continue;") && advanced.Contains("ComboSetting(\"Advanced.PagePlace\",\"Advanced.PagePlaceTip\""),"one choice per page, by the shared component");
            Check(advanced.Contains("if(value==Desktop.PagePlace.Both)configuration.PagePlaces.Remove(name);"),"« both » removes the entry (no entry: both)");
            Check(Read("src/TenshoAssistant.Desktop/Host.cs").Contains("Surface.DesktopMode();") && Read("src/TenshoAssistant/MainWindow.DesktopData.cs").Contains("DesktopMode=DesktopMode,"),"the app's window knows it is the app");
            Check(DesktopContract.Members.Any(member=>member.Name=="DesktopSurfaceMap"),"in the contract (typed since 0.1238)");
            Check(Read("src/TenshoAssistant/TenshoAssistant.csproj").Contains("..\\TenshoAssistant.Desktop\\DesktopPages.cs"),"one list shared by the plugin and the app");
        }),
    ];
}
