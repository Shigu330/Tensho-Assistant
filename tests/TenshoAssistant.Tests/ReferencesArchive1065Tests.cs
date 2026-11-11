// 0.1065 (audit A054, A075): the archive of the reference records names the goal sheets without dead links and says where they are;
// REFERENCES is in the list of ProjectLinks1063Tests.
internal static class ReferencesArchive1065Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("References archive: goal sheets named without links, place said",() =>
        {
            var text = File.ReadAllText(Path.GetFullPath("docs/project/REFERENCES.md")).Replace("\r\n","\n");
            Check(text.Contains("> Les fiches d'objectif nommées ici (MAP-001A, CRAFT-CONDITIONS-004…) sont dans les notes du mainteneur, hors du dépôt."),"the archive says where the goal sheets are");
            Check(!text.Contains("](../goals/"),"no link to a goal sheet left");
            var test = File.ReadAllText(Path.GetFullPath("tests/TenshoAssistant.Tests/ProjectLinks1063Tests.cs"));
            Check(test.Contains("\"docs/project/REFERENCES.md\""),"REFERENCES is checked for dead links");
        }),
    ];
}
