// 0.1866 (X-860): review 0.1864, M1. On a lasting refusal, write_atomic of tools/discoveries-merge.py tried the temporary file's removal
// once and swallowed its failure: the .tmp could stay beside the target, unsaid, when the antivirus or the indexer held it. Now the removal
// is tried again with the rename's waits, and one still refused is said with the file's name. The behaviour is checked by the tool's
// selftest (run by Anonymous1832Tests, so by Verify and CI): removals made up, then on Windows the .tmp really held by another handle, let
// go during the waits, then held for good. Here: the wiring.
internal static class TempHeld1866Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Temp held 1866: write_atomic tries the temporary file's removal again and says one still refused; the selftest holds the .tmp for real",() =>
        {
            var tool=File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py")).ReplaceLineEndings("\n");
            Check(tool.Contains("def remove_retrying(path,remove=None,sleep=None):",StringComparison.Ordinal)
                && tool.Contains("if not remove_retrying(temporary,remove,sleep):",StringComparison.Ordinal)
                && tool.Contains("is still held by another program and was left",StringComparison.Ordinal),"the removal retried, a leftover said");
            Check(!tool.Contains("try:os.remove(temporary)\n        except OSError:pass",StringComparison.Ordinal),"no removal tried once and swallowed");
            Check(tool.Contains("X-860: a .tmp held by another program, let go during the removal's waits, is removed",StringComparison.Ordinal)
                && tool.Contains("X-860: a .tmp held for good is left, and said with its name",StringComparison.Ordinal)
                && tool.Contains("X-860: a temporary file whose removal stays refused is said",StringComparison.Ordinal),"the selftest's cases");
            var log=File.ReadAllText(Path.GetFullPath("CHANGELOG.md"));
            Check(!log.Contains("le fichier visé intact et sans fichier temporaire laissé",StringComparison.Ordinal)
                && !log.Contains("the target file left as it was and no temporary file left",StringComparison.Ordinal),"the 0.1864 sentence made exact");
        }),
    ];
}
