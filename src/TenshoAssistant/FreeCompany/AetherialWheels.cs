using System.Globalization;
using TenshoAssistant.Alarms;

namespace TenshoAssistant.FreeCompany;

/// <summary>X-225 (0.1719): a row of the game's AetherialWheel sheet: the wheel set on the stand (unprimed), the same wheel once primed, its
/// grade and the hours it takes (docs/evidence/0.1234-aetherial-wheels.md: 20, 45 or 70).</summary>
public sealed record AetherialWheelRow(uint Unprimed,uint Primed,int Grade,int Hours);

/// <summary>X-225 (0.1719): one slot of the stand as read in the game's window: the item set (0: empty) and its charge in percent.</summary>
public sealed record AetherialWheelRead(int Slot,uint Item,int Percent);

/// <summary>
/// X-225 (0.1719): a wheel on the company's stand as last read, kept in the configuration (the game shows it only in the stand's window).
/// Its name in the client's language at read time. <see cref="FullFrom"/> and <see cref="FullBy"/>: when it is primed at the earliest and
/// at the latest; the same when it was seen being set (<see cref="SeenSet"/>: the game's hours from that moment), else from its charge
/// (<see cref="AetherialWheels.Bounds"/>). <see cref="ReadAt"/>: when its charge was first read at <see cref="Percent"/>.
/// </summary>
public sealed record AetherialWheelSlot
{
    public ulong HouseId { get; init; }
    public int Slot { get; init; }
    public uint Item { get; init; }
    public string Name { get; init; } = "";
    public int Percent { get; init; }
    public int Hours { get; init; }
    public DateTimeOffset ReadAt { get; init; }
    public DateTimeOffset? FullFrom { get; init; }
    public DateTimeOffset? FullBy { get; init; }
    public bool SeenSet { get; init; }
    public bool Primed { get; init; }
}

/// <summary>X-225 (0.1719): the rules of the aetherial wheels' reminder. Pure (tests).</summary>
public static class AetherialWheels
{
    /// <summary>The window's numbers (read on 21/11, evidence 0.1234): number 0, the stand's slots; slot k from number <see cref="First"/>
    /// + <see cref="Stride"/> × k: the item set, the slot's grade, the charge in percent (100 when empty); its texts after them.</summary>
    public const int First=3,Stride=8,MaximumSlots=6;
    /// <summary>A wheel read at 1 % at most, its slot read empty this long before in the same opening of the window, was just set.</summary>
    public static readonly TimeSpan SetWindow=TimeSpan.FromSeconds(15);
    public const string KeyPrefix="wheel:";

    /// <summary>The slots read from the window's numbers (null for a number that is not one); null when anything is not as read on 21/11
    /// (more slots than a stand has, an item that is no wheel of the sheet, a charge out of 0 to 100): nothing is read then (R08).</summary>
    public static IReadOnlyList<AetherialWheelRead>? Parse(IReadOnlyList<int?> values,Func<uint,AetherialWheelRow?> wheel)
    {
        if(values.Count==0 || values[0] is not { } count || count is <0 or >MaximumSlots)return null;
        var slots=new List<AetherialWheelRead>(count);
        for(var slot=0;slot<count;slot++)
        {
            var at=First+slot*Stride;
            if(at+2>=values.Count || values[at] is not { } item || values[at+2] is not { } percent || item<0 || percent is <0 or >100)return null;
            if(item!=0 && wheel((uint)item) is null)return null;
            slots.Add(new(slot+1,(uint)item,percent));
        }
        return slots;
    }

    /// <summary>When a wheel of <paramref name="total"/> hours read at <paramref name="percent"/> is primed, at the earliest and the latest. The
    /// game shows 1 % just after a wheel is set (21/11): the percent shown is taken within one percent of the time gone by, either way.</summary>
    public static (DateTimeOffset From,DateTimeOffset By) Bounds(int percent,TimeSpan total,DateTimeOffset now)
        =>(now+total*(Math.Clamp(99-percent,0,100)/100.0),now+total*(Math.Clamp(101-percent,0,101)/100.0));

    /// <summary>The wheel was just set: read at 1 % at most, its slot read empty in the same opening of the window less than
    /// <see cref="SetWindow"/> before.</summary>
    public static bool JustSet(AetherialWheelRead read,(IReadOnlyList<AetherialWheelRead> Slots,DateTimeOffset At)? before,DateTimeOffset now)
        =>read.Item!=0 && read.Percent<=1 && before is { } earlier && now>=earlier.At && now-earlier.At<=SetWindow
          && earlier.Slots.FirstOrDefault(slot=>slot.Slot==read.Slot) is { Item:0 };

    /// <summary>The saved wheels with this house's slots as just read: an empty slot has no wheel (taken off or converted); the other houses'
    /// wheels stay. A wheel read again at the same charge is kept as it was (nothing to save).</summary>
    public static List<AetherialWheelSlot> Update(IEnumerable<AetherialWheelSlot>? saved,ulong houseId,IReadOnlyList<AetherialWheelRead> read,
        (IReadOnlyList<AetherialWheelRead> Slots,DateTimeOffset At)? before,DateTimeOffset now,Func<uint,AetherialWheelRow?> wheel,Func<uint,string> name)
    {
        var all=(saved ?? []).ToList();
        var next=all.Where(slot=>slot.HouseId!=houseId).ToList();
        foreach(var each in read)
        {
            if(each.Item==0 || wheel(each.Item) is not { } row)continue;
            var prior=all.FirstOrDefault(slot=>slot.HouseId==houseId && slot.Slot==each.Slot && wheel(slot.Item)?.Unprimed==row.Unprimed);
            next.Add(Next(prior,houseId,each,row,before,now,name));
        }
        return next.OrderBy(slot=>slot.HouseId).ThenBy(slot=>slot.Slot).ToList();
    }

    private static AetherialWheelSlot Next(AetherialWheelSlot? prior,ulong houseId,AetherialWheelRead read,AetherialWheelRow row,
        (IReadOnlyList<AetherialWheelRead> Slots,DateTimeOffset At)? before,DateTimeOffset now,Func<uint,string> name)
    {
        if(prior is not null && prior.Item==read.Item && prior.Percent==read.Percent)return prior;
        var total=TimeSpan.FromHours(row.Hours);
        var primed=read.Item==row.Primed || read.Percent>=100;
        DateTimeOffset? from,by;
        var seen=prior?.SeenSet==true;
        if(primed)
        {
            from=prior?.FullFrom is { } earliest && earliest<=now ? earliest:null;
            by=prior?.FullBy is { } latest && latest<=now ? latest:now;
        }
        else if(JustSet(read,before,now)){from=by=now+total;seen=true;}
        else
        {
            var (low,high)=Bounds(read.Percent,total,now);
            from=low;by=high;
            if(prior is { Primed:false, FullFrom:{ } priorFrom, FullBy:{ } priorBy } && (priorFrom>low ? priorFrom:low) is var narrowFrom
               && (priorBy<high ? priorBy:high) is var narrowBy && narrowFrom<=narrowBy){from=narrowFrom;by=narrowBy;}
            // Readings that disagree (another wheel of the same kind set meanwhile): the new reading alone.
            else seen=false;
        }
        var shown=name(read.Item);
        return new AetherialWheelSlot{HouseId=houseId,Slot=read.Slot,Item=read.Item,Name=shown.Length>0 ? shown:"#"+read.Item.ToString(CultureInfo.InvariantCulture),
            Percent=read.Percent,Hours=row.Hours,ReadAt=now,FullFrom=from,FullBy=by,SeenSet=seen,Primed=primed};
    }

    public static bool SameAs(IReadOnlyList<AetherialWheelSlot>? saved,IReadOnlyList<AetherialWheelSlot> updated)=>(saved ?? []).SequenceEqual(updated);

    /// <summary>Primed: read so, or past the latest time it could be.</summary>
    public static bool IsPrimed(AetherialWheelSlot slot,DateTimeOffset now)=>slot.Primed || slot.FullBy is { } by && by<=now;

    /// <summary>The next time a wheel is surely primed (its latest), none for none.</summary>
    public static DateTimeOffset? NextPrimed(IEnumerable<AetherialWheelSlot>? slots,DateTimeOffset now)
        =>(slots ?? []).Where(slot=>!IsPrimed(slot,now) && slot.FullBy>now).Select(slot=>slot.FullBy).Min();

    public static string Key(AetherialWheelSlot slot)=>string.Create(CultureInfo.InvariantCulture,$"{KeyPrefix}{slot.HouseId}/{slot.Slot}");

    /// <summary>The automatic alarms' deadlines: each wheel not primed, at the latest time it is primed (never before it is).</summary>
    public static List<DueEvent> Due(IEnumerable<AetherialWheelSlot>? slots,DateTimeOffset now,Func<string,string> label,string group)
        =>(slots ?? []).Where(slot=>!IsPrimed(slot,now) && slot.FullBy>now).Select(slot=>new DueEvent(Key(slot),label(slot.Name),group,slot.FullBy!.Value,AlarmSource.Reminder)).ToList();

    /// <summary>The wheels' automatic alarms whose wheel is no longer on the stand (its slot read empty): taken back.</summary>
    public static List<string> Gone(IEnumerable<TenshoAlarm>? alarms,IEnumerable<AetherialWheelSlot>? slots)
    {
        var present=(slots ?? []).Select(Key).ToHashSet(StringComparer.Ordinal);
        return (alarms ?? []).Where(alarm=>alarm.AutoKey.StartsWith(KeyPrefix,StringComparison.Ordinal) && !present.Contains(alarm.AutoKey)).Select(alarm=>alarm.AutoKey).ToList();
    }

    /// <summary>The slots read, for the developer's log (item numbers and charges only).</summary>
    public static string Describe(IReadOnlyList<AetherialWheelRead> read)
        =>read.Count==0 ? "no slot":string.Join("; ",read.Select(slot=>slot.Item==0
            ? string.Create(CultureInfo.InvariantCulture,$"slot {slot.Slot} empty"):string.Create(CultureInfo.InvariantCulture,$"slot {slot.Slot} item {slot.Item}, {slot.Percent} %")));
}
