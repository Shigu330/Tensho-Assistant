using System.Text.RegularExpressions;
using TenshoAssistant;

// System plan 4.16 (documents, lots 2 and 3): tools/docgen.py also writes the settings inventory (docs/project/SETTINGS.md), the table of
// the network sources (SECURITY.md, English and French), the index of the DECs (docs/project/DECISIONS.md) and the index of the evidence
// (docs/evidence/README.md). A setting, a file that makes requests, a DEC or an evidence file missing from its table does not pass.
internal static class DocsGen1018Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private const string Write=" (python -I -X utf8 tools/docgen.py write --blocks)";
    private static string Block(string path,string key)
    {
        var match=Regex.Match(Read(path),$@"<!-- docgen:{key} -->\n(.*?)\n<!-- /docgen:{key} -->",RegexOptions.Singleline);
        Check(match.Success,$"{path}: the block {key} is missing");
        return match.Groups[1].Value;
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Documents 4.16: SETTINGS.md lists every setting written by a settings control and every setting of « restore every setting »",() =>
        {
            var block=Block("docs/project/SETTINGS.md","settings");
            var written=Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"MainWindow*.cs")
                .SelectMany(file=>Regex.Matches(File.ReadAllText(file),@"\b(?:BoolSetting|IntSetting|SliderSetting)\(""[\w.]+"",""[\w.]+"",[^;]*?\bvalue\s*=>\s*configuration\.([\w.]+)\s*=\s*value\b").Select(match=>match.Groups[1].Value))
                .Distinct().ToArray();
            Check(written.Length>=120 && written.Contains("ShowHeaderClock"),"the settings controls are read");
            foreach(var path in written)Check(block.Contains($"| `{path}` |"),$"SETTINGS.md: the setting {path} is not in the inventory"+Write);
            foreach(var name in ConfigurationReset.Reset)
                Check(Regex.IsMatch(block,$@"\| `{Regex.Escape(name)}[`.]"),$"SETTINGS.md: {name} (ConfigurationReset.Reset) is not in the inventory"+Write);
        }),
        ("Documents 4.16: every file of the plugin that makes a web request is in the network table of SECURITY.md, in both languages",() =>
        {
            var root=Path.GetFullPath("src/TenshoAssistant");
            var leftOut=Regex.Match(Read("tools/docgen.py"),@"NETWORK_LEFT_OUT = \{(.*?)\n\}",RegexOptions.Singleline).Groups[1].Value;
            var requests=Directory.GetFiles(root,"*.cs",SearchOption.AllDirectories)
                .Where(file=>!file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                .Where(file=>Regex.IsMatch(File.ReadAllText(file),@"\bHttpClient\b|\bBoundedHttp\b|\bHttpRequestMessage\b"))
                .Select(file=>Path.GetRelativePath(root,file).Replace('\\','/')).ToArray();
            Check(requests.Contains("Market/MarketService.cs") && requests.Contains("Alarms/DiscordNotifier.cs"),"the request files are read");
            foreach(var key in new[]{"network-en","network-fr"})
            {
                var block=Block("SECURITY.md",key);
                foreach(var file in requests.Where(file=>!leftOut.Contains($"\"{file}\"")))
                    Check(block.Contains($"`{file}`"),$"SECURITY.md {key}: {file} makes requests but is not in the table (tools/docgen.py, NETWORK)"+Write);
            }
        }),
        ("Documents 4.16: the index of DECISIONS.md has one row per DEC, in the order of the file",() =>
        {
            var decisions=Regex.Matches(Read("docs/project/DECISIONS.md"),@"^DEC-ID: (DEC-\d+)$",RegexOptions.Multiline).Select(match=>match.Groups[1].Value).ToArray();
            var rows=Regex.Matches(Block("docs/project/DECISIONS.md","decisions"),@"^\| (DEC-\d+) \|",RegexOptions.Multiline).Select(match=>match.Groups[1].Value).ToArray();
            Check(decisions.Length>=49,"the DECs are read");
            Check(rows.SequenceEqual(decisions),"DECISIONS.md: the index is not one row per DEC in the order of the file"+Write);
        }),
        ("Documents 4.16: docs/evidence/README.md has one row per evidence file",() =>
        {
            var block=Block("docs/evidence/README.md","evidence");
            var files=Directory.GetFiles(Path.GetFullPath("docs/evidence")).Select(Path.GetFileName).Where(name=>name!="README.md").ToArray();
            Check(files.Length>=200,"the evidence files are read");
            foreach(var name in files)Check(block.Contains($"| [{name}]({name}) |"),$"docs/evidence/README.md: {name} is not in the index"+Write);
        }),
    ];
}
