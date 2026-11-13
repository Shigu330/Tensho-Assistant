using TenshoAssistant.Caching;

// 0.1217 (X-147, GPT audit of 11/11, A14): measured first on a copy of a real cache (2 053 files, 69 MB): each write read the header of every
// file again, about 150 ms under the disk lock (reads wait behind it). The identity of a file is now kept while its name, time and size stay
// the same; only new or changed files are read again. The copies kept in « previous » count in the byte limit. Behaviour tests on a real folder.
internal static class CacheTrim1217Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static void InTemp(Action<string> run)
    {
        var folder=Path.Combine(Path.GetTempPath(),"tensho-cache-1217-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try{run(folder);}
        finally{try{Directory.Delete(folder,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Cache trim 1217: behaviour: a write reads only the headers of new or changed files; what is kept and replaced is unchanged",() => InTemp(folder =>
        {
            var cache=new DataCache(folder);
            for(var index=0;index<60;index++)Check(cache.WriteAsync("family-"+index,"scope","v1",new[]{index},DateTimeOffset.UtcNow).GetAwaiter().GetResult(),"written "+index);
            Check(cache.HeaderReads<=2*60,$"60 writes read {cache.HeaderReads} headers (each one read about once, not 60 × 61 / 2 = 1 830)");
            var before=cache.HeaderReads;
            Check(cache.WriteAsync("family-5","scope","v2",new[]{500},DateTimeOffset.UtcNow).GetAwaiter().GetResult(),"a newer copy of family-5");
            Check(cache.HeaderReads-before<=2,"one more write: one or two headers read: "+(cache.HeaderReads-before));
            var files=Directory.GetFiles(folder,"*.json");
            Check(files.Length==60,"one file per family and scope, the older copy of family-5 replaced: "+files.Length);
            var fresh=new DataCache(folder);
            Check(fresh.WriteAsync("family-60","scope","v1",new[]{60},DateTimeOffset.UtcNow).GetAwaiter().GetResult() && fresh.HeaderReads==61,"a new instance reads every header once: "+fresh.HeaderReads);
            var changed=Directory.GetFiles(folder,"*.json").First();
            File.SetLastWriteTimeUtc(changed,DateTime.UtcNow.AddMinutes(1));
            before=fresh.HeaderReads;
            Check(fresh.WriteAsync("family-61","scope","v1",new[]{61},DateTimeOffset.UtcNow).GetAwaiter().GetResult() && fresh.HeaderReads-before==2,"a file changed meanwhile is read again: "+(fresh.HeaderReads-before));
        })),
        ("Cache trim 1217: the copies kept in previous count in the byte limit",() =>
        {
            var source=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Caching/DataCache.cs"));
            Check(source.Contains("long bytes=kept.Where(entry=>entry.Catalogue).Sum(entry=>entry.File.Length)+PreviousBytes();",StringComparison.Ordinal),"previous counted");
            Check(source.Contains("known.Written==written && known.Length==length ? known.Identity:ReadIdentity(file)",StringComparison.Ordinal),"identity kept while name, time and size stay");
        }),
    ];
}
