using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Carnivale;

/// <summary>W-02 (0.683): the stages, once. Enemy names in the game's language.</summary>
internal static class CarnivaleStageLoader
{
    public static IReadOnlyList<CarnivaleStage> Read(IDataManager data)
    {
        var arrangements=data.GetSubrowExcelSheet<AOZArrangement>();var bosses=data.GetExcelSheet<AOZBoss>();
        var stages=new List<CarnivaleStage>();
        foreach(var row in data.GetExcelSheet<AOZContent>())
        {
            if(row.RowId==0)continue;
            var names=new List<string>();
            void Act(uint id,int type)
            {
                if(id==0)return;
                if(CarnivaleBoard.IsArrangement(type))
                {
                    if(arrangements.TryGetRow(id,out var enemies))
                        foreach(var enemy in enemies)names.Add(enemy.AOZContentBriefingBNpc.ValueNullable?.BNpcName.ValueNullable?.Singular.ExtractText() ?? "");
                }
                else if(bosses.TryGetRow(id,out var boss))names.Add(boss.Boss.ValueNullable?.BNpcName.ValueNullable?.Singular.ExtractText() ?? "");
            }
            Act(row.Act1.RowId,row.Act1FightType);
            Act(row.Act2.RowId,row.Act2FightType);
            Act(row.Act3.RowId,row.Act3FightType);
            stages.Add(new(row.RowId,row.Order,CarnivaleBoard.Enemies(names),row.StandardFinishTime,row.IdealFinishTime));
        }
        return stages;
    }
}

/// <summary>W-02 (0.683): a field read on the framework thread; no request to the server and no action in the game.</summary>
internal static class CarnivaleReader
{
    public static unsafe CarnivaleSnapshot Read(IReadOnlyList<CarnivaleStage> stages)
    {
        var player=PlayerState.Instance();
        if(player==null || !player->IsLoaded)return CarnivaleSnapshot.Of(CarnivaleReadStatus.NotLoaded);
        var bytes=player->CompletedMaskedCarnivale;
        var bits=bytes.Length*8;
        var status=CarnivaleBoard.Check(stages,bits);
        if(status!=CarnivaleReadStatus.Loaded)return CarnivaleSnapshot.Of(status);
        var completed=new bool[bits];
        for(var index=0;index<bits;index++)completed[index]=player->CompletedMaskedCarnivaleBitArray[index];
        return new(status,completed);
    }
}
