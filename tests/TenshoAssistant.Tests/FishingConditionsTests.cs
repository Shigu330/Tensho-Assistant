using System.Text.Json;
using TenshoAssistant.Community;
using TenshoAssistant.Gathering;
using TenshoAssistant.Monsters;

internal static class FishingConditionsTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static FishingConditions Read(string source)
    {
        using var document=JsonDocument.Parse(source);
        return FishingConditionParser.Parse(document.RootElement);
    }
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Community ET integer hour windows match independent minute membership oracle",()=>
        {
            for(var start=0;start<24;start++)for(var end=0;end<=24;end++)
            {
                var conditions=Read(JsonSerializer.Serialize(new{during=new{start,end}}));
                Check(conditions.Error is null,"valid integer ET hours");
                for(var minute=0;minute<1440;minute+=7)
                {
                    var expected=start<end ? minute>=start*60 && minute<end*60 : minute>=start*60 || minute<end*60;
                    var actual=conditions.Timing.Kind==GatheringScheduleKind.Unrestricted
                        || conditions.Timing.Windows.Any(window=>window.StartMinute<=minute && minute<window.EndMinute);
                    Check(expected==actual,$"membership {start}–{end} at {minute}");
                }
            }
        }),
        ("Community time conversion reuses ET UTC rational clock and midnight merger",()=>
        {
            var epoch=DateTimeOffset.UnixEpoch;
            var day=Read("""{"during":{"start":9,"end":14}}""").Timing;
            var opening=GatheringTime.Next(day,epoch).Openings[0];
            Check(opening.StartsAt==epoch.AddSeconds(1575) && opening.EndsAt==epoch.AddSeconds(2450),"hours not HHMM or UTC");
            Check(GatheringTime.Next(day,opening.StartsAt).Openings[0].Current,"inclusive start");
            Check(!GatheringTime.Next(day,opening.EndsAt).Openings[0].Current,"exclusive end");
            var overnight=Read("""{"during":{"start":22,"end":2}}""").Timing;
            var current=GatheringTime.Next(overnight,epoch.AddSeconds(175)).Openings[0];
            Check(current.Current && current.StartsAt==epoch.AddSeconds(-350) && current.EndsAt==epoch.AddSeconds(350),"one midnight interval");
        }),
        ("Missing conditions and empty weather lists never imply unrestricted fishing",()=>
        {
            foreach(var source in new[]{"{}","{\"during\":null,\"weather\":null,\"transition\":null}"})
            {
                var value=Read(source);
                Check(value.Timing.Kind==GatheringScheduleKind.Unknown && value.Weather is null && value.PreviousWeather is null && value.Error is null,"missing stays unknown");
            }
            var empty=Read("""{"weather":[],"transition":[],"during":{"start":0,"end":24}}""");
            Check(empty.Weather?.Count==0 && empty.PreviousWeather?.Count==0 && empty.Timing.Kind==GatheringScheduleKind.Unrestricted,"explicit full day distinct from missing, empty weather still list only");
        }),
        ("Weather alternatives and prior period retain source spelling and owned readonly copies",()=>
        {
            var value=Read("""{"weather":["Rain","Showers"],"transition":["Clear Skies"]}""");
            Check(value.Weather!.SequenceEqual(new[]{"Rain","Showers"}) && value.PreviousWeather!.Single()=="Clear Skies","current alternatives distinct from preceding period");
            var blocked=false;try{((IList<string>)value.Weather!)[0]="Fog";}catch(NotSupportedException){blocked=true;}
            Check(blocked && value.Weather![0]=="Rain","independent readonly list after JsonDocument disposed");
        }),
        ("Malformed conditions preserve bait routes and monster links but expose unknown conditions",()=>
        {
            Check(Read("[]").Error is not null && Read("null").Error is not null,"nonobject input is an explicit unknown error");
            var fields=new[]{"\"during\":[]","\"during\":{}","\"during\":{\"start\":9}","\"during\":{\"start\":-1,\"end\":3}","\"during\":{\"start\":24,\"end\":3}","\"during\":{\"start\":9,\"end\":25}","\"during\":{\"start\":9.5,\"end\":14}","\"during\":{\"start\":\"9\",\"end\":14}","\"weather\":{}","\"weather\":[1]","\"transition\":[\"\"]","\"weather\":[\"Rain\\nFog\"]","\"weather\":[\""+new string('x',65)+"\"]","\"weather\":["+string.Join(',',Enumerable.Repeat("\"Rain\"",33))+"]"};
            foreach(var conditionJson in fields)
            {
                var source="{\"item\":{\"id\":42,\"drops\":[777],\"fish\":{\"spots\":[{\"spot\":11,\"baits\":[[2585]],"+conditionJson+"}]}}}";
                var value=MonsterDocuments.ParseItem(source,42,DateTimeOffset.UnixEpoch);
                var spot=value.Fishing!.Spots.Single();
                Check(value.FishingError is null && value.Monsters.Single().Id==777 && spot.BaitChains![0].Single()==2585,"other source facts survive invalid optional conditions");
                Check(spot.Conditions is {Error:not null,Weather:null,PreviousWeather:null} && spot.Conditions.Timing.Kind==GatheringScheduleKind.Unknown,"no fallback to unrestricted or available");
            }
        }),
    };
}
