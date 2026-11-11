// 0.1115 (X-137, user's choice of 2026-11-11, R03): the Beastmaster labyrinth is no longer cut; PvP and ranked content still are; DEC-038
// completed in both halves of DECISIONS.md.
using TenshoAssistant.Competitive;

internal static class LabyrinthOpen1115Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Labyrinth open 1115: not cut in the labyrinth, still cut in PvP, tournaments, Mahjong and deep dungeons; DEC-038 says it in both languages",() =>
        {
            var zones=CompetitiveZones.Build([(250,true)],[(561,false,CompetitiveZones.DeepDungeonContentType)],[1339u,1399u]);
            foreach(var board in new uint[]{1339,1340,1341,1342,1343,1399})
                Check(!CompetitiveZones.IsCut(true,board,false,zones,false) && CompetitiveZones.IsLabyrinth(board),$"board {board}: not cut, still known");
            foreach(var cut in new uint[]{250,561,940,941,831})Check(CompetitiveZones.IsCut(true,cut,false,zones,false),$"{cut} still cut");
            Check(CompetitiveZones.IsCut(true,1339,true,zones,false),"PvP (IsPvP) still cuts everywhere");
            CompetitiveZones.Build([],[]);
            var decisions=File.ReadAllText(Path.GetFullPath("docs/project/DECISIONS.md"));
            Check(decisions.Contains("the Crucible of the Unbroken (Beastmaster labyrinth) is no longer cut",StringComparison.Ordinal)
                && decisions.Contains("le Dédale bestial n'est plus coupé",StringComparison.Ordinal),"DEC-038 completed in both halves");
        }),
    ];
}
