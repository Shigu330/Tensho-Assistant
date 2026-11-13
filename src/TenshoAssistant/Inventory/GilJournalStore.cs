using System.Globalization;
using Microsoft.Data.Sqlite;

namespace TenshoAssistant.Inventory;

/// <summary>
/// X-173 (0.1139, user's choice of 12/11): where the gil journal is kept. Since this version, in the player's data file
/// (<see cref="TenshoDataDatabase"/>, <c>TenshoData.db</c>): one row per entry, a character's journal replaced whole in one transaction at each
/// save (at most once a minute), so a stop in the middle leaves the last save whole. The file of before (GilJournal/&lt;id&gt;.json) is converted
/// the first time its character is loaded: its rows and a « converted » mark written in one transaction, then the file moved, never removed, into
/// GilJournal/before-sqlite-yyyyMMdd-HHmmss/. A file that cannot be read is left where it is and the journal is not written (as before). A data
/// file that exists but cannot be opened (SQLite not loaded, damaged, newer layout) is never replaced: the journal is unavailable and nothing is
/// written this session; when the data file cannot be created at all, the JSON files stay in use as before.
/// </summary>
public sealed class GilJournalStore
{
    private readonly string folder;
    private readonly TenshoDataFile data;
    private readonly object gate=new();

    /// <summary>Test hook: run inside a save's transaction, after its rows and before the commit (a stop or an error there).</summary>
    internal Action? DuringSave { get; set; }

    /// <summary>0.1140: the plugin's one data file, shared with the other families.</summary>
    public GilJournalStore(string folder,TenshoDataFile data){this.folder=folder;this.data=data;}
    public GilJournalStore(string folder,string databasePath,Action<Exception,string> warn,Action<string>? info=null):this(folder,new TenshoDataFile(databasePath,warn,info)){}

    /// <summary>The file of before of a character (local character id, never an Account ID).</summary>
    public static string FileName(ulong character)=>character.ToString(CultureInfo.InvariantCulture)+".json";

    /// <summary>« sqlite », « json » (data file could not be created) or « blocked » (data file there but unusable); opens it on first use.</summary>
    public string Mode=>data.Mode;

    /// <summary>A character's journal, oldest first (throws when it cannot be read: the caller then never writes over it).</summary>
    public List<GilEntry> Load(ulong character,DateTimeOffset now)
    {
        var json=Path.Combine(folder,FileName(character));
        if(data.Database() is not { } opened)return GilJournal.Read(json);
        Convert(opened,character,json,now);
        return ReadRows(opened,character);
    }

    /// <summary>
    /// A character's file of before converted (at a load, and at a save when it was held at the load); true when nothing is left to convert.
    /// Relay 37 (0.1205): a file held by another program at the load (read nothing, nothing marked) left the session without any save of that
    /// journal; the next save converts it now that it may be free. One that cannot be read still throws: the journal is never written over it.
    /// </summary>
    private bool Convert(TenshoDataDatabase opened,ulong character,string json,DateTimeOffset now)
    {
        if(!File.Exists(json))return true;
        lock(gate)
        {
            if(!File.Exists(json))return true;
                {
                    var marked=true;
                    using(var open=opened.Open())
                    {
                        if(ConvertedAt(open,character) is not { } converted)
                        {
                            if(TenshoDataFile.StillThere(()=>GilJournal.Read(json)) is { } found)WriteRows(open,character,found,now);
                            else marked=false;
                        }
                        // X-182 (0.1169, night review N3): written again after the conversion (an older version still running): merged.
                        else if(TenshoDataFile.WrittenAgain(json,converted) && TenshoDataFile.StillThere(()=>GilJournal.Read(json)) is { } again)
                            WriteRows(open,character,Merge(ReadRows(opened,character),again),null);
                    }
                    if(marked)data.SetAside(json,now,"gil journal");
                    return marked;
                }
        }
    }

    /// <summary>A character's journal saved whole (replaces what was there); refused while its file of before is not converted.</summary>
    public void Save(ulong character,IReadOnlyList<GilEntry> entries)
    {
        var json=Path.Combine(folder,FileName(character));
        if(data.Database() is not { } opened){GilJournal.Write(json,entries);return;}
        using var open=opened.Open();
        if(File.Exists(json) && !Converted(open,character))
        {
            // Relay 37 (0.1205): held at the load, converted at the first save that finds it free; this save replaces the character's rows,
            // so what the file held (never loaded this session) is merged in, never dropped. Still held: refused as before.
            if(!TenshoDataFile.ConvertBeforeSave(()=>Convert(opened,character,json,DateTimeOffset.UtcNow),"gil journal"))
                throw new InvalidOperationException("The gil journal file of before is not converted yet; nothing written over it.");
            entries=Merge(ReadRows(opened,character),entries);
        }
        WriteRows(open,character,entries,null);
    }

    /// <summary>
    /// X-119 with X-173 (0.1147): the character whose journal holds the newest entry, or null (none, or the JSON files in use): the desktop
    /// application, with no character logged in, shows that one (choice noted in the plugin's decisions).
    /// </summary>
    public ulong? Latest()
    {
        if(data.Database() is not { } opened)return null;
        using var open=opened.Open();using var command=open.CreateCommand();
        command.CommandText="SELECT character FROM gil_entries ORDER BY at DESC, character LIMIT 1;";
        return command.ExecuteScalar() is long id ? unchecked((ulong)id):null;
    }

    /// <summary>The data file's journal folded in (at unload); nothing when the JSON files are in use.</summary>
    public void Checkpoint()=>data.Checkpoint();

    private static long Id(ulong character)=>unchecked((long)character);

    private static bool Converted(SqliteConnection open,ulong character)=>ConvertedAt(open,character) is not null;
    private static DateTimeOffset? ConvertedAt(SqliteConnection open,ulong character)
    {
        using var command=open.CreateCommand();
        command.CommandText="SELECT at FROM gil_converted WHERE character=$character;";
        command.Parameters.AddWithValue("$character",Id(character));
        return command.ExecuteScalar() is long at ? new DateTimeOffset(at,TimeSpan.Zero):null;
    }

    /// <summary>X-182 (0.1169): two journals of a character as one: every entry once (same time, change, balance, source and kind), in time order.</summary>
    public static List<GilEntry> Merge(IReadOnlyList<GilEntry> saved,IReadOnlyList<GilEntry> found)
        =>saved.Concat(found).Distinct().OrderBy(entry=>entry.At).ToList();

    private void WriteRows(SqliteConnection open,ulong character,IReadOnlyList<GilEntry> entries,DateTimeOffset? converted)
    {
        using var transaction=open.BeginTransaction();
        using(var delete=open.CreateCommand())
        {
            delete.Transaction=transaction;delete.CommandText="DELETE FROM gil_entries WHERE character=$character;";
            delete.Parameters.AddWithValue("$character",Id(character));
            delete.ExecuteNonQuery();
        }
        using(var insert=open.CreateCommand())
        {
            insert.Transaction=transaction;
            insert.CommandText="INSERT INTO gil_entries(character,seq,at,delta,balance,source,category) VALUES($character,$seq,$at,$delta,$balance,$source,$category);";
            insert.Parameters.AddWithValue("$character",Id(character));
            var seq=insert.Parameters.Add("$seq",SqliteType.Integer);var at=insert.Parameters.Add("$at",SqliteType.Integer);
            var delta=insert.Parameters.Add("$delta",SqliteType.Integer);var balance=insert.Parameters.Add("$balance",SqliteType.Integer);
            var source=insert.Parameters.Add("$source",SqliteType.Integer);var category=insert.Parameters.Add("$category",SqliteType.Integer);
            for(var index=0;index<entries.Count;index++)
            {
                var entry=entries[index];
                seq.Value=index;at.Value=entry.At.UtcTicks;delta.Value=entry.Delta;balance.Value=entry.Balance;
                source.Value=(int)entry.Source;category.Value=(int)entry.Category;
                insert.ExecuteNonQuery();
            }
        }
        if(converted is { } when)
        {
            using var mark=open.CreateCommand();
            mark.Transaction=transaction;mark.CommandText="INSERT OR REPLACE INTO gil_converted(character,at) VALUES($character,$at);";
            mark.Parameters.AddWithValue("$character",Id(character));mark.Parameters.AddWithValue("$at",when.UtcTicks);
            mark.ExecuteNonQuery();
        }
        DuringSave?.Invoke();
        transaction.Commit();
    }

    private static List<GilEntry> ReadRows(TenshoDataDatabase opened,ulong character)
    {
        using var open=opened.Open();using var command=open.CreateCommand();
        command.CommandText="SELECT at,delta,balance,source,category FROM gil_entries WHERE character=$character ORDER BY seq;";
        command.Parameters.AddWithValue("$character",Id(character));
        var entries=new List<GilEntry>();
        using var reader=command.ExecuteReader();
        while(reader.Read())
            entries.Add(new GilEntry(new DateTimeOffset(reader.GetInt64(0),TimeSpan.Zero),reader.GetInt64(1),reader.GetInt64(2),(GilSource)reader.GetInt32(3),(GilCategory)reader.GetInt32(4)));
        return entries;
    }
}
