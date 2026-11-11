// Test audit of 2026-11-09, batch 2 (0.1021): Labyrinth979Tests, LabyrinthSheets1001Tests merged into LabyrinthTests; every case is kept.
using TenshoAssistant.Competitive;
internal static class LabyrinthTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From Labyrinth979Tests: X-109 (0.979, DEC-047): a developer reading inside the Beastmaster labyrinth only, read only, written on change, to find when a fight starts.
        // X-137 (0.1115, user's choice of 2026-11-11): the labyrinth is no longer cut (DEC-038 completed); its territories stay known.
        ("Labyrinth 979: its territories are known, no longer cut (X-137); the reading is developer-only, labyrinth-only, on change",() =>
        {
            Check(!CompetitiveZones.Labyrinth.Any(CompetitiveZones.Listed.Contains) && CompetitiveZones.Labyrinth.All(CompetitiveZones.IsLabyrinth),"known, not cut");
            var gate=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Competitive/CompetitiveGate.Dalamud.cs"));
            Check(gate.Contains("if(CompetitiveGate.Developer?.Invoke()==true && CompetitiveZones.IsLabyrinth((uint)client.TerritoryType))"),"guard");
            Check(gate.Contains("if(seen==labyrinthSeen)return;"),"on change");
        }),
        // From LabyrinthSheets1001Tests: X-92 (0.1001): the labyrinth's boards also come from the sheets, so a new board is cut (and its board panel allowed) without code.
        ("Labyrinth sheets 1001: a board found in the sheets is known as labyrinth (not cut since X-137); the written list stays",() =>
        {
            var zones=CompetitiveZones.Build([],[],[1399u,0u]);
            Check(!zones.Contains(1399) && CompetitiveZones.IsLabyrinth(1399) && CompetitiveZones.IsLabyrinth(1339) && !zones.Contains(1343),"found and written, neither cut");
            Check(!CompetitiveZones.IsLabyrinth(0),"no zero");
            CompetitiveZones.Build([],[]);
            Check(!CompetitiveZones.IsLabyrinth(1399) && CompetitiveZones.IsLabyrinth(1339),"reset to the written list");
        }),
    ];
}
