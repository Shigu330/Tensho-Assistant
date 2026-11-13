namespace TenshoAssistant.Maps;

/// <summary>
/// X-01 C5 (0.1206, analysis of 12/11: the first region map took 1 311 ms on the game's thread, building the names of every map and the
/// aetheryte rows there): a value built once, under a lock, by whichever thread asks first (the frame, or the warm-up started off the frame
/// after loading); the other waits for it rather than building it twice, then everyone reads the published value without a lock. A build
/// that throws keeps nothing: the error goes to the caller that asked, and the next ask tries again.
/// </summary>
public sealed class BuiltOnce<T>(Func<T> build) where T:class
{
    private readonly object gate=new();
    private T? value;

    /// <summary>True once the value is built.</summary>
    public bool Built=>Volatile.Read(ref value) is not null;

    public T Value
    {
        get
        {
            if(Volatile.Read(ref value) is { } known)return known;
            lock(gate)
            {
                if(value is { } built)return built;
                var made=build() ?? throw new InvalidOperationException("A value built once cannot be null.");
                Volatile.Write(ref value,made);
                return made;
            }
        }
    }
}
