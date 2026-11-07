using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Deliveries;

/// <summary>W-01 (0.675): the clients, once. Names in the game's language.</summary>
internal static class DeliveryClientLoader
{
    public static IReadOnlyList<DeliveryClient> Read(IDataManager data)
    {
        var clients=new List<DeliveryClient>();
        var supply=data.GetSubrowExcelSheet<SatisfactionSupply>();
        foreach(var row in data.GetExcelSheet<SatisfactionNpc>())
        {
            if(row.RowId==0 || row.Npc.RowId==0)continue;
            var name=row.Npc.ValueNullable?.Singular.ExtractText() ?? "";
            var place=row.Level.ValueNullable?.Territory.ValueNullable?.PlaceName.ValueNullable?.Name.ExtractText() ?? "";
            clients.Add(new(row.RowId,name,place,row.LevelUnlock,row.DeliveriesPerWeek,row.QuestRequired.RowId,row.SatisfactionNpcParams.Select(item=>(int)item.SatisfactionRequired).ToArray(),Rewards(row,supply),Gifts(row)));
        }
        return clients;
    }

    private static IReadOnlyList<DeliveryReward> Rewards(SatisfactionNpc client,Lumina.Excel.SubrowExcelSheet<SatisfactionSupply> supply)
    {
        var rows=new List<DeliveryRewardRow>();
        for(var rank=1;rank<=DeliveryBoard.MaxRank && rank<client.SatisfactionNpcParams.Count;rank++)
        {
            if(!supply.TryGetRow((uint)client.SatisfactionNpcParams[rank].SupplyIndex,out var lines))continue;
            foreach(var line in lines)
            {
                if(line.Reward.ValueNullable is not { } reward)continue;
                rows.Add(new(rank,line.Reward.RowId,line.Slot,line.IsBonus,reward.BonusMultiplier,new(reward.SatisfactionLow,reward.SatisfactionMid,reward.SatisfactionHigh),new(reward.GilLow,reward.GilMid,reward.GilHigh),
                    reward.SatisfactionSupplyRewardData.Select(data=>((uint)data.RewardCurrency,new DeliveryTiers(data.QuantityLow,data.QuantityMid,data.QuantityHigh))).ToArray()));
            }
        }
        return DeliveryBoard.Rewards(rows);
    }

    private static IReadOnlyList<DeliveryGift> Gifts(SatisfactionNpc client)
    {
        var gifts=new List<DeliveryGift>();
        for(var rank=1;rank<=DeliveryBoard.MaxRank && rank<client.SatisfactionNpcParams.Count;rank++)
        {
            var rankRow=client.SatisfactionNpcParams[rank];
            for(var index=0;index<rankRow.Item.Count;index++)
                if(rankRow.Item[index].RowId!=0 && rankRow.Item[index].ValueNullable is { } item && !item.Name.IsEmpty)
                    gifts.Add(new(rank,item.Name.ExtractText(),rankRow.ItemCount[index],rankRow.IsHQ[index]));
        }
        return gifts;
    }
}

/// <summary>W-01 (0.675): field reads on the framework thread; no request to the server and no action in the game.</summary>
internal static class DeliveryReader
{
    public static unsafe DeliverySnapshot Read(IReadOnlyList<DeliveryClient> clients,Func<uint,bool> questDone)
    {
        var now=DateTimeOffset.UtcNow;
        var player=PlayerState.Instance();
        if(player==null || !player->IsLoaded)return DeliverySnapshot.Of(DeliveryReadStatus.NotLoaded,now);
        var manager=SatisfactionSupplyManager.Instance();
        if(manager==null)return DeliverySnapshot.Of(DeliveryReadStatus.Unavailable,now);
        var ranks=manager->SatisfactionRanks.ToArray().Select(item=>(int)item).ToArray();
        var points=manager->Satisfaction.ToArray().Select(item=>(int)item).ToArray();
        var used=manager->UsedAllowances.ToArray().Select(item=>(int)item).ToArray();
        var remaining=manager->GetRemainingAllowances();
        var status=DeliveryBoard.Check(clients,ranks,points,used,remaining);
        if(status!=DeliveryReadStatus.Loaded)return DeliverySnapshot.Of(status,now);
        return new(status,now,remaining,ranks,points,used,clients.Where(item=>item.QuestRequired!=0 && questDone(item.QuestRequired)).Select(item=>item.QuestRequired).ToHashSet());
    }
}
