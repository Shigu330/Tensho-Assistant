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
/// amount allocated since the last one and the type of the object that crossed the mark): the amount by type (the largest <see cref="Kept"/>)
/// and by thread (the game's, the JIT warm-up's, the others). A sample: a type's amount is near what it allocated, not exact. Thread-safe;
/// pure (fed by <see cref="RuntimeSuspensionListener"/>, or by the tests).
/// </summary>
public sealed class AllocationTypes(int kept=10,int maximumTypes=4000)
{
    public int Kept { get; } = kept;
    public int MaximumTypes { get; } = maximumTypes;
    private readonly object gate=new();
    private readonly Dictionary<string,long> byType=new(StringComparer.Ordinal);
    private long other;
    public long Ticks { get{lock(gate)return ticks;} }
    public long Total { get{lock(gate)return total;} }
    public long OnGameThread { get{lock(gate)return onGame;} }
    public long OnWarmupThread { get{lock(gate)return onWarmup;} }
    private long ticks,total,onGame,onWarmup;

    /// <summary>One allocation tick: the amount since the last one, the type named, the thread (system ids; 0: not known yet).</summary>
    public void See(string? type,long bytes,long thread,long gameThread,long warmupThread)
    {
        if(bytes<=0)return;
        lock(gate)
        {
            ticks++;total+=bytes;
            if(gameThread!=0 && thread==gameThread)onGame+=bytes;
            if(warmupThread!=0 && thread==warmupThread)onWarmup+=bytes;
            var name=string.IsNullOrEmpty(type) ? "?":type;
            if(byType.TryGetValue(name,out var sum))byType[name]=sum+bytes;
            else if(byType.Count<MaximumTypes)byType[name]=bytes;
            else other+=bytes;
        }
    }

    private static string Mb(long bytes)=>bytes>=10*1_048_576
        ? string.Create(CultureInfo.InvariantCulture,$"{bytes/1_048_576} MB")
        : string.Create(CultureInfo.InvariantCulture,$"{bytes/1_048_576.0:0.0} MB");

    /// <summary>The line said when the watch ends.</summary>
    public string Summary(TimeSpan watched)
    {
        lock(gate)
        {
            var top=byType.OrderByDescending(pair=>pair.Value).ThenBy(pair=>pair.Key,StringComparer.Ordinal).Take(Kept).Select(pair=>pair.Key+" "+Mb(pair.Value));
            return string.Create(CultureInfo.InvariantCulture,
                $"Allocated in the {watched.TotalSeconds:0} s after the load (allocation ticks, about one per 100 KB): {Mb(total)} in {ticks} ticks, {Mb(onGame)} on the game's thread, {Mb(onWarmup)} on the JIT warm-up's; by type: {(byType.Count==0 ? "none":string.Join(", ",top))}{(other>0 ? "; other types "+Mb(other):"")}.");
        }
    }
}
