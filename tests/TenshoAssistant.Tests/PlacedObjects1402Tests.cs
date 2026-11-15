using System.Numerics;
using TenshoAssistant.Maps;

// X-386 (0.1402): bells and company chests from the layouts, only where the game's own map has no marker of them. Positions from the probe
// (docs/evidence/0.1402-bells-and-chests.md): the company workshop of Mist (map 204, size factor 400, no offset): chest at world -3.0/11.4 →
// pixel 1012/1070, bell at -5.0/8.0 → 1004/1056.
internal static class PlacedObjects1402Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Placed objects 1402: the game's ids and icons", () =>
        {
            Check(PlacedObjects.IconOf(2000470) == 60460 && PlacedObjects.IconOf(2006565) == 60425 && PlacedObjects.IconOf(2000401) == 60425
                && PlacedObjects.IconOf(2000403) == 60425 && PlacedObjects.IconOf(2010284) == 60425 && PlacedObjects.IconOf(2000441) == 60425, "bells and chest");
            Check(PlacedObjects.IconOf(2014985) == 0 && PlacedObjects.IconOf(0) == 0 && PlacedObjects.IconOf(2000072) == 0, "festival bell and others left out");
        }),
        ("Placed objects 1402: positions of the company workshop of Mist, from the probe", () =>
        {
            var chest = PlacedObjects.Pixel(-3.0f, 11.4f, 0, 0, 400);
            var bell = PlacedObjects.Pixel(-5.0f, 8.0f, 0, 0, 400);
            Check(Vector2.Distance(chest, new(1012, 1070)) < 1 && Vector2.Distance(bell, new(1004, 1056)) < 1, "pixels " + chest + " " + bell);
        }),
        ("Placed objects 1402: kept only where the game's map has no marker of the same icon", () =>
        {
            var marks = new List<(uint, Vector2)> { (60425u, new Vector2(1039, 1087)), (60460u, new Vector2(500, 500)) };
            Check(!PlacedObjects.Missing(marks, 60425, new Vector2(1038, 1087)), "the game's own bell marker: not added twice");
            Check(!PlacedObjects.Missing(marks, 60425, new Vector2(1039 + 27.5f, 1087)), "27.5 px (the farthest match of the probe) is the same spot");
            Check(PlacedObjects.Missing(marks, 60425, new Vector2(500, 500)), "a chest marker does not hide a bell");
            Check(PlacedObjects.Missing(marks, 60460, new Vector2(1012, 1070)), "no chest marker here: added");
            Check(!PlacedObjects.Missing(marks, 60425, new Vector2(-298, -667)) && !PlacedObjects.Missing(marks, 60425, new Vector2(1000, 2100)),
                "outside the map's texture (a housing subdivision's own map): left out");
            Check(!PlacedObjects.Missing(marks, 0, new Vector2(1000, 1000)) && !PlacedObjects.Missing(marks, 60425, new Vector2(float.NaN, 1)), "no icon, no position");
        }),
        ("Placed objects 1402: the index lists them per map; the map draws them as the game's markers", () =>
        {
            var index = new MapLocalIndex([new(MapLocalKind.PlacedObject, 1, 204, -3.0f, 11.4f, 0, 60460), new(MapLocalKind.PlacedObject, 2, 204, -5.0f, 8.0f, 0, 60425),
                new(MapLocalKind.PlacedObject, 3, 0, 1, 1, 0, 60425)]);
            Check(index.OnMap(MapLocalKind.PlacedObject, 204).Select(row => row.Data).Order().SequenceEqual(new uint[] { 60425, 60460 })
                && index.OnMap(MapLocalKind.PlacedObject, 0).Count == 0, "two on the workshop's map, none without a map");
            var view = Read("MainWindow.MapView.cs");
            Check(view.Contains("foreach(var placed in placedIndex.OnMap(MapLocalKind.PlacedObject,map.MapId))", StringComparison.Ordinal)
                && view.Contains("Add(placed.Data,0,MapProjection.PixelFromWorld(new Vector3(placed.X,0,placed.Z),map),null);", StringComparison.Ordinal),
                "drawn with the game's markers: same icon, size table (filter) and name");
            var loader = Read("Maps/MapLocalIndexLoader.Dalamud.cs");
            Check(loader.Contains("foreach(var step in CopyPlacedObjects(count))", StringComparison.Ordinal), "the loader reads the layouts");
        }),
    ];
}
