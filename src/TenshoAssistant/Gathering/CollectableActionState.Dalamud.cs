using FFXIVClientStructs.FFXIV.Client.Game;

namespace TenshoAssistant.Gathering;

/// <summary>0.483: whether the game would accept an action now, as it tells it (0: yes). Read only, on the game's thread (from the draw); nothing is used or sent. Null if the function is not resolved.</summary>
internal static class CollectableActionState
{
    public static unsafe uint? Status(uint action)
    {
        if((nint)ActionManager.MemberFunctionPointers.GetActionStatus==0)return null;
        var manager=ActionManager.Instance();
        return manager==null ? null:manager->GetActionStatus(ActionType.Action,action);
    }
}
