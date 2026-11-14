// 0.1088 (audit A048, D-138): BACKLOG and USER-REQUESTS are removed; the R-01 to R-14 table lives once, in ROADMAP, and new requests
// go to the global plan as X-NN points.
internal static class RequestsOnce1088Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Requests: one R table in ROADMAP, BACKLOG and USER-REQUESTS gone",() =>
        {
            Check(!File.Exists(Path.GetFullPath("docs/project/BACKLOG.md")) && !File.Exists(Path.GetFullPath("docs/project/USER-REQUESTS.md")),"both files are removed");
            var roadmap = Read("docs/project/ROADMAP.md");
            Check(roadmap.Contains("This table is the only list of the requests R-01 to R-14") && roadmap.Contains("Ce tableau est la seule liste des demandes R-01 à R-14"),"ROADMAP holds the R table in both halves");
            Check(roadmap.Split('\n').Count(l => l.StartsWith("| R-14 |")) == 2,"one R-14 row per half of ROADMAP");
            var project = Read("docs/project/PROJECT.md");
            Check(!project.Contains("| Request | Domain | Where in the code |") && !project.Contains("| Demande | Domaine | Où dans le code |"),"PROJECT no longer repeats an R table");
            foreach (var path in new[] { "docs/project/PROJECT.md","docs/project/ROADMAP.md","docs/project/DECISIONS.md","docs/project/GAPS.md",
                                         "tools/README.md","docs/README.md" })
            {
                var text = Read(path);
                Check(!text.Contains("BACKLOG.md") && !text.Contains("USER-REQUESTS.md"),path + " no longer names a removed file");
            }
        }),
    ];
}
