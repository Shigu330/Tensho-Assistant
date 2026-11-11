using System.Text.RegularExpressions;

// 0.1054 (audit A038): the decisions written for a submission to the official repository say, in both halves of DECISIONS.md, that
// DEC-039 replaced that part (custom repository only).
internal static class DecisionsSuperseded1054Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static string Block(string text,string marker,string id)
    {
        var match = Regex.Match(text,"^" + marker + ": " + id + "$",RegexOptions.Multiline);
        Check(match.Success,marker + ": " + id + " found");
        var end = text.IndexOf("\n\n",match.Index,StringComparison.Ordinal);
        return end < 0 ? text[match.Index..] : text[match.Index..end];
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Decisions: the submission decisions point to DEC-039 in both halves",() =>
        {
            var text = File.ReadAllText(Path.GetFullPath("docs/project/DECISIONS.md")).Replace("\r\n","\n");
            foreach(var id in new[]{"DEC-012","DEC-023","DEC-024","DEC-037"})
                foreach(var marker in new[]{"ID","DEC-ID"})
                {
                    var references = Block(text,marker,id).Split('\n').LastOrDefault(line => line.StartsWith("References: ",StringComparison.Ordinal)) ?? "";
                    Check(references.Contains("DEC-039"),id + " (" + (marker == "ID" ? "English" : "French") + " half) says DEC-039 replaced its submission part");
                }
        }),
    ];
}
