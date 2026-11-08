namespace TenshoAssistant.FreeCompany;

/// <summary>
/// T26 (0.919): the route of an airship's last voyage, rebuilt from the game's voyage log (WorkshopTerritory.Airship.AirshipLogs: lines such as
/// « Mettons le cap vers “Mer de nuages - secteur 1” », docs/evidence/0.919-airship-reading-2026-11-08.log). Each line is searched for the
/// sector names of AirshipExplorationPoint (client language), longest first so « … secteur 1 » never matches inside « … secteur 10 »; the
/// sectors are kept in the order they first appear. Unknown text is ignored, nothing is guessed.
/// </summary>
public static class AirshipLogRoute
{
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
