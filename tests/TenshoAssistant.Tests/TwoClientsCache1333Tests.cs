using TenshoAssistant.Caching;

// 0.1333 (X-171, GPT audit of 11/12, last part): two game clients on one profile share DataCache-v1; the purge of 0.1122 was ordered within one
// client only. Behaviour tests (R28.14): two DataCache on the same folder stand for the two clients (each has its own queue and disk lock, as
// two processes); the lock of the profile is a real file opened without sharing, as between processes.
internal static class TwoClientsCache1333Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static T Await<T>(Task<T> task)=>task.WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();

    private static void InTemp(Action<string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-cache-1333-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{run(root);}
        finally{try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Two clients 1333: the other client's write asked during a purge waits, then stays after it",() => InTemp(root =>
        {
            var folder=Path.Combine(root,"DataCache-v1");
            using var first=new DataCache(folder);
            using var second=new DataCache(folder);
            for(var i=0;i<3;i++)Check(Await(first.WriteAsync("sources",$"s{i}","1",new[]{i},DateTimeOffset.UtcNow)),"earlier write "+i);
            var inCopy=-1;var copyStarted=new ManualResetEventSlim();var release=new ManualResetEventSlim();
            var clear=first.ClearAsync(()=>
            {
                inCopy=Directory.GetFiles(first.Root,"*.json").Length;
                copyStarted.Set();release.Wait(10_000);
                return "copy.zip";
            });
            Check(copyStarted.Wait(10_000),"the purge starts its copy");
            var other=second.WriteAsync("sources","other","1",new[]{7},DateTimeOffset.UtcNow);
            Thread.Sleep(300);
            Check(!other.IsCompleted,"the other client's write waits for the purge (before: written during the copy, then erased)");
            release.Set();
            var done=Await(clear);
            Check(inCopy==3 && done.Removed==3 && done.Failed==0,"the copy held the three earlier writes, all three removed: "+done);
            Check(Await(other),"the other client's write succeeds after the purge: "+second.Error);
            Check(Await(first.ReadAsync<int[]>("sources","other","1")) is { } seen && seen.Value[0]==7,"its file is on disk after the purge, seen by both clients");
            Check(Await(second.ReadAsync<int[]>("sources","s0","1")) is null,"an entry written before the purge is gone for the other client too");
            Check(File.Exists(first.LockPath) && !first.LockPath.StartsWith(first.Root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),
                "the lock of the profile lives beside the folder, never removed by the purge");
        })),
        ("Two clients 1333: a lock held too long fails the write (nothing written) and the purge (nothing removed)",() => InTemp(root =>
        {
            var folder=Path.Combine(root,"DataCache-v1");
            using var cache=new DataCache(folder){LockWaitMs=300};
            Check(Await(cache.WriteAsync("sources","a","1",new[]{1},DateTimeOffset.UtcNow)),"one entry");
            using(new FileStream(cache.LockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None))
            {
                Check(!Await(cache.WriteAsync("sources","b","1",new[]{2},DateTimeOffset.UtcNow)),"the write fails while the other client holds the lock");
                Check(cache.Error?.Contains("lock is held",StringComparison.Ordinal)==true,"and says why: "+cache.Error);
                Check(Directory.GetFiles(folder).Length==1,"nothing written, no temporary file left: "+string.Join(", ",Directory.GetFiles(folder).Select(Path.GetFileName)));
                var refused=false;
                try{Await(cache.ClearAsync(()=>"copy.zip"));}catch(DataCache.CacheLockBusyException){refused=true;}
                Check(refused && Directory.GetFiles(folder,"*.json").Length==1,"the purge fails and removes nothing");
            }
            Check(Await(cache.WriteAsync("sources","b","1",new[]{2},DateTimeOffset.UtcNow)),"a write works again once the lock is free");
            Check(Await(cache.ClearAsync()).Removed==2,"and the purge too");
        })),
        ("Two clients 1333: a file the other client reads an instant during the purge is removed all the same",() => InTemp(root =>
        {
            var folder=Path.Combine(root,"DataCache-v1");
            using var cache=new DataCache(folder);
            Check(Await(cache.WriteAsync("sources","a","1",new[]{1},DateTimeOffset.UtcNow)) && Await(cache.WriteAsync("sources","b","1",new[]{2},DateTimeOffset.UtcNow)),"two entries");
            var file=Directory.GetFiles(folder,"*.json")[0];
            var reading=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.Read);
            var released=Task.Run(()=>{Thread.Sleep(120);reading.Dispose();});
            var done=Await(cache.ClearAsync());
            released.Wait(5_000);
            Check(done.Removed==2 && done.Failed==0 && !File.Exists(file),"the file read an instant is removed after a short retry (before: counted as failed): "+done);
        })),
        ("Two clients 1333: wiring (writes, patch reports and purge under the lock of the profile; a vanished file is a miss)",() =>
        {
            var code=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Caching/DataCache.cs")).Replace("\r\n","\n");
            Check(code.Contains("using(HoldProfile(token))") && code.Contains("using(HoldProfile(lifetime.Token))AtomicFile.Write(PatchReportsPath,bytes);")
                && code.Contains("using var held=HoldProfile(lifetime.Token);"),"the three writers hold the lock");
            Check(code.Contains("catch(Exception gone) when(gone is FileNotFoundException or DirectoryNotFoundException){return null;}"),"a file removed by the other client is a miss");
        }),
    ];
}
