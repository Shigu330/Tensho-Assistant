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
            if(!File.Exists(file) && !File.Exists(AtomicFile.BackupOf(file))){Blocked=false;return null;}
            // X-833: the file, or its previous version (« .bak ») when it is missing or does not read; the damaged file kept aside.
            var plans=AtomicFile.ReadKept(file,path=>BeastTeams.Read(File.ReadAllText(path)) ?? throw new InvalidDataException("Beast team plans unreadable."),
                (aside,error)=>{if(aside!=file){SetAside=aside;broken?.Invoke(aside,error);}},now);
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
            File.Move(file,aside,false);
            SetAside=aside;Blocked=false;
            // X-618 (M1): said once moved (before, said before the move: a move that failed was said as done, again every 10 s).
            broken?.Invoke(aside,error);
        }
        catch(Exception){}   // not moved: nothing written over it, read again later (the window says it once: « could not be read »)
        return null;
    }

    /// <summary>
    /// X-617 (review of block 0.1551 to 0.1554, I1): the plans to keep, <paramref name="kept"/> being those in memory. Read while the file was
    /// held, they are joined to the file once it is read (BeastTeams.Join), never replaced by it; Save: something read meanwhile is to be
    /// written now that the file may be (joined, or the file gone or set aside meanwhile). Still held: the memory, nothing to write.
    /// </summary>
    public (BeastTeams.SavedPlans Plans, bool Save) Load(string file,BeastTeams.SavedPlans? kept,DateTimeOffset now,Action<string,Exception>? broken=null)
    {
        var held=Blocked && kept is not null;
        var plans=ReadIfDue(file,now,broken);
        if(plans is null)return (kept ?? new([],[]),held && MayWrite);
        if(!held)return (plans,false);
        var joined=BeastTeams.Join(plans,kept!);
        return (joined,BeastTeams.Write(joined)!=BeastTeams.Write(plans));
    }

    /// <summary>Another file to read (the desktop application's copy changed).</summary>
    public void Reset(){read=false;Blocked=false;SetAside=null;}
}
