namespace TenshoAssistant;

/// <summary>Owns successful registrations and releases all of them in reverse order.</summary>
internal sealed class RegistrationScope : IDisposable
{
    private readonly Stack<Action> cleanup = new();
    private bool disposed;

    public void Register(Action subscribe, Action unsubscribe)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        subscribe();
        cleanup.Push(unsubscribe);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        List<Exception>? errors = null;
        while (cleanup.TryPop(out var release))
        {
            try { release(); }
            catch (Exception error) { (errors ??= []).Add(error); }
        }

        if (errors is not null) throw new AggregateException(errors);
    }
}
