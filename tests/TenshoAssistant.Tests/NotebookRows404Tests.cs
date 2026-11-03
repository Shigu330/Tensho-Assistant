// 0.404 (user report « notebooks still loading » while in game; dalamud.log: index 10000 > 304 on the spearfishing notebook): each row is
// asked on its own, a refused row is set aside as unknown, the other notebooks are still published.
internal static class NotebookRows404Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Notebooks: a refused row is set aside, never the whole read",() =>
        {
            var reader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/NotebookReader.Dalamud.cs"));
            Check(reader.Contains("catch(ArgumentOutOfRangeException){refused.Add((kind,row));skipped++;}",StringComparison.Ordinal)
                && reader.Contains("Read(entries,NotebookKind.Spearfishing,spear,",StringComparison.Ordinal),"row guarded");
        }),
    ];
}
