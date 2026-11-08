namespace TenshoAssistant.Caching;

/// <summary>
/// X-76 (0.956): a value drawn from a published state object (states are replaced, never changed in place), built again only when another
/// object is published. Draw-thread only, like the pages that use it.
/// </summary>
public sealed class ReferenceMemo<TSource,TValue>(Func<TSource,TValue> build) where TSource : class
{
    private TSource? source;
    private TValue value=default!;
    /// <summary>How many times the value was built (tests).</summary>
    public int Builds { get; private set; }
    public TValue Of(TSource current)
    {
        ArgumentNullException.ThrowIfNull(current);
        if(Builds>0 && ReferenceEquals(source,current))return value;
        value=build(current);
        source=current;
        Builds++;
        return value;
    }
}
