namespace TenshoAssistant.Community;

/// <summary>
/// X-363 (0.1350): true once the same item has been hovered without a break for the delay, so a tooltip asks a community source for the
/// item the player stopped on, not for every row swept by the mouse (Garland answers one request every three seconds, in order).
/// A break longer than <see cref="Gap"/> (the tooltip no longer drawn) starts the wait again. Main thread only.
/// </summary>
public sealed class HoverDwell(long delayMilliseconds)
{
    public const long Gap=250;
    private uint current;private long since;private long lastSeen=-100_000;
    public bool Settled(uint id,long nowMilliseconds)
    {
        if(id==0){current=0;return false;}
        if(id!=current || nowMilliseconds-lastSeen>Gap){current=id;since=nowMilliseconds;}
        lastSeen=nowMilliseconds;
        return nowMilliseconds-since>=delayMilliseconds;
    }
}
