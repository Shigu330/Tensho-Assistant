using TenshoAssistant.Beastmaster;

// 0.1545 (X-575 part 5, the user's request of 17/11 around 23:30: « the rank is not a criterion, but one can say the rank is low for the
// current board »): the board window says « Rang de bête recommandé : X à Y » (Addon 17912, two numbers given by the game's code); Y is the
// synchronisation rank, XBMContent column 3 (5, 10, 15, 20, 25). 0.1546 (X-576, the user's capture « 6 à 10 » on the 2nd board): X is the
// previous board's Y plus one (1, 6…), not column 2 (docs/evidence/0.1546-beast-rank-range.md). The warning never changes the choice.
internal static class BeastRankLow1545Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static Board Second() => BeastPlan1543Tests.FirstBoard() with { Id = 2, Name = "2e plateau", TeamSize = 12, RankLimit = 10, RankRecommended = 6 };

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast rank low 1545: two boards, a rank read below the recommended range is low; at or above it, assumed or the range unknown: never", () =>
        {
            var first = BeastPlan1543Tests.FirstBoard() with { RankRecommended = 1 };
            var second = Second();
            Check(!BeastTeams.RankLow(first, 1) && !BeastTeams.RankLow(first, 5), "1st board (1 to 5): no rank read is low");
            Check(BeastTeams.RankLow(second, 5) && BeastTeams.RankLow(second, 1) && !BeastTeams.RankLow(second, 6) && !BeastTeams.RankLow(second, 10), "2nd board (6 to 10): 1 and 5 low, 6 and 10 not");
            Check(!BeastTeams.RankLow(second, 5, known: false), "a rank assumed (not read) is never said low");
            Check(!BeastTeams.RankLow(BeastPlan1543Tests.FirstBoard(), 1), "range not known (0): never");
        }),
        ("Beast rank low 1545: the rank is not a criterion of the choice: the same plan whatever the ranks below the range", () =>
        {
            var beasts = BeastPlan1543Tests.Held.Select(BeastPlan1543Tests.Make).ToArray();
            var held = BeastPlan1543Tests.Held.Select(row => row.Id).ToHashSet();
            var low = held.ToDictionary(id => id, _ => 1);
            var high = held.ToDictionary(id => id, _ => 8);
            // The test's beasts have the same attributes at every rank: only a rule on the rank itself could change the choice.
            var a = BeastTeams.Plan(Second(), beasts, held, low);
            var b = BeastTeams.Plan(Second(), beasts, held, high);
            Check(a.Team.Select(pick => pick.Beast).SequenceEqual(b.Team.Select(pick => pick.Beast)), "the same twelve");
            Check(a.Battles.Zip(b.Battles).All(pair => pair.First.Team.Select(pick => pick.Beast).SequenceEqual(pair.Second.Team.Select(pick => pick.Beast))), "the same trios, in the same order");
        }),
        ("Beast rank low 1545: the catalog gives each board its range's first number (0.1546: RankFloor, not column 2); the panel says it beside the ten and for the team taken, both languages", () =>
        {
            var catalog = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Beastmaster/BeastCatalog.cs"));
            Check(catalog.Contains("board with { RankRecommended = RankFloor(limits, board.RankLimit) }") && !catalog.Contains("row.UInt(0), row.Int(2));"), "the range's first number from the previous board's rank, column 2 no longer read");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("if (BeastTeams.RankLow(board, pick.Rank, pick.RankKnown))") && page.Contains("\"BeastUi.RankLow\", board.RankRecommended, board.RankLimit"), "beside the ten's rank");
            Check(page.Contains("if (BeastTeams.RankLow(board, member.Rank))") && page.Contains("\"BeastUi.TeamReadRankLow\""), "for the team taken");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                Check(text.Contains("\"RankLow\": \"") && text.Contains("\"TeamReadRankLow\": \"{0}"), language + ": both texts");
            }
        }),
    ];
}
