using System.Diagnostics;
using System.Text.RegularExpressions;

// 0.1063 (audit A054, A075, A025): the project documents below cite only Markdown files of the repository; notes kept outside it are
// named as such. PROJECT and ROADMAP call the master prompt a historical source, the user's decisions and DECISIONS prevailing.
internal static class ProjectLinks1063Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static readonly string[] Documents = ["docs/project/PROJECT.md","docs/project/ROADMAP.md","docs/project/USER-REQUESTS.md",
        "docs/project/MAP-PROPOSALS.md","docs/project/CRAFT-GATHER-PROPOSALS.md","docs/project/DECISIONS.md"];

    private static HashSet<string> Tracked()
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = Path.GetFullPath("."), RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach(var argument in new[]{"-c","core.quotepath=off","ls-files","-z"}) start.ArgumentList.Add(argument);
        using var git = Process.Start(start) ?? throw new Exception("git could not be started");
        var output = git.StandardOutput.ReadToEnd();
        git.WaitForExit();
        Check(git.ExitCode == 0,"git ls-files failed: " + git.StandardError.ReadToEnd());
        return output.Split('\0',StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
    }

    private static string Normal(string path)
    {
        var parts = new List<string>();
        foreach(var part in path.Split('/'))
            if(part == "..") { if(parts.Count > 0) parts.RemoveAt(parts.Count - 1); }
            else if(part != "." && part.Length > 0) parts.Add(part);
        return string.Join("/",parts);
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Project documents: every Markdown file cited is in the repository",() =>
        {
            var tracked = Tracked();
            var dead = new List<string>();
            foreach(var document in Documents)
            {
                var text = File.ReadAllText(Path.GetFullPath(document));
                foreach(Match match in Regex.Matches(text,@"(?<![\w./-])((?:[\w.-]+/)*[\w-]+\.md)(?!\w)"))
                {
                    var path = match.Groups[1].Value;
                    if(!tracked.Contains(Normal(path)) && !tracked.Contains(Normal("docs/project/" + path))) dead.Add(document + ": " + path);
                }
            }
            Check(dead.Count == 0,"Markdown files cited that are not in the repository: " + string.Join(", ",dead.Distinct()));
        }),
        ("Project documents: the master prompt is a historical source in PROJECT and ROADMAP",() =>
        {
            foreach(var document in new[]{"docs/project/PROJECT.md","docs/project/ROADMAP.md"})
            {
                var text = File.ReadAllText(Path.GetFullPath(document)).Replace("\r\n","\n");
                Check(text.Contains("a historical source since") && text.Contains("source historique depuis le"),document + " calls the master prompt a historical source in both halves");
                Check(!text.Contains("Authoritative source: the master prompt") && !text.Contains("Source qui fait foi : le prompt maître"),document + " no longer calls it authoritative");
            }
        }),
    ];
}
