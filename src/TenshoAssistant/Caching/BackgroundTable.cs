namespace TenshoAssistant.Caching;

/// <summary>
/// X-332 (0.1327, review of 0.1318 I3, R07): a lookup table read from the game's sheets once, off the frame. The first lookup starts the build on
/// the thread pool and answers « not yet » (default); every later one is a dictionary read. A build that throws leaves an empty table (each key
/// then answers default, the page shows its fallback) and is reported once. Safe from any thread.
/// </summary>
public sealed class BackgroundTable<TKey,TValue>(Func<IReadOnlyDictionary<TKey,TValue>> build,Action<Exception>? failed=null) where TKey:notnull
{
    private IReadOnlyDictionary<TKey,TValue>? table;
    private int started,builds;
    /// <summary>The build, once started (tests wait for it).</summary>
    public Task? Building { get; private set; }
    /// <summary>How many times the table was built (tests).</summary>
    public int Builds=>Volatile.Read(ref builds);
    public bool Ready=>Volatile.Read(ref table) is not null;

    /// <summary>The value of <paramref name="key"/>; false while the table is being built (started by this call) or when the key is absent.</summary>
    public bool TryGet(TKey key,out TValue? value)
    {
        if(Volatile.Read(ref table) is not { } held){Start();value=default;return false;}
        if(held.TryGetValue(key,out var found)){value=found;return true;}
        value=default;
        return false;
    }
    public TValue? Get(TKey key)=>TryGet(key,out var value) ? value:default;

    private void Start()
    {
        if(Interlocked.Exchange(ref started,1)==1)return;
        Building=Task.Run(()=>
        {
            IReadOnlyDictionary<TKey,TValue> built;
            try{built=build();}
            catch(Exception error){failed?.Invoke(error);built=new Dictionary<TKey,TValue>();}
            Interlocked.Increment(ref builds);
            Volatile.Write(ref table,built);
        });
    }
}
