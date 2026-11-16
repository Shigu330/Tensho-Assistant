// 0.1092 (audit A044, D-138): in GLOBAL-PLAN a done point is one short row (number, short title, « Done in 0.xxx » / « Fait en 0.xxx »);
// open points keep their full text. A size budget keeps the file from growing back.
// 0.1473 (X-489): GLOBAL-PLAN reached its budget again (308,805 bytes on 11/16: 155 open points, 502 done rows). The done rows now live in a
// page of their own, produced with it (GLOBAL-PLAN-DONE.md, both languages, its own budget); GLOBAL-PLAN keeps the open points whole and,
// under each table, the count of its done points with a link to their section. GLOBAL-PLAN now grows with the open points only.
internal static class PlanCompact1092Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static readonly System.Text.RegularExpressions.Regex DoneRow = new(@"\| (Done in|Fait en) 0\.\d+(\.\d+)? \|$");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("GLOBAL-PLAN: open points stay whole, no done row, each table links to its done points",() =>
        {
            var text = Read("docs/project/GLOBAL-PLAN.md");
            var rows = text.Split('\n').Where(l => l.StartsWith("| X-") || l.StartsWith("| W-")).ToList();
            Check(rows.Count(l => DoneRow.IsMatch(l.TrimEnd())) == 0,"no done row in GLOBAL-PLAN (they are in GLOBAL-PLAN-DONE)");
            // 0.1332: X-01, the open point checked here, was closed on 14/11: any open point now.
            Check(rows.Any(l => l.Length > 320),"an open point keeps its full text");
            var links = text.Split('\n').Where(l => l.Contains("](GLOBAL-PLAN-DONE.md#")).ToList();
            Check(links.Count(l => l.Contains(" of this table: [the done points](")) >= 10 && links.Count(l => l.Contains(" de ce tableau : [les points faits](")) >= 10,
                "the tables link to their done points in both halves: " + links.Count);
            // 0.1455 (X-454): counted on the text read with LF line ends: FileInfo.Length said 299,900 bytes on an LF checkout and 301,699 on
            // the CI's Windows checkout (CRLF), so the budget passed locally and failed in the CI.
            var size = System.Text.Encoding.UTF8.GetByteCount(text);
            // 0.1135 (relais 26): 160 KB → 200 KB; 0.1172 (relais 32): 200 KB → 250 KB; 0.1284 (relais 47): 250 KB → 300 KB: open points keep
            // their full text by design. 0.1473 (X-489): 215 KB measured once the done rows left.
            Check(size < 300_000,"GLOBAL-PLAN stays under its budget (300 KB, both languages): " + size);
        }),
        ("GLOBAL-PLAN-DONE: every done point is a short row in both halves, the sections of GLOBAL-PLAN, under its own budget",() =>
        {
            var text = Read("docs/project/GLOBAL-PLAN-DONE.md");
            var halves = text.Split("\n---\n");
            Check(halves.Length == 2 && halves[0].StartsWith("# Global plan: done points") && halves[1].Contains("# Plan global : points faits"),"two halves, English then French");
            var en = halves[0].Split('\n').Where(l => l.StartsWith("| X-") || l.StartsWith("| W-")).ToList();
            var fr = halves[1].Split('\n').Where(l => l.StartsWith("| X-") || l.StartsWith("| W-")).ToList();
            Check(en.Count > 400 && en.Count == fr.Count,"done X and W rows in both halves: " + en.Count + " / " + fr.Count);
            Check(en.All(l => DoneRow.IsMatch(l.TrimEnd()) || l.TrimEnd().EndsWith("| Done |")) && fr.All(l => DoneRow.IsMatch(l.TrimEnd()) || l.TrimEnd().EndsWith("| Fait |")),"each row says its version");
            var heavy = en.Concat(fr).Where(l => l.Length > 320).ToList();
            Check(heavy.Count == 0,"a done row stays short: " + string.Join(" / ",heavy.Select(l => l[..40])));
            var plan = Read("docs/project/GLOBAL-PLAN.md");
            foreach (var heading in halves[0].Split('\n').Where(l => l.StartsWith("## ")).Select(l => l[3..]))
                Check(plan.Contains("## " + heading.Split(" — ")[0]),"a section of the done page is a section of GLOBAL-PLAN: " + heading);
            var size = System.Text.Encoding.UTF8.GetByteCount(text);
            Check(size < 400_000,"GLOBAL-PLAN-DONE stays under its budget (400 KB, both languages): " + size);
        }),
    ];
}
