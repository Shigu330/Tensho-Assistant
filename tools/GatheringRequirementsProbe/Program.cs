using System.Globalization;
using Lumina;
using Lumina.Data;
using Lumina.Excel.Sheets;

if(args.Length is <1 or >2 || args.Length==2 && args[1]!="--compare-source")throw new ArgumentException("Supply sqpack directory, optionally --compare-source.");
using var data=new GameData(args[0],new LuminaOptions{DefaultExcelLanguage=Language.French});
var levels=data.Excel.GetSheet<ItemLevel>();var conversions=data.Excel.GetSheet<GatheringItemLevelConvertTable>();
var items=data.Excel.GetSheet<Item>();var gathering=data.Excel.GetSheet<GatheringItem>();
var facts=new List<(uint GatheringId,uint ItemId,uint LevelId,int? Requirement)>();var printed=0;
foreach(var row in gathering)
{
    if(row.Item.RowId==0 || row.Item.RowType!=typeof(Item))continue;
    int? requirement=levels.TryGetRow(row.GatheringItemLevel.RowId,out var level) ? level.Gathering:null;
    facts.Add((row.RowId,row.Item.RowId,row.GatheringItemLevel.RowId,requirement));
    if(printed++<6)Console.WriteLine($"ITEM {row.Item.RowId} {items.GetRow(row.Item.RowId).Name.ExtractText()} gatheringRow={row.RowId} levelKey={row.GatheringItemLevel.RowId} ItemLevel.Gathering={requirement?.ToString() ?? "missing"}");
}
var conflicts=facts.GroupBy(row=>row.ItemId).Where(group=>group.Select(row=>row.Requirement).Distinct().Count()>1).ToArray();
Console.WriteLine($"NATIVE FACTS gatheringRows={gathering.Count}, normalRefs={facts.Count}, missingItemLevel={facts.Count(row=>row.Requirement is null)}, zeroRequirement={facts.Count(row=>row.Requirement==0)}, conflictingItems={conflicts.Length}, conversionRows={conversions.Count}");
foreach(var id in new uint[]{1,50,100,420,690})
    if(levels.TryGetRow(id,out var level))Console.WriteLine($"ITEMLEVEL {id} Gathering={level.Gathering}");
foreach(var group in conflicts.Take(4))Console.WriteLine($"CONFLICT {group.Key}: "+string.Join(',',group.Select(row=>$"{row.GatheringId}/{row.LevelId}/{row.Requirement}")));
if(args.Length==2)
{
    const string commit="a204f2f45cb98302dbe938921255c3a6d617c775";
    using var http=new HttpClient{Timeout=TimeSpan.FromSeconds(15),MaxResponseContentBufferSize=1_000_000};
    var csv=await http.GetStringAsync($"https://raw.githubusercontent.com/FFXIV-CombatReborn/GatherBuddyReborn/{commit}/GatherBuddy/CustomInfo/IlvConvertTable.csv");
    var external=new Dictionary<uint,int>();
    foreach(var line in csv.Split('\n').Skip(1).Where(line=>!string.IsNullOrWhiteSpace(line)))
    {
        var fields=line.Trim().Split(',');
        if(fields.Length!=3 || !uint.TryParse(fields[0],NumberStyles.None,CultureInfo.InvariantCulture,out var id)
            || !int.TryParse(fields[1],NumberStyles.None,CultureInfo.InvariantCulture,out var value) || !external.TryAdd(id,value))throw new Exception("Unexpected source CSV shape.");
    }
    var compared=0;var mismatched=0;var missing=0;
    foreach(var id in facts.Select(row=>row.LevelId).Distinct().Order())
    {
        if(!external.TryGetValue(id,out var source) || !levels.TryGetRow(id,out var native)){missing++;continue;}
        compared++;if(source!=native.Gathering){if(mismatched++<8)Console.WriteLine($"SOURCE DIFFERENCE key={id} sourceBaseGathering={source}, nativeItemLevelGathering={native.Gathering}");}
    }
    Console.WriteLine($"SOURCE COMPARISON commit={commit}, comparedKeys={compared}, mismatches={mismatched}, missingKeys={missing}; facts only, no formula or runtime claim");
}
