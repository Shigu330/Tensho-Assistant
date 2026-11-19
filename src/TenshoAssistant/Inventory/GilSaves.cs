namespace TenshoAssistant.Inventory;

/// <summary>
/// X-587 (external audit A14 of 18/11): the gil journal's saves, acknowledged only once written. Before, the journal was marked clean as
/// soon as its write was queued; a write that failed (data file held, disk full) was only logged, and without a new gil change before the
/// plugin closed or the character changed, its last entries were lost. Here each change is counted (<see cref="Changed"/>); a write holds
/// the count it copied and acknowledges it only once done (<see cref="Done"/>); a write that fails keeps its copy, written again at each
/// save (<see cref="Again"/>), after the character changed too, and given back as that character's journal if it comes back first
/// (<see cref="Failed"/>). The writes go one after the other (BackgroundWriter): a write that ends later never holds older entries.
/// </summary>
public sealed class GilSaves
{
    private readonly object gate=new();
    private long changes,written,generation;
    private readonly Dictionary<ulong,GilEntry[]> failed=[];

    /// <summary>Another character (or none): its count starts again; a write of the one before that ends later acknowledges nothing.</summary>
    public void Switch(){lock(gate){changes=written=0;generation++;}}
    /// <summary>The current journal changed.</summary>
    public void Changed(){lock(gate)changes++;}
    /// <summary>The current journal holds changes no finished write holds.</summary>
    public bool Due{get{lock(gate)return changes!=written;}}
    /// <summary>The count of changes the copy taken now holds.</summary>
    public long Stamp{get{lock(gate)return changes;}}
    /// <summary>The journal of a character whose last write failed (newer than the data file's), or null.</summary>
    public GilEntry[]? Failed(ulong id){lock(gate)return failed.GetValueOrDefault(id);}
    public int FailedCount{get{lock(gate)return failed.Count;}}

    /// <summary>The journals whose last write failed, to write again now: the current character's too, unless a newer copy of it goes now.</summary>
    public KeyValuePair<ulong,GilEntry[]>[] Again(ulong current,bool currentGoesNow)
    {
        lock(gate)return failed.Where(pair=>!(currentGoesNow && pair.Key==current)).ToArray();
    }

    /// <summary>
    /// The work of one write: runs <paramref name="write"/>, then forgets the character's failed copy and acknowledges <paramref name="stamp"/>
    /// (null: a copy written again) if the character is still the current one; a failure keeps <paramref name="copy"/> to write again and is
    /// thrown on, for the writer to log it.
    /// </summary>
    public Action Done(ulong id,GilEntry[] copy,long? stamp,Action write)
    {
        long gen;
        lock(gate)gen=generation;
        return ()=>
        {
            try{write();}
            catch(Exception){lock(gate)failed[id]=copy;throw;}
            lock(gate)
            {
                failed.Remove(id);
                if(stamp is { } done && gen==generation && done>written)written=done;
            }
        };
    }
}
