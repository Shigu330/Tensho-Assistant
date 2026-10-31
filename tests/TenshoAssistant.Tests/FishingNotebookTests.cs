using TenshoAssistant.Gathering;

internal static class FishingNotebookTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static FishingNotebookEntry Entry(uint fish,uint item)=>new(fish,item,99,"Description du jeu","Tome du jeu",true,false,null,null,false);
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Fish notebook keeps fish row identities separate from item IDs and preserves variants",()=>
        {
            var rows=new[]{Entry(2,42),Entry(1,42),Entry(42,43)};var index=new FishingNotebookIndex(rows);
            Check(index.Count==3 && index.ForItem(42).Select(row=>row.FishId).SequenceEqual(new uint[]{1,2}),"variant identity and deterministic order");
            Check(index.ForItem(2).Count==0 && index.ForItem(43).Single().FishId==42,"never confuses row and Item IDs");
        }),
        ("Fish notebook restriction codes preserve known negative positive and unknown values",()=>
        {
            Check(FishingNotebookIndex.Flag(0)==false && FishingNotebookIndex.Flag(1)==true,"native 0 and 1 distinct");
            foreach(var value in new byte[]{2,7,128,255})Check(FishingNotebookIndex.Flag(value) is null,"unrecognized value not absence");
            var unknown=Entry(1,42);var index=new FishingNotebookIndex(new[]{unknown});
            Check(index.ForItem(42).Single().TimeRestricted is null && !index.ForItem(42).Single().NoteLinked,"missing linked note remains unknown");
        }),
        ("Fish notebook owns producer collections and rejects conflicting row identities",()=>
        {
            var rows=new List<FishingNotebookEntry>{Entry(1,42),Entry(1,42)};var index=new FishingNotebookIndex(rows);rows.Clear();
            Check(index.Count==1 && index.ForItem(42).Single().Description=="Description du jeu","managed immutable values and exact duplicate coalescing");
            var rejected=false;try{new FishingNotebookIndex(new[]{Entry(1,42),Entry(1,43)});}catch(InvalidOperationException){rejected=true;}
            Check(rejected,"conflicting fish identity not silently overwritten");
        }),
        ("Fish notebook invalid rows cancellation and shared catalogue lifecycle preserve explicit coverage",()=>
        {
            var invalid=new[]{Entry(0,42),Entry(1,0)};Check(new FishingNotebookIndex(invalid).Count==0,"invalid identity ignored");
            using var cancellation=new CancellationTokenSource();cancellation.Cancel();var cancelled=false;
            try{new FishingNotebookIndex(new[]{Entry(1,42)},cancellation.Token);}catch(OperationCanceledException){cancelled=true;}
            Check(cancelled,"worker construction observes cancellation");
            var catalog=new GatheringCatalog([],"fixture",fishing:new[]{Entry(1,42)});
            Check(catalog.Count==0 && catalog.Fishing.Count==1 && catalog.Fishing.ForItem(42).Count==1,"notebook exists without inventing a gathering location");
            Check(new GatheringCatalog([],"legacy").Fishing.Count==0,"older catalogue callers remain compatible");
            var service=new GatheringCatalogService();service.Publish(catalog);service.Clear();Check(service.State.Catalog is null,"one shared catalogue cleared as a unit");
        }),
    };
}
