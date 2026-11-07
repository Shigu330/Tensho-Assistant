using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Relics;

/// <summary>W-03 (0.708): RelicItem and RelicNote, once. Names in the game's language.</summary>
internal static class RelicCatalogLoader
{
    public static RelicCatalog Read(IDataManager data)
    {
        var stages=data.GetExcelSheet<RelicItem>().Select(row=>(IReadOnlyList<uint>)new[]{row.GladiatorItem.RowId,row.ShieldItem.RowId,row.PugilistItem.RowId,row.MarauderItem.RowId,row.LancerItem.RowId,row.ArcherItem.RowId,
            row.ConjurerItem.RowId,row.ThaumaturgeItem.RowId,row.ArcanistSMNItem.RowId,row.ArcanistSCHItem.RowId,row.RogueItem.RowId}).ToArray();
        string Zone(MonsterNoteTarget target)=>target.PlaceNameZone.Select(place=>place.ValueNullable?.Name.ExtractText() ?? "").FirstOrDefault(name=>name.Length>0) ?? "";
        var books=new Dictionary<uint,RelicBook>();
        foreach(var row in data.GetExcelSheet<Lumina.Excel.Sheets.RelicNote>())
        {
            if(row.RowId==0)continue;
            var monsters=new List<RelicTarget>();
            for(var index=0;index<row.MonsterNoteTargetCommon.Count;index++)
                if(row.MonsterNoteTargetCommon[index].ValueNullable is { RowId:not 0 } target)
                    monsters.Add(new(target.BNpcName.ValueNullable?.Singular.ExtractText() ?? "",Zone(target),index<row.MonsterCount.Count ? row.MonsterCount[index]:0));
            var dungeons=row.MonsterNoteTargetNM.Select(item=>item.ValueNullable).Where(item=>item is { RowId:not 0 }).Select(item=>new RelicTarget(item!.Value.BNpcName.ValueNullable?.Singular.ExtractText() ?? "",Zone(item.Value))).ToArray();
            var fates=new List<RelicTarget>();
            for(var index=0;index<row.Fate.Count;index++)
                if(row.Fate[index].ValueNullable is { RowId:not 0 } fate)
                    fates.Add(new(fate.Name.ExtractText(),index<row.PlaceNameFate.Count ? row.PlaceNameFate[index].ValueNullable?.Name.ExtractText() ?? "":""));
            var leves=row.Leve.Select(item=>item.ValueNullable).Where(item=>item is { RowId:not 0 }).Select(item=>new RelicTarget(item!.Value.Name.ExtractText(),item.Value.PlaceNameStartZone.ValueNullable?.Name.ExtractText() ?? "")).ToArray();
            books[row.RowId]=new(row.RowId,row.EventItem.ValueNullable?.Name.ExtractText() ?? "",monsters,dungeons,fates,leves);
        }
        // W-13 (0.716): the later relics, found in the sheet by item level and English name.
        var levels=RelicBoard.Levels();
        var english=data.GetExcelSheet<Item>(Dalamud.Game.ClientLanguage.English);
        var rows=english.Where(row=>row.EquipSlotCategory.RowId is 1 or 13 && levels.Contains((int)row.LevelItem.RowId))
            .Select(row=>new RelicItemRow(row.RowId,row.Name.ExtractText(),(int)row.LevelItem.RowId,row.Rarity,row.ClassJobCategory.RowId)).ToArray();
        var families=RelicBoard.Rules.Select(family=>(family.Key,RelicBoard.FamilyLines(rows,family.Stages))).Where(family=>family.Item2.Count>0).ToArray();
        // W-13 (0.720): their armour, the same way: head, body, hands, legs, feet.
        var armourLevels=RelicBoard.ArmourLevels();
        var armourRows=english.Where(row=>row.EquipSlotCategory.RowId is 3 or 4 or 5 or 7 or 8 && armourLevels.Contains((int)row.LevelItem.RowId))
            .Select(row=>new RelicItemRow(row.RowId,row.Name.ExtractText(),(int)row.LevelItem.RowId,row.Rarity,row.ClassJobCategory.RowId)).ToArray();
        var categories=data.GetExcelSheet<ClassJobCategory>();
        var armours=RelicBoard.ArmourRules.ToDictionary(family=>family.Key,family=>RelicBoard.ArmourSets(armourRows,family.Stages,category=>categories.GetRowOrDefault(category)?.Name.ExtractText() ?? category.ToString()));
        return new(RelicBoard.Lines(stages),books,families,armours);
    }
}

/// <summary>W-03 (0.708): field reads on the framework thread; no request to the server and no action in the game.</summary>
internal static class RelicReader
{
    public static unsafe RelicProgress Read()
    {
        var player=PlayerState.Instance();
        if(player==null || !player->IsLoaded)return RelicProgress.Of(RelicReadStatus.NotLoaded);
        var note=FFXIVClientStructs.FFXIV.Client.Game.UI.RelicNote.Instance();
        if(note==null)return RelicProgress.Of(RelicReadStatus.Unavailable);
        var monsters=note->MonsterProgress.ToArray().Select(item=>(int)item).ToArray();
        var dungeons=new[]{note->IsDungeonComplete(0),note->IsDungeonComplete(1),note->IsDungeonComplete(2)};
        var fates=new[]{note->IsFateComplete(0),note->IsFateComplete(1),note->IsFateComplete(2)};
        var leves=new[]{note->IsLeveComplete(0),note->IsLeveComplete(1),note->IsLeveComplete(2)};
        return new(RelicReadStatus.Loaded,note->RelicNoteId,monsters,dungeons,fates,leves);
    }
}
