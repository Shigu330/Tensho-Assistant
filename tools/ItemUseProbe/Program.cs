using Lumina;
using Lumina.Data;
using Lumina.Excel.Sheets;
using TenshoAssistant.Items;

if(args.Length!=1)throw new ArgumentException("Supply installed sqpack directory.");
using var data=new GameData(args[0],new LuminaOptions{DefaultExcelLanguage=Language.French});
var leves=new List<ItemLeveUse>();var skipped=0;var supplies=new HashSet<uint>();var aquarium=new HashSet<uint>();
var craft=data.Excel.GetSheet<CraftLeve>();var gc=data.Excel.GetSheet<GCSupplyDuty>();var fish=data.Excel.GetSheet<AquariumFish>();
foreach(var row in craft)
{
    var copied=ItemUseSheetCopy.Craft(row);if(copied is not null)leves.Add(copied);else if(row.RowId!=0 && row.Leve.RowId!=0)skipped++;
}
foreach(var row in gc)foreach(var item in ItemUseSheetCopy.Supplies(row))supplies.Add(item);
foreach(var row in fish)if(row.Item.RowId!=0)aquarium.Add(row.Item.RowId);
var catalog=new ItemUseCatalog(leves,supplies,aquarium,"French","installed",skipped);
var expectedGc=gc.SelectMany(row=>row.SupplyData.SelectMany(mission=>mission.Item.Select(item=>item.RowId))).Where(id=>id!=0).ToHashSet();
if(!expectedGc.SetEquals(supplies))throw new Exception("Supply projection differs from direct sheet enumeration.");
var items=data.Excel.GetSheet<Item>();
var references=leves.SelectMany(leve=>leve.Requests.Select(request=>request.ItemId)).Concat(supplies).Concat(aquarium).Distinct().ToArray();
if(references.Any(id=>!items.HasRow(id)) || catalog.LeveCount==0 || catalog.SupplyItemCount==0 || catalog.AquariumItemCount==0)throw new Exception("Unresolved item reference or empty native usage family.");
if(leves.Any(leve=>leve.IconId!=0 && !data.FileExists($"ui/icon/{leve.IconId/1000*1000:000000}/{leve.IconId:000000}.tex")))throw new Exception("Native leve icon asset missing.");
foreach(var leve in leves)
foreach(var id in leve.Requests.Select(request=>request.ItemId).Distinct())
    if(catalog.LevesFor(id).Count(row=>row.RowId==leve.RowId)!=1)throw new Exception("Leve inverse index loses or duplicates a variant.");
Console.WriteLine($"PASS native usages: craftRows={craft.Count}, leves={catalog.LeveCount}, unresolved={skipped}, GC candidates={catalog.SupplyItemCount}, aquarium={catalog.AquariumItemCount}, itemRefs={references.Length}");
Console.WriteLine($"LEVE SHAPES multipleRequests={leves.Count(row=>row.Requests.Count>1)}, unknownQuantities={leves.Sum(row=>row.Requests.Count(request=>request.Quantity is null))}, repeats={string.Join(',',leves.Select(row=>row.Repeats).Distinct().Order())}");
foreach(var leve in leves.Take(3))Console.WriteLine($"LEVE {leve.LeveId} {leve.Name}, {leve.Job} level={leve.Level}, requests="+string.Join(',',leve.Requests.Select(request=>$"{request.ItemId} {items.GetRow(request.ItemId).Name.ExtractText()} x{request.Quantity}")));
foreach(var id in aquarium.Order().Take(2))Console.WriteLine($"AQUARIUM {id} {items.GetRow(id).Name.ExtractText()}");
foreach(var leve in leves.Where(leve=>leve.Requests.Count>1).Take(2))Console.WriteLine($"MULTILINE {leve.LeveId}: "+string.Join(',',leve.Requests.Select(request=>$"{request.ItemId}x{request.Quantity}")));
