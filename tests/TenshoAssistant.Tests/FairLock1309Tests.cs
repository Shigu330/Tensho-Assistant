using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Inventory;

// 0.1309 (X-310): the profile's lock file is fair to a waiting client. Before, a client saving in a row released the lock and took it again within
// microseconds while the other tried every 50 ms: it waited past its limit and its save was refused (« Bank save failed: The bank lock is held by
// another program », CI run 43546325155). Behaviour tests (R28.14): a greedy client in a tight loop and a client that waits once, on the lock
// itself and on two real bank stores sharing one data file; the waiting marks never left behind; a stale mark never blocks.
internal static class FairLock1309Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(),"tensho-fairlock-1309-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root,true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    private static string[] Marks(string root) => Directory.GetFiles(root,"*.wait-*");

    // A client that takes the lock again as soon as it released it, for `seconds` (or until stopped); returns how many times it held it.
    private static Task<int> Greedy(Func<IDisposable> hold,int holdMs,int seconds,CancellationToken stop) => Task.Run(() =>
    {
        var count = 0;
        var until = Environment.TickCount64 + seconds * 1000L;
        while (Environment.TickCount64 < until && !stop.IsCancellationRequested)
        {
            using (hold()) Thread.Sleep(holdMs);
            count++;
        }
        return count;
    });

    private static BankEntry Entry(ulong character,BankStorageKind kind,ulong storage = 0)
    {
        var owner = new InventoryOwnerId(kind == BankStorageKind.Retainer ? storage : character);
        var containers = BankStorageScope.ContainerIds(kind).Select(id => new InventoryContainerSnapshot(new(owner,new(id)),InventoryCoverage.Complete,
            id == BankStorageScope.ContainerIds(kind)[0] ? [new InventoryStack(new(new(5057),false),3,0)] : [])).ToArray();
        return new(new(character,kind,storage),kind == BankStorageKind.Retainer ? "Servant" : "Name (World)",new(DateTimeOffset.UtcNow,"test",containers));
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Fair lock 1309: behaviour: a client taking the lock again in a tight loop lets a waiting client in, well before its limit (it was starved)",() => InTemp(root =>
        {
            var path = Path.Combine(root,"Profile.lock");
            using var stop = new CancellationTokenSource();
            var greedy = Greedy(() => FairFileLock.Hold(path,10_000,error => error),20,6,stop.Token);
            Thread.Sleep(200);
            for (var round = 0; round < 5; round++)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                using (FairFileLock.Hold(path,2_000,error => new TimeoutException("starved: " + error.Message))) { }
                Check(watch.ElapsedMilliseconds < 1_500,"the waiting client got the lock in " + watch.ElapsedMilliseconds + " ms (round " + round + ")");
            }
            stop.Cancel();
            Check(greedy.Wait(TimeSpan.FromSeconds(30)) && greedy.Result > 5,"the greedy client kept saving too: " + greedy.Result);
            Check(Marks(root).Length == 0,"no waiting mark left: " + string.Join(", ",Marks(root)));
        })),
        ("Fair lock 1309: behaviour: two bank stores on one data file, one saving in a tight loop: the other's change goes through (lock wait 2 s)",() => InTemp(root =>
        {
            var folder = Path.Combine(root,"InventoryBank");
            var legacy = Path.Combine(root,"InventoryBank.json");
            var database = Path.Combine(root,TenshoDataDatabase.FileName);
            TenshoDataFile Data() => new(database,(error,message) => { });
            var busy = new PerCharacterBankStore(folder,legacy,10_000,Data());
            var waiting = new PerCharacterBankStore(folder,legacy,2_000,Data());
            Check(busy.Load().Length == 0 && waiting.Load().Length == 0,"both loaded, empty");
            using var stop = new CancellationTokenSource();
            var next = 0UL;
            var greedy = Task.Run(() =>
            {
                var count = 0;
                var until = Environment.TickCount64 + 8_000;
                while (Environment.TickCount64 < until && !stop.IsCancellationRequested)
                {
                    busy.Apply(new BankChange(Put: Entry(7,BankStorageKind.Retainer,1000 + next++)),[]);
                    count++;
                }
                return count;
            });
            Thread.Sleep(300);
            for (var i = 0UL; i < 5; i++)
            {
                var saved = waiting.Apply(new BankChange(Put: Entry(8,BankStorageKind.Retainer,2000 + i)),[]);
                Check(saved.Count(entry => entry.Key.CharacterId == 8) == (int)i + 1,"the waiting store's change " + i + " saved");
            }
            stop.Cancel();
            Check(greedy.Wait(TimeSpan.FromSeconds(30)),"the greedy store stopped");
            var all = new PerCharacterBankStore(folder,legacy,10_000,Data()).Load();
            Check(all.Count(entry => entry.Key.CharacterId == 8) == 5 && all.Count(entry => entry.Key.CharacterId == 7) == greedy.Result,
                "nothing lost on either side: " + all.Count(entry => entry.Key.CharacterId == 7) + " of " + greedy.Result);
            Check(Marks(root).Length == 0,"no waiting mark left");
        })),
        ("Fair lock 1309: the lock held by another program: refused past the wait with the caller's exception, the mark removed; free again: taken at once",() => InTemp(root =>
        {
            var path = Path.Combine(root,"Profile.lock");
            using (new FileStream(path,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None))
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var refused = false;
                try { using (FairFileLock.Hold(path,300,error => new TimeoutException("busy",error))) { } }
                catch (TimeoutException error) when (error.InnerException is IOException) { refused = true; }
                Check(refused && watch.ElapsedMilliseconds >= 250,"refused after its wait: " + watch.ElapsedMilliseconds + " ms");
                Check(Marks(root).Length == 0,"its waiting mark removed");
            }
            var free = System.Diagnostics.Stopwatch.StartNew();
            using (FairFileLock.Hold(path,10_000,error => error)) { }
            Check(free.ElapsedMilliseconds < 1_000,"free lock taken at once: " + free.ElapsedMilliseconds + " ms");
        })),
        ("Fair lock 1309: another client's waiting mark lets it go first (half the wait at most, never a refusal); a stale mark is ignored",() => InTemp(root =>
        {
            var path = Path.Combine(root,"Profile.lock");
            var mark = path + ".wait-" + Guid.NewGuid().ToString("N") + ".lock";
            File.WriteAllBytes(mark,[]);
            Check(FairFileLock.OthersWaiting(path),"the mark seen");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            using (FairFileLock.Hold(path,600,error => error)) { }
            Check(watch.ElapsedMilliseconds >= 250,"it waited for the marked client first: " + watch.ElapsedMilliseconds + " ms");
            File.SetCreationTimeUtc(mark,DateTime.UtcNow - FairFileLock.StaleMark - TimeSpan.FromMinutes(1));
            Check(!FairFileLock.OthersWaiting(path),"a stale mark is not a waiting client");
            var stale = System.Diagnostics.Stopwatch.StartNew();
            using (FairFileLock.Hold(path,10_000,error => error)) { }
            Check(stale.ElapsedMilliseconds < 1_000,"no wait for a stale mark: " + stale.ElapsedMilliseconds + " ms");
        })),
        ("Fair lock 1309: the bank store and the seen prices use the fair lock (one source); a mark ends in .lock, so the desktop link sends no change for it",() =>
        {
            var bank = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Inventory/PerCharacterBankStore.cs"));
            var prices = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Market/SeenPrices.cs"));
            Check(bank.Contains("FairFileLock.Hold(LockPath,lockWaitMs,error=>new BankLockBusyException(LockPath,error))",StringComparison.Ordinal),"bank store");
            Check(prices.Contains("FairFileLock.Hold(path,10_000,error=>error)",StringComparison.Ordinal),"seen prices");
            Check(!bank.Contains("Thread.Sleep(50)",StringComparison.Ordinal) && !prices.Contains("Thread.Sleep(100)",StringComparison.Ordinal),"no polling of its own left");
            var root = Path.Combine(Path.GetTempPath(),"tensho-fairlock-link-" + Guid.NewGuid().ToString("N"));
            Check(TenshoAssistant.Link.LocalLinkProtocol.ChangeCategory(Path.Combine(root,"TenshoAssistant.json"),Path.Combine(root,"TenshoAssistant"),
                Path.Combine(root,"TenshoAssistant","InventoryBank.lock.wait-" + Guid.NewGuid().ToString("N") + ".lock")) is null,"no change sent for a mark");
        }),
    ];
}
