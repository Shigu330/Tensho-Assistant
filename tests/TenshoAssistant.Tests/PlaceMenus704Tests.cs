using System.Text.RegularExpressions;

// 0.704 (user request): wherever a page places a flag, a right click also leads to the nearest aetheryte.
internal static class PlaceMenus704Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    // What is left out, and why: the map's own canvas has its entry (DrawNearestAetheryteEntry); Maps.cs is checked below by its own line.
    private static readonly string[] Exempt=["MainWindow.MapView.cs","MainWindow.ChatMapLinks.cs"];

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Place menus: the shared entries lead to the nearest aetheryte",() =>
        {
            var menus=Read("UI/Components/EntityMenus.cs");
            Check(menus.Contains("MapView.NearestAetheryte") && menus.Contains("AskNearestAetheryteAt(map,MapProjection.PixelFromCoordinate(coordinate,map)"),"entry");
        }),
        ("Place menus: every page that places a flag also has the right click",() =>
        {
            foreach(var file in Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"MainWindow*.cs"))
            {
                var name=Path.GetFileName(file);
                if(Exempt.Contains(name))continue;
                var text=File.ReadAllText(file);
                if(!Regex.IsMatch(text,@"PlaceFlagFromMapView\(|OpenMapPointFromPage\(|[^a-zA-Z]FlagAt\(map"))continue;
                Check(Regex.IsMatch(text,@"PlaceMenu\(|NpcMenu\(|PlaceEntries\(|AskNearestAetheryteAt\("),name+" places a flag with no way to the nearest aetheryte");
            }
        }),
        ("Place menus: no aetheryte entry on a map that has none, the island first",() =>
        {
            var menus=Read("UI/Components/EntityMenus.cs");
            Check(menus.Contains("map.TerritoryId!=IslandResources.Territory") && menus.Contains("marker.AetheryteId!=0"),"the check");
            Check(menus.Contains("if(MapHasAetheryte(map) && ImGui.Selectable(localization.Get(\"MapView.NearestAetheryte\")"),"the menus");
            Check(Read("MainWindow.MapView.cs").Contains("if(!MapHasAetheryte(map))return;"),"the map");
        }),
        ("Hover vendors: every vendor by default, and each tooltip goes through the settings",() =>
        {
            var settings=new TenshoAssistant.Configuration();
            Check(settings.HoverVendors && !settings.HoverVendorsMetOnly && !settings.HoverVendorsByCurrency,"as before by default");
            // 0.904 (X-76): the old vendor rows of the hover (HoveredNpcCount, DrawHoveredNpcs) had no caller left and were removed; the hover reads HoverOffers.
            Check(!Read("MainWindow.NpcVendors.cs").Contains("DrawHoveredNpcs") && Read("MainWindow.ListPanels.cs").Contains("private IReadOnlyList<NpcVendorOffer> HoverOffers(uint item)"),"vendor lines");
            var hover=Read("MainWindow.MarketHover.cs");
            Check(hover.Contains("HoverOffers(item.Id) is { Count:>0 } offers") && hover.Contains("HoverOffers(id) is { Count:>0 } offers"),"sources");
        }),
        ("Place menus: monster loot from the wiki reads its place",() =>
        {
            Check(Read("MainWindow.Monsters.cs").Contains("PlaceMenu($\"WikiMonsterPlace{wikiRow}\""),"wiki rows");
        }),
    ];
}
