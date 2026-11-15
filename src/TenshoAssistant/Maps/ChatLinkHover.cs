using System.Numerics;

namespace TenshoAssistant.Maps;

/// <summary>A chat map link under the mouse, copied from the game's link data (map coordinates as the game shows them).</summary>
public sealed record HoveredMapLink(uint TerritoryId,uint MapId,float X,float Y,string Place);

/// <summary>
/// X-365 (0.1351): the chat map link under the mouse, from the chat panels' link events (game thread), for the right-click Tensho menu
/// (drawn by the window). A link of another kind under the mouse (item, player) clears it. The game sends « mouse out » as the button
/// goes down on some frames: the link stays known <see cref="Grace"/> ms after it. Nothing while Tensho is cut or the chat links are off.
/// X-395 M4 (0.1366): a link whose « mouse out » never came is not kept for ever: it holds <see cref="MaxAge"/> ms after its last « over »
/// and only while the cursor stays within <see cref="Reach"/> pixels of where it was then (a right click in the world later, to turn the
/// camera, is not about it). X-387 (0.1366): the link clicked with the left button is kept <see cref="ClickLife"/> ms for the Tensho map.
/// </summary>
public sealed class ChatLinkHover
{
    public const long Grace=400,MaxAge=60_000,ClickLife=3_000;
    public const float Reach=48f;
    private readonly object gate=new();
    private HoveredMapLink? link;private long outAt=long.MaxValue,overAt;private Vector2? overCursor;
    private (HoveredMapLink Link,long At)? clicked;
    public void Over(HoveredMapLink? hovered,long now,Vector2? cursor){lock(gate){link=hovered;outAt=long.MaxValue;overAt=now;overCursor=cursor;}}
    public void Out(long now){lock(gate){if(link is not null && outAt==long.MaxValue)outAt=now;}}
    public void Clear(){lock(gate){link=null;outAt=long.MaxValue;overCursor=null;clicked=null;}}
    public HoveredMapLink? Current(long now,Vector2? cursor){lock(gate){return Why(now,cursor) is null ? link:null;}}
    /// <summary>The link a right click at <paramref name="now"/> is about; null when cut, off, or no map link under the mouse.</summary>
    public HoveredMapLink? RightClick(long now,Vector2? cursor,bool cut,bool enabled)
    {
        if(cut || !enabled){Clear();return null;}
        return Current(now,cursor);
    }
    /// <summary>For the developer line written at a right click: null when no map link was hovered at all, else what was decided.</summary>
    public string? Describe(long now,Vector2? cursor)
    {
        lock(gate)
        {
            if(link is null)return null;
            return Why(now,cursor) is { } why ? $"map {link.MapId} not taken: {why}":$"map {link.MapId}, menu opened";
        }
    }
    private string? Why(long now,Vector2? cursor)
    {
        if(link is null)return "no link";
        if(outAt!=long.MaxValue && now-outAt>Grace)return $"mouse out {now-outAt} ms before";
        if(now-overAt>MaxAge)return $"hovered {now-overAt} ms before";
        if(overCursor is { } then && cursor is { } here && Vector2.Distance(then,here)>Reach)return $"cursor {Vector2.Distance(then,here):0} px away";
        return null;
    }
    /// <summary>X-387: the map link clicked with the left button (the game places its flag), for the Tensho map.</summary>
    public void Clicked(HoveredMapLink map,long now){lock(gate)clicked=(map,now);}
    /// <summary>X-387: the link clicked within <see cref="ClickLife"/> ms, once; nothing when cut or off.</summary>
    public HoveredMapLink? TakeClick(long now,bool cut,bool enabled)
    {
        lock(gate)
        {
            if(clicked is not { } seen)return null;
            clicked=null;
            return cut || !enabled || now<seen.At || now-seen.At>ClickLife ? null:seen.Link;
        }
    }
}
