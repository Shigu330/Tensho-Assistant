using TenshoAssistant.Inventory;

// X-814 (0.1848, review of the block 0.1807 to 0.1812): M2, the « sleeps and sells » list compared a high-quality line's market price with
// the NPC's normal-quality one; M3, a failed « Best team » calculation was kept for good, never logged.
internal static class ReviewMinors1848Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review minors 1848: behaviour: a high-quality line is never told cheaper at an NPC; a normal-quality line keeps its gap", () =>
        {
            Check(StockValue.NpcBuyGap(120, 400, hq: true) is null, "HQ sold 400, NPC sells NQ at 120: no gap told");
            Check(StockValue.NpcBuyGap(120, 400, hq: false) == 280, "NQ: 280 cheaper at the NPC");
            Check(StockValue.NpcBuyGap(500, 400, hq: false) == -100, "NQ: the market cheaper");
            Check(StockValue.NpcBuyGap(null, 400, hq: false) is null && StockValue.NpcBuyGap(120, 0, hq: false) is null, "unknown");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.StockValue.cs"));
            Check(page.Contains("Inventory.StockValue.NpcBuyGap(buy,line.Sold,line.Hq) is > 0 and var less", StringComparison.Ordinal)
                && !page.Contains("MarketOverNpcBuy(buy,line.Sold)", StringComparison.Ordinal), "the list asks with the line's quality");
        }),
        ("Review minors 1848: wiring: a failed « Best team » calculation is logged once and worked out again 10 s later", () =>
        {
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("failed.Plan is { IsFaulted: true } or { IsCanceled: true }", StringComparison.Ordinal), "a failure is seen");
            // X-848 M3: « logged once » is now true (BestTeamRetry, its behaviour in ReviewMinors1851Tests): the log line on Step.Log only.
            Check(page.Contains("case BestTeamRetry.Step.Log:", StringComparison.Ordinal) && page.Contains("could not be worked out; worked out again in 10 s, then less often, without this line.\", board.Id);", StringComparison.Ordinal), "logged once");
            Check(page.Contains("case BestTeamRetry.Step.Retry: beastBestPlans.Remove(board.Id); break;", StringComparison.Ordinal), "worked out again");
        }),
    ];
}
