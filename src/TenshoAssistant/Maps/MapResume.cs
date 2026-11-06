namespace TenshoAssistant.Maps;

/// <summary>
/// 0.625 (user report, log: « The game closed its map window »): talking to an NPC makes the game close its map, and the Tensho map went with it for good. Closed
/// that way it comes back once the player is free again. Closed while free, it is the player's own choice (the map key): it stays closed.
/// </summary>
public sealed class MapResume
{
    private bool waiting;
    private uint territory;
    public void GameClosedMap(bool occupied,uint inTerritory){waiting=occupied;territory=inTerritory;}
    /// <summary>True once, when the map should open again. A map opened meanwhile, or another zone reached, cancels it.</summary>
    public bool ShouldReopen(bool occupied,bool gameMapVisible,uint inTerritory)
    {
        if(!waiting)return false;
        if(gameMapVisible || inTerritory!=territory){waiting=false;return false;}
        if(occupied)return false;
        waiting=false;
        return true;
    }
}
