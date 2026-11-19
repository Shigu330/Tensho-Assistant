namespace TenshoAssistant.Discoveries;

/// <summary>
/// X-588 (external audit A01 of 18/11): the start's read of your own discoveries (your facts and the list of shared kinds, from the database).
/// It was outside any try, in a task nobody waited for: an error left the Discoveries page « loading » for the whole session and the error
/// unobserved. Now it is tried again (<c>tries</c> times, <c>wait</c> apart), each failure reported (the page says it), and a read that still
/// fails gives up cleanly: the caller opens the page without your facts and sends nothing to GitHub (DiscoveryStore.MineIncomplete).
/// </summary>
public static class DiscoveryStart
{
    public const int Tries=5;
    public static readonly TimeSpan Wait=TimeSpan.FromSeconds(10);

    /// <summary>(true, the value) once <paramref name="read"/> returns; (false, default) after <paramref name="tries"/> failures or when stopped.</summary>
    public static async Task<(bool Read,T? Value)> ReadWithRetries<T>(Func<T> read,int tries,TimeSpan wait,Action<Exception,int> failed,CancellationToken token)
    {
        for(var attempt=1;;attempt++)
        {
            try{return (true,read());}
            catch(Exception error){failed(error,attempt);if(attempt>=tries)return (false,default);}
            try{await Task.Delay(wait,token).ConfigureAwait(false);}
            catch(OperationCanceledException){return (false,default);}
        }
    }
}
