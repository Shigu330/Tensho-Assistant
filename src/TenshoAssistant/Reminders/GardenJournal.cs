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
    /// <summary>X-178 (0.1256): the seed's item, noted when the planting is read from the chat or the seed picked in the list; 0 = unknown
    /// (typed by hand, or noted before 0.1256): the icon is then found by the name.</summary>
    public uint SeedId { get; set; }
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

    public static GardenBed Plant(string place,string seed,int growHours,int tendHours,DateTimeOffset now,uint seedId=0)
        =>new(){Place=place,Seed=seed,SeedId=seedId,PlantedAt=now,GrowHours=ClampGrow(growHours),TendHours=ClampTend(tendHours)};

    /// <summary>
    /// 0.460: a planting seen in the chat: replaces the bed noted at the same place (it was dug up or harvested), or null when the journal is full.
    /// X-185 (0.1211, N24): « the same place » is also the same slot (zone, patch, bed) noted in another language of the plugin.
    /// </summary>
    public static List<GardenBed>? Detected(IEnumerable<GardenBed>? beds,string place,string seed,int growHours,int tendHours,DateTimeOffset now,uint seedId=0)
    {
        var slot=Slot(new GardenBed{Place=place});
        var kept=(beds ?? []).Where(bed=>bed.Place!=place && (slot is null || Slot(bed)!=slot)).ToList();
        if(kept.Count>=Capacity)return null;
        kept.Add(Plant(place,seed,growHours,tendHours,now,seedId));
        return kept;
    }

    /// <summary>
    /// 0.616: the beds noted at a place (« zone ward-plot »), for a game line that does not say which bed: only one can be acted on, nothing is guessed among several.
    /// </summary>
    public static GardenBed[] At(IEnumerable<GardenBed>? beds,string? place)
        =>string.IsNullOrWhiteSpace(place) ? []:(beds ?? []).Where(bed=>bed.Place==place || bed.Place.StartsWith(place+" ·",StringComparison.Ordinal)).ToArray();

    public static void Tend(GardenBed bed,DateTimeOffset now){bed.TendedAt=now;bed.TendSaidFor=null;}

    /// <summary>
    /// X-178 (0.1263, user request of 13/11: up to three patches at a large house): « whole patch tended » for one patch of one place: every
    /// bed of that patch that waits for a tending is tended now; the other patches of the same place, and the other places, are left alone.
    /// The number of beds tended.
    /// </summary>
    public static int TendPatch(IEnumerable<GardenBed>? beds,string zone,int patch,DateTimeOffset now)
    {
        var tended=0;
        foreach(var bed in beds ?? [])
        {
            if(Slot(bed) is not { } slot || slot.Zone!=zone || slot.Patch!=patch || TendAt(bed) is null)continue;
            Tend(bed,now);tended++;
        }
        return tended;
    }

    /// <summary>
    /// X-179 (0.1165): the beds a care line (tending, uprooting, harvest) may be about. With the bed read in the game's menu: that bed alone, at that
    /// place and in that patch, or none when it is not noted (never another bed). Without it: every bed noted at the place (the caller acts only on one).
    /// X-185 (0.1211, N24): two beds noted at the same slot (one noted in another language of the plugin): the latest planted, never the older one.
    /// </summary>
    public static GardenBed[] CareTargets(IEnumerable<GardenBed>? beds,string? place,(int Patch,int Bed)? menu)
    {
        var here=At(beds,place);
        if(menu is not { } slot)return here;
        return here.Where(bed=>Slot(bed) is { } seen && seen.Zone==place && seen.Patch==slot.Patch && seen.Bed==slot.Bed).OrderByDescending(bed=>bed.PlantedAt).Take(1).ToArray();
    }

    /// <summary>
    /// X-132 (0.1160, user request of 11/11): the garden's alarms still to ring, as the Garden tool's « Alarms » tab lists them: per bed its
    /// next tending (when it comes before the harvest) and its harvest, ordered by time, then by place. A passed one is not listed (it was said
    /// in the chat; the journal shows the bed's state); a bed planted in the future (clock change) neither.
    /// </summary>
    public static IReadOnlyList<(GardenBed Bed,GardenState Kind,DateTimeOffset At)> Alarms(IEnumerable<GardenBed>? beds,DateTimeOffset now)
    {
        var alarms=new List<(GardenBed Bed,GardenState Kind,DateTimeOffset At)>();
        foreach(var bed in beds ?? [])
        {
            if(bed.PlantedAt>now)continue;
            if(TendAt(bed) is { } tend && tend>now)alarms.Add((bed,GardenState.ToTend,tend));
            if(RipeAt(bed) is var ripe && ripe>now)alarms.Add((bed,GardenState.Ripe,ripe));
        }
        return [.. alarms.OrderBy(alarm=>alarm.At).ThenBy(alarm=>alarm.Bed.Place,StringComparer.Ordinal)];
    }

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
