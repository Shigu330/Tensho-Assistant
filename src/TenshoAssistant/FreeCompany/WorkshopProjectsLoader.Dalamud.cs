using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.FreeCompany;

/// <summary>C22 (0.664): the workshop's projects, read once from the game's sheets, off the frame.</summary>
public static class WorkshopProjectsLoader
{
    public static IReadOnlyList<WorkshopProject> Read(IDataManager data)
    {
        var projects=new List<WorkshopProject>();
        foreach(var sequence in data.GetExcelSheet<CompanyCraftSequence>())
        {
            if(sequence.ResultItem.RowId==0)continue;
            var parts=new List<WorkshopPart>();
            foreach(var partRow in sequence.CompanyCraftPart)
            {
                if(partRow.RowId==0 || partRow.ValueNullable is not { } part)continue;
                var phases=new List<WorkshopPhase>();
                foreach(var processRow in part.CompanyCraftProcess)
                {
                    if(processRow.RowId==0 || processRow.ValueNullable is not { } process)continue;
                    var materials=new List<WorkshopMaterial>();
                    for(var index=0;index<process.SupplyItem.Count;index++)
                    {
                        var supply=process.SupplyItem[index];
                        if(supply.RowId==0 || supply.ValueNullable is not { } row || row.Item.RowId==0)continue;
                        materials.Add(new(row.Item.RowId,process.SetQuantity[index],process.SetsRequired[index]));
                    }
                    if(materials.Count>0)phases.Add(new(phases.Count+1,materials));
                }
                if(phases.Count>0)parts.Add(new(part.CompanyCraftType.ValueNullable?.Name.ExtractText() ?? "",phases));
            }
            if(parts.Count>0)projects.Add(new(sequence.RowId,sequence.ResultItem.RowId,sequence.CompanyCraftType.ValueNullable?.Name.ExtractText() ?? "",parts,sequence.CompanyCraftDraft.RowId,sequence.Order,sequence.Category));
        }
        return projects;
    }
}
