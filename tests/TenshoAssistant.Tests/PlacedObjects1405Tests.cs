using System.Numerics;
using TenshoAssistant.Maps;

// X-424 (0.1405, review 0.1404 I1): a layout placement the game marks on another map of the same territory is not added to the main map.
// Probe rerun (docs/evidence/0.1402-bells-and-chests.md, section 0.1405): Eulmore (territory 820, maps 498, 554, 555; 498 and 555 both
// without offset, size factor 200): the bell of planevent at world 7.2/31.5 is pixel 1038/1087, with no bell marker on 498 and the game's
// bell marker on 555 at 0.6 px. The five company workshops have one map each: their bell and chest stay added.
internal static class PlacedObjects1405Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Placed objects 1405: Eulmore's bell, marked by the game on map 555, is not drawn on map 498", () =>
        {
            var main = new List<(uint, Vector2)> { (60425u, new Vector2(1180, 940)) };
            var pixel = PlacedObjects.Pixel(7.2f, 31.5f, 0, 0, 200);
            Check(Vector2.Distance(pixel, new(1038.4f, 1087)) < 1, "pixel on 498 " + pixel);
            Check(PlacedObjects.Missing(main, 60425, pixel), "the main map alone has no bell there (what 0.1402 saw)");
            var floor555 = new PlacedObjects.MapFrame(0, 0, 200, [(60425u, new Vector2(1038.4f + 0.6f, 1087))]);
            var floor554 = new PlacedObjects.MapFrame(0, 0, 200, []);
            Check(PlacedObjects.MarkedOnAnyMap([floor554, floor555], 60425, 7.2f, 31.5f), "the game's marker on map 555 is that bell");
        }),
        ("Placed objects 1405: another map's marker counts only for the same icon and the same spot, with that map's own projection", () =>
        {
            var other = new PlacedObjects.MapFrame(-100, 50, 400, [(60425u, PlacedObjects.Pixel(10, 20, -100, 50, 400)), (60460u, new Vector2(300, 300))]);
            Check(PlacedObjects.MarkedOnAnyMap([other], 60425, 10, 20), "projected with the other map's offset and size");
            Check(PlacedObjects.MarkedOnAnyMap([other], 60425, 10 + 7.5f, 20), "30 px at size 400: same spot");
            Check(!PlacedObjects.MarkedOnAnyMap([other], 60425, 10 + 9, 20), "36 px: another spot");
            Check(!PlacedObjects.MarkedOnAnyMap([other], 60460, 10, 20), "a bell marker does not hide a chest");
            Check(!PlacedObjects.MarkedOnAnyMap([new PlacedObjects.MapFrame(0, 0, 0, [(60425u, new Vector2(1024, 1024))])], 60425, 0, 0), "a map without size is skipped");
        }),
        ("Placed objects 1405: a workshop (one map, no other floor) keeps its bell and chest", () =>
        {
            Check(!PlacedObjects.MarkedOnAnyMap([], 60425, -5.0f, 8.0f) && !PlacedObjects.MarkedOnAnyMap([], 60460, -3.0f, 11.4f), "no other map: still missing");
            Check(PlacedObjects.Missing([], 60460, PlacedObjects.Pixel(-3.0f, 11.4f, 0, 0, 400)), "the workshop's chest is added");
        }),
        ("Placed objects 1405: the loader asks every map of the territory, rebuilds the cache once and logs its longest step", () =>
        {
            var loader = Read("Maps/MapLocalIndexLoader.Dalamud.cs");
            Check(loader.Contains("if(!PlacedObjects.Missing(marks,icon,pixel) || PlacedObjects.MarkedOnAnyMap(others,icon,at.X,at.Z))continue;", StringComparison.Ordinal),
                "both checks before a placement is added");
            Check(loader.Contains("territoryMaps.Where(other=>other.RowId!=map.RowId)", StringComparison.Ordinal)
                && loader.Contains("foreach(var each in data.GetExcelSheet<Map>())", StringComparison.Ordinal), "the other maps come from Map.TerritoryType");
            Check(loader.Contains("longest step {2:F1} ms ({3})", StringComparison.Ordinal), "the longest layout step is logged (X-425 M3)");
            var rules = System.Text.RegularExpressions.Regex.Match(Read("Maps/MapLocalIndex.cs"), @"private const int CopyRules=(\d+);");
            Check(rules.Success && int.Parse(rules.Groups[1].Value) >= 5, "the positions' cache is rebuilt once (copy rules 5 or later)");
        }),
    ];
}
