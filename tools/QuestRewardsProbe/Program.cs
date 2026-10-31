using Lumina;
using Lumina.Data;
using Lumina.Excel.Sheets;
using TenshoAssistant.Quests;

if(args.Length!=1)throw new ArgumentException("Supply installed sqpack directory; no network or game state.");
using var data=new GameData(args[0],new LuminaOptions{DefaultExcelLanguage=Language.French});
var quests=data.Excel.GetSheet<Quest>().Where(row=>row.RowId!=0 && !string.IsNullOrWhiteSpace(row.Name.ExtractText())).ToArray();
Console.WriteLine($"Named quests={quests.Length}");
foreach(var group in quests.GroupBy(row=>row.ItemRewardType))
{
    Console.WriteLine($"MODE {group.Key}: {group.Count()} quests; fixed row types={string.Join(',',group.SelectMany(row=>row.Reward).Where(row=>row.RowId!=0).Select(row=>row.RowType?.Name ?? "unknown").Distinct())}");
    foreach(var row in group.Take(2))
    {
        Console.WriteLine($"SAMPLE {row.RowId} {row.Name.ExtractText()}; gil={row.GilReward}; currency={row.CurrencyReward.RowId}x{row.CurrencyRewardCount}; tome={row.TomestoneReward}x{row.TomestoneCountReward}; shape={row.Reward.Count}/{row.ItemCountReward.Count}/{row.OptionalItemReward.Count}/{row.OptionalItemCountReward.Count}/{row.OptionalItemIsHQReward.Count}");
        Console.WriteLine(" FIXED "+string.Join(';',row.Reward.Select((reward,index)=>$"{reward.RowType?.Name}:{reward.RowId}x{row.ItemCountReward[index]}")));
        Console.WriteLine(" CHOICE "+string.Join(';',row.OptionalItemReward.Select((reward,index)=>$"{reward.RowId}x{row.OptionalItemCountReward[index]} HQ={row.OptionalItemIsHQReward[index]}")));
    }
}
Console.WriteLine($"Currencies={quests.Count(row=>row.CurrencyRewardCount>0)}, tomestones={quests.Count(row=>row.TomestoneCountReward>0)}, catalysts={quests.Count(row=>row.ItemCatalyst.Any(item=>item.RowId!=0))}");
foreach(var row in quests.Where(row=>row.CurrencyRewardCount>0 && row.CurrencyReward.RowId==0))Console.WriteLine($"MISSING CURRENCY quest={row.RowId} {row.Name.ExtractText()}, GCTypeReward={row.GCTypeReward}, requirementGC={row.GrandCompany.RowId}, amount={row.CurrencyRewardCount}, mode={row.ItemRewardType}");
foreach(var row in quests.Where(row=>row.CurrencyRewardCount>0 && row.CurrencyReward.RowId!=0).Take(4))Console.WriteLine($"KNOWN CURRENCY quest={row.RowId} {row.Name.ExtractText()}, GCTypeReward={row.GCTypeReward}, Item={row.CurrencyReward.RowId}, amount={row.CurrencyRewardCount}");
foreach(var item in quests.Where(row=>row.CurrencyReward.RowId!=0).Select(row=>row.CurrencyReward.Value).DistinctBy(row=>row.RowId))Console.WriteLine($"CURRENCY ITEM {item.RowId} {item.Name.ExtractText()}, itemGC={item.GrandCompany.RowId}, UI={item.ItemUICategory.RowId} {item.ItemUICategory.Value.Name.ExtractText()}");
var mismatched=quests.Count(row=>row.Reward.Select((item,index)=>item.RowId!=0 && row.ItemCountReward[index]==0).Any(flag=>flag));
Console.WriteLine($"Quests with nonzero fixed reference and zero direct quantity={mismatched}");
var currencyMap=QuestRewardCurrencyMap.Build(data.Excel.GetSheet<TomestonesItem>().Select(row=>(row.Tomestones.RowId,row.Item.RowId)));
var version=string.Join(';',data.Repositories.Values.OrderBy(row=>row.Name).Select(row=>$"{row.Name}={row.Version}"));
var snapshots=quests.Select(row=>(Quest:row,Rewards:QuestRewardCopy.FromRow(row,data.Excel.GetSubrowSheet<QuestClassJobReward>(),data.Excel.GetSheet<BeastRankBonus>(),currencyMap,version))).ToArray();
var rewardItems=snapshots.SelectMany(row=>row.Rewards.Items).ToArray();
if(rewardItems.Any(reward=>!data.Excel.GetSheet<Item>().HasRow(reward.ItemId)))throw new Exception("Projected Item ID does not exist.");
if(currencyMap.GetValueOrDefault(1u)!=28 || currencyMap.GetValueOrDefault(3u)!=49)throw new Exception("Tomestone type confused with TomestonesItem.RowId.");
Console.WriteLine($"PASS production projection: {snapshots.Length} snapshots, {rewardItems.Length} reward/condition alternatives, {rewardItems.Count(item=>item.Quantity is null)} unknown quantities, {snapshots.Sum(row=>row.Rewards.Unresolved.Count)} unresolved references; all Item IDs resolve");
foreach(var group in rewardItems.GroupBy(item=>item.Rule))Console.WriteLine($"PROJECTED {group.Key}: {group.Count()} lines");
foreach(var group in snapshots.SelectMany(row=>row.Rewards.Unresolved).GroupBy(row=>row.Source))Console.WriteLine($"UNRESOLVED {group.Key}: {group.Count()} references, quantity retained");
var managed=snapshots.Select(row=>new QuestEntry(row.Quest.RowId,row.Quest.Name.ExtractText(),row.Quest.JournalGenre.Value.Name.ExtractText(),row.Quest.Icon,QuestProgress.Unconfirmed,null){Rewards=row.Rewards}).ToArray();
using(var service=new QuestService())
{
    service.Publish(0,managed);await service.IndexWork;
    var index=service.State.RewardIndex ?? throw new Exception(service.State.RewardIndexError ?? "Missing reward index.");
    if(service.State.CharacterId!=0 || service.State.ObservedAt is not null || service.State.Entries.Any(row=>row.Progress!=QuestProgress.Unconfirmed || row.Sequence is not null))throw new Exception("Static inverse index fabricated owner/progression.");
    if(index.Coverage.TotalQuests!=5373 || index.Coverage.CoveredQuests!=5373 || index.Coverage.UnresolvedReferences!=15 || index.Coverage.UnknownQuantityLines!=4)throw new Exception("Native reward source coverage mismatch.");
    var totalVariants=index.DataVersions.Count;
    foreach(var item in rewardItems.Select(row=>row.ItemId).Distinct())
    {
        var expected=snapshots.Where(row=>row.Rewards.Items.Any(reward=>reward.ItemId==item)).ToArray();
        var actual=index.ForItem(item);
        if(!actual.Select(row=>row.QuestId).ToHashSet().SetEquals(expected.Select(row=>row.Quest.RowId)))throw new Exception("Inverse index quest set mismatch.");
        foreach(var source in actual)
        {
            var exact=expected.Single(row=>row.Quest.RowId==source.QuestId).Rewards.Items.Where(row=>row.ItemId==item);
            if(!source.Variants.SequenceEqual(exact))throw new Exception("Inverse index lost/reordered variants.");
        }
    }
    Console.WriteLine($"PASS inverse production index: {index.ItemCount} Items, {index.SourceCount} quest/Item associations, every variant agrees with forward projection; {index.ForItem(4551).Count} Potion quests, versions={totalVariants}; offline progression remains unconfirmed");
}
if(rewardItems.Any(item=>item.StainId!=0 && !data.Excel.GetSheet<Stain>().HasRow(item.StainId)))throw new Exception("Projected stain ID unresolved.");
var jobSheet=data.Excel.GetSubrowSheet<QuestClassJobReward>();
var jobs=quests.Where(row=>row.ItemRewardType==6).SelectMany(row=>row.Reward).Where(row=>row.RowId!=0).Select(row=>row.RowId).Distinct().ToArray();
Console.WriteLine($"JOB tables={jobs.Length}, subrows={jobs.Sum(id=>jobSheet.GetSubrowCount(id))}, maximum subrows={jobs.Max(id=>jobSheet.GetSubrowCount(id))}");
foreach(var subrow in jobSheet.GetRow(jobs[0]).Take(2))Console.WriteLine($"JOB {subrow.RowId}/{subrow.SubrowId}, category={subrow.ClassJobCategory.RowId} {subrow.ClassJobCategory.Value.Name.ExtractText()}; rewards={string.Join(';',subrow.RewardItem.Select((item,index)=>$"{item.RowId}x{subrow.RewardAmount[index]}"))}");
foreach(var bonus in data.Excel.GetSheet<BeastRankBonus>().Where(row=>row.RowId!=0).Take(3))Console.WriteLine($"TRIBE {bonus.RowId}: Item={bonus.Item.RowId}, quantities={string.Join(',',bonus.ItemQuantity)}, neutral={bonus.Neutral}, recognized={bonus.Recognized}");
foreach(var rank in data.Excel.GetSheet<BeastReputationRank>().Take(10))Console.WriteLine($"RANK {rank.RowId}: {rank.Name.ExtractText()}");
Console.WriteLine("TOME quest keys="+string.Join(',',quests.Where(row=>row.TomestoneCountReward>0).Select(row=>row.TomestoneReward).Distinct()));
foreach(var tome in data.Excel.GetSheet<TomestonesItem>())if(tome.Item.RowId!=0)Console.WriteLine($"TOME ItemRow={tome.RowId}, type={tome.Tomestones.RowId}, Item={tome.Item.RowId} {tome.Item.Value.Name.ExtractText()}");
foreach(var row in quests.Where(row=>row.ItemRewardType!=6 && row.Reward.Select((item,index)=>item.RowId!=0 && row.ItemCountReward[index]==0).Any(flag=>flag)))Console.WriteLine($"ZERO QUANTITY {row.RowId} {row.Name.ExtractText()}, mode={row.ItemRewardType}");
