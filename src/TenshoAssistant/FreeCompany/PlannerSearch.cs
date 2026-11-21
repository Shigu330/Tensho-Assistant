using System.Diagnostics.CodeAnalysis;

namespace TenshoAssistant.FreeCompany;

/// <summary>
/// X-604 (0.1706): a search of the route planner (submarine routes, airship routes, the airship fleet) on a worker, one per set of inputs
/// (its key): when the inputs change (<see cref="Needs"/>), <see cref="Start"/> cancels the running search and starts another; the result is
/// read once on the UI thread (<see cref="Take"/>). Written three times before (key, cancellation source, task, reading). Pure.
/// </summary>
public sealed class PlannerSearch<T> : IDisposable
{
    private string? key;
    private Task<T>? task;
    private CancellationTokenSource? cancel;

    /// <summary>True while a search runs or its result has not been read yet.</summary>
    public bool Searching=>task is not null;

    /// <summary>True when <paramref name="inputs"/> differ from the last search's: a new search is due.</summary>
    public bool Needs(string inputs)=>inputs!=key;

    /// <summary>Cancels the running search and starts <paramref name="work"/> on a worker for <paramref name="inputs"/>.</summary>
    public void Start(string inputs,Func<CancellationToken,T> work)
    {
        Stop();
        key=inputs;
        cancel=new CancellationTokenSource();
        var token=cancel.Token;
        task=Task.Run(()=>work(token),token);
    }

    /// <summary>
    /// The search ended: true with its result; false with its failure in <paramref name="error"/>; false with neither while it runs, once it
    /// has been read, or when it was cancelled. A search read is done: the next result comes from the next <see cref="Start"/>.
    /// </summary>
    public bool Take([MaybeNullWhen(false)] out T result,out Exception? error)
    {
        result=default;error=null;
        if(task is not { IsCompleted:true } done)return false;
        task=null;
        if(done.IsCompletedSuccessfully){result=done.Result;return true;}
        if(!done.IsCanceled)error=done.Exception;
        return false;
    }

    /// <summary>The next <see cref="Needs"/> says yes even with the same inputs (prices fetched since: the values changed).</summary>
    public void Again()=>key=null;

    /// <summary>The running search cancelled (the window closed); the next <see cref="Needs"/> says yes, so it runs again when drawn.</summary>
    public void Cancel(){Stop();key=null;}

    private void Stop()
    {
        cancel?.Cancel();cancel?.Dispose();cancel=null;
        task=null;
    }

    public void Dispose()=>Cancel();
}
