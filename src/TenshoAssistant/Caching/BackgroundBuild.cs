namespace TenshoAssistant.Caching;

/// <summary>
/// X-453 M4 (0.1457): a value built once off the game's thread (the garden chat's rules since 0.1452: their regular expressions' tables were
/// 3.8 MB built in a frame). <see cref="Start"/> builds it on the thread pool; <see cref="Get"/> gives it, waiting for a build still running
/// (a chat line that comes first is not lost) or building it now when it was never started; a build that fails is said once (the
/// <c>failed</c> callback) and the value stays null; <see cref="Stop"/> (Dispose) waits a bounded time for a build still running and drops its
/// result or its error. X-464 M1 (0.1466): a build still running at the end of the wait cannot be stopped from outside: it ends by itself
/// (it may still read the game's data until then), Stop returns false and the caller logs it. Thread-safe; pure (tests).
/// </summary>
public sealed class BackgroundBuild<T>(Func<T> build,Action<Exception> failed) where T:class
{
    private readonly object gate=new();
    private Task<T>? running;
    private T? value;
    private bool failedOnce,stopped;

    /// <summary>Builds on the thread pool, once (nothing when built, failed, running or stopped).</summary>
    public void Start()
    {
        lock(gate)
        {
            if(running is null && value is null && !failedOnce && !stopped)running=Task.Run(build);
        }
    }

    /// <summary>The value: the build started waited for, or built now; null once a build failed or after <see cref="Stop"/>.</summary>
    public T? Get()
    {
        Task<T>? task;
        lock(gate)
        {
            if(value is not null || failedOnce || stopped)return value;
            task=running;
        }
        T? made=null;
        Exception? error=null;
        try{made=task is not null ? task.GetAwaiter().GetResult():build();}
        catch(Exception caught){error=caught;}
        lock(gate)
        {
            if(value is not null || failedOnce || stopped)return value;   // another caller got there first: said once, kept once
            running=null;
            if(error is null)value=made;
            else failedOnce=true;
        }
        if(error is not null)failed(error);
        return made;
    }

    /// <summary>Stops: waits at most <paramref name="wait"/> for a build still running; its result or error is dropped. False when the build
    /// was still running at the end of the wait.</summary>
    public bool Stop(TimeSpan wait)
    {
        Task<T>? task;
        lock(gate)
        {
            stopped=true;
            task=running;
            running=null;
        }
        if(task is null)return true;
        try{return task.Wait(wait);}
        catch(AggregateException){return true;}   // a failed build: ended, its error observed and dropped
    }
}
