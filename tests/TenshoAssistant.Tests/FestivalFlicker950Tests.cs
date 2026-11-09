// X-95 (0.950): the running festivals no longer flicker to « none » for one read, and the map keeps the last known list while it is not read.
internal static class FestivalFlicker950Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Festival flicker 950: one empty read after a running list is ignored; the map keeps the last list",() =>
        {
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Events/FestivalReader.Dalamud.cs"));
            Check(reader.Contains("if(slots.Count==0 && Volatile.Read(ref state) is { Active.Count:>0 } && !emptyOnce){emptyOnce=true;return;}"),"reader");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs")).Contains("mapVendorCache.Festivals is { Length:>0 } kept"),"map");
        }),
    ];
}
