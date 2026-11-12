using TenshoAssistant.Caching;

// 0.1122 (X-142 part 2, audit A05): emptying the cache is one operation of its write queue: the copy is taken after every earlier write,
// the purge follows under the same disk lock, a write asked meanwhile waits and survives, a file in use is counted (partial purge said),
// a copy that fails removes nothing. Behaviour tests (R28): a real cache folder, a write racing the purge, a file held open.
internal static class CacheClear1122Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static T Await<T>(Task<T> task)=>task.WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();

    private static void InTemp(Action<string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-cache-1122-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{run(root);}
        finally{try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Cache 1122: the copy holds every earlier write, a write asked during the purge waits and survives it",() => InTemp(root =>
        {
            using var cache=new DataCache(Path.Combine(root,"DataCache"));
            for(var i=0;i<3;i++)_=cache.WriteAsync("sources",$"s{i}","1",new[]{i},DateTimeOffset.UtcNow);
            var inCopy=-1;var copyStarted=new ManualResetEventSlim();var release=new ManualResetEventSlim();
            var clear=cache.ClearAsync(()=>
            {
                inCopy=Directory.GetFiles(cache.Root,"*.json").Length;
                copyStarted.Set();release.Wait(10_000);
                return "copy.zip";
            });
            Check(copyStarted.Wait(10_000),"the copy starts");
            var late=cache.WriteAsync("sources","late","1",new[]{9},DateTimeOffset.UtcNow);
            Thread.Sleep(200);
            Check(!late.IsCompleted,"the later write waits for the purge");
            release.Set();
            var done=Await(clear);
            Check(inCopy==3 && done.Removed==3 && done.Failed==0 && done.Backup=="copy.zip","the copy held the three earlier writes, all three removed: "+done);
            Check(Await(late) && Await(cache.ReadAsync<int[]>("sources","late","1")) is { } kept && kept.Value[0]==9,"the later write is on disk after the purge");
            Check(Await(cache.ReadAsync<int[]>("sources","s0","1")) is null,"an earlier entry is gone");
        })),
        ("Cache 1122: a file in use is counted (partial purge said); a copy that fails removes nothing",() => InTemp(root =>
        {
            using var cache=new DataCache(Path.Combine(root,"DataCache"));
            Check(Await(cache.WriteAsync("sources","a","1",new[]{1},DateTimeOffset.UtcNow)) && Await(cache.WriteAsync("sources","b","1",new[]{2},DateTimeOffset.UtcNow)),"two entries");
            var failed=false;
            try{Await(cache.ClearAsync(()=>throw new IOException("disk full (test)")));}catch(IOException){failed=true;}
            Check(failed && Directory.GetFiles(cache.Root,"*.json").Length==2,"the copy failed: nothing removed");
            Check(Await(cache.WriteAsync("sources","c","1",new[]{3},DateTimeOffset.UtcNow)),"a write after a failed purge still works");
            var held=Directory.GetFiles(cache.Root,"*.json")[0];
            using(new FileStream(held,FileMode.Open,FileAccess.Read,FileShare.Read))
            {
                var done=Await(cache.ClearAsync());
                Check(done.Failed==1 && done.Removed==2,"one file in use counted: "+done);
            }
            Check(File.Exists(held),"the file in use stays (said, not hidden)");
        })),
        ("Cache 1122: wiring (the window gives the copy to the purge, a partial purge is said in en and fr)",() =>
        {
            var window=Read("src/TenshoAssistant/MainWindow.Caching.cs");
            Check(window.Contains("var done=await store.ClearAsync(()=>BackupZip.Create(root,backups,\"cache\",DateTimeOffset.UtcNow)).ConfigureAwait(false);") && !window.Contains("var copy=BackupZip.Create(root,backups"),"one operation");
            Check(window.Contains("localization.Format(\"CacheUi.ClearedPartial\",done.Removed,done.Failed,done.Backup ?? \"—\")"),"partial purge said");
            foreach(var lang in new[]{"en","fr"})Check(Read($"src/TenshoAssistant/Localization/Languages/{lang}.json").Contains("\"ClearedPartial\": "),"text in "+lang);
        }),
    ];
}
