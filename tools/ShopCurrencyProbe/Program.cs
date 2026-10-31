using Lumina;
using Lumina.Data;
using Lumina.Excel.Sheets;

// Read-only evidence for NPC-VENDORS-014: how scrip exchanges encode currencies and how NPCs reach their shops.
if(args.Length is <1 or >2)throw new ArgumentException("Supply installed sqpack directory and optionally an item name.");
using var data=new GameData(args[0],new LuminaOptions{DefaultExcelLanguage=Language.French,CacheFileResources=false});
// PROBE_LOCAL (0.114): proves the map « local lists » links — Quest.IssuerLocation, Fate.Location and
// ExportedGatheringPoint are Level/positions on a map — before the plugin relies on them.
if(Environment.GetEnvironmentVariable("PROBE_LOCAL") is { Length:>0 })
{
    var levels=data.Excel.GetSheet<Level>();
    var quests=data.Excel.GetSheet<Quest>();
    var withIssuer=quests.Where(quest=>quest.IssuerLocation.RowId!=0).ToList();
    Console.WriteLine($"QUEST rows={quests.Count} issuer={withIssuer.Count} issuerWithMap={withIssuer.Count(quest=>quest.IssuerLocation.IsValid && quest.IssuerLocation.Value.Map.RowId!=0)}");
    foreach(var quest in withIssuer.Take(3)){var l=quest.IssuerLocation.Value;Console.WriteLine($"  quest {quest.RowId} '{quest.Name.ExtractText()}' level {l.RowId} map {l.Map.RowId} x={l.X:0.0} z={l.Z:0.0}");}
    var fates=data.Excel.GetSheet<Fate>().Where(fate=>fate.Name.ExtractText().Length>0).ToList();
    var located=fates.Where(fate=>fate.Location!=0 && levels.TryGetRow(fate.Location,out var level) && level.Map.RowId!=0).ToList();
    Console.WriteLine($"FATE named={fates.Count} locationIsLevelWithMap={located.Count} icons={string.Join(",",fates.GroupBy(fate=>fate.Icon).Select(group=>$"{group.Key}:{group.Count()}"))}");
    foreach(var fate in located.Take(3)){var l=levels.GetRow(fate.Location);Console.WriteLine($"  fate {fate.RowId} '{fate.Name.ExtractText()}' lv{fate.ClassJobLevel} icon {fate.Icon} level {l.RowId} map {l.Map.RowId} x={l.X:0.0} z={l.Z:0.0}");}
    var exported=data.Excel.GetSheet<ExportedGatheringPoint>();var bases=data.Excel.GetSheet<GatheringPointBase>();
    Console.WriteLine($"GATHER bases={bases.Count} exported={exported.Count} basesWithExport={bases.Count(row=>exported.TryGetRow(row.RowId,out var e) && (e.X!=0 || e.Y!=0))}");
    foreach(var point in data.Excel.GetSheet<GatheringPoint>().Where(point=>point.GatheringPointBase.RowId!=0 && point.TerritoryType.RowId!=0).Take(3))
        if(exported.TryGetRow(point.GatheringPointBase.RowId,out var e))Console.WriteLine($"  point {point.RowId} base {point.GatheringPointBase.RowId} territory {point.TerritoryType.RowId} x={e.X:0.0} y={e.Y:0.0} r={e.Radius} type {e.GatheringType.RowId}");
    return;
}
var items=data.Excel.GetSheet<Item>();
string Name(uint id)=>items.TryGetRow(id,out var row) ? row.Name.ExtractText():"?";
// Target item: the potion by default, or an item name given as second argument.
uint potion=12669;
if(args.Length>1)potion=items.First(row=>row.Name.ExtractText()==args[1]).RowId;
var shops=data.Excel.GetSheet<SpecialShop>();
var referencing=new HashSet<uint>();
foreach(var shop in shops)
foreach(var entry in shop.Item)
{
    if(!entry.ReceiveItems.Any(receive=>receive.Item.RowId==potion))continue;
    referencing.Add(shop.RowId);
    Console.WriteLine($"SHOP {shop.RowId} '{shop.Name.ExtractText()}' UseCurrencyType={shop.UseCurrencyType} costs="
        +string.Join(" + ",entry.ItemCosts.Where(cost=>cost.CurrencyCost>0).Select(cost=>$"{cost.CurrencyCost}x[{cost.ItemCost.RowId} '{Name(cost.ItemCost.RowId)}' type{cost.CostType}]")));
}
// Who points at these shops: NPC data, topic menus, pre-handlers, talk scripts, inclusion series.
foreach(var npc in data.Excel.GetSheet<ENpcBase>())
    foreach(var value in npc.ENpcData)if(referencing.Contains(value.RowId))Console.WriteLine($"ENPC {npc.RowId} direct -> {value.RowId}");
foreach(var topic in data.Excel.GetSheet<TopicSelect>())
    foreach(var value in topic.Shop)if(referencing.Contains(value.RowId))Console.WriteLine($"TOPIC {topic.RowId} -> {value.RowId}");
foreach(var talk in data.Excel.GetSheet<CustomTalk>())
    foreach(var script in talk.Script)if(referencing.Contains(script.ScriptArg))Console.WriteLine($"TALK {talk.RowId} -> {script.ScriptArg}");
foreach(var handler in data.Excel.GetSheet<PreHandler>())if(referencing.Contains(handler.Target.RowId))Console.WriteLine($"PREHANDLER {handler.RowId} -> {handler.Target.RowId}");
foreach(var fate in data.Excel.GetSheet<FateShop>())foreach(var value in fate.SpecialShop)if(referencing.Contains(value.RowId))Console.WriteLine($"FATESHOP {fate.RowId} -> {value.RowId}");
// Raw scan: any NPC whose data mentions a shop id or any id that mentions it one level down, whatever the sheet.
var raw=new Dictionary<uint,List<uint>>();
foreach(var talk in data.Excel.GetSheet<CustomTalk>())foreach(var script in talk.Script)if(script.ScriptArg!=0){if(!raw.TryGetValue(script.ScriptArg,out var l))raw[script.ScriptArg]=l=[];l.Add(talk.RowId);}
foreach(var npc in data.Excel.GetSheet<ENpcBase>())foreach(var value in npc.ENpcData)
    if(value.RowId!=0 && !referencing.Contains(value.RowId) && data.Excel.GetSheet<CustomTalk>().TryGetRow(value.RowId,out var talk2)
       && talk2.Script.Any(script=>referencing.Contains(script.ScriptArg)))Console.WriteLine($"ENPC {npc.RowId} via talk {value.RowId}");
var series=data.Excel.GetSubrowSheet<InclusionShopSeries>();
foreach(var row in series)foreach(var sub in row)if(referencing.Contains(sub.SpecialShop.RowId))Console.WriteLine($"SERIES {row.RowId}.{sub.SubrowId} -> {sub.SpecialShop.RowId}");
foreach(var category in data.Excel.GetSheet<InclusionShopCategory>())
    if(series.TryGetRow(category.InclusionShopSeries.RowId,out var list) && list.Any(sub=>referencing.Contains(sub.SpecialShop.RowId)))
        Console.WriteLine($"CATEGORY {category.RowId} '{category.Name.ExtractText()}' series {category.InclusionShopSeries.RowId}");
foreach(var inclusion in data.Excel.GetSheet<InclusionShop>())
    foreach(var category in inclusion.Category)
        if(category.RowId!=0 && data.Excel.GetSheet<InclusionShopCategory>().TryGetRow(category.RowId,out var cat)
           && series.TryGetRow(cat.InclusionShopSeries.RowId,out var list) && list.Any(sub=>referencing.Contains(sub.SpecialShop.RowId)))
            Console.WriteLine($"INCLUSION {inclusion.RowId} '{inclusion.ShopName.ExtractText()}' via category {category.RowId}");
// The Radz-at-Han scrip NPC named by the user.
var residents=data.Excel.GetSheet<ENpcResident>();
foreach(var resident in residents)
    if(resident.Singular.ExtractText().Contains("comptable des assignats",StringComparison.OrdinalIgnoreCase)
       && data.Excel.GetSheet<ENpcBase>().TryGetRow(resident.RowId,out var npc))
        Console.WriteLine($"NPC {resident.RowId} '{resident.Singular.ExtractText()}' data="+string.Join(",",npc.ENpcData.Select(value=>value.RowId).Where(id=>id!=0)));
// For each encoded (type, id): sample shop names and received items, to tie codes to the currency seen in game.
foreach(var group in shops.SelectMany(shop=>shop.Item.SelectMany(entry=>entry.ItemCosts.Where(cost=>cost.CurrencyCost>0 && cost.ItemCost.RowId is >0 and <100)
        .Select(cost=>(Type:shop.UseCurrencyType,Id:cost.ItemCost.RowId,Kind:cost.CostType,Shop:shop.Name.ExtractText(),Item:entry.ReceiveItems.Select(receive=>receive.Item.RowId).FirstOrDefault(),Amount:cost.CurrencyCost))))
    .GroupBy(row=>(row.Type,row.Id,row.Kind)).OrderBy(group=>group.Key.Type).ThenBy(group=>group.Key.Id).ThenBy(group=>group.Key.Kind))
    Console.WriteLine($"CODE t{group.Key.Type}:{group.Key.Id} costType{group.Key.Kind} count={group.Count()} shops="+string.Join(" | ",group.Select(row=>row.Shop).Where(name=>name.Length>0).Distinct().Take(4)));
// The item the user checked in game (Zircon, Solution Nine).
foreach(var shop in shops)foreach(var entry in shop.Item)
    if(entry.ReceiveItems.Any(receive=>Name(receive.Item.RowId)=="Casque d'abatteur du roman royal"))
        Console.WriteLine($"ROYAL {shop.RowId} '{shop.Name.ExtractText()}' t{shop.UseCurrencyType} "+string.Join(" + ",entry.ItemCosts.Where(cost=>cost.CurrencyCost>0).Select(cost=>$"{cost.CurrencyCost}x{cost.ItemCost.RowId} costType{cost.CostType}")));
// Nested talk menus: CustomTalkNestHandlers (subrows keyed by CustomTalk) → shop, then NPCs reaching that CustomTalk.
var nested=data.Excel.GetSubrowSheet<CustomTalkNestHandlers>();
var nestTalks=new HashSet<uint>();
foreach(var row in nested)foreach(var sub in row)if(referencing.Contains(sub.NestHandler.RowId)){nestTalks.Add(row.RowId);Console.WriteLine($"NEST {row.RowId}.{sub.SubrowId} -> {sub.NestHandler.RowId}");}
foreach(var npc in data.Excel.GetSheet<ENpcBase>())foreach(var value in npc.ENpcData)if(nestTalks.Contains(value.RowId))
    Console.WriteLine($"NESTNPC {npc.RowId} '{(residents.TryGetRow(npc.RowId,out var who) ? who.Singular.ExtractText():"")}' via {value.RowId}");
// Overview maps (world, regions) and how their markers link to other maps.
if(Environment.GetEnvironmentVariable("PROBE_MAPS") is { Length:>0 })
{
    var mapSheet=data.Excel.GetSheet<Map>();var markers=data.Excel.GetSubrowSheet<MapMarker>();
    foreach(var map in mapSheet.Where(map=>map.TerritoryType.RowId==0 || map.Id.ExtractText().StartsWith("default",StringComparison.Ordinal)).Take(30))
    {
        var linked=markers.TryGetRow(map.MapMarkerRange,out var rows) ? rows.Where(row=>row.DataType!=0).Take(4).Select(row=>$"t{row.DataType}:{row.DataKey.RowId}").ToArray():[];
        Console.WriteLine($"OVERVIEW {map.RowId} id='{map.Id.ExtractText()}' territory={map.TerritoryType.RowId} place='{map.PlaceName.Value.Name.ExtractText()}' region='{map.PlaceNameRegion.Value.Name.ExtractText()}' markers={map.MapMarkerRange} links={string.Join(",",linked)}");
    }
    var ilsabard=mapSheet.First(map=>map.Id.ExtractText().StartsWith("region/",StringComparison.Ordinal) && map.PlaceName.Value.Name.ExtractText()=="Ilsabard").PlaceNameRegion.RowId;
    foreach(var map in mapSheet.Where(map=>map.PlaceNameRegion.RowId==ilsabard).OrderBy(map=>map.PriorityCategoryUI).ThenBy(map=>map.PriorityUI))
        Console.WriteLine($"ILSABARD {map.RowId} '{map.PlaceName.Value.Name.ExtractText()}' sub='{map.PlaceNameSub.Value.Name.ExtractText()}' id='{map.Id.ExtractText()}' territory={map.TerritoryType.RowId} cat={map.PriorityCategoryUI} prio={map.PriorityUI} index={map.MapIndex} event={map.IsEvent}");
    var zone=mapSheet.First(map=>map.PlaceName.Value.Name.ExtractText()=="Radz-at-Han");
    if(markers.TryGetRow(zone.MapMarkerRange,out var zoneRows))
    {
        Console.WriteLine("RADZ types="+string.Join(",",zoneRows.GroupBy(row=>row.DataType).Select(group=>$"t{group.Key}x{group.Count()}")));
        var aetherytes=data.Excel.GetSheet<Aetheryte>();
        foreach(var row in zoneRows.Where(row=>row.DataType is 3 or 4))
            Console.WriteLine($"RADZ marker icon={row.Icon} type={row.DataType} key={row.DataKey.RowId} aetheryte='{(aetherytes.TryGetRow(row.DataKey.RowId,out var a) ? a.PlaceName.Value.Name.ExtractText()+"/"+a.AethernetName.Value.Name.ExtractText()+" isAetheryte="+a.IsAetheryte:"?")}'");
    }
}
// Commendation crystal exchanges: shops costing item 40479, talks named like the officer's, and NPCs using those talks.
if(Environment.GetEnvironmentVariable("PROBE_CRYSTAL") is { Length:>0 })
{
    var directIds=data.Excel.GetSheet<ENpcBase>().SelectMany(row=>row.ENpcData.Select(value=>value.RowId)).ToHashSet();
    foreach(var shop in shops.Where(shop=>shop.Item.Any(entry=>entry.ItemCosts.Any(cost=>cost.ItemCost.RowId==40479))))
        Console.WriteLine($"CRYSTALSHOP {shop.RowId} referencedByNpc={directIds.Contains(shop.RowId)} items={shop.Item.Count(entry=>entry.ReceiveItems.Any(receive=>receive.Item.RowId!=0))}");
    var talks=data.Excel.GetSheet<CustomTalk>().Where(talk=>talk.Name.ExtractText().Contains("CommendationCrystal",StringComparison.Ordinal)).Select(talk=>talk.RowId).ToHashSet();
    foreach(var npc in data.Excel.GetSheet<ENpcBase>())foreach(var value in npc.ENpcData)if(talks.Contains(value.RowId))
        Console.WriteLine($"CRYSTALNPC {npc.RowId} '{(residents.TryGetRow(npc.RowId,out var who) ? who.Singular.ExtractText():"")}' talk {value.RowId} '{data.Excel.GetSheet<CustomTalk>().GetRow(value.RowId).Name.ExtractText()}'");
}
// NPC placements in territory layouts (planevent.lgb), for NPCs absent from the Level sheet.
if(Environment.GetEnvironmentVariable("PROBE_LGB") is { Length:>0 } wanted)
{
    // Numeric id, or a name: every ENpcResident whose name matches. Every layout file kind is scanned.
    var targets=uint.TryParse(wanted,out var single) ? new HashSet<uint>{single}
        :residents.Where(row=>row.Singular.ExtractText().Equals(wanted,StringComparison.OrdinalIgnoreCase)).Select(row=>row.RowId).ToHashSet();
    Console.WriteLine($"LGB targets={string.Join(",",targets)}");
    var files=0;var placements=0;var skipped=0;var found=new HashSet<uint>();
    foreach(var territory in data.Excel.GetSheet<TerritoryType>())
    foreach(var kind in new[]{"planevent","planner"})
    {
        var bg=territory.Bg.ExtractText();var cut=bg.IndexOf("/level/",StringComparison.Ordinal);
        if(cut<0)continue;
        var layoutPath=$"bg/{bg[..(cut+1)]}level/{kind}.lgb";var bytes=data.FileExists(layoutPath) ? data.GetFile(layoutPath)?.Data:null;if(bytes is null || bytes.Length<16 || bytes[0]!=0x4C || bytes[1]!=0x47 || bytes[2]!=0x42 || bytes[3]!=0x31 || BitConverter.ToInt32(bytes,4)!=bytes.Length || bytes[12]!=0x4C || bytes[13]!=0x47 || bytes[14]!=0x50 || bytes[15]!=0x31){skipped++;continue;}
        var file=data.GetFile<Lumina.Data.Files.LgbFile>(layoutPath);
        if(file is null)continue;files++;
        foreach(var layer in file.Layers)foreach(var instance in layer.InstanceObjects)
        {
            if(instance.AssetType!=Lumina.Data.Parsing.Layer.LayerEntryType.EventNPC)continue;placements++;
            var npc=(Lumina.Data.Parsing.Layer.LayerCommon.ENPCInstanceObject)instance.Object;
            var target=npc.ParentData.ParentData.BaseId;
            if(!targets.Contains(target))continue;found.Add(target);
            Console.Write($"[{kind}] ");
            var map=territory.Map.Value;var t=instance.Transform.Translation;
            float Coord(float value,int offset)=>41f/(map.SizeFactor/100f)*((value+offset)*(map.SizeFactor/100f)+1024f)/2048f+1f;
            Console.WriteLine($"LGB npc {target} territory {territory.RowId} map {map.RowId} '{map.PlaceName.Value.Name.ExtractText()}' X {Coord(t.X,map.OffsetX):0.0} Y {Coord(t.Z,map.OffsetY):0.0} layer '{layer.Name}'");
        }
    }
    Console.WriteLine($"LGB files={files} eventNpcPlacements={placements} notFound={string.Join(",",targets.Except(found))} skippedLayouts={skipped}");
}
// NPCs whose name mentions marks/wolves: raw data ids and which sheet each id belongs to.
var sheetsById=new (string Name,Func<uint,bool> Has)[]
{
    ("SpecialShop",id=>shops.HasRow(id)),("TopicSelect",id=>data.Excel.GetSheet<TopicSelect>().HasRow(id)),("PreHandler",id=>data.Excel.GetSheet<PreHandler>().HasRow(id)),
    ("CustomTalk",id=>data.Excel.GetSheet<CustomTalk>().HasRow(id)),("InclusionShop",id=>data.Excel.GetSheet<InclusionShop>().HasRow(id)),("GilShop",id=>data.Excel.GetSheet<GilShop>().HasRow(id)),
};
var npcFilter=Environment.GetEnvironmentVariable("PROBE_NPC") ?? "robe noire";
foreach(var resident in residents)
{
    var name=resident.Singular.ExtractText();
    if(!name.Contains(npcFilter,StringComparison.OrdinalIgnoreCase) || !data.Excel.GetSheet<ENpcBase>().TryGetRow(resident.RowId,out var npc))continue;
    // Raw dump of every data id with its first sheet match, then one level of CustomTalk script arguments.
    foreach(var id in npc.ENpcData.Select(value=>value.RowId).Where(id=>id!=0))
        if(data.Excel.GetSheet<CustomTalk>().TryGetRow(id,out var talk))
            Console.WriteLine($"  TALK {id} name='{talk.Name.ExtractText()}' special={talk.SpecialLinks.RowId} args="+string.Join(",",talk.Script.Where(script=>script.ScriptArg!=0).Select(script=>$"{script.ScriptInstruction.ExtractText()}={script.ScriptArg}")));
    if(npcFilter=="robe noire")foreach(var wolf in shops.Where(shop=>shop.Item.Any(entry=>entry.ItemCosts.Any(cost=>cost.CurrencyCost>0 && cost.ItemCost.RowId==25))))
    {
        var linked=data.Excel.GetSheet<ENpcBase>().Any(other=>other.ENpcData.Any(value=>value.RowId==wolf.RowId));
        var sold=wolf.Item.SelectMany(entry=>entry.ReceiveItems).Select(receive=>receive.Item.RowId).Where(item=>item!=0).ToArray();
        Console.WriteLine($"  WOLFSHOP {wolf.RowId} t{wolf.UseCurrencyType} '{wolf.Name.ExtractText()}' items={sold.Length} directNpc={linked} sample="+string.Join(" | ",sold.Take(4).Select(Name)));
    }
    // Which sheets the wolf shop ids neighbour: list special shops 1769740..1769750 with names and first sold item.
    if(npcFilter=="robe noire")foreach(var id in Enumerable.Range(1769735,20).Select(value=>(uint)value))
        if(shops.TryGetRow(id,out var near))Console.WriteLine($"  NEAR {id} '{near.Name.ExtractText()}' first={Name(near.Item.SelectMany(entry=>entry.ReceiveItems).Select(receive=>receive.Item.RowId).FirstOrDefault())}");
    Console.WriteLine($"WOLFNPC {resident.RowId} '{name}' "+string.Join(",",npc.ENpcData.Select(value=>value.RowId).Where(id=>id!=0)
        .Select(id=>$"{id}:{string.Join("/",sheetsById.Where(sheet=>sheet.Has(id)).Select(sheet=>sheet.Name))}")));
}
// Encoded currency evidence: distinct (type, id) pairs below 100 across special shops.
Console.WriteLine("ENCODED "+string.Join(" ",shops.SelectMany(shop=>shop.Item.SelectMany(entry=>entry.ItemCosts)
    .Where(cost=>cost.CurrencyCost>0 && cost.ItemCost.RowId is >0 and <100).Select(cost=>(shop.UseCurrencyType,cost.ItemCost.RowId)))
    .Distinct().OrderBy(pair=>pair.UseCurrencyType).ThenBy(pair=>pair.RowId).Select(pair=>$"t{pair.UseCurrencyType}:{pair.RowId}")));
