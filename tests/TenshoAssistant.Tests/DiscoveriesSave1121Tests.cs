using TenshoAssistant.Discoveries;
using TenshoAssistant.Market;

// 0.1121 (X-142 part 1, audit A04 and A09): the discoveries and the seen prices are marked written only once their file is on disk; a write
// that fails keeps them to save, a change made during the copy goes in the next save; at unload a running save is waited for, then the rest
// written; the craft recorder calls the game's function by the dispose-safe call in the competitive cut too. Behaviour tests (R28): real
// writes that fail (the file's folder is a file), then succeed.
internal static class DiscoveriesSave1121Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static readonly DateTimeOffset At=new(2026,11,12,1,0,0,TimeSpan.Zero);

    private static void InTemp(Action<string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-save-1121-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{run(root);}
        finally{try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discoveries 1121: a save that fails on disk keeps the base to save; a change during the copy goes in the next save; a written save clears it",() => InTemp(root =>
        {
            var store=new DiscoveryStore();
            store.SeeSource(DiscoveryKind.FestivalSeen,1,1,6,At);
            Check(store.Dirty,"a sighting is to save");
            var blocked=Path.Combine(root,"blocked");File.WriteAllText(blocked,"a file where the folder should be");
            var file=store.SnapshotToSave(At,out var stamp);
            var failed=false;
            try{DiscoveryFiles.Write(Path.Combine(blocked,"Discoveries.json"),file);store.MarkSaved(stamp);}
            catch(Exception error) when(error is IOException or UnauthorizedAccessException){failed=true;}
            Check(failed && store.Dirty,"the write failed: still to save (the old code had cleared it before writing)");
            var good=Path.Combine(root,"Discoveries.json");
            file=store.SnapshotToSave(At,out stamp);
            store.SeeSource(DiscoveryKind.FestivalSeen,2,1,6,At);
            DiscoveryFiles.Write(good,file);store.MarkSaved(stamp);
            Check(File.Exists(good) && store.Dirty,"written, but a sighting made during the copy keeps it to save");
            file=store.SnapshotToSave(At,out stamp);
            DiscoveryFiles.Write(good,file);store.MarkSaved(stamp);
            Check(!store.Dirty && DiscoveryFiles.Read(good)!.Facts.Count==2,"the next save holds both and clears it");
            store.MarkSaved(stamp-1);
            Check(!store.Dirty,"an older stamp changes nothing");
            store.Snapshot(At,false);store.SeeSource(DiscoveryKind.FestivalSeen,3,1,6,At);store.Snapshot(At);
            Check(store.Dirty,"an export or a check never marks it written");
        })),
        ("Discoveries 1121: the seen prices too: a failed write keeps them to save, a written one clears them unless they changed meanwhile",() => InTemp(root =>
        {
            var prices=new SeenPriceStore();
            prices.SeeSales(40,5057,[new SeenSale(100,false,1,At)],At);
            var blocked=Path.Combine(root,"blocked");File.WriteAllText(blocked,"x");
            var list=prices.SnapshotToSave(out var stamp);
            var failed=false;
            try{SeenPriceStore.Write(Path.Combine(blocked,"MarketSeen.json"),list);prices.MarkSaved(stamp);}
            catch(Exception error) when(error is IOException or UnauthorizedAccessException){failed=true;}
            Check(failed && prices.Dirty,"failed: still to save");
            list=prices.SnapshotToSave(out stamp);
            prices.SeeSales(40,5058,[new SeenSale(200,false,1,At)],At);
            SeenPriceStore.Write(Path.Combine(root,"MarketSeen.json"),list);prices.MarkSaved(stamp);
            Check(prices.Dirty,"a price seen during the write stays to save");
            list=prices.SnapshotToSave(out stamp);SeenPriceStore.Write(Path.Combine(root,"MarketSeen.json"),list);prices.MarkSaved(stamp);
            Check(!prices.Dirty && list.Count==2,"then saved whole");
        })),
        ("Discoveries 1121: wiring (marked after the write, unload waits for a running save, the recorder's safe call in the cut)",() =>
        {
            var save=Read("src/TenshoAssistant/MainWindow.Discoveries.cs");
            Check(save.Contains("var file=store.SnapshotToSave(DateTimeOffset.UtcNow,out var stamp);lock(sharedWritten)DiscoveryFiles.WriteSplit(path,file,sharedWritten,held);store.MarkSaved(stamp);"),"discoveries marked after the write");
            Check(save.Contains("Market.SeenPriceStore.Write(pricesPath,prices);seenPrices!.MarkSaved(pricesStamp);"),"prices marked after the write");
            Check(save.Contains("while(Volatile.Read(ref discoverySaving)==1 && Environment.TickCount64<until)Thread.Sleep(20);"),"unload waits (bounded) for a running save");
            Check(Read("src/TenshoAssistant/MainWindow.Lifetime.cs").Contains("FlushDiscoveries();"),"unload flushes");
            var recorder=Read("src/TenshoAssistant/Craft/CraftRecorder.Dalamud.cs");
            Check(!recorder.Contains(".Original(") && recorder.Contains("if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return result;"),"the cut uses the safe call too");
        }),
    ];
}
