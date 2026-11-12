using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Fields;

/// <summary>W-14 (0.717): from the game's files only. Fate.Location and DynamicEvent.LGBMapRange are instance ids of the zone's layout: docs/evidence/0.717-eureka-bozja.md. Off the frame.</summary>
internal static class FieldCatalogLoader
{
    private static readonly string[] Layouts=["planevent","planmap"];

    /// <summary>W-14b (0.721): every open zone's FATEs, in the game's order of zones.</summary>
    public static IReadOnlyList<FieldZone> ReadOpenZones(IDataManager data,IPluginLog log)
    {
        var rows=FateRows(data);
        var zones=new List<FieldZone>();
        foreach(var territory in data.GetExcelSheet<TerritoryType>())
        {
            if(territory.RowId==0 || territory.TerritoryIntendedUse.RowId!=FieldBoard.OpenZoneUse || territory.PlaceName.ValueNullable?.Name.ExtractText() is not { Length:>0 } name)continue;
            var events=FieldBoard.Place(Entries(data,log,territory),rows);
            if(events.Count>0)zones.Add(new(territory.RowId,territory.Map.RowId,name,events));
        }
        return zones;
    }

    private static List<FieldEventRow> FateRows(IDataManager data)
    {
        var rows=new List<FieldEventRow>();
        foreach(var row in data.GetExcelSheet<Fate>())
            if(row.Location!=0)rows.Add(new(FieldEventKind.Fate,row.RowId,row.Location,row.Name.ExtractText(),row.Description.ExtractText(),row.ClassJobLevel,0));
        return rows;
    }

    public static FieldCatalog Read(IDataManager data,IPluginLog log)
    {
        var rows=FateRows(data);
        foreach(var row in data.GetExcelSheet<DynamicEvent>())
            if(row.LGBMapRange!=0)rows.Add(new(FieldEventKind.Engagement,row.RowId,row.LGBMapRange,row.Name.ExtractText(),row.Description.ExtractText(),null,row.MaxParticipants));
        var zones=new List<FieldZone>();
        foreach(var territory in data.GetExcelSheet<TerritoryType>())
        {
            if(territory.RowId==0 || !FieldBoard.FieldZoneUses.Contains(territory.TerritoryIntendedUse.RowId))continue;
            zones.Add(new(territory.RowId,territory.Map.RowId,territory.PlaceName.ValueNullable?.Name.ExtractText() ?? "",FieldBoard.Place(Entries(data,log,territory),rows))
                {Use=territory.TerritoryIntendedUse.RowId});
        }
        var records=data.GetExcelSheet<MYCWarResultNotebook>()
            .Where(row=>row.RowId!=0 && !row.Name.IsEmpty)
            .Select(row=>new FieldRecord(row.RowId,row.Number,row.Name.ExtractText(),row.Description.ExtractText(),row.Rarity,row.Quest.ValueNullable?.Name.ExtractText() ?? "",(uint)Math.Max(0,row.Icon)))
            .OrderBy(row=>row.Number).ToArray();
        var descriptions=data.GetExcelSheet<ActionTransient>();
        var actions=new List<FieldAction>();
        foreach(var row in data.GetExcelSheet<MYCTemporaryItem>())
            if(row.RowId!=0 && row.Action.ValueNullable is { } action && !action.Name.IsEmpty)
                actions.Add(new(row.RowId,row.Order,action.Name.ExtractText(),row.Category.ValueNullable?.Name.ExtractText() ?? "",row.Max,descriptions.GetRowOrDefault(action.RowId)?.Description.ExtractText() ?? "",action.Icon));
        // X-188 (0.1176): the phantom jobs: each level's action (MKDSupportJob.Actions) and trait (MKDTrait), docs/evidence/0.1176-occult-jobs.md.
        var traits=data.GetExcelSheet<MKDTrait>().Where(row=>row.RowId!=0 && !row.Name.IsEmpty).ToArray();
        var jobs=new List<OccultJob>();
        foreach(var job in data.GetExcelSheet<MKDSupportJob>())
        {
            if(job.Name.IsEmpty)continue;
            var unlocks=new List<OccultUnlock>();
            foreach(var step in job.Actions)
                if(step.LevelUnlock>0 && step.Action.ValueNullable is { } action && !action.Name.IsEmpty)
                    unlocks.Add(new(step.LevelUnlock,action.Name.ExtractText(),descriptions.GetRowOrDefault(action.RowId)?.Description.ExtractText() ?? "",action.Icon,false));
            foreach(var trait in traits)
                if(trait.MKDSupportJob.RowId==job.RowId && trait.LevelUnlock>0)unlocks.Add(new(trait.LevelUnlock,trait.Name.ExtractText(),trait.Description.ExtractText(),(uint)Math.Max(0,trait.Icon),true));
            jobs.Add(new(job.RowId,job.Name.ExtractText(),job.LevelMax,unlocks.OrderBy(unlock=>unlock.Level).ThenBy(unlock=>unlock.Trait).ToArray()));
        }
        var addon=data.GetExcelSheet<Addon>();
        string Label(uint row)=>addon.GetRowOrDefault(row)?.Text.ExtractText() ?? "";
        return new(zones,records,actions.OrderBy(row=>row.Order).ToArray()){OccultJobs=jobs,OccultLabels=new(Label(16607),Label(16611),Label(16608),Label(16618))};
    }

    private static List<FieldLayoutEntry> Entries(IDataManager data,IPluginLog log,TerritoryType territory)
    {
        var entries=new List<FieldLayoutEntry>();
        var bg=territory.Bg.ExtractText();var cut=bg.IndexOf("/level/",StringComparison.Ordinal);
        if(cut<0)return entries;
        foreach(var kind in Layouts)
        {
            var path=$"bg/{bg[..(cut+1)]}level/{kind}.lgb";
            try
            {
                // As for the vendors: Lumina's parser trusts its counts, so only a file with the expected headers reaches it.
                if(!data.FileExists(path) || data.GetFile(path)?.Data is not { } bytes || !Npcs.NpcShopLinks.ValidLayout(bytes))continue;
                if(data.GetFile<Lumina.Data.Files.LgbFile>(path) is not { } file)continue;
                foreach(var layer in file.Layers)
                    foreach(var entry in layer.InstanceObjects)
                        entries.Add(new(entry.InstanceId,entry.Transform.Translation.X,entry.Transform.Translation.Z));
            }
            catch(Exception error){log.Debug(error,"Territory layout unreadable: {0}",path);}
        }
        return entries;
    }
}
