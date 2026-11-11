// 0.1074 (audit A043 and A045): the old queue's « étape h » / « step h » is phase 6 of the global plan; the working documents no
// longer use the old name (evidence files keep theirs: they record their day).
internal static class StepH1074Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Working documents: « étape h » is now phase 6",() =>
        {
            foreach (var name in new[]{"TESTING.md","CRAFT-GATHER-PROPOSALS.md","G-PROPOSALS.md","MAP-PROPOSALS.md"})
            {
                var text = File.ReadAllText(Path.GetFullPath("docs/project/" + name));
                Check(!text.Contains("étape h") && !text.Contains("step h"),$"{name} no longer names the old step h");
            }
            var testing = File.ReadAllText(Path.GetFullPath("docs/project/TESTING.md"));
            Check(testing.Contains("campagne de tests en jeu de la fin (phase 6)"),"TESTING headings say phase 6");
        }),
    ];
}
