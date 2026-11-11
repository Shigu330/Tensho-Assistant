// 0.1064 (audit A075, A025): BACKLOG and GAPS are in the list of ProjectLinks1063Tests (only Markdown files of the repository cited),
// and the old pointers to the goal sheet of the time now name the maintainer's notes in both halves of BACKLOG.
internal static class BacklogLinks1064Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Backlog: covered by the link test, current step in the maintainer's notes in both halves",() =>
        {
            var test = File.ReadAllText(Path.GetFullPath("tests/TenshoAssistant.Tests/ProjectLinks1063Tests.cs"));
            Check(test.Contains("\"docs/project/BACKLOG.md\",\"docs/project/GAPS.md\""),"BACKLOG and GAPS are checked for dead links");
            var backlog = File.ReadAllText(Path.GetFullPath("docs/project/BACKLOG.md")).Replace("\r\n","\n");
            Check(backlog.Contains("the maintainer's notes determine the current next") && backlog.Contains("les notes du mainteneur déterminent la suite"),"BACKLOG names the maintainer's notes in both halves");
        }),
    ];
}
