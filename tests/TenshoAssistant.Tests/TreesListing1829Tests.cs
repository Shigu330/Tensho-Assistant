// 0.1829 (X-827, review of 0.1826, M3): the trusted merge (tools/discoveries-trust.py) listed community-discoveries/players and v3 through
// GitHub's contents API, which lists at most 1 000 entries of a folder: past 1 000 player files, the base replayed and the pull request's head
// were read cut short, and every trusted merge became a public doubt (« TELL THE MAINTAINER »). Now the git trees API, walked folder by folder
// from the commit's tree, without that bound; a tree GitHub says is truncated, a link or a folder where plain files are written: refused. The
// behaviour is in the tool's selftest (1 201 player files listed, truncated tree, link, folder), run by DiscoveryOrigin1824Tests; here the wiring.
internal static class TreesListing1829Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Trees listing 1829: the trusted merge lists the discoveries folders through the git trees API, never the contents API's 1 000 entries",() =>
        {
            var tool=Read("tools/discoveries-trust.py");
            var listing=tool[tool.IndexOf("def folder_blobs(ref):",StringComparison.Ordinal)..tool.IndexOf("def blob(sha):",StringComparison.Ordinal)];
            Check(listing.Contains("gh(\"api\",f\"repos/{REPOSITORY}/git/trees/{sha}\")",StringComparison.Ordinal) && !listing.Contains("/contents/",StringComparison.Ordinal),
                "the folders listed through git/trees, not contents");
            Check(listing.Contains("data.get(\"truncated\")",StringComparison.Ordinal) && listing.Contains("entry.get(\"mode\") not in (\"100644\",\"100755\")",StringComparison.Ordinal),
                "a truncated tree, a link or a folder refused");
            foreach(var name in new[]{"X-827: past 1 000 player files, every one listed with its blob","X-827: a tree GitHub says is truncated: refused, never a partial list",
                "X-827: a link or a folder where a sync writes plain files: refused","X-827: no community-discoveries folder: nothing listed"})
                Check(tool.Contains(name,StringComparison.Ordinal),"selftest case: "+name);
            Check(tool.Contains("if \"/git/trees/\" in url:return fake_tree(trees,url.rsplit(\"/git/trees/\",1)[1])",StringComparison.Ordinal),
                "the end-to-end cases read the made-up GitHub through the trees API too");
        }),
    ];
}
