using Microsoft.Data.Sqlite;

namespace TenshoAssistant.Collections;

/// <summary>X-274 (0.1387): one complete reading of a character's titles or achievements: the ids obtained, and when the game had sent the list.</summary>
public sealed record CompletionList(ulong Character,CollectionKind Kind,DateTimeOffset ReadAt,IReadOnlyList<uint> Ids)
{
    /// <summary>The ids as a set (built once per list).</summary>
    public HashSet<uint> Obtained { get; } = [.. Ids];
}

/// <summary>
/// X-274 (0.1387, user's request of 13/11): the title and achievement lists as the game last sent them completely (the game sends them only once
/// their window was opened), kept per character in the player's data file (TenshoData.db, table completion_lists, one row per character and
/// kind, the ids obtained and the reading's date), so the Titles and Achievements pages show the last reading after a reload instead of
/// starting from nothing. A row is replaced only by a newer reading (two clients on one character: the later reading wins). Erased with the
/// character (« Erase this character »: the table is in <see cref="TenshoDataDatabase.CharacterTables"/>). When the data file cannot be used,
/// nothing is kept (the pages ask for the game's window as before).
/// </summary>
public sealed class CompletionListsData
{
    /// <summary>Bound of a reading (the game has about 1 000 titles and 3 500 achievements): anything beyond is refused, never written.</summary>
    public const int MaxIds=20_000;
    private readonly TenshoDataFile data;

    public CompletionListsData(TenshoDataFile data)=>this.data=data;

    /// <summary>A reading worth keeping: a character, a title or achievement list, ids within bounds and each once.</summary>
    public static bool Valid(CompletionList list)
        => list.Character!=0 && list.Kind is CollectionKind.Title or CollectionKind.Achievement && list.Ids.Count<=MaxIds
           && list.Ids.All(id=>id>0) && list.Obtained.Count==list.Ids.Count;

    /// <summary>
    /// Saves a reading; false when it was not written (data file not used, reading refused, or an equal or newer reading already saved).
    /// Throws when the data file is there but unusable or the write fails.
    /// </summary>
    public bool Save(CompletionList list)
    {
        if(!Valid(list))return false;
        if(data.Database() is not { } opened)return false;
        using var open=opened.Open();
        using var transaction=open.BeginTransaction(deferred:false);
        using(var newest=open.CreateCommand())
        {
            newest.Transaction=transaction;
            newest.CommandText="SELECT read_at FROM completion_lists WHERE character=$c AND kind=$k;";
            newest.Parameters.AddWithValue("$c",unchecked((long)list.Character));newest.Parameters.AddWithValue("$k",(long)list.Kind);
            if(newest.ExecuteScalar() is long saved && saved>=list.ReadAt.UtcTicks)return false;
        }
        using(var write=open.CreateCommand())
        {
            write.Transaction=transaction;
            write.CommandText="INSERT OR REPLACE INTO completion_lists(character,kind,read_at,ids) VALUES($c,$k,$at,$ids);";
            write.Parameters.AddWithValue("$c",unchecked((long)list.Character));write.Parameters.AddWithValue("$k",(long)list.Kind);
            write.Parameters.AddWithValue("$at",list.ReadAt.UtcTicks);
            write.Parameters.AddWithValue("$ids",string.Join(',',list.Ids.Order().Select(id=>id.ToString(System.Globalization.CultureInfo.InvariantCulture))));
            write.ExecuteNonQuery();
        }
        transaction.Commit();
        return true;
    }

    /// <summary>One character's last readings, by kind (empty when the data file is not used); a row that cannot be read is left out.</summary>
    public Dictionary<CollectionKind,CompletionList> Load(ulong character)
    {
        var found=new Dictionary<CollectionKind,CompletionList>();
        if(character==0 || data.Database() is not { } opened)return found;
        using var open=opened.Open();
        using var command=open.CreateCommand();
        command.CommandText="SELECT kind,read_at,ids FROM completion_lists WHERE character=$c;";
        command.Parameters.AddWithValue("$c",unchecked((long)character));
        using var reader=command.ExecuteReader();
        while(reader.Read())
        {
            var kind=(CollectionKind)reader.GetInt64(0);
            var text=reader.GetString(2);
            var ids=new List<uint>();
            var readable=true;
            foreach(var part in text.Split(',',StringSplitOptions.RemoveEmptyEntries))
            {
                if(!uint.TryParse(part,System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out var id)){readable=false;break;}
                ids.Add(id);
            }
            var list=new CompletionList(character,kind,new DateTimeOffset(reader.GetInt64(1),TimeSpan.Zero),ids);
            if(readable && Valid(list))found[kind]=list;
        }
        return found;
    }

    /// <summary>
    /// Pure: a fresh entry (the game has not sent its list this session) with the last reading: obtained stays obtained; not in the reading
    /// counts as not obtained while the reading is younger than the « Titles and achievements » cache duration; a fresh positive always wins.
    /// </summary>
    public static CollectionEntry Apply(CollectionEntry fresh,CompletionList? list,bool keepNegative)
    {
        if(list is null || fresh.ConfirmedUnlocked || fresh.Kind!=list.Kind)return fresh;
        if(list.Obtained.Contains(fresh.Id))return fresh with {ConfirmedUnlocked=true,NegativeConfirmed=false};
        return keepNegative && !fresh.NegativeConfirmed ? fresh with {NegativeConfirmed=true}:fresh;
    }
}
