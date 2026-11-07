namespace TenshoAssistant.Gathering;

/// <summary>0.568 (C9): whether a fish's conditions hold now. Null: no such condition, or not known; never a guess.</summary>
public static class FishNow
{
    public static bool? InWindow(int? startMinute,int? endMinute,int etMinute)
    {
        if(startMinute is not { } start || endMinute is not { } end || start==end)return null;
        return start<end ? etMinute>=start && etMinute<end:etMinute>=start || etMinute<end;
    }
    public static bool? WeatherFits(IReadOnlyList<uint>? wanted,uint? seen)
    {
        if(wanted is not { Count:>0 } || seen is not { } weather)return null;
        return wanted.Contains(weather);
    }
}
