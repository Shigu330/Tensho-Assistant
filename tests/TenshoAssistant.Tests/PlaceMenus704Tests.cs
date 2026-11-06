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
        ("Hover vendors: every vendor by default, and each tooltip goes through the settings",() =>
        {
            var settings=new TenshoAssistant.Configuration();
            Check(settings.HoverVendors && !settings.HoverVendorsMetOnly && !settings.HoverVendorsByCurrency,"as before by default");
            Check(Read("MainWindow.NpcVendors.cs").Contains("private int HoveredNpcCount(uint id)=>HoverOffers(id).Count;") && Read("MainWindow.NpcVendors.cs").Contains("HoverOffers(id) is not { Count:>0 } offers"),"vendor lines");
            var hover=Read("MainWindow.MarketHover.cs");
            Check(hover.Contains("HoverOffers(item.Id) is { Count:>0 } offers") && hover.Contains("HoverOffers(id) is { Count:>0 } offers"),"sources");
        }),
        ("Place menus: monster loot from the wiki reads its place",() =>
        {
            Check(Read("MainWindow.Monsters.cs").Contains("PlaceMenu($\"WikiMonsterPlace{wikiRow}\""),"wiki rows");
        }),
    ];
}
