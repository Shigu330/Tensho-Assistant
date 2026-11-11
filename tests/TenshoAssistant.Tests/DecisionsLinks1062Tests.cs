using System.Diagnostics;
using System.Text.RegularExpressions;

// 0.1062 (audit A075, A025): every Markdown file that DECISIONS.md cites is a file of the repository (tracked by git), as a path from the
// root or from docs/project; notes kept outside the repository are named as such, never by a path that is dead on GitHub.
internal static class DecisionsLinks1062Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

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
        ("Decisions: every Markdown file cited is in the repository",() =>
        {
            var tracked = Tracked();
            Check(tracked.Contains("docs/project/DECISIONS.md"),"the tracked files are listed");
            var text = File.ReadAllText(Path.GetFullPath("docs/project/DECISIONS.md"));
            var dead = new SortedSet<string>(StringComparer.Ordinal);
            foreach(Match match in Regex.Matches(text,@"(?<![\w./-])((?:[\w.-]+/)*[\w-]+\.md)(?!\w)"))
            {
                var path = match.Groups[1].Value;
                if(!tracked.Contains(Normal(path)) && !tracked.Contains(Normal("docs/project/" + path))) dead.Add(path);
            }
            Check(dead.Count == 0,"Markdown files cited by DECISIONS.md that are not in the repository: " + string.Join(", ",dead));
        }),
    ];
}
