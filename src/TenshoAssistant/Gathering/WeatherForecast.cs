using System.Collections.Frozen;

namespace TenshoAssistant.Gathering;

public sealed record WeatherDefinition(uint Id,string Name,uint Icon,string? EnglishName=null);
public sealed record WeatherShare(uint WeatherId,int Chance);
public sealed record ZoneWeather(uint TerritoryId,uint RateId,IReadOnlyList<WeatherShare> Shares,bool Individual=false);
public sealed record WeatherPeriod(DateTimeOffset StartsAt,DateTimeOffset EndsAt,int EtHour,WeatherDefinition? Weather,string? UnknownReason);
public sealed class WeatherCatalog
{
    private readonly FrozenDictionary<uint,WeatherDefinition> definitions;
    private readonly FrozenDictionary<(uint Territory,string Name),WeatherDefinition> zoneSourceNames;
    private readonly FrozenDictionary<uint,ZoneWeather> zones;
    public string DataVersion {get;}
    public int ZoneCount=>zones.Count;
    public WeatherCatalog(IEnumerable<WeatherDefinition> definitions,IEnumerable<ZoneWeather> zones,string version)
    {
        this.definitions=definitions.Where(row=>row.Id>0 && !string.IsNullOrWhiteSpace(row.Name)).ToFrozenDictionary(row=>row.Id);
        this.zones=zones.Where(row=>row.TerritoryId>0).Select(row=>row with {Shares=Array.AsReadOnly(row.Shares.ToArray())}).ToFrozenDictionary(row=>row.TerritoryId);
        // Exact English names from the same native row, scoped to the zone's positive rates.
        // Missing English metadata in that scope prevents proving uniqueness.
        zoneSourceNames=this.zones.Values.Where(zone=>zone.RateId!=0 && zone.Shares.All(share=>share.Chance is >=0 and <=100)
                && zone.Shares.Sum(share=>(long)share.Chance)<=100
                && zone.Shares.Where(share=>share.Chance>0).All(share=>this.definitions.TryGetValue(share.WeatherId,out var row) && !string.IsNullOrWhiteSpace(row.EnglishName)))
            .SelectMany(zone=>zone.Shares.Where(share=>share.Chance>0).Select(share=>share.WeatherId).Distinct()
                .Select(id=>this.definitions.GetValueOrDefault(id)).OfType<WeatherDefinition>()
                .Where(row=>!string.IsNullOrWhiteSpace(row.EnglishName)).GroupBy(row=>row.EnglishName!,StringComparer.Ordinal)
                .Where(group=>group.Count()==1).Select(group=>new{Key=(zone.TerritoryId,group.Key),Value=group.Single()}))
            .ToFrozenDictionary(pair=>pair.Key,pair=>pair.Value);
        DataVersion=version;
    }
    public ZoneWeather? Zone(uint territory)=>zones.GetValueOrDefault(territory);
    public WeatherDefinition? Weather(uint id)=>definitions.GetValueOrDefault(id);
    public WeatherDefinition? WeatherBySourceName(string name,uint territory)=>zoneSourceNames.GetValueOrDefault((territory,name));
    public static int Target(DateTimeOffset date)
    {
        var seconds=date.ToUnixTimeSeconds();if(seconds<0)throw new ArgumentOutOfRangeException(nameof(date));
        var hour=seconds/175;var shifted=(uint)(hour+8-hour%8)%24;
        unchecked
        {
            var seed=(uint)(seconds/4200)*100+shifted;
            var first=(seed<<11)^seed;
            return (int)(((first>>8)^first)%100);
        }
    }
    public IReadOnlyList<WeatherPeriod> Forecast(uint territory,DateTimeOffset date,int count=6)
    {
        var seconds=date.ToUnixTimeSeconds();
        if(seconds<0 || count is <1 or >24 || seconds/1400*1400+(long)count*1400>DateTimeOffset.MaxValue.ToUnixTimeSeconds())throw new ArgumentOutOfRangeException(nameof(date));
        var start=seconds/1400*1400;var zone=Zone(territory);var result=new List<WeatherPeriod>();
        for(var index=0;index<count;index++)
        {
            var time=DateTimeOffset.FromUnixTimeSeconds(start+index*1400L);var target=Target(time);WeatherDefinition? weather=null;string? reason=null;
            if(zone is null || zone.RateId==0 || zone.Shares.Count==0)reason="Table météo absente.";
            else if(zone.Individual)reason="Météo individuelle / forcée non couverte.";
            else if(zone.Shares.Any(share=>share.Chance is <0 or >100) || zone.Shares.Sum(share=>(long)share.Chance)>100)reason="Table météo incohérente.";
            else
            {
                var cumulative=0;
                foreach(var share in zone.Shares)
                {
                    cumulative+=share.Chance;if(target>=cumulative)continue;
                    weather=Weather(share.WeatherId);break;
                }
                if(weather is null)reason="Tirage non couvert ou météo native inconnue.";
            }
            result.Add(new(time,time.AddSeconds(1400),(int)((start+index*1400L)/175%24),weather,reason));
        }
        return Array.AsReadOnly(result.ToArray());
    }
}
public sealed record WeatherCatalogState(WeatherCatalog? Catalog,string? Error);
public sealed class WeatherCatalogService
{
    private WeatherCatalogState state=new(null,null);
    public WeatherCatalogState State=>Volatile.Read(ref state);
    internal void Publish(WeatherCatalog catalog)=>Interlocked.Exchange(ref state,new(catalog,null));
    internal void Fail(Exception error)=>Interlocked.Exchange(ref state,new(null,error.Message));
    internal void Clear()=>Interlocked.Exchange(ref state,new(null,null));
}
