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
        if(File.Exists(file))
            lock(gate)
                if(File.Exists(file))
                {
                    using(var open=opened.Open())
                        if(!TenshoDataDatabase.Converted(open,Family))
                            ReplaceAll(open,ReadFile().Select(pair=>(pair.Key,(IReadOnlyDictionary<uint,AchievementProgress>)pair.Value)).ToList(),now);
                    data.SetAside(file,now,"achievement progress");
                }
        return ReadRows(opened);
    }

    /// <summary>
    /// A character's progress saved whole (replaces that character's rows; <paramref name="all"/> is what the JSON file holds when it is still in
    /// use); refused while the file of before is not converted.
    /// </summary>
    public void Save(ulong character,IReadOnlyDictionary<uint,AchievementProgress> progress,Dictionary<ulong,Dictionary<uint,AchievementProgress>> all)
    {
        if(data.Database() is not { } opened){AtomicFile.Write(file,AchievementProgressStore.Write(all));return;}
        using var open=opened.Open();
        if(File.Exists(file) && !TenshoDataDatabase.Converted(open,Family))
            throw new InvalidOperationException("The achievement progress file of before is not converted yet; nothing written over it.");
        ReplaceAll(open,[(character,progress)],null);
    }

    /// <summary>The data file's journal folded in (at unload).</summary>
    public void Checkpoint()=>data.Checkpoint();

    private Dictionary<ulong,Dictionary<uint,AchievementProgress>> ReadFile()
    {
        if(!File.Exists(file))return [];
        return AchievementProgressStore.Read(File.ReadAllText(file)) ?? throw new InvalidDataException("Saved achievement progress unreadable.");
    }

    private static long Id(ulong character)=>unchecked((long)character);

    // Characters' rows replaced in one transaction; at a conversion (converted set) every character of the file of before is in the same transaction
    // as the « converted » mark: either the whole file is in the database, or none of it.
    private void ReplaceAll(SqliteConnection open,IReadOnlyList<(ulong Character,IReadOnlyDictionary<uint,AchievementProgress> Progress)> characters,DateTimeOffset? converted)
    {
        using var transaction=open.BeginTransaction();
        using(var delete=open.CreateCommand())
        using(var insert=open.CreateCommand())
        {
            delete.Transaction=transaction;delete.CommandText="DELETE FROM achievement_progress WHERE character=$character;";
            var gone=delete.Parameters.Add("$character",SqliteType.Integer);
            insert.Transaction=transaction;
            insert.CommandText="INSERT INTO achievement_progress(character,achievement,current,max,seen_at) VALUES($character,$achievement,$current,$max,$seen);";
            var who=insert.Parameters.Add("$character",SqliteType.Integer);var achievement=insert.Parameters.Add("$achievement",SqliteType.Integer);
            var current=insert.Parameters.Add("$current",SqliteType.Integer);var max=insert.Parameters.Add("$max",SqliteType.Integer);
            var seen=insert.Parameters.Add("$seen",SqliteType.Integer);
            foreach(var (character,progress) in characters)
            {
                gone.Value=Id(character);delete.ExecuteNonQuery();
                foreach(var (id,value) in progress)
                {
                    who.Value=Id(character);achievement.Value=(long)id;current.Value=(long)value.Current;max.Value=(long)value.Max;seen.Value=value.SeenAt.UtcTicks;
                    insert.ExecuteNonQuery();
                }
            }
        }
        if(converted is { } when)TenshoDataDatabase.MarkConverted(open,transaction,Family,when);
        DuringSave?.Invoke();
        transaction.Commit();
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
