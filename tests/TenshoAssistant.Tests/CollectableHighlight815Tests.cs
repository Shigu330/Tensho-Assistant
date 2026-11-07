// 0.815 (X-51, user request): the collectable plan's move framed on the hotbars.
internal static class CollectableHighlight815Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Collectable highlight: the plan's move, behind the gathering help's setting, not when impossible",() =>
        {
            var panel=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CollectablePanel.cs"));
            Check(panel.Contains("if(!advice.Impossible && action!=0 && configuration.GatheringNextHighlight && BarLocator is { } located)"),"conditions");
            Check(panel.Contains("located.Want(action);") && panel.Contains("else DrawGatheringBarHighlight(located);"),"same frame");
        }),
    ];
}
