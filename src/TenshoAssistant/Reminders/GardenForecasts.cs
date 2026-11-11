namespace TenshoAssistant.Reminders;

public enum GardenForecastKind { Crosses, NoPartner, UnknownSeed, UnknownNeighbour }

/// <param name="Bed">The bed (1 to 8).</param>
/// <param name="Partner">The neighbour it crosses with (Crosses), or the neighbour whose seed is unknown (UnknownNeighbour).</param>
/// <param name="Outcomes">The seeds the pair may give (Crosses), in id order.</param>
public sealed record GardenForecast(int Bed,GardenForecastKind Kind,int? Partner,uint PartnerSeed,IReadOnlyList<uint> Outcomes);

/// <summary>
/// X-130 (0.1109, user request of 2026-11-11): what each planted bed of a Deluxe Garden Patch may give, from the journal. Rules of the
/// Console Games Wiki (docs/evidence/0.456-garden-grid.md, not from the game, not checked by Tensho): the crossbreed is decided when the seed
/// is planted, with the first compatible neighbour already there, looked at right, down, up, left; an empty bed, the same seed or a pair
/// that gives nothing is skipped. Pairs from nick75g/FFXIV-Crossbreed-Helper (GardenCrosses). A seed missing from the pairs is said, never
/// guessed; an unknown neighbour met before a compatible one stops the forecast (it could be the one the seed crosses with).
/// </summary>
public static class GardenForecasts
{
    private static readonly (int Row,int Column)[] Order=[(0,1),(1,0),(-1,0),(0,-1)];

    /// <summary>The beds next to this one (no diagonal, no centre), in the order the game tries them: right, down, up, left.</summary>
    public static IReadOnlyList<int> Neighbours(int bed)
    {
        var (row,column)=GardenCrosses.BedCell(bed);
        return Order.Select(step=>GardenCrosses.BedAt(row+step.Row,column+step.Column)).OfType<int>().ToArray();
    }

    /// <summary>The pairs (lower id first) and the seeds each may give, built once per catalogue.</summary>
    public static IReadOnlyDictionary<(uint,uint),uint[]> Index(GardenCrossData data)
        =>data.Crosses.GroupBy(cross=>(Math.Min(cross.First,cross.Second),Math.Max(cross.First,cross.Second)))
            .ToDictionary(group=>group.Key,group=>group.Select(cross=>cross.Target).Distinct().Order().ToArray());

    /// <param name="beds">The patch's planted beds: bed number, the seed's id (null when its name matches no seed) and when it was planted.</param>
    /// <param name="known">The seeds named in the pairs.</param>
    public static IReadOnlyList<GardenForecast> For(IReadOnlyList<(int Bed,uint? Seed,DateTimeOffset PlantedAt)> beds,IReadOnlyDictionary<(uint,uint),uint[]> pairs,Func<uint,bool> known)
    {
        var forecasts=new List<GardenForecast>();
        foreach(var (bed,seed,plantedAt) in beds.OrderBy(entry=>entry.Bed))
        {
            if(seed is not { } own || !known(own)){forecasts.Add(new(bed,GardenForecastKind.UnknownSeed,null,0,[]));continue;}
            GardenForecast? found=null;
            foreach(var number in Neighbours(bed))
            {
                var neighbour=beds.FirstOrDefault(entry=>entry.Bed==number);
                if(neighbour.Bed!=number || neighbour.PlantedAt>=plantedAt)continue;
                if(neighbour.Seed is not { } other || !known(other)){found=new(bed,GardenForecastKind.UnknownNeighbour,number,0,[]);break;}
                if(other==own || !pairs.TryGetValue((Math.Min(own,other),Math.Max(own,other)),out var outcomes) || outcomes.Length==0)continue;
                found=new(bed,GardenForecastKind.Crosses,number,other,outcomes);
                break;
            }
            forecasts.Add(found ?? new(bed,GardenForecastKind.NoPartner,null,0,[]));
        }
        return forecasts;
    }
}
