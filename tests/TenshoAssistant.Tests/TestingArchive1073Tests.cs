// 0.1073 (audit A043, first part): TESTING.md no longer carries the verification journal up to 0.99; one note says it stays in
// the git history, and no open test box was lost with it.
internal static class TestingArchive1073Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("TESTING: archive up to 0.99 removed, pointer to the git history kept",() =>
        {
            var testing = File.ReadAllText(Path.GetFullPath("docs/project/TESTING.md")).Replace("\r\n","\n");
            var at = testing.IndexOf("\n### Archive\n");
            Check(at > 0,"the Archive heading stays, with its note");
            var tail = testing[at..];
            Check(tail.Contains("a été retiré en 0.1073") && tail.Contains("git show 1171209:docs/project/TESTING.md"),"the note says where the journal stays");
            Check(!tail.Contains("\n## ") && !tail.Contains("- [x]") && !tail.Contains("- [ ]"),"nothing of the old journal is left after the note");
            Check(!testing.Contains("## UI-005 — version 0.4.0") && !testing.Contains("## BANK-001 — version 0.5.22"),"the old per-version sections are gone");
        }),
    ];
}
