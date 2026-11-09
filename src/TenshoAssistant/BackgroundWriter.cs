namespace TenshoAssistant;

/// <summary>
/// X-76 H10 (0.925): saved files written off the game's framework thread, one after the other in the order asked (a later write never
/// overtakes an earlier one). The caller hands over a copy of the data; a failed write is reported through <c>warn</c>. Flush waits for the
/// queue at shutdown, with a time limit.
/// </summary>
public sealed class BackgroundWriter(Action<Exception,string> warn)
{
    private readonly object gate=new();
    private Task tail=Task.CompletedTask;

    public void Enqueue(string failure,Action write)
    {
        lock(gate)tail=tail.ContinueWith(_=>
        {
            try{write();}
            catch(Exception error){warn(error,failure);}
        },TaskScheduler.Default);
    }

    /// <summary>Waits until every write asked so far is done; false when <paramref name="wait"/> ran out.</summary>
    public bool Flush(TimeSpan wait)
    {
        Task pending;lock(gate)pending=tail;
        try{return pending.Wait(wait);}
        catch(AggregateException){return true;}
    }
}
