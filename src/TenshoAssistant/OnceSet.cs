namespace TenshoAssistant;

/// <summary>
/// X-76 E4 (0.937): « once per event » for the watchers: Add is true the first time a value is seen. Bounded: past <c>capacity</c> the oldest values are
/// forgotten, so a long session keeps noting new events (the loot set stopped silently at 256 entries, the voyage set at 4096).
/// </summary>
public sealed class OnceSet<T>(int capacity) where T:notnull
{
    private readonly HashSet<T> seen=[];
    private readonly Queue<T> order=new();

    public int Count=>seen.Count;

    public bool Add(T value)
    {
        if(!seen.Add(value))return false;
        order.Enqueue(value);
        while(order.Count>Math.Max(1,capacity))seen.Remove(order.Dequeue());
        return true;
    }

    /// <summary>X-159 (0.1174): whether the value was seen (and not forgotten yet).</summary>
    public bool Contains(T value)=>seen.Contains(value);

    public void Clear(){seen.Clear();order.Clear();}
}
