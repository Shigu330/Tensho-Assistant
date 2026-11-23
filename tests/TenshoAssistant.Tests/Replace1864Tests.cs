// 0.1864 (X-858): review 0.1863. M1: on Windows, os.replace is sometimes refused for a moment (WinError 5, an antivirus or the indexer holding
// the new file); write_atomic of tools/discoveries-merge.py now tries the rename again a few times, and the tool's selftest (run by
// Anonymous1832Tests, so by Verify and CI) makes up two refusals, then a lasting one. M2: name_order, rebuild's fallback owner and withdraw
// read an issue number by numbered() as well, so 16 digits are a random id everywhere. Here: the wiring.
internal static class Replace1864Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Replace 1864: write_atomic tries the rename again on a passing refusal; 16 digits never read as an issue number anywhere",() =>
        {
            var tool=File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py"));
            Check(tool.Contains("replace_retrying(lambda:os.replace(temporary,path) if replace is None else replace(temporary,path),sleep)",StringComparison.Ordinal)
                && tool.Contains("except PermissionError:\n            if attempt==REPLACE_TRIES-1:raise",StringComparison.Ordinal),"write_atomic retries on PermissionError, then raises");
            Check(tool.Contains("X-858 (M1): write_atomic tries the rename again after two refusals",StringComparison.Ordinal)
                && tool.Contains("X-858 (M1): a refusal that lasts is raised",StringComparison.Ordinal),"the selftest makes up the refusals");
            Check(tool.Contains("def name_order(n):return (0,int(stem(n)),n) if numbered(n) else (1,0,n)",StringComparison.Ordinal)
                && tool.Contains("int(issue) if numbered(issue) else -1))",StringComparison.Ordinal)
                && tool.Contains("if numbered(name) and name in links:",StringComparison.Ordinal)
                && tool.Contains("elif numbered(name):paths=",StringComparison.Ordinal),"name_order, rebuild and withdraw use numbered()");
            Check(!tool.Contains("if stem(n).isdigit() else",StringComparison.Ordinal) && !tool.Contains("if name.isdigit() and name in links",StringComparison.Ordinal),
                "no reading of digits only as an issue number is left");
        }),
    ];
}
