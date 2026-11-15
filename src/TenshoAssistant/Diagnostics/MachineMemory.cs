using System.Globalization;
using System.Runtime.InteropServices;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// X-433 (0.1447): the machine's physical memory (Windows GlobalMemoryStatusEx) and this process's private memory, said in the load lines and
/// with each collection of generation 2: a blocking collection « induced for low memory » is the runtime's answer to the system saying its
/// available physical memory is low (docs/evidence/0.1446-low-memory-collections.md); these words tell whether it was, and how much the game's
/// process holds. Reading is cheap; the words are pure (tests).
/// </summary>
public static class MachineMemory
{
    /// <summary>The machine's physical memory: available and total bytes, the load in percent, the commit charge still available.</summary>
    public readonly record struct Reading(long AvailableBytes,long TotalBytes,int LoadPercent,long CommitAvailableBytes);

    /// <summary>The machine's memory now; null off Windows or when it could not be read.</summary>
    public static Reading? Read()
    {
        if(!OperatingSystem.IsWindows())return null;
        try
        {
            var status=new Native.MemoryStatus{Length=(uint)Marshal.SizeOf<Native.MemoryStatus>()};
            if(!Native.GlobalMemoryStatusEx(ref status) || status.TotalPhysical==0)return null;
            return new Reading((long)Math.Min(status.AvailablePhysical,(ulong)long.MaxValue),(long)Math.Min(status.TotalPhysical,(ulong)long.MaxValue),(int)status.MemoryLoad,
                (long)Math.Min(status.AvailablePageFile,(ulong)long.MaxValue));
        }
        catch(Exception){return null;}
    }

    /// <summary>This process's private memory in bytes; -1 when it could not be read.</summary>
    public static long ProcessPrivateBytes()
    {
        try
        {
            using var self=System.Diagnostics.Process.GetCurrentProcess();
            return self.PrivateMemorySize64;
        }
        catch(Exception){return -1;}
    }

    private static long Mb(long bytes)=>bytes/1_048_576;

    /// <summary>The words of a load line or of a collection: the process's private memory, then the machine's.</summary>
    public static string Text(Reading? machine,long processPrivateBytes)=>
        (processPrivateBytes>=0 ? string.Create(CultureInfo.InvariantCulture,$"process private {Mb(processPrivateBytes)} MB"):"process private unknown")+
        (machine is { } m
            ? string.Create(CultureInfo.InvariantCulture,$", machine {Mb(m.AvailableBytes)} MB available of {Mb(m.TotalBytes)} MB (load {m.LoadPercent} %), commit {Mb(m.CommitAvailableBytes)} MB available")
            :", machine memory unknown");

    /// <summary>The memory load the collector saw at its last full blocking collection (GC.GetGCMemoryInfo), and its high-load mark; empty
    /// when unknown.</summary>
    public static string CollectorText(long loadBytes,long highLoadBytes,long totalBytes)=>
        totalBytes<=0 || loadBytes<0 ? ""
            : string.Create(CultureInfo.InvariantCulture,$"; the collector saw a memory load of {loadBytes*100/totalBytes} % (high above {Math.Max(0,highLoadBytes)*100/totalBytes} %)");

    /// <summary>The two minutes after a load: what the whole process allocated (every thread, every plugin), and its managed memory before
    /// and after.</summary>
    public static string AllocatedText(long allocatedBytes,long managedBefore,long managedAfter,TimeSpan watched)=>
        string.Create(CultureInfo.InvariantCulture,
            $"Process allocated {Mb(Math.Max(0,allocatedBytes))} MB in the {watched.TotalSeconds:0} s after the load (every thread); managed memory {Mb(managedBefore)} MB before the load, {Mb(managedAfter)} MB after.");

    private static class Native
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct MemoryStatus
        {
            internal uint Length;
            internal uint MemoryLoad;
            internal ulong TotalPhysical;
            internal ulong AvailablePhysical;
            internal ulong TotalPageFile;
            internal ulong AvailablePageFile;
            internal ulong TotalVirtual;
            internal ulong AvailableVirtual;
            internal ulong AvailableExtendedVirtual;
        }

        [DllImport("kernel32.dll",SetLastError=true)]
        [return:MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GlobalMemoryStatusEx(ref MemoryStatus buffer);
    }
}

/// <summary>
/// X-433 (0.1447): what is allocated in the two minutes after a load, from the runtime's allocation ticks (one about every 100 KB, with the
/// amount allocated since the last one and the type of the object that crossed the mark). X-433 (0.1448): by thread, each with its own
/// largest types (<see cref="Kept"/>): the game's, the JIT warm-up's, the readers' compiler's, the event listener's own (the cost of this
/// measure, which a player does not have), the others. A sample: a type's amount is near what it allocated, not exact. Thread-safe; pure
/// (fed by <see cref="RuntimeSuspensionListener"/>, or by the tests).
/// </summary>
public sealed class AllocationTypes(int kept=4,int maximumTypes=2000,int? keptOther=null)
{
    /// <summary>X-433 (0.1450): how many types the other threads list (most of the amount: 537 MB of 777 at 0.1449's load).</summary>
    public int KeptOther { get; } = keptOther ?? kept;
    /// <summary>Where a tick was made.</summary>
    public enum Where { Game, Warmup, Compiler, Listener, Other }
    private static readonly string[] Names=["the game's thread","the JIT warm-up's thread","the readers' compiler's thread","the measure's own listener","other threads"];
    public int Kept { get; } = kept;
    /// <summary>At most this many types kept by thread; the rest counted as « other types ».</summary>
    public int MaximumTypes { get; } = maximumTypes;
    private readonly object gate=new();
    private readonly Dictionary<string,long>[] byType=[new(StringComparer.Ordinal),new(StringComparer.Ordinal),new(StringComparer.Ordinal),new(StringComparer.Ordinal),new(StringComparer.Ordinal)];
    private readonly long[] amounts=new long[5],others=new long[5];
    private long ticks,total;
    public long Ticks { get{lock(gate)return ticks;} }
    public long Total { get{lock(gate)return total;} }
    public long On(Where where){lock(gate)return amounts[(int)where];}
    public long OnGameThread=>On(Where.Game);
    public long OnWarmupThread=>On(Where.Warmup);

    /// <summary>The thread a tick was made on, from the system ids known (0: not known yet, never matched).</summary>
    public static Where Of(long thread,long gameThread,long warmupThread,long compilerThread,long listenerThread)=>
        thread==0 ? Where.Other : thread==gameThread ? Where.Game : thread==warmupThread ? Where.Warmup
            : thread==compilerThread ? Where.Compiler : thread==listenerThread ? Where.Listener : Where.Other;

    /// <summary>One allocation tick: the amount since the last one, the type named, the thread (system ids; 0: not known yet).</summary>
    public void See(string? type,long bytes,long thread,long gameThread,long warmupThread,long compilerThread=0,long listenerThread=0)
    {
        if(bytes<=0)return;
        var where=(int)Of(thread,gameThread,warmupThread,compilerThread,listenerThread);
        lock(gate)
        {
            ticks++;total+=bytes;amounts[where]+=bytes;
            var name=string.IsNullOrEmpty(type) ? "?":type;
            var table=byType[where];
            if(table.TryGetValue(name,out var sum))table[name]=sum+bytes;
            else if(table.Count<MaximumTypes)table[name]=bytes;
            else others[where]+=bytes;
        }
    }

    private static string Mb(long bytes)=>bytes>=10*1_048_576
        ? string.Create(CultureInfo.InvariantCulture,$"{bytes/1_048_576.0:0} MB")
        : string.Create(CultureInfo.InvariantCulture,$"{bytes/1_048_576.0:0.0} MB");

    /// <summary>The line said when the watch ends: the total, then each thread with an amount, its largest types first.</summary>
    public string Summary(TimeSpan watched)
    {
        lock(gate)
        {
            var parts=Enumerable.Range(0,Names.Length).Where(index=>amounts[index]>0).Select(index=>Names[index]+" "+Mb(amounts[index])+" ("
                +string.Join(", ",byType[index].OrderByDescending(pair=>pair.Value).ThenBy(pair=>pair.Key,StringComparer.Ordinal).Take(index==(int)Where.Other ? KeptOther:Kept).Select(pair=>pair.Key+" "+Mb(pair.Value)))
                +(others[index]>0 ? ", other types "+Mb(others[index]):"")+")");
            return string.Create(CultureInfo.InvariantCulture,
                $"Allocated in the {watched.TotalSeconds:0} s after the load (allocation ticks, about one per 100 KB): {Mb(total)} in {ticks} ticks{(ticks==0 ? "":"; "+string.Join("; ",parts))}.");
        }
    }
}
