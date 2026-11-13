using TenshoAssistant.Maps;

// 0.1206 (X-01 C5): the map's names and aetheryte rows built once, off the frame after loading (the first region map took 1 311 ms on the
// game's thread); each tab of Storage and Settings timed in developer mode (their pics of 3.1 and 2.1 s had no tab to blame).
internal static class MapWarm1206Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Map warm 1206: behaviour: built once when the frame and the warm-up ask at the same time; the second waits and gets the same value", () =>
        {
            var builds = 0; using var started = new ManualResetEventSlim(false);
            var once = new BuiltOnce<string[]>(() => { Interlocked.Increment(ref builds); started.Set(); Thread.Sleep(200); return ["Limsa", "Gridania"]; });
            Check(!once.Built, "nothing before the first ask");
            var warm = Task.Run(() => once.Value);
            Check(started.Wait(5000), "the warm-up builds");
            var frame = once.Value; // asked while the warm-up is building: waits for it
            Check(ReferenceEquals(frame, warm.Result) && builds == 1 && once.Built, "one build, one value: " + builds);
            var many = Enumerable.Range(0, 16).Select(_ => Task.Run(() => once.Value)).ToArray();
            Task.WaitAll(many, 5000);
            Check(many.All(task => ReferenceEquals(task.Result, frame)) && builds == 1, "read without building again");
        }),
        ("Map warm 1206: behaviour: a build that fails keeps nothing: the caller gets the error, the next ask builds again", () =>
        {
            var calls = 0;
            var once = new BuiltOnce<string>(() => ++calls == 1 ? throw new InvalidDataException("sheet unreadable (test)") : "ok");
            var thrown = false;
            try { _ = once.Value; } catch (InvalidDataException) { thrown = true; }
            Check(thrown && !once.Built, "the first ask fails, nothing kept");
            Check(once.Value == "ok" && calls == 2 && once.Built, "the next ask builds again");
            Check(new BuiltOnce<string>(() => null!) is var empty && Throws(() => _ = empty.Value) && !empty.Built, "never a null value");
        }),
        ("Map warm 1206: wiring: the map names and aetherytes go through BuiltOnce and are warmed off the frame, stopped with the plugin; Storage and Settings tabs timed", () =>
        {
            var source = Read("src/TenshoAssistant/Maps/MapMarkerSource.Dalamud.cs");
            Check(source.Contains("private readonly BuiltOnce<NamedMapIndex> namedMaps;") && source.Contains("public IReadOnlyList<AetheryteRow> Aetherytes=>aetheryteRows.Value;")
                && source.Contains("private string AetheryteName(uint id)=>aetheryteNames.Value.GetValueOrDefault(id,\"\");") && !source.Contains("namedMapsLock") && !source.Contains("aetherytes=["), "built once");
            Check(source.Contains("public void Warm(CancellationToken token)") && source.Contains("_=namedMaps.Value;") && source.Contains("_=aetheryteNames.Value;"), "warm");
            var plugin = Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Contains("try{await Task.Delay(MapMarkersWarmDelay,gameDataCancel.Token).ConfigureAwait(false);mapMarkers.Warm(gameDataCancel.Token);}")
                && plugin.Contains("catch(OperationCanceledException){}") && plugin.Contains("gameDataCancel.Cancel();"), "off the frame, after the loading, stopped with the plugin");
            var storage = Read("src/TenshoAssistant/MainWindow.Storage.cs");
            foreach (var method in new[] { "DrawBankBrowser", "DrawCrystalsTab", "DrawCurrenciesTab", "DrawCurrencyValueTab", "DrawImportManager", "DrawCharactersTab", "DrawStorageReport", "DrawGilJournal" })
                Check(storage.Contains(", " + method + "); } finally", StringComparison.Ordinal) && storage.Contains("{ try { MeasureMap(\"storage ", StringComparison.Ordinal), "timed: " + method);
            Check(Read("src/TenshoAssistant/MainWindow.Settings.cs").Contains("try{if(DeveloperMode)MeasureMap(\"settings \"+key,draw);else draw();}"), "settings tabs timed");
        }),
    ];

    private static bool Throws(Action run)
    {
        try { run(); return false; }
        catch (InvalidOperationException) { return true; }
    }
}
