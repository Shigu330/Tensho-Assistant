using System.Text.Json;
using System.Text.RegularExpressions;

// X-568 (audit of 17/11, G1 and B8): two generators of tools/docgen.py wrote wrong text. The index of docs/README.md said every working
// document was in French, though most have their two halves; the English « Where » column of SETTINGS.md wrote the internal keys of the
// Settings tabs (« Settings › Panels › ChatWindow »), a file name instead of a page, and the same place twice.
internal static class DocsWhere1877Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private const string Write=" (python -I -X utf8 tools/docgen.py write)";
    private static string Block(string path,string key)
    {
        var match=Regex.Match(Read(path),$@"<!-- docgen:{key} -->\n(.*?)\n<!-- /docgen:{key} -->",RegexOptions.Singleline);
        Check(match.Success,$"{path}: the block {key} is missing");
        return match.Groups[1].Value;
    }
    private static readonly string[] Working=["GLOBAL-PLAN","GLOBAL-PLAN-DONE","TESTING","TESTING-ROUTE","SETTINGS","DECISIONS","IDS","GAPS","REFERENCES"];
    private static bool Translated(string name)=>Read($"docs/project/{name}.md") is var text && text[..Math.Min(2000,text.Length)].Contains("Traduction française plus bas");

    private static Dictionary<string,string> Section(string name)
    {
        using var json=JsonDocument.Parse(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json")));
        return json.RootElement.GetProperty(name).EnumerateObject().Where(p=>p.Value.ValueKind==JsonValueKind.String).ToDictionary(p=>p.Name,p=>p.Value.GetString()!);
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("X-568 G1: the index of docs/README.md says « French only » of the working documents without a French half, and of them only, in both languages",() =>
        {
            var french=Working.Where(name=>!Translated(name)).ToArray();
            Check(french.Contains("TESTING") && !french.Contains("SETTINGS"),"the documents are read: TESTING is in French only, SETTINGS has both halves");
            foreach(var (key,words) in new[]{("index-en","Written in French only for now"),("index-fr","Écrits en français seulement pour l'instant")})
            {
                var block=Block("docs/README.md",key);
                Check(!block.Contains("written in French for now") && !block.Contains("écrits en français pour l'instant"),$"{key}: the old sentence about every working document is back"+Write);
                var sentence=Regex.Match(block,Regex.Escape(words)+@"[^:]*: ([^;.]*)");
                Check(sentence.Success,$"{key}: the sentence on the French-only documents is missing"+Write);
                var named=sentence.Groups[1].Value.Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
                Check(named.OrderBy(x=>x).SequenceEqual(french.OrderBy(x=>x)),$"{key}: French only says {string.Join(", ",named)}, the files say {string.Join(", ",french)}"+Write);
            }
        }),
        ("X-568 B8: the English « Where » of SETTINGS.md uses the labels of the Settings tabs and categories, names a page, never a place twice",() =>
        {
            var block=Block("docs/project/SETTINGS.md","settings");
            var sections=Section("SettingsSections");
            var categories=Section("SettingsCategories");
            Check(sections.ContainsKey("Panels") && sections["Panels"]!="Panels","the Settings tabs are read");
            var places=block.Split('\n').Where(line=>line.StartsWith("| `")).Select(line=>line.Split(" | ")).Where(cells=>cells.Length>=5).Select(cells=>cells[3]).ToArray();
            Check(places.Length>=200,"the rows of the settings inventory are read");
            foreach(var where in places)
            {
                var parts=where.Split(" · ");
                Check(parts.Distinct().Count()==parts.Length,$"SETTINGS.md: the same place twice in « {where} »"+Write);
                Check(!Regex.IsMatch(where,@"`[\w./]+\.cs`"),$"SETTINGS.md: a file name instead of a page in « {where} »"+Write);
                foreach(var part in parts.Where(p=>p.StartsWith("Settings › ")))
                {
                    var steps=part.Split(" › ");
                    Check(steps.Length==3,$"SETTINGS.md: « {part} » is not « Settings › tab › category »");
                    Check(!sections.Any(s=>s.Key==steps[1] && s.Value.Trim()!=s.Key),$"SETTINGS.md: the internal tab key {steps[1]} in « {part} »"+Write);
                    Check(!categories.Any(c=>c.Key==steps[2] && c.Value.Trim()!=c.Key),$"SETTINGS.md: the internal category key {steps[2]} in « {part} »"+Write);
                }
            }
        }),
    ];
}
