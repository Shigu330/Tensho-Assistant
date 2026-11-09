// X-85 V5 (0.963): ocean fish that need Fisher's Intuition are marked, with the predators to catch first.
internal static class OceanIntuition963Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Ocean intuition 963: eye mark and predators in the tooltip",() =>
        {
            var panel=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.OceanVoyagePanel.cs"));
            Check(panel.Contains("gatherBuddyFish?.State.Data?.Find(fishId) is { Predators.Count:>0 } needs") && panel.Contains("FontAwesomeIcon.Eye"),"marked");
            Check(panel.Contains("localization.Format(\"FishingPanel.Predators\""),"predators");
        }),
    ];
}
