using System.Numerics;

namespace TenshoAssistant.MiniGames;

public sealed record MiniCactpotResult(bool ChooseLine, IReadOnlyList<double?> Choices, double ExpectedReward, int States);

public static class MiniCactpotSolver
{
    private static readonly int[] Rewards = [0, 0, 0, 0, 0, 0, 10000, 36, 720, 360, 80, 252, 108, 72, 54, 180, 72, 180, 119, 36, 306, 1080, 144, 1800, 3600];
    private static readonly int[][] Lines = [[0, 1, 2], [3, 4, 5], [6, 7, 8], [0, 3, 6], [1, 4, 7], [2, 5, 8], [0, 4, 8], [2, 4, 6]];

    public static MiniCactpotResult Solve(IReadOnlyList<int> input, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Count != 9 || input.Any(value => value is < 0 or > 9)
            || input.Where(value => value != 0).Distinct().Count() != input.Count(value => value != 0))
            throw new ArgumentException("Saisir neuf cases, sans doublons parmi les chiffres de 1 à 9.", nameof(input));
        var known = input.Count(value => value != 0);
        if (known is < 1 or > 4) throw new ArgumentException("Saisir entre un et quatre chiffres révélés.", nameof(input));
        var board = input.ToArray();
        var memo = new Dictionary<ulong, double>();
        var steps = 0;
        cancellation.ThrowIfCancellationRequested();

        int RemainingMask()
        {
            var mask = 511;
            foreach (var value in board) if (value > 0) mask &= ~(1 << (value - 1));
            return mask;
        }
        double[] LineValues(int mask)
        {
            var scores = new double[8];
            for (var line = 0; line < Lines.Length; line++)
            {
                var sum = 0;
                var blanks = 0;
                foreach (var index in Lines[line]) { sum += board[index]; if (board[index] == 0) blanks++; }
                if (blanks == 0) { scores[line] = Rewards[sum]; continue; }
                var total = 0d;
                var count = 0;
                for (var subset = mask; subset != 0; subset = (subset - 1) & mask)
                {
                    if (BitOperations.PopCount((uint)subset) != blanks) continue;
                    var extra = 0;
                    for (var bit = 0; bit < 9; bit++) if ((subset & (1 << bit)) != 0) extra += bit + 1;
                    total += Rewards[sum + extra]; count++;
                }
                scores[line] = total / count;
            }
            return scores;
        }
        double Evaluate(int revealed)
        {
            if ((++steps & 255) == 0) cancellation.ThrowIfCancellationRequested();
            ulong key = 0;
            for (var index = 0; index < 9; index++) key |= (ulong)board[index] << (index * 4);
            if (memo.TryGetValue(key, out var saved)) return saved;
            var mask = RemainingMask();
            if (revealed == 4) return memo[key] = LineValues(mask).Max();
            var best = 0d;
            var outcomes = BitOperations.PopCount((uint)mask);
            for (var index = 0; index < 9; index++)
            {
                if (board[index] != 0) continue;
                var total = 0d;
                for (var number = 1; number <= 9; number++)
                {
                    if ((mask & (1 << (number - 1))) == 0) continue;
                    board[index] = number;
                    total += Evaluate(revealed + 1);
                    board[index] = 0;
                }
                best = Math.Max(best, total / outcomes);
            }
            return memo[key] = best;
        }
        var expected = Evaluate(known);
        var choices = new double?[known == 4 ? 8 : 9];
        var remaining = RemainingMask();
        if (known == 4)
        {
            var lines = LineValues(remaining);
            for (var index = 0; index < lines.Length; index++) choices[index] = lines[index];
        }
        else
        {
            for (var index = 0; index < 9; index++)
            {
                if (board[index] != 0) continue;
                var total = 0d;
                for (var number = 1; number <= 9; number++)
                {
                    if ((remaining & (1 << (number - 1))) == 0) continue;
                    board[index] = number;
                    total += Evaluate(known + 1);
                    board[index] = 0;
                }
                choices[index] = total / BitOperations.PopCount((uint)remaining);
            }
        }
        return new(known == 4, Array.AsReadOnly(choices), expected, memo.Count);
    }
}
