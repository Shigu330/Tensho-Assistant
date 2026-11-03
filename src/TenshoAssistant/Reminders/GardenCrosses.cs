namespace TenshoAssistant.Reminders;

/// <summary>Two seeds planted side by side that can give a third (item ids).</summary>
public sealed record GardenCross(uint Target,uint First,uint Second);

/// <summary>0.456: a pair and every seed it can give (the fewer the others, the better for the seed aimed at).</summary>
public sealed record GardenCrossPair(uint First,uint Second,IReadOnlyList<uint> Outcomes);

/// <summary>One bed of the plan: planting order from 1, its place in the 3 × 3 grid (the centre has no bed), its seed; the first one has no neighbour to cross with.</summary>
public sealed record GardenBedPlan(int Order,int Row,int Column,uint Seed,bool Crosses);

/// <summary>The crossbreeds Tensho could link to the game's items, and the source names it could not find in the English Item sheet.</summary>
public sealed record GardenCrossData(IReadOnlyList<GardenCross> Crosses,IReadOnlyList<string> Unmatched)
{
    /// <summary>Every seed named in the data (targets and parents).</summary>
    public IReadOnlyList<uint> Seeds { get; }=Crosses.SelectMany(cross=>new[]{cross.Target,cross.First,cross.Second}).Distinct().ToArray();

    /// <summary>The pairs that can give this seed.</summary>
    public IReadOnlyList<GardenCross> ProducersOf(uint seed)=>Crosses.Where(cross=>cross.Target==seed).ToArray();

    /// <summary>0.456: the pairs that give this seed, those that can give the fewest other seeds first.</summary>
    public IReadOnlyList<GardenCrossPair> BestPairsFor(uint seed)
    {
        var outcomes=Crosses.ToLookup(cross=>(cross.First,cross.Second),cross=>cross.Target);
        return ProducersOf(seed).Select(cross=>new GardenCrossPair(cross.First,cross.Second,outcomes[(cross.First,cross.Second)].Distinct().ToArray()))
            .OrderBy(pair=>pair.Outcomes.Count).ThenBy(pair=>pair.First).ThenBy(pair=>pair.Second).ToArray();
    }

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

    // The 8 beds of a Deluxe Garden Patch around its empty centre, clockwise from the top left bed.
    private static readonly (int Row,int Column)[] Ring=[(0,0),(0,1),(0,2),(1,2),(2,2),(2,1),(2,0),(1,0)];

    /// <summary>0.461: the grid cell of bed 1 to 8 (the same drawing as the plan); bed 1 at the top left, the rest clockwise, to compare in game.</summary>
    public static (int Row,int Column) BedCell(int bed)=>Ring[Math.Clamp(bed,1,8)-1];
    /// <summary>The bed drawn in a grid cell, or null for the empty centre.</summary>
    public static int? BedAt(int row,int column){var index=Array.IndexOf(Ring,(row,column));return index<0 ? null:index+1;}

    /// <summary>
    /// 0.456 (user request, in-game campaign): the planting plan of a pair on a Deluxe Garden Patch, from the Gardening Guide of the Console Games Wiki
    /// (docs/evidence/0.456-garden-grid.md): the two seeds alternate around the ring, so each new seed sits next to the other one; four of each;
    /// a seed crosses when it is planted, with a neighbour already there, so the first has none. The slower seed goes first (its crop stays longest).
    /// </summary>
    public static IReadOnlyList<GardenBedPlan> Plan(GardenCrossPair pair,Func<uint,int?> growHours)
    {
        var firstSeed=growHours(pair.Second)>growHours(pair.First) ? pair.Second:pair.First;
        var secondSeed=firstSeed==pair.First ? pair.Second:pair.First;
        return Ring.Select((bed,index)=>new GardenBedPlan(index+1,bed.Row,bed.Column,index%2==0 ? firstSeed:secondSeed,index>0)).ToArray();
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
