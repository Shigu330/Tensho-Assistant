namespace TenshoAssistant.Reminders;

/// <summary>
/// N-01 (0.440): a held hunt bill as the game keeps it — its mark index (MobHuntOrderType row, 0-21), the MobHuntOrder row the bill holds
/// and the kills of its up to five targets (MobHunt.CurrentKills). docs/evidence/0.440-hunt-bills.md.
/// </summary>
public sealed record HuntBillProgress(byte MarkIndex,uint OrderRow,IReadOnlyList<int> Kills);

/// <summary>A target of a hunt order (MobHuntOrder subrow → MobHuntTarget): name, map and place in the game's language, kills needed, FATE.</summary>
public sealed record HuntOrderTarget(int Slot,string Name,uint MapId,string Place,int Needed,uint FateId);

public sealed record HuntBillTarget(HuntOrderTarget Target,int Killed)
{
    public bool Done=>Killed>=Target.Needed;
}

public sealed record HuntBillView(byte MarkIndex,string Bill,IReadOnlyList<HuntBillTarget> Targets)
{
    public bool Done=>Targets.Count>0 && Targets.All(target=>target.Done);
}

public static class HuntBills
{
    /// <summary>The held bills with their targets and kills; a bill whose order the data does not know is left out.</summary>
    public static IReadOnlyList<HuntBillView> Views(IEnumerable<HuntBillProgress>? held,Func<uint,IReadOnlyList<HuntOrderTarget>> order,Func<byte,string> bill)
        =>(held ?? []).Select(progress=>new HuntBillView(progress.MarkIndex,bill(progress.MarkIndex),
                order(progress.OrderRow).Where(target=>target.Needed>0 && target.Name.Length>0)
                    .Select(target=>new HuntBillTarget(target,target.Slot<progress.Kills.Count ? Math.Clamp(progress.Kills[target.Slot],0,target.Needed):0)).ToArray()))
            .Where(view=>view.Targets.Count>0).ToArray();
}
