namespace TenshoAssistant.Diagnostics;

/// <summary>How a dashboard line or the whole of Tensho is doing: fine, worth a look, or a problem.</summary>
public enum HealthLevel { Ok, Watch, Problem }

/// <summary>
/// X-165 (0.1244, user request of 2026-11-12): what the dashboard reads, measures Tensho already takes (nothing new measured), copied once a
/// second on the draw thread while the dashboard is shown. A value not known is null, never 0 (R08).
/// </summary>
public sealed record HealthInputs(long? HeapBytes,long? ManagedBytes,string? SlowestPage,double? SlowestMedianMs,double? SlowestMaxMs,
    IReadOnlyList<string> Unavailable,string? CacheError,string? BankError,bool DiscoveriesBlocked,bool DiscoveriesUnsaved,string? SaveError,
    IReadOnlyDictionary<string,int> Requests,int SelfOk,int SelfSuspect,int SelfFailed,bool SelfRun);

/// <summary>One line of the dashboard: its text key, how it is doing, and its value (arguments of the text).</summary>
public sealed record HealthLine(string Key,HealthLevel Level,params object[] Values);

/// <summary>
/// X-165 (0.1244): the dashboard's rules, Tensho's own policy. A problem: a failed self-check, a failed save, an unreadable cache or bank,
/// discoveries that cannot be written. Worth a look: a reader unavailable after a game update, a suspect self-check, discoveries waiting
/// to be written, a page whose usual (median) draw time is over a quarter of a 60 fps frame (4 ms; the steady state measured in 0.1190 is
/// 0.46 ms a frame for all of Tensho). Everything else is shown as it is.
/// </summary>
public static class HealthBoard
{
    public const double SlowPageMs=4;

    public static (HealthLevel Overall,IReadOnlyList<HealthLine> Lines) Evaluate(HealthInputs inputs)
    {
        var lines=new List<HealthLine>
        {
            inputs.HeapBytes is { } heap && inputs.ManagedBytes is { } managed
                ? new("Dashboard.Memory",HealthLevel.Ok,Megabytes(heap),Megabytes(managed)):new("Dashboard.MemoryUnknown",HealthLevel.Ok),
            inputs.SlowestPage is { } page && inputs.SlowestMedianMs is { } median && inputs.SlowestMaxMs is { } max
                ? new("Dashboard.SlowestPage",median>SlowPageMs ? HealthLevel.Watch:HealthLevel.Ok,page,median,max):new("Dashboard.NoPageTimed",HealthLevel.Ok),
            inputs.Unavailable.Count==0 ? new("Dashboard.ReadersOk",HealthLevel.Ok):new("Dashboard.ReadersUnavailable",HealthLevel.Watch,inputs.Unavailable.Count,string.Join(", ",inputs.Unavailable)),
            inputs.CacheError is { } cache ? new("Dashboard.CacheError",HealthLevel.Problem,cache):new("Dashboard.CacheOk",HealthLevel.Ok),
            inputs.BankError is { } bank ? new("Dashboard.BankError",HealthLevel.Problem,bank):new("Dashboard.BankOk",HealthLevel.Ok),
            inputs.DiscoveriesBlocked ? new("Dashboard.DiscoveriesBlocked",HealthLevel.Problem)
                :inputs.DiscoveriesUnsaved ? new("Dashboard.DiscoveriesUnsaved",HealthLevel.Watch):new("Dashboard.DiscoveriesOk",HealthLevel.Ok),
            inputs.SaveError is { } save ? new("Dashboard.SaveError",HealthLevel.Problem,save):new("Dashboard.SavesOk",HealthLevel.Ok),
            !inputs.SelfRun ? new("Dashboard.SelfCheckNotRun",HealthLevel.Ok)
                :new("Dashboard.SelfCheck",inputs.SelfFailed>0 ? HealthLevel.Problem:inputs.SelfSuspect>0 ? HealthLevel.Watch:HealthLevel.Ok,inputs.SelfOk,inputs.SelfSuspect,inputs.SelfFailed),
            inputs.Requests.Count==0 ? new("Dashboard.NoRequests",HealthLevel.Ok)
                :new("Dashboard.Requests",HealthLevel.Ok,inputs.Requests.Values.Sum(),string.Join(", ",inputs.Requests.OrderByDescending(pair=>pair.Value).ThenBy(pair=>pair.Key,StringComparer.Ordinal).Select(pair=>$"{pair.Key} {pair.Value}"))),
        };
        return (lines.Max(line=>line.Level),lines);
    }

    /// <summary>The median and the largest of a page's last draw times (ring of samples); null when it has none.</summary>
    public static (double Median,double Max)? MedianMax(IReadOnlyList<double> samples,int count)
    {
        if(count<=0)return null;
        var sorted=samples.Take(Math.Min(count,samples.Count)).Order().ToArray();
        return (sorted[sorted.Length/2],sorted[^1]);
    }

    private static double Megabytes(long bytes)=>Math.Round(bytes/1048576.0,1);
}
