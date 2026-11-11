using System.Text.RegularExpressions;

// 0.1051 (audit A076, C033, A067): the plugin's code does not point to the agent's files, which are not in the repository, and
// scripts/New-Tool.ps1 and CONTRIBUTING.md name every navigation group the code uses.
internal static class AgentPointers1051Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static IEnumerable<string> SourceFiles() =>
        Directory.EnumerateFiles(Path.GetFullPath("src"),"*.cs",SearchOption.AllDirectories)
            .Where(path=>!path.Replace('\\','/').Contains("/bin/") && !path.Replace('\\','/').Contains("/obj/"));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Agent files: no source file of the plugin points to them",() =>
        {
            foreach(var path in SourceFiles())
            {
                var text=File.ReadAllText(path);
                foreach(var agent in new[]{"docs/"+"goals/","docs/"+"ai/","\"CLAUDE"+".md\"","PROMPT-"+"MAITRE"})
                    Check(!text.Contains(agent),Path.GetFileName(path)+" points to an agent file: "+agent);
            }
            var en=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json"));
            Check(!en.Contains("\"DocAgent\""),"the contributor panel's agent document text is gone");
        }),
        ("New-Tool.ps1 and CONTRIBUTING.md: every navigation group of MainWindow.Pages.cs",() =>
        {
            var pages=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Pages.cs"));
            var groups=Regex.Matches(pages,@"new\(Page\.\w+,""(\w+)""").Select(match=>match.Groups[1].Value).Distinct().ToList();
            Check(groups.Count>=9,"groups read from MainWindow.Pages.cs: "+groups.Count);
            var script=File.ReadAllText(Path.GetFullPath("scripts/New-Tool.ps1"));
            var set=Regex.Match(script,@"\[ValidateSet\(([^)]*)\)\]\[string\]\$Category");
            Check(set.Success,"ValidateSet of -Category found");
            var accepted=Regex.Matches(set.Groups[1].Value,"'(\\w+)'").Select(match=>match.Groups[1].Value).ToHashSet();
            Check(accepted.SetEquals(groups),"New-Tool.ps1 -Category: "+string.Join(",",accepted)+" vs code: "+string.Join(",",groups));
            var contributing=File.ReadAllText(Path.GetFullPath("CONTRIBUTING.md"));
            foreach(var line in new[]{"`-Category` is one of the navigation groups:","`-Category` est l'un des groupes de la navigation :"})
            {
                var at=contributing.IndexOf(line,StringComparison.Ordinal);
                Check(at>=0,"CONTRIBUTING.md line: "+line);
                var end=contributing.IndexOf('\n',at);
                var text=contributing[at..(end<0?contributing.Length:end)];
                foreach(var group in groups)Check(text.Contains("`"+group+"`"),"CONTRIBUTING.md misses "+group+" in: "+line);
            }
        }),
    ];
}
