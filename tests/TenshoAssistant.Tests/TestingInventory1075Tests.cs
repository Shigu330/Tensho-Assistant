// 0.1075 (audit A043, second part): TESTING.md no longer carries the test inventory frozen at 0.315.0 and no longer links to the
// maintainer's notes; the current count is in the CHANGELOG.
internal static class TestingInventory1075Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("TESTING: frozen inventory removed, current count in the CHANGELOG",() =>
        {
            var testing = File.ReadAllText(Path.GetFullPath("docs/project/TESTING.md")).Replace("\r\n","\n");
            Check(!testing.Contains("569 au 0.315.0 ; liste non reprise") && !testing.Contains("**0.232 → 0.315 : 12 tests"),"the frozen inventory is gone");
            Check(testing.Contains("### Inventaire des tests\n") && testing.Contains("git show 8bda2ca:docs/project/TESTING.md"),"one paragraph says where the count and the old list are");
            Check(!testing.Contains("](../" + "ai/"),"no link to the maintainer's notes");
            Check(testing.Contains("\n### Essai en jeu\n"),"the next section is kept");
        }),
    ];
}
