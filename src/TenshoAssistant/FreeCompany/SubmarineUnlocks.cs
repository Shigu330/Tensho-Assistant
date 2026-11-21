namespace TenshoAssistant.FreeCompany;

/// <summary>What unlocks a submarine sector: the sector whose voyage discovers it (0: open from the start) and the rank it asks (LuminaSupplemental).</summary>
public sealed record SubmarineUnlock(uint UnlockedBy,int Rank);

/// <summary>How a sector aimed at can be reached: unknown (not in the table), open from the start, already unlocked, or a chain to explore.</summary>
public enum SubmarineUnlockState { Unknown, Open, Unlocked, Path }

/// <summary>
/// X-200 (0.1703, the user's request of 12/11): the submarine unlock tree of LuminaSupplemental (Critical-Impact, GPL-3.0, same licence as
/// Tensho), embedded as FreeCompany/Data/submarine-unlock.csv with its notice (docs/evidence/0.1703-submarine-unlocks.md); the same model as
/// the airships' (AirshipSectors.UnlockPath). The sector ids are SubmarineExploration rows, as in the game data.
/// </summary>
public static class SubmarineUnlocks
{
    /// <summary>SubmarineUnlock rows: sector, the sector that unlocks it (0: none), rank required. Comments (#) and the header are skipped.</summary>
    public static IReadOnlyDictionary<uint,SubmarineUnlock> Read(TextReader reader)
    {
        var table=new Dictionary<uint,SubmarineUnlock>();
        string? line;
        while((line=reader.ReadLine()) is not null)
        {
            if(line.Length==0 || line.StartsWith('#'))continue;
            var cells=line.Split(',');
            if(cells.Length>=3 && uint.TryParse(cells[0],out var sector) && uint.TryParse(cells[1],out var by) && int.TryParse(cells[2],out var rank))
                table[sector]=new(by,rank);
        }
        return table;
    }

    /// <summary>The chain of sectors from the first one to explore to the sector itself (an unknown link, a loop or a sector open from the start ends it).</summary>
    public static IReadOnlyList<uint> Path(IReadOnlyDictionary<uint,SubmarineSector> sectors,IReadOnlyDictionary<uint,SubmarineUnlock> unlocks,uint sector)
    {
        var path=new List<uint>{sector};
        var current=sector;
        while(unlocks.TryGetValue(current,out var unlock) && unlock.UnlockedBy!=0 && unlock.UnlockedBy!=current && !path.Contains(unlock.UnlockedBy) && sectors.ContainsKey(unlock.UnlockedBy))
        {
            current=unlock.UnlockedBy;
            path.Insert(0,current);
        }
        return path;
    }

    /// <summary>X-711 (0.1705, review M3): the chain goes back to a sector open from the start (not cut by an unknown link or a loop).</summary>
    public static bool Whole(IReadOnlyDictionary<uint,SubmarineUnlock> unlocks,IReadOnlyList<uint> path)
        =>path.Count>0 && unlocks.TryGetValue(path[0],out var first) && first.UnlockedBy==0;

    /// <summary>
    /// The sectors to explore, in order, to unlock <paramref name="target"/>: its chain without itself, from the last sector of the chain already
    /// unlocked in the company's workshop (<paramref name="unlocked"/>; null when not read: the whole chain). Unknown, open from the start or
    /// already unlocked: said as such, nothing to explore. X-711 (0.1705, review M3): a chain cut before any sector already unlocked (a link
    /// the game's sheet does not know, a loop) is unknown, never shown as whole.
    /// </summary>
    public static (SubmarineUnlockState State,IReadOnlyList<uint> Explore) Route(IReadOnlyDictionary<uint,SubmarineSector> sectors,
        IReadOnlyDictionary<uint,SubmarineUnlock> unlocks,uint target,IReadOnlyCollection<uint>? unlocked)
    {
        if(!sectors.ContainsKey(target) || !unlocks.TryGetValue(target,out var unlock))return (SubmarineUnlockState.Unknown,[]);
        if(unlocked is not null && unlocked.Contains(target))return (SubmarineUnlockState.Unlocked,[]);
        if(unlock.UnlockedBy==0)return (SubmarineUnlockState.Open,[]);
        var path=Path(sectors,unlocks,target);
        if(path.Count<2)return (SubmarineUnlockState.Unknown,[]);
        var explore=path.Take(path.Count-1).ToList();
        var last=unlocked is null ? -1:explore.FindLastIndex(id=>unlocked.Contains(id));
        if(last<0 && !Whole(unlocks,path))return (SubmarineUnlockState.Unknown,[]);
        if(last>0)explore=explore.Skip(last).ToList();
        return (SubmarineUnlockState.Path,explore);
    }
}
