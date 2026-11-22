using System.Text.Json;
using TenshoAssistant.Reminders;

namespace TenshoAssistant.MyTools;

/// <summary>An installed tool and its progress: ticks with their date, and counters. Keys are « block:entry ».</summary>
public sealed record InstalledTool(ToolTemplate Template,string Json,IReadOnlyDictionary<string,DateTimeOffset> Done,IReadOnlyDictionary<string,int> Counts);

public static class ToolProgress
{
    public static string Key(int block,int entry=0)=>$"{block}:{entry}";
    public static DateTimeOffset? LastReset(TemplateReset reset,DateTimeOffset now)=>reset switch
    {
        TemplateReset.Daily=>ResetSchedule.PreviousDaily(now,ResetSchedule.DailyHour),
        TemplateReset.Weekly=>ResetSchedule.PreviousWeekly(now,ResetSchedule.WeeklyDay,ResetSchedule.WeeklyHour),
        _=>null,
    };
    public static bool IsDone(InstalledTool tool,string key,TemplateReset reset,DateTimeOffset now)
        =>tool.Done.TryGetValue(key,out var done) && (LastReset(reset,now) is not { } last || ResetSchedule.DoneSince(done,last));
    public static DateTimeOffset NextTime(TimerBlock timer,DateTimeOffset now)=>timer.Timer switch
    {
        TemplateTimer.Weekly=>ResetSchedule.NextWeekly(now,ResetSchedule.WeeklyDay,ResetSchedule.WeeklyHour),
        TemplateTimer.Supply=>ResetSchedule.NextDaily(now,ResetSchedule.SupplyHour),
        TemplateTimer.Hour=>ResetSchedule.NextDaily(now,timer.HourUtc),
        _=>ResetSchedule.NextDaily(now,ResetSchedule.DailyHour),
    };
}

/// <summary>
/// TEMPLATES-001 (DEC-029): the tools of this install, in « MyTools.json » beside the configuration. Written off the draw, in order, through a temporary file; a file that cannot be read is never written
/// over. A saved tool is checked again at each start: one the format no longer accepts is not shown, and stays in the file.
/// </summary>
public sealed class ToolTemplateStore:IDisposable
{
    public const int MaxTools=50;
    internal sealed record SavedTool(string Json,Dictionary<string,DateTimeOffset>? Done,Dictionary<string,int>? Counts);
    internal sealed record FileDto(int Schema,SavedTool[] Tools);
    /// <summary>X-832: the wait for another game client's save of this file (a save takes milliseconds; off the draw).</summary>
    public const int LockWaitMs=10_000;
    // X-832: the tools as this instance last read or wrote them (its own, never another client's): the base of the three-way merge. Written
    // by the constructor, then only by the writes' chain (one at a time).
    private FileDto? before;
    private static readonly JsonSerializerOptions Options=TenshoJson.Own(new(){WriteIndented=true});
    private readonly object gate=new();
    private readonly string path;
    private readonly Action<Exception,string> warn;
    private List<InstalledTool> tools=[];
    private readonly List<SavedTool> refused=[];
    private Task writes=Task.CompletedTask;
    private bool disposed;
    public bool Unavailable{get;private set;}
    public int RefusedCount{get{lock(gate)return refused.Count;}}
    public IReadOnlyList<InstalledTool> Tools{get{lock(gate)return tools;}}

    /// <summary>X-180 (0.1168): read-only (the desktop application's copy, overwritten from the game): nothing is added, removed, changed or written.</summary>
    public bool ReadOnly{get;}

    public ToolTemplateStore(string path,Action<Exception,string> warn,bool readOnly=false)
    {
        this.path=path;this.warn=warn;ReadOnly=readOnly;
        try
        {
            // X-833: the file, or its previous version (« .bak ») when it is missing or does not read.
            if(!File.Exists(path) && !File.Exists(AtomicFile.BackupOf(path)))return;
            var dto=AtomicFile.ReadKept(path,ReadFile,Recovered);
            before=new FileDto(1,dto.Tools.Take(MaxTools).ToArray());
            foreach(var saved in dto.Tools.Take(MaxTools))
            {
                if(saved?.Json is not null && ToolTemplateFormat.Parse(saved.Json).Template is { } template && tools.All(tool=>tool.Template.Id!=template.Id))
                    tools.Add(new(template,saved.Json,saved.Done ?? [],saved.Counts ?? []));
                else if(saved?.Json is not null)refused.Add(saved);
            }
        }
        catch(Exception error) when(error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException or NotSupportedException)
        {Unavailable=true;tools=[];warn(error,"My tools could not be read; the file is left as it is.");}
    }
    private void Recovered(string aside,Exception error)=>warn(error,$"My tools did not read; their previous version (.bak) was read, the damaged file kept as {Path.GetFileName(aside)}.");

    /// <summary>
    /// X-832 (security review I4): two game clients on one profile each wrote their whole copy at every tick or counter: one client's tool or
    /// ticks were erased by the other's next change. Three-way, by the tool's id (a tool the format refuses: by its text): a tool this client
    /// did not change is the file's (or stays removed when the file removed it); one it removed stays removed; one only the file has, never
    /// seen here, is kept; one it changed is its own, and when the file holds the same tool (same text) its ticks and counters are merged key
    /// by key the same way (each client's ticks of the day kept). Mine's order first, then the file's other tools.
    /// </summary>
    internal static FileDto Merge(FileDto? before,FileDto mine,FileDto theirs)
    {
        var old=ByKey(before?.Tools ?? []);var file=ByKey(theirs.Tools);var own=ByKey(mine.Tools);
        var result=new List<SavedTool>();
        foreach(var tool in mine.Tools)
        {
            var key=KeyOf(tool);
            var had=old.TryGetValue(key,out var was);
            var there=file.TryGetValue(key,out var other);
            if(had && Same(was!,tool)){if(there)result.Add(other!);continue;}
            if(there && other!.Json==tool.Json && (!had || was!.Json==tool.Json))
                result.Add(tool with {Done=MergeMap(was?.Done,tool.Done,other.Done),Counts=MergeMap(was?.Counts,tool.Counts,other.Counts)});
            else result.Add(tool);
        }
        foreach(var tool in theirs.Tools)
            if(!own.ContainsKey(KeyOf(tool)) && !old.ContainsKey(KeyOf(tool)))result.Add(tool);
        return new FileDto(1,result.ToArray());
    }
    private static string KeyOf(SavedTool tool)=>ToolTemplateFormat.Parse(tool.Json).Template?.Id is { } id ? "id:"+id : "text:"+tool.Json;
    private static Dictionary<string,SavedTool> ByKey(IEnumerable<SavedTool> tools)
    {
        var byKey=new Dictionary<string,SavedTool>(StringComparer.Ordinal);
        foreach(var tool in tools)byKey.TryAdd(KeyOf(tool),tool);
        return byKey;
    }
    private static bool Same(SavedTool a,SavedTool b)=>a.Json==b.Json && SameMap(a.Done,b.Done) && SameMap(a.Counts,b.Counts);
    private static bool SameMap<TValue>(Dictionary<string,TValue>? a,Dictionary<string,TValue>? b)
        =>(a?.Count ?? 0)==(b?.Count ?? 0) && (a ?? []).All(pair=>b is not null && b.TryGetValue(pair.Key,out var value) && EqualityComparer<TValue>.Default.Equals(value,pair.Value));
    private static Dictionary<string,TValue> MergeMap<TValue>(Dictionary<string,TValue>? before,Dictionary<string,TValue>? mine,Dictionary<string,TValue>? theirs)
    {
        var result=new Dictionary<string,TValue>(StringComparer.Ordinal);
        foreach(var (key,value) in mine ?? [])
        {
            if(before is not null && before.TryGetValue(key,out var was) && EqualityComparer<TValue>.Default.Equals(was,value))
            {
                if(theirs is not null && theirs.TryGetValue(key,out var other))result[key]=other;
                continue;
            }
            result[key]=value;
        }
        foreach(var (key,value) in theirs ?? [])
            if(!result.ContainsKey(key) && !(mine?.ContainsKey(key) ?? false) && !(before?.ContainsKey(key) ?? false))result[key]=value;
        return result;
    }

    private static FileDto ReadFile(string file)
    {
        if(new FileInfo(file).Length>8*1024*1024)throw new InvalidDataException("My tools file too large.");
        var dto=JsonSerializer.Deserialize<FileDto>(File.ReadAllBytes(file),Options) ?? throw new InvalidDataException("Empty my tools file.");
        if(dto.Schema!=1 || dto.Tools is null)throw new InvalidDataException("Unknown my tools schema.");
        return dto;
    }
    /// <summary>Adds a tool, or replaces the one with its id. Its progress stays only if the file is the same. False when the store is unavailable, read-only or full.</summary>
    public bool Install(ToolTemplate template,string json)
    {
        lock(gate)
        {
            if(disposed || Unavailable || ReadOnly)return false;
            var index=tools.FindIndex(tool=>tool.Template.Id==template.Id);
            if(index<0 && tools.Count+refused.Count>=MaxTools)return false;
            var same=index>=0 && tools[index].Json==json;
            var added=new InstalledTool(template,json,same ? tools[index].Done:new Dictionary<string,DateTimeOffset>(),same ? tools[index].Counts:new Dictionary<string,int>());
            var next=tools.ToList();
            if(index>=0)next[index]=added;else next.Add(added);
            tools=next;Save();
            return true;
        }
    }
    public void Remove(string id)
    {
        lock(gate)
        {
            if(disposed || Unavailable || ReadOnly)return;
            tools=tools.Where(tool=>tool.Template.Id!=id).ToList();Save();
        }
    }
    public void SetDone(string id,string key,bool done,DateTimeOffset now)=>Edit(id,tool=>
    {
        var next=new Dictionary<string,DateTimeOffset>(tool.Done);
        if(done)next[key]=now;else next.Remove(key);
        return tool with {Done=next};
    });
    public void SetCount(string id,string key,int count)=>Edit(id,tool=>
    {
        var next=new Dictionary<string,int>(tool.Counts){[key]=Math.Clamp(count,0,ToolTemplateFormat.MaxTarget)};
        return tool with {Counts=next};
    });
    private void Edit(string id,Func<InstalledTool,InstalledTool> edit)
    {
        lock(gate)
        {
            if(disposed || Unavailable || ReadOnly)return;
            var index=tools.FindIndex(tool=>tool.Template.Id==id);
            if(index<0)return;
            var next=tools.ToList();next[index]=edit(next[index]);tools=next;Save();
        }
    }
    private void Save()
    {
        var snapshot=new FileDto(1,tools.Select(tool=>new SavedTool(tool.Json,new(tool.Done),new(tool.Counts))).Concat(refused).ToArray());
        writes=writes.ContinueWith(_=>Write(snapshot),TaskScheduler.Default);
    }
    private void Write(FileDto snapshot)
    {
        try
        {
            // X-832: under the profile's lock (« MyTools.json.lock »), the file read again and merged with this client's change; a file that
            // does not read (nor its .bak) is never written over.
            using(FairFileLock.Hold(path+".lock",LockWaitMs,error=>new IOException("My tools' lock is held by another game client.",error)))
            {
                var onDisk=File.Exists(path) || File.Exists(AtomicFile.BackupOf(path)) ? AtomicFile.ReadKept(path,ReadFile,Recovered) : null;
                var merged=onDisk is null ? snapshot : Merge(before,snapshot,onDisk);
                AtomicFile.Write(path,JsonSerializer.SerializeToUtf8Bytes(merged,Options),keep:true);
                before=snapshot;
            }
        }
        catch(Exception error) when(error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException or NotSupportedException)
        {warn(error,"My tools could not be saved.");}
    }
    public Task FlushAsync(){lock(gate)return writes;}
    public void Dispose()
    {
        Task pending;
        lock(gate)
        {
            if(disposed)return;
            disposed=true;pending=writes;
        }
        pending.Wait(TimeSpan.FromSeconds(3));
    }
}
