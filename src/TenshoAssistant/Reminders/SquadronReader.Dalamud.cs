using FFXIVClientStructs.FFXIV.Client.Game;

namespace TenshoAssistant.Reminders;

/// <summary>
/// N-06 (0.451, DEC-027): reads the squadron from GcArmyManager.Data (FFXIVClientStructs; the game loads it only inside the Grand
/// Company barracks). Framework thread only, on the player's click; field reads only (no game function called), at most 8 members.
/// Null when the data is not loaded.
/// </summary>
internal static class SquadronReader
{
    public const int MaximumMembers=8;

    public static unsafe SquadronRaw? Read()
    {
        var manager=GcArmyManager.Instance();
        if(manager==null || manager->Data==null)return null;
        var data=manager->Data;
        var count=Math.Min((int)data->MemberCount,MaximumMembers);
        var members=new List<SquadronRawMember>(count);
        for(var slot=0;slot<count;slot++)
        {
            var member=data->Members[slot];
            if(member.ENpcResidentId==0 && member.ClassJob==0)continue;
            members.Add(new(slot,member.ENpcResidentId,member.Race,member.ClassJob,member.Level,member.Experience,(byte)member.Flags,
                member.ActiveTrait,member.ActiveTraitCond,member.InactiveTrait,member.InactiveTraitCond));
        }
        return new(data->Progress,data->CurrentExpedition,new(data->BonusPhysical,data->BonusMental,data->BonusTactical),members);
    }
}
