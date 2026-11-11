using TenshoAssistant.Caching;

// 0.1047 (X-117, part 2): Settings › Data › cache shows what the last game patch changed in the cached catalogues.
internal static class PatchShow1047Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static PatchArrayDiff Diff(int added,int removed,int changed)=>new("List",added,removed,changed,0,[],[],[]);
    private static PatchCatalogReport Report(string family,string scope,string? skipped,int added,int removed,int changed,string old="2026.10.01.0000.0000",string now="2026.11.03.0000.0000")=>
        new(family,scope,old,now,DateTimeOffset.UnixEpoch,skipped,skipped is null ? [Diff(added,removed,changed)]:[]);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Patch show 1047: one summary per family, scopes summed, skipped scopes left out of the counts",() =>
        {
            var rows=PatchCatalogDiff.Summaries([
                Report("catalog-recipes","en",null,1,0,2),
                Report("catalog-items","en",null,3,1,4,old:"2026.09.01.0000.0000"),
                Report("catalog-items","fr",null,2,0,1),
                Report("catalog-gathering","en","record shape changed",0,0,0),
                Report("catalog-unknown","en",null,0,0,0)]);
            Check(string.Join(",",rows.Select(row=>row.Family))=="catalog-gathering,catalog-items,catalog-recipes,catalog-unknown","family order");
            var items=rows[1];
            Check(items.Compared && items.Added==5 && items.Removed==1 && items.Changed==5,"items summed");
            Check(items.OldGame=="2026.09.01.0000.0000" && items.NewGame=="2026.11.03.0000.0000","oldest before, newest after");
            Check(!rows[0].Compared && rows[0].Added==0,"skipped family");
            Check(rows[1].Key=="Items" && rows[0].Key=="Gathering" && rows[3].Key is null,"family keys");
            Check(PatchCatalogDiff.Summaries([]).Count==0,"no report");
        }),
        ("Patch show 1047: every known family has its text in English and French; the cache section draws the summaries",() =>
        {
            foreach (var family in new[]{"catalog-items","catalog-recipes","catalog-gathering","catalog-map-positions","catalog-npc-vendors"})
                Check(PatchCatalogDiff.FamilyKey(family) is not null,"key "+family);
            foreach (var language in new[]{"en","fr"})
            {
                var text=File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                foreach (var key in new[]{"PatchTitle","PatchTip","PatchNone","PatchLine","PatchSkipped","PatchItems","PatchRecipes","PatchGathering","PatchMapPositions","PatchVendors"})
                    Check(text.Contains($"\"{key}\": "),language+" "+key);
            }
            var code=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Caching.cs"));
            Check(code.Contains("DrawPatchChanges();") && code.Contains("PatchCatalogDiff.Summaries(reports)") && code.Contains("EmptyState(\"CacheUi.PatchNone\")"),"wiring");
            Check(code.Contains("ReferenceEquals(reports,patchReportsShown)"),"summaries not rebuilt every frame");
        }),
    ];
}
