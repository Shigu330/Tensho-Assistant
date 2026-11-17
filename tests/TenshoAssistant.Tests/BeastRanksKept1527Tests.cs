using TenshoAssistant.Beastmaster;

// 0.1527 (X-561, review of 0.1526, I1): a rank read in the game's team window never lowers a rank known higher, whatever the board's limit is
// (not known: 0, or a lower one), so the saved ranks never lose the 7 the result window gave; the reader merges after the board is read.
internal static class BeastRanksKept1527Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static Beast[] Beasts()
    {
        BeastStats[] Ranks() => Enumerable.Range(1, 25).Select(_ => new BeastStats(10, 10, 10, 10, 10)).ToArray();
        return [new Beast(1, "Cu sith", 242001, "", "", "", "", Ranks()), new Beast(2, "Opo-opo", 242002, "", "", "", "", Ranks()), new Beast(3, "Puk", 242003, "", "", "", "", Ranks())];
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast ranks kept 1527: board not known (limit 0): the window's « 5 » does not bring the result's 7 down, saved again and again", () =>
        {
            // The user's case: Cu sith rank 7 from the board's result, the team window shows « Cu sith;5 » before the board is known.
            var read = BeastTeams.ParseRanks("Exp;608/608;PV;Cu sith;5;Exp;667/667;PV;Opo-opo;5;Exp;511/511;PV;Puk;3".Split(';'), Beasts());
            var known = new Dictionary<uint, int> { [1] = 7, [3] = 2 };
            var (ranks, floors) = BeastTeams.MergeRanks(known, new HashSet<uint>(), read, 0);
            Check(ranks[1] == 7, $"the 7 stays (was {ranks[1]})");
            Check(ranks[2] == 5 && ranks[3] == 3, "a beast not known, or known lower, takes the rank read");
            // Read again many times (the reader reads each 250 ms): nothing moves, so nothing more is saved.
            for (var turn = 0; turn < 5; turn++) (ranks, floors) = BeastTeams.MergeRanks(ranks, floors, read, 0);
            Check(ranks[1] == 7 && ranks[2] == 5 && ranks[3] == 3, "the same after five more readings");
            // Then the board becomes known (limit 5): the 7 is still kept, never a floor of 5.
            var (atLimit, atLimitFloors) = BeastTeams.MergeRanks(ranks, floors, read, 5);
            Check(atLimit[1] == 7 && !atLimitFloors.Contains(1) && atLimit[2] == 5 && atLimit[3] == 3, "board known later: 7 kept, the others as read");
        }),
        ("Beast ranks kept 1527: a known floor stays a floor when the same rank is read with no limit; below a limit read lower, never lowered", () =>
        {
            var (ranks, floors) = BeastTeams.MergeRanks(new Dictionary<uint, int> { [2] = 5 }, new HashSet<uint> { 2 }, new Dictionary<uint, int> { [2] = 5 }, 0);
            Check(ranks[2] == 5 && floors.Contains(2), "« at least 5 » is not turned into « exactly 5 » by a reading whose limit is not known");
            // A window number taken for a higher board (limit 10) while the true limit is 5: a 7 known is not lowered to the 5 read.
            var (higher, higherFloors) = BeastTeams.MergeRanks(new Dictionary<uint, int> { [1] = 7 }, null, new Dictionary<uint, int> { [1] = 5 }, 10);
            Check(higher[1] == 7 && !higherFloors.Contains(1), "a rank read below the limit but lower than known: the known rank stays");
            var (up, upFloors) = BeastTeams.MergeRanks(new Dictionary<uint, int> { [1] = 7 }, new HashSet<uint> { 1 }, new Dictionary<uint, int> { [1] = 9 }, 10);
            Check(up[1] == 9 && !upFloors.Contains(1), "a higher rank read below the limit: the beast's own");
        }),
        ("Beast ranks kept 1527: the reader merges the ranks after it holds the board; a window's board is said as to be confirmed", () =>
        {
            var reader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Beastmaster/BeastBoardReader.Dalamud.cs"));
            var stateAt = reader.IndexOf("state = new(true, boardUnit->X", StringComparison.Ordinal);
            var mergeAt = reader.IndexOf("if (ranks is { Count: > 0 }) ranksRead(ranks);", StringComparison.Ordinal);
            Check(stateAt > 0 && mergeAt > stateAt, "ranksRead comes after the state of this turn (its board)");
            Check(!reader.Contains("if (ranks.Count > 0) ranksRead(ranks);"), "no merge before the state");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("source == BoardSource.Window ? \"BeastUi.BoardReadWindow\" : \"BeastUi.BoardRead\""), "the window's board is said as to be confirmed");
        }),
    ];
}
