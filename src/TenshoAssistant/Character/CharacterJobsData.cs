using Microsoft.Data.Sqlite;

namespace TenshoAssistant.Character;

/// <summary>One gear set as kept per character: its game number (1-based), name, job and average item level (no pieces).</summary>
public sealed record SavedGearset(int Number,string Name,uint Job,int ItemLevel);

/// <summary>A character's job levels (ClassJob row → level, only levels above 0) and gear sets, as read at one moment.</summary>
public sealed record CharacterJobs(ulong Character,DateTimeOffset SeenAt,IReadOnlyDictionary<uint,int> Levels,IReadOnlyList<SavedGearset> Gearsets)
{
    /// <summary>The same levels and sets (the time aside): nothing new to write.</summary>
    public bool SameContent(CharacterJobs? other)
        => other is not null && other.Character==Character && other.Levels.Count==Levels.Count
           && Levels.All(pair=>other.Levels.TryGetValue(pair.Key,out var level) && level==pair.Value)
           && other.Gearsets.SequenceEqual(Gearsets);
}

/// <summary>
/// X-218 (0.1228, user's request of 12/11, K-32): the job levels and gear sets of every character, copied at login (and again when they change
/// while logged in) and kept in the player's data file (TenshoData.db, tables character_jobs and character_gearsets), so Storage › Characters
/// shows them for the characters not logged in. A character's rows are replaced whole in one transaction, only by a newer reading (two
/// clients on the same character: the later reading wins, never an older one written over it). Erased with the character (« Erase this
/// character »: both tables are in <see cref="TenshoDataDatabase.CharacterTables"/>). No file of before: nothing to convert. When the data file
/// cannot be used, nothing is written (the page says « not recorded yet »).
/// </summary>
public sealed class CharacterJobsData
{
    /// <summary>Bounds of a reading (the game has about 40 jobs and 100 gear sets): anything beyond is refused, never written.</summary>
    public const int MaxLevels=128,MaxGearsets=128,MaxName=64;
    private readonly TenshoDataFile data;

    /// <summary>Test hook: run inside a save's transaction, after its rows and before the commit.</summary>
    internal Action? DuringSave { get; set; }

    public CharacterJobsData(TenshoDataFile data)=>this.data=data;

    /// <summary>Whether a reading is plausible enough to keep: a character, at least one level, every value within the game's bounds.</summary>
    public static bool Valid(CharacterJobs jobs)
        => jobs.Character!=0 && jobs.Levels.Count is > 0 and <= MaxLevels && jobs.Gearsets.Count<=MaxGearsets
           && jobs.Levels.All(pair=>pair.Key is > 0 and < 1000 && pair.Value is > 0 and <= 200)
           && jobs.Gearsets.All(set=>set.Number is > 0 and <= 1000 && set.Job is > 0 and < 1000 && set.ItemLevel is >= 0 and < 10_000 && set.Name.Length<=MaxName)
           && jobs.Gearsets.Select(set=>set.Number).Distinct().Count()==jobs.Gearsets.Count;

    /// <summary>
    /// Saves a reading; false when it was not written (data file not usable, reading refused, or an equal or newer reading already saved).
    /// Throws when the data file is there but unusable or the write fails.
    /// </summary>
    public bool Save(CharacterJobs jobs)
    {
        if(!Valid(jobs))return false;
        if(data.Database() is not { } opened)return false;
        using var open=opened.Open();
        using var transaction=open.BeginTransaction(deferred:false);
        var id=unchecked((long)jobs.Character);
        using(var newest=open.CreateCommand())
        {
            newest.Transaction=transaction;
            newest.CommandText="SELECT MAX(seen_at) FROM (SELECT seen_at FROM character_jobs WHERE character=$c UNION ALL SELECT seen_at FROM character_gearsets WHERE character=$c);";
            newest.Parameters.AddWithValue("$c",id);
            if(newest.ExecuteScalar() is long saved && saved>=jobs.SeenAt.UtcTicks)return false;
        }
        foreach(var table in new[]{"character_jobs","character_gearsets"})
        {
            using var delete=open.CreateCommand();delete.Transaction=transaction;
            delete.CommandText=$"DELETE FROM {table} WHERE character=$c;";delete.Parameters.AddWithValue("$c",id);delete.ExecuteNonQuery();
        }
        using(var insert=open.CreateCommand())
        {
            insert.Transaction=transaction;
            insert.CommandText="INSERT INTO character_jobs(character,job,level,seen_at) VALUES($c,$job,$level,$at);";
            insert.Parameters.AddWithValue("$c",id);insert.Parameters.AddWithValue("$at",jobs.SeenAt.UtcTicks);
            var job=insert.Parameters.Add("$job",SqliteType.Integer);var level=insert.Parameters.Add("$level",SqliteType.Integer);
            foreach(var (key,value) in jobs.Levels){job.Value=(long)key;level.Value=(long)value;insert.ExecuteNonQuery();}
        }
        using(var insert=open.CreateCommand())
        {
            insert.Transaction=transaction;
            insert.CommandText="INSERT INTO character_gearsets(character,number,job,item_level,name,seen_at) VALUES($c,$number,$job,$ilvl,$name,$at);";
            insert.Parameters.AddWithValue("$c",id);insert.Parameters.AddWithValue("$at",jobs.SeenAt.UtcTicks);
            var number=insert.Parameters.Add("$number",SqliteType.Integer);var job=insert.Parameters.Add("$job",SqliteType.Integer);
            var ilvl=insert.Parameters.Add("$ilvl",SqliteType.Integer);var name=insert.Parameters.Add("$name",SqliteType.Text);
            foreach(var set in jobs.Gearsets){number.Value=(long)set.Number;job.Value=(long)set.Job;ilvl.Value=(long)set.ItemLevel;name.Value=set.Name;insert.ExecuteNonQuery();}
        }
        DuringSave?.Invoke();
        transaction.Commit();
        return true;
    }

    /// <summary>Every character's last reading (empty when the data file is not used); throws when it is there but unusable.</summary>
    public Dictionary<ulong,CharacterJobs> Load()
    {
        var all=new Dictionary<ulong,CharacterJobs>();
        if(data.Database() is not { } opened)return all;
        using var open=opened.Open();
        var levels=new Dictionary<ulong,(Dictionary<uint,int> Levels,long At)>();
        using(var command=open.CreateCommand())
        {
            command.CommandText="SELECT character,job,level,seen_at FROM character_jobs;";
            using var reader=command.ExecuteReader();
            while(reader.Read())
            {
                var character=unchecked((ulong)reader.GetInt64(0));
                if(!levels.TryGetValue(character,out var found))levels[character]=found=([],0);
                found.Levels[(uint)reader.GetInt64(1)]=(int)reader.GetInt64(2);
                levels[character]=(found.Levels,Math.Max(found.At,reader.GetInt64(3)));
            }
        }
        var sets=new Dictionary<ulong,List<SavedGearset>>();
        using(var command=open.CreateCommand())
        {
            command.CommandText="SELECT character,number,name,job,item_level FROM character_gearsets ORDER BY character,number;";
            using var reader=command.ExecuteReader();
            while(reader.Read())
            {
                var character=unchecked((ulong)reader.GetInt64(0));
                if(!sets.TryGetValue(character,out var list))sets[character]=list=[];
                list.Add(new((int)reader.GetInt64(1),reader.GetString(2),(uint)reader.GetInt64(3),(int)reader.GetInt64(4)));
            }
        }
        foreach(var (character,(found,at)) in levels)
            all[character]=new(character,new DateTimeOffset(at,TimeSpan.Zero),found,sets.GetValueOrDefault(character) ?? []);
        return all;
    }
}
