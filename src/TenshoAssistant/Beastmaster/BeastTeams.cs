using System.Text.Json;

namespace TenshoAssistant.Beastmaster;

/// <summary>A beast taken, with the rank its attributes are read at (the board's limit synchronises higher ranks).</summary>
public sealed record BeastPick(uint Beast, int Rank, bool RankKnown);
/// <summary>One fight of a board: its index in the board's list, the 3 beasts to call, and how many enemies' weakness they hit.</summary>
public sealed record BattlePlan(int Battle, IReadOnlyList<BeastPick> Team, int WeaknessesHit, int Enemies);
/// <summary>The 10 beasts to bring for a board, and the 3 of each fight among them.</summary>
public sealed record BoardPlan(uint Board, IReadOnlyList<BeastPick> Team, IReadOnlyList<BattlePlan> Battles, bool RanksAssumed);

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
    /// (otherwise the board's limit is assumed, and said).
    /// </summary>
    public static BoardPlan Plan(Board board, IReadOnlyList<Beast> beasts, IReadOnlySet<uint>? held, IReadOnlyDictionary<uint, int>? ranks)
    {
        var limit = Math.Max(1, board.RankLimit);
        var pool = beasts.Where(beast => (held is null || held.Contains(beast.Id)) && beast.Ranks.Count > 0).Select(beast =>
        {
            var known = ranks?.TryGetValue(beast.Id, out var rank) == true;
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
        return new BoardPlan(board.Id, team.Select(member => pool[member].Pick).ToArray(), battles, held is null || pool.Any(entry => !entry.Pick.RankKnown));
    }

    /// <summary>The plans kept on disk (X-109: « cache disque »), one per board, with what they were computed from.</summary>
    public sealed record SavedPlans(Dictionary<uint, BoardPlan> Plans, Dictionary<uint, string> Keys);
    public static string Key(IReadOnlySet<uint>? held, IReadOnlyDictionary<uint, int>? ranks)
        => string.Join(",", (held ?? new HashSet<uint>()).Order()) + "|" + string.Join(",", (ranks ?? new Dictionary<uint, int>()).OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}:{pair.Value}"));
    public static string Write(SavedPlans plans) => JsonSerializer.Serialize(plans);
    public static SavedPlans? Read(string text)
    {
        try { return JsonSerializer.Deserialize<SavedPlans>(text) is { Plans: not null, Keys: not null } plans ? plans : null; }
        catch (JsonException) { return null; }
    }
}
