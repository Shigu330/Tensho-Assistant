using TenshoAssistant.Maps;

// 0.1464 (X-462): Tensho's map showed, on each of Eulmore's floors, the icons of the other floors (user, 11/15). Eulmore as the game's sheets and
// layout give it (docs/evidence/0.1464-map-floors.md): territory 820, maps 498 The Root (markers 371), 554 The Trunk (372), 555 The Canopy (373),
// one frame; planmap.lgb's MapRange boxes 498 at y -7 (half 22), 554 at y 41.3 (half 30.3), 555 at y 90 (half 24).
internal static class MapFloors1464Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static readonly MapDefinition Root = new(498, 820, "Norvrandt", "Eulmore", "The Root", 1, 400, OffsetX: 0, OffsetY: 0, MarkerRange: 371);
    private static readonly MapDefinition Trunk = new(554, 820, "Norvrandt", "Eulmore", "The Trunk", 2, 400, OffsetX: 0, OffsetY: 0, MarkerRange: 372);
    private static readonly MapDefinition Canopy = new(555, 820, "Norvrandt", "Eulmore", "The Canopy", 3, 400, OffsetX: 0, OffsetY: 0, MarkerRange: 373);
    private static readonly MapFloorRange[] Boxes =
    [
        new(498, 0, -7, -20, 150, 22, 200, 0), new(554, 0, 41.3f, 14.4f, 100, 30.3f, 65.6f, 0), new(555, 0, 90, -20, 100, 24, 100, 0),
        new(498, 25.4f, 8, -58.4f, 10, 10, 10, 0),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Map floors 1464: Eulmore's three floors are not the same map: an icon of a floor shows on that floor only", () =>
        {
            var catalog = new MapCatalog([Root, Trunk, Canopy]);
            foreach (var floor in new[] { Root, Trunk, Canopy })
                Check(catalog.Equivalent(floor).SequenceEqual([floor.MapId]), $"{floor.Subzone}: {string.Join(",", catalog.Equivalent(floor))}");
            // the vendor rows of a map: a vendor on The Canopy is on none of the two others
            var vendors = new[] { (Name: "Canopy vendor", Map: 555u), (Name: "Root vendor", Map: 498u), (Name: "Trunk vendor", Map: 554u) };
            foreach (var floor in new[] { Root, Trunk, Canopy })
            {
                var same = catalog.Equivalent(floor).ToHashSet();
                Check(vendors.Count(vendor => same.Contains(vendor.Map)) == 1, floor.Subzone + " shows one vendor");
            }
        }),
        ("Map floors 1464: versions of one land stay together (Auxesia's stages, the Island Sanctuary's), a default map without markers too", () =>
        {
            var catalog = new MapCatalog([new MapDefinition(1245, 1319, "?", "Auxesia", "", 0, 100), new MapDefinition(1246, 1319, "?", "Auxesia", "", 0, 100, MarkerRange: 752),
                new MapDefinition(1247, 1319, "?", "Auxesia", "", 0, 100, MarkerRange: 752), new MapDefinition(772, 1055, "?", "Island", "", 0, 100, MarkerRange: 533),
                new MapDefinition(773, 1055, "?", "Island", "", 0, 100, MarkerRange: 533)]);
            Check(catalog.Equivalent(catalog.Find(1247)!).Order().SequenceEqual([1245u, 1246u, 1247u]), "Auxesia's stages with the default map");
            Check(catalog.Equivalent(catalog.Find(773)!).Order().SequenceEqual([772u, 773u]), "the Island's versions");
        }),
        ("Map floors 1464: a layout's placement takes the floor of the box holding it (Eulmore's Level NPCs: -14 to 7, 23 to 45, 82 to 86)", () =>
        {
            Check(MapFloors.MapOf(Boxes, 498, 10, -14.1f, 30) == 498 && MapFloors.MapOf(Boxes, 498, -20, 6.8f, -60) == 498, "The Root");
            Check(MapFloors.MapOf(Boxes, 498, 5, 23, 20) == 554 && MapFloors.MapOf(Boxes, 498, -10, 45, 40) == 554, "The Trunk");
            Check(MapFloors.MapOf(Boxes, 498, 0, 82, -10) == 555 && MapFloors.MapOf(Boxes, 498, 30, 85.5f, -60) == 555, "The Canopy");
            Check(MapFloors.MapOf(Boxes, 498, 25, 8, -58) == 498, "the smallest box wins where two hold the point");
        }),
        ("Map floors 1464: a placement in no box is on no other floor (0.1469, X-480: on the territory's map); a territory without floors keeps its map", () =>
        {
            Check(MapFloors.MapOf(Boxes, 498, 0, 200, 0) == 498 && MapFloors.MapOf(Boxes, 498, 900, 0, 0) == 498, "outside every box: the territory's map");
            Check(MapFloors.MapOf([], 12, 0, 200, 0) == 12, "no boxes: the territory's map");
            Check(MapFloors.MapOf([new MapFloorRange(12, 0, 0, 0, 10, 10, 10, 0)], 12, 500, 0, 0) == 12, "one map's boxes only: not floors, the territory's map");
        }),
        ("Map floors 1464: a turned box is read in its own axes", () =>
        {
            var turned = new MapFloorRange(7, 0, 0, 0, 10, 5, 1, MathF.PI / 2);   // 20 long along z once turned a quarter
            Check(MapFloors.Contains(turned, 0, 0, 9) && !MapFloors.Contains(turned, 9, 0, 0), "turned a quarter about the vertical axis");
        }),
        ("Map floors 1464: the loaders of layout placements read the floors (vendors, Triple Triad players, bells and chests)", () =>
        {
            static string Read(string path) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + path)).Replace("\r\n", "\n");
            Check(Read("Npcs/NpcVendorLoader.Dalamud.cs").Contains("var floorMap=Maps.MapFloors.MapOf(floors.Of(bg,map),map.RowId,position.X,position.Y,position.Z);", StringComparison.Ordinal)
                  && Read("Npcs/NpcVendorLoader.Dalamud.cs").Contains("list.Add(new(territory.RowId,floorMap,zone,", StringComparison.Ordinal), "vendors");
            Check(Read("MiniGames/TriadNpcLoader.Dalamud.cs").Contains("Maps.MapFloors.MapOf(floors.Of(bg, map), map.RowId, position.X, position.Y, position.Z)", StringComparison.Ordinal), "Triple Triad");
            Check(Read("Maps/MapLocalIndexLoader.Dalamud.cs").Contains("found.Add(new(MapLocalKind.PlacedObject,++added,floorMap,at.X,at.Z,0,icon));", StringComparison.Ordinal), "bells and chests");
        }),
    ];
}
