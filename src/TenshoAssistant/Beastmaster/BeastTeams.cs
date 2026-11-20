using System.Text.Json;

namespace TenshoAssistant.Beastmaster;

/// <summary>A beast taken, with the rank its attributes are read at (the board's limit synchronises higher ranks).</summary>
public sealed record BeastPick(uint Beast, int Rank, bool RankKnown);
/// <summary>One fight of a board: its index in the board's list, the 3 beasts to call, and how many enemies' weakness they hit.</summary>
public sealed record BattlePlan(int Battle, IReadOnlyList<BeastPick> Team, int WeaknessesHit, int Enemies);
/// <summary>The 10 beasts to bring for a board, and the 3 of each fight among them.</summary>
/// <param name="RanksAssumed">X-549 (0.1522): a beast of the team (not of every beast held) has its rank assumed, or the beasts held are not read.</param>
/// <param name="RanksUnknown">X-549: how many beasts of the team have their rank assumed (the board's limit).</param>
/// <param name="Left">X-575 (0.1543): the weaknesses of the board that no fight's three hit, and why (null: a plan kept before 0.1543).</param>
public sealed record BoardPlan(uint Board, IReadOnlyList<BeastPick> Team, IReadOnlyList<BattlePlan> Battles, bool RanksAssumed, int RanksUnknown = 0, IReadOnlyList<WeaknessLeft>? Left = null);
/// <summary>X-575 (0.1543): why a weakness of the board is left out: no beast held hits it, or the team's places all went to weaknesses of more enemies.</summary>
public enum LeftReason { NoneHeld, NoRoom }
/// <summary>X-575 (0.1543): a weakness of the board (its XBMElement row and name) that the plan leaves out, with the reason.</summary>
public sealed record WeaknessLeft(uint Weakness, string Name, LeftReason Reason);
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
/// X-575 (0.1543, the user's report of 17/11: « with an empty team it should take every weakness but does not »): the 10 first take, one by
/// one, the beast held that adds the most enemies hit by their weakness over the whole board (each fight's three at most); only then the
/// rule above fills the places left.
/// </summary>
public static class BeastTeams
{
    /// <summary>X-366 (0.1373): the text of « weakness hit: n of m enemies », the noun agreed with m (« 0 of 1 enemy », « 0 sur 1 ennemi »).</summary>
    public static string HitKey(int enemies) => enemies <= 1 ? "BeastUi.TeamHitOne" : "BeastUi.TeamHit";

    public const int PerBattle = 3;

    /// <summary>
    /// X-579 (0.1547): an enemy's strength, the sum of its five stars of the board window (XBMBattleDetail columns 1 to 5); 0 when not read.
    /// The sheets mark no enemy as summoned nor as the fight's leader (docs/evidence/0.1547-beast-strongest.md): the stars decide.
    /// </summary>
    public static int Power(BoardEnemy enemy) => enemy.Stars is { } stars ? stars.Strength + stars.Intelligence + stars.PhysicalResistance + stars.MagicalResistance + stars.Vitality : 0;

    /// <summary>X-579 (0.1547): the fight's strongest enemy (the first in the sheet's order on a tie), or null when its enemies are all as strong.</summary>
    public static BoardEnemy? Strongest(BoardBattle fight) => fight.Enemies.Select(Power).Distinct().Count() > 1 ? fight.Enemies.OrderByDescending(Power).First() : null;

    /// <summary>
    /// X-579 (0.1547, the user's request of 18/11: « the weaknesses must be of the strongest monster first »): the weight of each enemy of a
    /// fight, so that hitting a stronger enemy's weakness always counts more than hitting any number of weaker ones (the enemies of a same
    /// strength count the same; all as strong: 1 each, the count of before).
    /// </summary>
    public static long[] Weights(IReadOnlyList<BoardEnemy> enemies)
    {
        var levels = enemies.Select(Power).Distinct().OrderDescending().ToArray();
        var radix = enemies.Count + 1L;
        return enemies.Select(enemy =>
        {
            long weight = 1;
            for (var level = Array.IndexOf(levels, Power(enemy)) + 1; level < levels.Length; level++) weight *= radix;
            return weight;
        }).ToArray();
    }

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
        // X-579 (0.1547): the enemies hit count by their weight (the strongest first), then the beasts' hits, attack and defence.
        var weights = Weights(enemies);
        (long Covered, int Hits, double Attack, double Guard) Score(int[] team)
        {
            long covered = 0; var hits = 0; double attack = 0, guard = 0;
            for (var enemy = 0; enemy < enemies.Count; enemy++)
            {
                var any = false;
                foreach (var member in team) { var value = scores[member][enemy]; if (value.Weak) { hits++; any = true; } attack += value.Attack; guard += value.Guard; }
                if (any) covered += weights[enemy];
            }
            return (covered, hits, attack, guard);
        }
        int[]? best = null; (long, int, double, double) bestScore = default;
        foreach (var team in Combinations(count, size))
        {
            var score = Score(team);
            if (best is null || score.CompareTo(bestScore) > 0) { best = team; bestScore = score; }
        }
        var chosen = best ?? [];
        return (CallOrder(chosen, scores, weights), Enumerable.Range(0, enemies.Count).Count(enemy => chosen.Any(member => scores[member][enemy].Weak)));
    }

    /// <summary>
    /// X-575 part 4 (0.1544, the user's request of 17/11 around 23:25: « the best beasts for the fights, knowing they come out one at a time:
    /// weakness first, then the rest, stats and others »): the three in calling order, the best first: the enemies whose weakness it hits, then
    /// its attack against their resistances, then its defence (DescriptionString 1751: Force and Intelligence the damage dealt, the resistances
    /// the damage taken, Vitalité the HP). The rank counts only through the attributes it gives. The enemies' vulnerabilities to effects are
    /// not used: their meaning is not established (docs/evidence/0.1523-beast-weakness.md). X-579 (0.1547): the enemies hit count by their
    /// weight (<see cref="Weights"/>): the beast that hits the strongest enemy's weakness is called first.
    /// </summary>
    private static int[] CallOrder(int[] picks, (bool Weak, double Attack, double Guard)[][] scores, long[] weights)
        => picks.OrderByDescending(member => scores[member].Select((value, enemy) => value.Weak ? weights[enemy] : 0L).Sum()).ThenByDescending(member => scores[member].Sum(value => value.Attack))
            .ThenByDescending(member => scores[member].Sum(value => value.Guard)).ThenBy(member => member).ToArray();

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
        var size = Math.Max(1, Math.Min(board.TeamSize > 0 ? board.TeamSize : 10, pool.Length));
        double Attack(int member) => fights.Sum(fight => fight.Enemies.Sum(enemy => Against(pool[member].Stats, pool[member].Beast.AutoRow, enemy).Attack));
        // X-575 (0.1543): the weaknesses first, over the whole board: each pick is the beast that adds the most enemies hit (a fight's three at
        // most), then the one strongest against the enemies it hits, then the best total attack.
        // X-585 M1 (review of 0.1550): as the three of a fight (X-579), the 10 weigh each fight's strongest enemy first: a fight's three
        // weaknesses counted are those of the most weight (Weights), and a pick first adds the strength (Power) of the fights' strongest enemies
        // it hits (a stronger guardian over a fight of weak enemies), then enemies.
        // Before, the count of enemies alone could leave the guardian's weakness (one enemy) for that of two weaker ones when places lacked.
        var weak = fights.Select(fight => Ranked(fight.Enemies)).ToArray();
        var team = new List<int>();
        var rows = new HashSet<uint>();
        (int Strongest, int Enemies) Covered(HashSet<uint> present)
        {
            var (strongest, enemies) = (0, 0);
            foreach (var fight in weak)
            {
                var counted = fight.Rows.Where(row => present.Contains(row.Row)).Take(PerBattle).ToArray();
                enemies += counted.Sum(row => row.Count);
                if (counted.Any(row => fight.Top.Contains(row.Row))) strongest += Math.Max(1, fight.Power);
            }
            return (strongest, enemies);
        }
        static bool Adds((int Strongest, int Enemies) gain) => gain.Strongest > 0 || gain.Strongest == 0 && gain.Enemies > 0;
        double Strength(int member) => fights.Sum(fight => fight.Enemies.Where(enemy => enemy.WeaknessRow != 0 && enemy.WeaknessRow == pool[member].Beast.AutoRow)
            .Sum(enemy => Against(pool[member].Stats, pool[member].Beast.AutoRow, enemy).Attack));
        while (team.Count < size)
        {
            var now = Covered(rows);
            var next = Enumerable.Range(0, pool.Length).Where(member => !team.Contains(member) && pool[member].Beast.AutoRow != 0 && !rows.Contains(pool[member].Beast.AutoRow))
                .Select(member => (Member: member, Gain: Covered([.. rows, pool[member].Beast.AutoRow]) is var after ? (after.Strongest - now.Strongest, after.Enemies - now.Enemies) : default))
                .Where(pick => Adds(pick.Gain))
                .OrderByDescending(pick => pick.Gain.Item1).ThenByDescending(pick => pick.Gain.Item2).ThenByDescending(pick => Strength(pick.Member)).ThenByDescending(pick => Attack(pick.Member))
                .ThenBy(pick => pool[pick.Member].Beast.Id).Select(pick => (pick.Member, Found: true)).FirstOrDefault();
            if (!next.Found) break;
            team.Add(next.Member); rows.Add(pool[next.Member].Beast.AutoRow);
        }
        // The places left: the beasts each fight would call from everything held, most fights first, then the best total attack.
        var chosenIn = new int[pool.Length];
        foreach (var fight in fights)
            foreach (var member in Best(Input(Enumerable.Range(0, pool.Length)), fight.Enemies).Picks) chosenIn[member]++;
        team.AddRange(Enumerable.Range(0, pool.Length).Where(member => !team.Contains(member)).OrderByDescending(member => chosenIn[member]).ThenByDescending(Attack)
            .ThenBy(member => pool[member].Beast.Id).Take(size - team.Count));
        var battles = fights.Select(fight =>
        {
            var (picks, hit) = Best(Input(team), fight.Enemies);
            return new BattlePlan(fight.Index, picks.Select(pick => pool[team[pick]].Pick).ToArray(), hit, fight.Enemies.Count);
        }).ToArray();
        // X-575: a weakness no fight's three hits is said with its reason: no beast held hits it, or the places went to weaknesses of more enemies.
        var hitRows = team.Select(member => pool[member].Beast.AutoRow).ToHashSet();
        var heldRows = pool.Select(member => member.Beast.AutoRow).ToHashSet();
        var left = fights.SelectMany(fight => fight.Enemies).Where(enemy => enemy.WeaknessRow != 0 && !hitRows.Contains(enemy.WeaknessRow)).GroupBy(enemy => enemy.WeaknessRow)
            .Select(group => new WeaknessLeft(group.Key, group.First().Weakness, heldRows.Contains(group.Key) ? LeftReason.NoRoom : LeftReason.NoneHeld)).OrderBy(row => row.Weakness).ToArray();
        var unknown = team.Count(member => !pool[member].Pick.RankKnown);
        return new BoardPlan(board.Id, team.Select(member => pool[member].Pick).ToArray(), battles, held is null || unknown > 0, unknown, left);
    }

    /// <summary>
    /// X-575 part 5 (0.1545, the user's request of 17/11 around 23:30: « the rank is not a criterion, but one can say the rank is low for the
    /// board »): a rank read below the board's recommended range (« Rang de bête recommandé : X à Y », X = <see cref="Board.RankRecommended"/>).
    /// Only a warning: the choice never uses it. A rank not read (assumed) or a range not known is never said low.
    /// </summary>
    public static bool RankLow(Board board, int rank, bool known = true) => known && board.RankRecommended > 0 && rank > 0 && rank < board.RankRecommended;

    /// <summary>
    /// X-585 M2 (review of 0.1550, R06): the recommended ranges seen in the game's board window (« 1 à 5 » on the 1st board, « 6 à 10 » on the
    /// 2nd: docs/evidence/0.1546-beast-rank-range.md). The other boards' range follows the rule drawn from these two, the expert boards
    /// (team of 12 and 15) perhaps not: their range is said « to be confirmed in game ».
    /// </summary>
    public static bool RankRangeSeen(Board board) => (board.RankRecommended, board.RankLimit) is (1, 5) or (6, 10);

    /// <summary>X-585 M2: the text key of a low rank, with « to be confirmed » when the board's range was not seen in game.</summary>
    public static string RankLowKey(Board board, bool team) => (team ? "BeastUi.TeamReadRankLow" : "BeastUi.RankLow") + (RankRangeSeen(board) ? "" : "Unsure");

    /// <summary>
    /// X-585 M1 (review of 0.1550): a fight's weaknesses for the 10: each row with how many enemies it hits, the rows of most weight first
    /// (<see cref="Weights"/>: the strongest enemy first, then the count), and the rows of its strongest enemies (a lone enemy, or enemies all
    /// as strong, are its strongest), and that strongest enemy's strength.
    /// </summary>
    public static (IReadOnlyList<(uint Row, int Count)> Rows, IReadOnlySet<uint> Top, int Power) Ranked(IReadOnlyList<BoardEnemy> enemies)
    {
        var weights = Weights(enemies);
        var top = enemies.Count == 0 ? 0 : enemies.Max(Power);
        var rows = enemies.Select((enemy, index) => (enemy.WeaknessRow, Weight: weights[index])).Where(pair => pair.WeaknessRow != 0).GroupBy(pair => pair.WeaknessRow)
            .OrderByDescending(group => group.Sum(pair => pair.Weight)).ThenBy(group => group.Key).Select(group => (group.Key, group.Count())).ToArray();
        return (rows, enemies.Where(enemy => enemy.WeaknessRow != 0 && Power(enemy) == top).Select(enemy => enemy.WeaknessRow).ToHashSet(), top);
    }

    /// <summary>X-575 (0.1543): the version of the rules that make a plan. A plan kept on disk is made again when it changes.
    /// 3 (0.1544): each fight's three in calling order. 4 (0.1547, X-579): the strongest enemy's weakness first.
    /// 5 (0.1558, X-585 M1): the 10 too weigh each fight's strongest enemy first.</summary>
    public const int PlanVersion = 5;

    /// <summary>
    /// X-575 (0.1543, the user's report of 17/11: trios that hit no fire nor water although the team had both): a plan kept on disk was made
    /// before 0.1523 (magical beasts hit nothing then) and its key held only the beasts held and their ranks, so it was never made again.
    /// The mark of the rules and of the game's data the plan comes from (each beast's attack and attributes, each enemy's weakness and stars,
    /// each board's size and limit), part of the key: another version of the rules or of the game's sheets makes the plans again.
    /// </summary>
    public static string Mark(BeastCatalog catalog)
    {
        var hash = 2166136261u;
        void Add(long value) { unchecked { hash = (hash ^ (uint)value) * 16777619u; hash = (hash ^ (uint)(value >> 32)) * 16777619u; } }
        void Stats(BeastStats? stats) { if (stats is null) { Add(-1); return; } Add(stats.Strength); Add(stats.Intelligence); Add(stats.PhysicalResistance); Add(stats.MagicalResistance); Add(stats.Vitality); }
        foreach (var beast in catalog.Beasts) { Add(beast.Id); Add(beast.AutoRow); Add(beast.Ranks.Count); foreach (var rank in beast.Ranks) Stats(rank); }
        foreach (var board in catalog.Boards)
        {
            Add(board.Id); Add(board.TeamSize); Add(board.RankLimit); Add(board.Battles.Count);
            foreach (var battle in board.Battles) { Add(battle.Enemies.Count); foreach (var enemy in battle.Enemies) { Add(enemy.WeaknessRow); Stats(enemy.Stars); } }
        }
        return $"v{PlanVersion}:{hash:x8}";
    }

    /// <summary>X-550 (0.1526): a beast of the team chosen in the game's team window: its rank shown there, and its HP (-1 and 0: not read).</summary>
    public sealed record TeamMember(uint Beast, int Rank, int Hp, int MaxHp)
    {
        /// <summary>Down: its HP read at 0.</summary>
        public bool Down => MaxHp > 0 && Hp <= 0;
        /// <summary>Low: alive with less than <see cref="LowPercent"/> % of its HP.</summary>
        public bool Low => MaxHp > 0 && Hp > 0 && Hp * 100 < MaxHp * LowPercent;
    }
    public const int LowPercent = 30;

    /// <summary>
    /// X-550 (0.1526, the user's request of 17/11: « no team computed from the beasts I chose if I do not want to listen to it »; « the 3 to call
    /// for the current team »; « see the beast's HP and whether it is dead », read at the end of each fight): the team the game's team window
    /// shows (XBMPetParty, opened again after each fight): each beast's name, then its rank; its HP « a/b » two texts before the name
    /// (« 608/608 ; PV ; Cu sith ; 5 », the window of 17/11 10:09). Each beast once, in the window's order; nothing else is guessed.
    /// </summary>
    public static IReadOnlyList<TeamMember> ParseTeam(IReadOnlyList<string> texts, IReadOnlyList<Beast> beasts)
    {
        var team = new List<TeamMember>();
        for (var index = 0; index + 1 < texts.Count; index++)
        {
            var beast = beasts.FirstOrDefault(row => row.Name.Length > 0 && string.Equals(row.Name, texts[index], StringComparison.CurrentCultureIgnoreCase));
            if (beast is null || team.Any(member => member.Beast == beast.Id) || !int.TryParse(texts[index + 1], out var rank) || rank < 1 || rank > Math.Max(1, beast.Ranks.Count)) continue;
            var (hp, max) = (-1, 0);
            if (index >= 2 && texts[index - 2].Split('/') is [var left, var right] && int.TryParse(left, out var now) && int.TryParse(right, out var top) && top > 0 && now >= 0 && now <= top)
                (hp, max) = (now, top);
            team.Add(new TeamMember(beast.Id, rank, hp, max));
        }
        return team;
    }

    /// <summary>X-550 (0.1526): one fight's three beasts taken among the team read (beasts down left out), with the weaknesses they hit, and
    /// against the advised plan: the advised beasts missing (not taken, or down) and those that take their place.</summary>
    public sealed record TeamFight(int Battle, IReadOnlyList<uint> Trio, int Hit, int Enemies, IReadOnlyList<uint> Lost, IReadOnlyList<uint> Instead);

    /// <summary>
    /// X-550 (0.1526): the three of each fight among the beasts the player took (the team window), by the same rules as the plan (weaknesses
    /// first, then attack, then defence; ranks at most the board's limit); the advised plan's beasts that are not there (or down) are said with
    /// the beasts that replace them. Nothing is chosen nor sent to the game (R03.3).
    /// </summary>
    public static IReadOnlyList<TeamFight> FromTeam(Board board, BoardPlan? advised, IReadOnlyList<TeamMember> team, IReadOnlyList<Beast> beasts)
    {
        var limit = Math.Max(1, board.RankLimit);
        var byId = beasts.GroupBy(beast => beast.Id).ToDictionary(group => group.Key, group => group.First());
        var alive = team.Where(member => !member.Down && byId.ContainsKey(member.Beast) && byId[member.Beast].Ranks.Count > 0).ToArray();
        var input = alive.Select(member => (byId[member.Beast].At(Math.Min(member.Rank, limit))!, byId[member.Beast].AutoRow)).ToArray();
        var fights = new List<TeamFight>();
        for (var index = 0; index < board.Battles.Count; index++)
        {
            var enemies = board.Battles[index].Enemies;
            if (enemies.Count == 0) continue;
            var (picks, hit) = Best(input, enemies);
            var trio = picks.Select(pick => alive[pick].Beast).ToArray();
            var wanted = advised?.Battles.FirstOrDefault(battle => battle.Battle == index)?.Team.Select(pick => pick.Beast).ToArray() ?? [];
            var lost = wanted.Where(beast => alive.All(member => member.Beast != beast)).ToArray();
            var instead = lost.Length > 0 ? trio.Where(beast => !wanted.Contains(beast)).ToArray() : [];
            fights.Add(new TeamFight(index, trio, hit, enemies.Count, lost, instead));
        }
        return fights;
    }

    /// <summary>X-562 M2 (review of 0.1526): how many beasts of the team have their rank assumed; a plan kept before 0.1522 has no count (0)
    /// although its beasts say it: they are counted then.</summary>
    public static int RanksUnknownOf(BoardPlan plan) => plan.RanksUnknown > 0 ? plan.RanksUnknown : plan.Team.Count(pick => !pick.RankKnown);

    /// <summary>
    /// X-486 (0.1529, the user's request of 16/11): the board's fights the game's board window shows (their XBMBattleDetail rows, read in
    /// AgentXBMStageDetailList's entries), in the board's order. None when nothing is read, or when the window shows every fight of the board
    /// (then no fight is its own: the whole board is shown as before).
    /// </summary>
    public static IReadOnlyList<int> FightsShown(Board board, IReadOnlyCollection<uint>? shown)
    {
        if (shown is not { Count: > 0 } || board.Battles.Count == 0) return [];
        var set = shown as IReadOnlySet<uint> ?? shown.ToHashSet();
        var found = Enumerable.Range(0, board.Battles.Count).Where(index => board.Battles[index].Detail != 0 && set.Contains(board.Battles[index].Detail)).ToArray();
        return found.Length == board.Battles.Count ? [] : found;
    }

    /// <summary>X-486 (0.1529): why a beast is called for a fight: the names of the enemies whose weakness its automatic attack hits (empty: none,
    /// chosen for its attack and defence).</summary>
    /// <remarks>X-579 (0.1547): the strongest enemy first.</remarks>
    public static IReadOnlyList<string> Hits(BoardBattle fight, Beast? beast)
        => beast is null || beast.AutoRow == 0 ? [] : fight.Enemies.Where(enemy => enemy.WeaknessRow == beast.AutoRow).OrderByDescending(Power).Select(enemy => enemy.Name).Distinct().ToArray();

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
    /// a rank read is taken as it is.
    /// X-561 (review of 0.1526, I1): a beast's rank never goes down in the game, so a rank read never lowers a rank known higher, whatever the
    /// limit (limit 0 when the board is not known, or a window's limit lower than the beast's rank): the known rank and its « at least » stay.
    /// </summary>
    /// <summary>
    /// X-580 (0.1549, the user's request of 18/11: « in the order of the bestiary's numbers, to make selecting easier »): the beasts to bring
    /// in the order of the game's bestiary: its number is the beast's XBMPet row (« N°1 » Cu sith, row 1; « N°3 » Mouton, row 3;
    /// docs/evidence/0.1548-beast-book.md). Each fight's three keep their calling order.
    /// </summary>
    public static IReadOnlyList<BeastPick> BookOrder(IEnumerable<BeastPick> team) => team.OrderBy(pick => pick.Beast).ToArray();
    /// <summary>X-585 M7 (review of 0.1550): the team read and the beasts named in a line (missed weaknesses, lost beasts) in the bestiary's order too.</summary>
    public static IReadOnlyList<uint> BookOrder(IEnumerable<uint> beasts) => beasts.Order().ToArray();
    public static IReadOnlyList<TeamMember> BookOrder(IEnumerable<TeamMember> team) => team.OrderBy(member => member.Beast).ToArray();

    /// <summary>X-578 (0.1548): the limit to give <see cref="MergeRanks"/> for a rank read in the bestiary: the beast's own, never a floor.</summary>
    public const int Exact = int.MaxValue;

    public static (Dictionary<uint, int> Ranks, HashSet<uint> Floors) MergeRanks(IReadOnlyDictionary<uint, int>? known, IReadOnlySet<uint>? floors, IReadOnlyDictionary<uint, int> read, int limit)
    {
        var ranks = new Dictionary<uint, int>(known ?? new Dictionary<uint, int>());
        var atLeast = new HashSet<uint>(floors ?? new HashSet<uint>());
        foreach (var (beast, rank) in read)
        {
            var had = ranks.TryGetValue(beast, out var before);
            if (had && before > rank) continue;
            if (limit > 0 && rank >= limit)
            {
                if (had && before >= rank) continue;
                ranks[beast] = rank; atLeast.Add(beast);
            }
            else if (limit == 0 && had && before == rank) continue;
            else { ranks[beast] = rank; atLeast.Remove(beast); }
        }
        return (ranks, atLeast);
    }

    /// <summary>The plans kept on disk (X-109: « cache disque »), one per board, with what they were computed from.</summary>
    /// <summary>X-135 (0.1163): Result = the last board's result read in the game's result window (null before the first one; older files have none).</summary>
    /// <summary>X-549 (0.1522): Floors = the beasts whose rank is only known to be at least the one kept (read at a board's limit).</summary>
    /// <summary>X-578 (0.1548): Book = each beast's page last read in the game's bestiary (null: none read, or a file of before).</summary>
    public sealed record SavedPlans(Dictionary<uint, BoardPlan> Plans, Dictionary<uint, string> Keys, Dictionary<uint, int>? Ranks = null, BoardResult? Result = null, HashSet<uint>? Floors = null,
        Dictionary<uint, BookRead>? Book = null);

    /// <summary>
    /// X-578 (0.1548): a beast's page of the game's bestiary (XBMMonsterBookDetail): its rank (« Rang 5 (8) »: 8 the beast's own rank, 5 the
    /// rank it is synchronised at on the board), the attributes and HP the page shows (those of the synchronised rank) and its experience.
    /// </summary>
    public sealed record BookRead(uint Beast, int Rank, int Synced, BeastStats Stats, int Hp, int Exp);

    /// <summary>
    /// X-578 (0.1548, docs/evidence/0.1548-beast-book.md): the texts of the bestiary's beast page, in the order of the window's nodes: each
    /// value before its label (« 0/3; Satiété; 83; Rés. magique; 95; Intelligence; 125; Vitalité; 88; Rés. physique; 154; Force; 608/608; PV;
    /// 78/100; Exp; (8); 5; Rang »), then the beast's name. Read by place from the only « (n) » text (the beast's own rank, the synchronised
    /// one after it), so the client's language does not matter; the beast by its name (the catalog's names come from the client's sheets).
    /// A beast not held shows no numbers (« Exp; Rang »): none. Anything out of shape (after a game update): none, never a wrong rank (R08).
    /// </summary>
    public static BookRead? ParseBook(IReadOnlyList<string> texts, IReadOnlyList<Beast> beasts)
    {
        var named = texts.Select(text => beasts.FirstOrDefault(row => row.Name.Length > 0 && string.Equals(row.Name, text, StringComparison.CurrentCultureIgnoreCase)))
            .Where(row => row is not null).Distinct().ToArray();
        var at = Enumerable.Range(0, texts.Count).Where(index => Own(texts[index]) > 0).ToArray();
        if (named is not [{ } beast] || at is not [var own] || own < 16 || own + 1 >= texts.Count) return null;
        static int Own(string text) => text.Length > 2 && text[0] == '(' && text[^1] == ')' && int.TryParse(text[1..^1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : 0;
        static int Whole(string text) => int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : -1;
        static int Part(string text) => text.Split('/') is [var now, var most] && Whole(now) >= 0 && Whole(most) > 0 ? Whole(now) : -1;
        var rank = Own(texts[own]);
        var synced = Whole(texts[own + 1]);
        int[] stats = [Whole(texts[own - 6]), Whole(texts[own - 12]), Whole(texts[own - 8]), Whole(texts[own - 14]), Whole(texts[own - 10])];
        var (exp, hp, satiety) = (Part(texts[own - 2]), Part(texts[own - 4]), Part(texts[own - 16]));
        if (rank > Math.Max(1, beast.Ranks.Count) || synced < 1 || synced > rank || stats.Any(value => value < 0) || exp < 0 || hp < 0 || satiety < 0) return null;
        return new BookRead(beast.Id, rank, synced, new BeastStats(stats[0], stats[1], stats[2], stats[3], stats[4]), hp, exp);
    }

    /// <summary>
    /// X-578 (0.1548): a page of the bestiary read: its rank is the beast's own (not a floor: <see cref="Exact"/>), merged so that a rank
    /// known higher never goes down (X-561); the page kept, unless one of a higher rank is kept. Null when nothing changes (no write).
    /// </summary>
    public static SavedPlans? WithBook(SavedPlans plans, BookRead read)
    {
        var (ranks, floors) = MergeRanks(plans.Ranks, plans.Floors, new Dictionary<uint, int> { [read.Beast] = read.Rank }, Exact);
        var book = new Dictionary<uint, BookRead>(plans.Book ?? []);
        if (!book.TryGetValue(read.Beast, out var before) || before.Rank <= read.Rank) book[read.Beast] = read;
        var same = plans.Ranks is { } known && known.Count == ranks.Count && ranks.All(pair => known.TryGetValue(pair.Key, out var rank) && rank == pair.Value)
            && (plans.Floors ?? []).SetEquals(floors) && plans.Book is { } kept && kept.Count == book.Count && book.All(pair => kept.TryGetValue(pair.Key, out var page) && page == pair.Value);
        return same ? null : plans with { Ranks = ranks, Floors = floors, Book = book };
    }

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
    /// <summary>X-575 (0.1543): mark = <see cref="Mark"/> of the catalog (empty: none), so a plan made by other rules or other data is made again.</summary>
    public static string Key(IReadOnlySet<uint>? held, IReadOnlyDictionary<uint, int>? ranks, IReadOnlySet<uint>? floors = null, string mark = "")
        => (mark.Length > 0 ? mark + "|" : "") + (held is null ? "?" : string.Join(",", held.Order())) + "|" + string.Join(",", (ranks ?? new Dictionary<uint, int>()).OrderBy(pair => pair.Key)
            .Select(pair => $"{pair.Key}:{pair.Value}{(floors?.Contains(pair.Key) == true ? "+" : "")}"));
    public static string Write(SavedPlans plans) => JsonSerializer.Serialize(plans);
    /// <summary>
    /// X-617 (review of block 0.1551 to 0.1554, I1): the plans read from the file joined with those kept in memory while the file could not be
    /// read (held by another program, BeastPlansFile.Blocked). Before, the file replaced the memory and all read meanwhile was lost without a
    /// word (a board's result is never read again). A rank never goes down (X-561): the higher one is kept, exact over a floor when they are
    /// equal (X-549); the most recent result; a bestiary page of a higher rank (X-578); the plans made meanwhile over the file's (their key
    /// says what they were made from: a plan made from other ranks is made again).
    /// </summary>
    public static SavedPlans Join(SavedPlans file, SavedPlans meanwhile)
    {
        var ranks = file.Ranks is null && meanwhile.Ranks is null ? null : new Dictionary<uint, int>(file.Ranks ?? []);
        var floors = file.Floors is null && meanwhile.Floors is null ? null : new HashSet<uint>(file.Floors ?? []);
        foreach (var (beast, rank) in meanwhile.Ranks ?? [])
        {
            var floor = meanwhile.Floors?.Contains(beast) == true;
            if (!ranks!.TryGetValue(beast, out var before) || rank > before)
            {
                ranks[beast] = rank;
                if (floor) floors!.Add(beast); else floors?.Remove(beast);
            }
            else if (rank == before && !floor) floors?.Remove(beast);
        }
        var result = meanwhile.Result is { } late && (file.Result is not { } early || late.At > early.At) ? late : file.Result;
        var book = file.Book is null && meanwhile.Book is null ? null : new Dictionary<uint, BookRead>(file.Book ?? []);
        foreach (var (beast, page) in meanwhile.Book ?? [])
            if (!book!.TryGetValue(beast, out var before) || before.Rank <= page.Rank) book[beast] = page;
        var plans = new Dictionary<uint, BoardPlan>(file.Plans);
        var keys = new Dictionary<uint, string>(file.Keys);
        foreach (var (board, plan) in meanwhile.Plans)
            if (meanwhile.Keys.TryGetValue(board, out var key)) { plans[board] = plan; keys[board] = key; }
        return file with { Plans = plans, Keys = keys, Ranks = ranks, Floors = floors, Result = result, Book = book };
    }
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
