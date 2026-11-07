using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.GrandCompanies;

/// <summary>W-11 (0.714): grades and the seal shops, once. Names in the game's language; grades in the masculine or feminine sheets, as the character.</summary>
internal static class GcCatalogLoader
{
    public static GcCatalog Read(IDataManager data,bool feminine)
    {
        string[] Names(uint row)
        {
            if(feminine)
                return [data.GetExcelSheet<GCRankLimsaFemaleText>().GetRowOrDefault(row)?.Singular.ExtractText() ?? "",data.GetExcelSheet<GCRankGridaniaFemaleText>().GetRowOrDefault(row)?.Singular.ExtractText() ?? "",data.GetExcelSheet<GCRankUldahFemaleText>().GetRowOrDefault(row)?.Singular.ExtractText() ?? ""];
            return [data.GetExcelSheet<GCRankLimsaMaleText>().GetRowOrDefault(row)?.Singular.ExtractText() ?? "",data.GetExcelSheet<GCRankGridaniaMaleText>().GetRowOrDefault(row)?.Singular.ExtractText() ?? "",data.GetExcelSheet<GCRankUldahMaleText>().GetRowOrDefault(row)?.Singular.ExtractText() ?? ""];
        }
        var ranks=data.GetExcelSheet<GrandCompanyRank>().Where(row=>row.RowId!=0).Select(row=>new GcRank((int)row.RowId,(int)row.MaxSeals,(int)row.RequiredSeals,Names(row.RowId),
            [row.QuestMaelstrom.ValueNullable?.Name.ExtractText() ?? "",row.QuestSerpents.ValueNullable?.Name.ExtractText() ?? "",row.QuestFlames.ValueNullable?.Name.ExtractText() ?? ""])).ToArray();
        var categories=data.GetExcelSheet<GCScripShopCategory>();
        var shop=new List<GcShopItem>();
        foreach(var group in data.GetSubrowExcelSheet<GCScripShopItem>())
            foreach(var row in group)
                if(row.Item.RowId!=0 && categories.GetRowOrDefault(row.RowId) is { } category)
                    shop.Add(new((int)category.GrandCompany.RowId,row.Item.RowId,(int)row.CostGCSeals,(int)row.RequiredGrandCompanyRank.RowId));
        return new(data.GetExcelSheet<GrandCompany>().OrderBy(row=>row.RowId).Select(row=>row.Name.ExtractText()).ToArray(),ranks,shop);
    }
}

/// <summary>W-11 (0.714): the game's own reads, on the framework thread with the character loaded; no request to the server and no action in the game.</summary>
internal static class GcReader
{
    public static unsafe bool? Feminine()
    {
        var player=PlayerState.Instance();
        return player==null || !player->IsLoaded ? null:player->Sex==1;
    }

    public static unsafe GcSnapshot Read(int rankRows)
    {
        var player=PlayerState.Instance();
        if(player==null || !player->IsLoaded)return GcSnapshot.Of(GcReadStatus.NotLoaded);
        var inventory=InventoryManager.Instance();
        if(inventory==null)return GcSnapshot.Of(GcReadStatus.Unavailable);
        var ranks=player->GCRanks.ToArray().Select(item=>(int)item).ToArray();
        var status=GcBoard.Check(player->GrandCompany,ranks,rankRows);
        if(status!=GcReadStatus.Loaded)return GcSnapshot.Of(status);
        var seals=new[]{(int)inventory->GetCompanySeals(1),(int)inventory->GetCompanySeals(2),(int)inventory->GetCompanySeals(3)};
        var max=new[]{(int)inventory->GetMaxCompanySeals(1),(int)inventory->GetMaxCompanySeals(2),(int)inventory->GetMaxCompanySeals(3)};
        return new(status,player->GrandCompany,ranks,seals,max);
    }
}
