using System.Text.Json;

namespace TenshoAssistant.Beastmaster;

/// <summary>A beast taken, with the rank its attributes are read at (the board's limit synchronises higher ranks).</summary>
public sealed record BeastPick(uint Beast, int Rank, bool RankKnown);
/// <summary>One fight of a board: its index in the board's list, the 3 beasts to call, and how many enemies' weakness they hit.</summary>
public sealed record BattlePlan(int Battle, IReadOnlyList<BeastPick> Team, int WeaknessesHit, int Enemies);
/// <summary>The 10 beasts to bring for a board, and the 3 of each fight among them.</summary>
/// <param name="RanksAssumed">X-549 (0.1522): a beast of the team (not of every beast held) has its rank assumed, or the beasts held are not read.</param>
/// <param name="RanksUnknown">X-549: how many beasts of the team have their rank assumed (the board's limit).</param>
public sealed record BoardPlan(uint Board, IReadOnlyList<BeastPick> Team, IReadOnlyList<BattlePlan> Battles, bool RanksAssumed, int RanksUnknown = 0);
/// <summary>X-549 (0.1522): where the board the panel shows comes from: the labyrinth's territory, the game's board or team window, or the
/// list of the page (chosen by hand, when the game tells nothing).</summary>
public enum BoardSource { Labyrinth, Window, Chosen }

/// <summary>
/// X-109 (0.980, DEC-046, user request: « help to choose the 10 before entering, and the 3 of each fight »). Prepared out of combat only. The game gives no
/// formula of the fight, so the choice follows plain rules, said on the page, in this order:
/// 1) the team hits the weakness of as many enemies as possible (a beast's automatic attack of the enemy's weak type: slashing, piercing or blunt);
/// 2) attack: for each enemy, the better of Strength over its Physical resistance stars and Intelligence over its Magical resistance stars;
/// 3) defence: Physical resistance weighted by the enemy's Strength stars, Magical resistance by its Intelligence stars, plus Vitality.
/// The 10: each fight's best 3 among every beast held, then the beasts chosen in the most fights, then the best attack. Nothing is guessed about a fight's outcome.
/// </summary>
public static class BeastTeams
{
    /// <summary>X-366 (0.1373): the text of « weakness hit: n of m enemies », the noun agreed with m (« 0 of 1 enemy », « 0 sur 1 ennemi »).</summary>
    public static string HitKey(int enemies) => enemies <= 1 ? "BeastUi.TeamHitOne" : "BeastUi.TeamHit";

    public const int PerBattle = 3;

    public static (bool Weak, double Attack, double Guard) Against(BeastStats beast, uint autoRow, BoardEnemy enemy)
    {
        var stars = enemy.Stars ?? new BeastStats(1, 1, 1, 1, 1);
        static int Star(int value) => Math.Max(1, value);
        var attack = Math.Max(beast.Strength / (double)Star(stars.PhysicalResistance), beast.Intelligence / (double)Star(stars.MagicalResistance));
        var guard = (beast.PhysicalResistance * Star(stars.Strength) + beast.MagicalResistance * Star(stars.Intelligence)) / (double)(Star(stars.Strength) + Star(stars.Intelligence)) + beast.Vitality;
        return (autoRow != 0 && autoRow == enemy.WeaknessRow, attack, guard);
    }

    /// <summary>The best team of at most 3 among these beasts for one fight (every combination is compared, in the order of the rules).</summary>
    public static (IReadOnlyList<int> Picks, int Hit) Best(IReadOnlyList<(BeastStats Stats, uint AutoRow)> beasts, IReadOnlyList<BoardEnemy> enemies, int size = PerBattle)
    {
        var count = beasts.Count;
        if (count == 0 || enemies.Count == 0) return ([], 0);
        size = Math.Min(size, count);
        var scores = beasts.Select(beast => enemies.Select(enemy => Against(beast.Stats, beast.AutoRow, enemy)).ToArray()).ToArray();
        (int Covered, int Hits, double Attack, double Guard) Score(int[] team)
        {
            var covered = 0; var hits = 0; double attack = 0, guard = 0;
            for (var enemy = 0; enemy < enemies.Count; enemy++)
            {
                var any = false;
                foreach (var member in team) { var value = scores[member][enemy]; if (value.Weak) { hits++; any = true; } attack += value.Attack; guard += value.Guard; }
                if (any) covered++;
            }
            return (covered, hits, attack, guard);
        }
        int[]? best = null; (int, int, double, double) bestScore = default;
        foreach (var team in Combinations(count, size))
        {
            var score = Score(team);
            if (best is null || score.CompareTo(bestScore) > 0) { best = team; bestScore = score; }
        }
        return (best ?? [], bestScore.Item1);
    }

    private static IEnumerable<int[]> Combinations(int count, int size)
    {
        var index = Enumerable.Range(0, size).ToArray();
        while (true)
        {
            yield return (int[])index.Clone();
            var position = size - 1;
            while (position >= 0 && index[position] == count - size + position) position--;
            if (position < 0) yield break;
            index[position]++;
            for (var next = position + 1; next < size; next++) index[next] = index[next - 1] + 1;
        }
    }

    /// <summary>
    /// The plan of a board. held: the beasts held (null: not read, every beast is taken and the plan says so); ranks: the rank of each beast when read
    /// (otherwise the board's limit is assumed, and said). X-549 (0.1522): floors: the beasts whose rank was read at a board's limit (the game
    /// shows a higher rank brought down to it): their rank is at least that, known for a board whose limit is not above it.
    /// </summary>
    public static BoardPlan Plan(Board board, IReadOnlyList<Beast> beasts, IReadOnlySet<uint>? held, IReadOnlyDictionary<uint, int>? ranks, IReadOnlySet<uint>? floors = null)
    {
        var limit = Math.Max(1, board.RankLimit);
        var pool = beasts.Where(beast => (held is null || held.Contains(beast.Id)) && beast.Ranks.Count > 0).Select(beast =>
        {
            var known = ranks?.TryGetValue(beast.Id, out var rank) == true && (floors?.Contains(beast.Id) != true || ranks[beast.Id] >= limit);
            var used = Math.Min(known ? Math.Max(1, ranks![beast.Id]) : limit, limit);
            return (Beast: beast, Pick: new BeastPick(beast.Id, used, known), Stats: beast.At(used)!);
        }).ToArray();
        var fights = board.Battles.Select((battle, index) => (Index: index, battle.Enemies)).Where(fight => fight.Enemies.Count > 0).ToArray();
        IReadOnlyList<(BeastStats, uint)> Input(IEnumerable<int> members) => members.Select(member => (pool[member].Stats, pool[member].Beast.AutoRow)).ToArray();
        // The 10: the beasts each fight would call from everything held, most fights first, then the best total attack.
        var size = Math.Max(1, Math.Min(board.TeamSize > 0 ? board.TeamSize : 10, pool.Length));
        var chosenIn = new int[pool.Length];
        foreach (var fight in fights)
            foreach (var member in Best(Input(Enumerable.Range(0, pool.Length)), fight.Enemies).Picks) chosenIn[member]++;
        double Attack(int member) => fights.Sum(fight => fight.Enemies.Sum(enemy => Against(pool[member].Stats, pool[member].Beast.AutoRow, enemy).Attack));
        var team = Enumerable.Range(0, pool.Length).OrderByDescending(member => chosenIn[member]).ThenByDescending(Attack).ThenBy(member => pool[member].Beast.Id).Take(size).ToArray();
        var battles = fights.Select(fight =>
        {
            var (picks, hit) = Best(Input(team), fight.Enemies);
            return new BattlePlan(fight.Index, picks.Select(pick => pool[team[pick]].Pick).ToArray(), hit, fight.Enemies.Count);
        }).ToArray();
        var unknown = team.Count(member => !pool[member].Pick.RankKnown);
        return new BoardPlan(board.Id, team.Select(member => pool[member].Pick).ToArray(), battles, held is null || unknown > 0, unknown);
    }

    /// <summary>X-553 (0.1523): a weakness of a fight's enemies that none of its three beasts hits: the team's other beasts that would hit it, and
    /// the beasts held outside the team that would (all beasts when the beasts held are not read).</summary>
    public sealed record WeaknessMissed(uint Weakness, string Name, IReadOnlyList<uint> InTeam, IReadOnlyList<uint> Held);

    /// <summary>
    /// X-553 (0.1523, the user's report of 17/11: « the help does not seem to make good teams for weakness first »): each weakness of the fight's
    /// enemies that the three chosen do not hit, with who would: the team's other beasts first, then the beasts held outside the team. The game's
    /// weakness and a beast's automatic attack use the same rows (XBMElement): the board window's « Faiblesse » and the beast book's « Attaque auto. ».
    /// </summary>
    public static IReadOnlyList<WeaknessMissed> Missed(BoardBattle fight, IReadOnlyList<BeastPick> trio, IReadOnlyList<BeastPick> team, IReadOnlyList<Beast> beasts, IReadOnlySet<uint>? held)
    {
        var byId = beasts.GroupBy(beast => beast.Id).ToDictionary(group => group.Key, group => group.First());
        uint AutoOf(uint id) => byId.TryGetValue(id, out var beast) ? beast.AutoRow : 0;
        var hit = trio.Select(pick => AutoOf(pick.Beast)).Where(row => row != 0).ToHashSet();
        var inTeam = team.Select(pick => pick.Beast).ToHashSet();
        var missed = new List<WeaknessMissed>();
        foreach (var enemy in fight.Enemies.Where(enemy => enemy.WeaknessRow != 0 && !hit.Contains(enemy.WeaknessRow)).GroupBy(enemy => enemy.WeaknessRow).Select(group => group.First()))
        {
            var others = team.Where(pick => trio.All(chosen => chosen.Beast != pick.Beast) && AutoOf(pick.Beast) == enemy.WeaknessRow).Select(pick => pick.Beast).ToArray();
            var outside = beasts.Where(beast => !inTeam.Contains(beast.Id) && (held is null || held.Contains(beast.Id)) && beast.AutoRow == enemy.WeaknessRow).Select(beast => beast.Id).ToArray();
            missed.Add(new(enemy.WeaknessRow, enemy.Weakness, others, outside));
        }
        return missed;
    }

    /// <summary>
    /// X-549 (0.1522, the user's report of 17/11: the panel showed a board chosen by hand while the game showed the 1st): the board the game is
    /// on. In the labyrinth, its territory's board (one territory per board: ContentFinderCondition 1088 to 1092, territories 1339 to 1343,
    /// docs/evidence/0.1522-beast-board-read.md); before entering, the board of the game's board or team window (their agent's ContentId: an
    /// XBMContent row or its ContentFinderCondition row, the two ranges apart); else none, and the page's list is used.
    /// </summary>
    public static (Board? Board, BoardSource Source) Current(IReadOnlyList<Board> boards, uint territoryBoard, uint content)
    {
        if (territoryBoard != 0 && boards.FirstOrDefault(board => board.Id == territoryBoard) is { } here) return (here, BoardSource.Labyrinth);
        if (content != 0 && boards.FirstOrDefault(board => board.Id == content || board.Content == content) is { } shown) return (shown, BoardSource.Window);
        return (null, BoardSource.Chosen);
    }

    /// <summary>
    /// X-549 (0.1522): ranks read in the game's team window merged with those known. The window shows a beast's rank brought down to the board's
    /// limit (the user's beasts at rank 7 showed « 5 » on the 1st board, limit 5; their result window said 7): a rank read at the limit is a floor,
    /// never put over a higher rank known, and kept as « at least » (floors); a rank read below the limit is the beast's own. limit 0: not known,
    /// every rank read is taken as it is.
    /// </summary>
    public static (Dictionary<uint, int> Ranks, HashSet<uint> Floors) MergeRanks(IReadOnlyDictionary<uint, int>? known, IReadOnlySet<uint>? floors, IReadOnlyDictionary<uint, int> read, int limit)
    {
        var ranks = new Dictionary<uint, int>(known ?? new Dictionary<uint, int>());
        var atLeast = new HashSet<uint>(floors ?? new HashSet<uint>());
        foreach (var (beast, rank) in read)
        {
            if (limit > 0 && rank >= limit)
            {
                if (ranks.TryGetValue(beast, out var before) && before >= rank) continue;
                ranks[beast] = rank; atLeast.Add(beast);
            }
            else { ranks[beast] = rank; atLeast.Remove(beast); }
        }
        return (ranks, atLeast);
    }

    /// <summary>The plans kept on disk (X-109: « cache disque »), one per board, with what they were computed from.</summary>
    /// <summary>X-135 (0.1163): Result = the last board's result read in the game's result window (null before the first one; older files have none).</summary>
    /// <summary>X-549 (0.1522): Floors = the beasts whose rank is only known to be at least the one kept (read at a board's limit).</summary>
    public sealed record SavedPlans(Dictionary<uint, BoardPlan> Plans, Dictionary<uint, string> Keys, Dictionary<uint, int>? Ranks = null, BoardResult? Result = null, HashSet<uint>? Floors = null);

    /// <summary>
    /// X-109 (0.981): the ranks shown in the game's team window (XBMPetParty: each beast's attributes, then its name, then its rank; docs/evidence/0.980-beast-teams.md).
    /// A name followed by a number from 1 to the highest rank is that beast's rank; anything else is left out.
    /// </summary>
    public static Dictionary<uint, int> ParseRanks(IReadOnlyList<string> texts, IReadOnlyList<Beast> beasts)
    {
        var found = new Dictionary<uint, int>();
        for (var index = 0; index + 1 < texts.Count; index++)
        {
            var beast = beasts.FirstOrDefault(row => row.Name.Length > 0 && string.Equals(row.Name, texts[index], StringComparison.CurrentCultureIgnoreCase));
            if (beast is null || !int.TryParse(texts[index + 1], out var rank) || rank < 1 || rank > Math.Max(1, beast.Ranks.Count)) continue;
            found[beast.Id] = rank;
        }
        return found;
    }
    public static string Key(IReadOnlySet<uint>? held, IReadOnlyDictionary<uint, int>? ranks, IReadOnlySet<uint>? floors = null)
        => (held is null ? "?" : string.Join(",", held.Order())) + "|" + string.Join(",", (ranks ?? new Dictionary<uint, int>()).OrderBy(pair => pair.Key)
            .Select(pair => $"{pair.Key}:{pair.Value}{(floors?.Contains(pair.Key) == true ? "+" : "")}"));
    public static string Write(SavedPlans plans) => JsonSerializer.Serialize(plans);
    /// <summary>
    /// X-284 M1 (review of block 0.1278, R07): the plans saved off the game's thread. The text is made here, from the plans as they are now
    /// (the caller's thread keeps changing them); the file is written by <paramref name="writer"/>, one write after the other, so a file held
    /// an instant by another program (AtomicFile tries again for up to about 750 ms) never makes the game wait while it draws.
    /// </summary>
    public static void SaveLater(BackgroundWriter writer, string file, SavedPlans plans)
    {
        var text = Write(plans);
        writer.Enqueue("Beast team plans could not be saved.", () => AtomicFile.Write(file, text));
    }
    public static SavedPlans? Read(string text)
    {
        try { return JsonSerializer.Deserialize<SavedPlans>(text) is { Plans: not null, Keys: not null } plans ? plans : null; }
        catch (JsonException) { return null; }
    }
}
