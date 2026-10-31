using System.Diagnostics;

namespace TenshoAssistant.MiniGames;

public sealed record TriadSolveOptions(int NodeLimit = 1_000_000, int Milliseconds = 3000, int MaxDepth = 9);
public sealed record TriadMoveValue(TriadMove Move, int BlueDifference);
public sealed record TriadSolveResult(IReadOnlyList<TriadMoveValue> Moves, int CompletedDepth, bool Exact, long Nodes, bool Limited)
{
    public IReadOnlyList<TriadMoveValue> Best(TriadPlayer player)
    {
        if (Moves.Count == 0) return [];
        var score = player == TriadPlayer.Blue ? Moves.Max(move => move.BlueDifference) : Moves.Min(move => move.BlueDifference);
        return Array.AsReadOnly(Moves.Where(move => move.BlueDifference == score).ToArray());
    }
}

/// <summary>Minimax for the position's explicit rules, iterative deepening and alpha/beta; no hidden-card inference.</summary>
public static class TripleTriadSolver
{
    private sealed class LimitReached : Exception { }
    public static TriadSolveResult Solve(TriadPosition position, TriadSolveOptions? options = null, CancellationToken cancellation = default)
    {
        options ??= new();
        if (!position.IsInitialized) throw new ArgumentException("Position non initialisée.", nameof(position));
        if (options.NodeLimit is < 1 or > 10_000_000 || options.Milliseconds is < 1 or > 30_000 || options.MaxDepth is < 1 or > 9)
            throw new ArgumentOutOfRangeException(nameof(options));
        cancellation.ThrowIfCancellationRequested();
        if (position.IsTerminal) return new([], 0, true, 0, false);
        var started = Stopwatch.GetTimestamp();
        long nodes = 0;
        var last = Array.Empty<TriadMoveValue>();
        var completed = 0;
        var limited = false;
        int Search(TriadPosition state, int depth, int alpha, int beta)
        {
            cancellation.ThrowIfCancellationRequested();
            if (nodes >= options.NodeLimit || Stopwatch.GetElapsedTime(started).TotalMilliseconds >= options.Milliseconds) throw new LimitReached();
            nodes++;
            if (depth == 0 || state.IsTerminal) return state.Difference;
            var maximize = state.Turn == TriadPlayer.Blue;
            var best = maximize ? -11 : 11;
            for (var index = maximize ? 0 : 5; index < (maximize ? 5 : 10); index++)
            {
                if (!state.CanPlayCard(index)) continue;
                for (var square = 0; square < 9; square++)
                {
                    if (state.CardAt(square) >= 0) continue;
                    var score = Search(state.PlayUnchecked(index, square), depth - 1, alpha, beta);
                    best = maximize ? Math.Max(best, score) : Math.Min(best, score);
                    if (maximize) alpha = Math.Max(alpha, best); else beta = Math.Min(beta, best);
                    if (alpha >= beta) return best;
                }
            }
            return best;
        }
        var legal = position.LegalMoves();
        for (var depth = 1; depth <= Math.Min(position.EmptySquares, options.MaxDepth); depth++)
        {
            try
            {
                var iteration = new List<TriadMoveValue>();
                foreach (var move in legal)
                    iteration.Add(new(move, Search(position.PlayUnchecked(move.CardIndex, move.Square), depth - 1, -11, 11)));
                // Publish only a complete iteration: every root move has the same search depth.
                last = iteration.ToArray(); completed = depth;
            }
            catch (LimitReached) { limited = true; break; }
        }
        return new(Array.AsReadOnly(last), completed, completed == position.EmptySquares, nodes, limited);
    }
}
