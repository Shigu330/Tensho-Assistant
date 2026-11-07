// 0.762 (X-15, user request): « Go there » in an item's right-click menu.
internal static class GoThereMenu762Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Go there from the menu: the same steps as the Gathering page, an open and near point first, only when placed",() =>
        {
            var links=Read("MainWindow.DutyFinder.cs");
            Check(links.Contains("if(GoThereSource(item.Id) is { } go && ImGui.Selectable(localization.Get(\"GatheringDetails.GoThere\")"),"in the menu");
            Check(links.Contains("AskNearestAetheryteAt(go.Target.Map,go.Target.Pixel,") && links.Contains("direct:configuration.GoThereTeleport"),"the same steps: a question unless the setting says otherwise");
            Check(links.Contains(".ThenBy(pair=>pair.Source.Zone==here ? 0:1).ThenBy(pair=>ListPanelZoneCost(pair.Source.Zone) ?? uint.MaxValue)"),"near first");
        }),
    ];
}
