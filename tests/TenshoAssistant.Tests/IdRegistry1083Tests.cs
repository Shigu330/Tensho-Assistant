// 0.1083 (audit A046, D-138): one registry of the identifier families (docs/project/IDS.md, both languages); only X- (GLOBAL-PLAN)
// and DEC- (DECISIONS) take new numbers, and each number is defined once in each half of its document.
internal static class IdRegistry1083Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static (string En,string Fr) Halves(string path)
    {
        var text = File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
        var half = text.IndexOf("\n---\n",StringComparison.Ordinal);
        Check(half > 0,path + " has two halves");
        return (text[..half],text[half..]);
    }

    private static void Unique(string text,string pattern,string what)
    {
        var ids = System.Text.RegularExpressions.Regex.Matches(text,pattern,System.Text.RegularExpressions.RegexOptions.Multiline)
            .Select(m => m.Groups[1].Value).ToList();
        Check(ids.Count > 0,what + ": some identifiers");
        var twice = ids.GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Check(twice.Count == 0,what + " defined twice: " + string.Join(", ",twice));
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("IDS: registry in both halves, same tables",() =>
        {
            var (en,fr) = Halves("docs/project/IDS.md");
            Check(en.Contains("| `X-NN` | [GLOBAL-PLAN](GLOBAL-PLAN.md)") && en.Contains("| `DEC-NNN` | [DECISIONS](DECISIONS.md)"),"open families (English)");
            Check(fr.Contains("| `X-NN` | [GLOBAL-PLAN](GLOBAL-PLAN.md)") && fr.Contains("| `DEC-NNN` | [DECISIONS](DECISIONS.md)"),"open families (French)");
            int Rows(string t) => t.Split('\n').Count(l => l.StartsWith("| "));
            Check(Rows(en) == Rows(fr),"same table rows in both halves");
            var map = File.ReadAllText(Path.GetFullPath("docs/README.md"));
            Check(map.Contains("[IDS](project/IDS.md)"),"the registry is in the documents' map");
        }),
        ("IDS: X- and DEC- numbers defined once per half",() =>
        {
            foreach (var half in new[] { Halves("docs/project/GLOBAL-PLAN.md").En,Halves("docs/project/GLOBAL-PLAN.md").Fr })
                Unique(half,@"^\| *(X-\d+) \|","GLOBAL-PLAN X-");
            var (en,fr) = Halves("docs/project/DECISIONS.md");
            Unique(en,@"^ID: (DEC-\d+)$","DECISIONS (English)");
            Unique(fr,@"^DEC-ID: (DEC-\d+)$","DECISIONS (French)");
        }),
    ];
}
