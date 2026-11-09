using System.Globalization;
using System.Text;
using System.Text.Json;
using Lumina;
using Lumina.Data;
using Lumina.Data.Files;
using Lumina.Excel.Sheets;

// Read-only: the facts the plugin's parsers recorded in game (Discoveries.json) and the market prices seen (MarketSeen.json), checked against the game's own
// sheets. Arguments: the sqpack directory, then the Discoveries folder. Nothing is written: the report goes to the console. Deterministic: « now » is the base's
// own Written date (the market file: its last write), facts are read in file order and every list is sorted.
// IMPOSSIBLE = contradicts a sheet or the plugin's own rules; SUSPECT = possible but unlikely, to look at; NOTE = not checkable from the sheets, or a confirmation.
if(args.Length<2){Console.Error.WriteLine("Arguments: <sqpack folder> <Discoveries folder>");return 2;}
Console.OutputEncoding=new UTF8Encoding(false);CultureInfo.DefaultThreadCurrentCulture=CultureInfo.InvariantCulture;CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
using var game=new GameData(args[0],new LuminaOptions{DefaultExcelLanguage=Language.French,CacheFileResources=false});
var json=new JsonSerializerOptions{PropertyNameCaseInsensitive=true};
var basePath=Path.Combine(args[1],"Discoveries.json");var marketPath=Path.Combine(args[1],"MarketSeen.json");
var file=JsonSerializer.Deserialize<FactFile>(File.ReadAllText(basePath),json) ?? throw new InvalidDataException("Discoveries.json unreadable");
var now=file.Written;

var items=game.GetExcelSheet<Item>()!;var bnpcNames=game.GetExcelSheet<BNpcName>()!;var bnpcBases=game.GetExcelSheet<BNpcBase>()!;var fates=game.GetExcelSheet<Fate>()!;
var territories=game.GetExcelSheet<TerritoryType>()!;var maps=game.GetExcelSheet<Map>()!;var weathers=game.GetExcelSheet<Weather>()!;var weatherRates=game.GetExcelSheet<WeatherRate>()!;
var fishParameters=game.GetExcelSheet<FishParameter>()!;var fishingSpots=game.GetExcelSheet<FishingSpot>()!;var spearItems=game.GetExcelSheet<SpearfishingItem>()!;
var gatheringPoints=game.GetExcelSheet<GatheringPoint>()!;var enpcResidents=game.GetExcelSheet<ENpcResident>()!;var enpcBases=game.GetExcelSheet<ENpcBase>()!;
var levels=game.GetExcelSheet<Level>()!;var quests=game.GetExcelSheet<Quest>()!;var festivals=game.GetExcelSheet<Festival>()!;var retainerTasks=game.GetExcelSheet<RetainerTask>()!;
var retainerNormals=game.GetExcelSheet<RetainerTaskNormal>()!;var fashionThemes=game.GetExcelSheet<FashionCheckWeeklyTheme>()!;var fashionCategories=game.GetExcelSheet<FashionCheckThemeCategory>()!;
var worlds=game.GetExcelSheet<World>()!;var eventItemSheet=game.GetExcelSheet<EventItem>()!;var notorious=game.GetExcelSheet<NotoriousMonster>()!;var uiCategories=game.GetExcelSheet<ItemUICategory>()!;

string ItemName(uint id)=>items.GetRowOrDefault(id) is { } row ? $"{id} « {row.Name.ExtractText()} »":$"{id} (no Item row)";
string MonsterName(uint id)=>bnpcNames.GetRowOrDefault(id) is { } row ? $"{id} « {row.Singular.ExtractText()} »":$"{id} (no BNpcName row)";
string TerritoryName(uint id)=>id==0 ? "0":territories.GetRowOrDefault(id) is { } row ? $"{id} « {(row.PlaceName.ValueNullable?.Name.ExtractText() is { Length:>0 } place ? place:row.Name.ExtractText())} »":$"{id} (no TerritoryType row)";
string LevelPath(uint territory){var bg=territories.GetRowOrDefault(territory)?.Bg.ExtractText() ?? "";var cut=bg.IndexOf("/level/",StringComparison.Ordinal);return cut<0 ? bg:bg[..cut];}
string MapName(uint id)=>id==0 ? "0":maps.GetRowOrDefault(id) is { } row ? $"{id} « {row.PlaceName.ValueNullable?.Name.ExtractText()}{(row.PlaceNameSub.RowId!=0 ? " / "+row.PlaceNameSub.ValueNullable?.Name.ExtractText():"")} »":$"{id} (no Map row)";
string WeatherName(uint id)=>weathers.GetRowOrDefault(id) is { } row ? $"{id} « {row.Name.ExtractText()} »":$"{id} (no Weather row)";
string FateName(uint id)=>fates.GetRowOrDefault(id) is { } row ? $"{id} « {row.Name.ExtractText()} »":$"{id} (no Fate row)";
string NpcName(uint id)=>enpcResidents.GetRowOrDefault(id) is { } row ? $"{id} « {row.Singular.ExtractText()} »":$"{id} (no ENpcResident row)";
string QuestName(uint id)=>quests.GetRowOrDefault(id) is { } row ? $"{id} « {row.Name.ExtractText()} »":$"{id} (no Quest row)";
string F(float value)=>value.ToString("0.0",CultureInfo.InvariantCulture);

// Indexes built once from the sheets.
var rodFish=fishParameters.Where(row=>row.Item.RowId!=0).Select(row=>row.Item.RowId).ToHashSet();
var spotsByTerritory=fishingSpots.Where(row=>row.TerritoryType.RowId!=0).GroupBy(row=>row.TerritoryType.RowId).ToDictionary(group=>group.Key,group=>group.ToArray());
// 0.1004: ocean fishing happens on the ship (IKDRoute.Instance → its territory); its fish are those of the IKDSpot fishing spots, its weather is the sea's
// (the spectral current included), so the ship's own WeatherRate does not apply.
var oceanTerritories=new HashSet<uint>();var oceanFish=new HashSet<uint>();
foreach(var route in game.GetExcelSheet<IKDRoute>()!)if(route.Instance.ValueNullable is { } duty && duty.TerritoryType.RowId!=0)oceanTerritories.Add(duty.TerritoryType.RowId);
foreach(var ikd in game.GetExcelSheet<IKDSpot>()!)
    foreach(var spot in new[]{ikd.SpotMain.ValueNullable,ikd.SpotSub.ValueNullable})
        if(spot is { } fishing)foreach(var item in fishing.Item)if(item.RowId!=0)oceanFish.Add(item.RowId);
var spearRowsByItem=spearItems.Where(row=>row.Item.RowId!=0).GroupBy(row=>row.Item.RowId).ToDictionary(group=>group.Key,group=>group.Select(row=>row.RowId).ToHashSet());
var spearPointsByTerritory=new Dictionary<uint,List<(uint Point,uint Base,HashSet<uint> SpearRows)>>();
foreach(var point in gatheringPoints)
{
    if(point.TerritoryType.RowId==0 || point.GatheringPointBase.ValueNullable is not { } basis)continue;
    var rows=basis.Item.Where(reference=>reference.RowId!=0 && reference.RowType==typeof(SpearfishingItem)).Select(reference=>reference.RowId).ToHashSet();
    if(rows.Count==0)continue;
    if(!spearPointsByTerritory.TryGetValue(point.TerritoryType.RowId,out var list))spearPointsByTerritory[point.TerritoryType.RowId]=list=[];
    list.Add((point.RowId,basis.RowId,rows));
}
var npcLevels=levels.Where(row=>row.Type==8 && row.Object.RowId!=0).GroupBy(row=>row.Object.RowId).ToDictionary(group=>group.Key,group=>group.ToArray());
// The bait category: the ItemUICategory of the items named as bait by no sheet is not known, so the category is read from the facts' baits and reported.
var dayZero=new DateTimeOffset(2013,8,27,0,0,0,TimeSpan.Zero);
int DayNumber(DateTimeOffset at)=>(int)Math.Clamp((at-dayZero).TotalDays,0,100_000);
int MinuteOfDay(DateTimeOffset at)=>(int)((at.UtcTicks-DateTimeOffset.UnixEpoch.UtcTicks)%(4200L*TimeSpan.TicksPerSecond)*12/(35L*TimeSpan.TicksPerSecond));
int Target(DateTimeOffset date)
{
    var seconds=date.ToUnixTimeSeconds();var hour=seconds/175;var shifted=(uint)(hour+8-hour%8)%24;
    unchecked{var seed=(uint)(seconds/4200)*100+shifted;var first=(seed<<11)^seed;return (int)(((first>>8)^first)%100);}
}
(uint Weather,uint Rate,string Why) Forecast(uint territory,DateTimeOffset at)
{
    if(territories.GetRowOrDefault(territory) is not { } zone || zone.WeatherRate.ValueNullable is not { } rate)return (0,0,"no WeatherRate");
    var target=Target(DateTimeOffset.FromUnixTimeSeconds(at.ToUnixTimeSeconds()/1400*1400));var cumulative=0;
    foreach(var (weather,chance) in rate.Weather.Zip(rate.Rate)){cumulative+=chance;if(target<cumulative)return (weather.RowId,rate.RowId,"");}
    return (0,rate.RowId,"roll not covered");
}
HashSet<uint> PossibleWeathers(uint territory)=>territories.GetRowOrDefault(territory)?.WeatherRate.ValueNullable is { } rate
    ? rate.Weather.Zip(rate.Rate).Where(pair=>pair.Second>0 && pair.First.RowId!=0).Select(pair=>pair.First.RowId).ToHashSet():[];
(float X,float Y,bool On) Coordinate(Map map,float x,float z)
{
    var scale=Math.Max(1,(int)map.SizeFactor)/100f;
    var px=(x+map.OffsetX)*scale+1024;var py=(z+map.OffsetY)*scale+1024;
    return (41f/scale*px/2048+1,41f/scale*py/2048+1,px is >= 0 and <= 2048 && py is >= 0 and <= 2048);
}
(float Px,float Py) Pixel(Map map,float x,float z){var scale=Math.Max(1,(int)map.SizeFactor)/100f;return ((x+map.OffsetX)*scale+1024,(z+map.OffsetY)*scale+1024);}

// FATE ranges in a territory's layout (Fate.Location = instance id, docs/evidence/0.721-fates.md).
var fateLayouts=new Dictionary<uint,Dictionary<uint,(float X,float Z)>>();
Dictionary<uint,(float X,float Z)> FateLayout(uint territory)
{
    if(fateLayouts.TryGetValue(territory,out var found))return found;
    var result=new Dictionary<uint,(float,float)>();
    if(territories.GetRowOrDefault(territory) is { } zone)
    {
        var bg=zone.Bg.ExtractText();var cut=bg.IndexOf("/level/",StringComparison.Ordinal);
        if(cut>=0)
            foreach(var kind in new[]{"planevent","planmap"})
            {
                var path=$"bg/{bg[..(cut+1)]}level/{kind}.lgb";
                if(!game.FileExists(path))continue;
                var bytes=game.GetFile(path)!.Data;
                if(bytes.Length<16 || bytes[0]!='L' || bytes[12]!='L')continue;
                foreach(var entry in game.GetFile<LgbFile>(path)!.Layers.SelectMany(layer=>layer.InstanceObjects))result.TryAdd(entry.InstanceId,(entry.Transform.Translation.X,entry.Transform.Translation.Z));
            }
    }
    return fateLayouts[territory]=result;
}

var kindNames=new Dictionary<int,string>{[1]="MonsterPlace",[2]="HuntMarkPlace",[3]="FateSeen",[4]="Desynthesis",[5]="Venture",[6]="DutyChest",[7]="FateReward",[8]="MonsterDrop",
    [9]="ItemFromItem",[10]="FishCatch",[11]="FishCondition",[12]="FashionEvaluation",[13]="VoyageDrop",[14]="VoyageUnlock",[15]="BlueSpellLearned",[16]="BeastTamed",[17]="FestivalSeen",
    [18]="VistaSeen",[19]="NpcPlace",[20]="FishLure",[21]="FishTransition",[22]="BlueSpellCaster",[23]="FishBite",[24]="Reduction",[25]="QuestItemAsked",[28]="SpearShadow",
    // 0.1004: the kinds added since 0.933 (DiscoveriesModel.cs), and those that keep a place (DiscoveryStore.HasPlace, tools/discoveries-merge.py PLACED).
    [29]="FishBiteBait",[30]="IntuitionStart",[31]="IntuitionCatch",[32]="VoyageVisit",[33]="MonsterKill",[34]="DutyChestOpened",[35]="SpectralBefore",[36]="FishPoints",[37]="AirshipDrop"};
var placeKinds=new HashSet<int>{1,2,3,8,10,19,22,23,28,29,30};
var report=new SortedDictionary<int,KindReport>();
KindReport For(int kind)=>report.TryGetValue(kind,out var found) ? found:report[kind]=new KindReport();
string Describe(Fact fact)
{
    var text=new StringBuilder($"#{fact.Index} kind {fact.Kind} subject {fact.Subject} detail {fact.Detail}");
    if(fact.Territory!=0)text.Append(fact.Kind==12 ? $" theme {fact.Territory}":$" territory {TerritoryName(fact.Territory)}");
    if(fact.Map!=0)text.Append($" map {MapName(fact.Map)} at {F(fact.X)},{F(fact.Z)}");
    text.Append($" levels {fact.LevelMin}..{fact.LevelMax} seen {fact.Seen}+{fact.Imported} {fact.First:yyyy-MM-dd HH:mm:ss}..{fact.Last:HH:mm:ss}");
    return text.ToString();
}

var facts=file.Facts;for(var index=0;index<facts.Count;index++)facts[index].Index=index;
// Duplicates: the plugin's own key.
var duplicateKeys=facts.GroupBy(fact=>$"{fact.Kind}:{fact.Subject}:{fact.Detail}:{fact.Territory}:{fact.Map}:{fact.CellX}:{fact.CellZ}").Where(group=>group.Count()>1).ToDictionary(group=>group.Key,group=>group.Count());
var baitCategories=new SortedDictionary<string,int>(StringComparer.Ordinal);var fashionValues=new SortedDictionary<string,int>(StringComparer.Ordinal);
foreach(var fact in facts)
{
    var problems=new List<string>();var notes=new List<string>();
    void Impossible(string why)=>problems.Add("IMPOSSIBLE: "+why);
    void Suspect(string why)=>problems.Add("SUSPECT: "+why);
    void Note(string why)=>notes.Add("NOTE: "+why);
    var key=$"{fact.Kind}:{fact.Subject}:{fact.Detail}:{fact.Territory}:{fact.Map}:{fact.CellX}:{fact.CellZ}";
    if(duplicateKeys.TryGetValue(key,out var twins))Impossible($"duplicate key {key} ({twins} facts share it; the store keeps one per key)");
    if(!kindNames.ContainsKey(fact.Kind))Impossible($"kind {fact.Kind} is not a current DiscoveryKind (26 and 27 are retired)");
    if(fact.Subject==0)Impossible("Subject 0");
    if(!float.IsFinite(fact.X) || !float.IsFinite(fact.Z))Impossible("non-finite place");
    else if(((int)MathF.Floor(fact.X/50),(int)MathF.Floor(fact.Z/50))!=(fact.CellX,fact.CellZ))Impossible($"cell {fact.CellX},{fact.CellZ} is not floor(X/50),floor(Z/50) = {(int)MathF.Floor(fact.X/50)},{(int)MathF.Floor(fact.Z/50)}");
    if(fact.First>fact.Last)Impossible($"First {fact.First:O} after Last {fact.Last:O}");
    if(fact.Last>now)Impossible($"Last {fact.Last:O} after the file's Written {now:O}");
    if(fact.First<dayZero)Impossible($"First {fact.First:O} before 2013-08-27");
    if(fact.Seen<0 || fact.Imported<0 || fact.Seen+fact.Imported==0)Suspect($"Seen {fact.Seen} Imported {fact.Imported}");
    if(fact.LevelMin>fact.LevelMax)Impossible($"LevelMin {fact.LevelMin} > LevelMax {fact.LevelMax}");
    // 0.1004: a sighting within the kind's gap widens the levels without counting a new one (DiscoveryStore.See): only a fact seen at one moment must have one level.
    if(fact.Seen==1 && fact.Imported==0 && fact.First==fact.Last && fact.LevelMin!=fact.LevelMax)Impossible($"seen once but LevelMin {fact.LevelMin} != LevelMax {fact.LevelMax}");
    // Place.
    Map? map=null;
    if(placeKinds.Contains(fact.Kind))
    {
        if(fact.Map==0)Impossible("a place kind without a map");
        else if(maps.GetRowOrDefault(fact.Map) is not { } row)Impossible($"Map {fact.Map} has no row");
        else
        {
            map=row;
            if(row.TerritoryType.RowId!=fact.Territory)
            {
                var own=territories.GetRowOrDefault(fact.Territory)?.Map.RowId ?? 0;
                if(own==fact.Map)Note($"Map.TerritoryType is {row.TerritoryType.RowId} but TerritoryType {fact.Territory}.Map is this map");
                else if(LevelPath(fact.Territory).Length>0 && LevelPath(fact.Territory)==LevelPath(row.TerritoryType.RowId))
                    Note($"game data edge case: territory {TerritoryName(fact.Territory)} (Map {MapName(own)}) is a copy of {TerritoryName(row.TerritoryType.RowId)} (same Bg {LevelPath(fact.Territory)}), whose map {MapName(fact.Map)} the game showed");
                else if(LevelPath(row.TerritoryType.RowId).Length==0 && maps.GetRowOrDefault(own) is { } ownMap && ownMap.Id.ExtractText().Split('/')[0]==row.Id.ExtractText().Split('/')[0])
                    Note($"game data edge case: map {MapName(fact.Map)} (Id {row.Id.ExtractText()}) still names the retired TerritoryType {row.TerritoryType.RowId} (no Bg); the territory {TerritoryName(fact.Territory)} has map {MapName(own)} (Id {ownMap.Id.ExtractText()}) of the same set");
                else Impossible($"Map {fact.Map}.TerritoryType = {TerritoryName(row.TerritoryType.RowId)} (Bg {LevelPath(row.TerritoryType.RowId)}), not the fact's territory {TerritoryName(fact.Territory)} (Bg {LevelPath(fact.Territory)}, its own Map {MapName(own)}, use {territories.GetRowOrDefault(fact.Territory)?.TerritoryIntendedUse.RowId}, CFC {territories.GetRowOrDefault(fact.Territory)?.ContentFinderCondition.RowId})");
            }
            var (cx,cy,on)=Coordinate(row,fact.X,fact.Z);
            if(fact.X==0 && fact.Z==0)Suspect("place exactly 0,0: the position was not read (a zero vector), not a real place");
            else if(!on)Impossible($"place {F(fact.X)},{F(fact.Z)} is outside map {fact.Map} (coordinate X {F(cx)} Y {F(cy)}, SizeFactor {row.SizeFactor}, offset {row.OffsetX},{row.OffsetY})");
            else if(cx<1 || cy<1 || cx>42 || cy>42)Suspect($"coordinate X {F(cx)} Y {F(cy)} outside 1..42");
        }
    }
    else if(fact.Map!=0 || fact.X!=0 || fact.Z!=0)Suspect($"a kind without place carries map {fact.Map} at {F(fact.X)},{F(fact.Z)}");
    if(fact.Territory!=0 && fact.Kind!=12 && territories.GetRowOrDefault(fact.Territory) is null)Impossible($"TerritoryType {fact.Territory} has no row");
    bool IsRodFish(uint id){if(rodFish.Contains(id))return true;Impossible($"{ItemName(id)} is not the Item of any FishParameter row (rod fishing)");return false;}
    void FishAtTerritory(uint fish,string what)
    {
        if(oceanTerritories.Contains(fact.Territory))
        {
            if(oceanFish.Contains(fish))Note($"{what} {ItemName(fish)}: an ocean fish (IKDSpot), caught on the ship");
            else Impossible($"{what} {ItemName(fish)}: on the ship but in no IKDSpot fishing spot");
            return;
        }
        var spots=spotsByTerritory.GetValueOrDefault(fact.Territory) ?? [];
        var holding=spots.Where(spot=>spot.Item.Any(item=>item.RowId==fish)).ToArray();
        if(holding.Length==0){Impossible($"{what} {ItemName(fish)}: no FishingSpot of territory {TerritoryName(fact.Territory)} lists it ({spots.Length} spots there)");return;}
        if(map is { } shown && fact.Map!=0)
        {
            var (px,py)=Pixel(shown,fact.X,fact.Z);
            var nearest=holding.Select(spot=>(Spot:spot,Distance:MathF.Sqrt((spot.X-px)*(spot.X-px)+(spot.Z-py)*(spot.Z-py)),Area:0.155f*spot.Radius)).OrderBy(pair=>pair.Distance).First();
            var label=$"FishingSpot {nearest.Spot.RowId} « {nearest.Spot.PlaceName.ValueNullable?.Name.ExtractText()} » at texture {nearest.Spot.X},{nearest.Spot.Z}, {F(nearest.Distance)} px from the fact, area radius ≈ {F(nearest.Area)} px (0.155 × Radius {nearest.Spot.Radius})";
            if(nearest.Distance>nearest.Area*2+40)Suspect($"{what} far from every spot holding it: {label}");
            else Note($"{what} at {label}");
        }
    }
    void WeatherPossible(uint weather,string what)
    {
        if(weathers.GetRowOrDefault(weather) is null){Impossible($"{what} {weather} has no Weather row");return;}
        if(oceanTerritories.Contains(fact.Territory)){Note($"{what} {WeatherName(weather)} at sea (the ship's own WeatherRate does not apply)");return;}
        var possible=PossibleWeathers(fact.Territory);
        if(possible.Count==0)Suspect($"territory {TerritoryName(fact.Territory)} has no WeatherRate to check {what} against");
        else if(!possible.Contains(weather))Impossible($"{what} {WeatherName(weather)} is not in territory {TerritoryName(fact.Territory)}'s WeatherRate ({string.Join(", ",possible.Order().Select(WeatherName))})");
    }
    void MinutesAgree()
    {
        if(fact.LevelMin is < 0 or > 1439 || fact.LevelMax is < 0 or > 1439){Impossible($"Eorzea minutes {fact.LevelMin}..{fact.LevelMax} outside 0..1439");return;}
        // The minute is computed from the catch time (First for the first sighting, Last for the latest): both must fall in the range.
        foreach(var (label,at) in new[]{("First",fact.First),("Last",fact.Last)})
        {
            var minute=MinuteOfDay(at);
            if(fact.Imported==0 && (minute<fact.LevelMin-1 || minute>fact.LevelMax+1))Suspect($"Eorzea minute of {label} ({at:HH:mm:ss}) is {minute}, outside the range {fact.LevelMin}..{fact.LevelMax}");
        }
    }
    switch(fact.Kind)
    {
        case 1:
            if(bnpcNames.GetRowOrDefault(fact.Subject) is not { } name || name.Singular.IsEmpty)Impossible($"BNpcName {fact.Subject} missing or nameless");
            if(fact.Detail!=0 && bnpcBases.GetRowOrDefault(fact.Detail) is null)Impossible($"BNpcBase {fact.Detail} has no row");
            if(fact.LevelMin<1 || fact.LevelMax>100)Suspect($"monster level {fact.LevelMin}..{fact.LevelMax} outside 1..100");
            else if(fact.LevelMax-fact.LevelMin>5)Suspect($"one monster (name and base) in one cell with levels {fact.LevelMin}..{fact.LevelMax}");
            if(notorious.FirstOrDefault(row=>row.BNpcBase.RowId==fact.Detail && fact.Detail!=0) is { RowId:not 0 } hunt)Note($"BNpcBase {fact.Detail} is NotoriousMonster {hunt.RowId} (rank {hunt.Rank}): a hunt mark also kept as a monster");
            break;
        case 2:
            if(bnpcNames.GetRowOrDefault(fact.Subject) is null)Impossible($"BNpcName {fact.Subject} has no row");
            if(fact.LevelMin is < 1 or > 3 || fact.LevelMax is < 1 or > 3)Impossible($"rank {fact.LevelMin}..{fact.LevelMax} outside 1..3");
            var marks=notorious.Where(row=>row.BNpcName.RowId==fact.Subject).ToArray();
            if(marks.Length==0)Suspect($"no NotoriousMonster row names {MonsterName(fact.Subject)}");
            else
            {
                if(!marks.Any(row=>row.BNpcBase.RowId==fact.Detail))Suspect($"NotoriousMonster rows of {MonsterName(fact.Subject)} have BNpcBase {string.Join("/",marks.Select(row=>row.BNpcBase.RowId))}, not {fact.Detail}");
                Note($"NotoriousMonster {string.Join("/",marks.Select(row=>$"{row.RowId} rank {row.Rank} base {row.BNpcBase.RowId}"))}; fact rank {fact.LevelMin}");
            }
            break;
        case 3:
            if(fates.GetRowOrDefault(fact.Subject) is not { } fate){Impossible($"Fate {fact.Subject} has no row");break;}
            if(fact.LevelMin!=0 && (fact.LevelMin<fate.ClassJobLevel || fact.LevelMax>Math.Max(fate.ClassJobLevel,fate.ClassJobLevelMax)))
                Suspect($"level {fact.LevelMin}..{fact.LevelMax} but Fate.ClassJobLevel {fate.ClassJobLevel}, ClassJobLevelMax {fate.ClassJobLevelMax}");
            if(fate.Location==0)Note("Fate.Location is 0: no layout place to compare");
            else if(FateLayout(fact.Territory).TryGetValue(fate.Location,out var range))
            {
                var distance=MathF.Sqrt((range.X-fact.X)*(range.X-fact.X)+(range.Z-fact.Z)*(range.Z-fact.Z));
                if(distance>60)Suspect($"{F(distance)} yalms from Fate.Location {fate.Location} in the territory's layout ({F(range.X)},{F(range.Z)})");
                else Note($"{F(distance)} yalms from its layout range {fate.Location}");
            }
            else Impossible($"Fate.Location {fate.Location} is in neither planevent.lgb nor planmap.lgb of territory {TerritoryName(fact.Territory)}");
            break;
        case 4:
            if(items.GetRowOrDefault(fact.Subject) is null)Impossible($"result Item {fact.Subject} has no row");
            if(items.GetRowOrDefault(fact.Detail) is not { } source)Impossible($"source Item {fact.Detail} has no row");
            else if(source.Desynth==0)Impossible($"source {ItemName(fact.Detail)} has Item.Desynth 0 (not desynthesizable); ClassJobRepair {source.ClassJobRepair.RowId}");
            else Note($"source Item.Desynth {source.Desynth}, ClassJobRepair {source.ClassJobRepair.RowId}");
            if(fact.Subject==fact.Detail)Suspect("result is the source item");
            if(fact.LevelMin<1)Impossible($"quantity {fact.LevelMin}");
            if(fact.LevelMax>999)Suspect($"quantity {fact.LevelMax}");
            break;
        case 5:
            if(items.GetRowOrDefault(fact.Subject) is null)Impossible($"Item {fact.Subject} has no row");
            if(retainerTasks.GetRowOrDefault(fact.Detail) is not { } task){Impossible($"RetainerTask {fact.Detail} has no row");break;}
            if(fact.LevelMin<1)Impossible($"quantity {fact.LevelMin}");
            if(task.IsRandom)Note($"RetainerTask {fact.Detail} is random (IsRandom, Task {task.Task.RowId}): rewards not listed by the sheet");
            else if(retainerNormals.GetRowOrDefault(task.Task.RowId) is { } normal)
            {
                if(normal.Item.RowId!=fact.Subject)Impossible($"RetainerTask {fact.Detail} → RetainerTaskNormal {normal.RowId} gives {ItemName(normal.Item.RowId)}, not {ItemName(fact.Subject)}");
                else if(!normal.Quantity.Any(quantity=>quantity==fact.LevelMin) || !normal.Quantity.Any(quantity=>quantity==fact.LevelMax))
                    Suspect($"quantity {fact.LevelMin}..{fact.LevelMax} not among RetainerTaskNormal.Quantity {string.Join("/",normal.Quantity)}");
                else Note($"RetainerTaskNormal {normal.RowId} item and quantity agree");
            }
            else Suspect($"RetainerTask {fact.Detail} not random but Task {task.Task.RowId} is no RetainerTaskNormal row");
            break;
        case 10:
            if(IsRodFish(fact.Subject))FishAtTerritory(fact.Subject,"fish");
            if(fact.LevelMin<1)Impossible($"quantity {fact.LevelMin}");
            if(fact.Detail==0)Suspect("bait unknown (0)");
            else if(items.GetRowOrDefault(fact.Detail) is not { } bait)Impossible($"bait Item {fact.Detail} has no row");
            else if(rodFish.Contains(fact.Detail)){FishAtTerritory(fact.Detail,"mooch fish (bait)");}
            else
            {
                var category=$"{bait.ItemUICategory.RowId} « {bait.ItemUICategory.ValueNullable?.Name.ExtractText()} »";
                baitCategories[category]=baitCategories.GetValueOrDefault(category)+1;
                if(bait.ItemUICategory.RowId!=33)Impossible($"bait {ItemName(fact.Detail)} has ItemUICategory {category}, not 33 (fishing tackle), and is no fish");
            }
            break;
        case 11:
            if(IsRodFish(fact.Subject))FishAtTerritory(fact.Subject,"fish");
            WeatherPossible(fact.Detail,"weather");MinutesAgree();
            if(fact.Imported==0 && !oceanTerritories.Contains(fact.Territory))foreach(var (label,at) in new[]{("First",fact.First),("Last",fact.Last)})
            {
                var (weather,_,why)=Forecast(fact.Territory,at);
                if(why.Length>0)Suspect($"forecast at {label} not computable: {why}");
                else if(weather!=fact.Detail)Impossible($"the forecast at {label} {at:HH:mm:ss} is {WeatherName(weather)}, not {WeatherName(fact.Detail)}");
            }
            break;
        case 12:
            if(items.GetRowOrDefault(fact.Subject) is not { } worn)Impossible($"Item {fact.Subject} has no row");
            else if(worn.EquipSlotCategory.RowId==0)Impossible($"{ItemName(fact.Subject)} is not equipment (EquipSlotCategory 0)");
            if(fact.Detail!=0 && fashionCategories.GetRowOrDefault(fact.Detail) is null)Impossible($"FashionCheckThemeCategory {fact.Detail} has no row");
            if(fashionThemes.GetRowOrDefault(fact.Territory) is not { } theme)Impossible($"FashionCheckWeeklyTheme {fact.Territory} has no row");
            else Note($"theme « {theme.Name.ExtractText()} », category {(fact.Detail==0 ? "0 (none)":$"{fact.Detail} « {fashionCategories.GetRowOrDefault(fact.Detail)?.Name.ExtractText()} »")}");
            fashionValues[$"evaluation {fact.LevelMin} with category {(fact.Detail==0 ? "0":"≠0")}"]=fashionValues.GetValueOrDefault($"evaluation {fact.LevelMin} with category {(fact.Detail==0 ? "0":"≠0")}")+1;
            if(fact.LevelMin is < 0 or > 255)Impossible($"evaluation {fact.LevelMin} is not a byte");
            break;
        case 17:
            if(festivals.GetRowOrDefault(fact.Subject) is not { } festival)Impossible($"Festival {fact.Subject} has no row");
            else Note($"Festival « {festival.Name.ExtractText()} », phase {fact.Detail}, days {fact.LevelMin}..{fact.LevelMax} = {dayZero.AddDays(fact.LevelMin):yyyy-MM-dd}..{dayZero.AddDays(fact.LevelMax):yyyy-MM-dd}");
            if(fact.LevelMin<DayNumber(fact.First) || fact.LevelMax>DayNumber(fact.Last))Impossible($"days {fact.LevelMin}..{fact.LevelMax} outside the days of First..Last {DayNumber(fact.First)}..{DayNumber(fact.Last)}");
            if(fact.Imported==0 && fact.Seen>(DayNumber(fact.Last)-DayNumber(fact.First)+1))
                Suspect($"Seen {fact.Seen} but the parser writes a festival once a day and First..Last spans {DayNumber(fact.Last)-DayNumber(fact.First)+1} day(s): SeeSource counts every call, the plugin's day guard is per session");
            break;
        case 19:
            if(enpcResidents.GetRowOrDefault(fact.Subject) is null)Impossible($"ENpcResident {fact.Subject} has no row");
            if(enpcBases.GetRowOrDefault(fact.Subject) is null)Impossible($"ENpcBase {fact.Subject} has no row");
            if(npcLevels.TryGetValue(fact.Subject,out var placed))
            {
                var here=placed.Where(row=>row.Territory.RowId==fact.Territory).ToArray();
                if(here.Length==0)Suspect($"Level places {NpcName(fact.Subject)} only in {string.Join(", ",placed.Select(row=>TerritoryName(row.Territory.RowId)).Distinct())}");
                else
                {
                    var nearest=here.Select(row=>(Row:row,Distance:MathF.Sqrt((row.X-fact.X)*(row.X-fact.X)+(row.Z-fact.Z)*(row.Z-fact.Z)))).OrderBy(pair=>pair.Distance).First();
                    if(nearest.Distance>10)Suspect($"Level {nearest.Row.RowId} places it {F(nearest.Distance)} yalms away ({F(nearest.Row.X)},{F(nearest.Row.Z)})");
                    else Note($"Level {nearest.Row.RowId} places it {F(nearest.Distance)} yalms away");
                }
            }
            else Note("no Level row places this ENpc (the reason the parser exists)");
            break;
        case 20:
            IsRodFish(fact.Subject);
            if(fact.Detail is not (1 or 2))Impossible($"lure {fact.Detail} is neither 1 (Ambitious) nor 2 (Modest)");
            if(fact.LevelMin<1)Impossible($"count {fact.LevelMin}");
            break;
        case 21:
            if(IsRodFish(fact.Subject))FishAtTerritory(fact.Subject,"fish");
            WeatherPossible(fact.Detail,"previous weather");MinutesAgree();
            if(fact.Imported==0 && !oceanTerritories.Contains(fact.Territory))foreach(var (label,at) in new[]{("First",fact.First),("Last",fact.Last)})
            {
                var (weather,_,why)=Forecast(fact.Territory,at.AddSeconds(-1400));
                if(why.Length>0)Suspect($"forecast 1400 s before {label} not computable: {why}");
                else if(weather!=fact.Detail)Impossible($"the forecast 1400 s before {label} ({at.AddSeconds(-1400):HH:mm:ss}) is {WeatherName(weather)}, not {WeatherName(fact.Detail)}");
            }
            break;
        case 23:
            if(IsRodFish(fact.Subject))FishAtTerritory(fact.Subject,"fish");
            if(fact.Detail>3)Impossible($"bite strength {fact.Detail} outside 0..3");
            else if(fact.Detail==0)Note("bite strength unknown (0)");
            if(fact.LevelMin<1 || fact.LevelMax>6000)Impossible($"bite time {fact.LevelMin}..{fact.LevelMax} tenths outside 1..6000 (the parser keeps < 600 s)");
            else if(fact.LevelMax>600)Suspect($"bite time {fact.LevelMax / 10.0:0.0} s, over 60 s");
            break;
        case 25:
        {
            // 0.911: an HQ demand is kept as 1 000 000 + item (QuestItemAsks.Subject).
            var asked=fact.Subject is >= 1_000_000 and < 2_000_000 ? fact.Subject-1_000_000:fact.Subject;
            if(asked!=fact.Subject)Note($"HQ demand of {ItemName(asked)}");
            if(items.GetRowOrDefault(asked) is null)Impossible($"Item {asked} has no row");
            if(quests.GetRowOrDefault(fact.Detail) is not { } quest){Impossible($"Quest {fact.Detail} has no row");break;}
            if(fact.LevelMin<1)Impossible($"quantity {fact.LevelMin}");
            var mentions=quest.QuestParams.Where(param=>param.ScriptArg==asked).Select(param=>param.ScriptInstruction.ExtractText()).ToArray();
            var eventItems=quest.QuestParams.Where(param=>param.ScriptArg>=2_000_000 && param.ScriptArg%500_000==asked).Select(param=>$"{param.ScriptInstruction.ExtractText()}={param.ScriptArg} « {eventItemSheet.GetRowOrDefault(param.ScriptArg)?.Name.ExtractText()} »").ToArray();
            if(mentions.Length>0)Note($"{QuestName(fact.Detail)}: QuestParams name the item ({string.Join(", ",mentions)})");
            else if(eventItems.Length>0)Impossible($"{QuestName(fact.Detail)} asks the key item (EventItem) {string.Join(", ",eventItems)}; {ItemName(asked)} is its id modulo 500 000 (ItemGains.BaseItem), a different, unrelated Item");
            else Suspect($"{QuestName(fact.Detail)}: no QuestParams.ScriptArg is {fact.Subject}; its item-like params: {string.Join(", ",quest.QuestParams.Where(param=>param.ScriptInstruction.ExtractText().Contains("ITEM",StringComparison.Ordinal)).Select(param=>$"{param.ScriptInstruction.ExtractText()}={param.ScriptArg}"))}; ItemCatalyst {string.Join("/",quest.ItemCatalyst.Where(item=>item.RowId!=0).Select(item=>item.RowId))}");
            break;
        }
        case 28:
            if(!spearRowsByItem.TryGetValue(fact.Subject,out var spearRows)){Impossible($"{ItemName(fact.Subject)} is the Item of no SpearfishingItem row");break;}
            // SpearfishingItem.TerritoryType is not compared: in this data it holds 1, 5, 43… (no zone), the column is misnamed by the schema; the GatheringPoint is the proof.
            var points=(spearPointsByTerritory.GetValueOrDefault(fact.Territory) ?? []).Where(point=>point.SpearRows.Overlaps(spearRows)).ToArray();
            if(points.Length==0)Impossible($"no spearfishing GatheringPoint of territory {TerritoryName(fact.Territory)} lists SpearfishingItem {string.Join("/",spearRows)}");
            else Note($"GatheringPointBase {string.Join("/",points.Select(point=>point.Base).Distinct().Order())} of territory {fact.Territory} lists SpearfishingItem {string.Join("/",spearRows)}");
            if(fact.Detail is < 1 or > 3)Impossible($"shadow size {fact.Detail} outside 1..3");
            foreach(var speed in new[]{fact.LevelMin,fact.LevelMax}.Distinct())
                if(speed is < 100 or > 600 || speed%50!=0)Suspect($"speed {speed} is not a multiple of 50 in 100..600 (docs/evidence/0.417-spearfishing.md; doubled speeds go to 1200)");
            if(fact.LevelMin!=fact.LevelMax)Impossible($"two speeds {fact.LevelMin} and {fact.LevelMax} joined in one fact (one size, one cell)");
            break;
    }
    var kindReport=For(fact.Kind);kindReport.Checked++;
    if(problems.Count==0)kindReport.Ok++;else kindReport.Anomalies.Add((fact.Index,Describe(fact),problems));
    if(notes.Count>0)kindReport.Notes.Add((fact.Index,Describe(fact),notes));
}

// SpearShadow: one fish, one shadow.
var shadowConflicts=new List<string>();
foreach(var group in facts.Where(fact=>fact.Kind==28).GroupBy(fact=>fact.Subject).OrderBy(group=>group.Key))
{
    var shadows=group.GroupBy(fact=>(Size:fact.Detail,fact.LevelMin)).Select(shadow=>(shadow.Key.Size,Speed:shadow.Key.LevelMin,Times:shadow.Sum(fact=>fact.Seen+fact.Imported),Facts:shadow.Count())).OrderByDescending(shadow=>shadow.Times).ToArray();
    if(shadows.Length>1)shadowConflicts.Add($"{ItemName(group.Key)}: {string.Join("; ",shadows.Select(shadow=>$"size {shadow.Size} speed {shadow.Speed} ×{shadow.Times} ({shadow.Facts} facts)"))}");
}
// FateSeen: one FATE, one territory.
var fateConflicts=facts.Where(fact=>fact.Kind==3).GroupBy(fact=>fact.Subject).Where(group=>group.Select(fact=>fact.Territory).Distinct().Count()>1)
    .Select(group=>$"{FateName(group.Key)} in {string.Join(", ",group.Select(fact=>TerritoryName(fact.Territory)).Distinct())}").ToArray();
// FashionEvaluation: one category of a theme on one equipment slot.
var fashionSlots=facts.Where(fact=>fact.Kind==12 && fact.Detail!=0).GroupBy(fact=>(fact.Territory,fact.Detail))
    .Select(group=>(group.Key,Slots:group.Select(fact=>items.GetRowOrDefault(fact.Subject)?.EquipSlotCategory.RowId ?? 0).Distinct().Order().ToArray()))
    .Where(pair=>pair.Slots.Length>1).Select(pair=>$"theme {pair.Key.Territory} category {pair.Key.Detail}: EquipSlotCategory {string.Join("/",pair.Slots)}").ToArray();

var output=new StringBuilder();
output.AppendLine($"DiscoveryCheckProbe — {basePath}");
output.AppendLine($"File Version {file.Version}, Producer « {file.Producer} », Written {file.Written:O} (taken as « now »), {facts.Count} facts.");
output.AppendLine();
output.AppendLine("SUMMARY (kind: checked / ok / with anomalies)");
foreach(var (kind,kindReport) in report)output.AppendLine($"  {kind,2} {kindNames.GetValueOrDefault(kind,"?"),-18} {kindReport.Checked,5} / {kindReport.Ok,5} / {kindReport.Anomalies.Count,5}");
output.AppendLine($"  total {report.Values.Sum(item=>item.Checked)} / {report.Values.Sum(item=>item.Ok)} / {report.Values.Sum(item=>item.Anomalies.Count)}");
output.AppendLine($"  duplicate keys: {duplicateKeys.Count}");
output.AppendLine();
output.AppendLine("CROSS-FACT CHECKS");
output.AppendLine($"  SpearShadow fish with more than one shadow: {shadowConflicts.Count}");
foreach(var line in shadowConflicts)output.AppendLine("    "+line);
output.AppendLine($"  FATEs seen in more than one territory: {fateConflicts.Length}");
foreach(var line in fateConflicts)output.AppendLine("    "+line);
output.AppendLine($"  Fashion categories of one theme on several equipment slots: {fashionSlots.Length}");
foreach(var line in fashionSlots)output.AppendLine("    "+line);
output.AppendLine($"  Fashion evaluation bytes: {string.Join("; ",fashionValues.Select(pair=>$"{pair.Key} ×{pair.Value}"))}");
output.AppendLine($"  Bait categories (non-fish baits): {string.Join("; ",baitCategories.Select(pair=>$"{pair.Key} ×{pair.Value}"))}");
output.AppendLine();
foreach(var (kind,kindReport) in report)
{
    output.AppendLine($"== KIND {kind} {kindNames.GetValueOrDefault(kind,"?")}: {kindReport.Checked} checked, {kindReport.Ok} ok, {kindReport.Anomalies.Count} with anomalies");
    foreach(var (_,description,problems) in kindReport.Anomalies){output.AppendLine("  "+description);foreach(var problem in problems)output.AppendLine("      "+problem);}
}
output.AppendLine();
output.AppendLine("NOTES (confirmations and what the sheets cannot check; not counted as anomalies)");
foreach(var (kind,kindReport) in report)
{
    if(kindReport.Notes.Count==0)continue;
    // Monster notes can be many: grouped by text.
    output.AppendLine($"== KIND {kind} {kindNames.GetValueOrDefault(kind,"?")}: {kindReport.Notes.Count} facts with notes");
    foreach(var (_,description,notes) in kindReport.Notes.Take(kind==1 ? 30:400)){output.AppendLine("  "+description);foreach(var note in notes)output.AppendLine("      "+note);}
}

// Market prices seen.
output.AppendLine();
if(File.Exists(marketPath))
{
    var market=JsonSerializer.Deserialize<List<MarketEntry>>(File.ReadAllText(marketPath),json) ?? [];
    var marketNow=new DateTimeOffset(File.GetLastWriteTimeUtc(marketPath),TimeSpan.Zero);
    if(now>marketNow)marketNow=now;
    var marketProblems=new List<string>();int listings=0,sales=0,okEntries=0;var softOutliers=new List<string>();
    var medians=market.SelectMany(entry=>entry.Listings.Select(listing=>(entry.Item,listing.Hq,listing.Price)).Concat(entry.Sales.Select(sale=>(entry.Item,sale.Hq,sale.Price))))
        .Where(price=>price.Price>0).GroupBy(price=>(price.Item,price.Hq)).ToDictionary(group=>group.Key,group=>{var sorted=group.Select(price=>price.Price).Order().ToArray();return sorted[sorted.Length/2];});
    foreach(var entry in market.OrderBy(entry=>entry.World).ThenBy(entry=>entry.Item))
    {
        var problems=new List<string>();
        listings+=entry.Listings.Count;sales+=entry.Sales.Count;
        var worldName=worlds.GetRowOrDefault(entry.World) is { } world ? $"{entry.World} « {world.Name.ExtractText()} »{(world.IsPublic ? "":" (not public)")}":$"{entry.World} (no World row)";
        if(worlds.GetRowOrDefault(entry.World) is not { IsPublic:true })problems.Add($"IMPOSSIBLE: world {worldName}");
        var item=items.GetRowOrDefault(entry.Item);
        if(item is null)problems.Add($"IMPOSSIBLE: Item {entry.Item} has no row");
        else if(item.Value.ItemSearchCategory.RowId==0)problems.Add($"IMPOSSIBLE: {ItemName(entry.Item)} is not marketable (ItemSearchCategory 0)");
        if(entry.ListingsAt>marketNow.AddMinutes(5) || entry.SalesAt>marketNow.AddMinutes(5))problems.Add($"IMPOSSIBLE: ListingsAt {entry.ListingsAt:O} / SalesAt {entry.SalesAt:O} after the file's write {marketNow:O}");
        if(entry.Listings.Count>200 || entry.Sales.Count>200)problems.Add($"IMPOSSIBLE: {entry.Listings.Count} listings, {entry.Sales.Count} sales (kept 200 at most)");
        void Price(string what,decimal price,bool hq,int quantity)
        {
            if(price<=0)problems.Add($"IMPOSSIBLE: {what} price {price}");
            if(quantity<=0 || quantity>9999)problems.Add($"IMPOSSIBLE: {what} quantity {quantity}");
            if(hq && item is { CanBeHq:false })problems.Add($"IMPOSSIBLE: {what} HQ but Item.CanBeHq is false");
            if(item is { } row && quantity>row.StackSize && row.StackSize>0)problems.Add($"IMPOSSIBLE: {what} quantity {quantity} over Item.StackSize {row.StackSize}");
            if(price>0 && medians.TryGetValue((entry.Item,hq),out var median) && median>0)
            {
                var ratio=price/median;
                if(ratio>=1000 || ratio<=0.001m)problems.Add($"SUSPECT: {what} price {price} is {ratio:0.####}× the median {median} of this item ({(hq ? "HQ":"NQ")})");
                else if(ratio>=100 || ratio<=0.01m)softOutliers.Add($"{worldName} {ItemName(entry.Item)} {what} price {price} = {ratio:0.####}× median {median}");
            }
        }
        foreach(var listing in entry.Listings)
        {
            Price("listing",listing.Price,listing.Hq,listing.Quantity);
            if(listing.Own && !entry.OwnKnown)problems.Add("SUSPECT: listing marked Own while OwnKnown is false");
        }
        foreach(var sale in entry.Sales)
        {
            Price($"sale {sale.At:yyyy-MM-dd HH:mm}",sale.Price,sale.Hq,sale.Quantity);
            if(sale.At>entry.SalesAt.AddMinutes(5))problems.Add($"IMPOSSIBLE: sale at {sale.At:O} after SalesAt {entry.SalesAt:O}");
            if(entry.SalesAt-sale.At>TimeSpan.FromDays(30))problems.Add($"SUSPECT: sale at {sale.At:O} older than 30 days before SalesAt (MergeSales keeps 30 days)");
        }
        var twins=entry.Sales.GroupBy(sale=>(sale.Price,sale.Hq,sale.Quantity,sale.At)).Where(group=>group.Count()>1).ToArray();
        foreach(var twin in twins)problems.Add($"SUSPECT: sale twin ×{twin.Count()} {twin.Key.Price} {(twin.Key.Hq ? "HQ":"NQ")} ×{twin.Key.Quantity} at {twin.Key.At:O} (MergeSales removes twins)");
        if(problems.Count==0)okEntries++;
        else marketProblems.Add($"  {worldName} {ItemName(entry.Item)}: {entry.Listings.Count} listings at {entry.ListingsAt:O}, {entry.Sales.Count} sales at {entry.SalesAt:O}\n"+string.Join("\n",problems.Distinct().Select(problem=>"      "+problem)));
    }
    output.AppendLine($"== MARKET {marketPath}: {market.Count} entries (world, item), {listings} listings, {sales} sales; {okEntries} ok, {marketProblems.Count} with anomalies; « now » {marketNow:O}");
    output.AppendLine($"  worlds: {string.Join(", ",market.Select(entry=>entry.World).Distinct().Order().Select(id=>worlds.GetRowOrDefault(id) is { } world ? $"{id} « {world.Name.ExtractText()} »":$"{id} (no row)"))}");
    foreach(var line in marketProblems)output.AppendLine(line);
    output.AppendLine($"  NOTE prices 100× to 1000× from their item's median (not counted): {softOutliers.Count}");
    foreach(var line in softOutliers.Distinct().Take(40))output.AppendLine("      "+line);
}
else output.AppendLine($"== MARKET: {marketPath} not found");
Console.Write(output.ToString());
return 0;

sealed class Fact
{
    public int Kind { get; set; }
    public uint Subject { get; set; }
    public uint Detail { get; set; }
    public uint Territory { get; set; }
    public uint Map { get; set; }
    public int CellX { get; set; }
    public int CellZ { get; set; }
    public float X { get; set; }
    public float Z { get; set; }
    public int LevelMin { get; set; }
    public int LevelMax { get; set; }
    public int Seen { get; set; }
    public int Imported { get; set; }
    public DateTimeOffset First { get; set; }
    public DateTimeOffset Last { get; set; }
    [System.Text.Json.Serialization.JsonIgnore] public int Index { get; set; }
}
sealed class FactFile
{
    public int Version { get; set; }
    public string? Producer { get; set; }
    public DateTimeOffset Written { get; set; }
    public List<Fact> Facts { get; set; }=[];
}
sealed class KindReport
{
    public int Checked;public int Ok;
    public List<(int Index,string Description,List<string> Problems)> Anomalies { get; }=[];
    public List<(int Index,string Description,List<string> Notes)> Notes { get; }=[];
}
sealed record Listing(decimal Price,bool Hq,int Quantity,bool Own=false);
sealed record Sale(decimal Price,bool Hq,int Quantity,DateTimeOffset At);
sealed class MarketEntry
{
    public uint World { get; set; }
    public uint Item { get; set; }
    public List<Listing> Listings { get; set; }=[];
    public DateTimeOffset ListingsAt { get; set; }
    public bool OwnKnown { get; set; }
    public List<Sale> Sales { get; set; }=[];
    public DateTimeOffset SalesAt { get; set; }
}
