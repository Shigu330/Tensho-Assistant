namespace TenshoAssistant.FreeCompany;

/// <summary>
/// T26 (0.919): the route of an airship's last voyage, rebuilt from the game's voyage log (WorkshopTerritory.Airship.AirshipLogs: lines such as
/// « Mettons le cap vers “Mer de nuages - secteur 1” », docs/evidence/0.919-airship-reading-2026-11-08.log). Each line is searched for the
/// sector names of AirshipExplorationPoint (client language), longest first so « … secteur 1 » never matches inside « … secteur 10 »; the
/// sectors are kept in the order they first appear. Unknown text is ignored, nothing is guessed.
/// </summary>
public static class AirshipLogRoute
{
    /// <summary>
    /// X-73 (0.1003): each stop's finds (in the order of the voyage) given the sector of the route at the same place. Null when the stops with a find
    /// are not exactly as many as the route's sectors: then nothing is paired (never guessed).
    /// </summary>
    public static IReadOnlyList<(uint Item,uint Sector,int Count)>? Pair(IReadOnlyList<(uint Primary,int PrimaryCount,uint Additional,int AdditionalCount)> stops,IReadOnlyList<uint> route)
    {
        var found=stops.Where(stop=>stop.Primary!=0 || stop.Additional!=0).ToArray();
        if(found.Length==0 || found.Length!=route.Count)return null;
        var loot=new List<(uint,uint,int)>();
        for(var index=0;index<found.Length;index++)
        {
            if(found[index].Primary!=0)loot.Add((found[index].Primary,route[index],found[index].PrimaryCount));
            if(found[index].Additional!=0)loot.Add((found[index].Additional,route[index],found[index].AdditionalCount));
        }
        return loot;
    }

    public static IReadOnlyList<uint> Sectors(IEnumerable<string> lines,IEnumerable<(uint Id,string Name)> sectors)
    {
        var names=sectors.Where(sector=>sector.Name.Length>0).OrderByDescending(sector=>sector.Name.Length).ToArray();
        var found=new List<(int Line,int At,uint Id)>();
        var lineIndex=0;
        foreach(var raw in lines)
        {
            var line=raw ?? "";
            var taken=new bool[line.Length];
            foreach(var (id,name) in names)
            {
                for(var at=line.IndexOf(name,StringComparison.Ordinal);at>=0;at=line.IndexOf(name,at+1,StringComparison.Ordinal))
                {
                    if(taken.AsSpan(at,name.Length).Contains(true))continue;
                    taken.AsSpan(at,name.Length).Fill(true);
                    found.Add((lineIndex,at,id));
                }
            }
            lineIndex++;
        }
        return found.OrderBy(match=>match.Line).ThenBy(match=>match.At).Select(match=>match.Id).Distinct().ToArray();
    }
}


/// <summary>An airship exploration sector (AirshipExplorationPoint row, game data) with its unlock (LuminaSupplemental) when known.</summary>
public sealed record AirshipSector(uint Id,string Name,string ShortName,int RankReq,int CeruleumTanks,int SurveyDistance,int SurveyMinutes,uint Exp,
    AirshipUnlock? Unlock);

/// <summary>What unlocks a sector: the sector whose voyage discovers it, the surveillance and the rank it asks (LuminaSupplemental AirshipUnlock).</summary>
public sealed record AirshipUnlock(uint UnlockedBy,int Surveillance,int Rank);

/// <summary>Airship sectors with the items that may drop there (LuminaSupplemental AirshipDrop: no quantity, no rate).</summary>
public sealed class AirshipCatalog
{
    public required IReadOnlyList<AirshipSector> Sectors { get; init; }
    public required IReadOnlyDictionary<uint,IReadOnlyList<uint>> Drops { get; init; }
    public AirshipSector? Find(uint id)=>Sectors.FirstOrDefault(sector=>sector.Id==id);
}

/// <summary>
/// DEC-023, point 3 (0.336): the airship unlock tree and possible loot of LuminaSupplemental (Critical-Impact, GPL-3.0, same licence as
/// Tensho), embedded as FreeCompany/Data/airship-unlock.csv and airship-drop.csv with their notice (docs/evidence/0.336-airship-sectors.md).
/// The drop list has no quantity and no rate: the value of an airship voyage stays unknown.
/// </summary>
public static class AirshipSectors
{
    /// <summary>AirshipUnlock rows: point, the point that unlocks it, surveillance and rank required. Comments (#) and the header are skipped.</summary>
    public static IReadOnlyDictionary<uint,AirshipUnlock> ReadUnlocks(TextReader reader)
    {
        var table=new Dictionary<uint,AirshipUnlock>();
        foreach(var cells in Rows(reader,4))
            if(uint.TryParse(cells[0],out var point) && uint.TryParse(cells[1],out var by) && int.TryParse(cells[2],out var surveillance) && int.TryParse(cells[3],out var rank))
                table[point]=new(by,surveillance,rank);
        return table;
    }

    /// <summary>AirshipDrop rows (item, point): the items seen per point, in file order, without duplicates.</summary>
    public static IReadOnlyDictionary<uint,IReadOnlyList<uint>> ReadDrops(TextReader reader)
    {
        var table=new Dictionary<uint,List<uint>>();
        foreach(var cells in Rows(reader,2))
            if(uint.TryParse(cells[0],out var item) && uint.TryParse(cells[1],out var point) && item!=0)
            {
                if(!table.TryGetValue(point,out var items))table[point]=items=[];
                if(!items.Contains(item))items.Add(item);
            }
        return table.ToDictionary(pair=>pair.Key,pair=>(IReadOnlyList<uint>)pair.Value);
    }

    /// <summary>X-201 (0.1243): the sectors where an item may drop (LuminaSupplemental AirshipDrop), in id order; empty when none is known.</summary>
    public static IReadOnlyList<uint> DropSectors(AirshipCatalog catalog,uint item)
        =>[.. catalog.Drops.Where(pair=>pair.Value.Contains(item) && catalog.Find(pair.Key) is not null).Select(pair=>pair.Key).Order()];

    /// <summary>X-201 (0.1243): the items that may drop somewhere (each once, in id order), for the loot goal's list.</summary>
    public static IReadOnlyList<uint> LootItems(AirshipCatalog catalog)
        =>[.. catalog.Drops.Where(pair=>catalog.Find(pair.Key) is not null).SelectMany(pair=>pair.Value).Distinct().Order()];

    /// <summary>
    /// X-201 (0.1243): the sector whose voyage unlocks this one (LuminaSupplemental AirshipUnlock), with the rank and surveillance it asks;
    /// null for a sector open from the start, unknown, or that names itself.
    /// </summary>
    public static (uint By,int Rank,int Surveillance)? Unlocker(AirshipCatalog catalog,uint sector)
        =>catalog.Find(sector)?.Unlock is { } unlock && unlock.UnlockedBy!=sector && catalog.Find(unlock.UnlockedBy) is not null
            ? (unlock.UnlockedBy,unlock.Rank,unlock.Surveillance):null;

    /// <summary>The chain of sectors to explore first, from the first one to the sector itself (a loop or an unknown link ends it).</summary>
    public static IReadOnlyList<uint> UnlockPath(AirshipCatalog catalog,uint sector)
    {
        var path=new List<uint>{sector};
        var current=sector;
        while(catalog.Find(current)?.Unlock is { } unlock && unlock.UnlockedBy!=current && !path.Contains(unlock.UnlockedBy) && catalog.Find(unlock.UnlockedBy) is not null)
        {
            current=unlock.UnlockedBy;
            path.Insert(0,current);
        }
        return path;
    }

    private static IEnumerable<string[]> Rows(TextReader reader,int columns)
    {
        string? line;
        while((line=reader.ReadLine()) is not null)
        {
            if(line.Length==0 || line.StartsWith('#'))continue;
            var cells=line.Split(',');
            if(cells.Length>=columns && char.IsDigit(cells[0].FirstOrDefault()))yield return cells;
        }
    }
}
