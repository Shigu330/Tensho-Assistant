namespace TenshoAssistant.MiniGames;

public sealed record TriadSolveState(TriadPosition? Position, ulong Owner, int Revision, TriadSolveResult? Result, bool Loading, string? Error);

/// <summary>Owns manual solve workers and rejects results after input, owner or lifetime changes.</summary>
public sealed class TriadSolveSession : IDisposable
{
    private readonly object gate = new();
    private CancellationTokenSource? cancellation;
    private int generation;
    private bool disposed;
    private TriadSolveState state = new(null, 0, 0, null, false, null);
    public TriadSolveState State => Volatile.Read(ref state);

    public Task StartAsync(TriadPosition position, ulong owner, int revision, TriadSolveOptions? options = null)
    {
        lock (gate)
        {
            if (disposed) return Task.CompletedTask;
            cancellation?.Cancel(); cancellation?.Dispose();
            cancellation = new();
            var token = cancellation.Token;
            var ticket = ++generation;
            state = new(position, owner, revision, null, true, null);
            return Task.Run(() =>
            {
                try
                {
                    var result = TripleTriadSolver.Solve(position, options, token);
                    lock (gate)
                        if (!disposed && ticket == generation) state = new(position, owner, revision, result, false, null);
                }
                catch (Exception error)
                {
                    lock (gate)
                        if (!disposed && ticket == generation)
                            state = new(position, owner, revision, null, false, error is OperationCanceledException ? "Calcul annulé." : error.Message);
                }
            });
        }
    }
    public void ValidateContext(ulong owner, int revision)
    {
        lock (gate)
            if (!disposed && state.Position is not null && (state.Owner != owner || state.Revision != revision)) Clear();
    }
    public void Cancel() { lock (gate) { if (!disposed) Clear(); } }
    private void Clear()
    {
        ++generation; cancellation?.Cancel();
        state = new(null, 0, 0, null, false, null);
    }
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            Clear(); disposed = true; cancellation?.Dispose();
        }
    }
}
