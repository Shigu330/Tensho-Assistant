using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Societies;

/// <summary>W-10 (0.713): the societies, once. Names in the game's language.</summary>
internal static class AlliedCatalogLoader
{
    public static AlliedCatalog Read(IDataManager data)
    {
        var societies=new List<AlliedSociety>();
        var (kinds,kindNames)=ReadKinds(data);
        foreach(var row in data.GetExcelSheet<BeastTribe>())
        {
            var name=row.Name.ExtractText();
            if(row.RowId==0 || name.Length==0)continue;
            var level=row.Level.ValueNullable;
            var place=level?.Territory.ValueNullable?.PlaceName.ValueNullable?.Name.ExtractText() ?? "";
            societies.Add(new(row.RowId,name,row.Expansion.ValueNullable?.Name.ExtractText() ?? "",place,row.MaxRank,row.CurrencyItem.RowId,row.DisplayOrder,level?.Map.RowId ?? 0,level?.X ?? 0,level?.Z ?? 0,kinds.GetValueOrDefault(row.RowId)));
        }
        var ranks=data.GetExcelSheet<BeastReputationRank>().Select(row=>row.Name.ExtractText()).ToArray();
        return new(AlliedBoard.Ordered(societies),ranks){KindNames=kindNames};
    }

    /// <summary>X-265 (0.1306): each society's kind from its quests' ClassJobCategory0, the class flags read raw (column i + 1 is ClassJob
    /// row i, checked on the 43 one-class categories by tools/AlliedKindProbe). A sheet that changed leaves the kinds unknown (R08): the
    /// societies still show, under « all ».</summary>
    private static (Dictionary<uint,AlliedKind> Kinds,Dictionary<AlliedKind,string> Names) ReadKinds(IDataManager data)
    {
        var kinds=new Dictionary<uint,AlliedKind>();var names=new Dictionary<AlliedKind,string>();
        try
        {
            var classes=data.GetExcelSheet<ClassJob>();
            var classCategories=new uint[(int)classes.Max(row=>row.RowId)+1];
            foreach(var row in classes)classCategories[row.RowId]=row.ClassJobCategory.RowId;
            var raw=data.Excel.GetSheet<RawRow>(name:"ClassJobCategory");
            var byCategory=new Dictionary<uint,AlliedKind>();
            AlliedKind Of(uint category)
            {
                if(byCategory.TryGetValue(category,out var known))return known;
                if(raw.GetRowOrDefault(category) is not { } row)return byCategory[category]=AlliedKind.None;
                var allowed=new bool[classCategories.Length];
                for(var index=0;index<allowed.Length && index+1<raw.Columns.Count;index++)allowed[index]=row.ReadColumn(index+1) is true;
                return byCategory[category]=AlliedKinds.OfCategory(allowed,classCategories);
            }
            foreach(var quest in data.GetExcelSheet<Quest>())
                if(quest.BeastTribe.RowId is var tribe and not 0 && quest.ClassJobCategory0.RowId is var category and not 0)
                    kinds[tribe]=kinds.GetValueOrDefault(tribe)|Of(category);
            foreach(var (kind,category) in new[]{(AlliedKind.Combat,AlliedKinds.CombatNameCategory),(AlliedKind.Crafting,AlliedKinds.CraftingCategory),(AlliedKind.Gathering,AlliedKinds.GatheringCategory)})
                if(data.GetExcelSheet<ClassJobCategory>().GetRowOrDefault(category)?.Name.ExtractText() is { Length:>0 } name)names[kind]=name;
        }
        catch(Exception){kinds.Clear();names.Clear();}
        return (kinds,names);
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
