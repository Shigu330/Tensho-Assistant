namespace TenshoAssistant.Quests;

/// <summary>
/// X-606 (0.1710): where a quest can be put on Tensho's map: its giver (the Level row of Quest.IssuerLocation, a fixed point of the sheets) and,
/// once accepted, its current step (the game's own quest markers; X-57: the giver's place is not where an accepted quest's step is). Pure.
/// </summary>
public static class QuestPlaces
{
    /// <summary>The sheet places the giver: a map and a territory (the Level row resolved).</summary>
    public static bool HasGiverPlace(QuestEntry entry)=>entry.IssuerMapId!=0 && entry.IssuerTerritoryId!=0 && float.IsFinite(entry.IssuerX) && float.IsFinite(entry.IssuerZ);

    /// <summary>
    /// The first point the game shows for this quest: its markers carry the quest's number in the low 16 bits (row 68142 is quest 2606, as the
    /// map's markers); null when it shows none, or nothing was read.
    /// </summary>
    public static (uint MapId,float X,float Z)? Step(IEnumerable<(uint Quest,uint MapId,float X,float Z)>? points,uint quest)
    {
        if(points is null || quest==0)return null;
        var key=quest & 0xFFFF;
        foreach(var point in points)
            if(point.Quest!=0 && (point.Quest & 0xFFFF)==key && point.MapId!=0 && float.IsFinite(point.X) && float.IsFinite(point.Z))return (point.MapId,point.X,point.Z);
        return null;
    }
}
