// 0.1059 (audit A055): G-PROPOSALS no longer said that nothing is decided, and every table row sat under a header.
// 0.1348 (X-357, the user's sort of 14/11): the proposal documents are removed; a link to one is a permanent link to its last version.
internal static class ProposalsShape1059Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly string[] Removed = ["MAP-PROPOSALS","ITEMS-PROPOSALS","CRAFT-GATHER-PROPOSALS","TOOLS-PROPOSALS","G-PROPOSALS","PROPOSALS-TRIAGE","CONTENT-TOOLS"];
    private const string Blob = "https://github.com/Shigu330/Tensho-Assistant/blob/024c426c1186a2e8f51e09087996a9c55208c28a/docs/project/";

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Proposals: the working documents are removed and no living document links them by a relative link",() =>
        {
            foreach (var name in Removed)
                Check(!File.Exists(Path.GetFullPath("docs/project/" + name + ".md")),name + " removed");
            foreach (var path in new[] { "docs/README.md","docs/project/GLOBAL-PLAN.md","docs/project/DECISIONS.md","docs/project/GAPS.md","docs/project/IDS.md" })
            {
                var text = File.ReadAllText(Path.GetFullPath(path));
                foreach (var name in Removed)
                    Check(!text.Contains("](" + name + ".md") && !text.Contains("](project/" + name + ".md"),path + " links " + name + " by a relative link");
            }
        }),
        ("Proposals: the links that remain open the last version of the document",() =>
        {
            var plan = File.ReadAllText(Path.GetFullPath("docs/project/GLOBAL-PLAN.md"));
            Check(plan.Contains("](" + Blob + "archive/GLOBAL-PLAN-X-2026-11-09.md#x-43)"),"the global plan reaches the archived detail of X-43 by its permanent link");
            Check(File.ReadAllText(Path.GetFullPath("docs/project/DECISIONS.md")).Contains(Blob + "CONTENT-TOOLS.md"),"DECISIONS reaches CONTENT-TOOLS by its permanent link");
        }),
    ];
}
