// 0.1061 (audit A100): the English part of the CHANGELOG becomes the commit bodies and the public release notes; it speaks of "the
// user" in the neutral voice (R19), never "your request" or "your choice" addressed to the reader.
internal static class ChangelogVoice1061Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static readonly string[] Phrases = ["at your request","your choice","your decision","your request","your agreement","your answer","you asked"];

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Changelog: the English part never addresses the user as « you » about requests and choices",() =>
        {
            var english = false;
            var wrong = new List<string>();
            var number = 0;
            foreach(var line in File.ReadAllText(Path.GetFullPath("CHANGELOG.md")).Replace("\r\n","\n").Split('\n'))
            {
                number++;
                if(line.StartsWith("## ",StringComparison.Ordinal)) english = false;
                else if(line.StartsWith("### English",StringComparison.Ordinal)) english = true;
                if(english && Phrases.Any(phrase => line.Contains(phrase,StringComparison.OrdinalIgnoreCase))) wrong.Add("line " + number);
            }
            Check(wrong.Count == 0,"English CHANGELOG lines that speak to the user: " + string.Join(", ",wrong.Take(10)));
        }),
    ];
}
