// 0.1079 (audit A043, end): the checks from 0.538 on have their own heading in TESTING.md instead of sitting under 0.537's.
internal static class TestingHeading1079Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("TESTING: the checks from 0.538 on have their own heading",() =>
        {
            var testing = File.ReadAllText(Path.GetFullPath("docs/project/TESTING.md")).Replace("\r\n","\n");
            var heading = testing.IndexOf("**0.538 et suivantes : un essai par ligne, marqué de sa version**");
            var first = testing.IndexOf("- [ ] (0.538) Une infobulle ou un menu du jeu");
            var previous = testing.IndexOf("**0.537 : s'effacer devant les fenêtres du jeu**");
            Check(previous >= 0 && heading > previous && first > heading,"0.537's heading, then the new heading, then the 0.538 check");
        }),
    ];
}
