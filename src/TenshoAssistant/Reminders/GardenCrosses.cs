namespace TenshoAssistant.Reminders;

/// <summary>Two seeds planted side by side that can give a third (item ids).</summary>
public sealed record GardenCross(uint Target,uint First,uint Second);

/// <summary>The crossbreeds Tensho could link to the game's items, and the source names it could not find in the English Item sheet.</summary>
public sealed record GardenCrossData(IReadOnlyList<GardenCross> Crosses,IReadOnlyList<string> Unmatched)
{
    /// <summary>Every seed named in the data (targets and parents).</summary>
    public IReadOnlyList<uint> Seeds { get; }=Crosses.SelectMany(cross=>new[]{cross.Target,cross.First,cross.Second}).Distinct().ToArray();

    /// <summary>The pairs that can give this seed.</summary>
    public IReadOnlyList<GardenCross> ProducersOf(uint seed)=>Crosses.Where(cross=>cross.Target==seed).ToArray();

    /// <summary>What this seed can give, with the seed to plant next to it.</summary>
    public IReadOnlyList<(uint Partner,uint Target)> UsesOf(uint seed)
        =>Crosses.Where(cross=>cross.First==seed || cross.Second==seed).Select(cross=>(cross.First==seed ? cross.Second:cross.First,cross.Target)).Distinct().ToArray();
}

/// <summary>
/// N-14 (0.450, DEC-027, point 2): the crossbreeding pairs of nick75g/FFXIV-Crossbreed-Helper (MIT), embedded as
/// Reminders/Data/garden-crosses.csv (tools/garden-data.py). The source names seeds in English only: each name is linked to an item id
/// through the game's English Item sheet, and Tensho shows the item in the game's language. A name the sheet does not know is not
/// guessed: its rows are left out and the name is reported.
/// </summary>
public static class GardenCrosses
{
    public static GardenCrossData Read(TextReader reader,Func<string,uint?> englishItem)
    {
        var crosses=new List<GardenCross>();
        var seen=new HashSet<(uint,uint,uint)>();
        var unmatched=new SortedSet<string>(StringComparer.Ordinal);
        while(reader.ReadLine() is { } line)
        {
            if(line.StartsWith('#') || line.StartsWith("target\t",StringComparison.Ordinal))continue;
            var cells=line.Split('\t');
            if(cells.Length!=3 || cells.Any(cell=>cell.Trim().Length==0))continue;
            var ids=cells.Select(cell=>englishItem(cell.Trim())).ToArray();
            for(var index=0;index<3;index++)if(ids[index] is null or 0)unmatched.Add(cells[index].Trim());
            if(ids.Any(id=>id is null or 0))continue;
            var (first,second)=ids[1]!.Value<=ids[2]!.Value ? (ids[1]!.Value,ids[2]!.Value):(ids[2]!.Value,ids[1]!.Value);
            if(seen.Add((ids[0]!.Value,first,second)))crosses.Add(new(ids[0]!.Value,first,second));
        }
        return new(crosses,unmatched.ToArray());
    }

    /// <summary>English item names to ids: exact name, case ignored; the lowest id wins when several items share a name.</summary>
    public static Func<string,uint?> EnglishIndex(IEnumerable<(uint Id,string Name)> items)
    {
        var index=new Dictionary<string,uint>(StringComparer.OrdinalIgnoreCase);
        foreach(var (id,name) in items.OrderBy(item=>item.Id))
            if(id!=0 && name.Length>0)index.TryAdd(name,id);
        return name=>index.TryGetValue(name,out var id) ? id:null;
    }
}
