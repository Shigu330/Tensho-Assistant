// 0.398 (g, R-04): bait in hand — read on the framework thread for fishers only, shown with its stock, matching fish framed.
internal static class FishingBait398Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Fishing bait: framework thread, fishers only, shown with stock and matching fish",() =>
        {
            var reader = Read("Gathering/FishingBaitReader.Dalamud.cs");
            Check(reader.Contains("framework.Update+=OnUpdate;",StringComparison.Ordinal) && reader.Contains("ClassJob.RowId==18 && state!=null && state->IsLoaded ? state->FishingBait:-1",StringComparison.Ordinal),"read");
            var panel = Read("MainWindow.FishingPanel.cs");
            Check(panel.Contains("rows.Count(row=>row.Route.Count>0 && row.Route[0]==held)",StringComparison.Ordinal) && panel.Contains("inHand==row.Route[0]",StringComparison.Ordinal),"shown");
        }),
    ];
}
