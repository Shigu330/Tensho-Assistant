using TenshoAssistant.Beastmaster;

// 0.1546 (X-576, the user's capture of 18/11 around 00:05 before the 2nd board of the Beastmaster labyrinth: « Rang de bête recommandé :
// 6 à 10 (synchronisation pour les bêtes dont le rang est supérieur) »; the 1st board showed « 1 à 5 », note of 0.498): the range's first
// number is the previous board's synchronisation rank (XBMContent column 3: 5, 10, 15, 20, 25) plus one. Column 2 (3, 8, 13, 18, 23), read
// by 0.1545, gave 8 to 10 on the 2nd board: wrong (docs/evidence/0.1546-beast-rank-range.md).
internal static class BeastRankFloor1546Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    // XBMContent column 3 of the five boards (rows 1 to 5: 1st, 2nd, 3rd board, 1st and 2nd expert board), XbmProbe 18/11.
    private static readonly int[] Limits = [5, 10, 15, 20, 25];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast rank floor 1546: the five boards give 1, 6, 11, 16, 21 (the captures: 1 to 5, 6 to 10), whatever the order of the rows", () =>
        {
            var floors = Limits.Select(limit => BeastCatalog.RankFloor(Limits, limit)).ToArray();
            Check(floors.SequenceEqual(new[] { 1, 6, 11, 16, 21 }), "floors " + string.Join(", ", floors));
            Check(BeastCatalog.RankFloor(Enumerable.Reverse(Limits), 10) == 6, "rows in another order: still 6 for the 2nd board");
            Check(BeastCatalog.RankFloor([5], 5) == 1, "a single board: 1");
            Check(BeastCatalog.RankFloor(Limits, 0) == 0 && BeastCatalog.RankFloor([0, 10], 10) == 1, "a rank not known: 0 for itself, ignored for the others");
        }),
        ("Beast rank floor 1546: on each of the five boards, a rank read just under the floor is low; the floor and the board's rank are not", () =>
        {
            foreach (var (limit, index) in Limits.Select((limit, index) => (limit, index)))
            {
                var floor = BeastCatalog.RankFloor(Limits, limit);
                var board = BeastPlan1543Tests.FirstBoard() with { Id = (uint)index + 1, RankLimit = limit, RankRecommended = floor };
                Check(!BeastTeams.RankLow(board, floor) && !BeastTeams.RankLow(board, limit), $"board {index + 1}: {floor} and {limit} not low");
                Check(floor == 1 ? !BeastTeams.RankLow(board, 1) : BeastTeams.RankLow(board, floor - 1), $"board {index + 1}: {floor - 1} low (none on the 1st)");
            }
            var second = BeastPlan1543Tests.FirstBoard() with { RankLimit = 10, RankRecommended = BeastCatalog.RankFloor(Limits, 10) };
            Check(!BeastTeams.RankLow(second, 7) && !BeastTeams.RankLow(second, 6), "2nd board: 6 and 7 are in the range the game shows (0.1545 said them low)");
        }),
    ];
}
