// 0.1231 (X-221 part 1, user's request of 12/11, K-37): the Grace of a manual. The game files hold its cap only in the manuals' descriptions
// (docs/evidence/0.1231-grace-cap.md); whether the status keeps the points left is read in game first. Tests: the developer reading covers the
// crafters and cites the evidence; the probe is listed; nothing shows an « experience left » yet (never a guessed value).
internal static class GraceReading1231Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Source(string path)=>File.ReadAllText(Path.GetFullPath(path));

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Grace reading 1231: the developer reading logs the statuses of crafters and gatherers, evidence cited",() =>
        {
            var capture=Source("src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs");
            Check(capture.Contains("if(job is >= 8 and <= 18)ReadStatuses(local);"),"crafters 8 to 15 and gatherers 16 to 18");
            Check(capture.Contains("docs/evidence/0.1231-grace-cap.md") && File.Exists(Path.GetFullPath("docs/evidence/0.1231-grace-cap.md")),"evidence cited and present");
        }),
        ("Grace reading 1231: the probes are listed in tools/README in both halves; no « experience left » shown yet",() =>
        {
            var readme=Source("tools/README.md");
            Check(readme.Split("`PROBE_GRACE`").Length==3 && readme.Split("`PROBE_MOUNT`").Length==3,"PROBE_GRACE and PROBE_MOUNT in English and French");
            Check(!Source("src/TenshoAssistant/Localization/Languages/en.json").Contains("Grace remaining",StringComparison.OrdinalIgnoreCase),"nothing shown before the reading in game");
        }),
    ];
}
