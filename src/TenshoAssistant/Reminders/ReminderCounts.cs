using TenshoAssistant.FreeCompany;

namespace TenshoAssistant.Reminders;

public enum FinishedKind { Ventures, Voyages, SquadronMission, SquadronTraining, Garden }

/// <summary>
/// 0.455 (user request, in-game campaign): what waits for the player, for the Reminders icon of the server bar: retainer ventures and vessels back,
/// squadron mission and training over, garden beds ripe (kinds with nothing finished are left out); 0.458: and the rows still to do.
/// </summary>
public static class ReminderCounts
{
    /// <summary>0.458: the Reminders rows with a « done » box: what stays to do until it is ticked or the game resets it.</summary>
    public static readonly IReadOnlyList<string> CheckedRows=["Supply","Deliveries","Fashion","Allied","HuntBills","ChallengeLog","DomanEnclave"];
    /// <summary>0.458: every row the player can show or hide in the server bar: the checked rows and the timers that finish.</summary>
    public static readonly IReadOnlyList<string> BarRows=[..CheckedRows,nameof(FinishedKind.Ventures),nameof(FinishedKind.Voyages),nameof(FinishedKind.SquadronMission),nameof(FinishedKind.SquadronTraining)];

    /// <summary>The last reset of a checked row, as the Reminders page counts it.</summary>
    public static DateTimeOffset LastReset(string row,DateTimeOffset now)=>row switch
    {
        "Supply"=>ResetSchedule.PreviousDaily(now,ResetSchedule.SupplyHour),
        "Allied"=>ResetSchedule.PreviousDaily(now,ResetSchedule.DailyHour),
        _=>ResetSchedule.PreviousWeekly(now,ResetSchedule.WeeklyDay,ResetSchedule.WeeklyHour),
    };

    /// <summary>0.558 (T17): the rows the game itself says are done: its counter was read at 0. An unknown counter says nothing.</summary>
    public static IReadOnlyList<string> DoneByGame(uint? alliedLeft,int? deliveriesLeft)
    {
        var rows=new List<string>();
        if(alliedLeft==0)rows.Add("Allied");
        if(deliveriesLeft==0)rows.Add("Deliveries");
        return rows;
    }

    /// <summary>0.458 (user request, in-game campaign): the checked rows still to do since their reset, without those the player hid.</summary>
    public static IReadOnlyList<string> Todo(Func<string,DateTimeOffset?> doneAt,IReadOnlyCollection<string>? hidden,DateTimeOffset now)
        =>CheckedRows.Where(row=>hidden?.Contains(row)!=true && !ResetSchedule.DoneSince(doneAt(row),LastReset(row,now))).ToArray();

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
