// 0.1078 (audit A070, part): each version has one heading in the CHANGELOG (0.59.0, 0.50.0 and 0.49.0 had two).
internal static class ChangelogHeadings1078Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("CHANGELOG: one heading per version",() =>
        {
            var lines = ChangelogSplit1093Tests.All().Split('\n');
            var headings = lines.Where(l => System.Text.RegularExpressions.Regex.IsMatch(l,@"^## \d+\.\d+\.\d+")).Select(l => l.Trim()).ToList();
            var twice = headings.GroupBy(h => h).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            Check(twice.Count == 0,"versions with two headings: " + string.Join(", ",twice));
            Check(headings.Count > 900,"the CHANGELOG still holds its history");
        }),
    ];
}
