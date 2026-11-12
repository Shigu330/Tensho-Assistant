using TenshoAssistant.Gathering;

// 0.1167 (X-134, user's captures of 2026-11-12, Miner 86, a quarry with Thanalan topsoil III « RARE »): after the one take of the rare item the
// panel planned the next item (dark matter cluster) from the node as it opened (« without GP: 6.0 », 6 attempts) instead of what was left (5).
// Now an item planned after GP or attempts went on another one is planned on what is left then, and its next step is counted from that start.
internal static class GatherRest1167Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Gather rest 1167: the plan's start: the node as it opened, a late item's first reading, or what was left for a later item", () =>
        {
            Check(GatheringNodes.PlanStart(false, 800, 6, 650, 6) == (800, 6), "the item first worked on: the node as it opened");
            Check(GatheringNodes.PlanStart(false, 800, 6, 650, 6, (300, 5)) == (300, 5), "the next item after the rare one's take: what was left (5 attempts)");
            Check(GatheringNodes.PlanStart(true, 800, 6, 650, 4, (300, 5)) == (650, 4), "a late item keeps its own reading (X-129)");
            Check(GatheringNodes.PlansRest(5111, 7766) && !GatheringNodes.PlansRest(5111, 5111) && !GatheringNodes.PlansRest(0, 7766) && !GatheringNodes.PlansRest(5111, 0),
                "another item than the one worked on, once something was spent");
        }),
        ("Gather rest 1167: wiring (the item worked on kept per node, the rest's start taken once, the next step counted from the plan's start, said under the plan)", () =>
        {
            var panel = Read("src/TenshoAssistant/MainWindow.GatheringPanel.cs");
            Check(panel.Contains("if(start.Frozen && start.WorkedOn==0)start.WorkedOn=") && panel.Contains("if(rest && start.Rest?.Item!=slot.ItemId)start.Rest=(slot.ItemId,(int)frame.Gp,frame.Attempts);"), "worked-on item and rest start, once per item");
            Check(panel.Contains("GpNextAction.Next(plan,optimization.Scenario.Budget,(int)frame.Gp,startAttempts,attempts)") && panel.Contains("var startAttempts=optimization.Scenario.Attempts;"), "steps counted from the plan's start");
            Check(panel.Contains("localization.Format(\"GatheringPanel.PlanRest\",plan.Scenario.Budget,plan.Scenario.Attempts)"), "said under the plan");
            foreach (var language in new[] { "en", "fr" }) Check(Read($"src/TenshoAssistant/Localization/Languages/{language}.json").Contains("\"PlanRest\": "), language);
        }),
    ];
}
