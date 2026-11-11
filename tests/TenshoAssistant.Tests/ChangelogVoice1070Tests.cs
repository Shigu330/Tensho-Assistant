// 0.1070 (audit A071): the phrasings that still addressed the maintainer in the English CHANGELOG ("your report", "tonight") are in the
// list of ChangelogVoice1061Tests, which reads every English part; player-facing labels (« Your notes and arena drawing ») stay allowed.
internal static class ChangelogVoice1070Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Changelog voice: report and tonight phrasings are refused, player labels kept",() =>
        {
            var test = File.ReadAllText(Path.GetFullPath("tests/TenshoAssistant.Tests/ChangelogVoice1061Tests.cs"));
            Check(test.Contains("\"your report\",\"tonight\",\"your go-ahead\""),"the 0.1061 test refuses the report and tonight phrasings");
            var text = ChangelogSplit1093Tests.All();
            Check(text.Contains("« Your notes and arena drawing »"),"the player-facing label is kept");
        }),
    ];
}
