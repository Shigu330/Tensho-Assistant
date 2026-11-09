using Lumina;
using Lumina.Data;
using Lumina.Excel.Sheets;

// Read-only evidence for NPC-VENDORS-014: how scrip exchanges encode currencies and how NPCs reach their shops.
if(args.Length is <1 or >2)throw new ArgumentException("Supply installed sqpack directory and optionally an item name.");
using var data=new GameData(args[0],new LuminaOptions{DefaultExcelLanguage=Language.French,CacheFileResources=false});
// PROBE_SRANK (X-76 P5, 0.931): the notorious monsters of every territory (TerritoryType.NotoriousMonsterTerritory -> NotoriousMonster: BNpcName,
// Rank), to check Tensho's S rank table (Hunting/SRankConditions.cs) against the game; read-only.
if(Environment.GetEnvironmentVariable("PROBE_SRANK") is { Length:>0 })
{
    var names=data.Excel.GetSheet<BNpcName>(Language.English);
    foreach(var territory in data.Excel.GetSheet<TerritoryType>().Where(row=>row.NotoriousMonsterTerritory.RowId!=0))
        foreach(var monster in territory.NotoriousMonsterTerritory.ValueNullable?.NotoriousMonsters.Where(m=>m.RowId!=0) ?? [])
            Console.WriteLine($"NM territory {territory.RowId} monster {monster.RowId} name {monster.ValueNullable?.BNpcName.RowId} '{names.GetRowOrDefault(monster.ValueNullable?.BNpcName.RowId ?? 0)?.Singular.ExtractText()}' rank {monster.ValueNullable?.Rank}");
    return;
}
// PROBE_OCEANFISH (X-85 research): every column of the ocean fishing sheets (IKDContentBonus, IKDFishParam, IKDSpot with the fish of
// both spots, IKDRoute's unnamed columns, IKDPlayerMissionCondition with its raw macros), the ocean fish rows of FishParameter, and the
// game texts that name the spectral current or the scrips; read-only.
if(Environment.GetEnvironmentVariable("PROBE_OCEANFISH") is { Length:>0 })
{
    var oceanItems=data.Excel.GetSheet<Item>();var oceanItemsEn=data.Excel.GetSheet<Item>(Language.English);
    string OceanItemName(uint id)=>$"{oceanItemsEn.GetRowOrDefault(id)?.Name.ExtractText()} / {oceanItems.GetRowOrDefault(id)?.Name.ExtractText()}";
    var bonusEn=data.Excel.GetSheet<IKDContentBonus>(Language.English);
    foreach(var row in data.Excel.GetSheet<IKDContentBonus>())
        Console.WriteLine($"BONUS {row.RowId} order {row.Order} x{row.BonusMultiplier} image {row.Image} '{bonusEn.GetRowOrDefault(row.RowId)?.Objective.ExtractText()}' :: '{bonusEn.GetRowOrDefault(row.RowId)?.Requirement.ExtractText()}' / FR '{row.Objective.ExtractText()}' :: '{row.Requirement.ExtractText()}'");
    var oceanFishParams=data.Excel.GetSheet<FishParameter>();
    var ikdByFish=new Dictionary<uint,string>();
    foreach(var row in data.Excel.GetSheet<IKDFishParam>().Where(row=>row.Fish.RowId!=0))
    {
        var fp=oceanFishParams.GetRowOrDefault(row.Fish.RowId);var item=fp?.Item.RowId ?? 0;
        var bonuses=string.Join(",",row.IKDContentBonus.Select(b=>b.RowId).Where(id=>id!=0));
        ikdByFish[item]=$"ikd {row.RowId} bonuses [{bonuses}]";
        Console.WriteLine($"IKDFISH {row.RowId} fishParam {row.Fish.RowId} item {item} '{OceanItemName(item)}' stars {fp?.OceanStars} hidden {fp?.IsHidden} inLog {fp?.IsInLog} spot {fp?.FishingSpot.RowId} record {fp?.FishingRecordType.RowId} bonuses [{bonuses}]");
    }
    var oceanSpotsSheet=data.Excel.GetSheet<FishingSpot>();
    foreach(var spot in data.Excel.GetSheet<IKDSpot>().Where(row=>row.PlaceName.RowId!=0))
    {
        var main=spot.SpotMain.ValueNullable?.Item.Select(i=>i.RowId).Where(i=>i!=0).ToArray() ?? [];
        var sub=spot.SpotSub.ValueNullable?.Item.Select(i=>i.RowId).Where(i=>i!=0).ToArray() ?? [];
        Console.WriteLine($"SPOT {spot.RowId} '{spot.PlaceName.ValueNullable?.Name.ExtractText()}' main {spot.SpotMain.RowId} sub {spot.SpotSub.RowId} territory {spot.SpotMain.ValueNullable?.TerritoryType.RowId}/{spot.SpotSub.ValueNullable?.TerritoryType.RowId}");
        foreach(var id in main)Console.WriteLine($"  MAIN {id} '{OceanItemName(id)}' {(sub.Contains(id) ? "also-spectral":"main-only")} {ikdByFish.GetValueOrDefault(id)}");
        foreach(var id in sub)Console.WriteLine($"  SUB  {id} '{OceanItemName(id)}' {(main.Contains(id) ? "also-main":"spectral-only")} {ikdByFish.GetValueOrDefault(id)}");
    }
    foreach(var route in data.Excel.GetSheet<IKDRoute>().Where(row=>!row.Name.IsEmpty))
        Console.WriteLine($"ROUTE {route.RowId} '{route.Name.ExtractText()}' image {route.Image} u0 {route.Unknown0} u1 {route.Unknown1} u2 {route.Unknown2} instance {route.Instance.RowId} territory {route.Instance.ValueNullable?.TerritoryType.RowId} '{route.Instance.ValueNullable?.Name.ExtractText()}' spots {string.Join(",",route.Spot.Select(s=>s.RowId))} times {string.Join(",",route.Time.Select(t=>t.RowId))}");
    var missionEn=data.Excel.GetSheet<IKDPlayerMissionCondition>(Language.English);
    foreach(var row in data.Excel.GetSheet<IKDPlayerMissionCondition>())
        Console.WriteLine($"MISSION {row.RowId} n {row.Unknown1} FR '{row.Unknown0.ExtractText()}' raw '{row.Unknown0}' / EN '{missionEn.GetRowOrDefault(row.RowId)?.Unknown0.ExtractText()}'");
    foreach(var id in new uint[]{29714,29715,29716,27590,2585,2591,2603,2619,36593,36594,36595,36596,36597})Console.WriteLine($"BAIT {id} '{OceanItemName(id)}'");
    foreach(var row in oceanItems.Where(row=>row.Name.ExtractText().Contains("ssignat",StringComparison.OrdinalIgnoreCase)))Console.WriteLine($"SCRIPITEM {row.RowId} '{OceanItemName(row.RowId)}'");
    foreach(var row in data.Excel.GetSheet<Addon>().Where(row=>row.Text.ExtractText() is var t && (t.Contains("spectral",StringComparison.OrdinalIgnoreCase) || (t.Contains("ssignat",StringComparison.OrdinalIgnoreCase) && t.Length<80))))
        Console.WriteLine($"ADDON {row.RowId} '{row.Text.ExtractText().Replace("\n"," ")}'");
    foreach(var row in data.Excel.GetSheet<LogMessage>().Where(row=>row.Text.ExtractText().Contains("spectral",StringComparison.OrdinalIgnoreCase)))
        Console.WriteLine($"LOG {row.RowId} '{row.Text.ExtractText().Replace("\n"," ")}'");
    // Ocean fish that are collectables: their scrip (CollectablesShopRewardScrip.Currency: 2/4/6/7 = purple/orange crafter/gatherer, Deliveries/CustomDeliveries.cs).
    var oceanFishItems=ikdByFish.Keys.ToHashSet();
    foreach(var row in data.Excel.GetSubrowSheet<CollectablesShopItem>().SelectMany(group=>group).Where(row=>oceanFishItems.Contains(row.Item.RowId)))
    {
        var reward=row.CollectablesShopRewardScrip.ValueNullable;var refine=row.CollectablesShopRefine.ValueNullable;
        Console.WriteLine($"COLLECTABLE item {row.Item.RowId} '{OceanItemName(row.Item.RowId)}' shopItem {row.RowId}.{row.SubrowId} currency {reward?.Currency} reward {reward?.LowReward}/{reward?.MidReward}/{reward?.HighReward} refine {refine?.LowCollectability}/{refine?.MidCollectability}/{refine?.HighCollectability}");
    }
    foreach(var text in data.Excel.GetSheet<InstanceContentTextData>().Where(row=>row.Text.ExtractText().Contains("spectral",StringComparison.OrdinalIgnoreCase)))
        Console.WriteLine($"ICTEXT {text.RowId} '{text.Text.ExtractText().Replace("\n"," ")}'");
    return;
}
// PROBE_LEGENDS (0.526): what the game calls « légendes locales », and every folklore tome: job, division, quest bit, item, how many points or fish use it.
if(Environment.GetEnvironmentVariable("PROBE_LEGENDS") is { Length:>0 })
{
    foreach(var row in data.GetExcelSheet<Lumina.Excel.Sheets.Addon>()!)
    {
        var text=row.Text.ExtractText();
        if(text.Contains("gendes locales",StringComparison.OrdinalIgnoreCase) || text.Contains("gende locale",StringComparison.OrdinalIgnoreCase))Console.WriteLine($"addon {row.RowId}: {text.Replace("\n"," ")[..Math.Min(text.Length,120)]}");
    }
    foreach(var sheetRow in data.GetExcelSheet<Lumina.Excel.Sheets.LogMessage>()!)
    {
        var text=sheetRow.Text.ExtractText();
        if(text.Contains("gendes locales",StringComparison.OrdinalIgnoreCase))Console.WriteLine($"log {sheetRow.RowId}: {text[..Math.Min(text.Length,120)]}");
    }
    foreach(var row in data.GetExcelSheet<Lumina.Excel.Sheets.Item>()!)
    {
        var name=row.Name.ExtractText();
        if(name.StartsWith("Légendes",StringComparison.OrdinalIgnoreCase) || name.Contains("Lég.",StringComparison.OrdinalIgnoreCase))Console.WriteLine($"item {row.RowId}: {name} uicat {row.ItemUICategory.RowId} {row.ItemUICategory.ValueNullable?.Name.ExtractText()} action {row.ItemAction.RowId} type {row.ItemAction.ValueNullable?.Action.RowId} icon {row.Icon}");
    }
    foreach(var group in data.GetExcelSheet<Lumina.Excel.Sheets.GatheringSubCategory>()!.Where(row=>row.Item.RowId!=0).GroupBy(row=>(Job:row.ClassJob.RowId,row.Item.RowId,Bit:row.Quest.RowId,row.Division)).OrderBy(group=>group.Key.Job).ThenBy(group=>group.Key.Bit))
        Console.WriteLine($"tome job {group.Key.Job} bit {group.Key.Bit} division {group.Key.Division} item {group.Key.RowId} {group.First().Item.Value.Name.ExtractText()} rows {group.Count()}");
    return;
}

// PROBE_GPACTIONS (0.517): the gathering actions' UnlockLink, ClassJobLevel and IsRoleAction, to know what « unlocked » can mean for them.
if(Environment.GetEnvironmentVariable("PROBE_GPACTIONS") is { Length:>0 })
{
    var gaSheet=data.GetExcelSheet<Lumina.Excel.Sheets.Action>()!;
    foreach(var id in new uint[]{235,237,295,239,241,21177,25589,21203,4073,272,232,4072,218,220,294,222,224,21178,25590,21204,4087,273,215,4086})
    {
        var row=gaSheet.GetRow(id);
        Console.WriteLine($"action {id}: {row.Name.ExtractText()} level {row.ClassJobLevel} unlock {row.UnlockLink.RowId} job {row.ClassJob.RowId} category {row.ClassJobCategory.RowId}");
    }
    // 0.519: every miner and botanist action with its cost and text, and how crystals are told apart.
    foreach(var row in gaSheet.Where(row=>row.ClassJob.RowId is 16 or 17 && row.ClassJobLevel>0 && !row.Name.IsEmpty).OrderBy(row=>row.ClassJob.RowId).ThenBy(row=>row.ClassJobLevel))
    {
        var text=data.GetExcelSheet<Lumina.Excel.Sheets.ActionTransient>()!.GetRowOrDefault(row.RowId)?.Description.ExtractText().Replace("\n"," ") ?? "";
        Console.WriteLine($"all {row.RowId}: job {row.ClassJob.RowId} lv {row.ClassJobLevel} cost {row.PrimaryCostValue} type {row.PrimaryCostType} unlock {row.UnlockLink.RowId} recast {row.Recast100ms} group {row.CooldownGroup} {row.Name.ExtractText()} :: {text}");
    }
    var gaItems=data.GetExcelSheet<Lumina.Excel.Sheets.Item>()!;
    foreach(var id in Enumerable.Range(1,20).Select(id=>(uint)id).Append(5111u))
    {
        var item=gaItems.GetRow(id);
        Console.WriteLine($"item {id}: {item.Name.ExtractText()} uicat {item.ItemUICategory.RowId} search {item.ItemSearchCategory.RowId} stack {item.StackSize}");
    }
    return;
}

// PROBE_FOLKLORE (0.517): what a gathering point needs: GatheringSubCategory (tome, quest, division) and the points that use it.
if(Environment.GetEnvironmentVariable("PROBE_FOLKLORE") is { Length:>0 })
{
    var fkPoints=data.GetExcelSheet<Lumina.Excel.Sheets.GatheringPoint>()!;
    var fkUse=fkPoints.GroupBy(point=>point.GatheringSubCategory.RowId).ToDictionary(group=>group.Key,group=>group.Count());
    foreach(var row in data.GetExcelSheet<Lumina.Excel.Sheets.GatheringSubCategory>()!)
    {
        var book=row.FolkloreBook.ExtractText();
        if(row.RowId==0 && book.Length==0)continue;
        Console.WriteLine($"sub {row.RowId}: division {row.Division}, item {row.Item.RowId} {row.Item.ValueNullable?.Name.ExtractText()}, quest {row.Quest.RowId}, job {row.ClassJob.RowId}, book « {book} », points {fkUse.GetValueOrDefault(row.RowId)}");
    }
    Console.WriteLine($"points without sub: {fkUse.GetValueOrDefault(0u)} of {fkPoints.Count}");
    var fkPlace=Environment.GetEnvironmentVariable("PROBE_FOLKLORE")!;
    foreach(var point in fkPoints.Where(point=>point.PlaceName.ValueNullable?.Name.ExtractText().Contains(fkPlace,StringComparison.OrdinalIgnoreCase)==true))
        Console.WriteLine($"point {point.RowId}: base {point.GatheringPointBase.RowId}, level {point.GatheringPointBase.Value.GatheringLevel}, type {point.Type}, sub {point.GatheringSubCategory.RowId}, place {point.PlaceName.Value.Name.ExtractText()}");
    return;
}

// PROBE_GATHERBONUS (0.515): node effects: GatheringCondition and GatheringPointBonusType in English and French, how often each pair is used, and the bonus rows of the normal manganese nodes.
if(Environment.GetEnvironmentVariable("PROBE_GATHERBONUS") is { Length:>0 })
{
    var gbGame=data;
    foreach(var language in new[]{Lumina.Data.Language.English,Lumina.Data.Language.French})
    {
        Console.WriteLine("language "+language);
        foreach(var row in gbGame.GetExcelSheet<Lumina.Excel.Sheets.GatheringCondition>(language)!)Console.WriteLine($"condition {row.RowId}: {row.Text.ExtractText()}");
        foreach(var row in gbGame.GetExcelSheet<Lumina.Excel.Sheets.GatheringPointBonusType>(language)!)Console.WriteLine($"type {row.RowId}: {row.Text.ExtractText()}");
    }
    var gbBonus=gbGame.GetExcelSheet<Lumina.Excel.Sheets.GatheringPointBonus>()!;
    foreach(var group in gbBonus.GroupBy(row=>(Condition:row.Condition.RowId,Type:row.BonusType.RowId)).OrderBy(group=>group.Key))
        Console.WriteLine($"pair condition {group.Key.Condition} type {group.Key.Type}: {group.Count()} rows, values {string.Join(",",group.Select(row=>row.BonusValue).Distinct().Order())}, needs {group.Min(row=>row.ConditionValue)}-{group.Max(row=>row.ConditionValue)}");
    var gbPoints=gbGame.GetExcelSheet<Lumina.Excel.Sheets.GatheringPoint>()!;
    var gbWith=gbPoints.Count(point=>point.GatheringPointBonus.Any(link=>link.RowId!=0));
    Console.WriteLine($"points {gbPoints.Count} with a bonus {gbWith}");
    foreach(var bonus in gbBonus.Where(row=>row.ConditionValue==770))
    {
        var users=gbPoints.Where(point=>point.GatheringPointBonus.Any(link=>link.RowId==bonus.RowId)).Select(point=>point.RowId).Take(6);
        Console.WriteLine($"bonus row {bonus.RowId}: condition {bonus.Condition.RowId} >= {bonus.ConditionValue}, type {bonus.BonusType.RowId} value {bonus.BonusValue}; points {string.Join(",",users)}");
    }
    return;
}

// PROBE_VESSELPARTS (R-11 step 3): submarine and airship build data — every field and row of SubmarinePart, SubmarineRank,
// AirshipExplorationPart/Level/Point and SubmarineExploration (typed fields next to the raw column layout, to see unmapped columns),
// and how a part row maps to its Item (name): items whose AdditionalData points to a part sheet, their FilterGroup and UI category.
if(Environment.GetEnvironmentVariable("PROBE_VESSELPARTS") is { Length:>0 })
{
    string VpCell(object? value)
    {
        if(value is null)return "";
        if(value is Lumina.Text.ReadOnly.ReadOnlySeString text)return text.ExtractText();
        if(value is Lumina.Excel.RowRef reference)return $"{reference.RowId}";
        var valueType=value.GetType();
        if(valueType.IsGenericType && valueType.GetGenericTypeDefinition()==typeof(Lumina.Excel.RowRef<>))return $"{valueType.GetProperty("RowId")!.GetValue(value)}";
        return value.ToString()??"";
    }
    string VpCsv(string text)=>text.Contains(',') || text.Contains('"') || text.Contains('\n') ? "\""+text.Replace("\"","\"\"").Replace("\n"," ")+"\"":text;
    void VpTyped<T>(Language language) where T:struct,Lumina.Excel.IExcelRow<T>
    {
        var properties=typeof(T).GetProperties().Where(property=>property.Name is not ("RowOffset" or "ExcelPage")).ToArray();
        var sheet=data.Excel.GetSheet<T>(language);
        Console.WriteLine($"CSV {typeof(T).Name} ({language}) rows {sheet.Count} fields "+string.Join(",",properties.Select(property=>property.Name+":"+property.PropertyType.Name)));
        Console.WriteLine(string.Join(",",properties.Select(property=>property.Name)));
        foreach(var row in sheet)Console.WriteLine(string.Join(",",properties.Select(property=>VpCsv(VpCell(property.GetValue(row))))));
        Console.WriteLine("END");
    }
    void VpRaw(string name,bool rows)
    {
        var raw=data.Excel.GetSheet<Lumina.Excel.RawRow>(null,name);
        var columns=raw.Columns;
        Console.WriteLine($"RAW {name} columns {columns.Count} rows {raw.Count} layout "+string.Join(" ",columns.Select((column,index)=>$"{index}:{column.Type}@{column.Offset}")));
        if(!rows)return;
        foreach(var row in raw)Console.WriteLine($"RAWROW {name} {row.RowId}: "+string.Join(" | ",Enumerable.Range(0,columns.Count).Select(index=>VpCell(row.ReadColumn(index)))));
    }
    Console.WriteLine("SHEETS "+string.Join(",",data.Excel.SheetNames.Where(sheetName=>sheetName.Contains("Submarine",StringComparison.OrdinalIgnoreCase) || sheetName.Contains("Airship",StringComparison.OrdinalIgnoreCase))));
    foreach(var name in new[]{"SubmarinePart","SubmarineRank","SubmarineExploration","SubmarineMap","SubmarineSpecCategory","AirshipExplorationPart","AirshipExplorationLevel","AirshipExplorationPoint","AirshipExplorationParamType","AirshipSkyIsland"})
        VpRaw(name,Environment.GetEnvironmentVariable("PROBE_VESSELPARTS")=="raw");
    VpTyped<SubmarinePart>(Language.English);
    VpTyped<SubmarineRank>(Language.English);
    VpTyped<AirshipExplorationPart>(Language.English);
    VpTyped<AirshipExplorationLevel>(Language.English);
    VpTyped<AirshipExplorationPoint>(Language.English);
    VpTyped<AirshipExplorationPoint>(Language.French);
    VpTyped<SubmarineExploration>(Language.English);
    VpTyped<SubmarineSpecCategory>(Language.English);
    VpTyped<SubmarineSpecCategory>(Language.French);
    VpTyped<AirshipExplorationParamType>(Language.English);
    VpTyped<AirshipExplorationParamType>(Language.French);
    VpTyped<SubmarineMap>(Language.English);
    // Part → Item: AdditionalData typed reference (RowType) on every item, then the same items found by name for an independent check.
    var vpItemsEnglish=data.Excel.GetSheet<Item>(Language.English);
    var vpItemsFrench=data.Excel.GetSheet<Item>(Language.French);
    var vpCategories=data.Excel.GetSheet<ItemUICategory>(Language.English);
    foreach(var category in vpCategories.Where(category=>category.Name.ExtractText() is { } categoryName && (categoryName.Contains("Submersible",StringComparison.OrdinalIgnoreCase) || categoryName.Contains("Airship",StringComparison.OrdinalIgnoreCase) || categoryName.Contains("Submarine",StringComparison.OrdinalIgnoreCase))))
        Console.WriteLine($"UICATEGORY {category.RowId} '{category.Name.ExtractText()}'");
    var vpByType=vpItemsEnglish.Where(item=>item.AdditionalData.Is<SubmarinePart>() || item.AdditionalData.Is<AirshipExplorationPart>()).ToArray();
    Console.WriteLine($"TYPED part items {vpByType.Length} filterGroups "+string.Join(",",vpByType.GroupBy(item=>(item.FilterGroup,item.AdditionalData.RowType?.Name)).Select(group=>$"{group.Key.FilterGroup}->{group.Key.Name}:{group.Count()}")));
    var vpByName=vpItemsEnglish.Where(item=>item.Name.ExtractText() is { } itemName && (itemName.Contains("-class ",StringComparison.Ordinal) || itemName.Contains("-type ",StringComparison.Ordinal))
        && (itemName.EndsWith(" Bow",StringComparison.Ordinal) || itemName.EndsWith(" Bridge",StringComparison.Ordinal) || itemName.EndsWith(" Hull",StringComparison.Ordinal) || itemName.EndsWith(" Stern",StringComparison.Ordinal)
            || itemName.EndsWith(" Forecastle",StringComparison.Ordinal) || itemName.EndsWith(" Aftcastle",StringComparison.Ordinal) || itemName.EndsWith(" Rigging",StringComparison.Ordinal))).ToArray();
    Console.WriteLine($"BYNAME part-like items {vpByName.Length}; not typed: "+string.Join(";",vpByName.Where(item=>!vpByType.Any(typed=>typed.RowId==item.RowId)).Select(item=>$"{item.RowId} {item.Name.ExtractText()} fg {item.FilterGroup} add {item.AdditionalData.RowId} type {item.AdditionalData.RowType?.Name} ui {item.ItemUICategory.RowId}")));
    Console.WriteLine("CSV ItemToPart");
    Console.WriteLine("ItemId,NameEn,NameFr,FilterGroup,AdditionalDataRowId,AdditionalDataSheet,ItemUICategory,ItemUICategoryName,LevelItem,PriceLow,FoundByName");
    foreach(var item in vpByType.OrderBy(item=>item.AdditionalData.RowType?.Name).ThenBy(item=>item.AdditionalData.RowId))
        Console.WriteLine(string.Join(",",new[]{$"{item.RowId}",VpCsv(item.Name.ExtractText()),VpCsv(vpItemsFrench.GetRow(item.RowId).Name.ExtractText()),$"{item.FilterGroup}",$"{item.AdditionalData.RowId}",item.AdditionalData.RowType?.Name??"",
            $"{item.ItemUICategory.RowId}",VpCsv(item.ItemUICategory.IsValid ? item.ItemUICategory.Value.Name.ExtractText():""),$"{item.LevelItem.RowId}",$"{item.PriceLow}",$"{vpByName.Any(named=>named.RowId==item.RowId)}"}));
    Console.WriteLine("END");
    // Part rows without an item, and items pointing twice at the same part row.
    var vpSubParts=data.Excel.GetSheet<SubmarinePart>();var vpAirParts=data.Excel.GetSheet<AirshipExplorationPart>();
    Console.WriteLine("SUB parts without item: "+string.Join(",",vpSubParts.Where(part=>!vpByType.Any(item=>item.AdditionalData.Is<SubmarinePart>() && item.AdditionalData.RowId==part.RowId)).Select(part=>part.RowId)));
    Console.WriteLine("AIR parts without item: "+string.Join(",",vpAirParts.Where(part=>!vpByType.Any(item=>item.AdditionalData.Is<AirshipExplorationPart>() && item.AdditionalData.RowId==part.RowId)).Select(part=>part.RowId)));
    Console.WriteLine("Duplicate targets: "+string.Join(",",vpByType.GroupBy(item=>(item.AdditionalData.RowType?.Name,item.AdditionalData.RowId)).Where(group=>group.Count()>1).Select(group=>$"{group.Key.Name}#{group.Key.RowId}:{string.Join("/",group.Select(item=>item.RowId))}")));
    // Independent consistency: the item's UI category (game text) against the part row's Slot, and the item name's class prefix against Class.
    string VpPrefix(string itemName)=>itemName[..itemName.IndexOf(itemName.Contains("-class ",StringComparison.Ordinal) ? "-class ":"-type ",StringComparison.Ordinal)];
    Console.WriteLine("SUB slot<->category "+string.Join(" ",vpByType.Where(item=>item.FilterGroup==36).GroupBy(item=>(vpSubParts.GetRow(item.AdditionalData.RowId).Slot,item.ItemUICategory.Value.Name.ExtractText())).Select(group=>$"slot{group.Key.Slot}={group.Key.Item2}x{group.Count()}")));
    Console.WriteLine("AIR slot<->category "+string.Join(" ",vpByType.Where(item=>item.FilterGroup==28).GroupBy(item=>(vpAirParts.GetRow(item.AdditionalData.RowId).Slot,item.ItemUICategory.Value.Name.ExtractText())).Select(group=>$"slot{group.Key.Slot}={group.Key.Item2}x{group.Count()}")));
    Console.WriteLine("SUB class<->name "+string.Join(" ",vpByType.Where(item=>item.FilterGroup==36).GroupBy(item=>(vpSubParts.GetRow(item.AdditionalData.RowId).Class,VpPrefix(item.Name.ExtractText()))).Select(group=>$"class{group.Key.Class}={group.Key.Item2}x{group.Count()}")));
    Console.WriteLine("AIR class<->name "+string.Join(" ",vpByType.Where(item=>item.FilterGroup==28).GroupBy(item=>(vpAirParts.GetRow(item.AdditionalData.RowId).Class,VpPrefix(item.Name.ExtractText()))).Select(group=>$"class{group.Key.Class}={group.Key.Item2}x{group.Count()}")));
    // Rank bonuses: cumulative totals (never decreasing) or per-rank increments?
    var vpRanks=data.Excel.GetSheet<SubmarineRank>().Where(rank=>rank.RowId is >0 and <=145).ToArray();
    Console.WriteLine("RANK bonuses non-decreasing: "+Enumerable.Range(1,vpRanks.Length-1).All(index=>vpRanks[index].SurveillanceBonus>=vpRanks[index-1].SurveillanceBonus && vpRanks[index].RetrievalBonus>=vpRanks[index-1].RetrievalBonus
        && vpRanks[index].SpeedBonus>=vpRanks[index-1].SpeedBonus && vpRanks[index].RangeBonus>=vpRanks[index-1].RangeBonus && vpRanks[index].FavorBonus>=vpRanks[index-1].FavorBonus)
        +"; last row with ExpToNext>0: "+data.Excel.GetSheet<SubmarineRank>().Where(rank=>rank.ExpToNext>0).Max(rank=>rank.RowId)
        +"; airship last row with ExpToNext>0: "+data.Excel.GetSheet<AirshipExplorationLevel>().Where(level=>level.ExpToNext>0).Max(level=>level.RowId));
    // Part tier components against the rank capacity at the part's own rank requirement (4 parts of the same tier).
    foreach(var tier in vpSubParts.Where(part=>part.RowId>0).GroupBy(part=>(part.Rank,part.Components)))
        Console.WriteLine($"SUB tier rank {tier.Key.Rank} components {tier.Key.Components} x4={tier.Key.Components*4} capacity@rank {data.Excel.GetSheet<SubmarineRank>().GetRow(tier.Key.Rank).Capacity}");
    foreach(var tier in vpAirParts.Where(part=>part.RowId>0).GroupBy(part=>(part.Rank,part.Components)))
        Console.WriteLine($"AIR tier rank {tier.Key.Rank} components {tier.Key.Components} x4={tier.Key.Components*4} capacity@rank {data.Excel.GetSheet<AirshipExplorationLevel>().GetRow(tier.Key.Rank).Capacity}");
    Console.WriteLine("AIRPOINT unknown columns nonzero rows: "+string.Join(",",data.Excel.GetSheet<AirshipExplorationPoint>().Where(point=>point.Unknown0!=0 || point.Unknown1!=0 || point.Unknown2!=0).Select(point=>point.RowId)));
    VpRaw("SubmarineExplorationLog",false);VpRaw("AirshipExplorationLog",false);
    return;
}
// PROBE_MELD (0.312): melding caps — ItemLevel column per BaseParam, slot percentages, check against an item's own top substat.
if(Environment.GetEnvironmentVariable("PROBE_MELD") is { Length:>0 })
{
    Console.WriteLine("ITEMLEVEL "+string.Join(",",typeof(ItemLevel).GetProperties().Where(property=>property.PropertyType==typeof(ushort)).Select(property=>property.Name)));
    var levels=data.Excel.GetSheet<ItemLevel>();var baseParams=data.Excel.GetSheet<BaseParam>();
    int Cap(uint level,uint param,string slotColumn,string levelColumn)
    {
        var row=levels.GetRow(level);var bp=baseParams.GetRow(param);
        var value=(ushort)typeof(ItemLevel).GetProperty(levelColumn)!.GetValue(row)!;
        var percent=(ushort)typeof(BaseParam).GetProperty(slotColumn)!.GetValue(bp)!;
        return (int)Math.Round(value*percent/1000.0,MidpointRounding.AwayFromZero);
    }
    foreach(var id in new uint[]{34830,34831})
    {
        var item=data.Excel.GetSheet<Item>().GetRow(id);
        var stats=string.Join(",",item.BaseParam.Zip(item.BaseParamValue).Where(pair=>pair.First.RowId!=0).Select(pair=>$"{pair.First.RowId}:{pair.Second}"));
        Console.WriteLine($"ITEM {id} ilvl {item.LevelItem.RowId} stats {stats} advanced {item.IsAdvancedMeldingPermitted} capTen {Cap(item.LevelItem.RowId,19,"EarringPercent","Tenacity")} capCrit {Cap(item.LevelItem.RowId,27,"EarringPercent","CriticalHit")} capDet {Cap(item.LevelItem.RowId,44,"EarringPercent","Determination")} capSks {Cap(item.LevelItem.RowId,45,"EarringPercent","SkillSpeed")}");
    }
    // A 7.x body piece with its stats and caps.
    var body=data.Excel.GetSheet<Item>().Where(row=>row.LevelEquip==100 && row.EquipSlotCategory.RowId==4 && row.LevelItem.RowId>=730).Take(2);
    foreach(var item in body)
    {
        var stats=string.Join(",",item.BaseParam.Zip(item.BaseParamValue).Where(pair=>pair.First.RowId!=0).Select(pair=>$"{pair.First.RowId}:{pair.Second}"));
        Console.WriteLine($"BODY {item.RowId} {item.Name.ExtractText()} ilvl {item.LevelItem.RowId} stats {stats} slots {item.MateriaSlotCount} advanced {item.IsAdvancedMeldingPermitted} capCrit {Cap(item.LevelItem.RowId,27,"ChestPercent","CriticalHit")} capDet {Cap(item.LevelItem.RowId,44,"ChestPercent","Determination")} capDh {Cap(item.LevelItem.RowId,22,"ChestPercent","DirectHitRate")}");
    }
    Console.WriteLine("MATERIA PROPS "+string.Join(",",typeof(Materia).GetProperties().Select(property=>property.Name+":"+property.PropertyType.Name)));
    foreach(var materia in data.Excel.GetSheet<Materia>().Where(row=>row.BaseParam.RowId!=0).Take(30))
        Console.WriteLine($"MATERIA {materia.RowId} param {materia.BaseParam.RowId} values {string.Join(",",materia.Value)} items {string.Join(",",materia.Item.Select(item=>item.RowId))} first {(materia.Item[0].IsValid ? materia.Item[0].Value.Name.ExtractText():"")}");
    return;
}
// PROBE_GEAR (0.310): BaseParam names, ClassJob abbreviations against ClassJobCategory columns, a sample of gear rows.
if(Environment.GetEnvironmentVariable("PROBE_GEAR") is { Length:>0 })
{
    foreach(var param in data.Excel.GetSheet<BaseParam>().Where(row=>!row.Name.IsEmpty).Take(80))Console.Write($"{param.RowId}={param.Name.ExtractText()}; ");
    Console.WriteLine();
    var english=data.Excel.GetSheet<ClassJob>(Language.English);
    var columns=typeof(ClassJobCategory).GetProperties().Where(property=>property.PropertyType==typeof(bool)).Select(property=>property.Name).ToHashSet();
    foreach(var job in english.Where(row=>!row.Abbreviation.IsEmpty))Console.Write($"{job.RowId}:{job.Abbreviation.ExtractText()}{(columns.Contains(job.Abbreviation.ExtractText()) ? "":"(MISSING)")} ");
    Console.WriteLine();
    var gear=data.Excel.GetSheet<Item>().Where(row=>row.EquipSlotCategory.RowId!=0 && row.LevelEquip==90).Take(4);
    foreach(var item in gear)
        Console.WriteLine($"ITEM {item.RowId} {item.Name.ExtractText()} slot {item.EquipSlotCategory.RowId} cjc {item.ClassJobCategory.RowId} '{item.ClassJobCategory.Value.Name.ExtractText()}' lvl {item.LevelEquip} ilvl {item.LevelItem.RowId} def {item.DefensePhys}/{item.DefenseMag} dmg {item.DamagePhys}/{item.DamageMag} materia {item.MateriaSlotCount} params "
            +string.Join(",",item.BaseParam.Zip(item.BaseParamValue).Where(pair=>pair.First.RowId!=0).Select(pair=>$"{pair.First.RowId}:{pair.Second}"))
            +" hq "+string.Join(",",item.BaseParamSpecial.Zip(item.BaseParamValueSpecial).Where(pair=>pair.First.RowId!=0).Select(pair=>$"{pair.First.RowId}:{pair.Second}")));
    Console.WriteLine("SLOTCAT "+string.Join(",",typeof(EquipSlotCategory).GetProperties().Select(property=>property.Name)));
    var materia=data.Excel.GetSheet<Materia>().First(row=>row.BaseParam.RowId!=0);
    Console.WriteLine($"MATERIA {materia.RowId} param {materia.BaseParam.RowId} values {string.Join(",",materia.Value)}");
    return;
}
// PROBE_HOUSING (0.444, X-09): residential districts (TerritoryType by intended use) with their place names.
if(Environment.GetEnvironmentVariable("PROBE_HOUSING") is { Length:>0 })
{
    foreach(var use in data.Excel.GetSheet<TerritoryType>().Where(row=>!row.PlaceName.ValueNullable?.Name.IsEmpty ?? false).GroupBy(row=>row.TerritoryIntendedUse.RowId).Where(group=>group.Key is 13 or 14 or 15 or 16))
        Console.WriteLine($"USE {use.Key} "+string.Join(" | ",use.Take(8).Select(row=>$"{row.RowId} '{row.PlaceName.ValueNullable?.Name.ExtractText()}'")));
    return;
}
// PROBE_BINGO (0.443, N-02): Wondrous Tails tasks (WeeklyBingoOrderData): type, data row, text.
if(Environment.GetEnvironmentVariable("PROBE_BINGO") is { Length:>0 })
{
    var cfc=data.Excel.GetSheet<ContentFinderCondition>();
    foreach(var type in data.Excel.GetSheet<WeeklyBingoOrderData>().GroupBy(row=>row.Type).OrderBy(group=>group.Key))
        Console.WriteLine($"TYPE {type.Key} rows {type.Count()} e.g. "+string.Join(" | ",type.Take(3).Select(row=>$"{row.RowId}: data {row.Data.RowId} cfc '{(type.Key==0 ? cfc.GetRowOrDefault(row.Data.RowId)?.Name.ExtractText():"")}' text '{row.Text.ValueNullable?.Description.ExtractText()}'")));
    return;
}
// PROBE_NOTORIOUS (0.441, N-09): hunt marks (NotoriousMonster): rank values with examples.
if(Environment.GetEnvironmentVariable("PROBE_NOTORIOUS") is { Length:>0 })
{
    var rows=data.Excel.GetSheet<NotoriousMonster>(Language.English).Where(row=>row.BNpcBase.RowId!=0).ToArray();
    foreach(var rank in rows.GroupBy(row=>row.Rank).OrderBy(group=>group.Key))
        Console.WriteLine($"RANK {rank.Key} rows {rank.Count()} e.g. "+string.Join(" | ",rank.Take(4).Select(row=>$"{row.BNpcName.ValueNullable?.Singular.ExtractText()} base {row.BNpcBase.RowId}")));
    return;
}
// PROBE_MOBHUNT (0.440, N-01): MobHuntOrderType rows (mark index → bill item, order start, amount) and one order's targets.
if(Environment.GetEnvironmentVariable("PROBE_MOBHUNT") is { Length:>0 })
{
    var types=data.Excel.GetSheet<MobHuntOrderType>(Language.English);
    foreach(var type in types)
        Console.WriteLine($"TYPE {type.RowId} type {type.Type} amount {type.OrderAmount} start {type.OrderStart.RowId} item '{type.EventItem.ValueNullable?.Name.ExtractText()}' quest {type.Quest.RowId}");
    var orders=data.Excel.GetSubrowSheet<MobHuntOrder>(Language.English);
    foreach(var group in orders.Take(3))
        foreach(var order in group)
            Console.WriteLine($"ORDER {order.RowId}.{order.SubrowId} kills {order.NeededKills} type {order.Type} rank {order.Rank} target '{order.Target.ValueNullable?.Name.ValueNullable?.Singular.ExtractText()}' map {order.Target.ValueNullable?.Map.RowId} place '{order.Target.ValueNullable?.PlaceName.ValueNullable?.Name.ExtractText()}' fate {order.Target.ValueNullable?.FATE.RowId}");
    Console.WriteLine($"ORDERGROUPS {orders.Count}");
    return;
}
// PROBE_CRAFTACTIONS (0.434, A-13): carpenter rows of CraftAction (and Action for crafter buffs) by English name: id, level, CP,
// the canonical row of each name, so the simulator's specs match the sheet the catalogue loader checks.
if(Environment.GetEnvironmentVariable("PROBE_CRAFTACTIONS") is { Length:>0 })
{
    var names=new[]{"Rapid Synthesis","Hasty Touch","Daring Touch","Intensive Synthesis","Precise Touch","Tricks of the Trade","Final Appraisal","Heart and Soul","Quick Innovation","Careful Observation","Basic Synthesis","Innovation"};
    var crafts=data.Excel.GetSheet<CraftAction>(Language.English);
    foreach(var row in crafts.Where(row=>names.Contains(row.Name.ExtractText()) && row.ClassJob.RowId==8 || row.Name.ExtractText().Contains("Daring")))
        Console.WriteLine($"CRAFTACTION {row.RowId} '{row.Name.ExtractText()}' job {row.ClassJob.RowId} lv {row.ClassJobLevel} cost {row.Cost} CRP {row.CRP.RowId} ALC {row.ALC.RowId}");
    foreach(var row in data.Excel.GetSheet<Lumina.Excel.Sheets.Action>(Language.English).Where(row=>names.Contains(row.Name.ExtractText()) && row.ClassJob.RowId==8))
        Console.WriteLine($"ACTION {row.RowId} '{row.Name.ExtractText()}' job {row.ClassJob.RowId} lv {row.ClassJobLevel} costType {row.PrimaryCostType} cost {row.PrimaryCostValue} second {row.SecondaryCostType}/{row.SecondaryCostValue.RowId}");
    return;
}
// PROBE_RECIPEROWS (0.428, user report « cannot display a recipe »): every Recipe row of an item (by French name), with its flags.
if(Environment.GetEnvironmentVariable("PROBE_RECIPEROWS") is { Length:>0 } recipeName)
{
    foreach(var recipe in data.Excel.GetSheet<Recipe>().Where(row=>row.ItemResult.RowId!=0 && row.ItemResult.ValueNullable?.Name.ExtractText()==recipeName))
        Console.WriteLine($"RECIPE {recipe.RowId} craftType {recipe.CraftType.RowId} level {recipe.RecipeLevelTable.ValueNullable?.ClassJobLevel} secretBook {recipe.SecretRecipeBook.RowId} "
            +$"expert {recipe.IsExpert} specialist {recipe.IsSpecializationRequired} quest {recipe.Quest.RowId} number {recipe.Number} hide? {recipe.CanHq}/{recipe.CanQuickSynth}");
    return;
}
// PROBE_CRAFTFOOD (0.420, A-02): items whose ItemAction points (Data[1]) to an ItemFood row with crafting attributes
// (BaseParam names in English), Data[0] (the status: well fed or medicated) and the values / caps (NQ and HQ).
if(Environment.GetEnvironmentVariable("PROBE_CRAFTFOOD") is { Length:>0 })
{
    var baseParams=data.Excel.GetSheet<BaseParam>(Language.English);var foods=data.Excel.GetSheet<ItemFood>();var statuses=data.Excel.GetSheet<Status>(Language.English);
    foreach(var bp in baseParams.Where(row=>row.RowId is 11 or 70 or 71))Console.WriteLine($"BASEPARAM {bp.RowId} '{bp.Name.ExtractText()}'");
    var shown=0;var total=0;var data0=new Dictionary<uint,int>();
    foreach(var item in data.Excel.GetSheet<Item>(Language.English).Where(row=>row.ItemAction.RowId!=0))
    {
        if(item.ItemAction.ValueNullable is not { } action || action.Data.Count<2)continue;
        if(foods.GetRowOrDefault(action.Data[1]) is not { } food)continue;
        var craft=food.Params.Where(param=>param.BaseParam.RowId is 11 or 70 or 71).ToArray();
        if(craft.Length==0)continue;
        total++;data0[action.Data[0]]=data0.GetValueOrDefault(action.Data[0])+1;
        if(shown++<8)Console.WriteLine($"FOOD {item.RowId} '{item.Name.ExtractText()}' data0 {action.Data[0]} '{statuses.GetRowOrDefault(action.Data[0])?.Name.ExtractText()}' data2 {action.Data[2]} food {action.Data[1]} "
            +string.Join(" | ",craft.Select(param=>$"bp{param.BaseParam.RowId} rel {param.IsRelative} {param.Value}%/max {param.Max} HQ {param.ValueHQ}%/max {param.MaxHQ}")));
    }
    Console.WriteLine($"TOTAL {total} by data0 "+string.Join(", ",data0.Select(pair=>$"{pair.Key}:{pair.Value}")));
    return;
}
// PROBE_OCEANLIVE (0.418, R-07): mission conditions, time-of-day rows, spots (main / spectral) and their fish.
if(Environment.GetEnvironmentVariable("PROBE_OCEANLIVE") is { Length:>0 })
{
    foreach(var row in data.Excel.GetSheet<IKDPlayerMissionCondition>(Language.English))Console.WriteLine($"MISSION {row.RowId} '{row.Unknown0.ExtractText()}' n {row.Unknown1}");
    foreach(var row in data.Excel.GetSheet<IKDTimeDefine>())Console.WriteLine($"TIME {row.RowId} tod {row.TimeOfDay}");
    foreach(var spot in data.Excel.GetSheet<IKDSpot>(Language.English).Where(row=>row.PlaceName.RowId!=0).Take(14))
        Console.WriteLine($"SPOT {spot.RowId} '{spot.PlaceName.ValueNullable?.Name.ExtractText()}' main {spot.SpotMain.RowId} ({spot.SpotMain.ValueNullable?.Item.Count(item=>item.RowId!=0)} fish) sub {spot.SpotSub.RowId} ({spot.SpotSub.ValueNullable?.Item.Count(item=>item.RowId!=0)} fish)");
    foreach(var route in data.Excel.GetSheet<IKDRoute>(Language.English).Where(row=>!row.Name.IsEmpty).Take(3))
        Console.WriteLine($"ROUTE {route.RowId} spots {string.Join(",",route.Spot.Select(s=>s.RowId))} times {string.Join(",",route.Time.Select(t=>t.RowId))} instance {route.Instance.RowId}");
    var fishParams=data.Excel.GetSheet<IKDFishParam>().Count(row=>row.Fish.RowId!=0);
    Console.WriteLine($"IKDFISHPARAM {fishParams}");
    return;
}
// PROBE_GATHERTYPE (0.411, R-10): GatheringType rows (names in English) and how many gathering point bases use each, to map a point to its job.
if(Environment.GetEnvironmentVariable("PROBE_GATHERTYPE") is { Length:>0 })
{
    var bases=data.Excel.GetSheet<GatheringPointBase>().GroupBy(row=>row.GatheringType.RowId).ToDictionary(group=>group.Key,group=>group.Count());
    foreach(var type in data.Excel.GetSheet<GatheringType>(Language.English))Console.WriteLine($"TYPE {type.RowId} '{type.Name.ExtractText()}' bases {bases.GetValueOrDefault(type.RowId)}");
    foreach(var job in data.Excel.GetSheet<ClassJob>(Language.English).Where(row=>row.RowId is 16 or 17 or 18))Console.WriteLine($"JOB {job.RowId} '{job.Name.ExtractText()}'");
    return;
}
// PROBE_GPACTIONS (0.410, R-09): every miner and botanist action (ClassJob 16/17): level, GP cost, English and French names, description.
if(Environment.GetEnvironmentVariable("PROBE_GPACTIONS") is { Length:>0 })
{
    var english=data.Excel.GetSheet<Lumina.Excel.Sheets.Action>(Language.English);var transient=data.Excel.GetSheet<ActionTransient>(Language.English);
    foreach(var row in data.Excel.GetSheet<Lumina.Excel.Sheets.Action>().Where(row=>row.ClassJob.RowId is 16 or 17 && !row.Name.IsEmpty).OrderBy(row=>row.ClassJob.RowId).ThenBy(row=>row.ClassJobLevel))
        Console.WriteLine($"ACTION {row.RowId} job {row.ClassJob.RowId} lv {row.ClassJobLevel} costType {row.PrimaryCostType} cost {row.PrimaryCostValue} second {row.SecondaryCostType}/{row.SecondaryCostValue.RowId} "
            +$"'{english.GetRowOrDefault(row.RowId)?.Name.ExtractText()}' / '{row.Name.ExtractText()}' :: {transient.GetRowOrDefault(row.RowId)?.Description.ExtractText().Replace((char)10,(char)32)}");
    var traitText=data.Excel.GetSheet<TraitTransient>(Language.English);
    foreach(var trait in data.Excel.GetSheet<Trait>(Language.English).Where(row=>row.ClassJob.RowId is 16 or 17 && !row.Name.IsEmpty).OrderBy(row=>row.Level))
        Console.WriteLine($"TRAIT {trait.RowId} job {trait.ClassJob.RowId} lv {trait.Level} '{trait.Name.ExtractText()}' :: {traitText.GetRowOrDefault(trait.RowId)?.Description.ExtractText().Replace((char)10,(char)32)}");
    var status=data.Excel.GetSheet<Status>(Language.English);
    foreach(var id in new uint[]{2765,757,2668,3910,225,2190,2665,3812,2226})Console.WriteLine($"STATUS {id} '{status.GetRowOrDefault(id)?.Name.ExtractText()}' :: {status.GetRowOrDefault(id)?.Description.ExtractText().Replace((char)10,(char)32)}");
    return;
}
// PROBE_COLLECTABLES (0.409, R-06): collectable shops, their items, collectability thresholds and scrip rewards (Currency column).
if(Environment.GetEnvironmentVariable("PROBE_COLLECTABLES") is { Length:>0 })
{
    foreach(var shop in data.Excel.GetSheet<CollectablesShop>().Where(row=>!row.Name.IsEmpty))
        Console.WriteLine($"SHOP {shop.RowId} '{shop.Name.ExtractText()}' rewardType {shop.RewardType} groups {string.Join(",",shop.ShopItems.Select(item=>item.RowId).Where(id=>id!=0))}");
    var rows=data.Excel.GetSubrowSheet<CollectablesShopItem>().SelectMany(group=>group).Where(row=>row.Item.RowId!=0).ToArray();
    Console.WriteLine($"ITEMS {rows.Length}");
    foreach(var currency in rows.GroupBy(row=>row.CollectablesShopRewardScrip.ValueNullable?.Currency ?? 9999).OrderBy(group=>group.Key))
        Console.WriteLine($"CURRENCY {currency.Key} rows {currency.Count()} e.g. "+string.Join(" | ",currency.Take(3).Select(row=>$"{row.RowId}.{row.SubrowId} {row.Item.ValueNullable?.Name.ExtractText()} grp '{row.CollectablesShopItemGroup.ValueNullable?.Name.ExtractText()}'")));
    foreach(var row in rows.Where(row=>row.RowId is >= 0).GroupBy(row=>row.RowId).SelectMany(group=>group.Take(2)).Take(60))
    {
        var refine=row.CollectablesShopRefine.ValueNullable; var reward=row.CollectablesShopRewardScrip.ValueNullable;
        Console.WriteLine($"ROW {row.RowId}.{row.SubrowId} item {row.Item.RowId} '{row.Item.ValueNullable?.Name.ExtractText()}' lv {row.LevelMin}-{row.LevelMax} stars {row.Stars} "
            +$"refine {refine?.LowCollectability}/{refine?.MidCollectability}/{refine?.HighCollectability} scrip c{reward?.Currency} {reward?.LowReward}/{reward?.MidReward}/{reward?.HighReward} "
            +$"grp '{row.CollectablesShopItemGroup.ValueNullable?.Name.ExtractText()}'");
    }
    var twice=rows.GroupBy(row=>row.Item.RowId).Where(group=>group.Count()>1).Take(5);
    foreach(var group in twice)Console.WriteLine($"DUP item {group.Key} rows {string.Join(",",group.Select(row=>$"{row.RowId}.{row.SubrowId}"))}");
    return;
}
// PROBE_LOGS (0.404): row numbers of the spearfishing and fishing notebooks.
if(Environment.GetEnvironmentVariable("PROBE_LOGS") is { Length:>0 })
{
    var spear=data.Excel.GetSheet<SpearfishingItem>().Where(row=>row.Item.RowId!=0).Select(row=>row.RowId).ToArray();
    Console.WriteLine($"SPEAR rows {spear.Length} min {spear.Min()} max {spear.Max()} first {string.Join(",",spear.Take(5))} last {string.Join(",",spear.TakeLast(5))}");
    var fishRows=data.Excel.GetSheet<FishParameter>().Where(row=>row.IsInLog && row.Item.RowId!=0).Select(row=>row.RowId).ToArray();
    Console.WriteLine($"FISH rows {fishRows.Length} min {fishRows.Min()} max {fishRows.Max()}");
    var gather=data.Excel.GetSheet<GatheringItem>().Where(row=>row.Item.RowId!=0).Select(row=>row.RowId).ToArray();
    Console.WriteLine($"GATHER rows {gather.Length} min {gather.Min()} max {gather.Max()}");
    return;
}
// PROBE_OCEAN (0.392, R-01): ocean fishing route loop (IKDRouteTable), routes (IKDRoute: name, stops, time of day).
if(Environment.GetEnvironmentVariable("PROBE_OCEAN") is { Length:>0 })
{
    var table=data.Excel.GetSheet<IKDRouteTable>().ToArray();
    Console.WriteLine($"ROUTETABLE rows {table.Length}");
    foreach(var row in table.Take(14))Console.WriteLine($"LOOP {row.RowId} aldenard {row.Route.RowId} othard {row.Unknown0}");
    foreach(var route in data.Excel.GetSheet<IKDRoute>().Where(row=>!row.Name.IsEmpty))
        Console.WriteLine($"ROUTE {route.RowId} '{route.Name.ExtractText()}' instance {route.Instance.RowId} stops "
            +string.Join(" | ",route.Spot.Zip(route.Time).Where(pair=>pair.First.RowId!=0).Select(pair=>$"{pair.First.ValueNullable?.PlaceName.ValueNullable?.Name.ExtractText()} t{pair.Second.ValueNullable?.TimeOfDay}")));
    Console.WriteLine("LOOPROUTES "+string.Join(",",table.Select(row=>row.Route.RowId))+" / "+string.Join(",",table.Select(row=>row.Unknown0)));
    return;
}
// PROBE_UNLOCKS (0.387): item actions of the 0.365 collection kinds — data columns against the unlocked row's name.
if(Environment.GetEnvironmentVariable("PROBE_UNLOCKS") is { Length:>0 })
{
    var orn=data.Excel.GetSheet<Ornament>();var gla=data.Excel.GetSheet<Glasses>();var bud=data.Excel.GetSheet<BuddyEquip>();var frame=data.Excel.GetSheet<BannerFrame>();
    var bg=data.Excel.GetSheet<BannerBg>();var deco=data.Excel.GetSheet<BannerDecoration>();var mcg=data.Excel.GetSheet<McGuffin>();
    string Label(uint type,uint key)=>type switch
    {
        20086=>orn.GetRowOrDefault(key)?.Singular.ExtractText() ?? "?",37312=>gla.GetRowOrDefault(key)?.Name.ExtractText() ?? "?",1013=>bud.GetRowOrDefault(key)?.Name.ExtractText() ?? "?",
        29459=>"frame "+(frame.GetRowOrDefault(key)?.Name.ExtractText() ?? "?")+" | bg "+(bg.GetRowOrDefault(key)?.Name.ExtractText() ?? "?")+" | deco "+(deco.GetRowOrDefault(key)?.Name.ExtractText() ?? "?"),
        _=>"mcguffin "+(mcg.GetRowOrDefault(key)?.UIData.ValueNullable?.Name.ExtractText() ?? "?"),
    };
    foreach(var type in new uint[]{20086,37312,1013,29459,25183,25174})
        foreach(var item in data.Excel.GetSheet<Item>().Where(row=>!row.Name.IsEmpty && row.ItemAction.ValueNullable?.Action.RowId==type).Take(6))
        {
            var action=item.ItemAction.Value;
            Console.WriteLine($"UNLOCK {type} item {item.RowId} '{item.Name.ExtractText()}' data "+string.Join(",",action.Data.ToArray().Take(5))
                +" -> d0 "+Label(type,action.Data[0])+" / d1 "+Label(type,action.Data[1]));
        }
    Console.WriteLine("MCGUFFIN "+string.Join(" ; ",mcg.Where(row=>row.UIData.RowId!=0).Take(5).Select(row=>$"{row.RowId} {row.UIData.Value.Name.ExtractText()}")));
    return;
}
// PROBE_HUNT (0.364, C-12): hunting log rows (MonsterNote) and their targets, to map rows to class, rank and entry.
if(Environment.GetEnvironmentVariable("PROBE_HUNT") is { Length:>0 })
{
    var rows=data.Excel.GetSheet<MonsterNote>().Where(row=>!row.Name.IsEmpty).ToArray();
    Console.WriteLine($"MONSTERNOTE rows {rows.Length} first {rows.First().RowId} last {rows.Last().RowId}");
    foreach(var row in rows.Where(row=>row.RowId<200 || row.RowId%1000<12).Take(80))
        Console.WriteLine($"NOTE {row.RowId} '{row.Name.ExtractText()}' reward {row.Reward} targets "+string.Join(" ; ",row.MonsterNoteTarget.Select((target,i)=>(target,i)).Where(t=>t.target.RowId!=0)
            .Select(t=>$"{t.target.Value.BNpcName.Value.Singular.ExtractText()} x{row.Count[t.i]} zone {string.Join("/",t.target.Value.PlaceNameZone.Where(z=>z.RowId!=0).Select(z=>z.Value.Name.ExtractText()))}")));
    Console.WriteLine("IDS "+string.Join(",",rows.Select(row=>row.RowId)));
    return;
}
// PROBE_ROULETTE (0.356, C-02): duty roulettes shown in the Duty Finder, their role-bonus row and completion index.
if(Environment.GetEnvironmentVariable("PROBE_ROULETTE") is { Length:>0 })
{
    foreach(var row in data.Excel.GetSheet<ContentRoulette>().Where(row=>!row.Name.IsEmpty))
        Console.WriteLine($"ROULETTE {row.RowId} '{row.Name.ExtractText()}' finder {row.IsInDutyFinder} bonus {row.ContentRouletteRoleBonus.RowId} completion {row.CompletionArrayIndex} level {row.RequiredLevel} sort {row.SortKey}");
    foreach(var row in data.Excel.GetSheet<ContentRouletteRoleBonus>())
        Console.WriteLine($"ROLEBONUS {row.RowId} item {row.ItemRewardType.RowId} amount {row.RewardAmount}");
    return;
}
// PROBE_CURRENCY (0.344, I-02): currency items (UI category « Currency »), tomestones and their weekly limits, Grand Company seal caps.
if(Environment.GetEnvironmentVariable("PROBE_CURRENCY") is { Length:>0 })
{
    var itemSheet=data.Excel.GetSheet<Item>();
    var categories=itemSheet.Where(row=>!row.Name.IsEmpty && row.ItemUICategory.RowId!=0).GroupBy(row=>row.ItemUICategory.RowId).ToDictionary(g=>g.Key,g=>g.Count());
    var uiCats=data.Excel.GetSheet<ItemUICategory>();
    foreach(var cat in uiCats.Where(row=>row.Name.ExtractText().Contains("onnaie",StringComparison.OrdinalIgnoreCase)))Console.WriteLine($"UICAT {cat.RowId} '{cat.Name.ExtractText()}' items {categories.GetValueOrDefault(cat.RowId)}");
    foreach(var row in itemSheet.Where(row=>!row.Name.IsEmpty && row.ItemUICategory.RowId==100).Take(60))
        Console.WriteLine($"CUR {row.RowId} '{row.Name.ExtractText()}' stack {row.StackSize} sort {row.ItemSortCategory.RowId} search {row.ItemSearchCategory.RowId}");
    foreach(var row in data.Excel.GetSheet<TomestonesItem>())
        Console.WriteLine($"TOME {row.RowId} item {row.Item.RowId} '{(row.Item.IsValid ? row.Item.Value.Name.ExtractText():"")}' tomestones {row.Tomestones.RowId} weekly {(row.Tomestones.IsValid ? row.Tomestones.Value.WeeklyLimit:0)} slot {row.CurrencyInventorySlot} stack {(row.Item.IsValid ? row.Item.Value.StackSize:0)}");
    foreach(var row in data.Excel.GetSheet<GrandCompanyRank>().Take(20))Console.WriteLine($"GCRANK {row.RowId} max {row.MaxSeals} required {row.RequiredSeals}");
    return;
}
// PROBE_FESTIVALNPC (0.342): event NPCs from the festival layers of each zone's planevent.lgb (Layer.FestivalID), with their shops.
if(Environment.GetEnvironmentVariable("PROBE_FESTIVALNPC") is { Length:>0 })
{
    var started=System.Diagnostics.Stopwatch.StartNew();
    var byFestival=new Dictionary<uint,HashSet<(uint Npc,uint Territory)>>();var files=0;var missing=0;
    foreach(var territory in data.Excel.GetSheet<TerritoryType>())
    {
        var bg=territory.Bg.ExtractText();var cut=bg.IndexOf("/level/",StringComparison.Ordinal);
        if(cut<0)continue;
        var path=$"bg/{bg[..(cut+7)]}planevent.lgb";
        var file=data.GetFile<Lumina.Data.Files.LgbFile>(path);
        if(file is null){missing++;continue;}
        files++;
        foreach(var layer in file.Layers)
        {
            if(layer.FestivalID==0)continue;
            foreach(var instance in layer.InstanceObjects)
                if(instance.AssetType==Lumina.Data.Parsing.Layer.LayerEntryType.EventNPC && instance.Object is Lumina.Data.Parsing.Layer.LayerCommon.ENPCInstanceObject npc)
                {
                    if(!byFestival.TryGetValue(layer.FestivalID,out var set))byFestival[layer.FestivalID]=set=[];
                    set.Add((npc.ParentData.ParentData.BaseId,territory.RowId));
                }
        }
    }
    Console.WriteLine($"LGB files {files} missing {missing} festivals {byFestival.Count} in {started.ElapsedMilliseconds} ms");
    var festResidents=data.Excel.GetSheet<ENpcResident>();var festBases=data.Excel.GetSheet<ENpcBase>();
    // 0.342: localized texts tied to each festival: CustomTalk (Name, MainOption, SubOption) of its NPCs, and its quests' genre.
    var talks=data.Excel.GetSheet<CustomTalk>();
    foreach(var festival in new uint[]{176,175,174,258,257,256,120,137})
    {
        if(!byFestival.TryGetValue(festival,out var talkSet)){Console.WriteLine($"TALK {festival} no layer");continue;}
        var seen=new HashSet<uint>();
        foreach(var (npc,_) in talkSet)
            if(festBases.TryGetRow(npc,out var npcRow))
                foreach(var value in npcRow.ENpcData.Select(d=>d.RowId).Where(id=>id>=720896 && id<786432))
                    if(seen.Add(value) && talks.TryGetRow(value,out var talk))
                        Console.WriteLine($"TALK {festival} npc '{(festResidents.TryGetRow(npc,out var who) ? who.Singular.ExtractText():"?")}' {value} name '{talk.Name.ExtractText()}' main '{talk.MainOption.ExtractText()}' sub '{talk.SubOption.ExtractText()}'");
        if(seen.Count==0)Console.WriteLine($"TALK {festival} npcs {talkSet.Count}, no CustomTalk");
    }
    foreach(var festival in new uint[]{176,175,174,258})
    {
        if(!byFestival.TryGetValue(festival,out var set)){Console.WriteLine($"FEST {festival} no layer");continue;}
        Console.WriteLine($"FEST {festival} npcs {set.Count}");
        foreach(var (npc,territory) in set.Take(40))
        {
            var npcShops=festBases.TryGetRow(npc,out var row) ? row.ENpcData.Select(d=>d.RowId).Where(id=>id>=262144 && id<1900000).ToArray():[];
            if(npcShops.Length==0)continue;
            Console.WriteLine($"  NPC {npc} '{(festResidents.TryGetRow(npc,out var r) ? r.Singular.ExtractText():"?")}' territory {territory} data {string.Join(",",npcShops.Take(6))}");
        }
    }
    return;
}
// PROBE_FESTIVAL (0.342): Festival rows with a name, and the quests linked to a festival (Quest.Festival), the last ones first.
if(Environment.GetEnvironmentVariable("PROBE_FESTIVAL") is { Length:>0 })
{
    var festivals=data.Excel.GetSheet<Festival>();
    Console.WriteLine($"FESTIVAL rows {festivals.Count} named {festivals.Count(row=>!row.Name.IsEmpty)} last {festivals.Max(row=>row.RowId)}");
    foreach(var row in festivals.Where(row=>!row.Name.IsEmpty).Take(8))Console.WriteLine($"FESTIVAL {row.RowId} '{row.Name.ExtractText()}'");
    var quests=data.Excel.GetSheet<Quest>().Where(row=>row.Festival.RowId!=0 && !row.Name.IsEmpty).ToArray();
    Console.WriteLine($"QUESTS with festival {quests.Length} festivals {quests.Select(row=>row.Festival.RowId).Distinct().Count()}");
    foreach(var group in quests.GroupBy(row=>row.Festival.RowId).OrderByDescending(group=>group.Key).Take(6))
        Console.WriteLine($"FEST {group.Key} name '{(festivals.TryGetRow(group.Key,out var f) ? f.Name.ExtractText():"?")}' quests {group.Count()}: {string.Join(" | ",group.Take(4).Select(q=>$"{q.RowId} {q.Name.ExtractText()} begin {q.FestivalBegin} end {q.FestivalEnd} gil {q.GilReward} items {string.Join(",",q.Reward.Where(r=>r.RowId!=0).Select(r=>r.RowId).Take(3))}"))}");
    return;
}
// PROBE_BARDING (0.340): chocobo barding rows (BuddyEquip) and the items whose ItemAction teaches them (action id, Data[0]).
if(Environment.GetEnvironmentVariable("PROBE_BARDING") is { Length:>0 })
{
    var bardings=data.Excel.GetSheet<BuddyEquip>().Where(row=>!row.Name.IsEmpty).ToArray();
    Console.WriteLine($"BARDING rows {bardings.Length}");
    foreach(var row in bardings.Take(12))Console.WriteLine($"BARDING {row.RowId} '{row.Name.ExtractText()}' icons {row.IconHead},{row.IconBody},{row.IconLegs} gc {row.GrandCompany.RowId} order {row.Order}");
    var byName=bardings.GroupBy(row=>row.Name.ExtractText()).ToDictionary(group=>group.Key,group=>group.First().RowId);
    var actions=new Dictionary<uint,int>();var matched=0;var dataMatches=0;
    foreach(var item in data.Excel.GetSheet<Item>())
    {
        if(item.ItemAction.RowId==0 || !item.ItemAction.IsValid)continue;
        var name=item.Name.ExtractText();
        if(!byName.TryGetValue(name,out var barding))continue;
        matched++;var action=item.ItemAction.Value.Action.RowId;actions[action]=actions.GetValueOrDefault(action)+1;
        if(item.ItemAction.Value.Data[0]==barding)dataMatches++;
        if(matched<=6)Console.WriteLine($"ITEM {item.RowId} '{name}' action {action} data {string.Join(",",item.ItemAction.Value.Data.ToArray().Take(3))} barding {barding}");
    }
    Console.WriteLine($"MATCHED {matched} data0==row {dataMatches} actions {string.Join(" ",actions.Select(pair=>$"{pair.Key}x{pair.Value}"))}");
    return;
}
// PROBE_MATERIARATE (0.338): overmeld success rates per row (materia grade?) and per advanced socket, NQ and HQ piece.
if(Environment.GetEnvironmentVariable("PROBE_MATERIARATE") is { Length:>0 })
{
    foreach(var row in data.Excel.GetSheet<MateriaJoinRate>())
        Console.WriteLine($"RATE {row.RowId} NQ {string.Join(" ",row.NQOvermeldPercentSlot)} HQ {string.Join(" ",row.HQOvermeldPercentSlot)}");
    foreach(var row in data.Excel.GetSheet<MateriaJoinRateGatherCraft>())
        Console.WriteLine($"RATEGC {row.RowId} NQ {string.Join(" ",row.NQOvermeldPercentSlot)} HQ {string.Join(" ",row.HQOvermeldPercentSlot)}");
    return;
}
// PROBE_AIRSHIP (0.336): airship exploration points as the sheet holds them (row, names, rank, surveillance, tanks, distance, duration).
if(Environment.GetEnvironmentVariable("PROBE_AIRSHIP") is { Length:>0 })
{
    foreach(var row in data.Excel.GetSheet<AirshipExplorationPoint>())
        Console.WriteLine($"POINT {row.RowId} '{row.Name.ExtractText()}' short '{row.NameShort.ExtractText()}' rank {row.RankReq} surv {row.SurveillanceReq} tanks {row.CeruleumTankReq} dist {row.SurveyDistance} min {row.SurveyDurationmin} exp {row.ExpReward} xy {row.X},{row.Y} passengers {row.Passengers}");
    return;
}
// PROBE_SUBROUTES (0.309): submarine sectors and the route planner on the real sheets and the embedded community loot.
if(Environment.GetEnvironmentVariable("PROBE_SUBROUTES") is { Length:>0 })
{
    var rows=data.Excel.GetSheet<SubmarineExploration>().Where(row=>row.Map.RowId!=0).ToArray();
    var sectors=rows.Select(row=>new TenshoAssistant.FreeCompany.SubmarineSector(row.RowId,row.Map.RowId,row.StartingPoint,row.RankReq,new System.Numerics.Vector3(row.X,row.Y,row.Z),row.SurveyDistance,row.SurveyDurationmin,row.ExpReward,row.Destination.ExtractText())).ToArray();
    using var file=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"../../../../../src/TenshoAssistant/FreeCompany/Data/submarine-loot.json"));
    var (loot,source,built)=TenshoAssistant.FreeCompany.VoyagePlanner.ReadLoot(file);
    var itemSheet=data.Excel.GetSheet<Item>();
    double Price(uint id)=>itemSheet.TryGetRow(id,out var item) ? item.PriceLow:0;
    foreach(var map in data.Excel.GetSheet<SubmarineMap>().Where(row=>row.RowId!=0 && !row.Name.IsEmpty))
    {
        var ofMap=sectors.Where(sector=>sector.Map==map.RowId).ToArray();
        Console.WriteLine($"MAP {map.RowId} '{map.Name.ExtractText()}' sectors {ofMap.Length} start {ofMap.Count(sector=>sector.Start)} with loot {ofMap.Count(sector=>loot.ContainsKey(sector.Id))}");
        var letters=rows.Where(row=>row.Map.RowId==map.RowId).ToDictionary(row=>row.RowId,row=>row.Location.ExtractText());
        foreach(var (rank,speed,range) in new[]{(125,130,140),(70,80,90)})
        {
            var started=System.Diagnostics.Stopwatch.StartNew();
            var routes=TenshoAssistant.FreeCompany.VoyagePlanner.Plan(ofMap,rank,speed,range,TenshoAssistant.FreeCompany.RouteGoal.ValuePerDay,id=>TenshoAssistant.FreeCompany.VoyagePlanner.ExpectedValue(loot.GetValueOrDefault(id),Price),3);
            foreach(var route in routes)
                Console.WriteLine($"  r{rank}/s{speed}/R{range} {string.Join("",route.Sectors.Select(id=>letters[id]))} dist {route.Distance} time {TimeSpan.FromSeconds(route.Seconds)} value {route.Value:N0} perDay {route.ValuePerDay:N0} ({started.ElapsedMilliseconds} ms)");
        }
    }
    Console.WriteLine($"LOOT {source} {built} sectors {loot.Count}");
    return;
}
// PROBE_FCC (0.295): Free Company credit shops — FccShop fields, which NPCs link to them (ENpcData), and the credit currency.
if(Environment.GetEnvironmentVariable("PROBE_FCC") is { Length:>0 })
{
    var fcc=data.Excel.GetSheet<FccShop>();
    Console.WriteLine("FCCSHOP PROPERTIES "+string.Join(",",typeof(FccShop).GetProperties().Select(property=>property.Name+":"+property.PropertyType.Name)));
    foreach(var shop in fcc.Take(3))
    {
        var names=string.Join("; ",shop.ItemData.Take(4).Select(row=>$"{row.Item.RowId}={(row.Item.IsValid ? row.Item.Value.Name.ExtractText():"")} cost {row.Cost} rank {row.FCRankRequired.RowId}"));
        Console.WriteLine($"FCC {shop.RowId} '{shop.Name.ExtractText()}' items {shop.ItemData.Count} :: {names}");
    }
    var ids=fcc.Select(row=>row.RowId).ToHashSet();
    foreach(var npc in data.Excel.GetSheet<ENpcBase>().Where(npc=>npc.ENpcData.Any(link=>ids.Contains(link.RowId))))
    {
        var resident=data.Excel.GetSheet<ENpcResident>().TryGetRow(npc.RowId,out var r) ? r.Singular.ExtractText():"";
        Console.WriteLine($"NPC {npc.RowId} '{resident}' -> {string.Join(",",npc.ENpcData.Where(link=>ids.Contains(link.RowId)).Select(link=>link.RowId))}");
    }
    return;
}
// PROBE_CHOICES (0.286): ARR main scenario quests that are alternatives (one of several): previous-quest joins and quest locks.
if(Environment.GetEnvironmentVariable("PROBE_CHOICES") is { Length:>0 })
{
    var msq=data.Excel.GetSheet<Quest>().Where(row=>row.JournalGenre.RowId==1 && row.Name.ExtractText().Length>0).ToDictionary(row=>row.RowId);
    string QuestName(uint id)=>msq.TryGetValue(id,out var q) ? q.Name.ExtractText():data.Excel.GetSheet<Quest>().TryGetRow(id,out var other) ? "~"+other.Name.ExtractText():id.ToString();
    var names=new[]{"En bonne compagnie (Deux Vipères)","En bonne compagnie (Immortels)","En bonne compagnie (Maelstrom)","L'appel de la mer","Pour la prospérité d'Ul'dah","Que la Sylve vous protège","Une visite s'impose"};
    foreach(var quest in msq.Values.Where(row=>names.Contains(row.Name.ExtractText()) || row.PreviousQuest.Count(link=>link.RowId!=0)>1 || row.QuestLock.Any(link=>link.RowId!=0)))
        Console.WriteLine($"Q {quest.RowId} '{quest.Name.ExtractText()}' prevJoin={quest.PreviousQuestJoin} prev=[{string.Join("; ",quest.PreviousQuest.Where(l=>l.RowId!=0).Select(l=>QuestName(l.RowId)))}] lockJoin={quest.QuestLockJoin} lock=[{string.Join("; ",quest.QuestLock.Where(l=>l.RowId!=0).Select(l=>QuestName(l.RowId)))}] gc={quest.GrandCompany.RowId}");
    return;
}
// PROBE_STARTCITY (0.285): ARR main scenario quests (genre 1) of each starting city: roots = genre-1 quests without a genre-1
// previous quest; each quest's roots = the roots its previous-quest chain leads back to. One root = that city's own line.
if(Environment.GetEnvironmentVariable("PROBE_STARTCITY") is { Length:>0 })
{
    var msq=data.Excel.GetSheet<Quest>().Where(row=>row.JournalGenre.RowId==1 && row.Name.ExtractText().Length>0).ToDictionary(row=>row.RowId);
    IEnumerable<uint> Previous(Quest quest)=>quest.PreviousQuest.Select(link=>link.RowId).Where(id=>id!=0 && msq.ContainsKey(id));
    var roots=msq.Values.Where(quest=>!Previous(quest).Any()).ToList();
    foreach(var root in roots)Console.WriteLine($"ROOT {root.RowId} '{root.Name.ExtractText()}' join={root.PreviousQuestJoin} prev={string.Join(",",root.PreviousQuest.Select(link=>link.RowId).Where(id=>id!=0))} place={root.PlaceName.RowId} '{(root.PlaceName.IsValid ? root.PlaceName.Value.Name.ExtractText():"")}'");
    var memo=new Dictionary<uint,HashSet<uint>>();
    HashSet<uint> RootsOf(uint id)
    {
        if(memo.TryGetValue(id,out var known))return known;
        memo[id]=[];
        var previous=Previous(msq[id]).ToList();
        var set=previous.Count==0 ? new HashSet<uint>{id}:previous.SelectMany(RootsOf).ToHashSet();
        return memo[id]=set;
    }
    foreach(var group in msq.Keys.GroupBy(id=>string.Join("+",RootsOf(id).OrderBy(root=>root))).OrderBy(group=>group.Key))
        Console.WriteLine($"ROOTSET [{group.Key}] quests {group.Count()} e.g. {string.Join(" | ",group.Take(3).Select(id=>msq[id].Name.ExtractText()))}");
    return;
}
// PROBE_QUESTODD (0.284): « Accident de la route »… listed in genre 1 (ARR main scenario) but not main scenario quests; compare
// their fields with a real main scenario quest to find what marks them.
if(Environment.GetEnvironmentVariable("PROBE_QUESTODD") is { Length:>0 })
{
    var oddNames=new[]{"Accident de la route","Bombo échappé","Chasse à l'ombre","La Voie des Héritiers","Les Héritiers de la septième aube","Arpenteur des finitudes"};
    foreach(var quest in data.Excel.GetSheet<Quest>().Where(row=>oddNames.Contains(row.Name.ExtractText().Trim())))
    {
        var fields=string.Join(" ",typeof(Quest).GetProperties().Where(property=>property.PropertyType.IsPrimitive || property.PropertyType.IsEnum)
            .Select(property=>$"{property.Name}={property.GetValue(quest)}"));
        var refs=$"genre={quest.JournalGenre.RowId} issuer={quest.IssuerStart.RowId} issuerLocation={quest.IssuerLocation.RowId} placeName={quest.PlaceName.RowId} expansion={quest.Expansion.RowId} eventIcon={quest.EventIconType.RowId} classJobCategory0={quest.ClassJobCategory0.RowId}";
        Console.WriteLine($"QUEST {quest.RowId} '{quest.Name.ExtractText()}' {refs} {fields}");
    }
    Console.WriteLine("QUEST PROPERTIES "+string.Join(",",typeof(Quest).GetProperties().Where(property=>!property.PropertyType.IsPrimitive && !property.PropertyType.IsEnum).Select(property=>property.Name)));
    foreach(var quest in data.Excel.GetSheet<Quest>().Where(row=>oddNames.Contains(row.Name.ExtractText().Trim())))
    {
        var town=typeof(Quest).GetProperty("StartTown")?.GetValue(quest);
        var townId=town?.GetType().GetProperty("RowId")?.GetValue(town);
        Console.WriteLine($"TOWN {quest.RowId} '{quest.Name.ExtractText()}' startTown={townId}");
    }
    var genre1=data.Excel.GetSheet<Quest>().Where(row=>row.JournalGenre.RowId==1).ToList();
    Console.WriteLine($"GENRE1 total={genre1.Count} withIssuerLocation={genre1.Count(row=>row.IssuerLocation.RowId!=0)} withIcon={genre1.Count(row=>row.Icon!=0)}");
    return;
}
// PROBE_QUESTGENRE (0.283): Quest → JournalGenre → JournalCategory → JournalSection, to group quests by kind (main scenario,
// side quests, raids…) with their genre as sub-category.
if(Environment.GetEnvironmentVariable("PROBE_QUESTGENRE") is { Length:>0 })
{
    foreach(var section in data.Excel.GetSheet<JournalSection>().Where(row=>row.Name.ExtractText().Length>0))
        Console.WriteLine($"SECTION {section.RowId} '{section.Name.ExtractText()}'");
    var categories=data.Excel.GetSheet<JournalCategory>().Where(row=>row.Name.ExtractText().Length>0).ToList();
    foreach(var category in categories.Take(40))
        Console.WriteLine($"CATEGORY {category.RowId} '{category.Name.ExtractText()}' section {category.JournalSection.RowId}");
    Console.WriteLine($"CATEGORIES {categories.Count}");
    foreach(var quest in data.Excel.GetSheet<Quest>().Where(row=>row.RowId is 65560 or 65677 or 66045 or 70000 or 69000).Where(row=>row.JournalGenre.IsValid))
    {
        var genre=quest.JournalGenre.Value;var category=genre.JournalCategory.Value;
        Console.WriteLine($"QUEST {quest.RowId} '{quest.Name.ExtractText()}' genre {genre.RowId} '{genre.Name.ExtractText()}' category {category.RowId} '{category.Name.ExtractText()}' section {category.JournalSection.RowId} '{category.JournalSection.Value.Name.ExtractText()}'");
    }
    var bySection=data.Excel.GetSheet<Quest>().Where(row=>row.JournalGenre.IsValid && row.JournalGenre.RowId!=0 && row.Name.ExtractText().Length>0)
        .GroupBy(row=>(Section:row.JournalGenre.Value.JournalCategory.Value.JournalSection.RowId,Genre:row.JournalGenre.Value.Name.ExtractText()));
    foreach(var group in bySection.Where(group=>group.Key.Section is 0 or 1).OrderBy(group=>group.Key.Section))
        Console.WriteLine($"SECTIONGENRE section {group.Key.Section} genre '{group.Key.Genre}' quests {group.Count()}");
    return;
}
// PROBE_TRIAD (0.278): which TripleTriadCardResident field tells the « Ex. » cards (numbered apart in the game's notebook).
if(Environment.GetEnvironmentVariable("PROBE_TRIAD") is { Length:>0 })
{
    var names=data.Excel.GetSheet<TripleTriadCard>();
    var cardRows=data.Excel.GetSheet<TripleTriadCardResident>().Where(row=>row.Order is 1 or 2 or 3).ToList();
    foreach(var row in cardRows)
    {
        var fields=string.Join(" ",typeof(TripleTriadCardResident).GetProperties().Where(property=>property.PropertyType.IsPrimitive)
            .Select(property=>$"{property.Name}={property.GetValue(row)}"));
        Console.WriteLine($"CARD {row.RowId} '{(names.TryGetRow(row.RowId,out var card) ? card.Name.ExtractText():"?")}' {fields}");
    }
    var allCards=data.Excel.GetSheet<TripleTriadCardResident>().Where(row=>row.Order>0).ToList();
    foreach(var group in allCards.GroupBy(row=>row.UIPriority))
        Console.WriteLine($"UIPRIORITY {group.Key}: {group.Count()} cards, Order {group.Min(row=>row.Order)}..{group.Max(row=>row.Order)}");
    return;
}
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
    // 0.126: FishingSpot X/Z coordinate system, checked against Garland (spot 2 The Vein 22.9/22.18, spot 35 Lower Decks 7.69/12.21).
    foreach(var spot in data.Excel.GetSheet<FishingSpot>().Where(spot=>spot.RowId is 2 or 35))
    {
        var map=spot.TerritoryType.Value.Map.Value;double scale=map.SizeFactor/100.0;
        double World(double world,int offset)=>41.0/scale*((world+offset)*scale+1024.0)/2048.0+1.0;
        double Pixel(double pixel)=>41.0/scale*pixel/2048.0+1.0;
        Console.WriteLine($"FISH {spot.RowId} x={spot.X} z={spot.Z} r={spot.Radius} map {map.RowId} size {map.SizeFactor} offset {map.OffsetX}/{map.OffsetY} "
            +$"asWorld=({World(spot.X,map.OffsetX):0.00},{World(spot.Z,map.OffsetY):0.00}) asPixel=({Pixel(spot.X):0.00},{Pixel(spot.Z):0.00})");
    }
    // 0.151: quest map icon kind (EventIconType) for quests seen in game (65560 → 71021, 65677 → 71151).
    foreach(var quest in data.Excel.GetSheet<Quest>().Where(row=>row.RowId is 65560 or 65677 or 65539))
        if(quest.EventIconType.IsValid){var e=quest.EventIconType.Value;Console.WriteLine($"ICONTYPE quest {quest.RowId} type {quest.EventIconType.RowId} mapAvail {e.MapIconAvailable} npcAvail {e.NpcIconAvailable} range {e.IconRange} genre {quest.JournalGenre.RowId} genreIcon {(quest.JournalGenre.IsValid ? quest.JournalGenre.Value.Icon:0)}");}
    // 0.146: which achievements reward these mount items (Achievement.Item).
    foreach(var achievement in data.Excel.GetSheet<Achievement>().Where(row=>row.Item.RowId is 41498 or 40382 or 39486))
        Console.WriteLine($"ACHIEVEMENT {achievement.RowId} '{achievement.Name.ExtractText()}' item {achievement.Item.RowId}");
    Console.WriteLine("ACHIEVEMENT scan done");
    // 0.141: ItemAction type → collection (mount / minion), checked by the Data[0] row name against the item name family.
    var mountSheet=data.Excel.GetSheet<Mount>();var companionSheet=data.Excel.GetSheet<Companion>();
    foreach(var group in data.Excel.GetSheet<Item>().Where(item=>item.ItemAction.RowId!=0 && item.ItemAction.IsValid).GroupBy(item=>item.ItemAction.Value.Action.RowId).OrderByDescending(group=>group.Count()).Take(25))
    {
        var sample=group.First();var key=sample.ItemAction.Value.Data[0];
        var mount=mountSheet.TryGetRow(key,out var m) ? m.Singular.ExtractText():"";var companion=companionSheet.TryGetRow(key,out var c) ? c.Singular.ExtractText():"";
        Console.WriteLine($"ACTION type {group.Key} items {group.Count()} sample '{sample.Name.ExtractText()}' data0 {key} mount '{mount}' companion '{companion}'");
    }
    // 0.139: map symbol icons and their names (shop icon for NPC vendors).
    foreach(var symbol in data.Excel.GetSheet<MapSymbol>().Where(row=>row.Icon>0 && row.PlaceName.RowId!=0))
        Console.WriteLine($"SYMBOL {symbol.Icon} '{symbol.PlaceName.Value.Name.ExtractText()}'");
    // 0.127: sightseeing (Adventure → Level) and aether currents (EObj.Data = AetherCurrent, placed by a Level whose Object is the EObj).
    var adventures=data.Excel.GetSheet<Adventure>().Where(row=>row.Name.ExtractText().Length>0).ToList();
    Console.WriteLine($"ADVENTURE named={adventures.Count} firstRow={adventures.First().RowId} lastRow={adventures.Last().RowId} withLevelMap={adventures.Count(row=>row.Level.IsValid && row.Level.Value.Map.RowId!=0)}");
    foreach(var row in adventures.Take(5))
    {
        var l=row.Level.Value;var m=l.Map.Value;double s=m.SizeFactor/100.0;
        double C(double w,int o)=>41.0/s*((w+o)*s+1024.0)/2048.0+1.0;
        Console.WriteLine($"  adv {row.RowId} '{row.Name.ExtractText()}' place '{row.PlaceName.Value.Name.ExtractText()}' level {l.RowId} map {l.Map.RowId} terr {l.Territory.RowId} x={l.X:0.0} y={l.Y:0.0} z={l.Z:0.0} coord=({C(l.X,m.OffsetX):0.0},{C(l.Z,m.OffsetY):0.0}) type {l.Type} object {l.Object.RowId} time {row.MinTime}-{row.MaxTime} emote {row.Emote.RowId} '{(row.Emote.IsValid ? row.Emote.Value.Name.ExtractText():"")}' level {row.MinLevel}-{row.MaxLevel}");
    }
    var currents=data.Excel.GetSheet<AetherCurrent>();
    var currentObjects=data.Excel.GetSheet<EObj>().Where(obj=>obj.Data.RowId!=0 && currents.HasRow(obj.Data.RowId) && obj.Data.RowId>=0x2B0000).ToDictionary(obj=>obj.RowId,obj=>obj.Data.RowId);
    var placed=levels.Where(level=>currentObjects.ContainsKey(level.Object.RowId) && level.Map.RowId!=0).ToList();
    Console.WriteLine($"CURRENT rows={currents.Count} first={currents.First().RowId} withQuest={currents.Count(row=>row.Quest.RowId!=0)} eobjs={currentObjects.Count} placedByLevel={placed.Count} distinctCurrents={placed.Select(level=>currentObjects[level.Object.RowId]).Distinct().Count()}");
    foreach(var level in placed.Take(3))Console.WriteLine($"  current {currentObjects[level.Object.RowId]} eobj {level.Object.RowId} map {level.Map.RowId} x={level.X:0.0} z={level.Z:0.0}");
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
// PROBE_MARKERS (MAP-017): for NPCs named like the value, their data links, Level positions and the game's map markers around
// them (icon, label, texture-pixel distance) — what the map should show for exchanges such as Rowena's.
// PROBE_ZONE=<place name>: territories whose map is named like the value, the event NPCs of their layouts with their data links
// (sheet of each ENpcData value), and the map's shop-like markers — why the Tensho map shows no vendor there.
// PROBE_TALK=<CustomTalk id>[,item name]: script instructions/arguments of a scripted dialogue, the sheets each argument belongs to, and the
// special shops selling an item named like the second value — how a dialogue-only NPC reaches its shop.
// PROBE_ACH=<item name>: achievements whose reward is that item, and achievements whose English name matches the wiki (en sheet).
// PROBE_GROW: ParamGrow per level (BaseSpeed, LevelModifier, MpModifier) — the level modifiers of the stat formulas — and ClassJob categories.
if(Environment.GetEnvironmentVariable("PROBE_GROW") is { Length:>0 })
{
    foreach(var level in new uint[]{50,70,80,90,100})
    {
        var row=data.Excel.GetSheet<ParamGrow>().GetRow(level);
        Console.WriteLine($"GROW {level} BaseSpeed {row.BaseSpeed} LevelModifier {row.LevelModifier} MpModifier {row.MpModifier} HpModifier {row.HpModifier}");
    }
    foreach(var job in data.Excel.GetSheet<ClassJob>().Where(job=>job.RowId is 19 or 24 or 25 or 8 or 16 or 23))
        Console.WriteLine($"JOB {job.RowId} '{job.Name.ExtractText()}' category {job.ClassJobCategory.RowId} role {job.Role} primary {job.PrimaryStat} limitBreak {job.LimitBreak1.RowId}");
    return;
}
// PROBE_FISHSPOT (0.226): FishingSpot rows (territory, X, Z, radius) to compare with known positions (world or map coordinates?).
if(Environment.GetEnvironmentVariable("PROBE_FISHSPOT") is { Length:>0 } spotQuery)
{
    foreach(var id in spotQuery.Split(',').Select(uint.Parse))
    {
        var spot=data.Excel.GetSheet<FishingSpot>().GetRow(id);
        var map=spot.TerritoryType.Value.Map.Value;
        Console.WriteLine($"FISHSPOT {id} '{spot.PlaceName.Value.Name.ExtractText()}' territory {spot.TerritoryType.RowId} X {spot.X} Z {spot.Z} radius {spot.Radius} map size {map.SizeFactor} offset {map.OffsetX},{map.OffsetY}");
    }
    return;
}
// PROBE_REDUCE (0.888, user asked where to get an item for the aetherial reduction test): items the game marks reducible (Item.AetherialReduce > 0) that are
// gathered (GatheringItem in a GatheringPointBase), with the gathering type, the point level, the zone and whether the point is ephemeral (GatheringPointTransient).
if(Environment.GetEnvironmentVariable("PROBE_REDUCE") is { Length:>0 })
{
    var itemsSheet=data.Excel.GetSheet<Item>();var transient=data.Excel.GetSheet<GatheringPointTransient>();
    var reducible=itemsSheet.Where(item=>item.AetherialReduce>0).Select(item=>item.RowId).ToHashSet();
    var gatheringToItem=data.Excel.GetSheet<GatheringItem>().Where(row=>row.Item.RowType==typeof(Item) && reducible.Contains(row.Item.RowId)).ToDictionary(row=>row.RowId,row=>row.Item.RowId);
    var seen=new HashSet<string>();
    foreach(var point in data.Excel.GetSheet<GatheringPoint>())
    {
        if(point.GatheringPointBase.RowId==0 || point.TerritoryType.RowId==0)continue;
        var basis=point.GatheringPointBase.Value;
        foreach(var entry in basis.Item)
        {
            if(!gatheringToItem.TryGetValue(entry.RowId,out var itemId))continue;
            var timing=transient.TryGetRow(point.RowId,out var found) ? found:default;
            var ephemeral=timing.RowId!=0 && timing.EphemeralStartTime!=65535 && timing.EphemeralStartTime!=timing.EphemeralEndTime;
            var zone=point.TerritoryType.Value.PlaceName.Value.Name.ExtractText();
            var rare=timing.RowId!=0 && timing.GatheringRarePopTimeTable.RowId!=0 ? timing.GatheringRarePopTimeTable.Value:default;
            var rareText=rare.RowId==0 ? "":" rare "+string.Join(",",Enumerable.Range(0,rare.StartTime.Count).Where(index=>rare.Duration[index]!=0).Select(index=>$"{rare.StartTime[index]}+{rare.Duration[index]}min"));
            var line=$"REDUCE {itemsSheet.GetRow(itemId).Name.ExtractText()} | {basis.GatheringType.Value.Name.ExtractText()} level {basis.GatheringLevel} | {zone} | point {point.RowId} type {point.Type} | ephemeral {ephemeral} {timing.EphemeralStartTime}-{timing.EphemeralEndTime}{rareText}";
            if(seen.Add(line))Console.WriteLine(line);
        }
    }
    Console.WriteLine($"REDUCE reducible items {reducible.Count}, gathered {gatheringToItem.Values.Distinct().Count()}");
    return;
}
// PROBE_GATHERLEVEL (0.224): for gathering items named like the value, GatheringItem → GatheringItemLevelConvertTable (level, stars)
// → ItemLevel row of that level with its Gathering and Perception columns (base values of the Bountiful bonus rule).
if(Environment.GetEnvironmentVariable("PROBE_GATHERLEVEL") is { Length:>0 } gatherName)
{
    var levels=data.Excel.GetSheet<ItemLevel>();
    foreach(var gathering in data.Excel.GetSheet<GatheringItem>())
    {
        if(gathering.Item.RowId==0 || gathering.Item.RowType!=typeof(Item))continue;
        var item=data.Excel.GetSheet<Item>().GetRow(gathering.Item.RowId);
        if(!item.Name.ExtractText().Contains(gatherName,StringComparison.OrdinalIgnoreCase))continue;
        var convert=gathering.GatheringItemLevel.Value;
        var level=convert.GatheringItemLevel;
        var row=levels.TryGetRow(level,out var found) ? found:default;
        var byRow=levels.TryGetRow(gathering.GatheringItemLevel.RowId,out var rowFound) ? rowFound:default;
        Console.WriteLine($"GATHERLEVEL item {item.RowId} '{item.Name.ExtractText()}' gatheringItem {gathering.RowId} convert {gathering.GatheringItemLevel.RowId} level {level} stars {convert.Stars} ItemLevel[level].Gathering {row.Gathering} ItemLevel[convert].Gathering {byRow.Gathering} Perception {byRow.Perception}");
    }
    return;
}
// PROBE_EVENTICONS (0.211): EventIconType rows (NPC and map icons of quests, available / locked, icon range) — the quest marker icons.
if(Environment.GetEnvironmentVariable("PROBE_EVENTICONS") is { Length:>0 })
{
    foreach(var row in data.Excel.GetSheet<EventIconType>())
        Console.WriteLine($"EVENTICON {row.RowId} npcAvailable {row.NpcIconAvailable} mapAvailable {row.MapIconAvailable} npcInvalid {row.NpcIconInvalid} mapInvalid {row.MapIconInvalid} range {row.IconRange}");
    return;
}
// PROBE_ALARMCMD (0.205): the TextCommand row of the alarm command in each client language (command, short form, alias, help text).
if(Environment.GetEnvironmentVariable("PROBE_ALARMCMD") is { Length:>0 })
{
    var english=data.Excel.GetSheet<TextCommand>(Language.English)!;
    foreach(var row in english.Where(row=>row.Command.ExtractText()=="/alarm"))
        foreach(var language in new[]{Language.English,Language.French,Language.German,Language.Japanese})
        {
            var local=data.Excel.GetSheet<TextCommand>(language)!.GetRow(row.RowId);
            Console.WriteLine($"ALARMCMD {row.RowId} {language} command '{local.Command.ExtractText()}' short '{local.ShortCommand.ExtractText()}' alias '{local.Alias.ExtractText()}' shortAlias '{local.ShortAlias.ExtractText()}'");
            Console.WriteLine($"ALARMHELP {language} {local.Description.ExtractText().Replace((char)10,(char)124).Replace((char)13,(char)32)}");
        }
    return;
}
// PROBE_GTYPE (0.203): GatheringType rows (name, icons) and ClassJob 16–18 names — which gathering job each type belongs to.
if(Environment.GetEnvironmentVariable("PROBE_GTYPE") is { Length:>0 })
{
    foreach(var type in data.Excel.GetSheet<GatheringType>())Console.WriteLine($"GTYPE {type.RowId} '{type.Name.ExtractText()}' iconMain {type.IconMain} iconOff {type.IconOff}");
    foreach(var job in new uint[]{16,17,18})Console.WriteLine($"GJOB {job} '{data.Excel.GetSheet<ClassJob>().GetRow(job).Name.ExtractText()}' abbr '{data.Excel.GetSheet<ClassJob>().GetRow(job).Abbreviation.ExtractText()}'");
    return;
}
// PROBE_SELLS=<item id> (0.199): SpecialShop rows that give the item, and the ENpcBase rows whose data values name each shop directly.
if(Environment.GetEnvironmentVariable("PROBE_SELLS") is { Length:>0 } sellsQuery)
{
    var sold=uint.Parse(sellsQuery);
    var sellingShops=data.Excel.GetSheet<SpecialShop>().Where(shop=>shop.Item.Any(entry=>entry.ReceiveItems.Any(receive=>receive.Item.RowId==sold))).Select(shop=>shop.RowId).ToHashSet();
    Console.WriteLine($"SELLS {sold} shops={string.Join(",",sellingShops)}");
    foreach(var npc in data.Excel.GetSheet<ENpcBase>())
        foreach(var value in npc.ENpcData.Select(entry=>entry.RowId).Where(sellingShops.Contains))
            Console.WriteLine($"SELLS npc {npc.RowId} '{data.Excel.GetSheet<ENpcResident>().GetRow(npc.RowId).Singular.ExtractText()}' shop {value}");
    // Indirect links: PreHandler, TopicSelect and CustomTalk script arguments naming the shop, then the NPCs naming those rows.
    var holders=new HashSet<uint>();
    foreach(var row in data.Excel.GetSheet<PreHandler>().Where(row=>sellingShops.Contains(row.Target.RowId))){holders.Add(row.RowId);Console.WriteLine($"SELLS prehandler {row.RowId}");}
    foreach(var row in data.Excel.GetSheet<TopicSelect>().Where(row=>row.Shop.Any(shop=>sellingShops.Contains(shop.RowId)||holders.Contains(shop.RowId)))){holders.Add(row.RowId);Console.WriteLine($"SELLS topicselect {row.RowId}");}
    foreach(var row in data.Excel.GetSheet<CustomTalk>().Where(row=>row.Script.Any(script=>sellingShops.Contains(script.ScriptArg)||holders.Contains(script.ScriptArg)))){holders.Add(row.RowId);Console.WriteLine($"SELLS customtalk {row.RowId} '{row.Name.ExtractText()}'");}
    foreach(var npc in data.Excel.GetSheet<ENpcBase>())
        foreach(var value in npc.ENpcData.Select(entry=>entry.RowId).Where(holders.Contains))
            Console.WriteLine($"SELLS npc {npc.RowId} '{data.Excel.GetSheet<ENpcResident>().GetRow(npc.RowId).Singular.ExtractText()}' via {value}");
    return;
}
if(Environment.GetEnvironmentVariable("PROBE_ACH") is { Length:>0 } achQuery)
{
    var achParts=achQuery.Split(',',2);
    var achItems=data.Excel.GetSheet<Item>().Where(item=>item.Name.ExtractText()==achParts[0]).Select(item=>item.RowId).ToHashSet();
    Console.WriteLine($"ACHITEM '{achParts[0]}' ids={string.Join(",",achItems)}");
    foreach(var row in data.Excel.GetSheet<Achievement>().Where(row=>achItems.Contains(row.Item.RowId)))
        Console.WriteLine($"ACH reward {row.RowId} '{row.Name.ExtractText()}' item {row.Item.RowId} title {row.Title.RowId}");
    if(achParts.Length>1)
    {
        var english=data.Excel.GetSheet<Achievement>(Language.English);
        foreach(var row in english!.Where(row=>row.Name.ExtractText().Equals(achParts[1],StringComparison.OrdinalIgnoreCase)))
            Console.WriteLine($"ACH english {row.RowId} '{row.Name.ExtractText()}' item {row.Item.RowId} -> fr '{data.Excel.GetSheet<Achievement>().GetRow(row.RowId).Name.ExtractText()}'");
    }
    return;
}
if(Environment.GetEnvironmentVariable("PROBE_TALK") is { Length:>0 } talkQuery)
{
    var parts=talkQuery.Split(',',2);var talkId=uint.Parse(parts[0]);
    var talk=data.Excel.GetSheet<CustomTalk>().GetRow(talkId);
    Console.WriteLine($"TALK {talkId} name '{talk.Name.ExtractText()}' special {talk.SpecialLinks.RowId}");
    var argumentSheets=new (string Name,Func<uint,bool> Has)[]
    {
        ("SpecialShop",id=>data.Excel.GetSheet<SpecialShop>().HasRow(id)),("GilShop",id=>data.Excel.GetSheet<GilShop>().HasRow(id)),
        ("CustomTalk",id=>data.Excel.GetSheet<CustomTalk>().HasRow(id)),("PreHandler",id=>data.Excel.GetSheet<PreHandler>().HasRow(id)),
        ("InclusionShop",id=>data.Excel.GetSheet<InclusionShop>().HasRow(id)),("TopicSelect",id=>data.Excel.GetSheet<TopicSelect>().HasRow(id)),
        ("Item",id=>data.Excel.GetSheet<Item>().HasRow(id)),
    };
    foreach(var script in talk.Script.Where(script=>script.ScriptArg!=0 || script.ScriptInstruction.ExtractText().Length>0))
        Console.WriteLine($"  {script.ScriptInstruction.ExtractText()} = {script.ScriptArg} [{string.Join("/",argumentSheets.Where(sheet=>sheet.Has(script.ScriptArg)).Select(sheet=>sheet.Name))}]");
    if(parts.Length>1)
        foreach(var shop in data.Excel.GetSheet<SpecialShop>())
            if(shop.Item.Any(entry=>entry.ReceiveItems.Any(receive=>receive.Item.RowId!=0 && receive.Item.IsValid && receive.Item.Value.Name.ExtractText().Contains(parts[1],StringComparison.OrdinalIgnoreCase))))
                Console.WriteLine($"  SHOP {shop.RowId} '{shop.Name.ExtractText()}' sells '{parts[1]}' (offset from talk {(long)shop.RowId-talkId})");
    return;
}
if(Environment.GetEnvironmentVariable("PROBE_ZONE") is { Length:>0 } zoneName)
{
    foreach(var zoneMap in data.Excel.GetSheet<Map>().Where(row=>row.PlaceName.ValueNullable?.Name.ExtractText()==zoneName))
        Console.WriteLine($"MAPROW {zoneMap.RowId} id '{zoneMap.Id.ExtractText()}' territory {zoneMap.TerritoryType.RowId} markers {zoneMap.MapMarkerRange} size {zoneMap.SizeFactor} offset {zoneMap.OffsetX},{zoneMap.OffsetY}");
    foreach(var zoneTerritory in data.Excel.GetSheet<TerritoryType>().Where(row=>row.PlaceName.ValueNullable?.Name.ExtractText()==zoneName))
        Console.WriteLine($"TERRITORY {zoneTerritory.RowId} map {zoneTerritory.Map.RowId} bg '{zoneTerritory.Bg.ExtractText()}'");
    var zoneSheets=new (string Name,Func<uint,bool> Has)[]
    {
        ("GilShop",id=>data.Excel.GetSheet<GilShop>().HasRow(id)),("SpecialShop",id=>data.Excel.GetSheet<SpecialShop>().HasRow(id)),
        ("CollectablesShop",id=>data.Excel.GetSheet<CollectablesShop>().HasRow(id)),("InclusionShop",id=>data.Excel.GetSheet<InclusionShop>().HasRow(id)),
        ("PreHandler",id=>data.Excel.GetSheet<PreHandler>().HasRow(id)),("TopicSelect",id=>data.Excel.GetSheet<TopicSelect>().HasRow(id)),
        ("CustomTalk",id=>data.Excel.GetSheet<CustomTalk>().HasRow(id)),("FateShop",id=>data.Excel.GetSheet<FateShop>().HasRow(id)),
    };
    var zoneResidents=data.Excel.GetSheet<ENpcResident>();var zoneBases=data.Excel.GetSheet<ENpcBase>();
    foreach(var territory in data.Excel.GetSheet<TerritoryType>().Where(row=>row.Map.IsValid && row.Map.Value.PlaceName.ValueNullable?.Name.ExtractText()==zoneName))
    {
        var map=territory.Map.Value;
        Console.WriteLine($"ZONE territory {territory.RowId} map {map.RowId} region '{map.PlaceNameRegion.ValueNullable?.Name.ExtractText()}' regionId {map.PlaceNameRegion.RowId} bg '{territory.Bg.ExtractText()}'");
        var bg=territory.Bg.ExtractText();var cut=bg.IndexOf("/level/",StringComparison.Ordinal);if(cut<0)continue;
        foreach(var kind in new[]{"planevent","planner"})
        {
            var path=$"bg/{bg[..(cut+1)]}level/{kind}.lgb";var zoneBytes=data.FileExists(path) ? data.GetFile(path)?.Data:null;
            if(zoneBytes is null || zoneBytes.Length<16 || zoneBytes[0]!=0x4C || zoneBytes[1]!=0x47 || zoneBytes[2]!=0x42 || zoneBytes[3]!=0x31){Console.WriteLine($"  {kind}: absent or unreadable");continue;}
            Console.WriteLine($"  {kind}: bytes {zoneBytes.Length} declared {BitConverter.ToInt32(zoneBytes,4)} chunk '{System.Text.Encoding.ASCII.GetString(zoneBytes,12,4)}'");
            var file=data.GetFile<Lumina.Data.Files.LgbFile>(path);if(file is null)continue;
            var seen=new HashSet<uint>();
            foreach(var layer in file.Layers)foreach(var instance in layer.InstanceObjects)
            {
                if(instance.AssetType!=Lumina.Data.Parsing.Layer.LayerEntryType.EventNPC)continue;
                var id=((Lumina.Data.Parsing.Layer.LayerCommon.ENPCInstanceObject)instance.Object).ParentData.ParentData.BaseId;
                if(!seen.Add(id) || !zoneBases.TryGetRow(id,out var npc))continue;
                var links=npc.ENpcData.Select(value=>value.RowId).Where(value=>value!=0).Select(value=>$"{value}:{string.Join("/",zoneSheets.Where(sheet=>sheet.Has(value)).Select(sheet=>sheet.Name))}");
                Console.WriteLine($"  [{kind}] NPC {id} '{(zoneResidents.TryGetRow(id,out var who) ? who.Singular.ExtractText():"")}' title '{(zoneResidents.TryGetRow(id,out var w2) ? w2.Title.ExtractText():"")}' links={string.Join(",",links)}");
            }
        }
    }
    return;
}
if(Environment.GetEnvironmentVariable("PROBE_MARKERS") is { Length:>0 } markerNpc)
{
    var npcBases=data.Excel.GetSheet<ENpcBase>();var levels=data.Excel.GetSheet<Level>();var markerRows=data.Excel.GetSubrowSheet<MapMarker>();
    var linkSheets=new (string Name,Func<uint,bool> Has)[]
    {
        ("GilShop",id=>data.Excel.GetSheet<GilShop>().HasRow(id)),("SpecialShop",id=>data.Excel.GetSheet<SpecialShop>().HasRow(id)),
        ("CollectablesShop",id=>data.Excel.GetSheet<CollectablesShop>().HasRow(id)),("InclusionShop",id=>data.Excel.GetSheet<InclusionShop>().HasRow(id)),
        ("PreHandler",id=>data.Excel.GetSheet<PreHandler>().HasRow(id)),("TopicSelect",id=>data.Excel.GetSheet<TopicSelect>().HasRow(id)),
        ("CustomTalk",id=>data.Excel.GetSheet<CustomTalk>().HasRow(id)),("GuildleveAssignment",id=>data.Excel.GetSheet<GuildleveAssignment>().HasRow(id)),
        ("SatisfactionNpc",id=>data.Excel.GetSheet<SatisfactionNpc>().HasRow(id)),
    };
    // Census: NPCs placed by the Level sheet with a direct shop-like link, and the icon of the game marker within 8 pixels.
    if(markerNpc=="*")
    {
        var shopLike=linkSheets.Where(sheet=>sheet.Name is "GilShop" or "SpecialShop" or "InclusionShop" or "PreHandler" or "TopicSelect" or "CollectablesShop").ToArray();
        var census=new Dictionary<uint,List<string>>();var lonely=0;
        foreach(var level in levels.Where(level=>level.Type==8 && level.Map.RowId!=0 && level.Map.IsValid))
        {
            if(!npcBases.TryGetRow(level.Object.RowId,out var npc) || !npc.ENpcData.Any(value=>value.RowId!=0 && shopLike.Any(sheet=>sheet.Has(value.RowId))))continue;
            var map=level.Map.Value;var scale=map.SizeFactor/100f;
            var pixel=new System.Numerics.Vector2((level.X+map.OffsetX)*scale+1024f,(level.Z+map.OffsetY)*scale+1024f);
            var near=markerRows.TryGetRow(map.MapMarkerRange,out var rows)
                ? rows.Where(row=>row.Icon!=0).Select(row=>(row.Icon,distance:System.Numerics.Vector2.Distance(new(row.X,row.Y),pixel))).Where(pair=>pair.distance<=8).OrderBy(pair=>pair.distance).Select(pair=>pair.Icon).FirstOrDefault():0u;
            if(near==0){lonely++;continue;}
            if(!census.TryGetValue(near,out var names))census[near]=names=[];
            names.Add(data.Excel.GetSheet<ENpcResident>().TryGetRow(level.Object.RowId,out var who) ? who.Singular.ExtractText():level.Object.RowId.ToString());
        }
        foreach(var (icon,names) in census.OrderByDescending(pair=>pair.Value.Count))
            Console.WriteLine($"CENSUS icon {icon} count {names.Count} sample {string.Join(" | ",names.Distinct().Take(5))}");
        Console.WriteLine($"CENSUS withoutMarker {lonely}");
        return;
    }
    foreach(var resident in data.Excel.GetSheet<ENpcResident>().Where(row=>row.Singular.ExtractText().Contains(markerNpc,StringComparison.OrdinalIgnoreCase)
        || row.Title.ExtractText().Contains(markerNpc,StringComparison.OrdinalIgnoreCase)))
    {
        if(!npcBases.TryGetRow(resident.RowId,out var npc))continue;
        var links=npc.ENpcData.Select(value=>value.RowId).Where(id=>id!=0).Select(id=>$"{id}:{string.Join("/",linkSheets.Where(sheet=>sheet.Has(id)).Select(sheet=>sheet.Name))}");
        Console.WriteLine($"MNPC {resident.RowId} '{resident.Singular.ExtractText()}' title='{resident.Title.ExtractText()}' links={string.Join(",",links)}");
        foreach(var id in npc.ENpcData.Select(value=>value.RowId).Where(id=>data.Excel.GetSheet<PreHandler>().HasRow(id)))
        {
            var target=data.Excel.GetSheet<PreHandler>().GetRow(id).Target.RowId;
            Console.WriteLine($"  PREHANDLER {id} target {target}:{string.Join("/",linkSheets.Where(sheet=>sheet.Has(target)).Select(sheet=>sheet.Name))}"
                +(data.Excel.GetSheet<CollectablesShop>().TryGetRow(target,out var collectable) ? $" collectables '{collectable.Name.ExtractText()}'":""));
        }
        // Positions: Level rows; NPCs placed only by layouts take PROBE_MARKERS_AT « map:x,y;… » (map coordinates printed by PROBE_LGB).
        var spots=levels.Where(level=>level.Type==8 && level.Object.RowId==resident.RowId && level.Map.RowId!=0)
            .Select(level=>(Map:level.Map.Value,Pixel:new System.Numerics.Vector2((level.X+level.Map.Value.OffsetX)*level.Map.Value.SizeFactor/100f+1024f,(level.Z+level.Map.Value.OffsetY)*level.Map.Value.SizeFactor/100f+1024f))).ToList();
        if(spots.Count==0)
            foreach(var given in (Environment.GetEnvironmentVariable("PROBE_MARKERS_AT") ?? "").Split(';',StringSplitOptions.RemoveEmptyEntries))
            {
                var parts=given.Split(':',',');var map=data.Excel.GetSheet<Map>().GetRow(uint.Parse(parts[0]));var scale=map.SizeFactor/100f;
                float Pixel(string coordinate)=>(float.Parse(coordinate,System.Globalization.CultureInfo.InvariantCulture)-1f)*2048f*scale/41f;
                spots.Add((map,new System.Numerics.Vector2(Pixel(parts[1]),Pixel(parts[2]))));
            }
        foreach(var (map,pixel) in spots.Take(8))
        {
            Console.WriteLine($"  SPOT map {map.RowId} '{map.PlaceName.Value.Name.ExtractText()}' pixel {pixel.X:0},{pixel.Y:0}");
            if(!markerRows.TryGetRow(map.MapMarkerRange,out var rows))continue;
            foreach(var row in rows.Select(row=>(row,distance:System.Numerics.Vector2.Distance(new(row.X,row.Y),pixel))).Where(pair=>pair.distance<=48).OrderBy(pair=>pair.distance))
                Console.WriteLine($"    MARKER icon {row.row.Icon} type {row.row.DataType} key {row.row.DataKey.RowId} label '{row.row.PlaceNameSubtext.Value.Name.ExtractText()}' at {row.row.X},{row.row.Y} distance {row.distance:0.0}");
        }
    }
    return;
}
// PROBE_TRIAD (TRIAD-003): Triple Triad NPCs — ENpcBase data → TripleTriad row (rules, fixed and variable cards, rewards), rule list,
// and where they stand (Level). The plugin relies on these links only after this probe.
if(Environment.GetEnvironmentVariable("PROBE_TRIAD") is { Length:>0 })
{
    // PROBE_TRIAD=resident: TripleTriadResident ids/order next to TripleTriad rows; card items (ItemAction) and card sources.
    if(Environment.GetEnvironmentVariable("PROBE_TRIAD")=="resident")
    {
        var residentsTt=data.Excel.GetSheet<TripleTriadResident>();
        Console.WriteLine($"RESIDENT rows={residentsTt.Count} sample="+string.Join(",",residentsTt.Take(6).Select(row=>$"{row.RowId}:order{row.Order}")));
        Console.WriteLine($"RESIDENT sameIdsAsTripleTriad={residentsTt.Count(row=>data.Excel.GetSheet<TripleTriad>().HasRow(row.RowId))}");
        var cardItems=data.Excel.GetSheet<Item>().Where(item=>item.ItemAction.RowId!=0 && item.ItemAction.IsValid).ToList();
        var cardActions=cardItems.Where(item=>item.Name.ExtractText().Contains("Carte",StringComparison.OrdinalIgnoreCase) && item.ItemUICategory.RowId==86)
            .GroupBy(item=>item.ItemAction.Value.Action.RowId).Select(group=>$"{group.Key}x{group.Count()}");
        Console.WriteLine("CARDITEM actions="+string.Join(",",cardActions));
        foreach(var item in cardItems.Where(item=>item.ItemUICategory.RowId==86).Take(5))
            Console.WriteLine($"CARDITEM {item.RowId} '{item.Name.ExtractText()}' action {item.ItemAction.Value.Action.RowId} data0 {item.ItemAction.Value.Data[0]}");
        var cardResident=data.Excel.GetSheet<TripleTriadCardResident>();
        foreach(var id in new uint[]{53,42,89,5,7})
        {
            var r=cardResident.GetRow(id);
            Console.WriteLine($"CARDSRC {id} acqType {r.AcquisitionType.RowId} acquisition {r.Acquisition.RowId} location {r.Location.RowId} quest {r.Quest.RowId}");
        }
        var types=data.Excel.GetSheet<TripleTriadCardResident>().GroupBy(row=>row.AcquisitionType.RowId).Select(group=>$"{group.Key}x{group.Count()}");
        Console.WriteLine("ACQTYPES "+string.Join(",",types));
        return;
    }
    // PROBE_TRIAD=<npc id>: that NPC's deck with card faces (TripleTriadCardResident), to match cards observed in a match.
    if(uint.TryParse(Environment.GetEnvironmentVariable("PROBE_TRIAD"),out var triadNpc) && triadNpc>1)
    {
        var npc=data.Excel.GetSheet<ENpcBase>().GetRow(triadNpc);var resident=data.Excel.GetSheet<TripleTriadCardResident>();
        foreach(var id in npc.ENpcData.Select(value=>value.RowId).Where(id=>data.Excel.GetSheet<TripleTriad>().HasRow(id)))
        {
            var row=data.Excel.GetSheet<TripleTriad>().GetRow(id);
            Console.WriteLine($"DECK npc {triadNpc} match {id} rules=[{string.Join(",",row.TripleTriadRule.Select(rule=>rule.RowId))}] regional={row.UsesRegionalRules}");
            foreach(var (kind,cards) in new[]{("fixed",row.TripleTriadCardFixed),("variable",row.TripleTriadCardVariable)})
                foreach(var card in cards.Where(card=>card.RowId!=0))
                {
                    var face=resident.GetRow(card.RowId);
                    Console.WriteLine($"  {kind} {card.RowId} '{data.Excel.GetSheet<TripleTriadCard>().GetRow(card.RowId).Name.ExtractText()}' U{face.Top} R{face.Right} D{face.Bottom} L{face.Left} rarity {face.TripleTriadCardRarity.RowId} type {face.TripleTriadCardType.RowId}");
                }
        }
        return;
    }
    var matches=data.Excel.GetSheet<TripleTriad>();var rules=data.Excel.GetSheet<TripleTriadRule>();var cardNames=data.Excel.GetSheet<TripleTriadCard>();
    Console.WriteLine($"TRIAD rows={matches.Count} withRules={matches.Count(row=>row.TripleTriadRule.Any(rule=>rule.RowId!=0))} regional={matches.Count(row=>row.UsesRegionalRules)}");
    foreach(var rule in rules)Console.WriteLine($"RULE {rule.RowId} '{rule.Name.ExtractText()}'");
    var npcs=data.Excel.GetSheet<ENpcBase>().Select(npc=>(npc.RowId,Match:npc.ENpcData.Select(value=>value.RowId).FirstOrDefault(id=>matches.HasRow(id)))).Where(pair=>pair.Match!=0).ToList();
    Console.WriteLine($"TRIAD npcsLinked={npcs.Count} distinctRows={npcs.Select(pair=>pair.Match).Distinct().Count()}");
    var triadResidents=data.Excel.GetSheet<ENpcResident>();var levels=data.Excel.GetSheet<Level>().Where(level=>level.Type==8).GroupBy(level=>level.Object.RowId).ToDictionary(group=>group.Key,group=>group.First());
    string Card(uint id)=>id==0 ? "-":cardNames.TryGetRow(id,out var card) ? $"{id}:{card.Name.ExtractText()}":$"{id}:?";
    foreach(var (npcId,matchId) in npcs.Where(pair=>triadResidents.TryGetRow(pair.RowId,out var who) && who.Singular.ExtractText() is "Rowena" or "Triple Triad Master" or "maître de Triple Triade" || pair.RowId is 1001304).Concat(npcs.Take(4)))
    {
        var row=matches.GetRow(matchId);
        var place=levels.TryGetValue(npcId,out var level) && level.Map.IsValid ? $"map {level.Map.RowId} '{level.Map.Value.PlaceName.Value.Name.ExtractText()}'":"no Level";
        Console.WriteLine($"NPC {npcId} '{(triadResidents.TryGetRow(npcId,out var r) ? r.Singular.ExtractText():"")}' match {matchId} {place} regional={row.UsesRegionalRules} fee={row.Fee}"
            +$" rules=[{string.Join(",",row.TripleTriadRule.Where(rule=>rule.RowId!=0).Select(rule=>$"{rule.RowId}:{rule.Value.Name.ExtractText()}"))}]"
            +$" fixed=[{string.Join(",",row.TripleTriadCardFixed.Select(card=>Card(card.RowId)))}] variable=[{string.Join(",",row.TripleTriadCardVariable.Select(card=>Card(card.RowId)))}]"
            +$" rewards=[{string.Join(",",row.ItemPossibleReward.Where(item=>item.RowId!=0).Select(item=>item.RowId))}]");
    }
    return;
}
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
