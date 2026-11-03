using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gathering;
using TenshoAssistant.Gear;

namespace TenshoAssistant;

// g, R-10 (0.411): on each gathering source, « Equip (job) » wears that job's gear set with the lowest number, on the player's click
// only, through the game's EquipGearset (GameGearsetReader.Equip, address-checked, 2 seconds apart). No set for the job: no button.
internal sealed partial class MainWindow
{
    // The job's set for « Go there » (0.412, setting GoThereEquip): same request as the button; nothing when the job has no set.
    private void EquipGatheringGearset(GatheringSource source)
    {
        if(GearsetSource is { } gearsets && GameGearsets.GatheringJob(source.TypeId,source.IsFishing) is { } job
           && gearsets.State?.Sets is { } sets && GameGearsets.ForJob(sets,job) is { } set)gearsets.Equip(set.Number);
    }

    private void DrawGatheringGearset(GatheringSource source)
    {
        if(GearsetSource is not { } gearsets || GameGearsets.GatheringJob(source.TypeId,source.IsFishing) is not { } job)return;
        gearsets.Want();
        if(gearsets.State?.Sets is not { } sets || GameGearsets.ForJob(sets,job) is not { } set)return;
        if(ImGui.SmallButton(localization.Format("GatheringDetails.Equip",JobName(job))))gearsets.Equip(set.Number);
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("GatheringDetails.EquipTip",set.Number,set.Name.Length>0 ? set.Name:JobName(job)));
    }
}
