using TenshoAssistant.Help;

// 0.1323 (X-342, user's decision of 2026-11-14): players' issues apart from the project's plan (label plan, pinned issues 49 to 51).
internal static class PlayerReport1323Tests
{
    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Player reports: each form has the player-report label and its kind's title prefix", () =>
        {
            foreach (var (file, prefix, label) in new[] { ("bug_report.yml", "[Bug] ", "bug"), ("feature_request.yml", "[Idea] ", "enhancement"),
                ("tool_proposal.yml", "[Idea] ", "tool-proposal"), ("translation.yml", "[Translation] ", "translation") })
            {
                var form = Read(".github/ISSUE_TEMPLATE/" + file).Replace("\r\n", "\n");
                Check(form.Contains($"\ntitle: \"{prefix}\"\n", StringComparison.Ordinal), file + ": title prefix");
                Check(form.Contains($"labels: [\"{label}\", \"player-report\"]", StringComparison.Ordinal), file + ": labels");
            }
            Check(Read(".github/ISSUE_TEMPLATE/bug_report.yml").Contains("is%3Aopen+label%3Aplayer-report", StringComparison.Ordinal), "open problems: player reports only");
        }),
        ("Player reports: the in-game form sends the prefixed title once, and the preview is what leaves", () =>
        {
            var send = ProblemReport.Prepare(new(ProblemKind.Bug, "Map & flag", "Step", "", "", "v", "c"), []);
            Check(send.Url.Contains("template=bug_report.yml&title=%5BBug%5D%20Map%20%26%20flag", StringComparison.Ordinal), "bug prefix in the address");
            Check(send.Preview().StartsWith("[title]\n[Bug] Map & flag", StringComparison.Ordinal), "the preview shows the prefix");
            Check(ProblemReport.PrefixedTitle(ProblemKind.Bug, " [bug] typed ") == "[bug] typed", "no double prefix");
            Check(ProblemReport.PrefixedTitle(ProblemKind.Idea, "") == "[Idea]", "empty title");
            Check(ProblemReport.Prepare(new(ProblemKind.Idea, "More", "Want", "", "", "v", "c"), []).Url.Contains("&title=%5BIdea%5D%20More", StringComparison.Ordinal), "idea");
            Check(ProblemReport.Prepare(new(ProblemKind.Translation, "de", "Text", "", "", "v", "de"), []).Url.Contains("&title=%5BTranslation%5D%20de", StringComparison.Ordinal), "translation");
            var big = new string('x', 7000);
            var tooLong = ProblemReport.Prepare(new(ProblemKind.Bug, "T", big, big, "", "v", "c"), []);
            Check(tooLong.Url.Contains("&title=%5BBug%5D%20T", StringComparison.Ordinal) && tooLong.Url.Length < 6000, "kept when the text goes to the clipboard");
        }),
    ];

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
