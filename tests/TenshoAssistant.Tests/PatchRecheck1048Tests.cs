using TenshoAssistant.Caching;
using TenshoAssistant.Discoveries;

// 0.1048 (X-117, part 2b): the discoveries to recheck after a game patch (changed map cells, changed vendors).
internal static class PatchRecheck1048Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset Patch=new(2026,11,4,0,0,0,TimeSpan.Zero);
    private static PatchCatalogReport Report(string family,string? skipped,string[] removed,string[] added)=>
        new(family,"en","2026.10.01.0000.0000","2026.11.03.0000.0000",Patch,skipped,skipped is null ? [new("Rows",added.Length,removed.Length,0,0,added,removed,[])]:[]);
    private static DiscoveryFact Fact(DiscoveryKind kind,uint subject,uint map,float x,float z,DateTimeOffset last)
    {
        var cell=DiscoveryStore.Cell(x,z);
        return new DiscoveryFact{Kind=kind,Subject=subject,Territory=1,Map=map,CellX=cell.X,CellZ=cell.Z,X=x,Z=z,Seen=1,First=last,Last=last};
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Patch recheck 1048: facts seen before the patch near a changed map position or about a changed vendor",() =>
        {
            var before=Patch.AddDays(-2);
            var reports=new[]{
                Report(PatchRecheck.PositionsFamily,null,["{\"Kind\":1,\"Id\":5,\"MapId\":12,\"X\":120.5,\"Z\":-30,\"Radius\":0}"],["{\"Kind\":1,\"Id\":5,\"MapId\":12,\"X\":400,\"Z\":400,\"Radius\":0}"]),
                Report(PatchRecheck.VendorsFamily,null,["{\"NpcId\":1000236,\"Name\":\"A\",\"ShopIds\":[],\"Locations\":[]}"],[]),
                Report("catalog-items",null,["{\"MapId\":12,\"X\":900,\"Z\":900}"],[]),
                Report(PatchRecheck.PositionsFamily,"record shape changed",[],[])};
            var facts=new[]{
                Fact(DiscoveryKind.MonsterPlace,1,12,160,-10,before),   // next cell of the old place: recheck
                Fact(DiscoveryKind.FishCatch,2,12,410,420,before),      // the new place's cell: recheck
                Fact(DiscoveryKind.MonsterPlace,3,12,120,-30,Patch.AddHours(1)), // seen after the patch: kept
                Fact(DiscoveryKind.MonsterPlace,4,13,120,-30,before),   // other map: kept
                Fact(DiscoveryKind.MonsterPlace,5,12,900,900,before),   // only an item row « there »: not a place, kept
                new DiscoveryFact{Kind=DiscoveryKind.NpcPlace,Subject=1000236,Territory=1,Map=12,CellX=40,CellZ=40,Seen=1,First=before,Last=before}, // vendor
                new DiscoveryFact{Kind=DiscoveryKind.Desynthesis,Subject=7,Detail=8,Seen=1,First=before,Last=before}}; // no place
            var result=PatchRecheck.Find(reports,facts);
            Check(result.ChangedCells==2 && result.ChangedVendors==1,"changed cells and vendors "+result);
            Check(result.Places==2 && result.Vendors==1 && result.Facts==3,"facts to recheck "+result);
        }),
        ("Patch recheck 1048: nothing to recheck without a compared report; keys that are not rows are skipped",() =>
        {
            var fact=Fact(DiscoveryKind.MonsterPlace,1,12,120,-30,Patch.AddDays(-1));
            Check(PatchRecheck.Find([],[fact]).Facts==0,"no report");
            Check(PatchRecheck.Find([Report(PatchRecheck.PositionsFamily,"same game version",[],[])],[fact]).Facts==0,"skipped report");
            Check(PatchRecheck.Find([Report(PatchRecheck.PositionsFamily,null,["17","{broken"],[])],[fact]).Facts==0,"identifier keys and broken text");
        }),
        ("Patch recheck 1048: counted off the frame once per report list, with its text in English and French",() =>
        {
            var code=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Caching.cs"));
            Check(code.Contains("DrawPatchRecheck(reports);") && code.Contains("_=Task.Run(()=>") && code.Contains("PatchRecheck.Find(reports,discoveries.Snapshot(DateTimeOffset.UtcNow,saving:false).Facts)"),"wiring off the frame");
            Check(code.Contains("ReferenceEquals(reports,patchRecheckFor)") && code.Contains("Tip(\"CacheUi.PatchRecheckTip\")"),"once per list, with its tip");
            foreach (var language in new[]{"en","fr"})
            {
                var text=File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                Check(text.Contains("\"PatchRecheck\": ") && text.Contains("\"PatchRecheckTip\": "),language);
            }
        }),
    ];
}
