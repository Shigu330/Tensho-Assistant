// 0.1059 (audit A055): G-PROPOSALS no longer says that nothing is decided, and every table row there sits under a table header.
internal static class ProposalsShape1059Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Proposals: no 'nothing decided' claim, decided proposal 11 under its own heading",() =>
        {
            var text = File.ReadAllText(Path.GetFullPath("docs/project/G-PROPOSALS.md")).Replace("\r\n","\n");
            Check(!text.Contains("Nothing here is decided yet") && !text.Contains("Rien ici n'est encore\ndécidé"),"no claim that nothing is decided");
            Check(text.Contains("## Proposal 11 (X-40): nearby monsters, other uses — **decided (DEC-049)**"),"English heading of proposal 11");
            Check(text.Contains("## Proposition 11 (X-40) : monstres proches, autres usages — **décidée (DEC-049)**"),"French heading of proposal 11");
        }),
        ("Proposals: every table row follows a header row or another row",() =>
        {
            var lines = File.ReadAllText(Path.GetFullPath("docs/project/G-PROPOSALS.md")).Replace("\r\n","\n").Split('\n');
            for(var index = 1; index < lines.Length; index++)
                if(lines[index].StartsWith("| ",StringComparison.Ordinal) && !lines[index-1].StartsWith("|",StringComparison.Ordinal))
                    Check(index + 1 < lines.Length && lines[index+1].StartsWith("|---",StringComparison.Ordinal),"line " + (index+1) + " is a table row outside a table");
        }),
    ];
}
