namespace TenshoAssistant.Deliveries;

/// <summary>W-01 (0.675): a custom delivery client, from SatisfactionNpc. RankPoints[rank] is what that rank asks for before the next one; docs/evidence/0.675-custom-deliveries.md.</summary>
public sealed record DeliveryClient(uint Row,string Name,string Place,int LevelUnlock,int PerWeek,uint QuestRequired,IReadOnlyList<int> RankPoints,
    IReadOnlyList<DeliveryReward>? Rewards=null,IReadOnlyList<DeliveryGift>? Gifts=null);

/// <summary>Three amounts, by collectability tier: low, middle, high.</summary>
public readonly record struct DeliveryTiers(int Low,int Mid,int High)
{
    /// <summary>The weekly bonus: rounded down, as the amounts of docs/evidence/0.730-delivery-rewards.md show.</summary>
    public DeliveryTiers Times(int percent)=>new(Low*percent/100,Mid*percent/100,High*percent/100);
}
/// <summary>A raw line of SatisfactionSupply with its reward row.</summary>
public sealed record DeliveryRewardRow(int Rank,uint RewardRow,int Slot,bool Bonus,int Percent,DeliveryTiers Points,DeliveryTiers Gil,IReadOnlyList<(uint Code,DeliveryTiers Amount)> Currencies);
public sealed record DeliveryReward(int Rank,bool Crafting,bool Bonus,int Percent,DeliveryTiers Points,DeliveryTiers Gil,IReadOnlyList<(uint ItemId,DeliveryTiers Amount)> Currencies);
/// <summary>An object the client's sheet attaches to a rank.</summary>
public sealed record DeliveryGift(int Rank,string Name,int Count,bool Hq);

public enum DeliveryReadStatus { Loaded,NotLoaded,Unavailable }

/// <summary>What the game holds, copied on the framework thread. The three lists are indexed by client row minus one.</summary>
public sealed record DeliverySnapshot(DeliveryReadStatus Status,DateTimeOffset ReadAt,int Remaining,IReadOnlyList<int> Ranks,IReadOnlyList<int> Points,IReadOnlyList<int> Used,IReadOnlyCollection<uint> QuestsDone)
{
    public static DeliverySnapshot Of(DeliveryReadStatus status,DateTimeOffset at)=>new(status,at,0,[],[],[],[]);
}

/// <summary>One line of the page. Needed is null at the last rank, where the sheet asks for nothing more.</summary>
public sealed record DeliveryRow(DeliveryClient Client,bool Unlocked,int Rank,int Points,int? Needed,int Used,int Left);

public static class DeliveryBoard
{
    public const int MaxRank=5;

    /// <summary>The codes of SatisfactionSupplyReward, the same as the scrip shops': purple and orange, crafters' then gatherers'. Amounts checked against a second source: docs/evidence/0.730-delivery-rewards.md.</summary>
    public static uint? ScripItem(uint code)=>code switch{2=>33913,4=>33914,6=>41784,7=>41785,_=>null};

    /// <summary>
    /// W-01 (0.730): one line by rank, kind of delivery and bonus: many items share a reward row. Slot 1 is crafted, the others gathered or fished. A bonus line carries its multiplied amounts,
    /// the points excepted. A currency whose code is unknown makes the line go: nothing is shown rather than a guess.
    /// </summary>
    public static IReadOnlyList<DeliveryReward> Rewards(IEnumerable<DeliveryRewardRow> rows)
    {
        var lines=new List<DeliveryReward>();
        foreach(var row in rows.DistinctBy(row=>(row.Rank,row.RewardRow,row.Bonus,row.Slot==1)).OrderBy(row=>row.Rank).ThenBy(row=>row.Bonus).ThenBy(row=>row.Slot==1 ? 0:1))
        {
            var used=row.Currencies.Where(currency=>currency.Code!=0).ToArray();
            if(used.Length==0 || used.Any(currency=>ScripItem(currency.Code) is null))continue;
            var percent=row.Bonus && row.Percent>100 ? row.Percent:100;
            lines.Add(new(row.Rank,row.Slot==1,row.Bonus,percent,row.Points,row.Gil.Times(percent),used.Select(currency=>(ScripItem(currency.Code)!.Value,currency.Amount.Times(percent))).ToArray()));
        }
        return lines;
    }

    /// <summary>A list too short or a value the sheets cannot give means the game has changed: nothing is shown rather than a wrong line.</summary>
    public static DeliveryReadStatus Check(IReadOnlyList<DeliveryClient> clients,IReadOnlyList<int> ranks,IReadOnlyList<int> points,IReadOnlyList<int> used,int remaining)
    {
        if(remaining<0)return DeliveryReadStatus.Unavailable;
        foreach(var client in clients)
        {
            var index=(int)client.Row-1;
            if(index<0 || index>=ranks.Count || index>=points.Count || index>=used.Count)return DeliveryReadStatus.Unavailable;
            if(ranks[index]<0 || ranks[index]>MaxRank || points[index]<0 || used[index]<0)return DeliveryReadStatus.Unavailable;
        }
        return DeliveryReadStatus.Loaded;
    }

    /// <summary>What a client still takes: its own weekly limit and the week's allowances both apply.</summary>
    public static int Left(int perWeek,int used,int remaining)=>Math.Max(0,Math.Min(perWeek-used,remaining));

    /// <summary>A client is locked until its quest is done, or while the game gives it no rank.</summary>
    public static IReadOnlyList<DeliveryRow> Rows(IReadOnlyList<DeliveryClient> clients,DeliverySnapshot snapshot)
    {
        if(snapshot.Status!=DeliveryReadStatus.Loaded)return [];
        var rows=new List<DeliveryRow>(clients.Count);
        foreach(var client in clients)
        {
            var index=(int)client.Row-1;
            var rank=snapshot.Ranks[index];
            var unlocked=client.QuestRequired==0 || snapshot.QuestsDone.Contains(client.QuestRequired);
            int? needed=rank>=0 && rank<client.RankPoints.Count && client.RankPoints[rank]>0 ? client.RankPoints[rank]:null;
            var left=unlocked ? Left(client.PerWeek,snapshot.Used[index],snapshot.Remaining):0;
            rows.Add(new(client,unlocked,rank,snapshot.Points[index],needed,snapshot.Used[index],left));
        }
        return rows;
    }
}
