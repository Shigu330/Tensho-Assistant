// 0.1085 (audit A073, D-138): every file of docs/evidence is cited by a tracked file (code, tests, documents, CHANGELOG or another
// evidence file); the six cited nowhere were removed and the produced index counts 0 « cited nowhere ».
internal static class EvidenceCited1085Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Evidence: none cited nowhere",() =>
        {
            var folder = Path.GetFullPath("docs/evidence");
            foreach (var gone in new[] { "0.678-felicite-terms.md","0.704-place-menus.md","0.73.0-native-craft-probe.log",
                                         "0.74.0-native-craft-probe.log","0.99.0-maelstrom-probe.log","craft-log-api-probe.log" })
                Check(!File.Exists(Path.Combine(folder,gone)),"removed: " + gone);
            var readme = File.ReadAllText(Path.Combine(folder,"README.md")).Replace("\r\n","\n");
            Check(readme.Contains("; 0 cited nowhere · citées nulle part."),"the index counts 0 evidence cited nowhere");
            Check(!readme.Contains("| — | — |"),"no row without a citation");
        }),
    ];
}
