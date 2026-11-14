namespace TenshoAssistant.Reminders;

public enum GardenForecastKind { Crosses, NoPartner, UnknownSeed, UnknownNeighbour, NotKept }

/// <param name="Bed">The bed (1 to 8).</param>
/// <param name="Partner">The neighbour it crosses with (Crosses), or the neighbour whose seed is unknown (UnknownNeighbour).</param>
/// <param name="Outcomes">The seeds the pair may give (Crosses), in id order.</param>
/// <param name="PartnerName">X-383: the partner's seed as noted when this bed was planted (null: read from the journal of now).</param>
public sealed record GardenForecast(int Bed,GardenForecastKind Kind,int? Partner,uint PartnerSeed,IReadOnlyList<uint> Outcomes,string? PartnerName=null);

/// <summary>
/// X-383 (0.1360, user report of 14/11): a planted bed for the forecast. <paramref name="AtPlanting"/>: its neighbours as they were when it was
/// planted (bed, seed id or null when unknown, name), kept in the journal (GardenBed.NeighboursAtPlanting); null for a bed noted before 0.1360.
/// </summary>
public sealed record GardenForecastBed(int Bed,uint? Seed,DateTimeOffset PlantedAt,IReadOnlyList<(int Bed,uint? Seed,string Name)>? AtPlanting);

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
        =>For(beds.Select(entry=>new GardenForecastBed(entry.Bed,entry.Seed,entry.PlantedAt,null)).ToArray(),pairs,known,strict:false);

    /// <summary>
    /// X-383 (0.1360, user report of 14/11: beds 2, 4, 6, 8 removed then replanted, the crossbreeds of beds 3, 5, 7 planted before them vanished):
    /// the crossbreed is decided when the seed is planted (docs/evidence/0.456-garden-grid.md, Console Games Wiki), so a bed's forecast reads
    /// the neighbours kept at its planting, never the journal of now: removing or replanting a neighbour afterwards changes nothing. A bed noted
    /// before 0.1360 kept none: the journal of now is read, and a neighbour met empty or planted after it, before any partner, could have held the
    /// partner since removed (<paramref name="strict"/>): « not known » (NotKept), never « no crossbreed » (R06).
    /// </summary>
    public static IReadOnlyList<GardenForecast> For(IReadOnlyList<GardenForecastBed> beds,IReadOnlyDictionary<(uint,uint),uint[]> pairs,Func<uint,bool> known,bool strict=true)
    {
        var forecasts=new List<GardenForecast>();
        foreach(var entry in beds.OrderBy(entry=>entry.Bed))
        {
            var bed=entry.Bed;
            if(entry.Seed is not { } own || !known(own)){forecasts.Add(new(bed,GardenForecastKind.UnknownSeed,null,0,[]));continue;}
            GardenForecast? found=null;
            foreach(var number in Neighbours(bed))
            {
                uint? other;string? name=null;
                if(entry.AtPlanting is { } kept)
                {
                    var seen=kept.FirstOrDefault(neighbour=>neighbour.Bed==number);
                    if(seen.Bed!=number)continue;
                    other=seen.Seed;name=seen.Name;
                }
                else
                {
                    var neighbour=beds.FirstOrDefault(candidate=>candidate.Bed==number);
                    if(neighbour is null || neighbour.PlantedAt>=entry.PlantedAt)
                    {
                        if(strict){found=new(bed,GardenForecastKind.NotKept,null,0,[]);break;}
                        continue;
                    }
                    other=neighbour.Seed;
                }
                if(other is not { } partner || !known(partner)){found=new(bed,GardenForecastKind.UnknownNeighbour,number,0,[],name);break;}
                if(partner==own || !pairs.TryGetValue((Math.Min(own,partner),Math.Max(own,partner)),out var outcomes) || outcomes.Length==0)continue;
                found=new(bed,GardenForecastKind.Crosses,number,partner,outcomes,name);
                break;
            }
            forecasts.Add(found ?? new(bed,GardenForecastKind.NoPartner,null,0,[]));
        }
        return forecasts;
    }
}
