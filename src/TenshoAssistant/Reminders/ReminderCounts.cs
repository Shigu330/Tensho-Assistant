using TenshoAssistant.FreeCompany;

namespace TenshoAssistant.Reminders;

public enum FinishedKind { Ventures, Voyages, SquadronMission, SquadronTraining, Garden }

/// <summary>
/// 0.455 (user request, in-game campaign): what is finished and waits for the player, for the Reminders icon of the server bar: retainer
/// ventures and vessels back, squadron mission and training over, garden beds ripe. Kinds with nothing finished are left out.
/// </summary>
public static class ReminderCounts
{
    public static IReadOnlyList<(FinishedKind Kind,int Count)> Finished(IEnumerable<DateTimeOffset?>? ventures,IEnumerable<VoyageVessel>? vessels,
        (ushort Id,DateTimeOffset? Done)? mission,(ushort Id,DateTimeOffset? Done)? training,IEnumerable<GardenBed>? beds,DateTimeOffset now)
    {
        var list=new List<(FinishedKind,int)>();
        void Add(FinishedKind kind,int count){if(count>0)list.Add((kind,count));}
        static bool Over((ushort Id,DateTimeOffset? Done)? timer,DateTimeOffset at)=>timer is { Id:not 0, Done:{ } done } && done<=at;
        Add(FinishedKind.Ventures,ventures?.Count(done=>done is { } at && at<=now) ?? 0);
        Add(FinishedKind.Voyages,vessels?.Count(vessel=>Voyages.StateOf(vessel,now)==VoyageState.Returned) ?? 0);
        Add(FinishedKind.SquadronMission,Over(mission,now) ? 1:0);
        Add(FinishedKind.SquadronTraining,Over(training,now) ? 1:0);
        Add(FinishedKind.Garden,beds?.Count(bed=>GardenJournal.State(bed,now)==GardenState.Ripe) ?? 0);
        return list;
    }
}
