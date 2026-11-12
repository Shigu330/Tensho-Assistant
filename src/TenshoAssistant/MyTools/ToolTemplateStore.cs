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
    private sealed record SavedTool(string Json,Dictionary<string,DateTimeOffset>? Done,Dictionary<string,int>? Counts);
    private sealed record FileDto(int Schema,SavedTool[] Tools);
    private static readonly JsonSerializerOptions Options=new(){WriteIndented=true};
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
            if(!File.Exists(path))return;
            if(new FileInfo(path).Length>8*1024*1024)throw new InvalidDataException("My tools file too large.");
            var dto=JsonSerializer.Deserialize<FileDto>(File.ReadAllBytes(path),Options) ?? throw new InvalidDataException("Empty my tools file.");
            if(dto.Schema!=1 || dto.Tools is null)throw new InvalidDataException("Unknown my tools schema.");
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
            AtomicFile.Write(path,JsonSerializer.SerializeToUtf8Bytes(snapshot,Options));
        }
        catch(Exception error) when(error is IOException or UnauthorizedAccessException){warn(error,"My tools could not be saved.");}
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
