// 0.853 (X-48, user report): the nearest aetheryte offered on a map without one, when there is a way in.
internal static class NearestOnTarget853Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Nearest aetheryte: a map with a shard or a neighbour counts",() =>
        {
            var menus=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/UI/Components/EntityMenus.cs"));
            Check(menus.Contains("marker.AetheryteId!=0 || marker.LinkMapId!=0") && menus.Contains("mapMarkers.Aetherytes.Any(row=>!row.Main && row.Group!=0 && row.Territory==map.TerritoryId)"),"ways in");
            var links=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatMapLinks.cs"));
            Check(links.Contains("if(markers.Count==0 && ZoneEntryFor(map,request.Pixel,failed ? null:costs) is { } entry)"),"the way said in the question (0.833)");
        }),
    ];
}
