// Test audit of 2026-11-09, batch 2 (0.1021): MapGatherers895Tests, LockedQuests966Tests, HeightArrows844Tests, MapMenuWidth400Tests, MapTabs677Tests, MapCoordinates905Tests merged into MapViewTests; every case is kept.
using TenshoAssistant;
using TenshoAssistant.Maps;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Numerics;
using TenshoAssistant.Craft;
using TenshoAssistant.Gathering;
using TenshoAssistant.UI;
internal static class MapViewTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static string ReadLf(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");
    private static string Read2(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name)).Replace("\r\n","\n");
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From MapGatherers895Tests: 0.895 (X-80): the map's gathering points split by gatherer (miner, botanist, fisher), under the « Gathering points » switch.
        ("Map gatherers: types 0–1 miner, 2–3 botanist, 4–5 and fishing spots fisher; each switch hides only its own",() =>
        {
            MapLocalGatheringPoint Point(uint type)=>new(1,"",1,"",[],false,null,TypeId:type);
            Check(Point(0).Job==16 && Point(1).Job==16 && Point(2).Job==17 && Point(3).Job==17 && Point(4).Job==18 && Point(5).Job==18 && Point(100).Job==18,"jobs");
            var settings=new MapViewSettings{ShowBotanyPoints=false};
            Check(settings.ShowsGatherer(16) && !settings.ShowsGatherer(17) && settings.ShowsGatherer(18),"switches");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(view.Contains("MapGatheringPoints(map).Where(point=>settings.ShowsGatherer(point.Job))"),"filter");
        }),
        // From LockedQuests966Tests: X-86 (0.966): locked quest markers can be hidden, from the map settings and the right-click menu; on by default.
        ("Locked quests 966: setting on by default, filter on the markers, box in the menu and the settings",() =>
        {
            Check(new TenshoAssistant.MapViewSettings().ShowLockedQuests,"on by default");
            var map=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(map.Contains("if(settings.ShowLockedQuests || mapMarkers?.QuestIcons.TryGetValue(marker.Icon,out var questIcon)!=true || !questIcon.Locked)"),"filter");
            Check(System.Text.RegularExpressions.Regex.Matches(map,"SettingBox\\(\"MapView.ShowLockedQuests\"").Count==2,"menu and settings");
        }),
        // From HeightArrows844Tests: 0.844 (X-67, user request): up and down arrows on map icons above or below the player.
        ("Height arrows: above, below, within the threshold, unknown",() =>
        {
            Check(MapHeight.Arrow(33.1f,-5.2f,10)==1 && MapHeight.Arrow(-5.2f,33.1f,10)==-1,"38 yalms apart");
            Check(MapHeight.Arrow(33.1f,31.9f,10)==0,"same floor");
            Check(MapHeight.Arrow(float.NaN,30,10)==0 && MapHeight.Arrow(30,float.NaN,10)==0,"unknown: none");
            var settings=new TenshoAssistant.MapViewSettings();
            Check(settings.HeightArrows && settings.HeightArrowYalms==10,"defaults");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(view.Contains("height:marker.Position.Y);") && view.Contains("height:fate.Position.Y);"),"quests, events, FATEs");
            // 0.849 (X-71): the game's chevron, turned down for « below ».
            var (upFrom,upTo)=MapHeight.ChevronUv(1);var (downFrom,downTo)=MapHeight.ChevronUv(-1);
            Check(upFrom.Y<upTo.Y && downFrom.Y>downTo.Y && upFrom.X==downFrom.X,"turned down");
            Check(MapHeight.ChevronTexture=="ui/uld/NaviMap.tex" && view.Contains("textures.GetFromGame(MapHeight.ChevronTexture)"),"the game's texture");
        }),
        // From MapMenuWidth400Tests: 0.400 (user report: right-click map menu, tabs cut): the lists are as wide as their tab names (at most 90 % of the screen), the tabs
        // scroll instead of being cut, and the width list names exactly the tabs drawn.
        ("Map menu lists: width from the tab names, scrolling tabs, same tabs in the width list and in the bar",() =>
        {
            var code = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapLocalLists.cs"));
            Check(code.Contains("ImGui.BeginTabBar(\"MapLocalTabs\",ImGuiTabBarFlags.FittingPolicyScroll)",StringComparison.Ordinal),"scrolling tabs");
            var drawn = Regex.Matches(code,@"BeginTabItem\(localization\.Get\(""([^""]+)""\)\)").Select(match=>match.Groups[1].Value).ToArray();
            var listed = Regex.Match(code,@"var tabs=new\[\]\{([^}]*)\}").Groups[1].Value.Split(',').Select(key=>key.Trim().Trim('"')).ToArray();
            Check(drawn.Length==9 && drawn.SequenceEqual(listed) && !drawn.Contains("MapLocal.Sightseeing"),"width list = tabs drawn");
        }),
        // From MapTabs677Tests: 0.677 (user request): the Maps page in tabs, and what fills a field of another tab brings that tab forward.
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
        // From MapCoordinates905Tests: 0.905 (X-76): one shared world-to-map conversion and one shared coordinate text, always with a dot as the game writes it.
        ("Map coordinate text: one decimal with a dot, even on a French Windows",() =>
        {
            var before=CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture=new CultureInfo("fr-FR");
                Check(MapProjection.CoordinateNumber(12.34f)=="12.3","number");
                Check(MapProjection.CoordinateText(new(12.34f,4.5f))=="X 12.3 Y 4.5","text");
                Check(MapBookmarks.DefaultName("Zone",12.34f,4.5f)=="Zone · 12.3, 4.5","default bookmark name");
            }
            finally { CultureInfo.CurrentCulture=before; }
        }),
        ("Map coordinate from a world position: the shared helper equals pixel then coordinate",() =>
        {
            var map=new MapDefinition(11,128,"La Noscea","Limsa Lominsa","Le Tillac",0,200,0,"ui/map/s1t1/00/s1t100_m.tex",-25,13,3);
            var world=new Vector3(31.5f,7,-112.25f);
            Check(MapProjection.CoordinateFromWorld(world,map)==MapProjection.CoordinateFromPixel(MapProjection.PixelFromWorld(world,map),map),"same coordinate");
        }),
        ("Tensho map after a reload: the game map left open waits for the Tensho map to be able to draw, for a short time",() =>
        {
            var replacer=ReadLf("Maps/NativeMapReplacer.Dalamud.cs");
            Check(replacer.Contains("var waiting=visible && !wasVisible && now<startingUntil && options.ReplaceGameMap && !bypass && !canShow();") && replacer.Contains("if(visible && !wasVisible && !waiting)") && replacer.Contains("wasVisible=visible && !waiting;"),"the decision waits");
            Check(replacer.Contains("startingUntil=DateTimeOffset.UtcNow.AddSeconds(15)"),"not for ever: a map that cannot draw (combat, duty) still leaves the game's");
        }),
        ("Map: its title bar on a setting, off by default; Balanced greyed is not choosable",() =>
        {
            Check(!new TenshoAssistant.MapViewSettings().HideTitleBar,"off by default");
            Check(Read2("MainWindow.MapView.cs").Contains("(settings.HideTitleBar ? ImGuiWindowFlags.NoTitleBar:0)") && Read2("MainWindow.MapView.cs").Contains("SettingBox(\"MapView.HideTitleBar\""),"setting");
            var panel=Read2("MainWindow.CollectablePanel.cs");
            Check(panel.Contains("if(same)ImGui.BeginDisabled();") && panel.Contains("if(same)ImGui.EndDisabled();") && panel.Contains("if(balancedOff && risk==50)risk=CollectableRisk.Safe;"),"disabled, shown as Safe");
            Check(panel.Contains("ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)"),"its tooltip still says why");
        }),
    ];
}
