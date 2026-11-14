using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

// 0.1329 (X-335, review of 0.1318 I7, R22): lines of tracked files pointed to the agent's files (its scripts, notes, the master's queue,
// the relays' folder), and NoAgentFiles802 and AgentFilesDec1082 looked at two or three files only. Now every file git tracks, or would add
// (untracked, not ignored), is read: none may name a path of the agent's files, except .gitignore, which keeps them out. The paths are
// written here in pieces, so this file does not name them either.
internal static class AgentPaths1329Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    /// <summary>The entries .gitignore must hold (NoAgentFiles802).</summary>
    public static readonly string[] Ignored = ["CLAUDE" + ".md", "." + "claude/", "docs/" + "ai/", "scripts/" + "agent/"];
    private static readonly Regex Paths = new(string.Join("|",
        Regex.Escape("scripts/" + "agent"), Regex.Escape("docs/" + "ai/"), "(?<![A-Za-z])" + Regex.Escape("relais" + "/"), Regex.Escape("maitre" + "-inbox"),
        Regex.Escape("side-session" + "-notes"), Regex.Escape("." + "claude/"), Regex.Escape("CLAUDE" + ".md")), RegexOptions.CultureInvariant);

    /// <summary>Whether a text names a path of the agent's files.</summary>
    public static bool Cites(string text) => Paths.IsMatch(text);

    private static string[] Files()
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = Path.GetFullPath("."), RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in new[] { "-c", "core.quotepath=off", "ls-files", "-z", "--cached", "--others", "--exclude-standard" }) start.ArgumentList.Add(argument);
        using var git = Process.Start(start) ?? throw new Exception("git could not be started");
        var output = git.StandardOutput.ReadToEnd();
        git.WaitForExit();
        Check(git.ExitCode == 0, "git ls-files failed: " + git.StandardError.ReadToEnd());
        return output.Split('\0', StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.Ordinal).ToArray();
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Agent paths 1329: the pattern finds each path of the agent's files, and not ordinary words", () =>
        {
            foreach (var path in Ignored.Concat(["scripts/" + "agent/publish.sh", "docs/" + "ai/maitre" + "-inbox.jsonl", "(relais" + "/revue.md", " relais" + "/x.log"]))
                Check(Cites("see " + path + " here"), "found: " + path);
            foreach (var text in new[] { "the agent's notes", "docs/project/GLOBAL-PLAN.md", "relais 32 (a relay)", "scripts/Verify.ps1", "prerelais/x" })
                Check(!Cites(text), "not a path: " + text);
        }),
        ("Agent paths 1329: no tracked file names a path of the agent's files (except .gitignore)", () =>
        {
            var found = new List<string>();
            var files = Files();
            Check(files.Length > 500 && files.Contains(".gitignore"), "the repository's files are listed: " + files.Length);
            foreach (var file in files)
            {
                if (file == ".gitignore" || !File.Exists(file)) continue;
                var bytes = File.ReadAllBytes(file);
                if (Array.IndexOf(bytes, (byte)0, 0, Math.Min(bytes.Length, 8000)) >= 0) continue;
                var lines = Encoding.UTF8.GetString(bytes).Split('\n');
                for (var index = 0; index < lines.Length; index++)
                    if (Cites(lines[index])) found.Add($"{file}:{index + 1}");
            }
            Check(found.Count == 0, "paths of the agent's files in tracked files (R22): " + string.Join(", ", found.Take(20)));
            var ignore = File.ReadAllText(Path.GetFullPath(".gitignore"));
            Check(Ignored.All(ignore.Contains), "the agent's files stay ignored");
        }),
    ];
}
