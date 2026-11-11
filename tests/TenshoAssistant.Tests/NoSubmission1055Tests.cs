// 0.1055 (audit A038): the project documents no longer describe a submission to the official Dalamud repository as still possible;
// DEC-039 keeps Tensho on its custom repository, with the Dalamud rules applied.
internal static class NoSubmission1055Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static string Read(string file) => File.ReadAllText(Path.GetFullPath(file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Submission: PUBLISHING opens with DEC-039 in both halves",() =>
        {
            var publishing = Read("docs/project/PUBLISHING.md");
            Check(publishing.Contains("**Since DEC-039 (2026-11-05): no submission.**"),"PUBLISHING (English) opens with DEC-039");
            Check(publishing.Contains("DEC-039") && publishing.IndexOf("DEC-039",publishing.IndexOf("\n---",StringComparison.Ordinal),StringComparison.Ordinal) > 0,"PUBLISHING (French) cites DEC-039");
        }),
    ];
}
