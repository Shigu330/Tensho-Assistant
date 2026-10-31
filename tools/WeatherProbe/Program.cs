using Lumina;
using Lumina.Data;
using Lumina.Excel.Sheets;
using TenshoAssistant.Gathering;

if(args.Length!=1)throw new ArgumentException("Supply the installed game's sqpack directory.");
using var data=new GameData(args[0],new LuminaOptions{DefaultExcelLanguage=Language.French});
var weatherSheet=data.Excel.GetSheet<Weather>();var rates=data.Excel.GetSheet<WeatherRate>();var territories=data.Excel.GetSheet<TerritoryType>();
var definitions=new Dictionary<uint,WeatherDefinition>();var zones=new List<ZoneWeather>();
var englishSheet=data.Excel.GetSheet<Weather>(Language.English);
foreach(var row in weatherSheet.Where(row=>row.RowId!=0))
{
    Weather? english=englishSheet.TryGetRow(row.RowId,out var found) ? found:null;
    var copied=WeatherCopy.FromRows(row,english);
    if(english is not null && copied.EnglishName!=english.Value.Name.ExtractText())throw new Exception("English row identity mismatch.");
    if(WeatherCopy.FromRows(row,null).EnglishName is not null)throw new Exception("Missing English row invented a name.");
    var foreign=englishSheet.First(other=>other.RowId!=row.RowId);
    if(WeatherCopy.FromRows(row,foreign).EnglishName is not null)throw new Exception("Foreign English weather row accepted.");
    definitions[row.RowId]=copied;
}
foreach(var row in territories)
{
    if(row.RowId==0)continue;var shares=new List<WeatherShare>();
    if(row.WeatherRate.RowId!=0 && rates.TryGetRow(row.WeatherRate.RowId,out var rate))
    {
        for(var i=0;i<rate.Rate.Count && i<rate.Weather.Count;i++)
        {
            if(rate.Rate[i]==0)continue;var id=rate.Weather[i].RowId;shares.Add(new(id,rate.Rate[i]));
        }
    }
    zones.Add(new(row.RowId,row.WeatherRate.RowId,shares,row.IndividualWeather.RowId!=0));
}
var catalog=new WeatherCatalog(definitions.Values,zones,"installed-game");
var ambiguous=definitions.Values.Where(row=>!string.IsNullOrWhiteSpace(row.EnglishName) && !string.IsNullOrWhiteSpace(row.Name))
    .GroupBy(row=>row.EnglishName!,StringComparer.Ordinal).Where(group=>group.Count()>1).ToArray();
foreach(var zone in zones)
foreach(var group in ambiguous)
{
    var candidates=zone.Shares.Where(share=>share.Chance>0).Select(share=>share.WeatherId).Distinct().Where(id=>group.Any(row=>row.Id==id)).ToArray();
    if(candidates.Length>1 && catalog.WeatherBySourceName(group.Key,zone.TerritoryId) is not null)throw new Exception("Ambiguous scoped name resolved arbitrarily.");
}
foreach(var name in new[]{"Clear Skies","Fair Skies","Rain","Showers"})
{
    var territory=name is "Rain" or "Showers" ? 137u:129u;
    var match=catalog.WeatherBySourceName(name,territory) ?? throw new Exception($"Required scoped English alias missing: {name}, territory {territory}");
    Console.WriteLine($"ALIAS territory {territory}: {name} -> native {match.Id} {match.Name}, icon={match.Icon}");
}
Console.WriteLine($"PASS native bilingual Weather projection; {definitions.Count} rows, {ambiguous.Length} globally ambiguous English labels; scoped ambiguities remain unresolved; absent/foreign English rows rejected");
var partial=zones.Where(zone=>zone.Shares.Count>0 && zone.Shares.Sum(share=>share.Chance)!=100).ToArray();
Console.WriteLine($"PASS {zones.Count} territory rows, {definitions.Count} weather definitions, {zones.Count(zone=>zone.Shares.Count>0)} tables with rates, {zones.Count(zone=>zone.Individual)} individual-weather zones, {partial.Length} non-100 totals");
foreach(var weather in definitions.Values.Where(weather=>weather.Icon>0))
    if(!data.FileExists($"ui/icon/{weather.Icon/1000*1000:D6}/{weather.Icon:D6}.tex"))throw new Exception($"Missing weather icon {weather.Icon}");
var fixture=DateTimeOffset.Parse("2026-10-28T12:00:00Z",System.Globalization.CultureInfo.InvariantCulture);
var example=catalog.Forecast(129,fixture);
if(example[0].Weather is not { } exampleWeather)throw new Exception("Native capital forecast fixture missing");
Console.WriteLine($"PASS territory129 at fixed UTC {fixture:O}: {exampleWeather.Name}, period {example[0].StartsAt:O} through {example[0].EndsAt:O}");
foreach(var zone in partial.Take(10))Console.WriteLine($"PARTIAL territory {zone.TerritoryId}, rate {zone.RateId}, total {zone.Shares.Sum(share=>share.Chance)}");
var knownPartial=partial.FirstOrDefault(zone=>zone.TerritoryId==979);
if(knownPartial is not null)
{
    var total=knownPartial.Shares.Sum(share=>share.Chance);
    var missing=Enumerable.Range(0,360).Select(index=>fixture.AddSeconds(index*1400)).First(date=>WeatherCatalog.Target(date)>=total);
    if(catalog.Forecast(979,missing,1)[0].Weather is not null)throw new Exception("Incomplete native table invented a fallback");
    Console.WriteLine("PASS native territory979 uncovered target remains unknown; no last-weather fallback");
}
