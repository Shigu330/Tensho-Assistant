using TenshoAssistant.Caching;

// X-412 (0.1384, review of 0.1375 to 0.1382, I1; R28.14): two clients of one profile share the cache folder and its patch reports. Client A
// compares its catalogue while client B makes and writes its own report (another catalogue) through its own cache, in the window between A's
// comparison and A's write (DataCache.ReportsCompared). A's write, under the profile's lock, reads the file again: B's report stays.
internal static class PatchReportsTwoClients1384Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private const string Old = "ffxiv=2026.10.01.0000.0000;ex1=2026.10.01.0000.0000|1.0.0.0|copy=2|Row(Id:UInt32,Name:String)";
    private const string New = "ffxiv=2026.11.03.0000.0000;ex1=2026.11.03.0000.0000|1.0.1.0|copy=2|Row(Id:UInt32,Name:String)";
    private sealed record Row(uint Id, string Name);
    private sealed record Rows(string DataVersion, IReadOnlyList<Row> List);

    private static void Age(string folder)
    {
        foreach (var file in Directory.GetFiles(folder, "*.json")) File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddMinutes(-5));
    }

    private static void Patch(DataCache cache, string folder, string family, Action? between = null)
    {
        Check(cache.WriteAsync(family, "s", Old, new Rows("a", [new(1, "a"), new(2, "b")]), DateTimeOffset.UtcNow).GetAwaiter().GetResult(), family + ": old written");
        cache.FlushAsync().GetAwaiter().GetResult();
        Age(folder);
        between?.Invoke();
        Check(cache.WriteAsync(family, "s", New, new Rows("b", [new(1, "a"), new(2, "c"), new(3, "e")]), DateTimeOffset.UtcNow).GetAwaiter().GetResult(), family + ": new written");
        cache.FlushAsync().GetAwaiter().GetResult();
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Patch reports two clients 1384: a report the other client writes between the comparison and the write is kept (X-359 M1)", () =>
        {
            var folder = Path.Combine(Path.GetTempPath(), "tensho-patch-two-" + Guid.NewGuid().ToString("N"));
            try
            {
                var seenA = new List<PatchCatalogReport>(); var seenB = new List<PatchCatalogReport>(); var window = 0;
                using (var a = new DataCache(folder) { PatchReported = seenA.Add })
                using (var b = new DataCache(folder) { PatchReported = seenB.Add })
                {
                    // B's old catalogue first, so that its patch can be made inside A's window.
                    Check(b.WriteAsync("catalog-y", "s", Old, new Rows("a", [new(7, "g")]), DateTimeOffset.UtcNow).GetAwaiter().GetResult(), "B: old written");
                    b.FlushAsync().GetAwaiter().GetResult();
                    a.ReportsCompared = () =>
                    {
                        if (window++ > 0) return;
                        Age(folder);
                        Check(b.WriteAsync("catalog-y", "s", New, new Rows("b", [new(7, "h")]), DateTimeOffset.UtcNow).GetAwaiter().GetResult(), "B: new written");
                        b.FlushAsync().GetAwaiter().GetResult();
                        Check(seenB.Count == 1 && seenB[0].Family == "catalog-y", "B wrote its report inside A's window");
                    };
                    Patch(a, folder, "catalog-x");
                    Check(window == 1 && seenA.Count == 1 && seenA[0].Family == "catalog-x", "A compared once, its window ran once");
                    Check(a.PatchReports.Select(report => report.Family).OrderBy(name => name).SequenceEqual(["catalog-x", "catalog-y"]), "A's reports in memory hold B's");
                }
                using var reloaded = new DataCache(folder);
                var reports = reloaded.LoadPatchReportsAsync().GetAwaiter().GetResult();
                Check(reports.Select(report => report.Family).OrderBy(name => name).SequenceEqual(["catalog-x", "catalog-y"]),
                    "the file holds both reports: " + string.Join(", ", reports.Select(report => report.Family)));
            }
            finally { try { Directory.Delete(folder, true); } catch (IOException) { } }
        }),
        ("Patch reports two clients 1384: the same family made by both keeps the newest once", () =>
        {
            var folder = Path.Combine(Path.GetTempPath(), "tensho-patch-same-" + Guid.NewGuid().ToString("N"));
            try
            {
                using (var a = new DataCache(folder)) Patch(a, folder, "catalog-x");
                using (var b = new DataCache(folder)) Patch(b, folder, "catalog-x");
                using var reloaded = new DataCache(folder);
                Check(reloaded.LoadPatchReportsAsync().GetAwaiter().GetResult().Count(report => report.Family == "catalog-x") == 1, "one report per family and scope");
            }
            finally { try { Directory.Delete(folder, true); } catch (IOException) { } }
        }),
    ];
}
