namespace TenshoAssistant.Diagnostics;

/// <summary>
/// X-01 C1.1 (0.1184, docs/ai analysis of 12/11: 87 % of the slow frames follow repeated hot reloads; full collections of 1.3 to 3.7 s grow with
/// the reload's rank): what the load lines of the log add to measure it: the managed heap, the load contexts that carry Tensho's name (an
/// unloaded instance that stays alive is a leak), and whether the previous instance's context was still alive ten seconds after this load.
/// Measurement only: nothing is collected or forced here.
/// </summary>
public static class LoadMemory
{
    /// <summary>Process-wide slot (AppDomain data) for a weak reference to the context of the instance loaded last: it never keeps it alive.</summary>
    public const string PreviousSlot="TenshoAssistant.X01.PreviousLoadContext";

    /// <summary>The load contexts whose name holds the plugin's name, out of all the names given.</summary>
    public static int Named(IEnumerable<string?> names,string plugin)=>names.Count(name=>name is not null && name.Contains(plugin,StringComparison.OrdinalIgnoreCase));

    /// <summary>The words added to a load line: heap and total memory in megabytes, Tensho's contexts out of all, the previous one alive or not (null: none known).</summary>
    public static string Describe(long heapBytes,long totalBytes,int tenshoContexts,int allContexts,bool? previousAlive)=>
        $"heap {heapBytes/1_048_576} MB, managed {totalBytes/1_048_576} MB, Tensho load contexts {tenshoContexts} of {allContexts}"+
        (previousAlive is { } alive ? $", previous instance {(alive ? "still alive" : "collected")}":", no previous instance known");
}
