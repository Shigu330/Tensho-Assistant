using TenshoAssistant.Diagnostics;

// 0.1204 (X-177 C6): research lines written whole once per game session, across the plugin's reloads (SessionOnce); then one short line
// that keeps the start the log reader looks for.
internal static class SessionOnce1204Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Session once 1204: behaviour: a text is first once per slot, shared by two loads; another text is first; the memory is bounded and holds only runtime integers", () =>
        {
            object? slot = null;
            Func<object?> read = () => slot; Action<object> write = value => slot = value;
            Check(SessionOnce.First("Submarine voyage check: a", read, write), "first time: written whole");
            Check(!SessionOnce.First("Submarine voyage check: a", read, write), "again in the same load: short");
            // A second load of the plugin reads the same process slot (the static state of the first is gone with it).
            Check(!SessionOnce.First("Submarine voyage check: a", () => slot, value => slot = value), "after a reload: short");
            Check(SessionOnce.First("Submarine voyage check: b", read, write), "another text: written whole");
            Check(slot is HashSet<int> set && set.Count == 2 && typeof(HashSet<int>).Assembly != typeof(SessionOnce).Assembly, "integers in a runtime set: nothing of the plugin kept alive");
            for (var i = 0; i < SessionOnce.Limit + 10; i++) SessionOnce.First("line " + i, read, write);
            Check(((HashSet<int>)slot!).Count <= SessionOnce.Limit, "bounded");
            var unique = "real slot " + Guid.NewGuid().ToString("N");
            Check(SessionOnce.First(unique) && !SessionOnce.First(unique), "the process slot itself");
            Check(AppDomain.CurrentDomain.GetData(SessionOnce.Slot) is HashSet<int>, "kept in the process data");
        }),
        ("Session once 1204: wiring: the three research lines go through it, the short lines keep the start logread.py looks for", () =>
        {
            var reading = Read("src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs");
            Check(reading.Contains("if(TenshoAssistant.Diagnostics.SessionOnce.First(\"Reading \"+part+\": \"+text))log.Information(\"Reading {0}: {1}\",part,text);", StringComparison.Ordinal)
                && reading.Contains("else log.Information(\"Reading {0}: the same as earlier in this game session ({1} characters).\",part,text.Length);", StringComparison.Ordinal), "Reading …");
            var probe = Read("src/TenshoAssistant/FreeCompany/SubmarineVoyageProbe.Dalamud.cs");
            Check(probe.Contains("if(TenshoAssistant.Diagnostics.SessionOnce.First(string.Join(\"\\n\",lines)))foreach(var line in lines)log.Info(line);", StringComparison.Ordinal)
                && probe.Contains("else log.Info($\"Submarine voyage check (territory {triedIn}): the same {lines.Count} lines", StringComparison.Ordinal), "Submarine voyage check …");
            var map = Read("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs");
            Check(map.Contains("&& TenshoAssistant.Diagnostics.SessionOnce.First($\"Map event marker {marker.IconId}", StringComparison.Ordinal), "Map event marker …");
        }),
    ];
}
