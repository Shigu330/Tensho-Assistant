namespace TenshoAssistant.Reminders;

/// <summary>
/// N-08 (0.448, DEC-026): one garden bed or pot the player noted. The game's sheets give neither the growing time nor the tending
/// interval, so both are typed by the player (hours); nothing is guessed. Tending 0 = no tending reminder.
/// </summary>
public sealed class GardenBed
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Place { get; set; } = "";
    public string Seed { get; set; } = "";
    public DateTimeOffset PlantedAt { get; set; }
    public int GrowHours { get; set; } = 1;
    public int TendHours { get; set; }
    public DateTimeOffset? TendedAt { get; set; }
    // Reminders already said (kept on disk, so a restart does not repeat them): the harvest, and the tending due time.
    public bool RipeSaid { get; set; }
    public DateTimeOffset? TendSaidFor { get; set; }
}

public enum GardenState { Growing, ToTend, Ripe }

public static class GardenJournal
{
    public const int Capacity=64,MaximumText=40,MaximumGrowHours=720,MaximumTendHours=168;

    public static int ClampGrow(int hours)=>Math.Clamp(hours,1,MaximumGrowHours);
    public static int ClampTend(int hours)=>Math.Clamp(hours,0,MaximumTendHours);

    /// <summary>A trimmed text of 1 to 40 characters without control characters, or null.</summary>
    public static string? CleanText(string? text)
    {
        var clean=(text ?? "").Trim();
        return clean.Length is 0 or > MaximumText || clean.Any(char.IsControl) ? null:clean;
    }

    public static DateTimeOffset RipeAt(GardenBed bed)=>bed.PlantedAt.AddHours(ClampGrow(bed.GrowHours));

    /// <summary>When the bed needs tending next (null without a tending interval, or once it is ripe).</summary>
    public static DateTimeOffset? TendAt(GardenBed bed)
    {
        if(ClampTend(bed.TendHours)==0)return null;
        var due=(bed.TendedAt ?? bed.PlantedAt).AddHours(ClampTend(bed.TendHours));
        return due<RipeAt(bed) ? due:null;
    }

    public static GardenState State(GardenBed bed,DateTimeOffset now)
        =>now>=RipeAt(bed) ? GardenState.Ripe:TendAt(bed) is { } due && now>=due ? GardenState.ToTend:GardenState.Growing;

    public static GardenBed Plant(string place,string seed,int growHours,int tendHours,DateTimeOffset now)
        =>new(){Place=place,Seed=seed,PlantedAt=now,GrowHours=ClampGrow(growHours),TendHours=ClampTend(tendHours)};

    /// <summary>0.460: a planting seen in the chat: replaces the bed noted at the same place (it was dug up or harvested), or null when the journal is full.</summary>
    public static List<GardenBed>? Detected(IEnumerable<GardenBed>? beds,string place,string seed,int growHours,int tendHours,DateTimeOffset now)
    {
        var kept=(beds ?? []).Where(bed=>bed.Place!=place).ToList();
        if(kept.Count>=Capacity)return null;
        kept.Add(Plant(place,seed,growHours,tendHours,now));
        return kept;
    }

    public static void Tend(GardenBed bed,DateTimeOffset now){bed.TendedAt=now;bed.TendSaidFor=null;}

    /// <summary>The same seed planted again in the same place now (reminders start over).</summary>
    public static void Replant(GardenBed bed,DateTimeOffset now){bed.PlantedAt=now;bed.TendedAt=null;bed.RipeSaid=false;bed.TendSaidFor=null;}

    /// <summary>
    /// The reminders due now, each said once: a bed that became ripe, or whose tending came due. Marks them said on the beds (the
    /// caller saves). A bed planted in the future (clock change) says nothing.
    /// </summary>
    public static IReadOnlyList<(GardenBed Bed,GardenState State)> Due(IEnumerable<GardenBed>? beds,DateTimeOffset now)
    {
        var due=new List<(GardenBed,GardenState)>();
        foreach(var bed in beds ?? [])
        {
            if(bed.PlantedAt>now)continue;
            var state=State(bed,now);
            if(state==GardenState.Ripe && !bed.RipeSaid){bed.RipeSaid=true;due.Add((bed,state));}
            else if(state==GardenState.ToTend && TendAt(bed) is { } at && bed.TendSaidFor!=at){bed.TendSaidFor=at;due.Add((bed,state));}
        }
        return due;
    }
}
