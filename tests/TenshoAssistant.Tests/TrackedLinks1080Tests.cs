using System.Diagnostics;
using System.Text.RegularExpressions;

// 0.1080 (audits A075, A025, A100): a Markdown link of a tracked file leads to a tracked file or folder. A link to a local note, a
// build output or a moved file fails here, on the PC and in the CI alike (the list comes from git, so local notes never count).
internal static class TrackedLinks1080Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static readonly Regex Link = new(@"\]\(([^)#\s]+)(#[^)\s]*)?\)",RegexOptions.Compiled);
    private static readonly Regex Scheme = new(@"^[a-zA-Z][a-zA-Z0-9+.-]*:",RegexOptions.Compiled);

    private static List<string> Tracked()
    {
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = Path.GetFullPath("."),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach(var argument in new[]{"-c","core.quotepath=off","ls-files","-z"}) start.ArgumentList.Add(argument);
        using var git = Process.Start(start) ?? throw new Exception("git could not be started");
        var output = git.StandardOutput.ReadToEnd();
        git.WaitForExit();
        Check(git.ExitCode == 0,"git ls-files failed: " + git.StandardError.ReadToEnd());
        return output.Split('\0',StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    internal static IEnumerable<string> BrokenLinks(string file,string text,ISet<string> known)
    {
        var folder = Path.GetDirectoryName(file)?.Replace('\\','/') ?? "";
        foreach(Match match in Link.Matches(text))
        {
            var target = Uri.UnescapeDataString(match.Groups[1].Value);
            if(Scheme.IsMatch(target)) continue;
            var parts = new List<string>();
            foreach(var part in (folder.Length == 0 ? target : folder + "/" + target).Split('/'))
            {
                if(part is "" or ".") continue;
                if(part == ".."){ if(parts.Count > 0) parts.RemoveAt(parts.Count - 1); continue; }
                parts.Add(part);
            }
            var path = string.Join("/",parts);
            if(path.Length > 0 && !known.Contains(path)) yield return target;
        }
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Links: the checker finds a link to an untracked path and accepts a tracked one",() =>
        {
            var known = new HashSet<string>{"docs","docs/project","docs/project/TESTING.md","README.md"};
            var found = BrokenLinks("docs/project/A.md","[a](TESTING.md) [b](../../README.md#x) [c](https://example.org) [d](../notes/N.md)",known).ToList();
            Check(found.Count == 1 && found[0] == "../notes/N.md","only the untracked target is reported: " + string.Join(", ",found));
        }),
        ("Links: every Markdown link of a tracked file leads to a tracked path",() =>
        {
            var tracked = Tracked();
            var known = new HashSet<string>(tracked);
            foreach(var file in tracked)
            {
                var parts = file.Split('/');
                for(var i = 1; i < parts.Length; i++) known.Add(string.Join("/",parts.Take(i)));
            }
            var broken = new List<string>();
            foreach(var file in tracked.Where(file => file.EndsWith(".md",StringComparison.OrdinalIgnoreCase) && File.Exists(Path.GetFullPath(file))))
                broken.AddRange(BrokenLinks(file,File.ReadAllText(Path.GetFullPath(file)),known).Select(target => file + " → " + target));
            Check(broken.Count == 0,"links to untracked paths: " + string.Join("; ",broken.Take(10)));
        }),
    ];
}
