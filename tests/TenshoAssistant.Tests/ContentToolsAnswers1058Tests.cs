// 0.1058 (audit A062): the questions of CONTENT-TOOLS carry the answers of DEC-045 in both halves, and the one it did not answer is
// said to be open.
internal static class ContentToolsAnswers1058Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Content tools: the answers of DEC-045 follow the questions in both halves",() =>
        {
            var text = File.ReadAllText(Path.GetFullPath("docs/project/CONTENT-TOOLS.md")).Replace("\r\n","\n");
            var english = text.IndexOf("Answers ([DEC-045](DECISIONS.md), 2026-11-09, point 8)",StringComparison.Ordinal);
            var french = text.IndexOf("Réponses ([DEC-045](DECISIONS.md), 2026-11-09, point 8)",StringComparison.Ordinal);
            var middle = text.IndexOf("\n---\n",StringComparison.Ordinal);
            Check(english > 0 && english < middle,"English answers before the French half");
            Check(french > middle,"French answers in the French half");
            Check(text.Contains("Question 3 was not in that\nbatch and stays open") && text.Contains("question 3 n'était pas dans ce lot et reste ouverte"),"question 3 said to be open in both halves");
        }),
    ];
}
