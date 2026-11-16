using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

// 0.1473 (X-491, review of 0.1472 I1; R04: other players' data, R25): a real player's name and world, and the start of their message, were
// written in a tracked file (a comment of the chat's glyphs, 0.1471, and the plan's text), and the user's own character name in an old
// evidence log (0.160). They now read « Name [world icon] World », « THE EXAMPLE » and « Joueur » (the git history is not rewritten: the
// user's decision). This guard reads every file git tracks, or would add, and refuses any name of a list kept out of the repository: its
// path comes from the TENSHO_PRIVATE_NAMES variable, set by the maintainer's scripts. Without the variable (the CI) the list is not read;
// with it, a missing list is a failure. A failure names the file, the line and the entry's number, never the name.
internal static class PrivateNames1473Tests
{
    public const string Variable = "TENSHO_PRIVATE_NAMES";
    private const string Phrase = "texte:";
    /// <summary>The line written when the list is not given (the CI, or Verify.ps1 run without the maintainer's scripts).</summary>
    public const string NotDone = "NOTE private names: list absent (" + Variable + " unset): check not done";
    private const string NotLetter = @"(?<![^\W\d_])";
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    /// <summary>An entry of the list: a character name (« First Last »: found whole, its parts joined by spaces, « @ » or nothing, and each
    /// part of four letters or more alone, after a non-letter), or « texte: SOME WORDS », a phrase (the start of a player's message) found
    /// whole. Any case; any run of spaces.</summary>
    public static Regex Pattern(string entry)
    {
        string text;
        if (entry.StartsWith(Phrase, StringComparison.OrdinalIgnoreCase))
            text = string.Join(@"\s+", entry[Phrase.Length..].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(Regex.Escape));
        else
        {
            var parts = entry.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            var alone = parts.Where(part => part.Length >= 4).Select(part => NotLetter + Regex.Escape(part));
            text = string.Join("|", new[] { NotLetter + string.Join(@"[\s@]*", parts.Select(Regex.Escape)) }.Concat(alone));
        }
        return new Regex(text, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    /// <summary>The list's entries: one per line, trimmed; blank lines and « # » comments left out.</summary>
    public static string[] Entries(string text) =>
        text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith('#')).ToArray();

    /// <summary>The (file, line, entry number from 1) where an entry is found.</summary>
    public static List<(string File, int Line, int Entry)> Find(IReadOnlyList<string> entries, IEnumerable<(string File, string Text)> files)
    {
        var patterns = entries.Select(Pattern).ToArray();
        var found = new List<(string, int, int)>();
        foreach (var (file, text) in files)
        {
            if (!patterns.Any(pattern => pattern.IsMatch(text))) continue;
            var lines = text.Split('\n');
            for (var index = 0; index < lines.Length; index++)
                for (var entry = 0; entry < patterns.Length; entry++)
                    if (patterns[entry].IsMatch(lines[index])) found.Add((file, index + 1, entry + 1));
        }
        return found;
    }

    /// <summary>Samples, read by the maintainer's tool too (the same answers there): entry, text, found.</summary>
    public static readonly (string Entry, string Text, bool Found)[] Samples =
    [
        ("Ann Example", "Ann Example@Alpha: hello", true),
        ("Ann Example", "« Ann ExampleAlpha: »", true),
        ("Ann Example", "ANN  EXAMPLE", true),
        ("Ann Example", "the « Example » family", true),
        ("Ann Example", "AnnExample", true),
        ("Ann Example", "Annex and Anna", false),
        ("Ann Example", "a counterexample", false),
        ("Ann Example", "Name [world icon] World", false),
        ("Bo Li", "Bo Li@Beta", true),
        ("Bo Li", "Bob and Lily", false),
        ("texte: THE SAMPLE CLUB", "the  sample club // November", true),
        ("texte: THE SAMPLE CLUB", "THE SAMPLE", false),
        ("texte: THE SAMPLE CLUB", "THE EXAMPLE // November", false),
    ];

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

    private static IEnumerable<(string File, string Text)> Texts(string root, IEnumerable<string> files)
    {
        foreach (var file in files)
        {
            var path = Path.Combine(root, file);
            if (!File.Exists(path)) continue;
            var bytes = File.ReadAllBytes(path);
            if (Array.IndexOf(bytes, (byte)0, 0, Math.Min(bytes.Length, 8000)) >= 0) continue;
            yield return (file, Encoding.UTF8.GetString(bytes));
        }
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Private names 1473: a name is found in the chat's shapes, a phrase whole, and ordinary words stay free", () =>
        {
            foreach (var (entry, text, expected) in Samples)
                Check(Pattern(entry).IsMatch(text) == expected, $"{entry} in « {text} »: {expected}");
            Check(Entries("# comment\r\n  Ann Example  \r\n\r\ntexte: THE SAMPLE CLUB\n").SequenceEqual(["Ann Example", "texte: THE SAMPLE CLUB"]), "the list's lines");
        }),
        ("Private names 1473: the files that name an entry are found with their line; binaries are left out", () =>
        {
            var root = Path.Combine(Path.GetTempPath(), "tensho-private-names-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                File.WriteAllText(Path.Combine(root, "a.md"), "ok\nsee Ann Example@Alpha\n");
                File.WriteAllBytes(Path.Combine(root, "b.png"), [0x89, 0x50, 0, 0, .. Encoding.UTF8.GetBytes("Ann Example")]);
                File.WriteAllText(Path.Combine(root, "c.cs"), "// the sample club\r\nclean\r\n");
                File.WriteAllText(Path.Combine(root, "d.md"), "« Name [world icon] World : THE WORDS »\n");
                var found = Find(["Ann Example", "texte: THE SAMPLE CLUB"], Texts(root, ["a.md", "b.png", "c.cs", "d.md", "missing.txt"]));
                Check(found.SequenceEqual([("a.md", 2, 1), ("c.cs", 1, 2)]), "found: " + string.Join(", ", found));
            }
            finally { Directory.Delete(root, true); }
        }),
        ("Private names 1473: no tracked file names a private name (the list given by TENSHO_PRIVATE_NAMES)", () =>
        {
            var list = Environment.GetEnvironmentVariable(Variable);
            // X-494 M3 (review of 0.1474): without the list the check is not done; the run says so in its output instead of passing in silence.
            if (string.IsNullOrWhiteSpace(list)) { Console.WriteLine(NotDone); return; }
            Check(File.Exists(list), "the private names' list given by " + Variable + " exists");
            var entries = Entries(File.ReadAllText(list));
            Check(entries.Length > 0, "the private names' list holds names");
            var files = Files();
            Check(files.Length > 500, "the repository's files are listed: " + files.Length);
            var found = Find(entries, Texts(Path.GetFullPath("."), files));
            Check(found.Count == 0, "private names in tracked files (R04, R25): " + string.Join(", ", found.Take(20).Select(f => $"{f.File}:{f.Line} (entry {f.Entry})")));
        }),
    ];
}
