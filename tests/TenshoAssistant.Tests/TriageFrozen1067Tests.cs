// 0.1067 (audit A049): PROPOSALS-TRIAGE was a frozen snapshot, and the global plan calls it the origin of the points, not their detail.
// 0.1348 (X-357): the snapshot is removed (git history keeps it); the global plan reaches it by a permanent link, in both halves.
internal static class TriageFrozen1067Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private const string Link = "(https://github.com/Shigu330/Tensho-Assistant/blob/024c426c1186a2e8f51e09087996a9c55208c28a/docs/project/PROPOSALS-TRIAGE.md)";

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Triage: removed, the global plan calls it the origin by its permanent link",() =>
        {
            Check(!File.Exists(Path.GetFullPath("docs/project/PROPOSALS-TRIAGE.md")),"PROPOSALS-TRIAGE removed");
            var plan = File.ReadAllText(Path.GetFullPath("docs/project/GLOBAL-PLAN.md"));
            Check(!plan.Contains("detail of each point: [PROPOSALS-TRIAGE]") && !plan.Contains("détail de chaque point : [PROPOSALS-TRIAGE]"),"the global plan no longer calls the triage the detail of each point");
            Check(plan.Contains("origin of each point: [PROPOSALS-TRIAGE]" + Link + " (frozen snapshot of that day)") && plan.Contains("origine de chaque point : [PROPOSALS-TRIAGE]" + Link + " (instantané figé de ce jour)"),"the global plan calls it the origin, in both halves");
        }),
    ];
}
