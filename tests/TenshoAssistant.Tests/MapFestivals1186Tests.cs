// 0.1186 (X-01 C3.1 and C3.2, analysis of the slow frames of 12/11): the map no longer builds its vendors twice after a load (without, then with
// the festivals): it waits the tick the festivals' state needs; and the map asks for the running festivals only, the event layouts (about
// 1 100 files) being read for the Events page alone.
internal static class MapFestivals1186Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Map festivals 1186: the vendors wait for the festivals' state once, then build; a reader that cannot read does not hold them", () =>
        {
            var view = Read("src/TenshoAssistant/MainWindow.MapView.cs");
            var wait = view.IndexOf("if(festivals is not null && festivalState is null && !festivals.Unavailable && mapVendorCache.Festivals.Length==0)", StringComparison.Ordinal);
            var build = view.IndexOf("var running=festivalState?.Active", StringComparison.Ordinal);
            Check(wait > 0 && build > wait, "the wait comes before the build");
            Check(view.Contains("return mapVendorCache.MapId==map.MapId && ReferenceEquals(mapVendorCache.Catalog,catalog) ? mapVendorCache.Rows:[];", StringComparison.Ordinal), "nothing built meanwhile");
            Check(view.Contains("var festivals=Festivals?.Invoke();festivals?.Want();", StringComparison.Ordinal), "the map asks for the state only");
        }),
        ("Map festivals 1186: the layouts are read for the Events page only, once", () =>
        {
            var reader = Read("src/TenshoAssistant/Events/FestivalReader.Dalamud.cs");
            var want = reader.IndexOf("public void Want()=>Volatile.Write(ref wantedUntilTicks,", StringComparison.Ordinal);
            var layouts = reader.IndexOf("public void WantLayouts()", StringComparison.Ordinal);
            Check(want > 0 && layouts > want, "Want and WantLayouts apart");
            Check(reader.IndexOf("Interlocked.Exchange(ref layoutsStarted,1)", StringComparison.Ordinal) > layouts, "layouts started by WantLayouts only");
            Check(Read("src/TenshoAssistant/MainWindow.Events.cs").Contains("reader?.WantLayouts();", StringComparison.Ordinal), "the Events page asks for the layouts");
        }),
    ];
}
