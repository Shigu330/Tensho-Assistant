using TenshoAssistant.Community;

namespace TenshoAssistant.Gathering;

[Flags]
public enum FishingConditionFields { None=0,Time=1,Weather=2,PreviousWeather=4 }
public sealed record FishingForecastSpan(DateTimeOffset StartsAt,DateTimeOffset EndsAt,bool? ReportedConditionsMatch);
public sealed record FishingForecastResult(DateTimeOffset CalculatedAt,DateTimeOffset HorizonEndsAt,
    FishingConditionFields ReportedFields,IReadOnlyList<FishingForecastSpan> Spans,string? Error=null);

/// <summary>Only intersection of reported conditions. Missing fields and non-modelled prerequisites never become proof of availability.</summary>
public static class FishingForecast
{
    public static FishingForecastResult Calculate(FishingConditions conditions,WeatherCatalog catalog,uint territory,
        DateTimeOffset now,int periods=12,CancellationToken cancellation=default)
    {
        ArgumentNullException.ThrowIfNull(conditions);ArgumentNullException.ThrowIfNull(catalog);cancellation.ThrowIfCancellationRequested();
        now=now.ToUniversalTime();
        if(now<DateTimeOffset.UnixEpoch || periods is <1 or >24)throw new ArgumentOutOfRangeException(nameof(now));
        var startSeconds=now.ToUnixTimeSeconds()/1400*1400;
        if(startSeconds+(long)periods*1400>DateTimeOffset.MaxValue.ToUnixTimeSeconds())throw new ArgumentOutOfRangeException(nameof(now));
        var end=DateTimeOffset.FromUnixTimeSeconds(startSeconds+(long)periods*1400);
        var fields=FishingConditionFields.None;
        if(conditions.Timing.Kind is GatheringScheduleKind.Limited or GatheringScheduleKind.Unrestricted)fields|=FishingConditionFields.Time;
        if(conditions.Weather is {Count:>0})fields|=FishingConditionFields.Weather;
        if(conditions.PreviousWeather is {Count:>0})fields|=FishingConditionFields.PreviousWeather;
        FishingForecastResult Empty(string error)=>new(now,end,fields,Array.AsReadOnly(Array.Empty<FishingForecastSpan>()),error);
        if(conditions.Error is not null)return Empty("Conditions non interprétables ; aucune plage calculée.");
        if(fields==FishingConditionFields.None)return Empty("Aucune condition exploitable renseignée ; aucune plage déduite.");
        var useTime=fields.HasFlag(FishingConditionFields.Time);
        var weather=catalog.Forecast(territory,now,periods);
        var previous=startSeconds>=1400 ? catalog.Forecast(territory,DateTimeOffset.FromUnixTimeSeconds(startSeconds-1400),1)[0]:null;
        var times=useTime ? GatheringTime.Between(conditions.Timing,now,end,cancellation):[];
        uint?[] Resolve(IReadOnlyList<string>? names)=>(names ?? []).Select(name=>(uint?)catalog.WeatherBySourceName(name,territory)?.Id).ToArray();
        var currentNames=Resolve(conditions.Weather);var priorNames=Resolve(conditions.PreviousWeather);
        var boundaries=new SortedSet<DateTimeOffset>{now,end};
        foreach(var period in weather)
        {
            if(period.StartsAt>now && period.StartsAt<end)boundaries.Add(period.StartsAt);
            if(period.EndsAt>now && period.EndsAt<end)boundaries.Add(period.EndsAt);
        }
        foreach(var time in times){boundaries.Add(time.StartsAt);boundaries.Add(time.EndsAt);}
        var points=boundaries.ToArray();var spans=new List<FishingForecastSpan>();
        for(var index=0;index<points.Length-1;index++)
        {
            cancellation.ThrowIfCancellationRequested();
            var start=points[index];var until=points[index+1];
            var period=(int)((start.ToUnixTimeSeconds()-startSeconds)/1400);
            bool? match=!useTime || times.Any(time=>time.StartsAt<=start && start<time.EndsAt);
            if(fields.HasFlag(FishingConditionFields.Weather))match=And(match,Matches(currentNames,weather[period].Weather));
            if(fields.HasFlag(FishingConditionFields.PreviousWeather))match=And(match,Matches(priorNames,period==0 ? previous?.Weather:weather[period-1].Weather));
            if(spans.Count>0 && spans[^1].EndsAt==start && spans[^1].ReportedConditionsMatch==match)spans[^1]=spans[^1] with {EndsAt=until};
            else spans.Add(new(start,until,match));
        }
        return new(now,end,fields,Array.AsReadOnly(spans.ToArray()));
    }

    private static bool? And(bool? left,bool? right)=>left==false || right==false ? false:left==true && right==true ? true:null;
    private static bool? Matches(IReadOnlyList<uint?> allowed,WeatherDefinition? actual)
    {
        if(actual is null)return null;
        if(allowed.Any(id=>id==actual.Id))return true;
        return allowed.Any(id=>id is null) ? null:false;
    }
}
