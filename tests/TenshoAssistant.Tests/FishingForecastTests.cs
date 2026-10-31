using System.Numerics;
using TenshoAssistant.Community;
using TenshoAssistant.Gathering;

internal static class FishingForecastTests
{
    private static readonly DateTimeOffset Epoch=DateTimeOffset.UnixEpoch;
    private static void Check(bool value,string why){if(!value)throw new Exception(why);}
    private static void Throws<T>(Action action) where T:Exception{try{action();}catch(T){return;}throw new Exception(typeof(T).Name);}
    private static WeatherCatalog Catalog(params WeatherShare[] shares)=>new([new(1,"Clair",1,"Clear"),new(2,"Pluie",2,"Rain")],[new(10,1,shares)],"test");
    private static bool? At(FishingForecastResult result,DateTimeOffset date)=>result.Spans.Single(span=>span.StartsAt<=date && date<span.EndsAt).ReportedConditionsMatch;
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Fishing forecast intersects reported ET hours with current and preceding weather periods",()=>
        {
            // Independent hand fixtures: weather targets at epoch, +1400 and +2800 = 56, 12 and 0.
            var catalog=Catalog(new(1,50),new(2,50));
            var conditions=new FishingConditions(GatheringSchedule.FromEphemeral(900,1400),["Clear"],["Rain"]);
            var result=FishingForecast.Calculate(conditions,catalog,10,Epoch.AddSeconds(1400),2);
            Check(result.ReportedFields==(FishingConditionFields.Time|FishingConditionFields.Weather|FishingConditionFields.PreviousWeather),"all three fields reported");
            Check(At(result,Epoch.AddSeconds(1574))==false && At(result,Epoch.AddSeconds(1575))==true && At(result,Epoch.AddSeconds(2450))==false,"9–14 ET intersection boundaries");
            Check(result.Spans.Single(span=>span.ReportedConditionsMatch==true) is { } good && good.StartsAt==Epoch.AddSeconds(1575) && good.EndsAt==Epoch.AddSeconds(2450),"no adjacent false period included");
        }),
        ("Fishing forecast partial fields retain coverage while absent and empty conditions yield no inferred window",()=>
        {
            var catalog=Catalog(new WeatherShare(1,100));
            var weatherOnly=FishingForecast.Calculate(new(GatheringSchedule.Unknown,["Clear"],null),catalog,10,Epoch,2);
            Check(weatherOnly.ReportedFields==FishingConditionFields.Weather && weatherOnly.Spans.Single().ReportedConditionsMatch==true,"reported weather only, not complete availability");
            var timeOnly=FishingForecast.Calculate(new(GatheringSchedule.FromEphemeral(100,200),null,[]),catalog,10,Epoch,1);
            Check(timeOnly.ReportedFields==FishingConditionFields.Time && At(timeOnly,Epoch.AddSeconds(175))==true,"explicit time only");
            foreach(var conditions in new[]{new FishingConditions(GatheringSchedule.Unknown,null,null),new(GatheringSchedule.Unknown,[],[])})
                Check(FishingForecast.Calculate(conditions,catalog,10,Epoch).Error is not null,"no data never becomes all-day compatible");
        }),
        ("Fishing forecast preserves unknown aliases periods and individual weather with false dominating unknown",()=>
        {
            var catalog=Catalog(new WeatherShare(1,40)); // target56 uncovered, target12 covered.
            var unknown=FishingForecast.Calculate(new(GatheringSchedule.Unknown,["Clear"],null),catalog,10,Epoch,2);
            Check(At(unknown,Epoch.AddSeconds(1)) is null && At(unknown,Epoch.AddSeconds(1401))==true,"partial table unknown distinct from mismatch");
            var unresolved=FishingForecast.Calculate(new(GatheringSchedule.FromEphemeral(100,200),["missing"],null),catalog,10,Epoch,1);
            Check(At(unresolved,Epoch.AddSeconds(1))==false && At(unresolved,Epoch.AddSeconds(176)) is null,"time mismatch proven despite unknown weather");
            var alternatives=FishingForecast.Calculate(new(GatheringSchedule.Unknown,["Clear","unknown"],null),catalog,10,Epoch.AddSeconds(1400),1);
            Check(alternatives.Spans.Single().ReportedConditionsMatch==true,"known accepted alternative establishes criterion match");
            var unknownAlternative=FishingForecast.Calculate(new(GatheringSchedule.Unknown,["Rain","unknown"],null),catalog,10,Epoch.AddSeconds(1400),1);
            Check(unknownAlternative.Spans.Single().ReportedConditionsMatch is null,"unresolved alternative cannot prove mismatch");
            var individual=new WeatherCatalog([new(1,"Clair",1,"Clear")],[new(10,1,[new(1,100)],true)],"test");
            Check(FishingForecast.Calculate(new(GatheringSchedule.Unknown,["Clear"],null),individual,10,Epoch).Spans.Single().ReportedConditionsMatch is null,"individual weather unknown");
            Check(FishingForecast.Calculate(new(GatheringSchedule.Unknown,["Clear"],null),catalog,99,Epoch).Spans.Single().ReportedConditionsMatch is null,"missing territory unknown");
        }),
        ("Fishing forecast previous weather uses the actual preceding period and handles epoch boundary",()=>
        {
            var catalog=Catalog(new(1,50),new(2,50));
            var result=FishingForecast.Calculate(new(GatheringSchedule.Unknown,null,["Rain"]),catalog,10,Epoch,3);
            Check(At(result,Epoch.AddSeconds(1)) is null && At(result,Epoch.AddSeconds(1400))==true && At(result,Epoch.AddSeconds(2800))==false,"preceding period, not current or previous day");
            var shifted=FishingForecast.Calculate(new(GatheringSchedule.Unknown,null,["Rain"]),catalog,10,Epoch.AddSeconds(1401).ToOffset(TimeSpan.FromHours(5)),1);
            Check(shifted.CalculatedAt.Offset==TimeSpan.Zero && shifted.Spans.Single().ReportedConditionsMatch==true,"normalizes UTC without losing prior cycle");
        }),
        ("Bounded ET range preserves rational minute boundaries midnight and source immutability",()=>
        {
            var schedule=GatheringSchedule.FromRare([(1,1),(2200,400)]);
            var range=GatheringTime.Between(schedule,Epoch,Epoch.AddSeconds(8400));
            var boundary=(35L*TimeSpan.TicksPerSecond+11)/12;
            var minuteRange=GatheringTime.Between(GatheringSchedule.FromRare([(1,1)]),Epoch,Epoch.AddSeconds(60));
            Check(minuteRange.Single().StartsAt==Epoch.AddTicks(boundary) && minuteRange.Single().EndsAt==Epoch.AddTicks((70L*TimeSpan.TicksPerSecond+11)/12),"minute boundary keeps first representable rational tick without overlapping midnight span");
            Check(range.Any(span=>span.StartsAt==Epoch.AddSeconds(3850) && span.EndsAt==Epoch.AddSeconds(4550)),"midnight pieces merged");
            Check(range.All(span=>span.StartsAt>=Epoch && span.EndsAt<=Epoch.AddSeconds(8400)),"range clipped exactly");
            Throws<NotSupportedException>(()=>((IList<GatheringOpening>)range).Clear());
            Throws<ArgumentOutOfRangeException>(()=>GatheringTime.Between(schedule,Epoch,Epoch));
            Throws<ArgumentOutOfRangeException>(()=>GatheringTime.Between(schedule,Epoch,Epoch.AddSeconds(9*4200+1)));
        }),
        ("Fishing combined forecast agrees with independent sampled weather and ET oracle",()=>
        {
            var catalog=Catalog(new(1,50),new(2,50));var mask=(BigInteger.One<<32)-1;
            uint Weather(long seconds)
            {
                var hour=seconds/175;var seed=(new BigInteger(seconds/4200)*100+(hour/8*8+8)%24)&mask;
                var first=((seed*2048)&mask)^seed;return (int)(((first/256)^first)%100)<50 ? 1u:2u;
            }
            foreach(var start in new[]{1,9,18,22})foreach(var end in new[]{2,14,24})
            {
                var schedule=GatheringSchedule.FromEphemeral((ushort)(start*100),(ushort)(end*100));
                var result=FishingForecast.Calculate(new(schedule,["Clear"],["Rain"]),catalog,10,Epoch.AddSeconds(1400),12);
                for(var seconds=1400L;seconds<16800;seconds+=37)
                {
                    var hour=seconds%4200/175d;
                    var time=start<end ? hour>=start && hour<end:hour>=start || hour<end;
                    var expected=time && Weather(seconds)==1 && Weather(seconds/1400*1400-1400)==2;
                    Check(At(result,Epoch.AddSeconds(seconds))==expected,$"oracle {start}–{end} ET at {seconds}");
                }
                Check(result.Spans.Zip(result.Spans.Skip(1)).All(pair=>pair.First.EndsAt==pair.Second.StartsAt && pair.First.ReportedConditionsMatch!=pair.Second.ReportedConditionsMatch),"partition contiguous and maximal");
            }
        }),
        ("Fishing forecasts validate horizon cancellation errors and expose immutable results",()=>
        {
            var catalog=Catalog(new WeatherShare(1,100));var conditions=new FishingConditions(GatheringSchedule.Unknown,["Clear"],null);
            foreach(var count in new[]{0,25})Throws<ArgumentOutOfRangeException>(()=>FishingForecast.Calculate(conditions,catalog,10,Epoch,count));
            Throws<ArgumentOutOfRangeException>(()=>FishingForecast.Calculate(conditions,catalog,10,Epoch.AddTicks(-1)));
            Throws<ArgumentOutOfRangeException>(()=>FishingForecast.Calculate(conditions,catalog,10,DateTimeOffset.MaxValue));
            using var cancellation=new CancellationTokenSource();cancellation.Cancel();
            Throws<OperationCanceledException>(()=>FishingForecast.Calculate(conditions,catalog,10,Epoch,cancellation:cancellation.Token));
            Throws<OperationCanceledException>(()=>GatheringTime.Between(GatheringSchedule.Unknown,Epoch,Epoch.AddSeconds(1),cancellation.Token));
            Check(FishingForecast.Calculate(conditions with {Error="bad"},catalog,10,Epoch).Error is not null,"invalid conditions never claimed compatible");
            var result=FishingForecast.Calculate(conditions,catalog,10,Epoch,24);
            Throws<NotSupportedException>(()=>((IList<FishingForecastSpan>)result.Spans).Clear());
            Check(result.HorizonEndsAt==Epoch.AddSeconds(24*1400) && result.Spans[0].StartsAt==Epoch,"max bounded horizon");
        }),
    };
}
