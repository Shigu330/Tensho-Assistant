namespace TenshoAssistant.Maps;

/// <summary>The game's five kinds of island resource, its own compass options; the values are MJIGatheringObject's category (docs/evidence/0.684-island-kinds.md).</summary>
public enum IslandResourceKind { Wood=1,Plants=2,Earth=4,Rocks=8,Fishing=16 }

/// <summary>N-07 (0.667): a place of the Island Sanctuary with what can be gathered there (MJIGatheringItem, docs/evidence/0.667-island-resources.md). Kind null: an item Tensho does not know yet.</summary>
public sealed record IslandPoint(int X,int Y,int Radius,IReadOnlyList<uint> Items,IslandResourceKind? Kind=null);

/// <summary>0.684 (user report): something named that stands on the island, from the Level sheet.</summary>
public sealed record IslandPlace(float X,float Z,string Name);

public static class IslandResources
{
    /// <summary>TerritoryType 1055, « h1m2 », map 773.</summary>
    public const uint Territory=1055;
    /// <summary>Places of one kind this close are one on the map: the sheet has sand at 236;88 and clay at 236;89.</summary>
    public const int SameSpot=3;

    private static readonly Dictionary<uint,IslandResourceKind> Kinds=new()
    {
        // Trees (tualong, palm, apple, mahogany).
        [37551]=IslandResourceKind.Wood,[37552]=IslandResourceKind.Wood,[37553]=IslandResourceKind.Wood,[37560]=IslandResourceKind.Wood,[37561]=IslandResourceKind.Wood,[37562]=IslandResourceKind.Wood,
        [37563]=IslandResourceKind.Wood,[39224]=IslandResourceKind.Wood,[39225]=IslandResourceKind.Wood,[39226]=IslandResourceKind.Wood,[39227]=IslandResourceKind.Wood,
        // Plants (cabbage, pumpkin, popoto, parsnip, cane, cotton, sisal, flowers, mushrooms).
        [37558]=IslandResourceKind.Plants,[37567]=IslandResourceKind.Plants,[37568]=IslandResourceKind.Plants,[37569]=IslandResourceKind.Plants,[37583]=IslandResourceKind.Plants,[37584]=IslandResourceKind.Plants,
        [37586]=IslandResourceKind.Plants,[37591]=IslandResourceKind.Plants,[39228]=IslandResourceKind.Plants,[39889]=IslandResourceKind.Plants,
        // Earth and sunken sand.
        [37559]=IslandResourceKind.Earth,[37570]=IslandResourceKind.Earth,[37571]=IslandResourceKind.Earth,
        // Rocks, and what shares their places.
        [37554]=IslandResourceKind.Rocks,[37564]=IslandResourceKind.Rocks,[37565]=IslandResourceKind.Rocks,[37566]=IslandResourceKind.Rocks,[37572]=IslandResourceKind.Rocks,[37573]=IslandResourceKind.Rocks,
        [37574]=IslandResourceKind.Rocks,[39887]=IslandResourceKind.Rocks,[39888]=IslandResourceKind.Rocks,[39890]=IslandResourceKind.Rocks,[39891]=IslandResourceKind.Rocks,[39892]=IslandResourceKind.Rocks,
        [39893]=IslandResourceKind.Rocks,[41630]=IslandResourceKind.Rocks,[41631]=IslandResourceKind.Rocks,[41632]=IslandResourceKind.Rocks,[41633]=IslandResourceKind.Rocks,[41634]=IslandResourceKind.Rocks,
        // The sea: shells, seaweed, coral, fish.
        [37555]=IslandResourceKind.Fishing,[37556]=IslandResourceKind.Fishing,[37557]=IslandResourceKind.Fishing,[37575]=IslandResourceKind.Fishing,[37576]=IslandResourceKind.Fishing,[37577]=IslandResourceKind.Fishing,
    };

    public static IslandResourceKind? KindOf(uint item)=>Kinds.TryGetValue(item,out var kind) ? kind:null;

    /// <summary>Several items share a place: one point each, and places of one kind at the same spot make one. Rows without an item or a place are left out.</summary>
    public static IReadOnlyList<IslandPoint> Points(IEnumerable<(uint Item,int X,int Y,int Radius)> rows)
    {
        var points=new List<IslandPoint>();
        foreach(var group in rows.Where(row=>row.Item!=0 && (row.X!=0 || row.Y!=0)).GroupBy(row=>(row.X,row.Y)))
        {
            var items=group.Select(row=>row.Item).Distinct().ToArray();
            var kind=items.Select(KindOf).FirstOrDefault(found=>found is not null);
            var near=points.FindIndex(point=>point.Kind==kind && Math.Abs(point.X-group.Key.X)<=SameSpot && Math.Abs(point.Y-group.Key.Y)<=SameSpot);
            var radius=group.Max(row=>row.Radius);
            if(near<0)points.Add(new(group.Key.X,group.Key.Y,radius,items,kind));
            else points[near]=points[near] with{Radius=Math.Max(points[near].Radius,radius),Items=points[near].Items.Concat(items).Distinct().ToArray()};
        }
        return points;
    }

    /// <summary>The nearest named thing within <paramref name="limit"/> world units, with its first letter raised: the sheets write « panneau de construction ».</summary>
    public static string? NearestName(IReadOnlyList<IslandPlace> places,float x,float z,float limit=3)
    {
        var best=limit*limit;IslandPlace? found=null;
        foreach(var place in places)
        {
            var distance=(place.X-x)*(place.X-x)+(place.Z-z)*(place.Z-z);
            if(distance<=best && place.Name.Length>0){best=distance;found=place;}
        }
        return found is null ? null:char.ToUpperInvariant(found.Name[0])+found.Name[1..];
    }
}
