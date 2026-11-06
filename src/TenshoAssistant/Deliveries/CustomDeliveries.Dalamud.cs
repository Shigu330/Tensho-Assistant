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
        foreach(var row in data.GetExcelSheet<SatisfactionNpc>())
        {
            if(row.RowId==0 || row.Npc.RowId==0)continue;
            var name=row.Npc.ValueNullable?.Singular.ExtractText() ?? "";
            var place=row.Level.ValueNullable?.Territory.ValueNullable?.PlaceName.ValueNullable?.Name.ExtractText() ?? "";
            clients.Add(new(row.RowId,name,place,row.LevelUnlock,row.DeliveriesPerWeek,row.QuestRequired.RowId,row.SatisfactionNpcParams.Select(item=>(int)item.SatisfactionRequired).ToArray()));
        }
        return clients;
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
