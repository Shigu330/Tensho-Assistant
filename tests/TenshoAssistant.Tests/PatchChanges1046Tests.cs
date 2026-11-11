using System.Text.Json;
using TenshoAssistant.Caching;

// 0.1046 (X-117, part 1): the catalogue copy from before a game patch is kept and compared with the one from the new client.
internal static class PatchChanges1046Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private const string Old="ffxiv=2026.10.01.0000.0000;ex1=2026.10.01.0000.0000|1.0.0.0|copy=2|Row(Id:UInt32,Name:String)";
    private const string New="ffxiv=2026.11.03.0000.0000;ex1=2026.11.03.0000.0000|1.0.1.0|copy=2|Row(Id:UInt32,Name:String)";
    private sealed record Row(uint Id,string Name);
    private sealed record Rows(string DataVersion,IReadOnlyList<Row> List);
    private static JsonElement Json(object value)=>JsonSerializer.SerializeToElement(value);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Patch changes 1046: game version read from a cache version, shapes compared without version numbers",() =>
        {
            Check(PatchCatalogDiff.GameVersion(Old)=="2026.10.01.0000.0000","game version");
            Check(PatchCatalogDiff.GameVersion("en|copy=1")==null,"no game version");
            Check(PatchCatalogDiff.Comparable(Old,New),"only numbers differ");
            Check(!PatchCatalogDiff.Comparable(Old,New.Replace("Name:String","Name:String,Icon:UInt32")),"shape change");
        }),
        ("Patch changes 1046: rows keyed by Id give added, removed and changed",() =>
        {
            var before=Json(new Rows("a",[new(1,"a"),new(2,"b"),new(4,"d")]));
            var after=Json(new Rows("b",[new(1,"a"),new(2,"c"),new(3,"e")]));
            var report=PatchCatalogDiff.Compare("catalog-x","s",Old,before,New,after,DateTimeOffset.UnixEpoch);
            Check(report.Skipped==null && report.Arrays.Count==1,"one list compared");
            var list=report.Arrays[0];
            Check(list.Name=="List" && list.Added==1 && list.Removed==1 && list.Changed==1 && list.Same==1,"counts");
            Check(list.AddedKeys.SequenceEqual(["3"]) && list.RemovedKeys.SequenceEqual(["4"]) && list.ChangedKeys.SequenceEqual(["2"]),"keys");
            Check(PatchCatalogDiff.Describe(report).Contains("1 added, 1 removed, 1 changed"),"log line");
            Check(PatchCatalogDiff.Compare("catalog-x","s",Old,before,Old,after,DateTimeOffset.UnixEpoch).Skipped=="same game version","same game");
            Check(PatchCatalogDiff.Compare("catalog-x","s",Old,before,New.Replace("Name:String","Icon:UInt32"),after,DateTimeOffset.UnixEpoch).Skipped=="record shape changed","shape");
            var plain=PatchCatalogDiff.CompareArray("P",Json(new[]{"a","b"}),Json(new[]{"b","c"}));
            Check(plain.Added==1 && plain.Removed==1 && plain.Changed==0,"rows without Id compared by text");
        }),
        ("Patch changes 1046: the cache keeps the previous game's catalogue, reports once and reloads the report",() =>
        {
            var folder=Path.Combine(Path.GetTempPath(),"tensho-patch-"+Guid.NewGuid().ToString("N"));
            try
            {
                var seen=new List<PatchCatalogReport>();
                using(var cache=new DataCache(folder){PatchReported=seen.Add})
                {
                    Check(cache.WriteAsync("catalog-x","s",Old,new Rows("a",[new(1,"a"),new(2,"b")]),DateTimeOffset.UtcNow).GetAwaiter().GetResult(),"old written");
                    foreach(var file in Directory.GetFiles(folder,"*.json"))File.SetLastWriteTimeUtc(file,DateTime.UtcNow.AddMinutes(-5));
                    Check(cache.WriteAsync("catalog-x","s",New,new Rows("b",[new(1,"a"),new(2,"c"),new(3,"e")]),DateTimeOffset.UtcNow).GetAwaiter().GetResult(),"new written");
                    cache.FlushAsync().GetAwaiter().GetResult();
                    Check(Directory.GetFiles(folder,"*.json").Length==1,"one current copy");
                    Check(Directory.GetFiles(Path.Combine(folder,"previous"),"*.json").Length==2,"previous copy and report kept");
                    Check(seen.Count==1 && seen[0].Added==1 && seen[0].Changed==1 && seen[0].OldGame=="2026.10.01.0000.0000","report sent once");
                    Check(cache.PatchReports.Count==1,"report kept in memory");
                    Check(cache.WriteAsync("catalog-x","s",New+"|again",new Rows("b",[]),DateTimeOffset.UtcNow).GetAwaiter().GetResult(),"same game written");
                    cache.FlushAsync().GetAwaiter().GetResult();
                    Check(seen.Count==1,"a new copy of the same game is not a patch");
                }
                using var reloaded=new DataCache(folder);
                var reports=reloaded.LoadPatchReportsAsync().GetAwaiter().GetResult();
                Check(reports.Count==1 && reports[0].Arrays[0].ChangedKeys.SequenceEqual(["2"]),"report read again");
            }
            finally{try{Directory.Delete(folder,true);}catch(IOException){}}
        }),
        ("Patch changes 1046: wiring, the plugin logs each report",() =>
        {
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("dataCache.PatchReported=") && plugin.Contains("LoadPatchReportsAsync()"),"plugin wiring");
        }),
    ];
}
