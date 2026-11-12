using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace TenshoAssistant.FreeCompany;

/// <summary>One voyage as read in the workshop: vessel, route, when it left and came back, and what it brought back.</summary>
public sealed record VoyageRecord(ulong HouseId,VesselKind Kind,int Slot,string Vessel,int Rank,DateTimeOffset RegisteredAt,DateTimeOffset? ReturnAt,
    IReadOnlyList<uint> Points,IReadOnlyList<string> Route,IReadOnlyList<VoyageLoot> Loot);

/// <summary>Real yield of one route (same sectors, same order): voyages read and the average count of each item per voyage.</summary>
public sealed record RouteYield(string Route,VesselKind Kind,int Voyages,IReadOnlyList<(uint ItemId,double PerVoyage)> Items);

/// <summary>
/// g, M-10 (0.379): the voyage loot history (user's choice: 180 days, shown apart from the community figures, export). One record per
/// voyage (house, kind, slot, registration time), updated while the same voyage is read again; a voyage is kept only once its loot is
/// known. Original code (ideas from Submarine Tracker, MIT, read only). One local file, 180 days and 5 000 voyages at most.
/// </summary>
public static class VoyageHistory
{
    public const int KeepDays=180,MaximumRecords=5000;

    /// <summary>Adds or updates the voyages of a workshop read; true when the history changed.</summary>
    public static bool Record(List<VoyageRecord> history,IEnumerable<VoyageVessel> vessels)
    {
        var changed=false;
        foreach(var vessel in vessels)
        {
            if(vessel.RegisteredAt is not { } registered || vessel.Loot.Count==0)continue;
            var record=new VoyageRecord(vessel.HouseId,vessel.Kind,vessel.Slot,vessel.Name,vessel.Rank,registered,vessel.ReturnAt,
                vessel.Points.ToArray(),vessel.Route.ToArray(),vessel.Loot.ToArray());
            var index=history.FindIndex(old=>old.HouseId==record.HouseId && old.Kind==record.Kind && old.Slot==record.Slot && old.RegisteredAt==record.RegisteredAt);
            if(index<0){history.Add(record);changed=true;}
            else if(!Same(history[index],record)){history[index]=record;changed=true;}
        }
        return changed;
    }
    public static bool Same(VoyageRecord a,VoyageRecord b)=>a.Vessel==b.Vessel && a.Rank==b.Rank && a.ReturnAt==b.ReturnAt
        && a.Points.SequenceEqual(b.Points) && a.Route.SequenceEqual(b.Route) && a.Loot.SequenceEqual(b.Loot);

    /// <summary>Records kept: the last 180 days (by registration), the newest 5 000 at most, oldest first.</summary>
    public static List<VoyageRecord> Prune(IEnumerable<VoyageRecord> records,DateTimeOffset now)
    {
        var kept=records.Where(record=>now-record.RegisteredAt<=TimeSpan.FromDays(KeepDays) && record.RegisteredAt<=now.AddDays(1)).OrderBy(record=>record.RegisteredAt).ToList();
        if(kept.Count>MaximumRecords)kept.RemoveRange(0,kept.Count-MaximumRecords);
        return kept;
    }

    /// <summary>Yield per route (submarines by sector list, airships by vessel kind only when no sector is known), most voyages first.</summary>
    public static IReadOnlyList<RouteYield> ByRoute(IEnumerable<VoyageRecord> records)
        =>records.GroupBy(record=>(record.Kind,Route:string.Join(" → ",record.Route)))
            .Select(group=>new RouteYield(group.Key.Route,group.Key.Kind,group.Count(),
                group.SelectMany(record=>record.Loot).GroupBy(loot=>loot.ItemId)
                    .Select(item=>(item.Key,item.Sum(loot=>(double)loot.Count)/group.Count())).OrderByDescending(item=>item.Item2).ToArray()))
            .OrderByDescending(route=>route.Voyages).ToArray();

    /// <summary>CSV export (one line per item brought back): return, vessel, kind, rank, route, item, count. Fields quoted.</summary>
    public static string Csv(IEnumerable<VoyageRecord> records,Func<uint,string> itemName)
    {
        var text=new StringBuilder("Registered,Returned,Vessel,Kind,Rank,Route,ItemId,Item,Count\n");
        foreach(var record in records)
            foreach(var loot in record.Loot)
                text.Append(string.Join(",",
                    Quote(record.RegisteredAt.ToString("u",CultureInfo.InvariantCulture)),Quote(record.ReturnAt?.ToString("u",CultureInfo.InvariantCulture) ?? ""),
                    Quote(record.Vessel),Quote(record.Kind.ToString()),record.Rank.ToString(CultureInfo.InvariantCulture),Quote(string.Join(" > ",record.Route)),
                    loot.ItemId.ToString(CultureInfo.InvariantCulture),Quote(itemName(loot.ItemId)),loot.Count.ToString(CultureInfo.InvariantCulture))).Append('\n');
        return text.ToString();
    }
    // Quoted, inner quotes doubled; a leading = + - @ is prefixed so a spreadsheet never reads a field as a formula.
    private static string Quote(string value)
    {
        var safe=value.Length>0 && "=+-@".Contains(value[0]) ? "'"+value:value;
        return "\""+safe.Replace("\"","\"\"")+"\"";
    }

    private static readonly JsonSerializerOptions Json=new(){MaxDepth=8};
    private sealed record FileDto(int Schema,VoyageRecord[] Records);

    /// <summary>Reads the history file (missing = empty; a bad one throws, so it is never overwritten by mistake).</summary>
    public static List<VoyageRecord> Read(string path)
    {
        if(!File.Exists(path))return [];
        if(new FileInfo(path).Length>16*1024*1024)throw new InvalidDataException("Voyage history too large.");
        var dto=JsonSerializer.Deserialize<FileDto>(File.ReadAllBytes(path),Json) ?? throw new InvalidDataException("Empty voyage history.");
        if(dto.Schema!=1 || dto.Records is null)throw new InvalidDataException("Unknown voyage history schema.");
        return dto.Records.ToList();
    }

    /// <summary>Writes the history through a temporary file.</summary>
    public static void Write(string path,IReadOnlyList<VoyageRecord> records)
    {
        AtomicFile.Write(path,JsonSerializer.SerializeToUtf8Bytes(new FileDto(1,records.ToArray()),Json));
    }
}

/// <summary>
/// X-173 (0.1148, user's choice of 12/11): where the voyage history is kept. Since this version, in the player's data file (<see cref="TenshoDataFile"/>,
/// TenshoData.db): one row per voyage, the history replaced whole in one transaction at each save (a stop in the middle leaves the last save
/// whole). The file of before (VoyageHistory.json) is converted the first time it is loaded (its rows and a « converted » mark written together),
/// then moved, never removed, into before-sqlite-&lt;date&gt;/ beside it. A file of before that cannot be read stays in place and nothing is written
/// (as before); a data file that is there but unusable is never replaced (nothing written this session); when the data file cannot be created, or
/// none is given, the JSON file stays in use as before.
/// </summary>
public sealed class VoyageHistoryData(string file,TenshoDataFile? data)
{
    public const string Family="voyages";
    private readonly object gate=new();
    private static readonly JsonSerializerOptions Json=new(){MaxDepth=8};

    /// <summary>Test hook: run inside a save's transaction, after its rows and before the commit (a stop or an error there).</summary>
    internal Action? DuringSave { get; set; }

    /// <summary>« sqlite », « json » or « blocked » (see <see cref="TenshoDataFile.Mode"/>); « json » without a data file.</summary>
    public string Mode=>data?.Mode ?? "json";

    /// <summary>
    /// The history, as saved (throws when it cannot be read: the caller then never writes over it). X-182 (0.1169, night review N3, N28): the
    /// file of before is read inside an immediate transaction (a second client waits, then finds it converted); found again after the conversion
    /// and written since (an older version still running), its voyages not in the database are added, never dropped.
    /// </summary>
    public List<VoyageRecord> Load(DateTimeOffset now)
    {
        if(data?.Database() is not { } opened)return VoyageHistory.Read(file);
        if(File.Exists(file))
            lock(gate)
                if(File.Exists(file))
                {
                    var marked=false;
                    using(var open=opened.Open())
                    using(var transaction=open.BeginTransaction(deferred:false))
                    {
                        var converted=TenshoDataDatabase.ConvertedAt(open,Family,transaction);
                        marked=converted is not null;
                        if(File.Exists(file) && (converted is null || TenshoDataFile.WrittenAgain(file,converted.Value))
                           && TenshoDataFile.StillThere(()=>VoyageHistory.Read(file)) is { } found)
                        {
                            Write(open,transaction,found,keep:true);
                            if(converted is null)TenshoDataDatabase.MarkConverted(open,transaction,Family,now);
                            marked=true;
                        }
                        DuringSave?.Invoke();
                        transaction.Commit();
                    }
                    if(marked)data.SetAside(file,now,"voyage history");
                }
        return ReadRows(opened);
    }

    /// <summary>
    /// X-182 (0.1169, night review N4): the voyages that changed written by their key (house, kind, slot, registration), the others left as they
    /// are (another client's company is never erased); then what is older than the history keeps (180 days, the newest 5 000) is removed. The
    /// JSON file, when it is still in use, is written whole from <paramref name="all"/>. Refused while the file of before is not converted.
    /// </summary>
    public void Save(IReadOnlyList<VoyageRecord> all,IReadOnlyList<VoyageRecord>? changed=null,DateTimeOffset? now=null)
    {
        if(data?.Database() is not { } opened){VoyageHistory.Write(file,all);return;}
        using var open=opened.Open();
        if(File.Exists(file) && !TenshoDataDatabase.Converted(open,Family))
            throw new InvalidOperationException("The voyage history file of before is not converted yet; nothing written over it.");
        using var transaction=open.BeginTransaction(deferred:false);
        Write(open,transaction,changed ?? all,keep:false);
        var at=now ?? DateTimeOffset.UtcNow;
        using(var purge=open.CreateCommand())
        {
            purge.Transaction=transaction;
            purge.CommandText="DELETE FROM voyage_records WHERE registered<$oldest OR registered>$latest;"
                +"DELETE FROM voyage_records WHERE registered<(SELECT registered FROM voyage_records ORDER BY registered DESC LIMIT 1 OFFSET $over);";
            purge.Parameters.AddWithValue("$oldest",(at-TimeSpan.FromDays(VoyageHistory.KeepDays)).UtcTicks);
            purge.Parameters.AddWithValue("$latest",at.AddDays(1).UtcTicks);
            purge.Parameters.AddWithValue("$over",VoyageHistory.MaximumRecords-1);
            purge.ExecuteNonQuery();
        }
        DuringSave?.Invoke();
        transaction.Commit();
    }

    // Rows written by key in the caller's transaction: replaced (a save: the voyage read again), or kept when already there (keep: a file of before).
    private static void Write(SqliteConnection open,SqliteTransaction transaction,IReadOnlyList<VoyageRecord> records,bool keep)
    {
        using(var insert=open.CreateCommand())
        {
            insert.Transaction=transaction;
            insert.CommandText=(keep ? "INSERT OR IGNORE":"INSERT OR REPLACE")+" INTO voyage_records(house,kind,slot,registered,vessel,rank,return_at,points,route,loot) "
                +"VALUES($house,$kind,$slot,$registered,$vessel,$rank,$return,$points,$route,$loot);";
            var house=insert.Parameters.Add("$house",SqliteType.Integer);var kind=insert.Parameters.Add("$kind",SqliteType.Integer);
            var slot=insert.Parameters.Add("$slot",SqliteType.Integer);var registered=insert.Parameters.Add("$registered",SqliteType.Integer);
            var vessel=insert.Parameters.Add("$vessel",SqliteType.Text);var rank=insert.Parameters.Add("$rank",SqliteType.Integer);
            var back=insert.Parameters.Add("$return",SqliteType.Integer);var points=insert.Parameters.Add("$points",SqliteType.Text);
            var route=insert.Parameters.Add("$route",SqliteType.Text);var loot=insert.Parameters.Add("$loot",SqliteType.Text);
            foreach(var record in records)
            {
                house.Value=unchecked((long)record.HouseId);kind.Value=(long)record.Kind;slot.Value=(long)record.Slot;registered.Value=record.RegisteredAt.UtcTicks;
                vessel.Value=record.Vessel;rank.Value=(long)record.Rank;back.Value=record.ReturnAt is { } at ? at.UtcTicks:DBNull.Value;
                points.Value=JsonSerializer.Serialize(record.Points,Json);route.Value=JsonSerializer.Serialize(record.Route,Json);loot.Value=JsonSerializer.Serialize(record.Loot,Json);
                insert.ExecuteNonQuery();
            }
        }
    }

    private static List<VoyageRecord> ReadRows(TenshoDataDatabase opened)
    {
        using var open=opened.Open();using var command=open.CreateCommand();
        command.CommandText="SELECT house,kind,slot,registered,vessel,rank,return_at,points,route,loot FROM voyage_records ORDER BY registered,house,kind,slot;";
        var records=new List<VoyageRecord>();
        using var reader=command.ExecuteReader();
        while(reader.Read())
            records.Add(new VoyageRecord(unchecked((ulong)reader.GetInt64(0)),(VesselKind)reader.GetInt64(1),(int)reader.GetInt64(2),
                reader.GetString(4),(int)reader.GetInt64(5),new DateTimeOffset(reader.GetInt64(3),TimeSpan.Zero),
                reader.IsDBNull(6) ? null:new DateTimeOffset(reader.GetInt64(6),TimeSpan.Zero),
                JsonSerializer.Deserialize<uint[]>(reader.GetString(7),Json) ?? throw new InvalidDataException("Voyage sectors unreadable."),
                JsonSerializer.Deserialize<string[]>(reader.GetString(8),Json) ?? throw new InvalidDataException("Voyage route unreadable."),
                JsonSerializer.Deserialize<VoyageLoot[]>(reader.GetString(9),Json) ?? throw new InvalidDataException("Voyage loot unreadable.")));
        return records;
    }
}

/// <summary>
/// g, M-10 (0.379): owner of the history. <see cref="Observe"/> is called by VoyageWatcher on the framework thread after a workshop
/// read; the page reads <see cref="Records"/>. A history that cannot be read is never written over. Since 0.1148 (X-173) kept through
/// <see cref="VoyageHistoryData"/> (TenshoData.db when a data file is given, else the JSON file as before).
/// </summary>
public sealed class VoyageHistoryStore(string path,Action<Exception,string> warn,TenshoDataFile? data=null)
{
    private readonly VoyageHistoryData source=new(path,data);
    private readonly object gate=new();
    private List<VoyageRecord>? records;
    private bool failed;
    // X-76 H10 (0.925): written off the framework thread, in order.
    private readonly BackgroundWriter writer=new(warn);
    public bool Flush(TimeSpan wait)=>writer.Flush(wait);
    public bool Unavailable{get{lock(gate){Load();return failed;}}}
    public IReadOnlyList<VoyageRecord> Records{get{lock(gate){Load();return records!.ToArray();}}}

    // X-182 (0.1169): the voyages as last read or written by this client, so a save writes only those that changed (night review N4).
    private Dictionary<(ulong,VesselKind,int,DateTimeOffset),VoyageRecord> written=[];
    private static (ulong,VesselKind,int,DateTimeOffset) Key(VoyageRecord record)=>(record.HouseId,record.Kind,record.Slot,record.RegisteredAt);

    public void Observe(IEnumerable<VoyageVessel> vessels)
    {
        lock(gate)
        {
            Load();
            if(failed)return;
            if(!VoyageHistory.Record(records!,vessels))return;
            var now=DateTimeOffset.UtcNow;
            records=VoyageHistory.Prune(records!,now);
            var copy=records.ToArray();
            var changed=copy.Where(record=>!written.TryGetValue(Key(record),out var known) || !VoyageHistory.Same(known,record)).ToArray();
            foreach(var record in changed)written[Key(record)]=record;
            writer.Enqueue("Voyage history could not be saved.",()=>source.Save(copy,changed,now));
        }
    }

    private void Load()
    {
        if(records is not null)return;
        try{var now=DateTimeOffset.UtcNow;records=VoyageHistory.Prune(source.Load(now),now);written=records.ToDictionary(Key);}
        catch(Exception error){records=[];failed=true;warn(error,"Voyage history could not be read; it is not written over.");}
    }
}
