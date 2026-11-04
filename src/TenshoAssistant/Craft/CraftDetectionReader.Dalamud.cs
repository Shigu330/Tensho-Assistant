using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace TenshoAssistant.Craft;

/// <summary>
/// 0.464 (user request, in-game campaign): what the simulator can read from the game instead of asking: the active food and medicine (Well Fed and Medicated on the player, their
/// Param naming the ItemFood row, see CraftConsumables.FromStatus) and whether the job is a specialist (PlayerState.IsMeisterFlag, only once resolved). Read from the draw (game thread).
/// </summary>
internal static class CraftDetectionReader
{
    public static unsafe CraftDetected Read(IObjectTable objects,uint job)
    {
        ushort? food=null,medicine=null;
        if(objects.LocalPlayer is { } local)
            foreach(var status in local.StatusList)
            {
                if(status.StatusId==CraftConsumables.WellFed)food=status.Param;
                else if(status.StatusId==CraftConsumables.Medicated)medicine=status.Param;
            }
        bool? specialist=null;
        var player=PlayerState.Instance();
        if(player!=null && player->IsLoaded && (nint)PlayerState.MemberFunctionPointers.IsMeisterFlag!=0)specialist=player->IsMeisterFlag(job);
        return new(food,medicine,specialist);
    }
}
