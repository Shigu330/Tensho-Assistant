namespace TenshoAssistant.Beastmaster;

/// <summary>
/// X-587 (external audit A03 of 18/11): the beast plans' file (BeastTeams.json), read before anything is written to it. Before, a read that
/// failed (the file held an instant by another program, damaged JSON) left empty plans marked read, and the next ranks read saved them over
/// the player's file. Now: absent, new plans; held by another program, nothing read and nothing written, read again every
/// <see cref="RetryEvery"/>; damaged, set aside under a dated name (Discoveries.DiscoveryFiles.SetAsideName, never over one set aside
/// before), then new plans; when it cannot even be moved, nothing is written over it this session. AfterError1551Tests.
/// </summary>
public sealed class BeastPlansFile
{
    public static readonly TimeSpan RetryEvery=TimeSpan.FromSeconds(10);
    private bool read;private DateTimeOffset retryAt;

    /// <summary>True while the file is there but was neither read nor set aside: nothing is written over it.</summary>
    public bool Blocked { get; private set; }
    /// <summary>Where a damaged file was set aside (null: none).</summary>
    public string? SetAside { get; private set; }
    /// <summary>The plans may be written: the file was read (or was absent, or set aside) and nothing blocks it.</summary>
    public bool MayWrite=>read && !Blocked;

    /// <summary>The plans of the file when it is read now; null when read before, absent, set aside or still held (<see cref="Blocked"/>).</summary>
    public BeastTeams.SavedPlans? ReadIfDue(string file,DateTimeOffset now,Action<string,Exception>? broken=null)
    {
        if(read && !(Blocked && now>=retryAt))return null;
        read=true;
        Exception error;
        try
        {
            if(!File.Exists(file)){Blocked=false;return null;}
            var plans=BeastTeams.Read(File.ReadAllText(file)) ?? throw new InvalidDataException("Beast team plans unreadable.");
            Blocked=false;
            return plans;
        }
        catch(Exception caught){error=caught;}
        if(Discoveries.DiscoveryFiles.Away(error)){Blocked=false;return null;}
        Blocked=true;retryAt=now+RetryEvery;
        if(Discoveries.DiscoveryFiles.Busy(error))return null;
        try
        {
            var aside=Discoveries.DiscoveryFiles.SetAsideName(file,now);
            broken?.Invoke(aside,error);
            File.Move(file,aside,false);
            SetAside=aside;Blocked=false;
        }
        catch(Exception){}   // not moved: nothing written over it, read again later
        return null;
    }

    /// <summary>Another file to read (the desktop application's copy changed).</summary>
    public void Reset(){read=false;Blocked=false;SetAside=null;}
}
