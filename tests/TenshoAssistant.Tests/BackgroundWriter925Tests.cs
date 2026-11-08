using TenshoAssistant;
using TenshoAssistant.FreeCompany;

// X-76 2b H10 (0.925): saved files written off the framework thread, in order; failures reported; the voyage history through it.
internal static class BackgroundWriter925Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Background writer 925: order kept, failure reported, flush waits; gil, character and voyage writers use it",() =>
        {
            var seen=new List<int>();var failures=new List<string>();
            var writer=new BackgroundWriter((_,message)=>{lock(failures)failures.Add(message);});
            for(var index=0;index<20;index++){var value=index;writer.Enqueue("never",()=>{Thread.Sleep(1);lock(seen)seen.Add(value);});}
            writer.Enqueue("failed",()=>throw new IOException("disk"));
            writer.Enqueue("never",()=>{lock(seen)seen.Add(99);});
            Check(writer.Flush(TimeSpan.FromSeconds(10)),"flushed");
            Check(seen.SequenceEqual(Enumerable.Range(0,20).Append(99)),"in order, a failure does not stop the queue");
            Check(failures.SequenceEqual(["failed"]),"failure reported");
            var folder=Path.Combine(Path.GetTempPath(),"tensho-writer-"+Guid.NewGuid().ToString("N"));
            try
            {
                var path=Path.Combine(folder,"VoyageHistory.json");
                var store=new VoyageHistoryStore(path,(_,_)=>{});
                store.Observe([new VoyageVessel{HouseId=1,Kind=VesselKind.Submarine,Slot=1,Name="S",RegisteredAt=DateTimeOffset.UtcNow.AddDays(-1),ReturnAt=DateTimeOffset.UtcNow.AddHours(-1),Loot=[new(10,2)]}]);
                Check(store.Flush(TimeSpan.FromSeconds(10)) && File.Exists(path) && VoyageHistory.Read(path).Count==store.Records.Count,"voyage history written in the background");
            }
            finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
            string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
            Check(Read("Inventory/GilWatcher.Dalamud.cs").Contains(".Enqueue(\"Gil journal could not be saved.\"") && Read("Character/CharacterStatusWatcher.Dalamud.cs").Contains("private void QueueWrite(ulong id)"),"writers");
        }),
    ];
}
