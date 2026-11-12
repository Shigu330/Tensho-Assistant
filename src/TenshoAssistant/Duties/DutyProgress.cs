namespace TenshoAssistant.Duties;

/// <summary>Where the current character stands with one duty, as the game reports it.</summary>
public enum DutyStatus : byte { Locked, Unlocked, Completed }

/// <summary>Duty list filter of the Duty loot page (C-01).</summary>
public enum DutyProgressFilter { All, NotCompleted, UnlockedNotCompleted, Completed }

/// <summary>Status of each catalog duty by InstanceContent row, copied on the framework thread; a missing entry is unknown, never « locked ».</summary>
/// <summary>X-106 (0.990): FromDisk = the state saved for the last character, shown while logged out.</summary>
public sealed record DutyProgressState(IReadOnlyDictionary<uint,DutyStatus> ByInstance,DateTimeOffset ReadAt,bool FromDisk=false)
{
    public DutyStatus? Of(uint instanceId)=>ByInstance.TryGetValue(instanceId,out var status) ? status:null;
}

/// <summary>
/// g, C-01 (0.355): unlocked and completed duties in the duty catalog. Unknown statuses stay in every « not … » list (absence of proof
/// is not proof of absence) and only confirmed completions count as completed.
/// </summary>
public static class DutyProgress
{
    /// <summary>X-106 (0.990, user choice DEC-045, from C-01): the states saved per character (content id), with when they were read.</summary>
    public sealed record Saved(Dictionary<uint,DutyStatus> ByInstance,DateTimeOffset ReadAt);
    public static string Write(Dictionary<ulong,Saved> all,ulong last)=>System.Text.Json.JsonSerializer.Serialize(new SavedFile(all,last));
    public static (Dictionary<ulong,Saved> All,ulong Last)? Read(string text)
    {
        try{return System.Text.Json.JsonSerializer.Deserialize<SavedFile>(text) is { All:not null } file ? (file.All,file.Last):null;}
        catch(System.Text.Json.JsonException){return null;}
    }
    public sealed record SavedFile(Dictionary<ulong,Saved> All,ulong Last);

    /// <summary>
    /// X-119 (0.1151): the state a saved file holds for its last character (what the game shows logged out, and the desktop application from its
    /// copy), null when it holds none; throws when the text does not read.
    /// </summary>
    public static DutyProgressState? LastShown(string text)=>Read(text) is not { } read ? throw new InvalidDataException("Saved duty progress unreadable.")
        : read.All.TryGetValue(read.Last,out var last) ? new(last.ByInstance,last.ReadAt,true):null;

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
