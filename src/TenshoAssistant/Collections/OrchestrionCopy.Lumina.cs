using Lumina.Excel.Sheets;

namespace TenshoAssistant.Collections;

/// <summary>Native static catalogue identity and metadata, with only a positive observation supplied by the caller.</summary>
public static class OrchestrionCopy
{
    public static CollectionEntry? FromRows(Orchestrion row,OrchestrionUiparam? ui,OrchestrionCategory? category,bool confirmed)
    {
        var name=row.Name.ExtractText();
        if(row.RowId==0 || string.IsNullOrWhiteSpace(name))return null;
        uint icon=0;OrchestrionMetadata? metadata=null;
        if(ui is { } detail && detail.RowId==row.RowId)
        {
            var matching=category is { } found && found.RowId==detail.OrchestrionCategory.RowId;
            metadata=new(detail.OrchestrionCategory.RowId,matching ? category!.Value.Name.ExtractText():"",detail.Order,matching ? category!.Value.Order:null);
            if(matching)icon=category!.Value.Icon;
        }
        return new(CollectionKind.Orchestrion,row.RowId,name,icon,confirmed,Description:row.Description.ExtractText(),Music:metadata);
    }
}
