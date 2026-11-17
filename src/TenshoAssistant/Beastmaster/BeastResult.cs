namespace TenshoAssistant.Beastmaster;

/// <summary>One beast of the team at the end of a board: rank and experience (out of 100) before and after.</summary>
public sealed record BeastGain(uint Beast, int RankBefore, int ExpBefore, int RankAfter, int ExpAfter);
/// <summary>A board's result as the game's result window (XBMResult) gives it: the score and each team beast's gain.</summary>
/// <param name="Bonus">X-481 (0.1470): the window's « Bonus »: the points of the bonuses obtained; null when not read (unknown, never 0 by default).</param>
/// <param name="Bonuses">X-481: how many bonuses were obtained; null when not read.</param>
/// <param name="Total">X-481: the window's « Total »: the score and the bonus; null when not read.</param>
/// <param name="Parts">X-481 (0.1525): the score's lines as the window shows them; null in a result kept before 0.1525.</param>
/// <param name="BonusPoints">X-481 (0.1525): each bonus's points, in the window's order; null when the bonus is not read.</param>
public sealed record BoardResult(DateTimeOffset At, int Score, IReadOnlyList<BeastGain> Beasts, int? Bonus = null, int? Bonuses = null, int? Total = null,
    BoardParts? Parts = null, IReadOnlyList<int>? BonusPoints = null);
/// <summary>X-481 (0.1525, the user's reading R-X-481 of 17/11: the game's window beside the developer line of the same board): the score's lines,
/// index 4 « Chemin parcouru » (moves made), 6 the enemies (« affrontement »), 8 the elite enemies (« cible d'élite »), 10 the boss (« gardien du
/// plateau »), 12 « PV restants » (remaining HP) with its value at 11. docs/evidence/0.1470-beast-result-window.md.</summary>
public sealed record BoardParts(int Path, int Enemies, int Elite, int Boss, int HpPoints, int? HpLeft);

/// <summary>
/// X-135 (0.1163, R06): the numbers of the game's result window XBMResult, as recorded on the 1st board on 2026-11-12 in developer mode
/// (docs/evidence/0.1163-beast-result.md): index 2 the score, the sum of indexes 4, 6, 8, 10 and 12 (X-465, 0.1465: 12 was 0 on the first
/// board and 312 on the board of 11/15, docs/evidence/0.1465-beast-result-index-12.md); index 72 the team size; then five rows of
/// 16 slots for each team beast: 73 its icon (XBMPet column 4), 89 its experience before, 105 after, 121 its rank before, 137 after.
/// The « before » values are those the game's bestiary showed for the same beasts before the board. Anything outside what was seen
/// (score not the sum, unknown icon, rank going down or above the beast's last rank, experience over 100) is refused whole: after a game
/// update the window is « unavailable », never a wrong rank (R08). Only numbers are read; the window's texts may name the character.
/// X-481 (0.1470, docs/evidence/0.1470-beast-result-window.md): the window's « Bonus » and « Total » (its labels in ui/uld/XBMResult.uld):
/// index 14 the number of bonuses obtained, 31 onwards each one's points, 13 their sum, 68 the total (score + bonus), on both boards recorded.
/// They are kept only when they add up; otherwise they stay unknown and the score and the beasts are kept (never a wrong total).
/// </summary>
public static class BeastResults
{
    public const string Window = "XBMResult";
    /// <summary>Numbers read from the window (indexes 0 to 159).</summary>
    public const int ValueCount = 160;
    private const int Score = 2, HpLeft = 11, TeamSize = 72, Icons = 73, ExpBefore = 89, ExpAfter = 105, RankBefore = 121, RankAfter = 137, MaxTeam = 10, MaxExp = 100;
    private static readonly int[] ScoreParts = [4, 6, 8, 10, 12];
    // X-481 (0.1470): the bonuses' sum, their number, the first one's points (one slot each, at most 16 before index 47), the total.
    private const int BonusSum = 13, BonusCount = 14, BonusList = 31, BonusSlots = 16, Total = 68;

    public static BoardResult? Parse(IReadOnlyDictionary<int, long> values, IReadOnlyList<Beast> beasts, DateTimeOffset at) => Parse(values, beasts, at, out _);

    /// <summary>X-465 (0.1465): why the window is refused, for the log (numbers only); empty when it is read.</summary>
    public static string Why(IReadOnlyDictionary<int, long> values, IReadOnlyList<Beast> beasts)
    {
        Parse(values, beasts, DateTimeOffset.UnixEpoch, out var why);
        return why;
    }

    private static BoardResult? Parse(IReadOnlyDictionary<int, long> values, IReadOnlyList<Beast> beasts, DateTimeOffset at, out string why)
    {
        long Value(int index) => values.TryGetValue(index, out var value) ? value : -1;
        var score = Value(Score);
        why = "";
        if (score < 0 || score > 1_000_000 || ScoreParts.Any(index => Value(index) < 0) || ScoreParts.Sum(Value) != score)
        {
            why = $"score {score} is not the sum of indexes {string.Join(", ", ScoreParts)} ({string.Join(" + ", ScoreParts.Select(Value))})";
            return null;
        }
        var size = Value(TeamSize);
        if (size < 1 || size > MaxTeam) { why = $"team size {size}"; return null; }
        var gains = new List<BeastGain>();
        for (var slot = 0; slot < size; slot++)
        {
            var icon = Value(Icons + slot);
            var matching = beasts.Where(beast => beast.Icon != 0 && beast.Icon == icon).ToArray();
            if (matching.Length != 1) { why = $"slot {slot}: icon {icon} matches {matching.Length} beasts"; return null; }
            var beast = matching[0];
            var top = Math.Max(1, beast.Ranks.Count);
            var (rankBefore, rankAfter, expBefore, expAfter) = (Value(RankBefore + slot), Value(RankAfter + slot), Value(ExpBefore + slot), Value(ExpAfter + slot));
            if (rankBefore < 1 || rankAfter < rankBefore || rankAfter > top) { why = $"slot {slot}: rank {rankBefore} to {rankAfter} (top {top})"; return null; }
            if (expBefore < 0 || expBefore > MaxExp || expAfter < 0 || expAfter > MaxExp) { why = $"slot {slot}: experience {expBefore} to {expAfter}"; return null; }
            if (gains.Any(gain => gain.Beast == beast.Id)) { why = $"slot {slot}: beast {beast.Id} twice"; return null; }
            gains.Add(new BeastGain(beast.Id, (int)rankBefore, (int)expBefore, (int)rankAfter, (int)expAfter));
        }
        var (bonus, bonuses, total) = Bonuses(Value, score);
        // X-481 (0.1525): the parts were checked above (their sum is the score); the remaining HP's value kept when it is a plain number.
        var hpLeft = Value(HpLeft);
        var parts = new BoardParts((int)Value(ScoreParts[0]), (int)Value(ScoreParts[1]), (int)Value(ScoreParts[2]), (int)Value(ScoreParts[3]), (int)Value(ScoreParts[4]),
            hpLeft is >= 0 and <= 1_000_000 ? (int)hpLeft : null);
        IReadOnlyList<int>? points = bonuses is { } count ? Enumerable.Range(BonusList, count).Select(index => (int)Value(index)).ToArray() : null;
        return new BoardResult(at, (int)score, gains.AsReadOnly(), bonus, bonuses, total, parts, points);
    }

    /// <summary>X-481 (0.1470): the window's bonus (sum, number) and total when they add up (each bonus above 0, their sum at 13, the total the score
    /// plus the bonus); else all unknown.</summary>
    private static (int? Bonus, int? Bonuses, int? Total) Bonuses(Func<int, long> value, long score)
    {
        var (sum, count, total) = (value(BonusSum), value(BonusCount), value(Total));
        if (count < 0 || count > BonusSlots || sum < 0 || sum > 1_000_000) return (null, null, null);
        var points = Enumerable.Range(BonusList, (int)count).Select(value).ToArray();
        if (points.Any(point => point <= 0) || points.Sum() != sum || total != score + sum) return (null, null, null);
        return ((int)sum, (int)count, (int)total);
    }

    /// <summary>The same board result read again (the window shown twice): its time aside.</summary>
    public static bool Same(BoardResult? left, BoardResult? right)
        => left is not null && right is not null && left.Score == right.Score && left.Beasts.SequenceEqual(right.Beasts);

    /// <summary>The known ranks with the result's « after » ranks over them.</summary>
    public static Dictionary<uint, int> Merge(IReadOnlyDictionary<uint, int>? known, BoardResult result)
    {
        var merged = new Dictionary<uint, int>(known ?? new Dictionary<uint, int>());
        foreach (var gain in result.Beasts) merged[gain.Beast] = gain.RankAfter;
        return merged;
    }
}
