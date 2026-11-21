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
    /// <summary>X-383 (0.1360): the beds next to this one that were planted when it was (the crossbreed is decided then): their number and seed.
    /// Null: not kept (noted before 0.1360, or a place that is no patch's bed).</summary>
    public List<GardenNeighbour>? NeighboursAtPlanting { get; set; }
    /// <summary>X-581 (0.1572): what the game said of this bed the last time it was opened (its state line, GardenTalk), and when; None: never
    /// read. A new planting starts with None.</summary>
    public GardenGameState GameState { get; set; }
    public DateTimeOffset? GameStateAt { get; set; }
    /// <summary>X-628 M2 (0.1576): the game's own words of that line (one line, at most 200 characters), shown under « Unknown »; null: not kept
    /// (read before 0.1576).</summary>
    public string? GameStateText { get; set; }
}

/// <summary>X-383 (0.1360): a neighbour kept at a bed's planting: its bed number, its seed's item (0 = unknown, found by the name) and the name noted.</summary>
public sealed class GardenNeighbour
{
    public int Bed { get; set; }
    public uint SeedId { get; set; }
    public string Seed { get; set; } = "";
}

/// <summary>X-581 (0.1572): Unknown = whether the bed is ripe is not known: the game said it was still growing after the time expected for its
/// harvest, or that it withered. Never « Ripe » then. X-720 (0.1723): Ripe = the game said so; Expected = the calculated time has passed
/// and the game has not said yet: « ripe by the calculation, to confirm in game ».</summary>
public enum GardenState { Growing, ToTend, Ripe, Unknown, Expected }

/// <summary>0.461: where a bed sits: its place (« zone ward-plot »), its patch and its bed number, read from the journal's place text.</summary>
public sealed record GardenSlot(string Zone,int Patch,int Bed);

public static class GardenJournal
{
    /// <summary>
    /// X-428 (0.1414): the one way the garden's regular expressions run: .NET's linear engine (NonBacktracking), whose time grows with the
    /// input's length only, never by going back over it. X-427 (0.1407) had raised a 50 ms timeout to a second after the CI of 0.1405 ran
    /// out of time on a busy processor; a slow player's PC could still lose a planting that way. The same matches and groups as before
    /// (checked on the game's rows), and no timeout any more: <see cref="MatchTimeout"/> is infinite, nothing can run out.
    /// </summary>
    public const System.Text.RegularExpressions.RegexOptions Linear=System.Text.RegularExpressions.RegexOptions.CultureInvariant|System.Text.RegularExpressions.RegexOptions.NonBacktracking;
    /// <summary>X-428 (0.1414): infinite: the linear engine needs no timeout, so a busy processor never turns a reading into « nothing read ».</summary>
    public static readonly TimeSpan MatchTimeout=System.Text.RegularExpressions.Regex.InfiniteMatchTimeout;
    private static readonly System.Text.RegularExpressions.Regex SlotText=new(@"^(.+) · \D*(\d+)\D+(\d+)$",Linear,MatchTimeout);

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

    /// <summary>
    /// X-581 (0.1572, user report of 18/11): the growing times come from outside the game (Lotlab); on 17/11 four carrots were said ripe at 23:55
    /// by them, the game still said « se porte bien » at 00:13, and they were harvested the next day. X-720 (0.1723, user report of 21/11: the
    /// carrots planted on 18/11 at 13:00, ripe in the game at about 14:01 on 21/11, 72 h and an hour): « Ripe » only when the game said so;
    /// the planting time plus the seed's growing time passed without it: Expected (ripe by the calculation, to confirm in game); said growing
    /// at or after that time: Unknown.
    /// </summary>
    public static GardenState State(GardenBed bed,DateTimeOffset now)
    {
        if(bed.GameState==GardenGameState.Ripe)return GardenState.Ripe;
        if(Unknown(bed,now))return GardenState.Unknown;
        return now>=RipeAt(bed) ? GardenState.Expected:TendAt(bed) is { } due && now>=due ? GardenState.ToTend:GardenState.Growing;
    }

    /// <summary>X-720 (0.1723): the chat notice of a due reminder: ripe as the game said, ripe by the calculation (to confirm in game), or to tend.</summary>
    public static string NoticeKey(GardenState state)=>state switch{GardenState.Ripe=>"Garden.RipeNotice",GardenState.Expected=>"Garden.ExpectedNotice",_=>"Garden.TendNotice"};

    /// <summary>X-720 (0.1723, the probe): a bed for the developer's log: its patch and bed (never the place's name), seed item, planting time to
    /// the second, growing time and its source, tending, the expected harvest and how far now is from it (minutes), the game's last state.</summary>
    public static string Describe(GardenBed bed,DateTimeOffset now,string source)
    {
        static string Local(DateTimeOffset at)=>at.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz",System.Globalization.CultureInfo.InvariantCulture);
        var slot=Slot(bed);var ripe=RipeAt(bed);
        var gap=(long)Math.Floor((now-ripe).Ticks/(double)TimeSpan.TicksPerMinute);
        var game=bed.GameStateAt is { } at ? bed.GameState+" at "+Local(at):bed.GameState.ToString();
        return string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"patch {slot?.Patch ?? 0}, bed {slot?.Bed ?? 0}, seed item {bed.SeedId}, planted {Local(bed.PlantedAt)}, growing {ClampGrow(bed.GrowHours)} h ({source}), tending {ClampTend(bed.TendHours)} h, expected ripe {Local(ripe)} ({(gap>=0 ? "+":"")}{gap} min now), game {game}, shown {State(bed,now)}");
    }

    /// <summary>X-581 (0.1572): the bed's ripeness is unknown: withered, or said growing by the game at or after the expected harvest, now past.</summary>
    public static bool Unknown(GardenBed bed,DateTimeOffset now)
        =>bed.GameState==GardenGameState.Withered
          || bed.GameState is GardenGameState.Growing or GardenGameState.Wilting && now>=RipeAt(bed) && bed.GameStateAt is { } said && said>=RipeAt(bed);

    /// <summary>X-581 (0.1572): the game's state line read when the bed's menu opened (place, patch, bed): kept on that bed alone (the latest
    /// planted at that slot), or null when it is not noted (never another bed).</summary>
    public static GardenBed? Examined(IEnumerable<GardenBed>? beds,string? place,int patch,int number,GardenGameState state,DateTimeOffset now,string? line=null)
    {
        if(state==GardenGameState.None || CareTargets(beds,place,(patch,number)) is not [var bed])return null;
        bed.GameState=state;bed.GameStateAt=now;bed.GameStateText=GameLine(line);
        return bed;
    }

    /// <summary>X-628 M2 (0.1576): a game line kept as one line (line breaks and control characters made spaces, spaces folded), at most
    /// 200 characters, or null when empty.</summary>
    public static string? GameLine(string? text)
    {
        var clean=string.Join(' ',new string((text ?? "").Select(letter=>char.IsControl(letter) ? ' ':letter).ToArray()).Split(' ',StringSplitOptions.RemoveEmptyEntries));
        return clean.Length==0 ? null:clean.Length>200 ? clean[..200]:clean;
    }

    /// <summary>X-628 M1 (0.1576): the beds a harvest may be of: ripe, or of unknown ripeness but not withered (a withered crop is not
    /// harvested, docs/evidence/0.1572-garden-ripe.md).</summary>
    public static GardenBed[] HarvestCandidates(IEnumerable<GardenBed> beds,DateTimeOffset now)
        =>beds.Where(bed=>State(bed,now) is GardenState.Ripe or GardenState.Expected || State(bed,now) is GardenState.Unknown && bed.GameState!=GardenGameState.Withered).ToArray();

    /// <param name="among">X-383 (0.1360): the journal at that moment: the neighbours planted then are kept in the new bed (null: none kept).</param>
    public static GardenBed Plant(string place,string seed,int growHours,int tendHours,DateTimeOffset now,uint seedId=0,IEnumerable<GardenBed>? among=null)
        =>new(){Place=place,Seed=seed,SeedId=seedId,PlantedAt=now,GrowHours=ClampGrow(growHours),TendHours=ClampTend(tendHours),NeighboursAtPlanting=among is null ? null:NeighboursOf(among,place,now)};

    /// <summary>
    /// X-383 (0.1360, user report of 14/11): the beds next to a patch's bed that are planted at <paramref name="now"/> (right, down, up, left;
    /// the latest planted when a slot is noted twice), or null when the place is no patch's bed. The crossbreed is decided at planting
    /// (docs/evidence/0.456-garden-grid.md): this is what its forecast reads later, whatever happens to the neighbours since.
    /// </summary>
    public static List<GardenNeighbour>? NeighboursOf(IEnumerable<GardenBed> beds,string place,DateTimeOffset now)
    {
        if(Slot(new GardenBed{Place=place}) is not { } slot)return null;
        var neighbours=new List<GardenNeighbour>();
        var around=beds.Select(bed=>(Bed:bed,Slot:Slot(bed))).Where(pair=>pair.Slot is { } seen && seen.Zone==slot.Zone && seen.Patch==slot.Patch && pair.Bed.PlantedAt<=now).ToArray();
        foreach(var number in GardenForecasts.Neighbours(slot.Bed))
            if(around.Where(pair=>pair.Slot!.Bed==number).OrderByDescending(pair=>pair.Bed.PlantedAt).Select(pair=>pair.Bed).FirstOrDefault() is { } bed)
                neighbours.Add(new(){Bed=number,SeedId=bed.SeedId,Seed=bed.Seed});
        return neighbours;
    }

    /// <summary>
    /// 0.460: a planting seen in the chat: replaces the bed noted at the same place (it was dug up or harvested), or null when the journal is full.
    /// X-185 (0.1211, N24): « the same place » is also the same slot (zone, patch, bed) noted in another language of the plugin.
    /// </summary>
    public static List<GardenBed>? Detected(IEnumerable<GardenBed>? beds,string place,string seed,int growHours,int tendHours,DateTimeOffset now,uint seedId=0)
    {
        var slot=Slot(new GardenBed{Place=place});
        var kept=(beds ?? []).Where(bed=>bed.Place!=place && (slot is null || Slot(bed)!=slot)).ToList();
        if(kept.Count>=Capacity)return null;
        kept.Add(Plant(place,seed,growHours,tendHours,now,seedId,kept));
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
            if(RipeAt(bed) is var ripe && ripe>now && bed.GameState!=GardenGameState.Ripe)alarms.Add((bed,GardenState.Ripe,ripe));
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
            // X-720 (0.1723): ripe by the calculation is said too, as such (NoticeKey); once said, not again when the game says ripe.
            if(state is GardenState.Ripe or GardenState.Expected && !bed.RipeSaid){bed.RipeSaid=true;due.Add((bed,state));}
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
            if(state is GardenState.Ripe or GardenState.Expected && !bed.RipeSaid && rung.Add((bed.Id,GardenState.Ripe,GardenJournal.RipeAt(bed))))due.Add((bed,state));
            else if(state==GardenState.ToTend && GardenJournal.TendAt(bed) is { } at && bed.TendSaidFor!=at && rung.Add((bed.Id,state,at)))due.Add((bed,state));
        }
        rung.RemoveWhere(entry=>!present.Contains(entry.Bed));
        return due;
    }
}
