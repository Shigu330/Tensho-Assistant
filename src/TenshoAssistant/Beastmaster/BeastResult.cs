namespace TenshoAssistant.Beastmaster;

/// <summary>One beast of the team at the end of a board: rank and experience (out of 100) before and after.</summary>
public sealed record BeastGain(uint Beast, int RankBefore, int ExpBefore, int RankAfter, int ExpAfter);
/// <summary>A board's result as the game's result window (XBMResult) gives it: the score and each team beast's gain.</summary>
public sealed record BoardResult(DateTimeOffset At, int Score, IReadOnlyList<BeastGain> Beasts);

/// <summary>
/// X-135 (0.1163, R06): the numbers of the game's result window XBMResult, as recorded on the 1st board on 2026-11-12 in developer mode
/// (docs/evidence/0.1163-beast-result.md): index 2 the score, the sum of indexes 4, 6, 8 and 10; index 72 the team size; then five rows of
/// 16 slots for each team beast: 73 its icon (XBMPet column 4), 89 its experience before, 105 after, 121 its rank before, 137 after.
/// The « before » values are those the game's bestiary showed for the same beasts before the board. Anything outside what was seen
/// (score not the sum, unknown icon, rank going down or above the beast's last rank, experience over 100) is refused whole: after a game
/// update the window is « unavailable », never a wrong rank (R08). Only numbers are read; the window's texts may name the character.
/// </summary>
public static class BeastResults
{
    public const string Window = "XBMResult";
    /// <summary>Numbers read from the window (indexes 0 to 159).</summary>
    public const int ValueCount = 160;
    private const int Score = 2, TeamSize = 72, Icons = 73, ExpBefore = 89, ExpAfter = 105, RankBefore = 121, RankAfter = 137, MaxTeam = 10, MaxExp = 100;
    private static readonly int[] ScoreParts = [4, 6, 8, 10];

    public static BoardResult? Parse(IReadOnlyDictionary<int, long> values, IReadOnlyList<Beast> beasts, DateTimeOffset at)
    {
        long Value(int index) => values.TryGetValue(index, out var value) ? value : -1;
        var score = Value(Score);
        if (score < 0 || score > 1_000_000 || ScoreParts.Any(index => Value(index) < 0) || ScoreParts.Sum(Value) != score) return null;
        var size = Value(TeamSize);
        if (size < 1 || size > MaxTeam) return null;
        var gains = new List<BeastGain>();
        for (var slot = 0; slot < size; slot++)
        {
            var icon = Value(Icons + slot);
            var matching = beasts.Where(beast => beast.Icon != 0 && beast.Icon == icon).ToArray();
            if (matching.Length != 1) return null;
            var beast = matching[0];
            var top = Math.Max(1, beast.Ranks.Count);
            var (rankBefore, rankAfter, expBefore, expAfter) = (Value(RankBefore + slot), Value(RankAfter + slot), Value(ExpBefore + slot), Value(ExpAfter + slot));
            if (rankBefore < 1 || rankAfter < rankBefore || rankAfter > top) return null;
            if (expBefore < 0 || expBefore > MaxExp || expAfter < 0 || expAfter > MaxExp) return null;
            if (gains.Any(gain => gain.Beast == beast.Id)) return null;
            gains.Add(new BeastGain(beast.Id, (int)rankBefore, (int)expBefore, (int)rankAfter, (int)expAfter));
        }
        return new BoardResult(at, (int)score, gains.AsReadOnly());
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
