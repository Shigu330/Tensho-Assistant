using Lumina.Excel.Sheets;

namespace TenshoAssistant.Items;

// Collection unlocked by an item, from its ItemAction: action 1322 teaches a mount, 853 a minion; Data[0] is the Mount or
// Companion row (verified on « Sifflet chocobo » → mount 1 and « Bombino » → minion 1, docs/evidence/0.141.0-itemaction-probe.log).
public static class ItemUnlockCopy
{
    private const uint MountAction=1322,CompanionAction=853;
    public static uint Mount(Item row)=>Data(row,MountAction);
    public static uint Companion(Item row)=>Data(row,CompanionAction);
    private static uint Data(Item row,uint action)=>row.ItemAction.RowId!=0 && row.ItemAction.IsValid && row.ItemAction.Value.Action.RowId==action ? row.ItemAction.Value.Data[0]:0u;
}
