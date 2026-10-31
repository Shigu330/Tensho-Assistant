using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;
using TenshoAssistant.Items;

namespace TenshoAssistant;

/// <summary>
/// Duty loot (0.207): a ✓ (obtained) or ✗ (not obtained) on the icon of collection items, and filters by kind — mount, minion, Triple Triad
/// card, orchestrion roll, equipment, other — with « not obtained only », shared by the duty loot tool and the Duty Finder panel.
/// </summary>
internal sealed partial class MainWindow
{
    private int lootKind;private bool lootMissingOnly;
    private int dutyViewLootKind=-1;private bool dutyViewLootMissing;
    private static readonly string[] LootKinds=["All","Mount","Companion","TripleTriad","Orchestrion","Equipment","Other"];

    private static int LootKindOf(ItemDefinition? item)=>item is null ? 6
        :item.MountId!=0 ? 1:item.CompanionId!=0 ? 2:item.TripleTriadCardId!=0 ? 3:item.OrchestrionId!=0 ? 4:item.Equipment!=EquipmentSlots.None ? 5:6;

    private bool LootKindMatches(ItemDefinition? item)
    {
        if(lootKind!=0 && LootKindOf(item)!=lootKind)return false;
        // « Not obtained only »: collection items whose state is known and not obtained.
        return !lootMissingOnly || CollectionStateOf(CollectionOf(item))==CollectionAcquisition.NotAcquired;
    }

    private void DrawLootKindFilter(string id)
    {
        ImGui.SetNextItemWidth(160);
        if(ImGui.BeginCombo("##LootKind"+id,localization.Get("LootKind."+LootKinds[lootKind])))
        {
            try{for(var index=0;index<LootKinds.Length;index++)if(ImGui.Selectable(localization.Get("LootKind."+LootKinds[index]),lootKind==index))lootKind=index;}
            finally{ImGui.EndCombo();}
        }
        Tip("LootKind.Tip");
        ImGui.SameLine();
        ImGui.Checkbox(localization.Get("LootKind.Missing")+"##LootMissing"+id,ref lootMissingOnly);
        Tip("LootKind.MissingTip");
    }

    // Small mark on the bottom-right corner of the last drawn icon: green check when obtained, red cross when not; nothing when unknown
    // or not a collection item.
    private void DrawCollectionMark(ItemDefinition? item)
    {
        var state=CollectionStateOf(CollectionOf(item));
        if(state==CollectionAcquisition.Unknown || CollectionOf(item) is null)return;
        var max=ImGui.GetItemRectMax();var size=Math.Max(9f,(ImGui.GetItemRectSize().Y)*0.45f);
        var min=max-new Vector2(size);var draw=ImGui.GetWindowDrawList();
        var obtained=state==CollectionAcquisition.Acquired;
        draw.AddCircleFilled(min+new Vector2(size/2),size/2,ImGui.GetColorU32(obtained ? new Vector4(0.15f,0.55f,0.2f,0.95f):new Vector4(0.65f,0.12f,0.12f,0.95f)));
        var white=ImGui.GetColorU32(new Vector4(1,1,1,1));var pad=size*0.27f;var thick=Math.Max(1.5f,size*0.14f);
        if(obtained)
        {
            draw.AddLine(min+new Vector2(pad,size*0.52f),min+new Vector2(size*0.43f,size-pad),white,thick);
            draw.AddLine(min+new Vector2(size*0.43f,size-pad),min+new Vector2(size-pad,pad),white,thick);
        }
        else
        {
            draw.AddLine(min+new Vector2(pad),min+new Vector2(size-pad),white,thick);
            draw.AddLine(min+new Vector2(size-pad,pad),min+new Vector2(pad,size-pad),white,thick);
        }
    }
}
