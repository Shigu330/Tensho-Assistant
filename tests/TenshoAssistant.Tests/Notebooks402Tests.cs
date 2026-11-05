using TenshoAssistant.Gathering;

// 0.402 (g, R-05): notebooks — done by item (done in any notebook), counts per notebook, formula-safe CSV; the gathering function is
// called only once resolved.
internal static class Notebooks402Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Notebooks: done by item, counts, CSV, gathering function checked",() =>
        {
            var state = new NotebookState([new(NotebookKind.Fishing,10,true),new(NotebookKind.Fishing,11,false),new(NotebookKind.Gathering,11,true),new(NotebookKind.Spearfishing,12,false)],DateTimeOffset.UtcNow);
            Check(state.Done(10)==true && state.Done(11)==true && state.Done(12)==false && state.Done(99) is null,"done by item");
            Check(state.Count(NotebookKind.Fishing)==(1,2) && state.Count(NotebookKind.Gathering)==(1,1),"counts");
            var csv = Notebooks.Csv(state.Entries,id=>id==10 ? "=bad":"Fish "+id);
            Check(csv.Split('\n',StringSplitOptions.RemoveEmptyEntries).Length==5 && csv.Contains("\"'=bad\"",StringComparison.Ordinal),"csv");
            Check(Read("Gathering/NotebookReader.Dalamud.cs").Contains("var gatheringReadable=(nint)QuestManager.MemberFunctionPointers.IsGatheringItemGathered!=0;",StringComparison.Ordinal),"function checked");
        }),
    ];
}
