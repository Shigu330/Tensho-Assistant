using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

// 0.1053 (audit A041): a date written by hand in a tracked Markdown document is never later than tomorrow (UTC, one day of slack for
// time zones). The list of documents comes from git (tracked files only), so local notes and build outputs are never read.
internal static class FutureDates1053Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static readonly Regex DatePattern = new(@"(?<![0-9])(20[0-9]{2})-([01][0-9])-([0-3][0-9])(?![0-9])",RegexOptions.Compiled);

    internal static IEnumerable<string> FutureDates(string text,DateOnly limit)
    {
        foreach(Match match in DatePattern.Matches(text))
            if(DateOnly.TryParseExact(match.Value,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date) && date > limit)
                yield return match.Value;
    }

    private static List<string> TrackedMarkdown()
    {
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = Path.GetFullPath("."),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach(var argument in new[]{"-c","core.quotepath=off","ls-files","-z","--","*.md"}) start.ArgumentList.Add(argument);
        using var git = Process.Start(start) ?? throw new Exception("git could not be started");
        var output = git.StandardOutput.ReadToEnd();
        git.WaitForExit();
        Check(git.ExitCode == 0,"git ls-files failed: " + git.StandardError.ReadToEnd());
        return output.Split('\0',StringSplitOptions.RemoveEmptyEntries).Where(file => File.Exists(Path.GetFullPath(file))).ToList();
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Dates: the scanner finds a date after the limit and nothing else",() =>
        {
            var limit = new DateOnly(2026,11,11);
            var found = FutureDates("Done 2026-11-10, today 2026-11-11, written ahead 2026-11-12, id 12026-11-20, number 2026-11-305.",limit).ToList();
            Check(found.SequenceEqual(new[]{"2026-11-12"}),"only the date after the limit is reported: " + string.Join(", ",found));
            Check(!FutureDates("2026-13-40 is not a date",limit).Any(),"an impossible date is ignored");
        }),
        ("Dates: no tracked Markdown document is dated after tomorrow (UTC)",() =>
        {
            var limit = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            var files = TrackedMarkdown();
            Check(files.Contains("README.md") && files.Contains("CHANGELOG.md") && files.Any(file => file.StartsWith("docs/project/")),"the tracked documents are scanned");
            var wrong = files.SelectMany(file => FutureDates(File.ReadAllText(Path.GetFullPath(file)),limit).Select(date => file + ": " + date)).ToList();
            Check(wrong.Count == 0,"dates later than " + limit.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture) + ": " + string.Join("; ",wrong.Take(10)));
        }),
    ];
}
