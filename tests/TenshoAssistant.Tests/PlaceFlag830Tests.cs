// 0.830 (X-60, user request): « Place the flag » in the item menu; why « Go there » is missing, logged.
internal static class PlaceFlag830Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Place flag: on the same point as « Go there », without a teleport; a missing one logged in developer mode, once per item",() =>
        {
            var menu=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.DutyFinder.cs"));
            Check(menu.Contains("FlagFromMap(point.Target.Map,place.X,place.Y);"),"flag only");
            Check(menu.Contains("if(go is null && developerMode && gatheringCatalog.State.Catalog?.ForItem(item.Id) is { Count:>0 } sources && goThereLogged.Add(item.Id))"),"logged once");
        }),
    ];
}
