using Dalamud.Bindings.ImGui;
using TenshoAssistant.Inventory;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// X-216 (0.1789, the user's request of 12/11, K-26), second part: Collections › Glamour: the glamour collection of the character logged in,
// piece by piece: every equipable piece with a look held in the storages read (glamour dresser, armoire, bags, armoury, saddlebags,
// retainers), where it is, and the other pieces of the same look. From the index of « same look » (LookIndexNow, built once per change of
// those storages); the rows built again only when the index, the filters or the language change. Nothing is moved.
internal sealed partial class MainWindow
{
    private bool collectionGlamourTab;
    private string glamourQuery="";
    private EquipmentSlots glamourSlot=EquipmentSlots.None;
    private (LookIndex Index,EquipmentSlots Slot,string Query,object? Items,int Language)? glamourKey;
    private IReadOnlyList<(LookPiece Piece,string Name,EquipmentSlots Slot)> glamourRows=[];
    private int glamourTotal,glamourLooks;
    private const int GlamourShown=500;
    // The slot a piece is shown under: the first of its slots in this order.
    private static readonly EquipmentSlots[] GlamourSlots=[EquipmentSlots.MainHand,EquipmentSlots.OffHand,EquipmentSlots.Head,EquipmentSlots.Body,EquipmentSlots.Hands,
        EquipmentSlots.Legs,EquipmentSlots.Feet,EquipmentSlots.Ears,EquipmentSlots.Neck,EquipmentSlots.Wrists,EquipmentSlots.Ring];

    private static EquipmentSlots GlamourSlotOf(EquipmentSlots slots)=>GlamourSlots.FirstOrDefault(slot=>slots.HasFlag(slot));

    private void DrawGlamourCollection()
    {
        ImGui.TextWrapped(localization.Get("GlamourCollection.Help"));
        var character=bankBridge.State.CharacterId;
        if(character==0){ImGui.TextDisabled(localization.Get("BankUi.LoginBank"));return;}
        if(LookIndexNow() is not { } index){EmptyState("GlamourCollection.NotReady");return;}
        if(!bank.State.Entries.Any(entry=>entry.Key.CharacterId==character && entry.Key.Kind==BankStorageKind.GlamourDresser))
            ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("StorageReport.NoDresser"));
        ImGui.SetNextItemWidth(240);
        ImGui.InputTextWithHint("##GlamourSearch",localization.Get("GlamourCollection.Search"),ref glamourQuery,64);
        Tip("GlamourCollection.SearchTip");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(160);
        if(ImGui.BeginCombo("##GlamourSlot",glamourSlot==EquipmentSlots.None ? localization.Get("ItemFilters.Any"):localization.Get("ItemFilters.Slot"+glamourSlot)))
        {
            try
            {
                if(ImGui.Selectable(localization.Get("ItemFilters.Any")+"##GlamourSlotAny",glamourSlot==EquipmentSlots.None))glamourSlot=EquipmentSlots.None;
                foreach(var slot in GlamourSlots)if(ImGui.Selectable(localization.Get("ItemFilters.Slot"+slot)+"##GlamourSlot"+slot,glamourSlot==slot))glamourSlot=slot;
            }
            finally{ImGui.EndCombo();}
        }
        Tip("GlamourCollection.SlotTip");
        var items=itemCatalog.State.Catalog;
        var key=(index,glamourSlot,glamourQuery.Trim(),(object?)items,localization.Revision);
        if(glamourKey!=key)
        {
            glamourKey=key;
            var all=index.Collection().Select(piece=>(Piece:piece,Item:items?.Find(piece.Item))).ToArray();
            glamourTotal=all.Length;
            var query=key.Item3;
            var rows=all.Select(row=>(row.Piece,Name:row.Item?.Name ?? $"#{row.Piece.Item}",Slot:GlamourSlotOf(row.Item?.Equipment ?? EquipmentSlots.None)))
                .Where(row=>glamourSlot==EquipmentSlots.None || row.Slot==glamourSlot)
                .Where(row=>query.Length==0 || SearchText.Contains(row.Name,query,StringComparison.CurrentCultureIgnoreCase))
                .OrderBy(row=>row.Slot==EquipmentSlots.None ? int.MaxValue:Array.IndexOf(GlamourSlots,row.Slot)).ThenBy(row=>row.Name,StringComparer.CurrentCultureIgnoreCase).ThenBy(row=>row.Piece.Item)
                .ToArray();
            glamourRows=rows;
            glamourLooks=rows.Select(row=>LookOf(row.Piece.Item) is { } look ? (look.Main,look.Sub,look.Slot):default).Distinct().Count();
        }
        ImGui.TextDisabled(localization.Format("GlamourCollection.Count",glamourRows.Count,glamourLooks,glamourTotal));
        if(glamourRows.Count==0){EmptyState("GlamourCollection.None");return;}
        if(!TenshoAssistant.UI.UiComponents.BeginDataTable("GlamourCollection",4,scrollHeight:Math.Max(200f,ImGui.GetContentRegionAvail().Y)))return;
        try
        {
            TenshoAssistant.UI.UiComponents.HeadersRow([localization.Get("GlamourCollection.ColumnSlot"),localization.Get("GlamourCollection.ColumnPiece"),
                localization.Get("GlamourCollection.ColumnWhere"),localization.Get("GlamourCollection.ColumnSameLook")],column=>false);
            foreach(var (piece,name,slot) in glamourRows.Take(GlamourShown))
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();ImGui.TextDisabled(slot==EquipmentSlots.None ? "—":localization.Get("ItemFilters.Slot"+slot));
                ImGui.TableNextColumn();ImGui.TextUnformatted(name);
                if(ImGui.IsItemHovered())DrawItemInfoTooltip(piece.Item);
                ImGui.TableNextColumn();ImGui.TextUnformatted(piece.Places.Count<=3 ? string.Join(", ",piece.Places):string.Join(", ",piece.Places.Take(3))+" …");
                if(piece.Places.Count>3)if(TipHovered())TipText(string.Join(", ",piece.Places));
                ImGui.TableNextColumn();
                if(piece.SameLook.Count==0){ImGui.TextDisabled("—");continue;}
                ImGui.TextColored(Warning,localization.Format("GlamourCollection.SameLookCount",piece.SameLook.Count));
                if(TipHovered())TipText(string.Join("\n",piece.SameLook.Select(other=>items?.Find(other)?.Name ?? $"#{other}")));
            }
        }
        finally{ImGui.EndTable();}
        if(glamourRows.Count>GlamourShown)ImGui.TextDisabled(localization.Format("GlamourCollection.More",glamourRows.Count-GlamourShown));
    }
}
