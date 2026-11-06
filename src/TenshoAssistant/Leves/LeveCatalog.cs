namespace TenshoAssistant.Leves;

/// <summary>A guildleve of the game data (Leve sheet): level, assignment type, client, start zone, gil reward and allowance cost.</summary>
public sealed record LeveDefinition(uint Id,string Name,int Level,uint TypeId,string Type,uint TypeIcon,string Client,string Zone,uint Gil,byte Allowances,LevePlace? ClientPlace=null,LevePlace? StartPlace=null);
/// <summary>W-08 (0.698): a Level row of a leve: its map and world position. The client's, and where the leve starts.</summary>
public sealed record LevePlace(uint MapId,float X,float Z);

/// <summary>What the game says about the character's guildleves: completed (null = not read), accepted ones (id, step), allowances left.</summary>
public sealed record LeveProgressState(IReadOnlySet<uint>? Completed,IReadOnlyList<uint> Accepted,int? Allowances,DateTimeOffset ReadAt);

/// <summary>Leve list filter: all, not completed, completed, accepted.</summary>
public enum LeveStateFilter { All, NotCompleted, Completed, Accepted }

/// <summary>
/// g, C-14 (0.363): guildleves — list, filters by level, type, client and state. Unknown completion (not read) stays in « not completed »
/// and never counts as completed. Original code (ideas from LeveHelper, read only; no automation is taken from Chilled Leves).
/// </summary>
public static class LeveCatalog
{
    public static bool Matches(LeveDefinition leve,LeveProgressState? progress,LeveStateFilter state,uint type,string client,int minLevel,int maxLevel,string query)
    {
        if(type!=0 && leve.TypeId!=type)return false;
        if(client.Length>0 && leve.Client!=client)return false;
        if(leve.Level<minLevel || maxLevel>0 && leve.Level>maxLevel)return false;
        if(query.Length>0 && !leve.Name.Contains(query,StringComparison.CurrentCultureIgnoreCase))return false;
        bool? completed=progress?.Completed is { } done ? done.Contains(leve.Id):null;
        return state switch
        {
            LeveStateFilter.NotCompleted=>completed!=true,
            LeveStateFilter.Completed=>completed==true,
            LeveStateFilter.Accepted=>progress?.Accepted.Contains(leve.Id)==true,
            _=>true,
        };
    }
}
