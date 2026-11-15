using TenshoAssistant.Diagnostics;

// 0.1451 (X-433): the self-check (developer mode) reads every page's data about thirty seconds after a load; what each read allocated is
// said once they are all read, so that its share of the two minutes after a load is told from the rest.
internal static class SelfCheckAllocations1451Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private const long Mb = 1_048_576;

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Self-check allocations 1451: the pages' reads, the largest first, nothing read said as such", () =>
        {
            var line = SelfCheck.PagesAllocated(new Dictionary<string, long> { ["FATEs"] = 3 * Mb, ["lexicon"] = 180 * Mb, ["relics"] = Mb / 2, ["map icon unlocks"] = 0 });
            Check(line == "Self-check pages' reads allocated (developer mode, on their threads, until their first wait): 184 MB; lexicon 180 MB, FATEs 3.0 MB, relics 0.5 MB.", line);
            Check(SelfCheck.PagesAllocated([]) == "Self-check pages' reads allocated (developer mode, on their threads, until their first wait): 0.0 MB.", "none");
        }),
        ("Self-check allocations 1451: each page's read is measured on its thread, and the line said once all are read", () =>
        {
            var pages = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.SelfCheckPages.cs")).Replace("\r\n", "\n");
            Check(pages.Contains("var before=GC.GetAllocatedBytesForCurrentThread();\n                try{return page();}\n                finally{selfCheckAllocated[domain]=GC.GetAllocatedBytesForCurrentThread()-before;}", StringComparison.Ordinal) && pages.Contains("return (domain,Task.Run(read));", StringComparison.Ordinal), "measured around the read");
            var list = pages.IndexOf("        selfCheckPages=\n        [", StringComparison.Ordinal);
            var said = pages.IndexOf("_=Task.WhenAll(selfCheckPages.Select(page=>page.Read)).ContinueWith(_=>log.Information(Diagnostics.SelfCheck.PagesAllocated(selfCheckAllocated)),TaskScheduler.Default);", StringComparison.Ordinal);
            Check(list > 0 && said > list, "after the list, once all are read");
        }),
    ];
}
