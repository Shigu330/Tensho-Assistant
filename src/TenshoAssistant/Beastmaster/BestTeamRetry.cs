namespace TenshoAssistant.Beastmaster;

/// <summary>
/// X-848 M3 (review 0.1848): when a failed « Best team » calculation of a board is worked out again. 0.1848 logged it and worked it out again
/// every 10 s for good (a calculation that always fails the same way: one warning with its stack every 10 s while the tab is drawn), and a
/// retry left by an old failure made the first failure of a new key start again at once, unlogged. Here: the first failure of a key is
/// logged and waits FirstDelay; each later failure of the same key waits twice as long (at most LongestDelay), unlogged; a new key starts over.
/// </summary>
internal sealed class BestTeamRetry
{
    public enum Step { Wait, Log, Retry }
    public const long FirstDelay = 10_000;
    public const long LongestDelay = 300_000;
    private readonly Dictionary<uint, (string Key, long At, int Tries)> boards = [];

    /// <summary>The wait after the n-th failure of a key (Environment.TickCount64 milliseconds).</summary>
    public static long Delay(int tries) => Math.Min(LongestDelay, FirstDelay << Math.Clamp(tries - 1, 0, 10));

    /// <summary>What to do with the failed calculation of the board for this key, seen at now: Log (its first failure: log it and wait),
    /// Retry (its wait is over: start it again), Wait (nothing yet).</summary>
    public Step Failed(uint board, string key, long now)
    {
        if (!boards.TryGetValue(board, out var seen) || seen.Key != key) { boards[board] = (key, now + FirstDelay, 1); return Step.Log; }
        if (seen.At == 0) { var tries = seen.Tries + 1; boards[board] = (key, now + Delay(tries), tries); return Step.Wait; }
        if (now >= seen.At) { boards[board] = (key, 0, seen.Tries); return Step.Retry; }
        return Step.Wait;
    }

    /// <summary>The board's calculation was worked out: its failures are forgotten.</summary>
    public void Succeeded(uint board) => boards.Remove(board);
}
