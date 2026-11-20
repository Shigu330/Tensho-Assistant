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

    /// <summary>
    /// X-618 (review of block 0.1551 to 0.1554, M5): what a row of your facts that cannot be read (damaged JSON) does: said (<paramref name="broken"/>)
    /// and your facts marked incomplete, so no exchange replaces your gist without that fact (GitHubExchange: « ShareBlocked ») until the row is
    /// set aside and its copy on your gist can be read back first (<see cref="Resume"/>, X-652). The row stays in the database.
    /// </summary>
    public static Action<string,Exception> Incomplete(DiscoveryStore store,Action<string,Exception> broken)=>(key,error)=>{store.MineIncomplete=true;broken(key,error);};

    /// <summary>
    /// X-652 (user decision D-188 b of 20/11, with D-190 a): after the start's read, your rows that could not be read (<paramref name="unreadable"/>)
    /// were set aside (<paramref name="moved"/> of them moved into facts_aside, DiscoveryDatabase.SetAside, their text kept, never erased). When every
    /// one of them was moved and is listed among the rows whose copy the next send reads back from your gist first (<c>store.Aside</c>,
    /// GitHubExchange.KeepAside), your facts are whole again for the send: the sending resumes this session (true). Before (0.1557 to 0.1595) the
    /// sending stayed blocked for the session. A row not moved (the set-aside failed, or the row changed meanwhile) or not listed keeps the block.
    /// Only for the start's read, where <see cref="Incomplete"/> alone marks your facts incomplete.
    /// </summary>
    public static bool Resume(DiscoveryStore store,IReadOnlyCollection<string> unreadable,int moved)
    {
        if(unreadable.Count==0 || moved!=unreadable.Count || store.Aside is not { } rows)return false;
        var listed=new HashSet<string>(rows.Keys,StringComparer.Ordinal);
        if(!unreadable.All(listed.Contains))return false;
        store.MineIncomplete=false;
        return true;
    }

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
