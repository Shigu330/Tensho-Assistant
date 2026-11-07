// 0.688 (user report): the Beastmaster page says where a beast is caught.
internal static class BeastPlaces688Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Beast places: columns 6 and 7 are a zone or a duty, read from their own sheets",() =>
        {
            var loader=Read("Beastmaster/BeastCatalogLoader.Dalamud.cs");
            Check(loader.Contains("1 => data.GetExcelSheet<PlaceName>().GetRowOrDefault(id)"),"zone");
            Check(loader.Contains("2 => data.GetExcelSheet<ContentFinderCondition>().GetRowOrDefault(id)"),"duty");
            Check(Read("Beastmaster/BeastCatalog.cs").Contains("catchPlace?.Invoke(row.Int(6), row.UInt(7))"),"columns");
        }),
        ("Beast places: the column is last, so the sort columns do not move",() =>
        {
            var page=Read("MainWindow.Beastmaster.cs");
            Check(page.Contains("BeginTable(\"BeastTable\", 9,") && page.Contains("localization.Get(\"BeastUi.CatchPlace\"), ImGuiTableColumnFlags.NoSort"),"column");
        }),
    ];
}
