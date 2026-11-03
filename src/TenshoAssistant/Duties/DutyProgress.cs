namespace TenshoAssistant.Duties;

/// <summary>Where the current character stands with one duty, as the game reports it.</summary>
public enum DutyStatus : byte { Locked, Unlocked, Completed }

/// <summary>Duty list filter of the Duty loot page (C-01).</summary>
public enum DutyProgressFilter { All, NotCompleted, UnlockedNotCompleted, Completed }

/// <summary>Status of each catalog duty by InstanceContent row, copied on the framework thread; a missing entry is unknown, never « locked ».</summary>
public sealed record DutyProgressState(IReadOnlyDictionary<uint,DutyStatus> ByInstance,DateTimeOffset ReadAt)
{
    public DutyStatus? Of(uint instanceId)=>ByInstance.TryGetValue(instanceId,out var status) ? status:null;
}

/// <summary>
/// g, C-01 (0.355): unlocked and completed duties in the duty catalog. Unknown statuses stay in every « not … » list (absence of proof
/// is not proof of absence) and only confirmed completions count as completed.
/// </summary>
public static class DutyProgress
{
    public static bool Matches(DutyProgressFilter filter,DutyStatus? status)=>filter switch
    {
        DutyProgressFilter.NotCompleted=>status is not DutyStatus.Completed,
        DutyProgressFilter.UnlockedNotCompleted=>status is DutyStatus.Unlocked or null,
        DutyProgressFilter.Completed=>status is DutyStatus.Completed,
        _=>true,
    };

    /// <summary>Completed duties among the given ones, and how many of them have a known status.</summary>
    public static (int Completed,int Known) Count(IEnumerable<DutyDefinition> duties,DutyProgressState progress)
    {
        int completed=0,known=0;
        foreach(var duty in duties)
            if(progress.Of(duty.InstanceId) is { } status){known++;if(status==DutyStatus.Completed)completed++;}
        return (completed,known);
    }
}
