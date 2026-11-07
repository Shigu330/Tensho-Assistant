using Lumina;
using Lumina.Data;
using Lumina.Data.Files;
using Lumina.Data.Parsing.Layer;
using Lumina.Excel;
using Lumina.Excel.Sheets;

// Read-only: which layout files of a territory hold the FATE range whose instance id is Fate.Location.
// Arguments: the sqpack directory, a language, then territory rows.
var language=Enum.Parse<Language>(args[1]);
using var game=new GameData(args[0],new LuminaOptions{DefaultExcelLanguage=language,CacheFileResources=false});
var fates=game.GetExcelSheet<Fate>(language)!;
var events=game.GetExcelSheet<DynamicEvent>(language)!;
var eventRanges=events.Where(row=>row.LGBMapRange!=0).ToDictionary(row=>row.LGBMapRange,row=>row);
var eventObjects=events.Where(row=>row.LGBEventObject!=0).ToDictionary(row=>row.LGBEventObject,row=>row);
var byLocation=fates.Where(row=>row.Location!=0).GroupBy(row=>row.Location).ToDictionary(group=>group.Key,group=>group.ToArray());
Console.WriteLine($"Fate rows {fates.Count}, with a location {byLocation.Sum(pair=>pair.Value.Length)}, flagged Eureka {fates.Count(row=>row.EurekaFate!=0)}");
foreach(var territoryId in args[2..].Select(uint.Parse))
{
    var territory=game.GetExcelSheet<TerritoryType>(language)!.GetRow(territoryId);
    var bg=territory.Bg.ExtractText();var cut=bg.IndexOf("/level/",StringComparison.Ordinal);
    Console.WriteLine($"TERRITORY {territoryId} {territory.PlaceName.Value.Name.ExtractText()} map {territory.Map.RowId} bg {bg}");
    if(cut<0)continue;
    foreach(var kind in new[]{"planevent","planlive","planmap","planner","bg"})
    {
        var path=$"bg/{bg[..(cut+1)]}level/{kind}.lgb";
        if(!game.FileExists(path)){Console.WriteLine($"  {kind}: no file");continue;}
        var bytes=game.GetFile(path)!.Data;
        if(bytes.Length<16 || bytes[0]!='L' || bytes[12]!='L'){Console.WriteLine($"  {kind}: unexpected header");continue;}
        var file=game.GetFile<LgbFile>(path)!;
        var entries=file.Layers.SelectMany(layer=>layer.InstanceObjects).ToArray();
        var ranges=entries.Where(entry=>entry.AssetType.ToString().Contains("Fate",StringComparison.OrdinalIgnoreCase)).ToArray();
        var known=entries.Where(entry=>byLocation.ContainsKey(entry.InstanceId)).ToArray();
        Console.WriteLine($"  {kind}: entries {entries.Length}, fate-type {ranges.Length}, matching a Fate.Location {known.Length}");
        foreach(var entry in entries)
        {
            if(eventRanges.TryGetValue(entry.InstanceId,out var range))Console.WriteLine($"    EVENT-RANGE {range.RowId}|{range.Name.ExtractText()}|{entry.AssetType}|{entry.Transform.Translation.X:0.0};{entry.Transform.Translation.Z:0.0}");
            if(eventObjects.TryGetValue(entry.InstanceId,out var eventObject))Console.WriteLine($"    EVENT-OBJECT {eventObject.RowId}|{eventObject.Name.ExtractText()}|{entry.AssetType}");
        }
        foreach(var entry in known)
            foreach(var fate in byLocation[entry.InstanceId])
                Console.WriteLine($"    {fate.RowId}|{fate.Name.ExtractText()}|level {fate.ClassJobLevel}|eureka {fate.EurekaFate}|{entry.AssetType}|{entry.Transform.Translation.X:0.0};{entry.Transform.Translation.Y:0.0};{entry.Transform.Translation.Z:0.0}");
    }
}
