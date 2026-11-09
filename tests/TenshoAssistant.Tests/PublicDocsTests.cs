using System.Text.Json;
using System.Text.RegularExpressions;

// System plan 4.14 (documents, lot 1): parts of the public documents are written by tools/docgen.py from what the code and the files
// already know: the map of docs/README.md, the page table of README.md and the version marks. A document missing from the map, a page
// missing from the table or out of step with its help text, or a version number written by hand does not pass.
internal static class PublicDocsTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private const string Write=" (python -I -X utf8 tools/docgen.py write)";
    private static string Block(string text,string key)
    {
        var match=Regex.Match(text,$@"<!-- docgen:{key} -->\n(.*?)\n<!-- /docgen:{key} -->",RegexOptions.Singleline);
        Check(match.Success,$"the block {key} is missing");
        return match.Groups[1].Value;
    }
    private static Dictionary<string,string> Section(string language,string section)
    {
        using var document=JsonDocument.Parse(Read($"src/TenshoAssistant/Localization/Languages/{language}.json").TrimStart('﻿'));
        return document.RootElement.GetProperty(section).EnumerateObject().ToDictionary(property=>property.Name,property=>property.Value.GetString() ?? "");
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Public documents: docs/README.md lists every document of docs/project in both languages, and its links open",() =>
        {
            var map=Read("docs/README.md");
            foreach(var key in new[]{"index-en","index-fr"})
            {
                var block=Block(map,key);
                foreach(var file in Directory.GetFiles(Path.GetFullPath("docs/project"),"*.md"))
                    Check(block.Contains($"(project/{Path.GetFileName(file)})"),$"{key}: docs/project/{Path.GetFileName(file)} is not in the map (tools/docgen.py, PROJECT_WORKING)");
            }
            foreach(Match link in Regex.Matches(map,@"\]\(([^)\s#]+)\)"))
            {
                var target=link.Groups[1].Value;
                if (target.StartsWith("http",StringComparison.Ordinal)) continue;
                var path=Path.GetFullPath(Path.Combine("docs",target));
                Check(File.Exists(path) || Directory.Exists(path),$"docs/README.md: the link {target} does not open");
            }
        }),
        ("Public documents: the page table of README.md is every page of the navigation, in its order, with its help title and purpose",() =>
        {
            // The navigation's groups keep the order of their first page (PageGroups), so the table does too.
            var pages=Regex.Matches(Read("src/TenshoAssistant/MainWindow.Pages.cs"),@"^\s*new\(Page\.(\w+),(null|""\w+""),(?!null,)",RegexOptions.Multiline)
                .GroupBy(match=>match.Groups[2].Value).SelectMany(group=>group.Select(match=>match.Groups[1].Value)).ToArray();
            Check(pages.Length>=40 && pages.Contains("Home") && pages.Contains("Diagnostics") && !pages.Contains("CraftLive"),"the navigation pages are read (a tab page is not one)");
            var readme=Read("README.md");
            foreach(var language in new[]{"en","fr"})
            {
                var help=Section(language,"PageHelp");
                var rows=Block(readme,"pages-"+language).Split('\n').Skip(2).ToArray();
                var titles=rows.Select(row=>Regex.Match(row,@"\| \*\*(.+?)\*\*").Groups[1].Value).ToArray();
                Check(titles.SequenceEqual(pages.Select(page=>help[page+"Title"].Trim())),$"{language}: the pages of the table are not those of MainWindow.Pages.cs, in its order"+Write);
                for(var i=0;i<pages.Length;i++)
                    Check(rows[i].Contains(string.Join(' ',help[pages[i]+"Purpose"].Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries)).Replace("|","\\|")),$"{language}: the purpose of {pages[i]} is out of date"+Write);
            }
        }),
        ("Public documents: a version number is a version mark of tools/docgen.py, never written by hand, and the marks agree",() =>
        {
            var csproj=Regex.Match(Read("src/TenshoAssistant/TenshoAssistant.csproj"),@"<Version>(\d+)\.(\d+)\.(\d+)</Version>");
            var now=new Version(int.Parse(csproj.Groups[1].Value),int.Parse(csproj.Groups[2].Value),int.Parse(csproj.Groups[3].Value));
            var hand=new Regex(@"\bversion\s+0\.\d{3,4}\b|\b(?:checked|vérifié|relu)\b[^.\n]{0,60}\b0\.\d{3,4}\b",RegexOptions.IgnoreCase);
            var mark=new Regex(@"<!-- docgen:version -->([^<]*)<!-- /docgen:version -->");
            var marks=new HashSet<string>();
            foreach(var file in new[]{"README.md","CONTRIBUTING.md","SECURITY.md","Localization/README.md","docs/README.md","docs/FAQ.md","tools/README.md",
                "publish/d17/PULL_REQUEST.md","publish/github/README.md","community-help/README.md","community-templates/README.md","community-discoveries/README.md"})
            {
                var text=Read(file);
                foreach(Match found in mark.Matches(text))marks.Add(found.Groups[1].Value);
                var hit=hand.Match(mark.Replace(text,""));
                Check(!hit.Success,$"{file}: « {hit.Value} » is a version written by hand (a version mark of tools/docgen.py, or no version)");
            }
            Check(marks.Count==1,"the version marks agree, in README.md and Localization/README.md"+Write);
            var parts=marks.Single().Split('.').Select(int.Parse).ToArray();
            Check(new Version(parts[0],parts[1],parts.Length>2 ? parts[2] : 0)<=now,"a version mark is newer than the csproj");
            var languages=Read("Localization/README.md");
            Check(Block(languages,"languages-en").Contains("English and French are complete") && Block(languages,"languages-fr").Contains("l'anglais et le français sont complets"),
                "the state of the translations is counted by tools/docgen.py");
        }),
    ];
}
