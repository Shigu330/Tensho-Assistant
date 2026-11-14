// 0.802 (user request): the agent's working files are kept out of the repository.
internal static class NoAgentFiles802Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Agent files: ignored by git, not linked from the README or the documents' index",() =>
        {
            var ignore=File.ReadAllText(Path.GetFullPath(".gitignore"));
            foreach(var entry in AgentPaths1329Tests.Ignored)Check(ignore.Contains(entry),"ignored: "+entry);
            Check(!AgentPaths1329Tests.Cites(File.ReadAllText(Path.GetFullPath("README.md"))) && !File.ReadAllText(Path.GetFullPath("docs/README.md")).Contains("](ai/"),"not linked");
        }),
    ];
}
