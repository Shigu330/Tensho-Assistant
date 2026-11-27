using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.FreeCompany;

// 0.1306 (found by publish.sh on 14/11, « Concurrent data 1169 » on round 10: a second client loading during the voyage conversion threw
// « being used by another process »): the copy set aside that another client is still writing could not be compared and the load failed.
// A copy that cannot be read proves nothing: the next one is looked at, and with none equal the file counts as written again (its entries
// are then merged only where missing). Behaviour test (R28.14): the copy is really held by another handle. X-574 (0.1793, the user's choice of
// 17/11): nothing compares nor merges the files of before any more (TenshoDataFile.WrittenAgain is gone): a copy set aside, or the file itself,
// held by another program, never stops a load; the cases say that now.
internal static class HeldCopy1306Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset Converted=new(2026,11,1,12,0,0,TimeSpan.Zero);

    private static void InTemp(Action<string> run)
    {
        var top=Path.Combine(Path.GetTempPath(),"tensho-1306-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(top);
        try{run(top);}
        finally{SqliteConnection.ClearAllPools();try{Directory.Delete(top,true);}catch(Exception){}}
    }
    private static VoyageRecord Voyage(int slot)=>new(17,VesselKind.Submarine,slot,"Vessel "+slot,50,Converted.AddDays(20),null,[1u],["Deep-sea Site 1"],[new VoyageLoot(5,slot+1)]);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Held copy 1306: a copy set aside and still held by another client does not stop the load; it is never looked at (X-574)",() => InTemp(top =>
        {
            var file=Path.Combine(top,"VoyageHistory.json");
            var data=new TenshoDataFile(Path.Combine(top,TenshoDataDatabase.FileName),(_,_)=>{});
            new VoyageHistoryData(file,data).Save([Voyage(1)],null,Converted.AddDays(20));
            var aside=Path.Combine(top,"before-sqlite-20261101-120000");Directory.CreateDirectory(aside);
            var kept=Path.Combine(aside,"VoyageHistory.json");VoyageHistory.Write(kept,[Voyage(2)]);
            using(new FileStream(kept,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
                Check(new VoyageHistoryData(file,new TenshoDataFile(Path.Combine(top,TenshoDataDatabase.FileName),(_,_)=>{})).Load().Select(record=>record.Slot).SequenceEqual([1]),"the copy held: no throw, the data file's voyage only");
            Check(VoyageHistory.Read(kept).Single().Slot==2,"the copy left as it was");
        })),
        ("Held copy 1306: the file itself held: no throw, still said as left aside; the file gone: nothing said",() => InTemp(top =>
        {
            var file=Path.Combine(top,"VoyageHistory.json");
            VoyageHistory.Write(file,[Voyage(3)]);
            var said=new List<string>();
            using(new FileStream(file,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
                Check(new VoyageHistoryData(file,new TenshoDataFile(Path.Combine(top,TenshoDataDatabase.FileName),(_,_)=>{},said.Add)).Load().Count==0 && said.Count(line=>line.Contains("old format",StringComparison.Ordinal))==1,"held whole: no throw, said once");
            File.Delete(file);
            said.Clear();
            Check(new VoyageHistoryData(file,new TenshoDataFile(Path.Combine(top,TenshoDataDatabase.FileName),(_,_)=>{},said.Add)).Load().Count==0 && said.All(line=>!line.Contains("old format",StringComparison.Ordinal)),"gone: nothing said");
        })),
    ];
}
