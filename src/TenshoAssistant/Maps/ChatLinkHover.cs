namespace TenshoAssistant.Maps;

/// <summary>A chat map link under the mouse, copied from the game's link data (map coordinates as the game shows them).</summary>
public sealed record HoveredMapLink(uint TerritoryId,uint MapId,float X,float Y,string Place);

/// <summary>
/// X-365 (0.1351): the chat map link under the mouse, from the chat panels' link events (game thread), for the right-click Tensho menu
/// (drawn by the window). A link of another kind under the mouse (item, player) clears it. The game sends « mouse out » as the button
/// goes down on some frames: the link stays known <see cref="Grace"/> ms after it. Nothing while Tensho is cut or the chat links are off.
/// </summary>
public sealed class ChatLinkHover
{
    public const long Grace=150;
    private readonly object gate=new();
    private HoveredMapLink? link;private long outAt=long.MaxValue;
    public void Over(HoveredMapLink? hovered){lock(gate){link=hovered;outAt=long.MaxValue;}}
    public void Out(long now){lock(gate){if(link is not null && outAt==long.MaxValue)outAt=now;}}
    public void Clear(){lock(gate){link=null;outAt=long.MaxValue;}}
    public HoveredMapLink? Current(long now){lock(gate){return link is not null && (outAt==long.MaxValue || now-outAt<=Grace) ? link:null;}}
    /// <summary>The link a right click at <paramref name="now"/> is about; null when cut, off, or no map link under the mouse.</summary>
    public HoveredMapLink? RightClick(long now,bool cut,bool enabled)
    {
        if(cut || !enabled){Clear();return null;}
        return Current(now);
    }
}
