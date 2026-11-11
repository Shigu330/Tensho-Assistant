// 0.1067 (audit A049): PROPOSALS-TRIAGE says in both halves that it is a frozen snapshot, and the global plan no longer sends readers
// there for the current detail of each point.
internal static class TriageFrozen1067Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Triage: frozen snapshot banner in both halves, global plan calls it the origin",() =>
        {
            var triage = File.ReadAllText(Path.GetFullPath("docs/project/PROPOSALS-TRIAGE.md")).Replace("\r\n","\n");
            Check(triage.Contains("**Frozen snapshot of 2026-11-05, no longer updated.**"),"English banner");
            Check(triage.Contains("**Instantané figé du 2026-11-05, plus mis à jour.**"),"French banner");
            var plan = File.ReadAllText(Path.GetFullPath("docs/project/GLOBAL-PLAN.md"));
            Check(!plan.Contains("detail of each point: [PROPOSALS-TRIAGE]") && !plan.Contains("détail de chaque point : [PROPOSALS-TRIAGE]"),"the global plan no longer calls the triage the detail of each point");
            Check(plan.Contains("origin of each point: [PROPOSALS-TRIAGE](PROPOSALS-TRIAGE.md) (frozen snapshot of that day)") && plan.Contains("origine de chaque point : [PROPOSALS-TRIAGE](PROPOSALS-TRIAGE.md) (instantané figé de ce jour)"),"the global plan calls it the origin, in both halves");
        }),
    ];
}
