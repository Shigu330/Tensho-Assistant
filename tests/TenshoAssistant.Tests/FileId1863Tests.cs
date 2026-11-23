// 0.1863 (X-857): the flaky « Anonymous 1832 » (« X-830: anonymize again: nothing to do », once on 23/11) was not the load: a player file's
// random id (16 hex digits) is digits only about once in 1,800 draws, and such a name read as an issue number, so anonymize took the anonymous
// file for one still to rename. Now an id is never drawn digits only, and 16 digits never read as an issue's number; the tool's selftest
// (run by Anonymous1832Tests, so by Verify and CI) forces that draw every run. Here: the wiring of both.
internal static class FileId1863Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("File id 1863: a player file's random id is never digits only, and 16 digits never read as an issue's number; the selftest forces that draw",() =>
        {
            var tool=File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py"));
            Check(tool.Contains("if not found.isdigit() and found not in taken",StringComparison.Ordinal),"new_file_id refuses an id of digits only");
            Check(tool.Contains("return stem(name).isdigit() and not FILE_ID.fullmatch(stem(name))",StringComparison.Ordinal),"numbered: 16 digits are a random id");
            Check(tool.Contains("real_hex=secrets.token_hex;draws=iter([\"1234567890123456\"])",StringComparison.Ordinal)
                && tool.Contains("X-857: a random id is never digits only, and 16 digits never read as an issue's number",StringComparison.Ordinal),
                "the selftest forces a first draw of digits only, then checks anonymize again has nothing to do");
        }),
    ];
}
