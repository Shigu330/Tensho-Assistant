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

/// <summary>0.461: where a bed sits: its place (« zone ward-plot »), its patch and its bed number, read from the journal's place text.</summary>
public sealed record GardenSlot(string Zone,int Patch,int Bed);

public static class GardenJournal
{
    private static readonly System.Text.RegularExpressions.Regex SlotText=new(@"^(.+) · \D*(\d+)\D+(\d+)$",System.Text.RegularExpressions.RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(50));

    /// <summary>0.461: the slot of a bed noted as « zone · patch N, bed M » (the format of the chat detection, in every language), or null for a free text or a pot.</summary>
    public static GardenSlot? Slot(GardenBed bed)
    {
        var match=SlotText.Match(bed.Place);
        if(!match.Success || !int.TryParse(match.Groups[2].Value,out var patch) || !int.TryParse(match.Groups[3].Value,out var number) || patch is <1 or >3 || number is <1 or >8)return null;
        return new(match.Groups[1].Value,patch,number);
    }

    /// <summary>0.461: the beds of each patch, by zone then patch: what the page draws as grids.</summary>
    public static IReadOnlyList<(string Zone,int Patch,IReadOnlyList<(int Number,GardenBed Bed)> Beds)> Grids(IEnumerable<GardenBed>? beds)
        =>(beds ?? []).Select(bed=>(Bed:bed,Slot:Slot(bed))).Where(pair=>pair.Slot is not null)
            .GroupBy(pair=>(pair.Slot!.Zone,pair.Slot.Patch)).OrderBy(group=>group.Key.Zone,StringComparer.CurrentCultureIgnoreCase).ThenBy(group=>group.Key.Patch)
            .Select(group=>(group.Key.Zone,group.Key.Patch,(IReadOnlyList<(int,GardenBed)>)group.Select(pair=>(pair.Slot!.Bed,pair.Bed)).ToArray())).ToArray();

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

    /// <summary>
    /// 0.616: the beds noted at a place (« zone ward-plot »), for a game line that does not say which bed: only one can be acted on, nothing is guessed among several.
    /// </summary>
    public static GardenBed[] At(IEnumerable<GardenBed>? beds,string? place)
        =>string.IsNullOrWhiteSpace(place) ? []:(beds ?? []).Where(bed=>bed.Place==place || bed.Place.StartsWith(place+" ·",StringComparison.Ordinal)).ToArray();

    public static void Tend(GardenBed bed,DateTimeOffset now){bed.TendedAt=now;bed.TendSaidFor=null;}

    /// <summary>
    /// The reminders due now, each said once: a bed that became ripe, or whose tending came due. Marks them said on the beds (the
    /// caller saves). A bed planted in the future (clock change) says nothing.
    /// </summary>
    public static IReadOnlyList<(GardenBed Bed,GardenState State)> Due(IEnumerable<GardenBed>? beds,DateTimeOffset now)
    {
        // X-163 (0.1153): the desktop application rings them too, by GardenBell (its own memory, the beds never changed).
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

/// <summary>
/// X-163 (0.1153, user's request of 12/11): the garden beds the desktop application rings, ripe or to tend, by the garden journal's own rule
/// (GardenJournal.State, TendAt): each once, in the application's own memory; a bed the plugin already said (RipeSaid, TendSaidFor in the saved
/// settings it reads) is not said again; the beds are never changed (the application never writes the plugin's settings). A bed gone from the
/// journal is forgotten, so the memory stays small. Not thread-safe: one caller (the application's bell, once a second).
/// </summary>
public sealed class GardenBell
{
    private readonly HashSet<(Guid Bed,GardenState State,DateTimeOffset At)> rung=[];

    public IReadOnlyList<(GardenBed Bed,GardenState State)> Due(IEnumerable<GardenBed>? beds,DateTimeOffset now)
    {
        var due=new List<(GardenBed,GardenState)>();
        var present=new HashSet<Guid>();
        foreach(var bed in beds ?? [])
        {
            present.Add(bed.Id);
            if(bed.PlantedAt>now)continue;
            var state=GardenJournal.State(bed,now);
            if(state==GardenState.Ripe && !bed.RipeSaid && rung.Add((bed.Id,state,GardenJournal.RipeAt(bed))))due.Add((bed,state));
            else if(state==GardenState.ToTend && GardenJournal.TendAt(bed) is { } at && bed.TendSaidFor!=at && rung.Add((bed.Id,state,at)))due.Add((bed,state));
        }
        rung.RemoveWhere(entry=>!present.Contains(entry.Bed));
        return due;
    }
}
