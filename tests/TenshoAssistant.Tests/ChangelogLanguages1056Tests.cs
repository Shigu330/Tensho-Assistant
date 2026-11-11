using System.Text.RegularExpressions;

// 0.1056 (audit A069): the map of the documents says where the CHANGELOG has an English part, and that rule matches the file: every
// entry from 0.219.0 on has a "### English" section, the older ones none.
internal static class ChangelogLanguages1056Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Changelog: English part from 0.219.0 on, as the map of the documents says",() =>
        {
            var text = ChangelogSplit1093Tests.All();
            var entries = Regex.Split(text,"^(?=## )",RegexOptions.Multiline).Where(part => part.StartsWith("## ",StringComparison.Ordinal)).ToList();
            Check(entries.Count > 900,"CHANGELOG entries read: " + entries.Count);
            var missing = new List<string>();
            foreach(var entry in entries)
            {
                var head = entry[3..entry.IndexOf('\n')].Trim();
                var match = Regex.Match(head,@"^0\.(\d+)\.");
                if(!match.Success) continue;
                var minor = int.Parse(match.Groups[1].Value);
                var english = entry.Contains("\n### English");
                if(minor >= 219 && !english) missing.Add(head);
                if(minor < 219 && english) missing.Add(head + " (English before 0.219)");
            }
            Check(missing.Count == 0,"entries out of the rule: " + string.Join(", ",missing.Take(10)));
            var map = File.ReadAllText(Path.GetFullPath("docs/README.md"));
            Check(map.Contains("`### English` from 0.219.0 on") && map.Contains("`### English` depuis 0.219.0"),"docs/README.md states the rule in both halves");
        }),
    ];
}
