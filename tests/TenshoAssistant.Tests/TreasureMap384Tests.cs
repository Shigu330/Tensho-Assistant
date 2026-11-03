// 0.384 (g, M-14): deciphered treasure map — game functions called only once resolved, resolved through TreasureSpot and Level,
// drawn with the treasure layer, shown from the Reminders page.
internal static class TreasureMap384Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Treasure map: functions checked before the call, spot from TreasureSpot and Level, map layer and Reminders button",() =>
        {
            var reader = Read("Maps/TreasureMapReader.Dalamud.cs");
            Check(reader.Contains("(nint)EventItemManager.MemberFunctionPointers.GetTreasureHuntRank==0",StringComparison.Ordinal)
                && reader.Contains("(nint)EventItemManager.MemberFunctionPointers.GetTreasureSpotSubKey==0",StringComparison.Ordinal),"addresses checked");
            Check(reader.Contains("GetSubrowExcelSheet<TreasureSpot>().TryGetSubrow(rank,sub,out var row)",StringComparison.Ordinal),"TreasureSpot row = rank, subrow = sub key");
            Check(Read("MainWindow.MapView.cs").Contains("GlyphTreasureMap=0xFFFF0006",StringComparison.Ordinal)
                && Read("MainWindow.Reminders.cs").Contains("ShowOnTenshoMap(treasureMap,treasure.Position)",StringComparison.Ordinal),"shown");
        }),
    ];
}
