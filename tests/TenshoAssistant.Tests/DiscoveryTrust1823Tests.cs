// 0.1823 (X-805, maintainer's decision of 22/11): the laptop worker merges by itself a trusted account's discoveries, only when every check
// passes (tools/discoveries-trust.py). Its decision is checked by its selftest (made-up pull requests, tickets and shares, run by the agent's
// check); here: the list is never inside the repository, the ticket's author decides, every red check is a doubt, the merge is pinned.
internal static class DiscoveryTrust1823Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discovery trust 1823: the trusted merge is pinned, the list outside the repository, the ticket's author decides, any doubt waits",() =>
        {
            var tool=Read("tools/discoveries-trust.py");
            Check(tool.Contains("\"--squash\",\"--match-head-commit\",pr[\"sha\"]",StringComparison.Ordinal),"squash pinned on the checked commit");
            Check(tool.Contains("if inside(path):raise ValueError(",StringComparison.Ordinal) && tool.Contains("DEFAULT_LIST=os.path.join(os.path.expanduser(\"~\"),\".tensho\",\"trusted-accounts.txt\")",StringComparison.Ordinal),
                "the list outside the repository");
            // X-830: a batch: each share's ticket author must be on the list, and own its gist
            Check(tool.Contains("if any((r.get(\"login\") or \"\").lower() not in trusted for r in read):return \"skip\"",StringComparison.Ordinal) && tool.Contains("if m.owner_doubt(r.get(\"owner\") or \"\",issue):return \"doubt\",\"owner\"",StringComparison.Ordinal),
                "the ticket's author, the gist's owner");
            foreach(var name in new[]{"a trusted account, every check green: merged","an account not on the list: nothing done","the ticket's author decides, never the pull request's",
                "its CI still runs: wait","a red check gives a doubt, never a merge","init writes the owner's account once, never over","the list never inside the repository"})
                Check(tool.Contains(name,StringComparison.Ordinal),"selftest case: "+name);
            Check(!tool.Contains("docs/"+"ai",StringComparison.Ordinal) && !tool.Contains("scripts/"+"agent",StringComparison.Ordinal),"no link to the agent's files");
            Check(Read("community-discoveries/README.md").Contains("since Tensho 0.1823, the share of an account on",StringComparison.Ordinal)
                && Read("tools/README.md").Contains("| `discoveries-trust.py` |",StringComparison.Ordinal),"said in the READMEs");
        }),
    ];
}
