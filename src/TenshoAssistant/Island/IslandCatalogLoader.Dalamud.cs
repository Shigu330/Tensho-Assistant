using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Island;

/// <summary>N-10 (0.671): the workshops' sheets, once. Names in the game's language.</summary>
internal static class IslandCatalogLoader
{
    public static IslandCatalog Read(IDataManager data)
    {
        var objects=data.GetExcelSheet<MJICraftworksObject>();
        var items=data.GetExcelSheet<Item>();
        var crafts=objects.Where(row=>row.Item.RowId!=0 && row.CraftingTime is >=1 and <=24 && row.Value>0)
            .Select(row=>new IslandCraft(row.RowId,row.Item.RowId,items.GetRowOrDefault(row.Item.RowId)?.Name.ExtractText() ?? "#"+row.Item.RowId,row.CraftingTime,row.Value,row.LevelReq,
                row.Theme.Select(theme=>theme.RowId).Where(theme=>theme!=0).ToArray()))
            .ToArray();
        return new(objects.Count,crafts,
            data.GetExcelSheet<MJICraftworksPopularity>().OrderBy(row=>row.RowId).Select(row=>row.Popularity.Select(type=>(byte)type.RowId).ToArray()).ToArray(),
            data.GetExcelSheet<MJICraftworksPopularityType>().OrderBy(row=>row.RowId).Select(row=>(int)row.Ratio).ToArray(),
            data.GetExcelSheet<MJICraftworksSupplyDefine>().OrderBy(row=>row.RowId).Select(row=>(int)row.Ratio).ToArray(),
            data.GetExcelSheet<MJICraftworksRankRatio>().OrderBy(row=>row.RowId).Select(row=>(int)row.Ratio).ToArray(),
            data.GetExcelSheet<MJICraftworksObjectTheme>().Where(row=>!row.Name.IsEmpty).ToDictionary(row=>row.RowId,row=>row.Name.ExtractText()));
    }
}
