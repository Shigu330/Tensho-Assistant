// 0.677 (user request): the Maps page in tabs, and what fills a field of another tab brings that tab forward.
internal static class MapTabs677Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Maps page: every section sits in its tab",() =>
        {
            var page=Read("MainWindow.Maps.cs");
            int At(string text){var at=page.IndexOf(text,StringComparison.Ordinal);Check(at>=0,text);return at;}
            var maps=At("###MapsTabMaps");var markers=At("###MapsTabMarkers");var hunt=At("###MapsTabHunt");var plots=At("###MapsTabPlots");
            Check(maps<markers && markers<hunt && hunt<plots,"tab order");
            foreach(var section in new[]{"DrawMapBookmarks(catalog);","DrawChatMapLinks(catalog);","DrawPlaceDirectory();"})Check(At(section)>markers && At(section)<hunt,section);
            foreach(var section in new[]{"DrawHuntTrain(catalog);","DrawSRankConditions(catalog);"})Check(At(section)>hunt && At(section)<plots,section);
            Check(At("DrawPlots();")>plots,"plots");
            Check(At("private void DrawMapsPicker")>At("EndTabBar"),"the maps list is its own method");
        }),
        ("Maps page: a click that fills the coordinates brings the Maps tab forward",() =>
        {
            Check(Read("MainWindow.MapBookmarks.cs").Contains("selectedBookmark = point.Id; selectedMapId = point.MapId; mapsTabRequest = 0;"),"a chosen bookmark");
            Check(Read("MainWindow.MapView.cs").Contains("selectedMapId=map.MapId;mapsTabRequest=0;"),"the map's context menu");
            Check(Read("MainWindow.Monsters.cs").Contains("selectedMapId=chosen.MapId; mapsTabRequest=0;"),"a monster's place");
            Check(Read("MainWindow.Settings.cs").Contains("{page=Page.Maps;mapsTabRequest=1;}"),"settings go to the bookmarks");
        }),
    ];
}
