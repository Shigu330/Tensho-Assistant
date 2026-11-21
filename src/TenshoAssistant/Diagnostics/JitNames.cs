using System.Diagnostics.Tracing;
using System.Reflection.Emit;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// X-564 (0.1633, measured idle on 20/11 17:55: the chat window's first frame with lines still compiled 7 methods in 43 ms on the game's thread,
/// after the warm-up's part): the methods the runtime starts compiling on one thread between <see cref="Start"/> and <see cref="Stop"/>, from
/// the runtime's own JIT events in this process (EventListener on Microsoft-Windows-DotNETRuntime, keyword JIT, verbose). The events come on
/// the runtime's own thread a little later, in each thread's order: the window is told by two marks, small methods made and compiled on the
/// thread itself at its start and end (no clock compared). For the log in developer mode only: listening makes the runtime report every
/// compilation of the process while it lasts; disposed after the reading.
/// </summary>
public sealed class JitNames : EventListener
{
    public const string RuntimeSource="Microsoft-Windows-DotNETRuntime";
    public const long JitKeyword=0x10;
    /// <summary>The runtime's event « MethodJittingStarted ».</summary>
    public const int JittingStarted=145;
    /// <summary>At most this many names given.</summary>
    public const int Cap=200;
    private readonly object gate=new();
    private readonly List<(long Thread,string Name)> seen=[];
    // X-703 (0.1684): the threads heard at least once, and the events left out past the cap, for Hears and State.
    private readonly HashSet<long> threads=[];
    private int dropped;
    private readonly string startMark="TenshoJitStart"+Guid.NewGuid().ToString("N"),stopMark="TenshoJitStop"+Guid.NewGuid().ToString("N");

    protected override void OnEventSourceCreated(EventSource source)
    {
        if(source.Name==RuntimeSource)EnableEvents(source,EventLevel.Verbose,(EventKeywords)JitKeyword);
    }

    protected override void OnEventWritten(EventWrittenEventArgs data)
    {
        if(data.EventId!=JittingStarted && data.EventName?.StartsWith("MethodJittingStarted",StringComparison.Ordinal)!=true)return;
        var method=Text(data,"MethodNamespace")+"::"+Text(data,"MethodName");
        lock(gate)
        {
            threads.Add(data.OSThreadId);
            if(seen.Count<20_000)seen.Add((data.OSThreadId,method));else dropped++;
        }
    }

    private static string Text(EventWrittenEventArgs data,string field)
    {
        var at=data.PayloadNames?.IndexOf(field) ?? -1;
        return at>=0 && data.Payload is { } payload && at<payload.Count ? payload[at]?.ToString() ?? "":"";
    }

    /// <summary>The window starts: a mark compiled on this thread.</summary>
    public void Start()=>Mark(startMark);
    /// <summary>The window ends: a mark compiled on this thread.</summary>
    public void Stop()=>Mark(stopMark);

    private static void Mark(string name)
    {
        var method=new DynamicMethod(name,typeof(int),Type.EmptyTypes,typeof(JitNames).Module);
        var il=method.GetILGenerator();
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Ret);
        ((Func<int>)method.CreateDelegate(typeof(Func<int>)))();
    }

    /// <summary>X-703 (0.1684): a compilation on <paramref name="thread"/> was heard: the listener hears that thread (its window can open).</summary>
    public bool Hears(long thread)
    {
        lock(gate)return threads.Contains(thread);
    }

    /// <summary>X-703 (0.1684): what was heard, for a log or a failed test: the marks of <paramref name="thread"/>, its events, all events, those left out.</summary>
    public string State(long thread)
    {
        lock(gate)
        {
            var mine=seen.Count(entry=>entry.Thread==thread);
            var start=seen.Any(entry=>entry.Thread==thread && entry.Name.EndsWith("::"+startMark,StringComparison.Ordinal));
            var stop=seen.Any(entry=>entry.Thread==thread && entry.Name.EndsWith("::"+stopMark,StringComparison.Ordinal));
            return string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"start mark {(start ? "heard":"not heard")}, stop mark {(stop ? "heard":"not heard")}, {mine} compilation(s) heard on the thread, {seen.Count} in all, {dropped} past the cap");
        }
    }

    /// <summary>Both marks of <paramref name="thread"/> (the system's thread id) were heard: <see cref="Names"/> is whole.</summary>
    public bool Heard(long thread)
    {
        lock(gate)return seen.Any(entry=>entry.Thread==thread && entry.Name.EndsWith("::"+stopMark,StringComparison.Ordinal));
    }

    /// <summary>The methods started compiling on <paramref name="thread"/> between the marks, in order, each once, at most <see cref="Cap"/>.</summary>
    public IReadOnlyList<string> Names(long thread)
    {
        lock(gate)
        {
            var names=new List<string>();
            // The start mark may come before the runtime's session is up: without it, from the listener's first event on that thread.
            var inside=!seen.Any(entry=>entry.Thread==thread && entry.Name.EndsWith("::"+startMark,StringComparison.Ordinal));
            foreach(var (id,name) in seen)
            {
                if(id!=thread)continue;
                if(name.EndsWith("::"+startMark,StringComparison.Ordinal)){inside=true;continue;}
                if(name.EndsWith("::"+stopMark,StringComparison.Ordinal))break;
                if(inside && names.Count<Cap && !names.Contains(name))names.Add(name);
            }
            return names;
        }
    }
}
