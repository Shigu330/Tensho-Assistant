using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Societies;

/// <summary>W-10 (0.713): the societies, once. Names in the game's language.</summary>
internal static class AlliedCatalogLoader
{
    public static AlliedCatalog Read(IDataManager data)
    {
        var societies=new List<AlliedSociety>();
        foreach(var row in data.GetExcelSheet<BeastTribe>())
        {
            var name=row.Name.ExtractText();
            if(row.RowId==0 || name.Length==0)continue;
            var level=row.Level.ValueNullable;
            var place=level?.Territory.ValueNullable?.PlaceName.ValueNullable?.Name.ExtractText() ?? "";
            societies.Add(new(row.RowId,name,row.Expansion.ValueNullable?.Name.ExtractText() ?? "",place,row.MaxRank,row.CurrencyItem.RowId,row.DisplayOrder,level?.Map.RowId ?? 0,level?.X ?? 0,level?.Z ?? 0));
        }
        var ranks=data.GetExcelSheet<BeastReputationRank>().Select(row=>row.Name.ExtractText()).ToArray();
        return new(AlliedBoard.Ordered(societies),ranks);
    }
}

/// <summary>W-10 (0.713): the game's own reads, on the framework thread with the character loaded; no request to the server and no action in the game.</summary>
internal static class AlliedReader
{
    public static unsafe AlliedSnapshot Read(AlliedCatalog catalog)
    {
        var player=PlayerState.Instance();
        if(player==null || !player->IsLoaded)return AlliedSnapshot.Of(AlliedReadStatus.NotLoaded);
        var standings=new Dictionary<uint,AlliedStanding>();
        foreach(var society in catalog.Societies)
        {
            if(society.Row>byte.MaxValue)continue;
            var index=(byte)society.Row;
            standings[society.Row]=new(player->GetBeastTribeRank(index),player->GetBeastTribeCurrentReputation(index),player->GetBeastTribeNeededReputation(index));
        }
        var status=AlliedBoard.Check(catalog.Societies,standings,catalog.RankNames.Count);
        if(status!=AlliedReadStatus.Loaded)return AlliedSnapshot.Of(status);
        var quests=QuestManager.Instance();
        return new(status,standings,quests==null ? null:(int)quests->GetBeastTribeAllowance());
    }
}
