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
    private static readonly char[] Colons=[':','\uFF1A'];

    /// <summary>
    /// X-404 (0.1372): the label the LeveClient sheet writes before each client's name (« Client: », « Client : », « Auftraggeber: »…), found from
    /// the sheet itself in any language: a text up to the first colon, short and without a comma, that starts at least two names and a tenth of them, the
    /// labels kept starting together at least four names in five. None when the sheet has no such label (Japanese: its colons part a group from
    /// a person, never the same text on many rows). Evidence: docs/evidence/0.1372-leve-client-label.md.
    /// </summary>
    public static IReadOnlyList<string> ClientLabels(IEnumerable<string> names)
    {
        var all=names.Where(name=>name.Trim().Length>0).ToArray();
        if(all.Length==0)return [];
        var counts=new Dictionary<string,int>(StringComparer.Ordinal);
        foreach(var name in all)
        {
            var colon=name.IndexOfAny(Colons);
            if(colon<=0 || colon>24)continue;
            var label=name[..(colon+1)];
            if(label.Contains(',') || label.Contains('\uFF0C') || label.Contains('\u3001'))continue;
            counts[label]=counts.GetValueOrDefault(label)+1;
        }
        var kept=counts.Where(pair=>pair.Value>=2 && pair.Value*10>=all.Length).OrderByDescending(pair=>pair.Key.Length).ThenBy(pair=>pair.Key,StringComparer.Ordinal).ToArray();
        return kept.Sum(pair=>pair.Value)*5>=all.Length*4 ? kept.Select(pair=>pair.Key).ToArray():[];
    }

    /// <summary>X-404 (0.1372): the client's name without the sheet's label (ClientLabels), the spaces after it removed; otherwise the name as it is.</summary>
    public static string WithoutClientLabel(string name,IReadOnlyList<string> labels)
    {
        foreach(var label in labels)
            if(name.StartsWith(label,StringComparison.Ordinal))
            {
                var rest=name[label.Length..].TrimStart(' ','\u00A0','\u202F','\u3000').TrimEnd();
                return rest.Length>0 ? rest:name;
            }
        return name.TrimEnd();
    }

    public static bool Matches(LeveDefinition leve,LeveProgressState? progress,LeveStateFilter state,uint type,string client,int minLevel,int maxLevel,string query)
    {
        if(type!=0 && leve.TypeId!=type)return false;
        if(client.Length>0 && leve.Client!=client)return false;
        if(leve.Level<minLevel || maxLevel>0 && leve.Level>maxLevel)return false;
        if(query.Length>0 && !SearchText.Contains(leve.Name,query,StringComparison.CurrentCultureIgnoreCase))return false;
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
