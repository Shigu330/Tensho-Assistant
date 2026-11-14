using TenshoAssistant;

// 0.1306 (found by publish.sh on 14/11, « Concurrent data 1169 » on round 10: a second client loading during the voyage conversion threw
// « being used by another process »): the copy set aside that another client is still writing could not be compared and the load failed.
// A copy that cannot be read proves nothing: the next one is looked at, and with none equal the file counts as written again (its entries
// are then merged only where missing). Behaviour test (R28.14): the copy is really held by another handle.
internal static class HeldCopy1306Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset Converted=new(2026,11,1,12,0,0,TimeSpan.Zero);

    private static void InTemp(Action<string> run)
    {
        var top=Path.Combine(Path.GetTempPath(),"tensho-1306-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(top);
        try{run(top);}
        finally{try{Directory.Delete(top,true);}catch(Exception){}}
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Held copy 1306: a copy set aside and still held by another client does not stop the load; a free equal copy still says « not written again »",() => InTemp(top =>
        {
            var file=Path.Combine(top,"VoyageHistory.json");
            File.WriteAllText(file,"[1,2,3]");
            var aside=Path.Combine(top,"before-sqlite-20261101-120000");Directory.CreateDirectory(aside);
            var kept=Path.Combine(aside,"VoyageHistory.json");File.WriteAllText(kept,"[1,2,3]");
            Check(!TenshoDataFile.WrittenAgain(file,Converted),"a free equal copy: not written again");
            using(new FileStream(kept,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
                Check(TenshoDataFile.WrittenAgain(file,Converted),"the only copy held: no throw, counted as written again (merged where missing)");
            var second=Path.Combine(top,"before-sqlite-20261101-130000");Directory.CreateDirectory(second);
            File.WriteAllText(Path.Combine(second,"VoyageHistory.json"),"[1,2,3]");
            using(new FileStream(kept,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
                Check(!TenshoDataFile.WrittenAgain(file,Converted),"one copy held, another equal and free: not written again");
        })),
        ("Held copy 1306: the file itself held: no throw; the file gone: not written again",() => InTemp(top =>
        {
            var file=Path.Combine(top,"Bank.json");
            File.WriteAllText(file,"{}");
            var aside=Path.Combine(top,"before-sqlite-20261101-120000");Directory.CreateDirectory(aside);
            File.WriteAllText(Path.Combine(aside,"Bank.json"),"{}");
            Check(!TenshoDataFile.WrittenAgain(file,Converted),"free: compared, equal");
            using(new FileStream(file,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
                Check(TenshoDataFile.WrittenAgain(file,Converted),"held whole: no throw (the caller's read then gives nothing)");
            File.Delete(file);
            Check(!TenshoDataFile.WrittenAgain(file,Converted),"gone");
        })),
    ];
}
