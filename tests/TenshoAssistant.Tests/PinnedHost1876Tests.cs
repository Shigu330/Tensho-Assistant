// X-871 (0.1876): the public evidence of X-120 said the pinned copy's .cs files were "the same as at 28f3160", already false before
// X-309 (stamp and clock written here first) and false since (the copy follows upstream: v0.2.0 plus later commits). It now gives the
// commit of the move as history and sends to tools/PluginUiTest/README.md, the one place that names the version and commit of the copy.
internal static class PinnedHost1876Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath(file)).Replace("\r\n","\n");

    internal static readonly (string Name,Action Run)[] Cases =
    [
        ("X-871: the evidence no longer says the copy is the same as at 28f3160; it names the README, which names the copy's commit",() =>
        {
            var evidence = Read("docs/evidence/x120-plugin-ui-test.md");
            var flat = string.Join(" ",evidence.Split(new[]{'\n',' '},StringSplitOptions.RemoveEmptyEntries));
            Check(!flat.Contains("the same as at `28f3160`",StringComparison.Ordinal),"no claim that the copy is 28f3160");
            Check(flat.Contains("were those of `28f3160` at the move",StringComparison.Ordinal),"28f3160 kept as the commit of the move");
            Check(flat.Contains("are written in `tools/PluginUiTest/README.md`",StringComparison.Ordinal),"sends to the README");
            var commit = System.Text.RegularExpressions.Regex.Match(flat,@"v0\.2\.0 plus commit `([0-9a-f]{7})`\)");
            Check(commit.Success,"gives the copy's commit of the day");
            Check(Read("tools/PluginUiTest/README.md").Split("\n---\n").All(half=>half.Contains("`"+commit.Groups[1].Value+"`",StringComparison.Ordinal)),
                  "the same commit as both halves of the README");
        }),
    ];
}
