// 0.1092 (audit A044, D-138): in GLOBAL-PLAN a done point is one short row (number, short title, « Done in 0.xxx » / « Fait en 0.xxx »);
// open points keep their full text. A size budget keeps the file from growing back.
internal static class PlanCompact1092Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("GLOBAL-PLAN: done points are short rows, open points stay whole",() =>
        {
            var path = Path.GetFullPath("docs/project/GLOBAL-PLAN.md");
            var text = File.ReadAllText(path).Replace("\r\n","\n");
            var rows = text.Split('\n').Where(l => l.StartsWith("| X-") || l.StartsWith("| W-")).ToList();
            var done = rows.Where(l => l.TrimEnd().EndsWith("|") && (System.Text.RegularExpressions.Regex.IsMatch(l,@"\| (Done in|Fait en) 0\.\d+ \|$") )).ToList();
            Check(done.Count > 200,"done X and W rows are compact in both halves: " + done.Count);
            var heavy = done.Where(l => l.Length > 320).ToList();
            Check(heavy.Count == 0,"a done row stays short: " + string.Join(" / ",heavy.Select(l => l[..40])));
            Check(rows.Any(l => l.StartsWith("| X-01 |") && l.Length > 320),"an open point (X-01) keeps its full text");
            var size = new FileInfo(path).Length;
            // 0.1135 (relais 26): 160 KB → 200 KB: the open points added since 0.1118 (X-165 to X-176, about 18 KB in both languages) keep their full text
            // by design; done rows stay short (checked above).
            Check(size < 200_000,"GLOBAL-PLAN stays under its budget (200 KB, both languages): " + size);
        }),
    ];
}
