// Test audit of 2026-11-09, batch 2 (0.1021): Notebooks402Tests, NotebookRows404Tests merged into NotebooksTests; every case is kept.
using TenshoAssistant.Gathering;
internal static class NotebooksTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From Notebooks402Tests: 0.402 (g, R-05): notebooks — done by item (done in any notebook), counts per notebook, formula-safe CSV; the gathering function is
        // called only once resolved.
        ("Notebooks: done by item, counts, CSV, gathering function checked",() =>
        {
            var state = new NotebookState([new(NotebookKind.Fishing,10,true),new(NotebookKind.Fishing,11,false),new(NotebookKind.Gathering,11,true),new(NotebookKind.Spearfishing,12,false)],DateTimeOffset.UtcNow);
            Check(state.Done(10)==true && state.Done(11)==true && state.Done(12)==false && state.Done(99) is null,"done by item");
            Check(state.Count(NotebookKind.Fishing)==(1,2) && state.Count(NotebookKind.Gathering)==(1,1),"counts");
            var csv = Notebooks.Csv(state.Entries,id=>id==10 ? "=bad":"Fish "+id);
            Check(csv.Split('\n',StringSplitOptions.RemoveEmptyEntries).Length==5 && csv.Contains("\"'=bad\"",StringComparison.Ordinal),"csv");
            Check(Read("Gathering/NotebookReader.Dalamud.cs").Contains("var gatheringReadable=(nint)QuestManager.MemberFunctionPointers.IsGatheringItemGathered!=0;",StringComparison.Ordinal),"function checked");
        }),
        // From NotebookRows404Tests: 0.404 (user report « notebooks still loading » while in game; dalamud.log: index 10000 > 304 on the spearfishing notebook): each row is
        // asked on its own, a refused row is set aside as unknown, the other notebooks are still published.
        ("Notebooks: a refused row is set aside, never the whole read",() =>
        {
            var reader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/NotebookReader.Dalamud.cs"));
            Check(reader.Contains("catch(ArgumentOutOfRangeException){refused.Add((kind,row));skipped++;}",StringComparison.Ordinal)
                && reader.Contains("Read(entries,NotebookKind.Spearfishing,spear,",StringComparison.Ordinal),"row guarded");
        }),
    ];
}
