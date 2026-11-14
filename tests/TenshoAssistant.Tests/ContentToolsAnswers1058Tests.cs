// 0.1058 (audit A062): the questions of CONTENT-TOOLS carried the answers of DEC-045 in both halves.
// 0.1348 (X-357): CONTENT-TOOLS is removed (git history keeps it); DEC-045 keeps the answers and the study's last version is linked.
internal static class ContentToolsAnswers1058Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Content tools: the study removed, DEC-045 keeps the answers in both halves",() =>
        {
            Check(!File.Exists(Path.GetFullPath("docs/project/CONTENT-TOOLS.md")),"CONTENT-TOOLS removed");
            var text = File.ReadAllText(Path.GetFullPath("docs/project/DECISIONS.md")).Replace("\r\n","\n");
            var middle = text.IndexOf("\n---\n",StringComparison.Ordinal);
            Check(text.IndexOf("DEC-045",StringComparison.Ordinal) is > 0 and var first && first < middle,"DEC-045 in the English half");
            Check(text.LastIndexOf("DEC-045",StringComparison.Ordinal) > middle,"DEC-045 in the French half");
        }),
    ];
}
