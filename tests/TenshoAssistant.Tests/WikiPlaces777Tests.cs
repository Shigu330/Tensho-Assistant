// 0.777 (X-22, user report): a wiki place with several place rows keeps its right click.
internal static class WikiPlaces777Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Wiki places: every place row of the name is tried, on both pages that show wiki coordinates",() =>
        {
            Check(Read("Plugin.cs").Contains("group=>(IReadOnlyList<uint>)group.Select(row=>row.RowId).ToArray()"),"all rows, not the first");
            var blue=Read("MainWindow.BlueSpellSources.cs");
            Check(blue.Contains("foreach(var id in EnglishPlaceIds?.Invoke(place) ?? [])"),"tried in turn");
            Check(blue.Contains("MapOfEnglishPlace(place.Place) is not { } map") && Read("MainWindow.Monsters.cs").Contains("MapOfEnglishPlace(place.Place) is not { } map"),"both pages");
            Check(!Read("MainWindow.Monsters.cs").Contains("EnglishPlaceId?.Invoke"),"the old lookup is gone");
        }),
    ];
}
