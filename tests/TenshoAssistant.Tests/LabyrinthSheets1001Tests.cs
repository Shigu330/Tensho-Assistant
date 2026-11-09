using TenshoAssistant.Competitive;

// X-92 (0.1001): the labyrinth's boards also come from the sheets, so a new board is cut (and its board panel allowed) without code.
internal static class LabyrinthSheets1001Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Labyrinth sheets 1001: a board found in the sheets is cut and known as labyrinth; the written list stays",() =>
        {
            var zones=CompetitiveZones.Build([],[],[1399u,0u]);
            Check(zones.Contains(1399) && CompetitiveZones.IsLabyrinth(1399) && CompetitiveZones.IsLabyrinth(1339) && zones.Contains(1343),"found and written");
            Check(!CompetitiveZones.IsLabyrinth(0),"no zero");
            CompetitiveZones.Build([],[]);
            Check(!CompetitiveZones.IsLabyrinth(1399) && CompetitiveZones.IsLabyrinth(1339),"reset to the written list");
        }),
    ];
}
