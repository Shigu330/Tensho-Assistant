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
