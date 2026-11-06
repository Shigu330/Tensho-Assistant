namespace TenshoAssistant.Maps;

/// <summary>
/// 0.625 (user report, log: « The game closed its map window »): talking to an NPC makes the game close its map, and the Tensho map went with it for good. Closed
/// that way it comes back once the player is free again. Closed while free, it is the player's own choice (the map key): it stays closed.
/// </summary>
public sealed class MapClickMemory
{
    public static readonly TimeSpan Window=TimeSpan.FromMilliseconds(400);
    private DateTimeOffset last=DateTimeOffset.MinValue;
    public void Clicked(DateTimeOffset now)=>last=now;
    /// <summary>True once when the left button was released just before: a link or quest was clicked, not the map key.</summary>
    public bool Take(DateTimeOffset now)
    {
        if(now-last>Window)return false;
        last=DateTimeOffset.MinValue;
        return true;
    }
}
/// <summary>Closed by the game during an event, the map comes back once the player is free (see the note above).</summary>
public sealed class MapResume
{
    private bool waiting;
    public void GameClosedMap(bool occupied)=>waiting=occupied;
    public void Cancel()=>waiting=false;
    /// <summary>
    /// True once, when the map should open again. A map opened meanwhile cancels it.
    /// 0.658 (user report: after a teleport the map stayed closed, « not every time »: a teleport to another zone, docs/evidence/0.658-map-after-teleport.md): another zone
    /// reached brings the map back too; 0.659 (user request): a duty as well. No territory (between characters) keeps waiting.
    /// </summary>
    public bool ShouldReopen(bool occupied,bool gameMapVisible,uint inTerritory)
    {
        if(!waiting)return false;
        if(gameMapVisible){waiting=false;return false;}
        if(occupied || inTerritory==0)return false;
        waiting=false;
        return true;
    }
}
