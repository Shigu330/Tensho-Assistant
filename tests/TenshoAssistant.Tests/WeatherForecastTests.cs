using System.Numerics;
using TenshoAssistant.Gathering;

internal static class WeatherForecastTests
{
    private static void Check(bool condition,string why){if(!condition)throw new Exception(why);}
    private static void Throws<T>(Action run) where T:Exception{try{run();}catch(T){return;}throw new Exception(typeof(T).Name);}
    private static WeatherCatalog Catalog(params WeatherShare[] shares)=>new([new(1,"Clair",1),new(2,"Pluie",2)],[new(10,1,shares)],"fixture");
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Weather target hand fixtures and independent arbitrary precision oracle",()=>
        {
            Check(WeatherCatalog.Target(DateTimeOffset.UnixEpoch)==56 && WeatherCatalog.Target(DateTimeOffset.UnixEpoch.AddSeconds(1400))==12 && WeatherCatalog.Target(DateTimeOffset.UnixEpoch.AddSeconds(2800))==0,"first three manually evaluated periods");
            var random=new Random(20261028);var mask=(BigInteger.One<<32)-1;
            for(var i=0;i<500;i++)
            {
                var seconds=random.NextInt64(0,250000000000);var day=seconds/4200;var hour=seconds/175;
                var seed=(new BigInteger(day)*100+(hour/8*8+8)%24)&mask;
                var first=((seed*2048)&mask)^seed;var expected=(int)(((first/256)^first)%100);
                Check(WeatherCatalog.Target(DateTimeOffset.FromUnixTimeSeconds(seconds))==expected,"independent 32-bit wrap and xor arithmetic");
            }
        }),
        ("Weather periods use exact boundaries UTC normalization and ET hours",()=>
        {
            var catalog=Catalog(new WeatherShare(1,100));var forecast=catalog.Forecast(10,DateTimeOffset.UnixEpoch.AddSeconds(1399).ToOffset(TimeSpan.FromHours(2)),4);
            Check(forecast[0].StartsAt==DateTimeOffset.UnixEpoch && forecast[0].EndsAt==DateTimeOffset.UnixEpoch.AddSeconds(1400),"current interval");
            Check(forecast.Select(row=>row.EtHour).SequenceEqual(new[]{0,8,16,0}) && forecast.All(row=>row.EndsAt-row.StartsAt==TimeSpan.FromSeconds(1400)),"8h ET windows");
            Check(catalog.Forecast(10,DateTimeOffset.UnixEpoch.AddSeconds(1400),1)[0].StartsAt==forecast[1].StartsAt,"boundary switches exactly");
        }),
        ("Weather cumulative thresholds and missing mass remain unknown without fallback",()=>
        {
            Check(Catalog(new(1,56),new(2,44)).Forecast(10,DateTimeOffset.UnixEpoch,1)[0].Weather?.Id==2,"strict upper bound target56");
            Check(Catalog(new WeatherShare(1,56)).Forecast(10,DateTimeOffset.UnixEpoch,1)[0].Weather is null,"uncovered mass unknown");
            Check(Catalog(new WeatherShare(99,100)).Forecast(10,DateTimeOffset.UnixEpoch,1)[0].Weather is null,"missing Weather reference unknown");
            Check(Catalog(new WeatherShare(1,101)).Forecast(10,DateTimeOffset.UnixEpoch,1)[0].UnknownReason is not null,"invalid total");
            Check(Catalog(new(1,-1),new(2,100)).Forecast(10,DateTimeOffset.UnixEpoch,1)[0].Weather is null,"negative rate");
        }),
        ("Weather absent individual and empty zones do not invent weather",()=>
        {
            var catalog=new WeatherCatalog([new(1,"Clair",1)],[new(10,1,[new(1,100)],true),new(11,0,[]),new(12,1,[])],"fixture");
            foreach(var zone in new uint[]{10,11,12,999})Check(catalog.Forecast(zone,DateTimeOffset.UnixEpoch,1)[0].Weather is null,"unavailable prediction");
            Throws<ArgumentOutOfRangeException>(()=>catalog.Forecast(10,DateTimeOffset.UnixEpoch,0));
            Throws<ArgumentOutOfRangeException>(()=>catalog.Forecast(10,DateTimeOffset.UnixEpoch,25));
            Throws<ArgumentOutOfRangeException>(()=>WeatherCatalog.Target(DateTimeOffset.UnixEpoch.AddSeconds(-1)));
        }),
        ("Weather catalog copies shares and outputs immutable versioned forecasts",()=>
        {
            var shares=new List<WeatherShare>{new(1,100)};var catalog=new WeatherCatalog([new(1,"Clair",1)],[new(10,1,shares)],"native-version");shares.Clear();
            var forecast=catalog.Forecast(10,DateTimeOffset.UnixEpoch,1);Check(forecast[0].Weather?.Id==1 && catalog.DataVersion=="native-version","copied source");
            Throws<NotSupportedException>(()=>((IList<WeatherPeriod>)forecast).Clear());
            Throws<NotSupportedException>(()=>((IList<WeatherShare>)catalog.Zone(10)!.Shares).Clear());
            var service=new WeatherCatalogService();service.Publish(catalog);service.Fail(new Exception("changed"));Check(service.State.Catalog is null,"no old metadata after fail");
        })
    };
}
