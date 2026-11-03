using System.Globalization;
using System.Text;
using System.Text.Json;

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
    private static bool Same(VoyageRecord a,VoyageRecord b)=>a.Vessel==b.Vessel && a.Rank==b.Rank && a.ReturnAt==b.ReturnAt
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
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        File.WriteAllBytes(temporary,JsonSerializer.SerializeToUtf8Bytes(new FileDto(1,records.ToArray()),Json));
        File.Move(temporary,path,true);
    }
}

/// <summary>
/// g, M-10 (0.379): owner of the history file. <see cref="Observe"/> is called by VoyageWatcher on the framework thread after a workshop
/// read; the page reads <see cref="Records"/>. A file that cannot be read is never written over.
/// </summary>
public sealed class VoyageHistoryStore(string path,Action<Exception,string> warn)
{
    private readonly object gate=new();
    private List<VoyageRecord>? records;
    private bool failed;
    public bool Unavailable{get{lock(gate){Load();return failed;}}}
    public IReadOnlyList<VoyageRecord> Records{get{lock(gate){Load();return records!.ToArray();}}}

    public void Observe(IEnumerable<VoyageVessel> vessels)
    {
        lock(gate)
        {
            Load();
            if(failed)return;
            if(!VoyageHistory.Record(records!,vessels))return;
            records=VoyageHistory.Prune(records!,DateTimeOffset.UtcNow);
            try{VoyageHistory.Write(path,records);}
            catch(Exception error){warn(error,"Voyage history could not be saved.");}
        }
    }

    private void Load()
    {
        if(records is not null)return;
        try{records=VoyageHistory.Prune(VoyageHistory.Read(path),DateTimeOffset.UtcNow);}
        catch(Exception error){records=[];failed=true;warn(error,"Voyage history could not be read; it is not written over.");}
    }
}
