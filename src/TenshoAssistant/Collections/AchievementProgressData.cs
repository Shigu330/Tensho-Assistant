using Microsoft.Data.Sqlite;

namespace TenshoAssistant.Collections;

/// <summary>
/// X-173 (0.1140, user's choice of 12/11): where the achievement progress seen is kept. Since this version, in the player's data file
/// (<see cref="TenshoDataFile"/>, TenshoData.db): one row per character and achievement, a character's rows replaced whole in one transaction at
/// each change (a failed save heals at the next one, as the whole file did before). The file of before (AchievementProgress.json) is converted
/// the first time it is loaded (its rows and a « converted » mark written together), then moved, never removed, into before-sqlite-&lt;date&gt;/
/// beside it. A file of before that cannot be read stays in place and nothing is written (as before); a data file that is there but unusable is
/// never replaced (nothing written this session); when the data file cannot be created, the JSON file stays in use as before.
/// </summary>
public sealed class AchievementProgressData
{
    public const string Family="achievements";
    private readonly string file;
    private readonly TenshoDataFile data;
    private readonly object gate=new();

    /// <summary>Test hook: run inside a save's transaction, after its rows and before the commit (a stop or an error there).</summary>
    internal Action? DuringSave { get; set; }

    public AchievementProgressData(string file,TenshoDataFile data){this.file=file;this.data=data;}

    /// <summary>« sqlite », « json » or « blocked » (see <see cref="TenshoDataFile.Mode"/>).</summary>
    public string Mode=>data.Mode;

    /// <summary>Everything seen, per character (throws when it cannot be read: the caller then never writes over it).</summary>
    public Dictionary<ulong,Dictionary<uint,AchievementProgress>> Load(DateTimeOffset now)
    {
        if(data.Database() is not { } opened)return ReadFile();
        Convert(opened,now);
        return ReadRows(opened);
    }

    /// <summary>
    /// The file of before converted (at a load, and at a save when it was held at the load); true when nothing is left to convert. Relay 37
    /// (0.1205): a file held by another program at the load (read nothing, nothing marked) left the session without any save of this family;
    /// the next save converts it now that it may be free. A file of before that cannot be read (damaged) is set aside whole, never written
    /// over nor removed, and the family goes on (the progress is seen again in game): it used to block every save for good (D-145).
    /// </summary>
    private bool Convert(TenshoDataDatabase opened,DateTimeOffset now)
    {
        if(!File.Exists(file))return true;
        lock(gate)
        {
            if(!File.Exists(file))return true;
                {
                    // X-182 (0.1169, night review N3): read in an immediate transaction; a file written again after the conversion (an older
                    // version still running) is merged the same way as a save: the newest reading of each achievement wins.
                    var marked=false;
                    using(var open=opened.Open())
                    using(var transaction=open.BeginTransaction(deferred:false))
                    {
                        var converted=TenshoDataDatabase.ConvertedAt(open,Family,transaction);
                        marked=converted is not null;
                        if(File.Exists(file) && (converted is null || TenshoDataFile.WrittenAgain(file,converted.Value))
                           && TenshoDataFile.StillThere(ReadOrSetAside) is { } found)
                        {
                            Upsert(open,transaction,found.Select(pair=>(pair.Key,(IReadOnlyDictionary<uint,AchievementProgress>)pair.Value)).ToList());
                            if(converted is null)TenshoDataDatabase.MarkConverted(open,transaction,Family,now);
                            marked=true;
                        }
                        DuringSave?.Invoke();
                        transaction.Commit();
                    }
                    if(marked)data.SetAside(file,now,"achievement progress");
                    return marked;
                }
        }
    }

    // D-145 (0.1205): a damaged file of before counts as nothing to add; Convert then marks the family and sets the file aside whole.
    private Dictionary<ulong,Dictionary<uint,AchievementProgress>> ReadOrSetAside()
    {
        try{return ReadFile();}
        catch(InvalidDataException){return [];}
    }

    /// <summary>
    /// A character's progress saved (<paramref name="all"/> is what the JSON file holds when it is still in use); refused while the file of before
    /// is not converted. X-182 (0.1169, night review N9): each achievement written only when newer than the one saved (seen later), never the
    /// character's rows deleted: a client left open with an older reading never takes back what another client saw since.
    /// </summary>
    public void Save(ulong character,IReadOnlyDictionary<uint,AchievementProgress> progress,Dictionary<ulong,Dictionary<uint,AchievementProgress>> all)
    {
        if(data.Database() is not { } opened){AtomicFile.Write(file,AchievementProgressStore.Write(all));return;}
        using var open=opened.Open();
        // Relay 37 (0.1205): held at the load, converted at the first save that finds it free; still held: refused as before.
        if(File.Exists(file) && !TenshoDataDatabase.Converted(open,Family) && !TenshoDataFile.ConvertBeforeSave(()=>Convert(opened,DateTimeOffset.UtcNow),"achievement progress"))
            throw new InvalidOperationException("The achievement progress file of before is not converted yet; nothing written over it.");
        using var transaction=open.BeginTransaction(deferred:false);
        Upsert(open,transaction,[(character,progress)]);
        DuringSave?.Invoke();
        transaction.Commit();
    }

    /// <summary>
    /// X-119 with X-173 (0.1150): the character whose progress was seen last (ties: the smaller id), or null when none was seen: the desktop
    /// application, with no character logged in, shows that one (as the gil journal, choice D-142).
    /// </summary>
    public static ulong? Latest(IReadOnlyDictionary<ulong,Dictionary<uint,AchievementProgress>> all)
        => all.Where(pair=>pair.Value.Count>0).OrderByDescending(pair=>pair.Value.Values.Max(seen=>seen.SeenAt)).ThenBy(pair=>pair.Key)
            .Select(pair=>(ulong?)pair.Key).FirstOrDefault();

    /// <summary>The data file's journal folded in (at unload).</summary>
    public void Checkpoint()=>data.Checkpoint();

    private Dictionary<ulong,Dictionary<uint,AchievementProgress>> ReadFile()
    {
        if(!File.Exists(file))return [];
        return AchievementProgressStore.Read(File.ReadAllText(file)) ?? throw new InvalidDataException("Saved achievement progress unreadable.");
    }

    private static long Id(ulong character)=>unchecked((long)character);

    // Rows written in the caller's transaction, each only when newer than the one saved (X-182); at a conversion the « converted » mark is in the
    // same transaction: either the whole file is in the database, or none of it.
    private static void Upsert(SqliteConnection open,SqliteTransaction transaction,IReadOnlyList<(ulong Character,IReadOnlyDictionary<uint,AchievementProgress> Progress)> characters)
    {
        using(var insert=open.CreateCommand())
        {
            insert.Transaction=transaction;
            insert.CommandText="INSERT INTO achievement_progress(character,achievement,current,max,seen_at) VALUES($character,$achievement,$current,$max,$seen) "
                +"ON CONFLICT(character,achievement) DO UPDATE SET current=excluded.current,max=excluded.max,seen_at=excluded.seen_at WHERE excluded.seen_at>achievement_progress.seen_at;";
            var who=insert.Parameters.Add("$character",SqliteType.Integer);var achievement=insert.Parameters.Add("$achievement",SqliteType.Integer);
            var current=insert.Parameters.Add("$current",SqliteType.Integer);var max=insert.Parameters.Add("$max",SqliteType.Integer);
            var seen=insert.Parameters.Add("$seen",SqliteType.Integer);
            foreach(var (character,progress) in characters)
            {
                foreach(var (id,value) in progress)
                {
                    who.Value=Id(character);achievement.Value=(long)id;current.Value=(long)value.Current;max.Value=(long)value.Max;seen.Value=value.SeenAt.UtcTicks;
                    insert.ExecuteNonQuery();
                }
            }
        }
    }

    private static Dictionary<ulong,Dictionary<uint,AchievementProgress>> ReadRows(TenshoDataDatabase opened)
    {
        using var open=opened.Open();using var command=open.CreateCommand();
        command.CommandText="SELECT character,achievement,current,max,seen_at FROM achievement_progress;";
        var all=new Dictionary<ulong,Dictionary<uint,AchievementProgress>>();
        using var reader=command.ExecuteReader();
        while(reader.Read())
        {
            var character=unchecked((ulong)reader.GetInt64(0));
            if(!all.TryGetValue(character,out var progress))all[character]=progress=[];
            progress[(uint)reader.GetInt64(1)]=new AchievementProgress((uint)reader.GetInt64(2),(uint)reader.GetInt64(3),new DateTimeOffset(reader.GetInt64(4),TimeSpan.Zero));
        }
        return all;
    }
}
