using Lumina.Excel.Sheets;

namespace TenshoAssistant.Items;

public static class ItemOrchestrionCopy
{
    public static uint Identity(Item row)=>row.AdditionalData.RowType==typeof(Orchestrion) ? row.AdditionalData.RowId:0;
}
