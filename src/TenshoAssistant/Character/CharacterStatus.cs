using System.Globalization;
using System.Text.Json;

namespace TenshoAssistant.Character;

/// <summary>One retainer's venture as last read (Done = when it ends; null = no venture).</summary>
public sealed record VentureState(string Retainer,DateTimeOffset? Done);

/// <summary>
/// X-196 (0.1409): one retainer of the character as last read at a summoning bell (RetainerManager, once the retainer list was opened): job
/// (abbreviation in the client's language, as read), level, gil, items held (the game's count), items on the market and when their sales
/// end, the venture and its end, the market town (Town sheet, as read); its stock in the game's item finder copy (X-307: stacks, HQ stacks,
/// crystals; null when the copy has no entry for it). No retainer id is kept: the copy is joined at the reading only.
/// </summary>
public sealed record RetainerState
{
    public string Name { get; init; } = "";
    public string Job { get; init; } = "";
    public int Level { get; init; }
    public long Gil { get; init; }
    public int Items { get; init; }
    public int MarketItems { get; init; }
    public DateTimeOffset? MarketEnds { get; init; }
    public uint Venture { get; init; }
    public DateTimeOffset? VentureDone { get; init; }
    public string Town { get; init; } = "";
    public int? Stacks { get; init; }
    public int? HqStacks { get; init; }
    public int? Crystals { get; init; }
    // X-469 (0.1762, D-174): its items on sale as last read in the game's sale list (null: never read), and when.
    public RetainerSaleItem[]? Listed { get; init; }
    public DateTimeOffset? ListedAt { get; init; }
}

/// <summary>X-469 (0.1762, D-174, the owner's choice of 15/11/2026 at about 10:30 pm): one item a retainer has on sale, as the game's sale list
/// showed it (RetainerSalesReader: item, HQ, quantity, unit price; null: not read).</summary>
public sealed record RetainerSaleItem(uint ItemId,bool Hq,int Quantity,long? UnitPrice);

/// <summary>X-196 (0.1409): the game's values of a retainer as Tensho keeps them (a zero time is « none », texts bounded).</summary>
public static class RetainerStates
{
    public const int MaximumText=40;

    /// <summary>X-469 (0.1762): a retainer sells 20 items at most (the game's sale list).</summary>
    public const int MaximumListed=20;

    /// <summary>X-469: the sale list's rows kept (an item and a quantity; 20 at most, in the list's order).</summary>
    public static RetainerSaleItem[] Listing(IEnumerable<(uint ItemId,bool Hq,int Quantity,long? UnitPrice)> rows)=>
        [.. rows.Where(row=>row.ItemId!=0 && row.Quantity>0).Take(MaximumListed).Select(row=>new RetainerSaleItem(row.ItemId,row.Hq,row.Quantity,row.UnitPrice is >= 0 ? row.UnitPrice:null))];

    /// <summary>X-469: the same listing of the same open retainer gives the same key (read again only when it changes or the list reopens).</summary>
    public static string ListingKey(ulong retainer,IEnumerable<RetainerSaleItem> listed)=>
        retainer.ToString(CultureInfo.InvariantCulture)+":"+string.Join(";",listed.Select(item=>FormattableString.Invariant($"{item.ItemId},{item.Hq},{item.Quantity},{item.UnitPrice}")));

    /// <summary>X-469: the retainers with this one's listing (by its name, unique among a character's retainers) read at <paramref name="at"/>;
    /// null when no retainer has that name (its list not read at a summoning bell yet).</summary>
    public static RetainerState[]? WithListing(RetainerState[] retainers,string name,RetainerSaleItem[] listed,DateTimeOffset at)
    {
        var index=Array.FindIndex(retainers,retainer=>retainer.Name==Text(name));
        if(index<0)return null;
        var next=(RetainerState[])retainers.Clone();
        next[index]=next[index] with{Listed=listed,ListedAt=at};
        return next;
    }

    /// <summary>
    /// X-469: a fresh reading at a summoning bell (no listing in it) keeps each retainer's listing as last read (by name); a retainer with
    /// nothing on the market has none for sure (an empty listing, dated once: read again it is the same, so nothing to save); one selling
    /// again after an empty listing is not read yet.
    /// </summary>
    public static RetainerState[]? KeepListings(RetainerState[]? old,RetainerState[]? fresh,DateTimeOffset at)
    {
        if(fresh is null)return null;
        return [.. fresh.Select(retainer=>
        {
            if(retainer.Listed is not null)return retainer;
            var known=old?.FirstOrDefault(kept=>kept.Name==retainer.Name);
            if(NothingOnSale(retainer.MarketItems))
                return known?.Listed is { Length:0 } empty ? retainer with{Listed=empty,ListedAt=known.ListedAt}:retainer with{Listed=[],ListedAt=at};
            // Something on sale again after an empty listing: that listing says nothing of it (not read since).
            return known?.Listed is { Length:>0 } listed ? retainer with{Listed=listed,ListedAt=known.ListedAt}:retainer;
        })];
    }

    public static RetainerState From(string name,string job,int level,long gil,int items,int marketItems,uint marketExpire,uint venture,uint ventureComplete,string town,
        (int Stacks,int HqStacks,int Crystals)? stock)=>new()
    {
        Name=Text(name),Job=Text(job),Level=Math.Max(0,level),Gil=Math.Max(0,gil),Items=Math.Max(0,items),MarketItems=Math.Max(0,marketItems),
        MarketEnds=SalesEnd(marketItems,marketExpire),
        Venture=venture,VentureDone=venture!=0 && ventureComplete>0 ? DateTimeOffset.FromUnixTimeSeconds(ventureComplete):null,
        Town=Text(town),Stacks=stock?.Stacks,HqStacks=stock?.HqStacks,Crystals=stock?.Crystals,
    };

    public static string Text(string? text)
    {
        var clean=(text ?? "").Trim();
        return clean.Length>MaximumText ? clean[..MaximumText]:clean;
    }

    /// <summary>X-435 M1 (0.1420): the retainer was read with nothing on sale (its « sales end » alarm is taken back); a retainer still selling
    /// whose time is unusable is not « nothing on sale ».</summary>
    public static bool NothingOnSale(int marketItems)=>marketItems<=0;

    /// <summary>X-196 (0.1411): when the automatic alarm of a retainer's sales rings: at their end, only while something is on sale (null otherwise).</summary>
    public static DateTimeOffset? SaleAlarmAt(int marketItems,uint marketExpire)=>SalesEnd(marketItems,marketExpire);

    /// <summary>X-429 (0.1415): no retainer's sale ends before the game began (A Realm Reborn, 27 August 2013): an earlier value is no time.</summary>
    public static readonly DateTimeOffset GameStart=new(2013,8,27,0,0,0,TimeSpan.Zero);

    /// <summary>
    /// X-429 (0.1415, docs/evidence/0.1415-retainer-sales-end.md): the end of a retainer's sales, the one reading of it for the table and the
    /// alarm. The game writes it in its own retainer list (Addon 2314: « Selling n items until date », the time given as lnum4 beside the town
    /// lnum2 and the count lnum3, the three values of RetainerManager.Retainer: Town, MarketItemCount, MarketExpire). X-434 (0.1420,
    /// docs/evidence/0.1420-retainer-sales-end-code.md): read in the game's own code, the function that writes that line pushes the retainer's
    /// bytes +0x30, +0x31 and the 32-bit value +0x34 (MarketExpire) as lnum2, lnum3 and lnum4: the game's « until » day is the day of this
    /// very time (the game shows the day only; Tensho the same time to the minute). Null when nothing is on
    /// sale (the game shows no time then: Addon 2317), when no time is given, or when the time is before <see cref="GameStart"/>.
    /// </summary>
    public static DateTimeOffset? SalesEnd(int marketItems,uint marketExpire)
    {
        if(marketItems<=0 || marketExpire==0)return null;
        var end=DateTimeOffset.FromUnixTimeSeconds(marketExpire);
        return end<GameStart ? null:end;
    }

    /// <summary>X-196 (0.1410): a sale ends « soon » within a day (the table shows it in orange; ended ones in red).</summary>
    public static readonly TimeSpan SaleSoon=TimeSpan.FromDays(1);

    /// <summary>X-196 (0.1410): where the retainer's sales stand at <paramref name="now"/>, as last read.</summary>
    public static RetainerSale Sale(RetainerState retainer,DateTimeOffset now)
    {
        if(retainer.MarketItems<=0)return RetainerSale.None;
        if(retainer.MarketEnds is not { } end)return RetainerSale.Selling;
        return end<=now ? RetainerSale.Ended:end-now<=SaleSoon ? RetainerSale.EndingSoon:RetainerSale.Selling;
    }

    /// <summary>X-196 (0.1410): the table's rows: each character in the given order, its retainers in the game's order; characters never read left out.</summary>
    public static IReadOnlyList<(CharacterStatus Character,RetainerState Retainer)> Table(IEnumerable<CharacterStatus> characters)
        =>[.. characters.Where(status=>status.Retainers is { Length:>0 }).SelectMany(status=>status.Retainers!.Select(retainer=>(status,retainer)))];
}

/// <summary>X-196 (0.1410): a retainer's sales as last read: none on sale, selling, ending within <see cref="RetainerStates.SaleSoon"/>, ended.</summary>
public enum RetainerSale { None, Selling, EndingSoon, Ended }

/// <summary>
/// g, M-04 (0.374): what Tensho last read for one of the player's characters (local character id, never an Account ID): retainer
/// ventures, next treasure map, Wondrous Tails, hunt bills, daily roulettes, and the Reminders page's « done » boxes. A null value is
/// unknown (never read), never zero.
/// </summary>
public sealed record CharacterStatus
{
    public ulong ContentId { get; init; }
    public string Name { get; init; } = "";
    public DateTimeOffset SeenAt { get; init; }
    public VentureState[]? Ventures { get; init; }
    public DateTimeOffset? NextMap { get; init; }
    public bool? BingoHeld { get; init; }
    public int? BingoStickers { get; init; }
    public DateTimeOffset? BingoExpires { get; init; }
    public int? HuntBills { get; init; }
    public int? RoulettesDone { get; init; }
    public int? RoulettesTotal { get; init; }
    public DateTimeOffset? RoulettesAt { get; init; }
    public Dictionary<string,DateTimeOffset>? Done { get; init; }
    // X-76 P9 (0.936): what GameTimers.Read already gave and was dropped: leve allowances (and the next ones), custom deliveries left this week, allied
    // society quests left today, the squadron's mission and training ends; TimersAt = when the daily and weekly counts were read.
    public int? LeveAllowances { get; init; }
    public DateTimeOffset? NextLeveAllowances { get; init; }
    public int? DeliveriesLeft { get; init; }
    public int? AlliedQuestsLeft { get; init; }
    public DateTimeOffset? TimersAt { get; init; }
    public DateTimeOffset? SquadronMissionDone { get; init; }
    public DateTimeOffset? SquadronTrainingDone { get; init; }
    // X-196 (0.1409): every retainer as last read at a summoning bell (null: never read; a file from before 0.1409 reads null).
    public RetainerState[]? Retainers { get; init; }
}

/// <summary>
/// g, M-04 (0.374): merging a fresh read into the kept status, the overview figures, and the files (one per character,
/// Characters/&lt;local character id&gt;.json, user's choice: one file per character, like I-03).
/// </summary>
public static class CharacterStatuses
{
    public const int MaximumFileBytes=256*1024,MaximumVentures=10,MaximumDone=64;

    /// <summary>The fresh read wins where it knows a value; what it does not know (retainer list not opened…) stays as last read.</summary>
    public static CharacterStatus Merge(CharacterStatus? old,CharacterStatus fresh)
    {
        if(old is null || old.ContentId!=fresh.ContentId)return fresh;
        return fresh with
        {
            Name=fresh.Name.Length>0 ? fresh.Name:old.Name,
            Ventures=fresh.Ventures ?? old.Ventures,
            NextMap=fresh.NextMap ?? old.NextMap,
            BingoHeld=fresh.BingoHeld ?? old.BingoHeld,
            BingoStickers=fresh.BingoHeld is null ? old.BingoStickers:fresh.BingoStickers,
            BingoExpires=fresh.BingoHeld is null ? old.BingoExpires:fresh.BingoExpires,
            HuntBills=fresh.HuntBills ?? old.HuntBills,
            RoulettesDone=fresh.RoulettesDone is null ? old.RoulettesDone:fresh.RoulettesDone,
            RoulettesTotal=fresh.RoulettesDone is null ? old.RoulettesTotal:fresh.RoulettesTotal,
            RoulettesAt=fresh.RoulettesDone is null ? old.RoulettesAt:fresh.RoulettesAt,
            Done=fresh.Done ?? old.Done,
            LeveAllowances=fresh.LeveAllowances ?? old.LeveAllowances,
            NextLeveAllowances=fresh.LeveAllowances is null ? old.NextLeveAllowances:fresh.NextLeveAllowances,
            DeliveriesLeft=fresh.DeliveriesLeft ?? old.DeliveriesLeft,
            AlliedQuestsLeft=fresh.AlliedQuestsLeft ?? old.AlliedQuestsLeft,
            TimersAt=fresh.DeliveriesLeft is null && fresh.AlliedQuestsLeft is null ? old.TimersAt:fresh.TimersAt,
            SquadronMissionDone=fresh.SquadronMissionDone ?? old.SquadronMissionDone,
            SquadronTrainingDone=fresh.SquadronTrainingDone ?? old.SquadronTrainingDone,
            // X-469 (0.1762): each retainer's listing kept through a reading at a summoning bell.
            Retainers=RetainerStates.KeepListings(old.Retainers,fresh.Retainers,fresh.SeenAt) ?? old.Retainers,
        };
    }

    /// <summary>True when the two statuses differ in anything but the time they were seen (a save is then due).</summary>
    public static bool Changed(CharacterStatus? old,CharacterStatus fresh)
    {
        if(old is null)return true;
        var a=old with{SeenAt=default,Ventures=null,Done=null,RoulettesAt=null,TimersAt=null,Retainers=null};var b=fresh with{SeenAt=default,Ventures=null,Done=null,RoulettesAt=null,TimersAt=null,Retainers=null};
        return a!=b || !Same(old.Ventures,fresh.Ventures) || !Same(old.Done,fresh.Done) || !Same(old.Retainers,fresh.Retainers);
    }
    private static bool Same<T>(T[]? a,T[]? b)=>a is null ? b is null:b is not null && a.SequenceEqual(b);
    private static bool Same(Dictionary<string,DateTimeOffset>? a,Dictionary<string,DateTimeOffset>? b)
        =>a is null ? b is null:b is not null && a.Count==b.Count && a.All(pair=>b.TryGetValue(pair.Key,out var at) && at==pair.Value);

    /// <summary>Ventures done, ventures running, and the next end (null when none runs). Unknown (never read) = null.</summary>
    public static (int Ready,int Running,DateTimeOffset? Next)? Ventures(CharacterStatus status,DateTimeOffset now)
    {
        if(status.Ventures is not { } ventures)return null;
        var ready=ventures.Count(venture=>venture.Done<=now);
        var running=ventures.Where(venture=>venture.Done>now).Select(venture=>venture.Done!.Value).ToArray();
        return (ready,running.Length,running.Length>0 ? running.Min():null);
    }

    /// <summary>Daily roulettes done today, or null when the last read was before the daily reset (the count then means nothing).</summary>
    public static (int Done,int Total)? RoulettesToday(CharacterStatus status,DateTimeOffset lastDailyReset)
        =>status is { RoulettesDone:{ } done,RoulettesTotal:{ } total,RoulettesAt:{ } at } && at>=lastDailyReset ? (done,total):null;

    /// <summary>X-76 P9 (0.936): allied quests left today and deliveries left this week, each only when read since its reset (null otherwise).</summary>
    public static (int? Allied,int? Deliveries) DailyWeekly(CharacterStatus status,DateTimeOffset lastDaily,DateTimeOffset lastWeekly)
        =>status.TimersAt is not { } at ? (null,null):(at>=lastDaily ? status.AlliedQuestsLeft:null,at>=lastWeekly ? status.DeliveriesLeft:null);

    public static string FileName(ulong contentId)=>contentId.ToString(CultureInfo.InvariantCulture)+".json";

    private static readonly JsonSerializerOptions Json=TenshoJson.Own(new(){MaxDepth=8});
    private sealed record FileDto(int Schema,CharacterStatus Status);

    /// <summary>Reads one character's file; a file too large, of another schema, or holding another character throws.</summary>
    public static CharacterStatus Read(string path,ulong expected)
    {
        var info=new FileInfo(path);
        if(info.Length>MaximumFileBytes)throw new InvalidDataException("Character file too large.");
        var dto=JsonSerializer.Deserialize<FileDto>(File.ReadAllBytes(path),Json) ?? throw new InvalidDataException("Empty character file.");
        if(dto.Schema!=1 || dto.Status is null)throw new InvalidDataException("Unknown character file schema.");
        if(dto.Status.ContentId!=expected)throw new InvalidDataException("Character file holds another character.");
        return Bound(dto.Status);
    }

    /// <summary>Writes one character's file through a temporary file (a crash never leaves half a file).</summary>
    public static void Write(string folder,CharacterStatus status)
    {
        if(status.ContentId==0)throw new ArgumentException("No character.",nameof(status));
        AtomicFile.Write(Path.Combine(folder,FileName(status.ContentId)),JsonSerializer.SerializeToUtf8Bytes(new FileDto(1,Bound(status)),Json));
    }

    /// <summary>Every character file of the folder; a bad file is reported and skipped (and never written over by another character).</summary>
    public static List<CharacterStatus> ReadAll(string folder,Action<string,Exception> failed)
    {
        var all=new List<CharacterStatus>();
        if(!Directory.Exists(folder))return all;
        foreach(var path in Directory.EnumerateFiles(folder,"*.json").Take(200))
        {
            if(!ulong.TryParse(Path.GetFileNameWithoutExtension(path),NumberStyles.None,CultureInfo.InvariantCulture,out var id) || id==0)continue;
            try{all.Add(Read(path,id));}
            catch(Exception error){failed(path,error);}
        }
        return all;
    }

    private static CharacterStatus Bound(CharacterStatus status)=>status with
    {
        Name=status.Name.Length>80 ? status.Name[..80]:status.Name,
        Ventures=status.Ventures?.Take(MaximumVentures).ToArray(),
        Retainers=status.Retainers?.Take(MaximumVentures).Select(retainer=>retainer with
        {
            Name=RetainerStates.Text(retainer.Name),Job=RetainerStates.Text(retainer.Job),Town=RetainerStates.Text(retainer.Town),
            Listed=retainer.Listed?.Take(RetainerStates.MaximumListed).ToArray(),
        }).ToArray(),
        Done=status.Done is { Count:>MaximumDone } done ? done.Take(MaximumDone).ToDictionary():status.Done,
    };
}
