using TenshoAssistant.Maps;

// 0.1469 (X-480, review of 0.1467 I2): MapOf of 0.1464 took every territory whose planmap.lgb names two maps or more as drawn on floors, and left
// out a placement in no box. The probe PROBE_FLOORS=check (docs/evidence/0.1469-map-floors-check.md) found 316 such territories, mostly not
// floors: on 4430 Level NPCs, 0.1464 put 295 on a map that is not theirs and 4 on none. The frames here are the probe's (Map sheet, 11/15):
// Château de Troïa 1089 shares its layout with 1070, whose maps its boxes name; Legs d'Alzadaal 1056's areas are drawn at other offsets;
// Eulmore 820's three floors; Ul'dah - Steps of Thal 131's two. Now only boxes of the territory's map's floors count (same territory, scale and
// offset, their own marker range), and a placement in no box stays on the territory's map (0 NPCs on a map not theirs, none left out).
internal static class MapFloors1469Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static readonly Dictionary<uint, MapFloorFrame> Frames = new[]
    {
        // Château de Troïa: 768 to 770 are territory 1070's (the layout's owner), 782 to 784 territory 1089's
        new MapFloorFrame(768, 1070, 200, -76, -180, 523), new(769, 1070, 200, 143, -64, 524), new(770, 1070, 200, 29, 112, 525),
        new(782, 1089, 200, -76, -180, 528), new(783, 1089, 200, 143, -64, 529), new(784, 1089, 200, 29, 112, 530),
        // Legs d'Alzadaal: territory 1056's map 752 belongs to 1050; its areas 753, 754, 785 at other offsets
        new(752, 1050, 200, 15, 0, 509), new(753, 1050, 200, 0, 50, 510), new(754, 1050, 200, -15, 0, 511), new(785, 1056, 200, -15, 0, 527),
        // Eulmore and Ul'dah - Steps of Thal: floors of one frame
        new(498, 820, 200, 0, 0, 371), new(554, 820, 200, 0, 0, 372), new(555, 820, 200, 0, 0, 373),
        new(14, 131, 200, 0, 0, 60), new(73, 131, 200, 0, 0, 62),
        // versions of one land share their marker range (the Island Sanctuary's 772 and 773: 533)
        new(772, 1055, 100, -175, 138, 533), new(773, 1055, 100, -175, 138, 533),
    }.ToDictionary(frame => frame.MapId);

    private static MapFloorFrame? FrameOf(uint id) => Frames.TryGetValue(id, out var frame) ? frame : null;
    private static MapFloorRange Box(uint map, float y, float half = 20) => new(map, 0, y, 0, 100, half, 100, 0);

    private static readonly MapFloorRange[] Eulmore =
    [
        new(498, 0, -7, -20, 150, 22, 200, 0), new(554, 0, 41.3f, 14.4f, 100, 30.3f, 65.6f, 0), new(555, 0, 90, -20, 100, 24, 100, 0),
        new(498, 25.4f, 8, -58.4f, 10, 10, 10, 0),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Map floors 1469: a layout shared with another territory: its boxes name that territory's maps, never used (Château de Troïa)", () =>
        {
            MapFloorRange[] boxes = [Box(768, 0), Box(769, 50), Box(770, 100), Box(782, 0), Box(783, 50), Box(784, 100)];
            var floors = MapFloors.Floors(boxes, Frames[782], FrameOf);
            Check(floors.All(range => range.MapId == 782) && !MapFloors.Layered(floors), "only the territory's own map: " + string.Join(",", floors.Select(range => range.MapId)));
            Check(MapFloors.MapOf(floors, 782, 0, 50, 0) == 782 && MapFloors.MapOf(floors, 782, 0, 100, 0) == 782, "every placement on 1089's map");
            Check(MapFloors.At(boxes, 0, 50, 0) is 769 or 783, "where 0.1464 put it: a map of another frame, or of territory 1070");
        }),
        ("Map floors 1469: areas drawn at another offset are not floors: a coordinate on the territory's map would be wrong there (Legs d'Alzadaal)", () =>
        {
            MapFloorRange[] boxes = [Box(752, 0), Box(753, 50), Box(754, 100), Box(785, 150)];
            var floors = MapFloors.Floors(boxes, Frames[752], FrameOf);
            Check(!MapFloors.Layered(floors) && MapFloors.MapOf(floors, 752, 0, 50, 0) == 752, "the territory's map, whose frame the coordinates use");
            Check(!MapFloors.SameFrame(Frames[752], Frames[753]) && !MapFloors.SameFrame(Frames[754], Frames[785]), "other offsets, other territory");
        }),
        ("Map floors 1469: floors of one frame stay floors (Eulmore, Ul'dah - Steps of Thal); versions of one land do not", () =>
        {
            var floors = MapFloors.Floors(Eulmore, Frames[498], FrameOf);
            Check(floors.Count == 4 && MapFloors.Layered(floors), "Eulmore's four boxes");
            Check(MapFloors.MapOf(floors, 498, 10, -14.1f, 30) == 498 && MapFloors.MapOf(floors, 498, 5, 23, 20) == 554 && MapFloors.MapOf(floors, 498, 0, 82, -10) == 555, "each floor");
            var thal = MapFloors.Floors([Box(14, 0), Box(73, 50)], Frames[14], FrameOf);
            Check(MapFloors.Layered(thal) && MapFloors.MapOf(thal, 14, 0, 50, 0) == 73, "Thal's upper floor");
            var island = MapFloors.Floors([Box(772, 0), Box(773, 50)], Frames[772], FrameOf);
            Check(!MapFloors.Layered(island) && MapFloors.MapOf(island, 772, 0, 50, 0) == 772, "the Island's versions stay together");
            Check(!MapFloors.Layered(MapFloors.Floors([Box(498, 0), Box(9999, 50)], Frames[498], FrameOf)), "a map the sheet does not have is no floor");
        }),
        ("Map floors 1469: a placement in no box stays on the territory's map, never on none (0.1464 left it out)", () =>
        {
            var floors = MapFloors.Floors(Eulmore, Frames[498], FrameOf);
            Check(MapFloors.MapOf(floors, 498, 0, 200, 0) == 498 && MapFloors.MapOf(floors, 498, 900, 0, 0) == 498, "above and beside every box");
            Check(MapFloors.MapOf([], 12, 0, 200, 0) == 12, "no boxes");
            var random = new Random(1469);
            for (var n = 0; n < 2000; n++)
                Check(MapFloors.MapOf(floors, 498, random.Next(-1000, 1000), random.Next(-100, 200), random.Next(-1000, 1000)) is 498 or 554 or 555, "always a map of Eulmore");
        }),
        ("Map floors 1469: the three loaders take the territory's floors once per layout and map, and keep a placement in no box (X-482 M5)", () =>
        {
            static string Read(string path) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + path)).Replace("\r\n", "\n");
            var vendors = Read("Npcs/NpcVendorLoader.Dalamud.cs");
            var triad = Read("MiniGames/TriadNpcLoader.Dalamud.cs");
            var bells = Read("Maps/MapLocalIndexLoader.Dalamud.cs");
            Check(vendors.Contains("new Maps.MapFloorReader.FloorCache(data)", StringComparison.Ordinal) && vendors.Contains("Maps.MapFloors.MapOf(floors.Of(bg,map),map.RowId,", StringComparison.Ordinal), "vendors");
            Check(triad.Contains("new Maps.MapFloorReader.FloorCache(data)", StringComparison.Ordinal) && triad.Contains("Maps.MapFloors.MapOf(floors.Of(bg, map), map.RowId,", StringComparison.Ordinal)
                  && !triad.Contains("MapFloorReader.Read(", StringComparison.Ordinal), "Triple Triad: no read per NPC");
            Check(bells.Contains("Maps.MapFloorReader.Floors(data,bg,map)", StringComparison.Ordinal), "bells and chests");
            foreach (var (name, text) in new[] { ("vendors", vendors), ("bells", bells) })
                Check(!text.Contains("if(floorMap==0)continue;", StringComparison.Ordinal), name + ": never left out");
            Check(vendors.Contains("floors-1469", StringComparison.Ordinal) && bells.Contains("floors-1469", StringComparison.Ordinal), "the caches built again");
        }),
    ];
}
