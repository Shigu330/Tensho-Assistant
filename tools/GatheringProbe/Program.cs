using Lumina;
using Lumina.Data;
using Lumina.Excel.Sheets;
using TenshoAssistant.Gathering;
using TenshoAssistant.Craft;
using TenshoAssistant.Inventory;
using TenshoAssistant.Maps;
using TenshoAssistant.Items;
using TenshoAssistant.Market;
using TenshoAssistant.MiniGames;

if (args.Length is < 1 or > 2) throw new ArgumentException("Supply sqpack directory, optionally --duty-loot=<finder-id>, --monster-loot=<item-id> or --fish-baits=<item-id> for explicit network consultation.");
if(args.Length==2 && !new[]{"--duty-loot=","--monster-loot=","--fish-baits="}.Any(option=>args[1].StartsWith(option,StringComparison.Ordinal)))throw new ArgumentException("Unknown consultation option.");
using var data = new GameData(args[0], new LuminaOptions { DefaultExcelLanguage = Language.French });
var points = data.Excel.GetSheet<GatheringPoint>();
var items = data.Excel.GetSheet<GatheringItem>();
var fishing = data.Excel.GetSheet<FishingSpot>();
var transientPoints = data.Excel.GetSheet<GatheringPointTransient>();
var rareTimes = data.Excel.GetSheet<GatheringRarePopTimeTable>();
GatheringSchedule ReadSchedule(GatheringPoint point)
{
    if (point.Type == 8 || !transientPoints.TryGetRow(point.RowId, out var timing)) return GatheringSchedule.Unknown;
    if (timing.GatheringRarePopTimeTable.RowId == 0)
        return GatheringSchedule.FromEphemeral(timing.EphemeralStartTime, timing.EphemeralEndTime);
    if (!rareTimes.TryGetRow(timing.GatheringRarePopTimeTable.RowId, out var rare)) return GatheringSchedule.Unknown;
    var slots = new List<(ushort, ushort)>();
    for (var index = 0; index < rare.StartTime.Count; index++) slots.Add((rare.StartTime[index], rare.Duration[index]));
    return GatheringSchedule.FromRare(slots);
}
var count = 0;
var fishingCount = 0;
var examples = new HashSet<uint>();
var managed = new List<GatheringSource>();
void Add(GatheringPoint point, uint id, bool hidden = false)
{
    if (point.GatheringPointBase.RowId == 0 || point.TerritoryType.RowId == 0) return;
    var basis = point.GatheringPointBase.Value;
    var type = basis.GatheringType.Value;
    var territory = point.TerritoryType.Value;
    managed.Add(new(id, point.RowId, basis.RowId, type.RowId, type.Name.ExtractText(),
        (uint)Math.Max(0, type.IconMain), basis.GatheringLevel, territory.RowId,
        territory.PlaceName.Value.Name.ExtractText(), point.PlaceName.Value.Name.ExtractText(), territory.Map.RowId, false, hidden, ReadSchedule(point)));
}
var links = data.Excel.GetSubrowSheet<GatheringItemPoint>();
foreach (var item in items)
{
    if (item.Item.RowId == 0 || item.Item.RowType != typeof(Item)) continue;
    if (links.TryGetRow(item.RowId, out var nodeLinks))
        foreach (var link in nodeLinks)
            if (link.GatheringPoint.RowId != 0) Add(link.GatheringPoint.Value, item.Item.RowId, item.IsHidden);
}
foreach (var point in points)
{
    if (point.GatheringPointBase.RowId == 0 || point.TerritoryType.RowId == 0) continue;
    var basis = point.GatheringPointBase.Value;
    foreach (var reference in basis.Item)
    {
        if (reference.RowId == 0 || reference.RowType != typeof(GatheringItem)) continue;
        var gatheringItem = items.GetRow(reference.RowId);
        var item = gatheringItem.Item;
        if (item.RowId == 0 || item.RowType != typeof(Item)) continue;
        count++;
        Add(point, item.RowId, gatheringItem.IsHidden);
        if (item.RowId is 12893 or 12891 or 8 && examples.Add(item.RowId))
            Console.WriteLine($"Item {item.RowId}: {basis.GatheringType.Value.Name.ExtractText()} / {point.TerritoryType.Value.PlaceName.Value.Name.ExtractText()} / {point.PlaceName.Value.Name.ExtractText()}");
    }
}
foreach (var spot in fishing)
    if (spot.TerritoryType.RowId != 0)
    {
        fishingCount += spot.Item.Count(item => item.RowId != 0);
        foreach (var item in spot.Item)
            if (item.RowId != 0)
                managed.Add(new(item.RowId, spot.RowId, spot.RowId, 100, "Pêche", 62118, spot.GatheringLevel,
                    spot.TerritoryType.RowId, spot.TerritoryType.Value.PlaceName.Value.Name.ExtractText(),
                    spot.PlaceName.Value.Name.ExtractText(), spot.TerritoryType.Value.Map.RowId, true));
    }
var catalog = new GatheringCatalog(managed, "installed-game");
var openingFixture=GatheringTime.Next(ReadSchedule(points.GetRow(30999)),DateTimeOffset.UnixEpoch,1).Openings.Single();
if(openingFixture.StartsAt!=DateTimeOffset.UnixEpoch.AddSeconds(1575) || openingFixture.EndsAt!=DateTimeOffset.UnixEpoch.AddSeconds(2100))
    throw new InvalidOperationException("Native 30999 opening conversion failed.");
Console.WriteLine("PASS native point30999 schedule09-12 ET converts to epoch+1575 through+2100 seconds UTC");
Console.WriteLine($"Deduplicated sources: {catalog.Count}");
var chestnut = catalog.ForItem(12893).FirstOrDefault() ?? throw new InvalidOperationException("Missing hidden chestnut source.");
Console.WriteLine($"Marron sombre: {chestnut.TypeName}, level {chestnut.Level}, {chestnut.Zone}, {chestnut.Place}, hidden={chestnut.IsHidden}");
Console.WriteLine($"Gathering links: {count}; fishing links: {fishingCount}; points: {points.Count}");
if (count == 0 || fishingCount == 0) throw new InvalidOperationException("Real data join produced an empty domain.");
var blue = data.Excel.GetSheet<AozAction>();
var blueDetails = data.Excel.GetSheet<AozActionTransient>();
foreach (var spell in blue.Where(row => row.RowId != 0 && row.Action.RowId != 0).Take(5))
{
    var details = blueDetails.GetRow(spell.RowId);
    Console.WriteLine($"Blue {spell.RowId}: {spell.Action.Value.Name.ExtractText()}, number={details.Number}, rank={spell.Rank}, location={details.Location.RowType?.Name}:{details.Location.RowId}");
}
Console.WriteLine($"Static catalog rows: mounts={data.Excel.GetSheet<Mount>().Count}, companions={data.Excel.GetSheet<Companion>().Count}, emotes={data.Excel.GetSheet<Emote>().Count}, quests={data.Excel.GetSheet<Quest>().Count}");
var recipeRows = new List<RecipeDefinition>();
foreach (var row in data.Excel.GetSheet<Recipe>())
{
    if (row.RowId == 0 || row.ItemResult.RowId == 0 || row.AmountResult == 0) continue;
    var ingredients = new List<RecipeIngredient>();
    for (var index = 0; index < row.Ingredient.Count; index++)
        if (row.Ingredient[index].RowId != 0 && row.AmountIngredient[index] > 0)
            ingredients.Add(new(row.Ingredient[index].RowId, row.AmountIngredient[index]));
    recipeRows.Add(new(row.RowId, row.ItemResult.RowId, row.AmountResult, row.CraftType.Value.Name.ExtractText(),
        row.RecipeLevelTable.Value.ClassJobLevel, ingredients, row.CraftType.RowId));
}
var recipes = RecipeCatalog.Build(recipeRows, "installed-game");
var itemSheet = data.Excel.GetSheet<Item>();
foreach (var candidate in itemSheet.Where(row => row.Name.ExtractText().Contains("bouquetin",StringComparison.OrdinalIgnoreCase)
    && row.Name.ExtractText().StartsWith("Peau",StringComparison.OrdinalIgnoreCase)).Take(4))
    Console.WriteLine($"Monster material candidate: {candidate.RowId} {candidate.Name.ExtractText()}");
foreach (var recipe in recipes.ForResult(12892))
{
    var plan = CraftChain.Build(recipe, 7, recipes, _ => new([], 0, 0, false, true, false, false));
    Console.WriteLine($"Real craft chain {itemSheet.GetRow(recipe.ResultItemId).Name.ExtractText()}: {plan.Requested} requested, {plan.Produced} produced, {plan.Steps.Count} steps, unresolved={plan.HasUnresolved}");
    foreach (var material in plan.Materials)
        Console.WriteLine($"  {itemSheet.GetRow(material.ItemId).Name.ExtractText()}: {material.Amount}, {material.Reason}");
    if (plan.Produced != 9 || plan.Materials.Single(material => material.ItemId == 12891).Amount != 9)
        throw new InvalidOperationException("Real birch syrup yield or ingredient join does not match expected quantities.");
}
if (recipes.ForResult(12892).Count == 0) throw new InvalidOperationException("Real birch syrup recipe missing.");
var costRecipe = recipes.ForResult(12892).First();
var costChain = CraftChain.Build(costRecipe,7,recipes,id=>new([],id==12891 ? 3 : 0,0,true,false,false,true));
var costNow = DateTimeOffset.UtcNow;
var fixtureCost = PreparationCost.FromChain(costChain,71,PreparationPriceQuality.Nq,id=>
{
    var row = itemSheet.GetRow(id);
    return new ItemDefinition(id,row.Name.ExtractText(),row.CanBeHq,row.Icon,IsMarketable:!row.IsUntradable && row.ItemSearchCategory.RowId!=0);
},id=>new(71,id,costNow,costNow,new(id==12891 ? 10 : 1,null,null),new(null,null,null)),costNow);
if (costChain.Materials.Single(material=>material.ItemId==12891).Amount!=6
    || fixtureCost.Lines.Single(line=>line.ItemId==12891).Quantity!=6 || fixtureCost.KnownSubtotal!=84 || !fixtureCost.AllCosted)
    throw new InvalidOperationException("Real recipe terminal cost double-counted stock or lost material quantities.");
Console.WriteLine("PASS native birch syrup recipe with SIMULATED stock/prices: 6 sap + 24 fire shards, indicative fixture cost 84");
var maps = new MapCatalog(data.Excel.GetSheet<Map>().Where(row => row.RowId != 0 && row.TerritoryType.RowId != 0)
    .Select(row => new MapDefinition(row.RowId, row.TerritoryType.RowId, row.PlaceNameRegion.Value.Name.ExtractText(),
        row.PlaceName.Value.Name.ExtractText(), row.PlaceNameSub.Value.Name.ExtractText(), row.MapIndex, row.SizeFactor,row.PlaceName.RowId)));
var sampleSource = catalog.ForItem(8).First(source => source.MapId != 0);
var sampleMap = maps.Find(sampleSource.MapId) ?? throw new InvalidOperationException("Gathering source references a missing map.");
if (sampleMap.TerritoryId != sampleSource.TerritoryId) throw new InvalidOperationException("Map and gathering territory mismatch.");
Console.WriteLine($"Native map catalog: {maps.Entries.Count} maps; sample {sampleMap.Zone}, map={sampleMap.MapId}, territory={sampleMap.TerritoryId}");
var hoverExamples = new uint[] { 12892, 4551, 1 }.Select(id => itemSheet.GetRow(id)).Select(row =>
    new ItemDefinition(row.RowId, row.Name.ExtractText(), row.CanBeHq, row.Icon,
        row.ItemUICategory.RowId, row.ItemUICategory.Value.Name.ExtractText(), !row.IsUntradable && row.ItemSearchCategory.RowId != 0)).ToArray();
var hoverCatalog = ItemCatalog.Build(hoverExamples, new("installed-game", "French", "local-probe", 1));
foreach (var example in hoverExamples)
    Console.WriteLine($"Hover eligibility {example.Id} {example.Name}: marketable={example.IsMarketable}, HQ={example.CanBeHq}");
if (MarketHoverTracker.Normalize(1012892, hoverCatalog) != new MarketHoverItem(12892, true)
    || MarketHoverTracker.Normalize(1, hoverCatalog) is not null || MarketHoverTracker.Normalize(2012892, hoverCatalog) is not null)
    throw new InvalidOperationException("Native market eligibility or HQ normalization did not match the copied game data.");
var bases = data.Excel.GetSheet<GatheringPointBase>();
Console.WriteLine($"Schedule sheet counts: points={points.Count}, bases={bases.Count}, transient={transientPoints.Count}, rare={rareTimes.Count}");
foreach (var timing in transientPoints.Where(row => row.GatheringRarePopTimeTable.RowId != 0 || row.EphemeralStartTime != row.EphemeralEndTime).Take(8))
{
    var pointFound = points.TryGetRow(timing.RowId, out var nativePoint);
    var baseFound = bases.TryGetRow(timing.RowId, out _);
    var rare = timing.GatheringRarePopTimeTable.Value;
    Console.WriteLine($"Schedule sample key={timing.RowId}, point={pointFound}, baseSameId={baseFound}, nativeBase={(pointFound ? nativePoint.GatheringPointBase.RowId : 0)}, rare={rare.RowId}, starts={string.Join(',', rare.StartTime)}, durations={string.Join(',', rare.Duration)}, ephemeral={timing.EphemeralStartTime}-{timing.EphemeralEndTime}");
}
Console.WriteLine($"Native rare durations: {string.Join(',', rareTimes.SelectMany(row => row.Duration).Distinct().Order())}");
Console.WriteLine($"Ephemeral examples: {string.Join(';', transientPoints.Where(row => row.EphemeralStartTime != row.EphemeralEndTime).Take(8).Select(row => $"{row.RowId}:{row.EphemeralStartTime}-{row.EphemeralEndTime}"))}");
if (ReadSchedule(points.GetRow(30999)).Windows.Single() != new GatheringWindow(540, 720)
    || ReadSchedule(points.GetRow(31436)).Windows.Single() != new GatheringWindow(1200, 1440))
    throw new InvalidOperationException("Real rare or ephemeral schedule mismatch.");
foreach (var group in catalog.Sources.GroupBy(source => source.Timing.Kind))
    Console.WriteLine($"Interpreted schedule {group.Key}: {group.Count()}");
if (catalog.Sources.Any(source => source.Timing.Kind == GatheringScheduleKind.Invalid))
    throw new InvalidOperationException("Unexpected native schedule encoding: inspect before release.");
Console.WriteLine("PASS installed-game schedule joins and minute conversion");
var triadCards = data.Excel.GetSheet<TripleTriadCard>();
var triadResidents = data.Excel.GetSheet<TripleTriadCardResident>();
Console.WriteLine($"Triad sheets: names={triadCards.Count}, resident={triadResidents.Count}");
foreach (var card in triadCards.Where(row => row.RowId != 0 && !string.IsNullOrWhiteSpace(row.Name.ExtractText())).Take(3))
{
    var resident = triadResidents.GetRow(card.RowId);
    Console.WriteLine($"Triad {card.RowId}: {card.Name.ExtractText()}, order={resident.Order}, stars={resident.TripleTriadCardRarity.Value.Stars}, top={resident.Top}, right={resident.Right}, bottom={resident.Bottom}, left={resident.Left}, type={resident.TripleTriadCardType.Value.Name.ExtractText()}");
}
foreach (var item in itemSheet.Where(row => row.AdditionalData.RowType == typeof(TripleTriadCard)).Take(3))
    Console.WriteLine($"Triad item {item.RowId} {item.Name.ExtractText()}, card={item.AdditionalData.RowId}, icon={item.Icon}");
Console.WriteLine($"Triad icon 88001 exists={data.FileExists("ui/icon/088000/088001.tex")}");
var triadValid = triadCards.Where(row => row.RowId != 0 && !string.IsNullOrWhiteSpace(row.Name.ExtractText()))
    .Select(row => triadResidents.GetRow(row.RowId)).Where(row => row.Top > 0).ToArray();
var missingTriadIcons = triadValid.Where(row => !data.FileExists($"ui/icon/{(88000 + row.RowId) / 1000 * 1000:000000}/{88000 + row.RowId:000000}.tex")).ToArray();
Console.WriteLine($"Triad valid cards={triadValid.Length}, missing icons={missingTriadIcons.Length}, invalid values={triadValid.Count(row => row.Top > 10 || row.Bottom is < 1 or > 10 || row.Right is < 1 or > 10 || row.Left is < 1 or > 10)}, stars={string.Join(',', triadValid.Select(row => row.TripleTriadCardRarity.Value.Stars).Distinct().Order())}");
var managedTriad = triadValid.Select(row => new TriadCardDefinition(row.RowId, row.Order,
    row.TripleTriadCardRarity.Value.Stars, row.Top, row.Right, row.Bottom, row.Left,
    row.TripleTriadCardType.RowId, row.TripleTriadCardType.Value.Name.ExtractText(), triadCards.GetRow(row.RowId).Description.ExtractText())).ToArray();
var triadItems = ItemCatalog.Build(itemSheet.Where(row => row.AdditionalData.RowType == typeof(TripleTriadCard))
    .Select(row => new ItemDefinition(row.RowId, row.Name.ExtractText(), row.CanBeHq, row.Icon,
        TripleTriadCardId: row.AdditionalData.RowId)), new("Item/AdditionalData", "French", "installed-game", 1));
var nativeDodo = managedTriad.Single(card => card.Id == 1);
if (managedTriad.Length != 475 || managedTriad.Any(card => !card.IsValid) || missingTriadIcons.Length != 0
    || nativeDodo.Top != 4 || nativeDodo.Right != 2 || nativeDodo.Bottom != 3 || nativeDodo.Left != 4
    || triadItems.ForTriadCard(1).Single().Id != 9772 || !managedTriad.Any(card => card.Id != card.Number))
    throw new InvalidOperationException("Native Triad catalog, artwork, orientation or typed item join mismatch.");
Console.WriteLine($"PASS installed-game Triad values, 475 icons, native/display identities and item join; {triadItems.Count} card items");
var triadMatch = TriadPosition.Create(managedTriad.Where(card => card.Id <= 5),
    managedTriad.Where(card => card.Id is >= 6 and <= 10), TriadPlayer.Blue);
var triadOpening = TripleTriadSolver.Solve(triadMatch);
Console.WriteLine($"Native Triad opening: completedDepth={triadOpening.CompletedDepth}, exact={triadOpening.Exact}, nodes={triadOpening.Nodes}, moves={triadOpening.Moves.Count}");
for (var step = 0; step < 6; step++) triadMatch = triadMatch.Play(triadMatch.LegalMoves()[0]);
var triadEndgame = TripleTriadSolver.Solve(triadMatch);
if (!triadEndgame.Exact || triadEndgame.CompletedDepth != 3 || triadEndgame.Moves.Count != 6
    || triadEndgame.Best(triadMatch.Turn).Count == 0 || triadMatch.BlueScore + triadMatch.RedScore != 10)
    throw new InvalidOperationException("Native Triad endgame integration failed.");
Console.WriteLine("PASS native-card Triad simulation and exact three-ply advice");
var typedTriad = managedTriad.Where(card => card.TypeId != 0).GroupBy(card => card.TypeId)
    .OrderByDescending(group => group.Count()).First().Take(10).ToArray();
if (typedTriad.Length != 10) throw new InvalidOperationException("Need ten native cards of a shared family for rule integration.");
var triadRuleCases = 0;
for (var family = 0; family < 3; family++)
for (var ordered = 0; ordered < 2; ordered++)
for (var mask = 0; mask < 16; mask++)
{
    var rules = (TriadRules)mask | (ordered == 1 ? TriadRules.Order : 0)
        | (family == 1 ? TriadRules.Ascension : family == 2 ? TriadRules.Descension : 0);
    var match = TriadPosition.Create(typedTriad.Take(5),typedTriad.Skip(5),TriadPlayer.Blue,rules);
    for (var step = 0; step < 6; step++) match = match.Play(match.LegalMoves()[0]);
    var advice = TripleTriadSolver.Solve(match);
    if (!advice.Exact || advice.CompletedDepth != 3 || advice.Moves.Count != (ordered == 1 ? 3 : 6) || match.Rules != rules)
        throw new InvalidOperationException("Native Triad rule-aware integration failed.");
    var modifier = family == 1 ? 6 : family == 2 ? -6 : 0;
    for (var index = 0; index < 10; index++)
        if (match.FamilyModifier(index) != modifier || match.EffectiveSide(index,0) != Math.Clamp(typedTriad[index].Top + modifier,1,10))
            throw new InvalidOperationException("Native Triad family display rank mismatch.");
    triadRuleCases++;
}
Console.WriteLine($"PASS native-card Triad exact endgames and effective ranks: {triadRuleCases} rule combinations, family={typedTriad[0].TypeId}");

var questRows = data.Excel.GetSheet<Quest>().Where(q => q.RowId != 0 && !string.IsNullOrWhiteSpace(q.Name.ExtractText())).ToArray();
var questEntries = questRows.Select(q => new TenshoAssistant.Quests.QuestEntry(q.RowId,q.Name.ExtractText(),"",q.Icon,TenshoAssistant.Quests.QuestProgress.Unconfirmed,null)
{
    PreviousQuestIds = Array.AsReadOnly(q.PreviousQuest.Select(p => p.RowId).Where(id => id != 0).Distinct().ToArray()),
    PreviousQuestJoin = q.PreviousQuestJoin, PreviousQuestStep = q.Unknown7, IsRepeatable = q.IsRepeatable
}).ToArray();
var questLinks = new TenshoAssistant.Quests.QuestLinks(questEntries);
var questEdges = 0;
foreach (var quest in questEntries)
foreach (var previous in quest.PreviousQuestIds)
{
    if (!questLinks.Following(previous).Any(next => next.Id == quest.Id)) throw new InvalidOperationException("Quest reverse link lost.");
    questEdges++;
}
if (questEntries.Length < 1000 || questEdges < 1000 || questEntries.Any(q => questLinks.CompletionEvidence(q)))
    throw new InvalidOperationException("Quest catalog or unconfirmed progression mismatch.");
Console.WriteLine($"PASS native quest direct links: {questEntries.Length} named rows, {questEdges} edges; joins {string.Join(", ",questRows.GroupBy(q => q.PreviousQuestJoin).Select(g => $"{g.Key}={g.Count()}"))}; raw step nonzero={questRows.Count(q => q.Unknown7 != 0)}");
var questLocations = questRows.Where(q => q.IssuerLocation.RowId != 0).Select(q => q.IssuerLocation.Value)
    .Where(level => level.Map.RowId != 0 && level.Territory.RowId != 0).ToArray();
var questMapMatches = questLocations.Count(level => level.Map.Value.TerritoryType.RowId == level.Territory.RowId);
if (questMapMatches < 1000) throw new InvalidOperationException("Quest issuer map/territory identities not supported by native rows.");
Console.WriteLine($"PASS native quest issuer locations: {questMapMatches}/{questLocations.Length} map/territory matches; {questRows.Count(q => q.ClassJobLevel.FirstOrDefault() != 0)} level rows. Mismatching locations remain unavailable in UI.");

// Duty identity evidence only: this probe does not infer chest items from unknown fields.
var nativeInstances = data.Excel.GetSheet<InstanceContent>();
var sampleInstance = nativeInstances.GetRow(4);
var sampleDuty = sampleInstance.ContentFinderCondition.Value;
if (sampleDuty.Content.RowId != sampleInstance.RowId || sampleDuty.TerritoryType.RowId == 0 || string.IsNullOrWhiteSpace(sampleDuty.Name.ExtractText()))
    throw new InvalidOperationException("Native Sastasha instance / duty / territory mapping mismatch.");
Console.WriteLine($"PASS native duty identity: instance={sampleInstance.RowId}, duty={sampleDuty.RowId}, territory={sampleDuty.TerritoryType.RowId}, linkType={sampleDuty.ContentLinkType}, name={sampleDuty.Name.ExtractText()}. No native loot list inferred.");
var nativeDutyEntries = nativeInstances.Where(row => row.RowId != 0 && row.ContentFinderCondition.RowId != 0)
    .Where(row => row.ContentFinderCondition.Value.ContentLinkType == 1 && row.ContentFinderCondition.Value.Content.RowId == row.RowId)
    .Select(row =>
    {
        var finder = row.ContentFinderCondition.Value;
        return new TenshoAssistant.Duties.DutyDefinition(finder.RowId,row.RowId,finder.TerritoryType.RowId,
            finder.Name.ExtractText(),finder.ContentType.Value.Name.ExtractText(),finder.ClassJobLevelRequired,finder.ContentType.Value.Icon);
    }).ToArray();
var nativeDutyCatalog = new TenshoAssistant.Duties.DutyCatalog(nativeDutyEntries);
if (nativeDutyCatalog.Find(sampleDuty.RowId)?.InstanceId != sampleInstance.RowId || nativeDutyCatalog.Entries.Count < 100)
    throw new InvalidOperationException("Native duty catalog identity mismatch.");
Console.WriteLine($"PASS native duty catalog: {nativeDutyCatalog.Entries.Count} named reciprocal regular-instance links; {nativeDutyCatalog.Entries.Count(d => d.DutyId != d.InstanceId)} finder/instance IDs differ.");
if (args.Length == 2 && args[1].StartsWith("--duty-loot=",StringComparison.Ordinal))
{
    const string option = "--duty-loot=";
    var distinctFinder = args[1] == option+"first-distinct";
    uint finderId = 0;
    if (!distinctFinder && (!args[1].StartsWith(option,StringComparison.Ordinal) || !uint.TryParse(args[1][option.Length..],out finderId)))
        throw new ArgumentException("Expected --duty-loot=<finder-id>.");
    if (distinctFinder) finderId = nativeDutyCatalog.Entries.OrderBy(d => d.DutyId).First(d => d.DutyId != d.InstanceId).DutyId;
    var duty = nativeDutyCatalog.Find(finderId) ?? throw new ArgumentException("Duty not in the validated native catalog.");
    using var lootService = new TenshoAssistant.Duties.DutyLootService();
    lootService.Select(duty);
    await lootService.RequestAsync();
    var loot = lootService.State.Document ?? throw new InvalidOperationException(lootService.State.Error ?? "No duty loot document.");
    if (loot.InstanceId != duty.InstanceId || loot.IsExhaustive) throw new InvalidOperationException("Duty loot scope or coverage mismatch.");
    var known = data.Excel.GetSheet<Item>();
    Console.WriteLine($"PASS explicit provider consultation: {duty.Name}, finder={duty.DutyId}, instance={loot.InstanceId}, {loot.Entries.Count} source/item entries, {loot.Entries.Select(e => e.ItemId).Distinct().Count()} distinct items, {loot.Entries.Count(e => !known.TryGetRow(e.ItemId,out _))} unresolved references; exhaustive={loot.IsExhaustive}, unparsedFightDrops={loot.HasUnparsedFightDrops}.");
}
if (args.Length == 2 && args[1].StartsWith("--monster-loot=",StringComparison.Ordinal))
{
    if (!uint.TryParse(args[1]["--monster-loot=".Length..],out var itemId) || !itemSheet.TryGetRow(itemId,out var material))
        throw new ArgumentException("Expected --monster-loot=<native-item-id>.");
    using var shared = new TenshoAssistant.Community.GarlandClient();
    using var monsterService = new TenshoAssistant.Monsters.MonsterLootService(shared);
    monsterService.SelectItem(itemId); await monsterService.RequestItemAsync();
    var list = monsterService.State.Item ?? throw new InvalidOperationException(monsterService.State.Error ?? "No monster item document.");
    var reference = list.Monsters.FirstOrDefault() ?? throw new InvalidOperationException("No monster references for the selected material.");
    monsterService.SelectMonster(reference.Id);
    await Task.Delay(TenshoAssistant.Community.GarlandClient.RequestInterval);
    await monsterService.RequestMonsterAsync();
    var detail = monsterService.State.Monster ?? throw new InvalidOperationException(monsterService.State.Error ?? "No monster detail.");
    var nameId = detail.Id % 10000000000UL; var baseId = detail.Id / 10000000000UL;
    if (nameId > uint.MaxValue || baseId > uint.MaxValue || !data.Excel.GetSheet<BNpcName>().TryGetRow((uint)nameId,out var nativeName)
        || !data.Excel.GetSheet<BNpcBase>().TryGetRow((uint)baseId,out _)) throw new InvalidOperationException("Composite monster native keys unresolved.");
    if (!string.Equals(nativeName.Singular.ExtractText(),detail.Name,StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Monster French name does not agree with the native name key.");
    var locationMaps = maps.ForPlace(detail.PlaceNameId);
    if (detail.PlaceNameId != 0 && locationMaps.Count == 0) throw new InvalidOperationException("Monster PlaceName did not resolve to native maps.");
    Console.WriteLine($"PASS explicit monster consultation: Item {itemId} {material.Name.ExtractText()}, {list.Monsters.Count} monster links; detail {detail.Id} {detail.Name}, base={baseId}, name={nameId}, PlaceName={detail.PlaceNameId}, {locationMaps.Count} native maps, drops={detail.DropItemIds.Count}, reverseItemLink={detail.DropItemIds.Contains(itemId)}; coordinates indicative only.");
}
if(args.Length==2 && args[1].StartsWith("--fish-baits=",StringComparison.Ordinal))
{
    if(!uint.TryParse(args[1]["--fish-baits=".Length..],out var id) || !itemSheet.TryGetRow(id,out var fishItem))throw new ArgumentException("Expected native fish Item ID.");
    using var shared=new TenshoAssistant.Community.GarlandClient();using var service=new TenshoAssistant.Monsters.MonsterLootService(shared);
    service.SelectItem(id);await service.RequestItemAsync();
    var document=service.State.Item ?? throw new InvalidOperationException(service.State.Error ?? "Missing item response.");
    var fish=document.Fishing ?? throw new InvalidOperationException(document.FishingError ?? "Missing fishing data.");
    var references=fish.Spots.SelectMany(spot=>spot.BaitChains ?? []).SelectMany(chain=>chain).Distinct().ToArray();
    var knownSpots=fish.Spots.Count(spot=>spot.SpotId is { } spotId && fishing.HasRow(spotId));
    if(!fish.SpotsKnown || fish.Spots.Count==0 || references.Length==0 || references.Any(item=>!itemSheet.HasRow(item)))throw new InvalidOperationException("Missing routes or unresolved bait Item IDs.");
    if(fish.Spots.Any(spot=>spot.Conditions is null || spot.Conditions.Error is not null))throw new InvalidOperationException("Fishing condition projection failed.");
    var englishWeather=data.Excel.GetSheet<Weather>(Language.English);
    var nativeWeather=data.Excel.GetSheet<Weather>().Where(row=>row.RowId!=0)
        .Select(row=>TenshoAssistant.Gathering.WeatherCopy.FromRows(row,englishWeather.TryGetRow(row.RowId,out var english) ? english:null)).ToArray();
    var fishTerritories=fish.Spots.Where(spot=>spot.SpotId is { } place && fishing.HasRow(place))
        .Select(spot=>fishing.GetRow(spot.SpotId!.Value).TerritoryType.Value).DistinctBy(row=>row.RowId).ToArray();
    var zoneRates=fishTerritories.Select(row=>
    {
        var rate=row.WeatherRate.Value;
        return new TenshoAssistant.Gathering.ZoneWeather(row.RowId,row.WeatherRate.RowId,
            rate.Rate.Select((chance,index)=>new TenshoAssistant.Gathering.WeatherShare(rate.Weather[index].RowId,chance)).ToArray(),row.IndividualWeather.RowId!=0);
    }).ToArray();
    var nativeWeatherCatalog=new TenshoAssistant.Gathering.WeatherCatalog(nativeWeather,zoneRates,"installed-game");
    foreach(var spot in fish.Spots.Where(spot=>spot.Conditions!.Timing.Kind!=TenshoAssistant.Gathering.GatheringScheduleKind.Unknown
        || spot.Conditions.Weather is not null || spot.Conditions.PreviousWeather is not null))
    {
        var conditions=spot.Conditions!;
        Console.WriteLine($"CONDITIONS spot={spot.SpotId}: {conditions.Timing.Label}; current={string.Join('/',conditions.Weather ?? [])}; prior={string.Join('/',conditions.PreviousWeather ?? [])}");
        if(spot.SpotId is { } place && fishing.TryGetRow(place,out var sourceSpot))
        foreach(var name in (conditions.Weather ?? []).Concat(conditions.PreviousWeather ?? []).Distinct())
        {
            var matched=nativeWeatherCatalog.WeatherBySourceName(name,sourceSpot.TerritoryType.RowId)
                ?? throw new InvalidOperationException($"Community weather {name} unresolved for territory {sourceSpot.TerritoryType.RowId}.");
            Console.WriteLine($"MATCH source weather {name} -> {matched.Id} {matched.Name}, icon={matched.Icon}, territory={sourceSpot.TerritoryType.RowId}");
        }
        if(conditions.Timing.Kind==TenshoAssistant.Gathering.GatheringScheduleKind.Limited)
        {
            var fixture=TenshoAssistant.Gathering.GatheringTime.Next(conditions.Timing,DateTimeOffset.UnixEpoch).Openings[0];
            Console.WriteLine($"FIXTURE ET-only UTC window since Unix epoch: {fixture.StartsAt:O} – {fixture.EndsAt:O}; not fish availability");
        }
        if(spot.SpotId is { } forecastSpot && fishing.TryGetRow(forecastSpot,out var forecastNative))
        {
            var fixtureTime=DateTimeOffset.Parse("2026-10-29T12:00:00Z",System.Globalization.CultureInfo.InvariantCulture);
            var projection=TenshoAssistant.Gathering.FishingForecast.Calculate(conditions,nativeWeatherCatalog,forecastNative.TerritoryType.RowId,fixtureTime);
            if(projection.Error is not null)throw new InvalidOperationException(projection.Error);
            Console.WriteLine($"FORECAST FIXTURE {fixtureTime:O}, reported={projection.ReportedFields}, compatible={projection.Spans.Count(span=>span.ReportedConditionsMatch==true)}, unknown={projection.Spans.Count(span=>span.ReportedConditionsMatch is null)}, horizon={projection.HorizonEndsAt:O}; not fish availability");
            foreach(var span in projection.Spans.Where(span=>span.ReportedConditionsMatch==true).Take(3))Console.WriteLine($"COMPATIBLE {span.StartsAt:O} – {span.EndsAt:O}");
        }
    }
    Console.WriteLine($"PASS explicit fish consultation: {id} {fishItem.Name.ExtractText()}, {fish.Spots.Count} source variants, {knownSpots} native FishingSpot links, {references.Length} distinct bait/intermediate Item IDs, fetched {document.FetchedAt:O}");
}
