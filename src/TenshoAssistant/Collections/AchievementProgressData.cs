using Microsoft.Data.Sqlite;

namespace TenshoAssistant.Collections;

/// <summary>
/// X-173 (0.1140, user's choice of 12/11): where the achievement progress seen is kept. Since this version, in the player's data file
/// (<see cref="TenshoDataFile"/>, TenshoData.db): one row per character and achievement, a character's rows replaced whole in one transaction at
/// each change (a failed save heals at the next one, as the whole file did before). X-574 (0.1793, the user's choice of 17/11): the file of
/// before (AchievementProgress.json) is no longer converted: still there while the data file is in use, it is left aside, never read, moved nor
/// removed (said once, <see cref="TenshoDataFile.LeftAside"/>). A data file that is there but unusable is never replaced (nothing written this
/// session); when the data file cannot be created, the JSON file stays in use as before.
/// </summary>
public sealed class AchievementProgressData
{
    private readonly string file;
    private readonly TenshoDataFile data;

    /// <summary>Test hook: run inside a save's transaction, after its rows and before the commit (a stop or an error there).</summary>
    internal Action? DuringSave { get; set; }

    public AchievementProgressData(string file,TenshoDataFile data){this.file=file;this.data=data;}

    /// <summary>« sqlite », « json » or « blocked » (see <see cref="TenshoDataFile.Mode"/>).</summary>
    public string Mode=>data.Mode;

    /// <summary>Everything seen, per character (throws when it cannot be read: the caller then never writes over it).</summary>
    public Dictionary<ulong,Dictionary<uint,AchievementProgress>> Load(DateTimeOffset now)
    {
        if(data.Database() is not { } opened)return ReadFile();
        // X-574 (0.1793): a file of before still there is left aside, never read (the progress is the data file's).
        if(File.Exists(file))data.LeftAside("Achievement progress",Path.GetFileName(file));
        return ReadRows(opened);
    }

    /// <summary>
    /// A character's progress saved (<paramref name="all"/> is what the JSON file holds when it is still in use). X-574 (0.1793): with the data file,
    /// a file of before is neither read nor written. X-182 (0.1169, night review N9): each achievement written only when newer than the one saved
    /// (seen later), never the character's rows deleted: a client left open with an older reading never takes back what another client saw since.
    /// </summary>
    public void Save(ulong character,IReadOnlyDictionary<uint,AchievementProgress> progress,Dictionary<ulong,Dictionary<uint,AchievementProgress>> all)
    {
        if(data.Database() is not { } opened){AtomicFile.Write(file,AchievementProgressStore.Write(all));return;}
        using var open=opened.Open();
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

    // Rows written in the caller's transaction, each only when newer than the one saved (X-182).
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
