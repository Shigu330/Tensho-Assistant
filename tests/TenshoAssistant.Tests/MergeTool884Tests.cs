// 0.884 (X-77.1): the merge tool reads gists given as gist:ID and ignores the plugin's computed Key property.
internal static class MergeTool884Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Merge tool: gist prefix removed, computed Key accepted then dropped",() =>
        {
            var tool=File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py"));
            Check(tool.Contains("reference.removeprefix(\"gist:\")") && tool.Contains("f.pop(\"Key\",None)"),"fixed");
        }),
    ];
}
