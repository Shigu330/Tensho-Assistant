using TenshoAssistant.Market;

// 0.1216 (X-171 part 1, GPT audit of 12/11, R02): two game clients on one profile both loaded MarketSeen.json at their start and wrote back their
// whole copy: the second save erased what the first had seen since. The write now holds the file's lock and joins what the file holds. Behaviour
// tests (R28, concurrency): two stores as two clients on a real file, in turn and at the same time; the same item seen by both; a save replayed.
internal static class TwoClients1216Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=DateTimeOffset.UtcNow.AddHours(-1);

    private static SeenPriceStore Client(string path)
    {
        var store=new SeenPriceStore();
        if(SeenPriceStore.Read(path) is { } saved)store.Load(saved);
        return store;
    }
    private static void Save(SeenPriceStore store,string path)
    {
        var entries=store.SnapshotToSave(out var stamp);
        SeenPriceStore.Write(path,entries);store.MarkSaved(stamp);
    }
    private static void InTemp(Action<string> run)
    {
        var folder=Path.Combine(Path.GetTempPath(),"tensho-1216-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try{run(Path.Combine(folder,"MarketSeen.json"));}
        finally{try{Directory.Delete(folder,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Two clients 1216: behaviour: each client's prices survive the other's save; the same item keeps the newer offers and both clients' sales",() => InTemp(path =>
        {
            var first=Client(path);var second=Client(path);
            first.SeeListings(40,5057,1,[new SeenListing(100,false,3)],At);first.SeeSales(40,5057,[new SeenSale(90,false,1,At.AddMinutes(-30))],At);
            second.SeeListings(40,5058,1,[new SeenListing(200,true,1)],At);
            second.SeeListings(40,5057,2,[new SeenListing(120,false,2)],At.AddMinutes(10));second.SeeSales(40,5057,[new SeenSale(95,true,2,At.AddMinutes(-20))],At.AddMinutes(10));
            Save(first,path);Save(second,path);
            var file=SeenPriceStore.Read(path)!;
            Check(file.Count==2 && file.Any(entry=>entry.Item==5058),"the first client's item kept after the second's save: "+file.Count);
            var both=file.Single(entry=>entry.Item==5057);
            Check(both.Listings.Single().Price==120 && both.ListingsAt==At.AddMinutes(10),"the newer offers (the second client's)");
            Check(both.Sales.Count==2 && both.Sales.Any(sale=>sale.Price==90) && both.Sales.Any(sale=>sale.Price==95),"the sales of both clients");
            Save(first,path);Save(second,path);Save(first,path);
            var again=SeenPriceStore.Read(path)!;
            Check(again.Count==2 && again.Single(entry=>entry.Item==5057).Sales.Count==2,"saves replayed: nothing doubled");
            Check(Client(path).Get(40,5058) is { } read && read.Listings.Single().Price==200,"a client started after reads both");
        })),
        ("Two clients 1216: behaviour: two clients saving at the same time lose nothing (the file's lock)",() => InTemp(path =>
        {
            var clients=new[]{new SeenPriceStore(),new SeenPriceStore()};
            Parallel.For(0,2,index =>
            {
                for(var round=0;round<15;round++)
                {
                    clients[index].SeeListings(40,(uint)(10_000+index*100+round),1,[new SeenListing(50+round,false,1)],At);
                    Save(clients[index],path);
                }
            });
            var file=SeenPriceStore.Read(path)!;
            Check(file.Count==30,"every item of both clients in the file: "+file.Count);
        })),
        ("Two clients 1216: the join keeps nothing older than 90 days from the file, and a file that cannot be read is replaced as before",() => InTemp(path =>
        {
            var old=new SeenMarketEntry{World=40,Item=1,Listings=[new SeenListing(1,false,1)],ListingsAt=DateTimeOffset.UtcNow.AddDays(-91)};
            Check(SeenPriceStore.Joined([],[old],DateTimeOffset.UtcNow).Count==0,"an entry older than 90 days in the file: dropped");
            File.WriteAllText(path,"{ broken");
            var client=new SeenPriceStore();client.SeeListings(40,5057,1,[new SeenListing(100,false,3)],At);Save(client,path);
            Check(SeenPriceStore.Read(path)!.Single().Item==5057,"a broken file replaced by this client's entries");
        })),
    ];
}
