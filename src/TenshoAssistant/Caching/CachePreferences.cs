namespace TenshoAssistant.Caching;

public enum CacheArea { Prices, Quests, Collections, Sources, Fashion, ImportedStock, CraftCalculations, Completion }
// 0.281 (user request): Never = data are kept and never read again on their own; only a refresh button reads them again.
public enum CacheRetention { FiveMinutes, OneHour, OneDay, SevenDays, Never }

public sealed class CachePreferences
{
    // 0.750 (X-02, user request: « fresh data are rarely needed, unless they are missing »): longer defaults. Prices and the Fashion Report keep theirs: they change.
    private int prices=2,quests=1,collections=3,sources=4,fashion=3,importedStock=2,craftCalculations=3,completion=3;

    /// <summary>0.750 (X-02): settings saved with the earlier defaults move to the new ones once; a duration the player chose otherwise stays.</summary>
    public bool LongerDefaults()
    {
        var changed=false;
        if(Quests==CacheRetention.FiveMinutes){Quests=CacheRetention.OneHour;changed=true;}
        if(Collections==CacheRetention.OneDay){Collections=CacheRetention.SevenDays;changed=true;}
        if(Sources==CacheRetention.SevenDays){Sources=CacheRetention.Never;changed=true;}
        if(CraftCalculations==CacheRetention.OneDay){CraftCalculations=CacheRetention.SevenDays;changed=true;}
        return changed;
    }
    public CacheRetention Prices {get=>(CacheRetention)Volatile.Read(ref prices);set=>Volatile.Write(ref prices,(int)value);}
    public CacheRetention Quests {get=>(CacheRetention)Volatile.Read(ref quests);set=>Volatile.Write(ref quests,(int)value);}
    public CacheRetention Collections {get=>(CacheRetention)Volatile.Read(ref collections);set=>Volatile.Write(ref collections,(int)value);}
    public CacheRetention Sources {get=>(CacheRetention)Volatile.Read(ref sources);set=>Volatile.Write(ref sources,(int)value);}
    public CacheRetention Fashion {get=>(CacheRetention)Volatile.Read(ref fashion);set=>Volatile.Write(ref fashion,(int)value);}
    public CacheRetention ImportedStock {get=>(CacheRetention)Volatile.Read(ref importedStock);set=>Volatile.Write(ref importedStock,(int)value);}
    public CacheRetention CraftCalculations {get=>(CacheRetention)Volatile.Read(ref craftCalculations);set=>Volatile.Write(ref craftCalculations,(int)value);}
    // Titles and achievements (0.166): how long « not obtained » states read when the game had sent its lists stay known.
    public CacheRetention Completion {get=>(CacheRetention)Volatile.Read(ref completion);set=>Volatile.Write(ref completion,(int)value);}
    public CacheRetention Get(CacheArea area)
    {
        var selected=area switch{CacheArea.Prices=>Prices,CacheArea.Quests=>Quests,CacheArea.Collections=>Collections,CacheArea.Sources=>Sources,CacheArea.Fashion=>Fashion,CacheArea.ImportedStock=>ImportedStock,CacheArea.CraftCalculations=>CraftCalculations,CacheArea.Completion=>Completion,_=>throw new ArgumentOutOfRangeException(nameof(area))};
        return Enum.IsDefined(selected) ? selected:area==CacheArea.Quests ? CacheRetention.FiveMinutes:area is CacheArea.Sources or CacheArea.Fashion or CacheArea.Completion ? CacheRetention.SevenDays:CacheRetention.OneDay;
    }
    // A century rather than TimeSpan.MaxValue: date arithmetic (recorded date + lifetime) must not overflow.
    public static readonly TimeSpan NeverExpires=TimeSpan.FromDays(36500);
    public TimeSpan Duration(CacheArea area)=>Duration(Get(area));
    public static TimeSpan Duration(CacheRetention value)=>value switch
    {CacheRetention.FiveMinutes=>TimeSpan.FromMinutes(5),CacheRetention.OneHour=>TimeSpan.FromHours(1),CacheRetention.OneDay=>TimeSpan.FromDays(1),CacheRetention.SevenDays=>TimeSpan.FromDays(7),CacheRetention.Never=>NeverExpires,_=>throw new ArgumentOutOfRangeException(nameof(value))};
    public CachePreferences With(CacheArea area,CacheRetention value)
    {
        if(!Enum.IsDefined(value))throw new ArgumentOutOfRangeException(nameof(value));
        var next=new CachePreferences{Prices=Prices,Quests=Quests,Collections=Collections,Sources=Sources,Fashion=Fashion,ImportedStock=ImportedStock,CraftCalculations=CraftCalculations,Completion=Completion};
        switch(area){case CacheArea.Prices:next.Prices=value;break;case CacheArea.Quests:next.Quests=value;break;case CacheArea.Collections:next.Collections=value;break;case CacheArea.Sources:next.Sources=value;break;case CacheArea.Fashion:next.Fashion=value;break;case CacheArea.ImportedStock:next.ImportedStock=value;break;case CacheArea.CraftCalculations:next.CraftCalculations=value;break;case CacheArea.Completion:next.Completion=value;break;default:throw new ArgumentOutOfRangeException(nameof(area));}
        return next;
    }
}
public static class CacheAge
{
    public static bool Fresh(DateTimeOffset? recordedAt,DateTimeOffset now,TimeSpan duration)=>recordedAt is { } date && duration>TimeSpan.Zero && now>=date && now-date<duration;
    /// <summary>
    /// X-76 (0.955): the first instant after <paramref name="now"/> at which <see cref="Fresh"/> changes for one of these dates (a date reached,
    /// or its lifetime over); null when none will change. A view keyed on it is rebuilt when a record turns fresh or expired, not every second.
    /// </summary>
    public static DateTimeOffset? NextChange(IEnumerable<DateTimeOffset?> recordedAt,DateTimeOffset now,TimeSpan duration)
    {
        if(duration<=TimeSpan.Zero)return null;
        DateTimeOffset? next=null;
        foreach(var recorded in recordedAt)
        {
            if(recorded is not { } date)continue;
            DateTimeOffset? change=now<date ? date:duration<=DateTimeOffset.MaxValue-date && now<date+duration ? date+duration:null;
            if(change is { } at && (next is null || at<next))next=at;
        }
        return next;
    }
}
